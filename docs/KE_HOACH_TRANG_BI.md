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
