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
