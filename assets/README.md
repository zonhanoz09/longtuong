# Thư mục tranh dùng trong game

**Đã dùng được: PNG tĩnh `body.png` và `portrait.png` cho tướng/địch.**
Copy đúng tên rồi mở game, vào trận mới hoặc mở lại màn liên quan để nhận
ảnh. Không phải sửa code, import thủ công hay tạo `.tres`/`.tscn`.

Chân dung đã nối vào Võ tướng, avatar Hub và ô Bày trận. Body đã nối vào
trận đấu, tự lật cho phe địch và giữ điểm chân. Ảnh thiếu dùng mẫu bằng code.

**Đã dùng được clip PNG:** `idle`, `attack`, `hit`, `ultimate`, `death` và
VFX `normal`, `ultimate`, `projectile`, `cast`. Clip/VFX thiếu dùng fallback.
Đã có aura dưới chân và bộ 17 icon PNG (trang bị, tiền tệ, chức năng).
Chưa có tự reload trên màn đang mở, victory và nền PNG.

Chi tiết timeline: [idle/attack/hit, tiến/lùi và VFX](../docs/KE_HOACH_ANIMATION_CHIEN_DAU.md).
**Gia Cát Lượng đã có bộ thật:** 5 clip × 8 frame và 5 VFX × 8 frame,
bao gồm hào quang. [Hướng dẫn bộ đầy đủ](heroes/gia_cat_luong/README.md).

## Bắt đầu với một tướng

Ví dụ Quan Vũ:

```text
assets/heroes/quan_vu/
├── portrait.png       # Chân dung vuông, dùng chung các màn
├── body.png           # Toàn thân trong trận, có cả vũ khí
├── animations/        # Tùy chọn: thêm khi muốn nhân vật chuyển động
│   ├── idle/
│   │   ├── 000.png
│   │   └── 001.png
│   ├── attack/
│   │   ├── 000.png
│   │   └── 001.png
│   └── ultimate/
│       ├── 000.png
│       └── 001.png
└── effects/           # Tùy chọn: hiệu ứng của riêng tướng này
    ├── normal/
    │   └── 000.png
    ├── ultimate/
    │   └── 000.png
    └── projectile/
        └── 000.png
```

Chỉ có `portrait.png` cũng được: các màn chân dung dùng ảnh mới, hình trong
trận vẫn là mẫu bằng code. Chỉ có `body.png` thì trận dùng hình toàn thân
mới và giữ chuyển động tiến/lùi, hiệu ứng hiện có.

Các thư mục 16 tướng đã tạo sẵn. Xem tên/ID ở [HERO_IDS.csv](HERO_IDS.csv).
Địch dùng cấu trúc tương tự trong `enemies/{id}/`; tên/ID ở
[ENEMY_IDS.csv](ENEMY_IDS.csv). CSV là bảng tra, không phải nơi thêm tướng
hay chỉnh gameplay.

## Chuẩn vẽ

| Loại | Canvas chuẩn | Cách vẽ |
|---|---|---|
| `portrait.png` | 512×512 | Chân dung; không vẽ sẵn khung phẩm, tên, sao |
| `body.png` và từng frame tướng | 512×512 | PNG trong suốt, toàn thân + vũ khí, nhìn sang phải |
| Frame VFX | 256×256 | PNG trong suốt, tâm hiệu ứng ở chính giữa |
| Icon trang bị/chức năng | 128×128 | PNG trong suốt, không chữ/khung/nút nền |
| Icon tiền tệ | 64×64 | PNG trong suốt |
| Nền | 1280×720 | Không vẽ UI, tên tướng, HP hoặc nhân vật chiến đấu |

Với **body và mọi frame nhân vật**, đặt trung điểm giữa hai chân ở
**x=256, y=460**. Giữ nguyên canvas và điểm chân cho cả bộ; không crop riêng
từng frame. Chừa chỗ cho vũ khí, mũ và áo choàng; toàn bộ nét vẽ nằm trong
canvas. Bóng chân do game vẽ, không vẽ bóng vào body để tránh chồng bóng.

Game sẽ thu ảnh theo kích thước hiển thị và tự lật tướng phe địch. Không
đưa chữ vào ảnh toàn thân. Chân dung không bị lật; ảnh được giữ tỷ lệ.
Khung phẩm, tên, HP/nộ vẫn do game vẽ.

Tất cả frame trong một clip có cùng kích thước. Tên frame là `000.png`,
`001.png`, `002.png`… liên tục, không bỏ số. Một frame vẫn dùng được như
hiệu ứng tĩnh. Không dùng tên `1.png`, `frame_1.png` hoặc trộn sprite sheet chưa khai báo vào thư mục frame rời.

Các clip tướng được hỗ trợ theo kế hoạch: `idle`, `attack`, `ultimate`,
`hit`, `death`, `victory`. Thiếu clip nào thì game dùng ảnh tĩnh hoặc
chuyển động bằng code. `idle` lặp ở 8 FPS; các clip còn lại mặc định 12 FPS,
không lặp. Game điều phối clip theo nhịp đòn đánh và tốc độ x1/x2/x3.

## Đăng ký clip để game phát

Copy các frame PNG sạch vào `animations/idle/`, `animations/attack/`,
`animations/hit/` của tướng, đánh số liên tục `000.png`, `001.png`…
Mở game hoặc vào trận mới là nhận. Không cần tạo SpriteFrames hay sửa code.
Idle lặp 8 FPS; attack/hit 12 FPS. Ultimate thiếu clip riêng sẽ mượn attack.
Death thiếu clip riêng dùng ngã bằng code. `victory` chưa được nối vào trận.

Trong mỗi clip, canvas phải giống nhau. Tối đa 120 frame, 10 giây và tổng
16 triệu pixel (khoảng 64 MiB texture RGBA mỗi clip, chưa tính overhead).
Frame thiếu số/hỏng/sai canvas sẽ dùng cả clip tốt trước đó hoặc fallback;
không trộn nửa bộ cũ và nửa bộ mới. Bỏ `.gitkeep` cũng được.

Mốc đánh mặc định là frame giữa, đánh số từ 0: clip 8 frame dùng `004.png`.
Cận chiến tiến tới rồi đánh tại mốc này; đòn xa phóng projectile tại mốc,
HP chỉ đổi khi đạn tới. `hit` phát khi nhận damage, né/heal không phát hit.
Toàn bộ frame, tiến/lùi và VFX chạy cùng tốc độ x1/x2/x3.

Không cần cấu hình nếu dùng mặc định. Muốn đổi mốc, tạo `animation.json`
trong thư mục tướng (ví dụ dưới đây dành cho attack có ít nhất 5 frame):

```json
{
  "version": 1,
  "clips": {
    "idle": { "fps": 8 },
    "attack": { "fps": 12, "event_frame": 4 },
    "hit": { "fps": 12 }
  },
  "effects": { "ultimate": { "fps": 12 } },
  "anchors": { "cast": [0.65, 0.55], "hit": [0.5, 0.55] }
}
```

FPS hợp lệ 1–60; event_frame phải nằm trong clip. Anchor là tọa độ chuẩn
hóa trên canvas, tự lật theo phe. Cấu hình sai dùng mặc định và báo console.

VFX cũng là chuỗi `000.png`…: `effects/normal` và `effects/ultimate` tại mục
tiêu; `effects/cast` ở tay caster khi phóng; `effects/projectile` bay từ tay
tới mục tiêu. Projectile vẽ hướng gốc sang phải; effect va chạm đặt tâm ảnh
ở giữa. Có thể dùng một frame cho VFX tĩnh. Shared hit/heal/projectile trong
`vfx/shared/` được dùng khi thiếu mẫu riêng tương ứng. VFX PNG và VFX code
không phát chồng nhau cho cùng một impact. `effects/aura` lặp dưới chân khi còn sống.

Bộ clip hiện đọc từ thư mục vật lý: source project hoặc `assets/` cạnh
executable desktop. Clip nằm hoàn toàn trong PCK chưa có inventory loader;
đóng gói clip theo PCK sẽ làm ở đợt export. Body/portrait vẫn có nhánh packed.

## Sprite sheet (tùy chọn)

Ảnh rời vẫn tự nhận theo tên. Bộ Gia Cát Lượng dùng thêm `atlas.png` với
khai báo trong `animation.json`, ví dụ một clip 8 frame trên sheet 3×3:

```json
{
  "version": 1,
  "clips": {
    "idle": { "sheet": "atlas.png", "columns": 3, "rows": 3, "frames": 8, "fps": 8 }
  }
}
```

Sheet đọc từ trái qua phải, trên xuống dưới; ô cuối không dùng. Mỗi clip
có thư mục riêng và một atlas. Nếu có `000.png`, game ưu tiên chuỗi PNG rời,
bỏ qua atlas cùng thư mục, nên bạn có thể thay art mà không sửa JSON.

Các tùy chọn nâng cao `regions`, `canvas`, `offsets`, `pivots`,
`display_scale`, `source_polygons` dùng để căn những sheet có khoảng trống
không đều. Bộ Gia Cát Lượng đã có metadata này; giữ nguyên khi copy bộ.
PNG nguồn được giữ nguyên, không cần export một ảnh riêng cho từng ô.

## Nền (quy ước) và icon PNG (đã nối UI)

```text
assets/
├── backgrounds/
│   ├── battle/
│   │   ├── default.png
│   │   ├── chapter_01.png
│   │   ├── chapter_02.png       # … đến chapter_08.png
│   │   ├── duel.png
│   │   ├── trial.png
│   │   ├── party.png
│   │   └── dungeon.png
│   └── hub/
│       └── default.png
├── icons/
│   ├── equipment/
│   │   ├── weapon.png
│   │   ├── armor.png
│   │   ├── helmet.png
│   │   ├── boots.png
│   │   ├── cloak.png
│   │   └── bracer.png
│   ├── currency/
│   │   ├── gold.png
│   │   └── silver.png
│   └── features/
│       ├── hero_list.png
│       ├── bag.png
│       ├── formation.png
│       ├── equipment.png
│       ├── recruit.png
│       ├── stage.png
│       ├── activity.png
│       ├── star_chart.png
│       └── money_tree.png
├── ui/                         # Để dành; panel/nút 9-patch chưa thuộc đợt đầu
└── vfx/shared/
    ├── hit/000.png
    ├── heal/000.png
    └── projectile/000.png
```

Bộ icon hiện có: 6 trang bị, 2 tiền tệ và 9 chức năng. Trang bị/chức năng
là PNG RGBA 128×128; tiền tệ 64×64. Hub, Túi đồ, Võ tướng, màn Trang bị
và icon tiền tệ Chiêu mộ dùng ảnh mới; thiếu ảnh sẽ hiện chữ dự phòng.
Khung ô, nhãn và huy hiệu cường hóa do UI vẽ. Mở lại màn sau khi thay ảnh.

Xem [toàn bộ icon](../docs/previews/ui-icons.png) và
[prompt tạo ảnh](icons/PROMPTS.md). Nền PNG bên dưới vẫn chưa được nạp.

Nền trận vẽ trên toàn canvas 1280×720. Phần trên/dưới sẽ bị HUD che một
phần; mặt đất phải có dưới các điểm đứng của cả ba hàng tướng. Dùng
[ảnh sân mẫu](../docs/previews/battle-arena.png) làm mốc bố cục, không vẽ
tướng hoặc thanh HP trong ảnh nền. Đợt đầu hỗ trợ một ảnh nền phẳng;
parallax và nhiều lớp nền làm sau.

## Dùng ảnh tĩnh ngay bây giờ

1. Copy PNG vào `heroes/{id}/body.png` hoặc `portrait.png`; địch dùng
   `enemies/{id}/body.png` hoặc `portrait.png`.
2. Mở game. Nếu game đã chạy, vào trận mới để nhận body mới; mở lại popup
   Võ tướng/Bày trận để nhận chân dung mới, hoặc vào lại Hub để đổi avatar.
3. Ghi đè cùng tên để thay; xóa để trở về hình mặc định. Ảnh lỗi giữ bản tốt
   đã nạp trong phiên nếu có, đồng thời báo đường dẫn ở console.
4. PNG tối đa 4096×4096 và 32 MB; chuẩn vẽ vẫn là 512×512. Bản xuất desktop
   đã có nhánh đọc thư mục `assets/` cạnh executable; kiểm thử đóng gói sạch
   vẫn thuộc đợt 5.

Không đặt tranh gốc vào thư mục test. PNG kỹ thuật trong test được tạo ở
thư mục tạm riêng, không được copy vào các thư mục tướng của bạn.

## Copy và thay ảnh tự động trên màn đang mở (bước tiếp theo)

Các bước sau áp dụng khi triển khai reload, hiện chưa cập nhật ngay trên màn đang mở:

1. Xuất **PNG** đúng tên ở thư mục tương ứng. Tên thư mục/file dùng chữ
   thường, không dấu; Linux phân biệt chữ hoa/chữ thường.
2. Copy vào `assets/` trong dự án. Game nhận ngay khi mở; nếu đang chạy,
   tự phát hiện và áp dụng khi về trạng thái chờ hoặc khi mở lại màn.
3. Ghi đè cùng tên để thay ảnh. Xóa file thì phần đó trở về hình mặc định.
4. Không đổi tên thư mục tướng để đổi tên hiển thị. Không tự thêm tướng
   mới bằng ảnh: tướng mới vẫn phải có dữ liệu gameplay.

Bản xuất **Windows/Linux** sẽ hỗ trợ cùng cây `assets/` đặt cạnh file chạy.
Chỉ cần copy các ảnh muốn thay; phần còn lại dùng asset đã đóng gói hoặc
hình mặc định. Web/mobile có cách cài asset khác, chưa thuộc đợt này.

Khi đang copy một bộ nhiều frame, game đợi file ổn định rồi mới nạp; không
đổi hình giữa một đòn đang đánh. Ảnh lỗi sẽ giữ bản tốt đang dùng và báo
đường dẫn cần sửa. Không cần tự xóa cache hay sửa file import của Godot.

Giữ file gốc `.psd`/`.kra`/`.aseprite` ngoài cây `assets/` (ví dụ một thư
mục `art_source/` riêng); thư mục này chỉ chứa bản xuất game dùng.
