# Kế hoạch chuẩn hoá asset game (icon, button, asset tướng...)

> Trạng thái: **HUỲNH ĐÃ CHỐT (29/10/2026)** — 1c (giữ placeholder vẽ bằng
> code, không art thật), 2 giữ nguyên style tối + gold, 3 làm cả icon lẫn
> chân dung, 4 chân dung VUÔNG (bo góc 8, không cắt tròn), 5 commit thẳng
> repo (không áp dụng vì phase này không có file ảnh).
> ĐÃ TRIỂN KHAI: Phase 2.27 (mục 6.1 dưới đây).

## 0. Hiện trạng (đo thật từ repo)

- **0 file ảnh** trong repo: không png/jpg/svg/font. Toàn bộ hình ảnh
  hiện tại đều vẽ bằng code (ColorRect, StyleBoxFlat, texture tròn
  vẽ bằng `Image`, text).
- **Đã có 1 chuẩn duy nhất, nhưng chưa đủ:** `core/ui/UiTheme.cs` —
  nền tối `(0.07, 0.07, 0.13)`, panel `(0.14, 0.14, 0.24)`, Gold
  accent `(0.91, 0.72, 0.29)`, Text/Muted, PlayerBlue/EnemyRed;
  title 28px gold, body 16px, button cao tối thiểu 56px font 17px,
  bo góc 8px, primary = nền gold chữ tối. Hầu hết scene đã dùng
  `StyleTitle/StyleBody/StyleButton`.
- **Chưa chuẩn, màu rải rác ngoài UiTheme:**
  - Màu 6 slot trang bị nằm riêng trong `HeroListScene` (weapon đỏ
    đất, armor xanh dương, helmet xanh lá, boots vàng đất, cloak
    tím, bracer cyan).
  - Màu 3 chấm phẩm nằm riêng trong `RecruitScene` (Lam xanh dương,
    Tím magenta, Cam cam).
  - Màu thanh HP (xanh lá) / nộ (xanh dương) nằm riêng trong
    `BattleScene`; màu ô formation, màu sao... cũng hardcode tại chỗ.
- **Kích thước chưa thống nhất:** icon trang bị 64×64 (lưới) vs
  56×56 (đang mang); ô formation 96×96 tròn (bo 48); chấm phẩm
  20×20; thanh HP 132×14, nộ 132×10; tab tướng 160×56; nút đóng
  popup 40×40; font size lẻ: 13/15/16/17/28 rải rác
  (`BattleScene`, `HeroListScene`, `FormationScene` tự override).
- **Tướng chưa có asset thật:** chỉ có tên text + `ColorRect`
  placeholder chân dung (`DetailPortrait`) + ô tròn formation +
  cell trận (tên + thanh HP/nộ). Không có chân dung, sprite,
  icon tướng.
- **Icon chưa có cái nào thật:** icon trang bị chỉ là ô màu theo
  slot; nút chức năng Hub chỉ có text ("Võ tướng", "Túi đồ"...);
  tiền tệ Vàng/Bạc chỉ là text; gần với "icon" nhất hiện nay là
  chấm màu phẩm ở Tửu quán.
- **Scene:** ~220 dòng offset/size hardcode trong `.tscn`; nhiều
  popup tự đặt vị trí tuyệt đối thay vì dùng container.
- **Font:** đang dùng font mặc định của Godot (hiển thị tốt tiếng
  Việt có dấu). Chưa có font riêng.
- **Không có:** thư mục `assets/`, quy ước đặt tên file, quy trình
  import, danh mục asset tập trung.

## 1. Nguyên tắc

1. **Không copy asset game gốc** — art/code/text tự làm (ranh giới
   đã chốt từ đầu; cơ chế thì copy 100%).
2. **Tách asset khỏi code logic:** game phải chạy được kể cả khi
   thiếu file ảnh (fallback về placeholder procedural hiện tại).
   Thay asset thật = thay file + đăng ký đường dẫn, không sửa code
   gameplay, không vỡ test headless.
3. **Một nguồn sự thật cho style:** mọi màu, font size, kích thước
   chuẩn chỉ sống ở `UiTheme` (sau này là `AssetCatalog`); cấm
   hardcode màu/size mới trong scene.
4. **Giữ phong cách hiện tại:** nền tối + gold accent đã nhất quán
   khắp game — chuẩn hoá theo nó, không đổi style tổng thể trừ khi
   Huynh chốt khác.
5. **Làm theo thứ tự thấy được trước:** icon nhỏ → chân dung →
   nền → polish; mỗi bước giao kèm test, không làm 1 lần tất cả.

## 2. Giai đoạn 1 — Tập trung hoá chuẩn hiện có (code, không cần art mới)

Mục tiêu: mọi màu/size/font size chỉ còn ở 1 chỗ, scene cũ vẫn
chạy y hệt.

- **Bảng màu tập trung** (`UiTheme` mở rộng, hoặc `AssetPalette`
  mới nếu Huynh muốn tách): chuyển vào đó màu 6 slot trang bị,
  màu 3 phẩm (Lam/Tím/Cam), màu HP/nộ, màu trạng thái (thắng/thua/
  khóa/mờ), màu ô formation. Xóa `new Color(...)` lẻ trong
  `HeroListScene`, `RecruitScene`, `BattleScene`, `FormationScene`.
  Chấm phẩm ở Tửu quán (đang vẽ bằng `ImageTexture`) chuyển sang
  dùng texture từ palette (vẫn vẽ bằng code, chỉ đổi nguồn màu).
- **Font scale duy nhất:** Title 28 / Heading 20 / Body 16 /
  Small 13 / Button 17 / Caption 12 (đề xuất). Xóa các override
  lẻ (15, 13, 17 rải rác) → gọi `StyleBody(size)` theo scale.
- **Kích thước chuẩn (đóng băng):** button cao 56 (đóng popup
  40 là ngoại lệ có lý do); icon lưới 64, icon đang mang 56, ô
  formation 96, chấm phẩm 20, thanh HP 132×14 / nộ 132×10, bo góc
  8 (ô tròn formation bo 48 là ngoại lệ có lý do).
- **Biến thể nút:** `StyleButton` hiện có primary/secondary +
  disabled. Thêm biến thể icon-only (vuông, không text) và danger
  (đỏ, cho nút Sa thải...) — dùng chung cho Hub, popup, battle.
- **Test:** TestPhase mới kiểm tra: không còn `new Color(` ngoài
  palette (kiểm bằng grep trong CI hoặc test đọc source — chọn
  grep trong workflow, đơn giản hơn); mọi scene vẫn load + suite cũ
  pass.

## 3. Giai đoạn 2 — Hạ tầng asset thật + thay icon trước

- **Cấu trúc thư mục (đề xuất):**
  - `assets/icons/equipment/` — 6 icon slot (weapon/armor/helmet/
    boots/cloak/bracer), PNG 64×64, nền trong suốt.
  - `assets/icons/features/` — icon 6 nút Hub (Võ tướng, Túi đồ,
    Bày trận, Cường hóa, Chiêu mộ, Vào ải...), PNG 64×64.
  - `assets/icons/currency/` — Vàng, Bạc, hồn Lam/Tím/Cam, PNG
    32×32 (dùng thay chấm màu ở Tửu quán khi có).
  - `assets/heroes/portraits/` — chân dung 8 tướng + 1 placeholder,
    PNG 256×256 (xem mục 5, câu hỏi 4).
  - `assets/ui/` — panel 9-patch, khung popup, khung thẻ tướng
    (làm sau, khi chốt style).
  - `assets/backgrounds/` — nền Hub (sân), nền trận (làm sau).
  - `assets/README.md` — ghi quy ước: định dạng PNG, nền trong
    suốt cho icon, tên file `snake_case` theo id
    (`weapon.png`, `luu_bi.png`...), kích thước theo bảng trên.
- **Lớp nạp asset (`AssetCatalog`, static, code mới):**
  - Ánh xạ theo id: `HeroPortrait(heroId)`, `EquipmentIcon(slotId)`,
    `FeatureIcon(featureId)`, `CurrencyIcon(currencyId)`.
  - Trả về texture file nếu tồn tại, ngược lại trả về placeholder
    procedural hiện tại (ô màu / chấm tròn / ColorRect) — game và
    test không bao giờ crash vì thiếu ảnh.
  - Nơi thay đầu tiên: icon 6 slot trang bị (ô màu → ảnh thật khi
    có file, không thì giữ ô màu), rồi icon Hub, rồi tiền tệ.
- **Test:** TestPhase kiểm tra catalog: mọi id đã đăng ký đều có
  file hoặc fallback hợp lệ; icon load được đúng kích thước chuẩn.

## 4. Giai đoạn 3 — Asset tướng + nền + polish scene

- **Chân dung tướng:** 8 tướng (tướng tự tạo dùng khung chung +
  chữ cái đầu tên tài khoản, hoặc 1 portrait mặc định) → thay
  `DetailPortrait` (ColorRect) ở Võ tướng, ô tròn formation (giữ
  khung tròn, ảnh cắt tròn), cell trận (thêm portrait nhỏ trên
  tên), avatar Hub.
- **Nền:** nền sân Hub (thay ColorRect nền hiện tại của
  `Courtyard`), nền trận (sau lưng 2 đội hình), nền popup (panel
  9-patch thay StyleBoxFlat phẳng).
- **Polish scene:** các popup mới dùng container (HBox/VBox/Grid)
  thay vị trí tuyệt đối; gom `.tscn` hardcode về đúng chuẩn mục 2.
  (Không đụng layout đã chốt 100% ở Tửu quán/Bày trận trừ khi
  Huynh yêu cầu.)
- **Font (tùy chọn, nếu Huynh muốn):** chọn 1 font Việt hoá có
  phong cách (vd. font serif cổ trang cho title + sans cho body),
  bỏ vào `assets/fonts/`, đăng ký qua Theme chung. Mặc định: giữ
  font Godot nếu không chốt font mới.

## 5. Cần Huynh chốt (trả lời theo số)

1. **Nguồn art:** (a) AI generate theo prompt chuẩn (nhanh, đồng
   nhất, cần duyệt từng ảnh), (b) vẽ tay/thuê vẽ (đẹp nhất, chậm,
   tốn kém), hay (c) giữ placeholder procedural, chỉ cải tiến bằng
   code (khung, màu phẩm, ký tự đại diện — không tốn art)? Đề xuất
   mặc định của Pax: làm hạ tầng Giai đoạn 1+2 trước với
   placeholder, art thật (a hoặc b) thay dần sau — không chờ art
   mới làm code.
2. **Phong cách:** giữ nền tối + gold hiện tại, hay muốn đổi
   (vd. sáng hơn, cổ trang giấy dó...)? Đề xuất: giữ nguyên.
3. **Ưu tiên thay trước:** icon trang bị, icon Hub/tiền tệ, hay
   chân dung tướng?
4. **Chân dung tướng:** vuông 256×256 hiển thị cắt tròn (hợp ô
   formation tròn) hay vuông bo góc 8 (hợp thẻ tướng)? Đề xuất:
   vuông 256×256, hiển thị bo góc 8 ở thẻ + cắt tròn ở formation.
5. **Lưu trữ:** ảnh commit thẳng vào repo git (tiện, repo sẽ phình
   lên) hay để ngoài (Drive) và chỉ commit ảnh nhỏ? Đề xuất: ảnh
   dưới ~200KB commit vào repo, ảnh nền lớn để ngoài rồi tính sau.

## 6. Thứ tự thực hiện (sau khi Huynh chốt mục 5)

### 6.1. Phase 2.27 — ĐÃ XONG (29/10/2026)

Triển khai theo chốt 1c: toàn bộ bằng code, không file ảnh.

- **Tập trung chuẩn về `UiTheme`** (nguồn sự thật duy nhất):
  palette mở rộng (`SlotColors` 6 slot, `TierColor()` 4 phẩm,
  `HpColor`, `BarBackground`, `SlotEmpty`, `SilverCoin`,
  `DarkText`, `GoldHover/GoldPressed`, `PanelHover/PanelPressed`,
  `DisabledBg`, `DangerHover`, `DeadGrey`); font scale
  (`FontTitle 28`, `FontBody 16`, `FontSmall 13`,
  `FontButton 17`, `FontSmallButton 15` — nút Sa thải giữ 15 theo
  sketch, giờ là hằng số có tên); kích thước chuẩn (`ButtonHeight
  56`, `PopupCloseSize 40`, `IconGridSize 64`,
  `IconEquippedSize 56`, `FormationSlotSize 96`,
  `TierDotSize 20`, `TabWidth 160`, `HpBarSize 132×14`,
  `RageBarSize 132×10`). Không còn `new Color(...)` palette nào
  ngoài `UiTheme` trong features/ (chỉ còn công thức lighten
  hover tính từ màu gốc).
- **`AssetIcons` mới** (`core/ui/AssetIcons.cs`): ký tự đại diện
  slot (`VK/GI/MŨ/GY/CH/UY`), ký tự chân dung (tên gọi: Bị, Vũ,
  Phi, Vân, Thuyền, Lượng, Bố; tướng tự tạo = chữ cái đầu tên
  đăng nhập viết hoa), viền chân dung theo màu phẩm (tướng tự
  tạo = gold), coin Vàng/Bạc (tròn + chữ V/B), chân dung vuông
  (`MakePortrait`, bo góc 8).
- **Áp dụng:**
  - Icon trang bị: ô màu + ký tự 2 chữ (lưới 64 + đang mang 56).
  - Chân dung Võ tướng: `DetailPortrait` (giữ nguyên node ở index
    1, 260×180 — test 2.16 vẫn xanh) thành nền panel + chữ tên
    gọi cỡ 60 + vòng viền màu phẩm trong suốt.
  - Ô Bày trận đã bày: viền vòng tròn theo màu phẩm (main giữ
    nền gold).
  - Tên tướng trong cell trận: màu theo phẩm.
  - Avatar Hub: `AvatarPortrait` thành nền panel + chữ ký tự
    (28px, hoặc 20px nếu tên gọi dài) + tên tướng màu phẩm; dòng
    tiền tệ Hub giữ nguyên text (layout VBox của AvatarBox mong
    manh, không đụng).
  - Tửu quán: dòng tiền tệ thành `CurrencyBox` (HBox) với 2 coin
    icon + nhãn Vàng/Bạc + nhãn cấp; chấm phẩm dùng
    `UiTheme.TierColor`.
- **Test:** TestPhase227 ALL PASS (palette, font, size, ký tự,
  coin, portrait, tích hợp HeroList/Formation/Hub/Tavern);
  cập nhật 225/226 theo `CurrencyBox`; toàn suite 0–2.27 pass;
  build 0 error; CI thêm 2.27.

### 6.2. Các bước sau (chưa làm, chờ Huynh khi cần)

- Nền Hub/trận, panel 9-patch, container hoá `.tscn` hardcode,
  font riêng — thuộc Giai đoạn 3–4 của plan gốc; với chốt 1c thì
  chỉ làm khi Huynh yêu cầu cụ thể.
