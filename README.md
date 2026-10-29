# longtuong

Game Tam Quốc màn hình ngang (side-view) tương tự về thể loại/cơ chế với Long Tướng
(龙将) — sưu tầm tướng, leo ải, đánh tự động theo lượt có can thiệp thời điểm.
Không dùng code, art, nhạc hay text của game gốc; mọi nội dung tự thiết kế lại.

## Stack (đã chốt 2026-10-26)

- Godot **4.7.1 .NET (C#)** — bản `.NET` bắt buộc, bản thường không nạp được C#.
- .NET SDK **8.0+** (VM hiện có 8.0.426).
- Chơi đơn trước, local-save; server/PvP realtime để phase sau.

## Plan (bản sửa được trong source)

Xem [docs/KE_HOACH_LONGTUONG.md](docs/KE_HOACH_LONGTUONG.md) — đây là bản plan
chính, sửa trực tiếp trong repo khi cần, không sửa bản copy ngoài repo.

## Phase 2.30 — đã làm (set trang bị theo mốc cấp)

- `EquipmentSets`: 6 bộ theo đúng spec Huynh (Tân Thủ Lv30 Lam,
  Chu Tước Lv50 Tím, Bạch Hổ Lv70 Cam, Thanh Long Lv80 Cam,
  Huyền Vũ Lv90 Cam, Kỳ Lân Lv100 Đỏ) + bonus 2/4/6 món.
  `SetBonusService` đếm món đang mang và cộng vào
  `GetEffectiveHero`; Miễn thương chạy tới engine
  (`DamageReduction`), còn Nộ khởi đầu/Kháng khống chế/Đỡ đòn giữ
  dạng data "(sắp mở)" chờ engine.
- Model mới: `Tier`/`Quality`/`SetId`/`Sockets`/`RefineLines` trên
  cả `EquipmentState` lẫn `BagEquipmentItem`; migration save cũ →
  set Tân Thủ, `RefineIndex` cũ thành dòng tẩy đầu tiên; mang/tháo
  copy đủ field; seed 6 món túi là Tân Thủ (Lam, 3 dòng).
- UI: hàng trang bị hiện tên set màu phẩm, badge = số dòng tẩy;
  panel phải liệt kê bonus set đang kích hoạt; tooltip Túi/Võ tướng
  hiện bộ + phẩm + các dòng tẩy + bonus set.
- Test: `tests/TestPhase230.tscn` ALL PASS (bảng set, seed,
  bonus 2/4/6, migration, mang/tháo giữ set, tẩy xoay dòng đầu);
  toàn suite 0–2.30 pass; build 0 error; CI thêm 2.30.

## Phase 2.29 — đã làm (màn hình Trang bị theo layout ảnh Huynh gửi)

- Nút Hub "Cường hóa" đổi tên thành **"Trang bị"**.
- Viết lại `HeroScreen` (popup 1140×680) theo đúng layout ảnh: trái
  = danh sách tướng dọc (chỉ hiện tướng đã sở hữu); giữa = 6
  hàng trang bị (icon ký tự slot + tên màu slot + "Cường hóa: +N/10"
  + "Tẩy luyện: {dòng}" + badge xanh số tẩy luyện; hàng đang chọn
  viền vàng); phải = hàng 5 tab (Cường hóa/Tẩy luyện/Kế thừa/Đục
  lỗ/Phụ ma) + panel thao tác. Click hàng giữa → panel phải hiện
  Cường hóa/Tẩy luyện/Tháo cho món đó (tốn Vàng, cường hóa 100%
  thành công, tối đa +10). 3 tab chưa có hệ thống hiện "Chưa mở.".
- Tướng Tinh + Chiêu mộ **dời ra khỏi** popup này (chốt 2b): Tướng
  Tinh có nút Hub riêng + popup `StarChartScene` (520×400); chiêu mộ
  giữ ở nút Hub "Chiêu mộ" (mở Tửu quán).
- Test: `tests/TestPhase229.tscn` ALL PASS (layout 3 cột, 5 tab,
  cường hóa/tẩy luyện/tháo qua panel phải, tab khóa "Chưa mở.",
  popup Tướng Tinh); cập nhật 213 (6 nút chức năng), 222, 223, 28;
  toàn suite 0–2.29 pass; build 0 error; CI thêm 2.29.

## Phase 2.28 — đã làm (Túi đồ theo layout ảnh Huynh gửi)

- Viết lại `BagScene` (popup 1020×640): title "Túi" căn giữa; cột
  tab trái 8 tab (Đạo cụ/Trang bị/Nguyên liệu/Thiên Phú/Bảo Vật/
  Trang sức/Cánh Giới Đơn/Kho tạm) + nhãn "Mở rộng túi" (trang
  trí, túi không giới hạn); lưới phải 5 cột × 4 hàng = 20 ô,
  scroll khi quá 20; ô trang bị có ký tự slot + badge "+N" cường
  hóa; dòng "Chứa: X"; tooltip kiểu ảnh (tên màu slot +
  "Cường hóa: +N" + chỉ số); tab trống hiện lưới trống + "Chưa có
  vật phẩm.". Chỉ tab Trang bị có đồ thật; xem-only (mang/tháo
  vẫn ở màn Võ tướng). Style giữ theme tối + gold của game.
  Bản gọn (Huynh: popup 1020×640 quá rộng): frame 760×600 fit
  content — cột tab 170px + lưới 536px (5×96+4×14) + lề 16px.
- Test: `tests/TestPhase228.tscn` ALL PASS; toàn suite 0–2.28
  pass; build 0 error; CI thêm 2.28.

## Phase 2.27 — đã làm (chuẩn hoá asset bằng code, không file ảnh)

Huynh chốt plan chuẩn hoá asset (`docs/KE_HOACH_CHUANHOA_ASSET.md`):
1c giữ placeholder vẽ bằng code, 2 giữ nguyên style tối + gold,
3 làm cả icon lẫn chân dung, 4 chân dung VUÔNG, 5 commit thẳng repo.

- `UiTheme` thành nguồn sự thật duy nhất: palette tập trung
  (màu 6 slot trang bị, 4 phẩm, HP/nộ, ô trống...), font scale
  (28/16/13/17/15), kích thước chuẩn (icon 64/56, ô 96, chấm 20,
  nút 56...). Không còn màu palette hardcode ngoài `UiTheme`.
- `AssetIcons` mới: ký tự đại diện slot (VK/GI/MŨ/GY/CH/UY), ký
  tự chân dung (Bị, Vũ, Phi, Vân, Thuyền, Lượng, Bố; tướng tự
  tạo = chữ đầu username), viền màu phẩm, coin Vàng/Bạc (V/B),
  chân dung vuông bo góc 8.
- Áp dụng: icon trang bị có ký tự; chân dung Võ tướng (nền panel
  + chữ tên gọi + viền phẩm, giữ layout 2.16); ô Bày trận viền
  màu phẩm; tên tướng trong trận màu phẩm; avatar Hub có ký tự
  chân dung; Tửu quán có coin icon ở dòng tiền tệ.
- Test: `tests/TestPhase227.tscn` ALL PASS; toàn suite 0–2.27
  pass; build 0 error; CI thêm 2.27.

## Phase 2.26 — đã làm (bỏ Kim Nguyên Bảo, chỉ còn Vàng + Bạc)

- Xóa hẳn KNB: field `Gems`, seed 10.000 KNB, thưởng trận +5/+25
  KNB, mọi hiển thị Vàng/KNB (Hub, Túi đồ, Cường hóa, Tửu quán,
  thanh tiền tệ) chuyển sang Vàng/Bạc. Thưởng thắng ải giờ chỉ còn
  Vàng/EXP. Save cũ có field Gems vẫn load được.
- Test: `tests/TestPhase226.tscn` (ALL PASS — Hub/Tửu quán/Túi đồ
  đều hiện Bạc, không còn KNB).

## Phase 2.25 — đã làm (tiền tệ Vàng/Bạc + tất thắng x10 + layout đúng ảnh)

- Tiền tệ tách đôi: đối tửu Thường/Cao tốn **Bạc** (150.000 /
  300.000, đúng giá gốc); nút mới **Tất thắng (x10)** tốn phẳng
  **200 Vàng**, trừ 1 lần upfront, auto 10 lần rót rượu = 30 tướng
  thắng cả 30 theo tỉ lệ mức Cao + khóa Cam cấp 70, xong hiện tổng
  kết "+X hồn Lam, +Y hồn Tím, +Z hồn Cam" ở dòng trạng thái dưới
  lưới. Tài khoản mới nhận 1.000.000 Bạc khởi đầu (số Pax tự setup);
  save cũ được cấp 1 lần qua cờ SilverSeeded.
- Layout đúng ảnh phác mới: title căn giữa, lưới 6 cột đều pitch
  152 (hàng Lam 2 / Tím 2 / Cam 3 theo roster hiện tại), ô tướng
  chỉ có tên + chấm màu phẩm + số giá (không chữ phẩm), dòng số
  dư hồn cũng dạng chấm màu + số, 3 nút thẳng hàng dưới phải đều
  ghi giá, dòng tiền tệ Vàng | Bạc trên đầu.
- Test: `tests/TestPhase225.tscn` (ALL PASS, gồm replica Random
  chính xác cho tất thắng và luồng bấm nút thật qua signal).

## Phase 2.24 — đã làm (layout Tửu quán 2 màn + hồn chung theo phẩm)

- Tửu quán có 2 màn theo ảnh phác: màn Chiêu mộ (lưới tướng xếp
  theo hàng phẩm Lam/Tím/Cam, bấm ô là chiêu mộ luôn nếu đủ hồn
  phẩm đó, không đủ thì báo thiếu; tướng đã sở hữu ghi "đã chiêu
  mộ"; dòng "Hồn Lam: X | Hồn Tím: Y | Hồn Cam: Z" trên đầu; 2 nút
  đối tửu góc dưới phải) và màn Đối tửu (3 tướng rút được, mỗi
  tướng ghi số hồn thắng được từ tướng đó, nút Kéo/Búa/Bao, nút
  "Quay lại" về Chiêu mộ; quay lại giữa chừng thì hồn đã thắng
  giữ nguyên, lượt chưa đánh coi như bỏ).
- Hồn dùng chung theo pool phẩm (không còn hồn riêng từng tướng;
  hồn cũ 2.23 bỏ luôn không quy đổi). Thắng oẳn tù tì nhận ngẫu
  nhiên 1–3 hồn (trước 1–5); hòa oẳn tù tì lại với tướng đó.
- Test: `tests/TestPhase224.tscn` (ALL PASS, gồm luồng bấm nút thật
  qua signal: đối tửu → oẳn tù tì → quay lại, và pool chung Lam
  dùng được cho cả 2 tướng Lam).

## Phase 2.23 — đã làm (Tửu quán: đối tửu + chiêu mộ, bỏ sao tướng, cấp tối đa 180)

- Tửu quán thay nút Chiêu mộ: chọn mức Thường (100 Vàng) hoặc Cao cấp
  (300 Vàng), mỗi lần đối tửu rút ngẫu nhiên 3 tướng theo tỉ lệ phẩm
  của mức (Thường Lam 70/Tím 25/Cam 5, Cao cấp Lam 60/Tím 30/Cam 10;
  dưới cấp tướng tự tạo 70 thì tỉ lệ Cam = 0). Không còn nút chọn phẩm.
- Oẳn tù tì lần lượt với cả 3 tướng: thắng nhận 1–5 Võ Hồn của tướng
  đó, hòa cộng 1 lượt miễn phí với tướng đó, thua thì thôi. Đủ hồn
  (Lam 100, Tím 500, Cam 1500; Đỏ chỉ từ event) thì chiêu mộ tướng
  về đội. Võ Hồn chỉ để chiêu mộ.
- Cấp người chơi = cấp tướng tự tạo; cấp tối đa nâng từ 60 lên 180
  để mốc Cam 70 tới được. Phẩm càng cao chỉ số càng cao và trưởng
  thành càng nhanh (Lam ×1.0, Tím ×1.15, Cam ×1.35, Đỏ ×1.6 — tạm).
- Sao tướng loại bỏ khỏi game: xóa field Stars, hệ số sao và hiển
  thị "Sao X/5". Sao ải và Tướng Tinh giữ nguyên.
- Test: `tests/TestPhase223.tscn` (43 check, gồm tỉ lệ rút khớp replica
  Random và tỉ lệ Cam Cao cấp ~10% ở cấp 70).

## Phase 2.22 — đã làm (loại bỏ hoàn toàn mảnh tướng)

- Xóa sạch mảnh tướng: không còn mảnh trong save, chiêu mộ, tăng
  sao, thưởng trận hay hiển thị. Nút Tăng sao bị xóa; thưởng
  thắng chỉ còn Vàng/KNB/EXP.
- Tài khoản mới chỉ có tướng tự tạo; Lưu Bị thành tướng thường
  (mở bằng chiêu mộ ở feature sau, sa thải được). Chiêu mộ tạm
  khóa kèm dòng "đang được làm lại", không trừ Vàng. Save cũ có
  mảnh vẫn load được, mảnh cũ bỏ luôn không quy đổi.
- Test: `tests/TestPhase222.tscn` (ALL PASS).

## Phase 2.21 — đã làm (màn trận "ĐẤU TRƯỜNG" tự động + popup kết quả)

- Trận đánh tự động hoàn toàn từng lượt (~1 giây/lượt ở x1), tuyệt
  kỹ tự tung khi đầy nộ, chỉ còn nút Tăng tốc (x1 → x2 → x3 → x1).
  Sân đấu: lưới đội hình 2 bên gương nhau + vòng "Thú cưỡi" chờ,
  ô tướng hiện tên + HP + nộ, không còn nhật ký.
- Kết thúc: popup Thắng/Thua trận; thắng có sao, dòng thưởng và 5
  ô thưởng trống (chưa rơi item), thua chỉ có nút Thoát. Thưởng
  cộng ngay khi thắng, Thoát về Hub.
- Test: `tests/TestPhase221.tscn` (31 check).

## Phase 2.20 — đã làm (Bày trận kéo thả 7 ô + hiệu ứng vị trí)

- Popup Bày trận: list tướng sở hữu (tướng đã bày không hiện) kéo thả
  vào lưới 7 ô (Hậu 3, Trung 3, Tiền 1), hoán đổi/gỡ bằng kéo hoặc bấm
  ô, tối đa 5 tướng, tự lưu khi kéo thả, không nút Lưu riêng.
- Main (tướng tự tạo) luôn xuất trận, chỉ ở cột Trung (kéo được giữa
  3 ô Trung), default ô giữa. Đội vào trận đúng bằng đội hình đã bày;
  màn Vào ải không chọn đội riêng nữa.
- Hiệu ứng vị trí: Tiền DEF +20% và chịu đòn đơn mục tiêu trước,
  Trung ATK +20%, Hậu 20% cơ hội đánh trước mỗi vòng.
- Test: `tests/TestPhase220.tscn` (40 check).

## Phase 2.19 — đã làm (popup Võ tướng chỉ hiện tướng sở hữu)

- Popup Võ tướng chỉ tạo tab cho tướng đã chiêu mộ/sở hữu, không còn
  tab tướng khóa hay hậu tố "(Khóa)". Tướng bị sa thải biến mất khỏi
  list ngay và khi mở lại, muốn thấy lại phải mở khóa bằng 10 mảnh.
- Test: `tests/TestPhase219.tscn` (11 check).

## Phase 2.18 — đã làm (canh lại cột giữa popup Võ tướng + nút Sa thải)

- Cột giữa canh theo ảnh phác: hàng chân dung flush 2 mép (ô 56×56,
  gap 6, cao 180), tiêu đề "Kinh nghiệm" căn giữa trên thanh EXP,
  chỉ số thành lưới 4 cột (nhãn 130 / giá trị 80) cho thẳng hàng,
  hàng Kỹ năng + tên tuyệt kỹ, nút Sa thải sát mép phải dưới cùng.
- Popup 1000×600 (cao thêm 40px so với 2.17 để vừa nội dung mới).
- Nút Sa thải: xóa tướng khỏi danh sách sở hữu, đồ trả về túi, mảnh
  reset về 0, cấp/sao giữ lại; khóa với tướng chưa sở hữu, tướng tự
  tạo, Lưu Bị và tướng cuối cùng.
- Test: `tests/TestPhase218.tscn` (27 check).

## Phase 2.17 — đã làm (thu nhỏ popup Võ tướng, xóa phần dư thừa)

- Popup Võ tướng thu từ 1180×660 xuống **1000×560 cố định**, Hub thấy
  rõ xung quanh; 3 cột: tab trái 160×500, giữa 440×500, lưới phải
  336×500.
- Xóa: dòng \"Chân dung\", dòng EXP/Sao/Mảnh (chỉ giữ thanh EXP), nút
  \"Tháo tất cả trang bị\" trong popup này. Giữ: tab \"Trang sức\" khóa,
  lưới cố định 28 ô.
- Tài khoản cũ cũng nhận tướng tự tạo khi load (không tốn mảnh).
- Test: `tests/TestPhase217.tscn` (23 check).

## Phase 2.16 — đã làm (layout popup Võ tướng theo ảnh phác)

- Popup 1180×660, 3 cột: tab tướng trái, giữa là tab Trang bị (tab
  Trang sức khóa xám vì chưa có hệ trang sức riêng) + chân dung ôm
  bởi 6 ô đang mang chia 3 trái + 3 phải + EXP + chỉ số dạng lưới +
  kỹ năng, phải là lưới trang bị trong túi.
- Sửa luôn lỗi root scene bị scale 0.48/0.62 từ bản sửa trên editor
  khiến popup hiển thị nhỏ/lệch.
- Test: `tests/TestPhase216.tscn` (20 check).

## Phase 2.15 — đã làm (nút Võ tướng + ESC tắt popup)

- Nút dưới đầu tiên đổi thành "Võ tướng", bấm mở popup danh sách
  tướng (tiêu đề Võ Tướng). ESC tắt mọi popup đang mở trên Hub, không
  có popup thì ESC không làm gì; nút X vẫn hoạt động như cũ.
- Test: `tests/TestPhase215.tscn` (11 check).

## Phase 2.14 — đã làm (Hub theo ảnh phác thứ hai)

- Vùng mới: trên y 0–306, giữa y 306–612, dưới y 612–720. Trên phải chỉ
  còn Vào ải (918, 30) và Đăng xuất (1101, 30). Dưới là 5 nút từ x=360:
  Danh sách tướng, Túi đồ, Bày trận, Cường hóa, Chiêu mộ.
- Bỏ nút Trang bị riêng (trùng với Cường hóa, cùng mở HeroScreen);
  trang bị vẫn vào được bằng Cường hóa.
- Test: `tests/TestPhase214.tscn` (13 check); test 2.13 cập nhật theo
  layout mới, CI đã thêm 2.13 + 2.14.

## Phase 2.13 — đã làm (Hub chia 3 vùng theo ảnh phác)

- Đo từ ảnh phác: trên 44,5%, giữa 44,8%, dưới 10,7%, cột avatar 21,3%
  → trên 1280×720 là y 0–320, giữa y 320–643, dưới y 643–720.
- Trên: thẻ avatar trái (tên, tướng đại diện + Lv/sao, Vàng/KNB, sở hữu/
  thắng/ải/đội hình) + 5 icon tính năng phải (Danh sách tướng, Túi đồ,
  Trang bị, Cường hóa, Chiêu mộ). Giữa: sân, tướng mặc định đi lại được
  (click sân để di chuyển, để yên tự đi dạo, tốc độ 140px/s). Dưới: 3 icon
  chức năng (Bày trận, Vào ải, Đăng xuất). Đủ 8 widget cũ, popup giữ nguyên.
- Test: `tests/TestPhase213.tscn` (22 check); test 2.3, 2.11, 2.12 chạy lại vẫn xanh.

## Phase 2.12 — đã làm (pilot scene-first: Danh sách tướng lên Godot scene)

- Khung tĩnh của màn Danh sách tướng (label chi tiết, chân dung, thanh
  EXP, hàng ô đang mang, nút tháo tất cả) giờ nằm trong
  `HeroListScene.tscn`, chỉnh trực tiếp trên editor; code chỉ đổ dữ liệu.
- 2 scene mẫu mới: `HeroTabButton.tscn` (160×56) và `EquipmentCell.tscn`
  (64×64, dùng chung cho lưới túi + ô đang mang). Hành vi cũ giữ nguyên
  (click đúp tháo/mang, tooltip, cuộn lưới) — test 2.4–2.7, 2.11 vẫn xanh.
- Test: `tests/TestPhase212.tscn` (10 check).

## Phase 2.11 — đã làm (tướng mặc định luôn là tướng đã sở hữu)

- Sửa lỗi: sau khi đăng nhập, tướng mặc định từng kẹt ở "quan_vu" (tướng
  khóa với tài khoản mới) vì `LoadGame` không sửa nó. Nay tướng mặc định
  luôn là tướng đã sở hữu (ưu tiên đội trưởng); xem tab tướng khóa không
  đổi tướng mặc định toàn cục; đăng xuất thì xóa tướng mặc định.
- Test: `tests/TestPhase211.tscn` (7 check).

## Phase 2.10 — đã làm (tướng tự tạo làm default tài khoản mới)

- Thêm tướng tự tạo (tên = tên đăng nhập, Tiên phong, chỉ số cơ bản, chưa
  có skill — tuyệt kỹ placeholder ×1.0, sẽ cập nhật sau). Tài khoản mới
  luôn chỉ sở hữu tướng này; nó KHÔNG có trong chiêu mộ. Tài khoản cũ
  không tự nhận thêm. Lưu Bị vẫn là tướng thường, quay được bằng chiêu mộ.
- Test: `tests/TestPhase210.tscn` (15 check).

## Phase 2.9 — đã làm (thêm Lưu Bị, tướng default tài khoản mới)

- Đội hình 6 → 7 tướng: Lưu Bị (Tiên phong cân bằng, Song Cổ Kiếm ×1.8).
  Tài khoản mới luôn nhận Lưu Bị, bỏ luật chọn tướng theo hash tên đăng
  nhập; tài khoản cũ không đổi. Chiêu mộ chia lại cho 7 tướng, tổng 100%.
- Test: `tests/TestPhase29.tscn` (11 check).

## Phase 2.8 — đã làm (thêm Choàng + Uyển, thành 6 ô trang bị)

- Thêm Choàng (HP + DEF) và Uyển (ATK + Tốc); mọi hệ thống (trận, cường
  hóa, tẩy luyện, tháo/mang, lưới túi) chạy theo danh sách Slots nên tự
  nhận 2 ô mới. Save cũ tự thêm 2 ô Rỗng, tài khoản mới có túi 6 món.
- Màn Trang bị có 6 hàng trong khung cuộn (không tràn layout).
- Test: `tests/TestPhase28.tscn` (12 check).

## Phase 2.7 — đã làm (tướng tab dọc + lưới trang bị 4×7 icon-only)

- Danh sách tướng: tab dọc bên trái, mỗi tab 1 tướng (tiêu đề có "(Khóa)"
  cho tướng chưa sở hữu); bấm tab đổi chi tiết tướng ở giữa. Lưu ý: Godot
  4.7 TabContainer chỉ có tab Trên/Dưới nên tab dọc làm bằng cột nút tab.
- Lưới trang bị bên phải: 4 cột, ô vuông 64×64 chỉ hiện icon (ô màu theo
  loại, chưa có sprite), không tên/cấp/chỉ số; chỉ chứa món trong túi, món
  đang mang không có trong lưới mà chỉ ở 4 ô đang mang của tướng. Túi quá 28
  món thì lưới cuộn thêm hàng (đã test 30 món → 32 ô, cuộn được).
- Hover ô (lưới hoặc đang mang) hiện tooltip nổi ngay kế bên ô (ưu tiên
  phải, hết chỗ lật trái, kẹp trong popup); click đơn chỉ chọn, click đúp
  tháo/mang, mang được cho mọi tướng kể cả tướng khóa.
- Test: `tests/TestPhase27.tscn` (11 check).

## Phase 2.6 — đã làm (trang bị: danh sách sở hữu bên trái, hover hiện chỉ số, layout fix cứng)

- Màn Danh sách tướng: trái là danh sách trang bị user sở hữu (túi + đang
  mang trên mọi tướng, cuộn được, 260×540), giữa là tướng đang chọn
  (500×580), phải là chỉ số tướng + khung tooltip cố định 320×200.
- Món đồ không hiện chỉ số — chỉ tên ô + cấp; hover mới hiện chi tiết đầy
  đủ (chỉ số theo cấp, tẩy luyện, trạng thái) trong khung tooltip. Click đơn
  chỉ chọn, click đúp tháo/mang; mang được cho tất cả các tướng, kể cả tướng
  chưa sở hữu. Đã sửa gốc layout vỡ do label autowrap trong VBoxContainer.
- Test: `tests/TestPhase26.tscn` (18 check).

## Phase 2.5 — đã làm (tướng kiểu ảnh mẫu + click đúp tháo/mang trang bị)

- Danh sách tướng làm lại 3 cột kiểu ảnh mẫu (thay tab 2.4): trái danh sách
  tướng, giữa chi tiết (chân dung placeholder + 4 ô đang mang + Tháo tất cả
  + EXP + kỹ năng), phải chỉ số chi tiết + lưới túi trang bị. Popup 1180×660.
- Tướng mặc định không có trang bị: ô Rỗng không cộng chỉ số; tướng mới có
  4 ô Rỗng + túi 4 món cơ bản để test. Save cũ giữ nguyên món đang mang.
- Click đúp món đang mang → tháo về túi; click đúp món trong túi → mang
  cho tướng đang chọn (ô đã có món thì hoán đổi, không mất món); click đơn
  chỉ chọn, không tháo/mang. Màn Trang bị thêm nút Tháo từng món + Tháo tất cả.
- Test: `tests/TestPhase25.tscn` (ô Rỗng, tháo/mang/hoán đổi, seed, save cũ,
  click đúp/click đơn trong popup thật).

## Phase 2.4 — đã làm (bỏ nút Quay lại + tướng dạng tab)

- Bỏ nút Quay lại Hub ở tất cả widget, chỉ đóng bằng nút X của popup;
  nút thoát trận nay về thẳng Hub.
- Danh sách tướng dạng tab: 1 tab = 1 tướng, mỗi tab có chi tiết đầy đủ
  (cấp/sao/mảnh, chỉ số hiệu lực, kỹ năng) + 4 trang bị mang theo;
  tab tướng chưa sở hữu ghi Khóa + số mảnh.
- Test: `tests/TestPhase24.tscn` (không còn BackButton, đủ 6 tab, tab có trang bị).

## Phase 2.3 — đã làm (widget thành popup, kích thước riêng)

- Hub ở lại phía sau; 6 widget mở thành popup trên nền mờ, khung viền Vàng,
  nút X góc phải, nút Quay lại đóng popup thay vì chuyển màn hình.
- Kích thước riêng từng widget: Chiêu mộ 700×360, Bày trận 900×540,
  Túi đồ 1020×640, Danh sách tướng 1100×620, Trang bị/Cường hóa 1140×680,
  Vào ải 1180×680.
- Test: `tests/TestPhase23.tscn` (mở/đóng từng popup, đúng kích thước, Back đóng popup).

## Phase 2.2 — đã làm (UI/UX + Hub thẻ + seed data)

- Hệ thống UI chung (`core/ui/UiTheme.cs`): nền tối, điểm nhấn Vàng đồng, tiêu đề
  28px, nút tối thiểu 56px, mọi màn hình có header + thanh Vàng/KNB góc phải +
  nút về Hub; nút khóa xám kèm lý do.
- Hub redesign: thẻ widget 2 cột (tên + mô tả + trạng thái), header hiện tài khoản,
  tướng đại diện, số tướng sở hữu, thắng/ải.
- Seed tài khoản mới: tướng khởi đầu xác định từ tên đăng nhập
  (`(tổng mã ký tự + 31*độ dài) % 6`), Vàng 100.000 + KNB 10.000 để test, chỉ sở
  hữu 1 tướng khởi đầu; tài khoản cũ giữ nguyên 6 tướng.
- Mở khóa tướng: đủ 10 mảnh thì tự mở khóa (trừ 10 mảnh), qua chiêu mộ hoặc
  thưởng trận. Đội hình/chọn ải từ 2.2 cho phép 1–3 tướng để tài khoản 1 tướng
  vào ải được ngay.
- Test: `tests/TestPhase22.tscn`.

## Phase 2.1 — đã làm (login/register -> hub -> widget)

- Màn hình chính giờ là Đăng nhập/Đăng ký (`features/auth/`): tài khoản lưu local,
  mật khẩu chỉ lưu dạng hash SHA-256 + salt riêng từng tài khoản, không lưu plaintext.
- Hub (`features/hub/`): sau khi đăng nhập, 7 widget + đăng xuất:
  Danh sách tướng sở hữu, Túi đồ, Trang bị, Bày trận, Vào ải, Cường hóa, Chiêu mộ.
- Save tách theo tài khoản (file `save_<tên>.json` local); chưa đăng nhập vẫn dùng
  `save.json` cũ. Server thật phase sau mới tách tài khoản thật.
- Màn hình mới: Danh sách tướng (bấm xem chi tiết), Túi đồ (vàng, KNB, mảnh,
  trang bị, sao ải), Bày trận (chọn đúng 3, lưu, xem lực chiến), Chiêu mộ riêng
  (tỉ lệ công khai). Trang bị/Cường hóa dùng chung màn hình Tướng.
- Test: `tests/TestPhase21.tscn`.

## Phase 2 — đã làm (nuôi tướng & trang bị)

- Cấp/EXP: lên cấp cần 100 + 50*(cấp-1) EXP, tối đa cấp 60; tăng sao bằng mảnh
  (tốn 10*sao hiện tại, tối đa 5 sao); chỉ số = gốc * (1+0.04*(cấp-1)) * (1+0.25*(sao-1)).
- Trang bị 4 món (vũ khí/giáp/mũ/giày) + cường hóa tối đa 10 (tốn 100*(cấp+1) Vàng)
  + tẩy luyện 1 dòng (5 dòng luân phiên, tốn 100 Vàng).
- Tướng Tinh: 12 ô, mở khi tổng sao ải >= (ô+1)*2, mỗi ô +3% HP/ATK toàn đội.
- Chiêu mộ: 100 Vàng = 5 mảnh 1 tướng, tỉ lệ công khai (Quan Vũ/Trương Phi/Triệu Vân
  20%, Điêu Thuyền/Gia Cát Lượng 15%, Lữ Bố 10%).
- Thắng trận: mỗi tướng trong đội +EXP (40+10*ải), +2 mảnh, +5 Kim Nguyên Bảo
  (+20 nếu phá ải lần đầu); trận dùng tướng hiệu lực (đã tính cấp/sao/trang bị/Tướng Tinh).
- Test: `tests/TestPhase2.tscn`.

## Phase 1 — đã làm (vertical slice)

- 6 tướng (`core/data/HeroDatabase.cs`): Quan Vũ, Trương Phi, Triệu Vân,
  Gia Cát Lượng, Điêu Thuyền, Lữ Bố — mỗi tướng 1 đánh thường + 1 tuyệt kỹ + 1 bị động.
- Combat 3v3 (`core/battle/BattleEngine.cs`): đánh theo tốc độ, nộ đầy 100 thì
  bấm tuyệt kỹ (hoặc auto), bạo kích, né, khắc hệ Võ > Mưu > Phụ > Võ, RNG có seed
  (cùng seed cho cùng kết quả), sao 1–3 theo HP còn lại/số lượt.
- Chương 1: 10 ải, độ khó tăng theo hệ số 0.85 → 1.66, thưởng 150 → 600 Vàng.
- Luồng: Màn hình chính → Chọn đội (đúng 3/6 tướng) & ải (mở theo ải đã qua)
  → Trận → kết quả + sao + thưởng lưu local.
- Test: `tests/TestPhase0.tscn` + `tests/TestPhase1.tscn`.

## Phase 0 — đã làm

- Màn hình chính (`features/main/scenes/MainScene.tscn`): vào trận placeholder,
  lưu/tải game local.
- Trận placeholder (`features/battle/scenes/BattlePlaceholder.tscn`): 2 bên
  placeholder, đánh theo lượt cố định, thắng khi địch hết HP.
- Lưu game local JSON (`core/save/`), logic trận thuần C# (`core/battle/`),
  test headless (`tests/TestPhase0.tscn`).

## Build & test (local)

```bash
export DOTNET_ROOT=$HOME/.dotnet PATH=$HOME/.dotnet:$PATH
dotnet build longtuong.csproj --nologo
# Godot 4.7.1 bản .NET, ví dụ đường dẫn VM:
GODOT=$HOME/.godot-bin/godot471-mono
"$GODOT" --headless --path . --import --quit
dotnet build longtuong.csproj --nologo
timeout 120 "$GODOT" --headless --path . res://tests/TestPhase0.tscn
```

CI (`.github/workflows/ci.yml`) chạy đúng 2 bước trên: build dotnet + test headless.
Export sau này phải dùng export templates bản `.NET` (mono), không dùng bản thường.
