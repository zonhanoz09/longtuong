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
