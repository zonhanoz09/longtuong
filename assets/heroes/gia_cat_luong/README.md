# Gia Cát Lượng — bộ hình và chuyển động hoàn chỉnh

Hoàn thiện 02/11/2026 từ ảnh tham chiếu áo vàng, vai đỏ, mũ quân sư và quạt
đỏ cam người dùng cung cấp. Body/portrait gốc được giữ nguyên.

- **5 clip × 8 frame:** idle, attack, hit, ultimate, death.
- **5 VFX × 8 frame:** quạt lửa bay, va chạm thường, bát quái tuyệt kỹ,
  tụ khí tại tay và hào quang dưới chân.
- Đã nối vào trận thật và hình nhân vật ở sân Hub; portrait dùng chung
  cho Võ tướng, avatar và Bày trận. VFX không thay công thức kỹ năng.

[Xem ảnh động](../../../docs/previews/gia_cat_luong_full.gif) ·
[Ảnh tổng quan](../../../docs/previews/gia_cat_luong_full.png) ·
[Hình ở sân Hub](../../../docs/previews/gia_cat_luong_hub.png).

## Xem trong Godot

```bash
dotnet build
godot --path . res://tests/PreviewGiaCatLuong.tscn
```

Space dừng/chạy xem thử. Nút **Trận mẫu** mở trận với tài khoản test riêng.
Trong game thường, chọn Gia Cát Lượng ở Võ tướng rồi đóng popup để sân Hub
hiện hình mới; thêm vào đội để xem trong trận.

## Cách lưu

```text
gia_cat_luong/
├── body.png
├── portrait.png
├── animation.json
├── animations/{idle,attack,hit,ultimate,death}/atlas.png
└── effects/{normal,ultimate,projectile,cast,aura}/atlas.png
```

Mỗi atlas có 8 hình khác nhau, xếp trên sheet 3×3, ô cuối không dùng. Game
đọc trực tiếp bằng AtlasTexture; không cắt hay chỉnh pixel PNG gốc. Metadata
căn vùng UV, phần đệm, kích thước và điểm chân vào canvas ảo 512×512.
Hai sheet đánh/tuyệt kỹ đã được chỉnh khoảng cách; vùng clip loại phần ảnh
lân cận và giữ đầu quạt trọn vẹn khi vươn tay.

Giữ `animation.json` khi copy cả bộ. Không đặt ảnh tham khảo hoặc draft vào
thư mục clip; ảnh JPEG gốc nằm ở thư mục tướng, không bị dùng như frame.

Nếu bạn tự vẽ lại, có thể bỏ `000.png`, `001.png`… vào thư mục clip tương
ứng để **ưu tiên ảnh rời thay atlas**, không cần sửa code hoặc JSON. Giữ
cùng canvas và điểm chân theo [chuẩn asset](../../README.md); vào trận/màn
mới để nhận. Muốn dùng lại atlas thì bỏ các frame đánh số vừa thêm.

## Nhịp và cấu hình

| Nhóm | FPS | Mốc phóng |
|---|---:|---|
| Idle | 8 | Lặp |
| Attack | 12 | Frame 004 |
| Hit | 12 | Nhận damage mới phát |
| Ultimate | 10 | Frame 004 |
| Death | 10 | Giữ frame 007 khi kết thúc |
| Projectile / Normal / Ultimate / Cast / Aura | 12 / 16 / 14 / 14 / 8 | Theo timeline game |

Hào quang nằm dưới chân và dừng khi ngã. Đổi x1/x2/x3 áp dụng cùng lúc lên
clip, di chuyển, đạn và VFX. Tướng trong Hub quay theo hướng đang di chuyển.

## Nguồn và kiểm tra

Tranh animation/VFX được tạo bằng **image_gen tích hợp**, dùng thiết kế
người dùng cung cấp; không phải frame cắt nguyên từ JPEG. Không nhân bản
body để giả clip. [Prompt đầy đủ](ANIMATION_PROMPTS.json),
[danh sách bản sinh được chọn](ANIMATION_SOURCES.json).

`tools/assets/calibrate_gia_cat_luong.py` chỉ đọc alpha của các sheet đang
phát hành để sinh metadata, không ghi/chỉnh lại PNG. Script này dành riêng
cho bộ sheet 1254px này; không cần chạy khi dùng PNG frame rời tự vẽ.

Kiểm tra bộ thật:

```bash
godot --headless --path . res://tests/TestGiaCatLuongAssets.tscn
```

Đã kiểm tra 40 frame nhân vật và 40 frame VFX, phát đạn/AoE/hit/death, giữ
điểm đứng, hình Hub và renderer OpenGL trên Godot 4.3 .NET. Đây là bộ hình
chạy bằng frame, không phải rig xương. Runtime reload và clip PCK vẫn theo
lộ trình chung; bộ này chạy từ thư mục asset vật lý của dự án/bản desktop.


Kiểm tra hồi quy: suite 61 scene vẫn chỉ có bốn lỗi layout cũ
(214/216/217/244) và log RID cũ ở 227. Sáu kiểm tra Hub/battle chạy lại sau
khi nối hình sân Hub không có lỗi mới; test clip và test bộ Gia Cát Lượng
đều qua. Bản xem thử/GIF được render trực tiếp từ các node game, không phải
ảnh dựng mô phỏng bên ngoài.
