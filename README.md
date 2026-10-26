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
