# Kế hoạch hệ thống Trang bị (theo spec Huynh gửi 2026-10-29)

Nguồn: spec chi tiết của Huynh (phân loại 6 ô, set theo mốc cấp,
cường hóa/rèn/khảm ngọc/tẩy luyện/kế thừa, chiến thuật theo vị trí).
Art/code/text tự làm; chỉ lấy cơ chế.

## 1. Hiện trạng (cuối 2.29) vs mục tiêu

| Hạng mục | Hiện tại | Mục tiêu (spec) |
|---|---|---|
| Ô trang bị | 6 ô đúng spec (Vũ khí/Hộ uyển/Y giáp/Mũ/Hộ phù/Hài) | Giữ nguyên |
| Set đồ | Không có (món đồ chỉ có slot + cấp + tẩy) | Set theo mốc Lv30/50/70/80/90/100/110, phẩm Lam/Tím/Cam/Đỏ, bonus 2/4/6 món |
| Cường hóa | Tốn **Vàng** 100×(cấp+1), 100% thành công, tối đa +10 | Tốn **Bạc**, chi phí tăng theo cấp số nhân, tỉ lệ thành công/cooldown ở cấp cao, giới hạn theo cấp tướng chính |
| Tẩy luyện | Xoay vòng 5 tên, tốn 100 Vàng | 3–6 dòng ngẫu nhiên theo phẩm (Lam/Tím/Cam/Đỏ), tốn Đá tẩy luyện/Vàng, khóa dòng |
| Khảm ngọc | Chưa có (tab "Đục lỗ" hiện "Chưa mở.") | Lỗ khảm mở theo cấp/Bạc/Vàng; ngọc Tốc độ/Công/HP/Phòng/Né/Bạo...; ghép 3 viên N → 1 viên N+1 |
| Rèn trang bị | Chưa có | Bản vẽ (rớt phụ bản Tinh Anh/Boss/đổi điểm) + Nguyên liệu rèn; nâng phẩm giữ/kế thừa thuộc tính |
| Kế thừa | Chưa có (tab "Kế thừa" hiện "Chưa mở.") | Chuyển cấp cường hóa/tẩy luyện sang món mới, tốn ít Bạc/Vàng |
| Nguồn đồ | Seed 6 món Lv0 trong túi, không nguồn mới | Set rớt theo ải/Boss/shop/rèn (xem §3) |
| Chiến thuật vị trí | Đã có trong engine (Tiền +DEF/ưu tiên đỡ đòn, Trung +ATK, Hậu +tốc đánh trước) | Giữ; gợi ý build ngọc/tẩy theo vị trí là tài liệu, không code cứng |

## 2. Mô hình dữ liệu mới (nền cho mọi phase)

`EquipmentInstance` thêm:
- `Tier` (int): mốc set 30/50/70/80/90/100/110.
- `Quality` (string): "Lam"/"Tim"/"Cam"/"Do".
- `SetId` (string): id bộ set (vd "tan_thu", "chu_tuoc"...).
- `Sockets`: danh sách lỗ khảm (mỗi lỗ: đã mở?, ngọc đang khảm).
- `RefineLines`: danh sách dòng tẩy luyện (thay `RefineIndex` hiện tại).

`SetBonusService`: đếm số món cùng `SetId` đang mang trên 1 tướng →
áp bonus 2/4/6 món vào chỉ số hiệu lực (`ProgressionService.GetEffectiveHero`).
Bảng bonus lấy đúng spec Huynh (Chu Tước: 2 món +10% HP, 4 món +Công,
6 món +Tốc độ; Bạch Hổ/Thanh Long/Huyền Vũ/Kỳ Lân tương tự).

Migration: save cũ (không có Tier/Quality/SetId) → gán set "Tân Thủ"
Lv30 Lam cho đồ đang có; `RefineIndex` cũ → 1 dòng tẩy luyện tương ứng.

## 3. Các phase đề xuất

### Phase 2.30 — Mô hình dữ liệu + Set đồ (nền tảng)
- Thêm Tier/Quality/SetId/Sockets/RefineLines vào `EquipmentInstance`;
  migration save cũ; seed bag → set Tân Thủ Lv30.
- `SetBonusService` + hook vào `GetEffectiveHero`; bảng set/bonus
  theo spec (Lv30 Tân Thủ, Lv50 Chu Tước, Lv70 Bạch Hổ, Lv80 Thanh
  Long, Lv90 Huyền Vũ, Lv100 Kỳ Lân).
- UI: hàng trang bị + tooltip hiện tên set, phẩm (màu), bonus đang
  kích hoạt (2/4/6).
- Test: set bonus cộng đúng chỉ số; migration save cũ.

### Phase 2.31 — Cường hóa mới (tốn Bạc)
- Đổi chi phí Vàng → Bạc; công thức chi phí tăng theo cấp số nhân
  (cần Huynh chốt số — đề xuất: `100 × 2^cấp` Bạc).
- Giới hạn cấp cường hóa theo cấp tướng chính (cần chốt: = cấp tướng?).
- Tỉ lệ thành công/cooldown ở cấp cao (cần chốt số; hiện tại 100%).
- UI panel Cường hóa hiện tỉ lệ % thật thay vì 100% cố định.
- ⚠️ Phụ thuộc kinh tế Bạc (xem §4).

### Phase 2.32 — Tẩy luyện mới (dòng ngẫu nhiên)
- Mỗi món 3–6 dòng theo phẩm; pool dòng theo vị trí
  (Tiền: %HP/Miễn thương/Né/Đỡ/Kháng bạo; Trung: Tốc/Công/Bạo/Trúng/
  Sát thương bạo; Hậu: Tốc/HP/Khống chế/Công).
- Tốn Đá tẩy luyện (item mới) hoặc Vàng; khóa dòng đã ưng.
- UI: panel Tẩy luyện hiện các dòng + nút khóa + nút tẩy.

### Phase 2.33 — Khảm ngọc (mở tab "Đục lỗ")
- Lỗ khảm mỗi món (số lỗ theo phẩm?); mở lỗ tốn Bạc/Vàng theo cấp.
- Ngọc: Tốc độ/Công (Vật/Pháp)/HP/Vật phòng/Pháp phòng/Né/Đỡ/
  Bạo/Phá kích/Trúng; ghép 3 viên cấp N → 1 viên N+1.
- UI: tab Đục lỗ — lưới lỗ, chọn ngọc trong túi, nút ghép.

### Phase 2.34 — Rèn trang bị (chế tạo)
- Bản vẽ + Nguyên liệu rèn (item mới trong túi, tab Nguyên liệu).
- Công thức rèn theo mốc set; nâng phẩm giữ/kế thừa thuộc tính,
  ngọc khảm, cấp cường hóa (hoặc theo quy tắc kế thừa §2.35).

### Phase 2.35 — Kế thừa (mở tab "Kế thừa")
- Chuyển cấp cường hóa / dòng tẩy luyện / ngọc từ món cũ sang món
  mới cùng slot, tốn ít Bạc/Vàng.
- UI: chọn món nguồn → món đích → xác nhận.

## 4. Cờ đỏ: kinh tế Bạc

Spec dùng Bạc cho: cường hóa, mở lỗ khảm, rèn, kế thừa. Hiện tại:
- Bạc chỉ có **1.000.000 seed một lần**, **chưa có nguồn thu nào**
  (Cây Rung Tiền/Thu Thuế/Boss/Arena thuộc Phase 3, chưa làm).
- Đối tửu đã tốn 150.000–300.000 Bạc/lần.

Nếu đổi cường hóa sang Bạc ngay mà chưa có nguồn thu, người chơi sẽ
cạn Bạc rất nhanh. Hai hướng:
- (a) Làm nguồn Bạc tối thiểu trước (Cây Rung Tiền gọn nhẹ) rồi mới
  đổi cường hóa sang Bạc.
- (b) Giữ cường hóa tốn Vàng tạm, đổi sang Bạc cùng Phase 3.

## 5. Chốt của Huynh (2026-10-29 khuya)

1. Phạm vi: **làm hết 2.30→2.35 theo đúng thứ tự**.
2. Cường hóa tốn Bạc: **(a) đổi ngay + làm Cây Rung Tiền gọn trước**
   (nguồn Bạc tối thiểu), không chờ Phase 3.
3. Nguồn set đồ trước mắt: **rèn (bản vẽ + nguyên liệu) là nguồn chính**.
4. Tẩy luyện: **(a) làm lại full theo spec** (3–6 dòng ngẫu nhiên theo
   phẩm + đá tẩy luyện + khóa dòng).

Thứ tự triển khai điều chỉnh (chèn Cây Rung Tiền trước cường hóa Bạc):
2.30 mô hình dữ liệu + set → 2.31a Cây Rung Tiền → 2.31b cường hóa mới
→ 2.32 tẩy luyện mới → 2.33 khảm ngọc → 2.34 rèn → 2.35 kế thừa.

Câu hỏi vòng 2 (cho 2.31+, đã hỏi ở chat): công thức Cây Rung Tiền,
công thức/tỉ lệ cường hóa, nguồn bản vẽ rèn, bonus set đặc biệt.


## 6. Tiến độ thực hiện

- **Phase 2.30 — ĐÃ HOÀN THÀNH** (2026-10-29 khuya): mô hình dữ liệu
  + set đồ. `EquipmentSets` (6 bộ đúng spec), `SetBonusService`
  (đếm + cộng bonus, Miễn thương tới engine, 3 bonus đặc biệt giữ
  data "(sắp mở)"), migration save cũ → Tân Thủ, seed Tân Thủ,
  UI hiện set/phẩm/bonus, TestPhase230 ALL PASS, suite 0–2.30 xanh.
- **Phase 2.31 — ĐÃ HOÀN THÀNH** (2026-10-29 khuya): 2.31a Cây Rung
  Tiền (10 lượt/ngày, +5.000 + 1.000×cấp Bạc/lượt, nút Hub + popup)
  + 2.31b cường hóa Bạc (100×2^cấp, max = cấp tướng chính, tỉ lệ
  100/90/80/70%, thất bại mất Bạc). TestPhase231 ALL PASS, suite
  0–2.31 xanh.
- **Phase 2.32 — ĐÃ HOÀN THÀNH** (2026-10-30 rạng sáng): tẩy luyện
  mới — pool theo vị trí (Tiền/Trung/Hậu), 3–6 dòng theo phẩm,
  khóa dòng, chi phí 1 đá hoặc 200 Vàng, đá từ Cây Rung Tiền
  (+3 khi đủ 10 lượt). TestPhase232 ALL PASS, suite 0–2.32 xanh.
- **Phase 2.33 — ĐÃ HOÀN THÀNH** (2026-10-30 rạng sáng): khảm ngọc —
  GemService (6 loại, Lv10 max), lỗ theo phẩm, mở lỗ bằng Bạc,
  ghép 3→1, mua ngọc 2.000 Vàng, ngọc cộng chỉ số thật; UI tab
  Đục lỗ đầy đủ. TestPhase233 ALL PASS, suite 0–2.33 xanh.
- **Phase 2.34 — ĐÃ HOÀN THÀNH** (2026-10-30 rạng sáng): rèn —
  ForgeService (bản vẽ 5 bộ mua bằng Vàng, Khoáng thạch 100
  Vàng/viên, rèn tốn Tier/2 quặng, gate cấp tướng chính, đồ vào
  túi); UI Lò Rèn từ popup Trang bị. TestPhase234 ALL PASS,
  suite 0–2.34 xanh.
- **Phase 2.35 — ĐÃ HOÀN THÀNH** (2026-10-30 rạng sáng): kế thừa —
  TryInherit (chuyển cường hóa/tẩy/ngọc, hủy món cũ, phí 5.000
  Bạc); UI tab Kế thừa. TestPhase235 ALL PASS, suite 0–2.35 xanh.

**Kế hoạch trang bị 2.30 → 2.35 HOÀN TẤT.**

## 7. Phase 2.36 — Nâng phẩm / Thăng giai (ĐÃ HOÀN THÀNH 2026-10-30)

Mảnh còn thiếu so với spec Huynh (2026-10-29): nâng TRỰC TIẾP món
đang mang từ bộ cấp cũ lên bộ cấp mới (VD: Giày Chu Tước 50 →
Giày Bạch Hổ 70), thay vì rèn món mới + kế thừa.

- Chuỗi nâng: Tân Thủ 30 → Chu Tước 50 → Bạch Hổ 70 →
  Thanh Long 80 → Huyền Vũ 90 → Kỳ Lân 100 (Kỳ Lân là max).
- Điều kiện (đề xuất): đã học bản vẽ bộ ĐÍCH + đủ Khoáng thạch
  (Tier_đích/2) + phí Bạc (1.000 × Tier_đích) + cấp tướng chính
  đạt Tier_đích (đồng bộ gate với rèn).
- Kết quả: SetId/Tier/Quality đổi sang bộ mới; GIỮ nguyên cấp
  cường hóa, lỗ đã mở, ngọc đang khảm; dòng tẩy giữ nguyên +
  tự bù thêm cho đủ số dòng của phẩm mới (roll theo pool vị trí);
  lỗ khảm tự thêm cho đủ số lỗ phẩm mới (lỗ mới ở trạng thái chưa mở).
- UI (đề xuất): tab "Nâng cấp" thứ 6 trong popup Trang bị
  (theo từng món đang mang), hiện yêu cầu + nút Nâng cấp.
- Test: TestPhase236 (chuỗi nâng, chi phí, bù dòng/lỗ, các gate,
  UI flow); cập nhật test 229 (5 → 6 tab).

- Huynh chốt: (1) tab "Nâng cấp" thứ 6 trong popup Trang bị,
  (2) đang phát triển nên chỉ tốn Bạc (1.000 × Tier đích),
  (3) bù dòng/lỗ mới như đề xuất (lỗ mới chưa mở; mở lỗ bằng đạo
  cụ/Vàng là thiết kế tương lai — hiện vẫn mở bằng Bạc),
  (4) chuỗi 30→50→70→80→90→100, Kỳ Lân max.
- TestPhase236 ALL PASS, suite 0–2.36 xanh, CI thêm 2.36.
- **Fix theo feedback Huynh (2026-10-29 khuya)**: tooltip ngọc khảm
  khi hover hàng trang bị; lỗ khảm mặc định khóa hết, mở bằng Vàng
  (500/1.000/2.000/4.000 — số Pax setup); sửa nút Lò rèn đè nút X;
  style nút chọn trong Lò Rèn.
- **Fix theo feedback Huynh (2026-10-29 khuya, tiếp)**: tooltip trang
  bị đồng nhất 3 popup (Võ tướng/Túi/Trang bị) qua EquipmentTooltip
  (BBCode, RichTextLabel); dòng tẩy luyện tô màu theo phẩm roll
  (Cam/Tím/Lam/Trắng theo tỉ lệ giá trị trong pool).

---

## §8. Phase 2.37 — Tab Cường hóa: Chế Tạo / Đúc Lại + Tẩy luyện update
(Plan theo sketch + yêu cầu của Huynh, 2026-10-29 khuya)

### 8.0. Chốt của Huynh (2026-10-30)
- Q1a: XÓA tab "Nâng cấp", dời logic vào tab Cường hóa (nút Đúc Lại).
- Q2: Chế Tạo cho ô trống, chỉ chế được ở phẩm thấp nhất (không chọn
  tay). Tên trang bị = "{Tên set} {Tên ô}" (VD: Chu Tước Vũ khí).
- Q3a: Chi phí Chế Tạo = quặng + Bạc như Đúc Lại.
- Q4a: "Tinh Thiết" = Khoáng thạch hiện tại (không thêm nguyên liệu).
  **Thay bởi chốt 2026-10-30 (Phase 2.44):** thêm "mảnh chế tạo" theo set —
  Đúc Lại lên set X tốn thêm mảnh X (10/15/20/25/30), rơi từ Phó Bản /
  Tổ Đội hoặc đổi bằng Chiến tích; xem `KE_HOACH_LONGTUONG.md` §Phase 2.40–2.52.
- Q5a: Chi phí tẩy = 1 + số dòng khóa (đá); fallback Vàng
  200 x (1 + số dòng khóa).
- Q6b: Cooldown chỉ hiển thị "Sẵn sàng" (không làm logic chờ).
- Remap phẩm set: Chu Tước=Lục, Bạch Hổ=Lam, Thanh Long=Tím,
  Huyền Vũ=Cam, Kỳ Lân=Đỏ (thêm màu Lục vào palette).
- Số dòng/lỗ giữ nguyên theo từng set (Lục=4/2, Lam=5/3, Tím=5/3,
  Cam=5/3, Đỏ=6/4 — không nerf đồ hiện có).
- PENDING Q7: Tân Thủ xử lý thế nào (xem cuối §8).

### 8.1. Tab Cường hóa — layout mới (theo sketch)
Panel phải (theo tướng + ô đang chọn) gồm:
1. Header: [icon] `{Tên set} {Tên ô} +{cấp}` (Lv.Tier); ô trống thì
   hiện "Ô trống".
2. `THUỘC TÍNH HIỆN TẠI → DỰ KIẾN SAU NÂNG`: các dòng chỉ số
   `Tên: trước → sau (+delta)` — tính bằng cách mô phỏng action trên
   bản copy rồi so với `GetEffectiveHero` hiện tại:
   - Cường hóa: copy eq với Level+1.
   - Đúc Lại: copy eq với SetId/Tier/Quality đích.
   - Chế Tạo: item sẽ được chế.
   Chỉ hiện các chỉ số thay đổi (HP/Công/Thủ/Tốc...).
3. `TÀI NGUYÊN TIÊU TỐN`: `Bạc: có / cần`, `Khoáng thạch: có / cần`
   (đỏ khi thiếu).
4. Thanh cooldown cường hóa (theo Q6).
5. Nút hành động chính theo ngữ cảnh:
   - Ô trống → **"Chế Tạo"** (vàng, primary).
   - Có đồ, chưa max tier → **"Cường Hóa"** + **"Đúc Lại"**.
   - Có đồ, max tier (Kỳ Lân) → chỉ **"Cường Hóa"**.
   - Giữ nút "Tháo".

### 8.2. Chế Tạo (mới, trong tab)
- Điều kiện: ô đang chọn trống.
- Bộ chế: bộ cao nhất mà người chơi đủ điều kiện (đã học bản vẽ +
  cấp tướng chính ≥ Tier); hiển thị "Chế tạo: {Tên set} {Tên ô}".
- Chi phí (theo Q2/Q3): Khoáng thạch × (Tier/2) + Bạc 1.000×Tier.
- Kết quả: item vào THẲNG ô đang chọn (không qua Túi): Level 0,
  dòng tẩy fresh theo pool vị trí tướng, lỗ khóa hết.
- Lò rèn popup giữ nguyên (mua bản vẽ/quặng, rèn vào Túi).

### 8.3. Đúc Lại (thăng tier, full flow)
- Thay chi phí 2.36 (chỉ Bạc) bằng full flow theo yêu cầu:
  **bản vẽ bộ đích đã học + Khoáng thạch × (Tier_đích/2) +
  Bạc 1.000 × Tier_đích**.
- Chuỗi, gate cấp, giữ cường hóa/dòng/lỗ/ngọc: như 2.36.
- Không roll thất bại (deterministic).
- Kết quả mang tên + ngoại hình set mới: tên set mới, icon viền
  màu phẩm mới.
- Đề xuất XÓA tab "Nâng cấp" (logic dời vào tab Cường hóa) — theo Q1.

### 8.4. Tab Tẩy luyện
- Dòng tẩy tô màu theo phẩm roll (dùng `RefineLineGrade` đã có):
  chữ của dòng = màu phẩm; viền vàng vẫn đánh dấu dòng đang khóa.
- Chi phí theo số dòng khóa (theo Q5): đề xuất
  `đá = 1 + số dòng khóa`, fallback Vàng `200 × (1 + số dòng khóa)`.

### 8.5. Kỹ thuật
- `ForgeService.TryRecast(save, heroId, slotId, rng)` →
  `RecastResult { Success, NoNext, LowLevel, NoSilver, NoOre,
  NoBlueprint, EmptySlot }`.
- `ForgeService.TryCraftIntoSlot(save, heroId, slotId, setId, rng)` →
  `CraftResult { Success, NoBlueprint, LowLevel, NoOre, NoSilver,
  SlotOccupied }`.
- `EquipmentService.ProjectEquipChange(...)`: temp-swap equipment
  trong save, `GetEffectiveHero` 2 lần, trả về delta các chỉ số.
- Test: TestPhase237 (chế tạo vào ô, đúc lại full chi phí, projection,
  tẩy luyện màu + chi phí khóa); cập nhật test 229 (tab), 234/236.
- Save: không thêm field mới.
- Tên item hiển thị "{SetName} {SlotName}" ở hàng trang bị, panel,
  tooltip, túi.
- Bản vẽ: yêu cầu đã học, KHÔNG tiêu hao (như Lò rèn 2.34).

### 8.6. Q7 — Tân Thủ: Huynh chốt (b)
Giữ Tân Thủ (Lam) làm đồ khởi đầu ngoài ladder; Chế Tạo chỉ chế Tân
Thủ (15 quặng + 30.000 Bạc, không cần bản vẽ/cấp). Chuỗi Đúc Lại giữ
nguyên từ Tân Thủ -> Chu Tước.

### 8.7. ĐÃ XONG Phase 2.37 (2026-10-30)
- EquipmentSets: remap phẩm (Chu Tước Lục, Bạch Hổ Lam, Thanh Long
  Tím, Huyền Vũ Cam, Kỳ Lân Đỏ; Tân Thủ giữ Lam); thêm màu Lục;
  Lines/Sockets theo set (không nerf đồ cũ); `DisplayName` =
  "{Set} {Slot}".
- Migration: Quality/Tier suy từ SetId (tự remap đồ cũ).
- ForgeService: `TryCraftIntoSlot` (Chế Tạo Tân Thủ vào ô trống),
  `TryRecast` (full cost: bản vẽ đã học + quặng + Bạc), bỏ
  `TryUpgradeTier`.
- EquipmentService: `ProjectEquipChange` (mô phỏng delta chỉ số).
- Tẩy luyện: chi phí = 1 + số dòng khóa (đá), Vàng = 200x.
- HeroScreen: xóa tab Nâng cấp (5 tab); tab Cường hóa theo sketch
  (header, DỰ KIẾN SAU NÂNG, TÀI NGUYÊN, cooldown "Sẵn sàng",
  nút Chế Tạo/Cường Hóa/Đúc Lại/Tháo); dòng tẩy tô màu phẩm;
  tên hàng "{Set} {Slot} +{Level}".
- EquipmentTooltip: tên mới "{Set} {Slot}".
- Test: TestPhase237 mới; cập nhật 2/229/230/232/233/234/235/236;
  suite 0-2.37 xanh; CI thêm 2.37.
Set khởi đầu Lv30 (phẩm Lam) xử lý thế nào khi remap?
(a) Bỏ Tân Thủ: ladder còn 5 set (Chu Tước Lục -> Kỳ Lân Đỏ);
    Chế Tạo chế Chu Tước; đồ Tân Thủ cũ (seed/save) migrate sang
    Chu Tước; chuỗi nâng bắt đầu từ Chu Tước.
(b) Giữ Tân Thủ (Lam) làm đồ khởi đầu ngoài ladder;
    Chế Tạo chỉ chế Tân Thủ; 5 set trên remap phẩm.
- **Fix 2.37b theo ảnh chụp của Huynh (2026-10-30)**: xóa nút "Lò rèn"
  (mua bản vẽ/quặng làm inline trong mục Đúc Lại: nút "Mua ..." cạnh
  dòng yêu cầu); tab Cường hóa bọc ScrollContainer (hết tràn panel,
  nút bấm luôn với tới); thêm header TÀI NGUYÊN TIÊU TỐN còn thiếu ở
  mục cường hóa/đúc lại; sửa bug format delta (+N35 -> +35).
  Test 234/237 cập nhật; suite 0-2.37 xanh.
- **Fix 2.37c theo yêu cầu Huynh (2026-10-30)**: tab Cường hóa gọn
  vừa popup, không scroll (bỏ ScrollContainer); nút nhỏ dạng icon
  (36px); text 11-15px; projection gộp thành 1 dòng tóm tắt
  ("HP +35 · Vật phòng +2"). Test đo chiều cao thực tế <= 560px.
