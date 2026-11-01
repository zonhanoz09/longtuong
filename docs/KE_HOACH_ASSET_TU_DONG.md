# Kế hoạch thư mục asset tự nhận

Ngày: 01/11/2026. Yêu cầu đã chốt: người dùng tự vẽ, copy ảnh đúng chỗ là
game nhận; không đăng ký từng đường dẫn, không sửa code khi thay art.
Trạng thái: đã hoàn thành đợt 1 cho PNG tĩnh; reload và các nhóm asset khác đang chờ triển khai.

## 1. Phạm vi và nguồn quy ước

[assets/README.md](../assets/README.md) là hướng dẫn chính để vẽ/xuất/copy.
Các tên file trong đó là hợp đồng cho bộ nạp, không tạo một danh sách tên
khác trong code. Bộ nạp nhận art cho ID đã có trong HeroDatabase và
EnemyDatabase. Không dùng tên hiển thị hay tên tài khoản làm đường dẫn.

Ưu tiên: tướng + portrait → VFX → nền → icon. Hỗ trợ Windows/Linux, khi
chạy từ source và khi chạy bản xuất. Ảnh PNG rời và chuỗi frame là đầu
vào chuẩn đợt đầu; sprite sheet, rig, audio, font và panel 9-patch để sau.
Không cần JSON bên cạnh ảnh trong quy trình mặc định.

## 2. Quy tắc tìm asset

Hai nguồn, với thứ tự rõ ràng:

- Trong lúc phát triển: thư mục vật lý `res://assets/` đã globalize. Đọc
  PNG trực tiếp để nhận file vừa copy mà chưa được editor import.
- Trong bản xuất desktop: thư mục `assets/` cạnh executable là lớp thay
  thế. Sau đó mới tìm asset đóng gói trong `res://assets/`.
- Thiếu cả hai nguồn: dùng hình bằng code hiện tại. Một ảnh override lỗi
  không được làm mất asset đóng gói hoặc làm trận đấu dừng.

Không dùng OS.GetExecutablePath để tìm gốc dự án khi chạy trong editor
(đường đó trỏ đến executable Godot). Không giả định `res://` trong PCK là
một thư mục vật lý có thể dùng System.IO đọc.

Phân giải riêng từng loại, không yêu cầu một tướng phải đủ mọi file:

| Thành phần | Thứ tự chọn |
|---|---|
| Chân dung | `portrait.png` của tướng → chữ đại diện hiện có |
| Chờ trong trận | clip `idle` → `body.png` → hình procedural |
| Attack/ultimate/hit/victory thiếu clip | ảnh hiện tại + chuyển động procedural thích hợp |
| Death thiếu clip | nghiêng/mờ hình hiện tại bằng code |
| VFX đánh thường | `effects/normal` của tướng → `vfx/shared/hit` → VFX code |
| VFX tuyệt kỹ gây damage | `effects/ultimate` của tướng → VFX motif code hiện tại |
| VFX tuyệt kỹ hồi máu | `effects/ultimate` của tướng → `vfx/shared/heal` → VFX code |
| Đạn | `effects/projectile` của tướng → `vfx/shared/projectile` → projectile code |
| Nền chiến dịch | `chapter_XX.png` → `default.png` → nền procedural |
| Nền chế độ khác | `{duel,trial,party,dungeon}.png` → `default.png` → procedural |
| Icon | PNG theo tên đã quy định → ký hiệu/icon code hiện có |

Portrait và body không thay lẫn nhau vì khác bố cục. Portrait dùng chung ở
Hub, Võ tướng, Bày trận và những nơi có avatar; không tạo nhiều bản copy.
Màu phẩm và màu phe vẫn là overlay độc lập, không tô lại toàn ảnh người vẽ.

## 3. Kiến trúc thực hiện

- `AssetPaths`: xây đường dẫn từ các nhóm cố định và ID gameplay hợp lệ.
- `AssetCatalog`: API dùng chung lấy portrait, body/clip, VFX, nền, icon;
  trả kết quả có fallback, không để scene tự đọc file rải rác.
- `LooseAssetSource`: đọc PNG vật lý bằng Image.LoadFromFile và tạo
  ImageTexture trên luồng chính. Không lệ thuộc `.import`/`.tres` của editor.
- `PackedAssetSource`: dùng ResourceLoader cho texture đã import/đóng gói.
- `AssetReloadService`: kiểm tra thay đổi, cache và thông báo theo asset key.
  BattleArtCatalog vẫn giữ preset procedural và thông tin trình diễn.
- `assets/catalog.generated.json`: inventory được tool build/export sinh
  tự động cho clip đã đóng gói. Người vẽ không chỉnh file này. Kiểm tra
  build xác nhận manifest không cũ; PNG đóng gói phải load được bằng
  ResourceLoader sau import/export, không dùng Image.LoadFromFile đọc PCK.

Catalog chỉ nạp asset đang cần; không giải mã toàn bộ ảnh tất cả tướng lúc
mở game. Texture/clip cache dùng chung cho nhiều instance cùng loại địch.
Không tạo texture mới trong _Draw hoặc giải mã PNG mỗi frame.

Kích thước canvas, pivot chân và điểm phát VFX tuân theo README. Khi gắn
ảnh vào BattleActor, tính lại scale từ pixel sang kích thước thế giới;
không nhân nguyên hệ số 0,7 của hình code vào ảnh 512px. Điểm chân không
đổi khi lật phe, thay frame, chết, di chuyển hoặc đổi loại body.

Ảnh UI dùng giữ tỷ lệ và clip trong vùng bố cục hiện có. Texture art mượt
dùng linear filter ở node tương ứng; không đổi filter toàn dự án chỉ vì
thêm ảnh. Pixel art có thể thêm tùy chọn riêng ở đợt cấu hình sau.

## 4. Tự cập nhật khi game đang chạy

- Kiểm tra thư mục vật lý mỗi khoảng 1 giây bằng danh sách file, kích thước
  và thời gian sửa; chỉ giải mã khi có thay đổi. Có lệnh Nạp lại asset trong
  màn xem thử để chủ động làm mới và chẩn đoán.
- Gom thay đổi một clip, đợi ít nhất hai lần kiểm tra liên tiếp ổn định.
  Bộ frame phải liên tục từ 000 và cùng canvas; chỉ thay cả clip khi hợp lệ.
- File copy dở/PNG hỏng: giữ bản tốt gần nhất, báo lỗi gọn với đường dẫn;
  thử lại khi file thay đổi. Không lặp log mỗi giây cho cùng một lỗi.
- Xóa file có chủ ý: invalidation cache rồi dùng nguồn tiếp theo/fallback.
  Thêm lại cùng tên phải nhận được dù lần tra trước đã ghi nhận thiếu file.
- Nếu đang đánh: giữ bộ texture/clip của hành động hiện tại, áp dụng thay
  đổi ở đầu hành động kế tiếp. UI cập nhật khi màn đang chờ hoặc được mở;
  nền áp dụng ở lần vào trận kế tiếp. Không nháy hình giữa chiêu.
- Không reload scene, reset engine, đổi RNG, đổi save hoặc phát lại thưởng
  khi asset đổi. Thoát scene phải hủy đăng ký reload và giải phóng tham chiếu.

## 5. Animation và nhịp gameplay

Mỗi clip là chuỗi PNG đánh số, fps mặc định theo README. SpriteFrames
hoặc trình phát frame được tạo tự động trong bộ nạp; người vẽ không tạo
resource Godot. Idle/victory/clip hành động được chọn theo trạng thái.

BattleAction vẫn là nguồn duy nhất của mục tiêu và kết quả. Animation
player đọc action đã tính, tuyệt đối không gọi Hit hoặc cộng/trừ máu.
Đòn được chia chuẩn bị/di chuyển → chạm → hồi phục/trở về; clip attack và
ultimate được ánh xạ vào timeline này, mặc định mốc chạm tại giữa clip.
Không để ảnh có nhiều nhát chém tự sinh nhiều lần damage.

Trong đợt đầu, fps quy định nhịp gốc; trình điều phối có thể giữ/cắt thời
gian frame ở ranh giới phase để khớp mốc chạm và projectile. x1/x2/x3 phải
áp dụng thống nhất với clip, VFX và di chuyển. Chỉ chuyển đội hoặc hiện
popup sau khi trình diễn cuối hoàn tất.

Cấu hình tùy chọn để đổi pivot, kích thước hiển thị hoặc mốc chạm chính
xác sẽ làm sau nếu bản mẫu cho thấy cần. Không bắt người vẽ tạo metadata
cho từng PNG hoặc sửa JSON mới dùng được asset.

## 6. Lộ trình giao từng phần

| Đợt | Nội dung | Cách nghiệm thu |
|---|---|---|
| 0 — Đã làm | Thư mục, chuẩn xuất và bảng ID | Tìm được đúng thư mục cho 16 tướng và bảng 35 ID địch |
| 1 — Đã làm | Catalog + PNG body/portrait + fallback; nối BattleActor, HeroList, Hub, Formation | Copy hai PNG Quan Vũ, mở game thấy đúng ở trận và các màn chân dung; không sửa code |
| 2 | Reload ảnh rời + công cụ xem/chẩn đoán | Ghi đè/xóa/thêm lại khi game đang chạy; hình đổi ở thời điểm an toàn |
| 3 | Chuỗi frame nhân vật, VFX và projectile | Copy các frame là chạy; cùng điểm chân ở cả hai phe; x3 và HP khớp |
| 4 | Nền chương/chế độ + icon trang bị, Hub và tiền tệ | Chỉ cần đúng tên file, tự áp dụng ở mọi màn dùng chung |
| 5 | Import/export inventory + loose overrides | Bản xuất sạch nhận asset đóng gói; copy thư mục cạnh executable thay được art mà không build lại |

Đợt 1 phải hỗ trợ một ảnh tĩnh trước, không đợi có đủ animation mới sử dụng
tranh tự vẽ. PNG kiểm thử kỹ thuật không được trình bày là art của người dùng.

## 7. Kiểm tra bắt buộc khi triển khai

- Một phần asset có, một phần thiếu; ID địch trùng nhau; ID lạ không crash.
- Ảnh 512px không làm phình cell hoặc che HP; resize/aspect ratio, lật phe,
  pivot và clip nhiều frame không rung chân. Render cửa sổ thật để kiểm tra.
- Tự nhận ảnh mới sau lần tra bị thiếu; ghi đè cùng tên không dùng cache cũ;
  file hỏng/copy dở không làm mất ảnh tốt; xóa file trở về fallback.
- Thay asset giữa chiêu không đổi seed, HP cuối, tiến độ hoặc thưởng; chạy
  trọn trận x1/x2/x3, AoE, hồi máu và chuyển đội Tổ Đội.
- Chuỗi frame thiếu số/sai kích thước bị phát hiện; log chỉ rõ file lỗi.
- Xuất desktop sạch rồi thử cả PNG đóng gói và PNG cạnh executable, không
  cần editor import; kiểm tra mới/thay/xóa clip, inventory và cache.
- Nhiều trận liên tiếp: không tích texture cũ hoặc subscription reload;
  đo bộ nhớ thực tế vì PNG nhỏ trên đĩa vẫn có thể chiếm nhiều VRAM.

Kế hoạch này cập nhật lựa chọn cũ “chỉ hình bằng code”: từ nay tranh tự vẽ
là nguồn art chính khi có file, hình bằng code là fallback. Không cần duyệt
lại nguồn art; người dùng đã chọn tự vẽ trong yêu cầu này.


## 8. Kết quả đợt 1 — PNG tĩnh

- `core/assets/AssetCatalog.cs`: tự phân biệt heroes/enemies từ ID, đọc PNG
  vật lý không cần import, cache chia sẻ, kiểm tra kích thước và fallback.
  Đường dẫn development là project/assets; desktop export dùng assets cạnh
  executable rồi mới tới resource đóng gói. Khi chạy editor không đọc lại
  ảnh đã xóa qua `.import` cũ.
- `BattleActor` nạp body lúc vào cây scene; scale từ kích thước canvas,
  giữ chân (256,460)/512, giữ bóng chân/vòng lượt và lật theo phe. Thiếu ảnh
  vẫn vẽ procedural. Texture được giữ suốt đời actor để không đổi giữa chiêu.
- `AssetIcons.ApplyPortrait` dùng TextureRect giữ tỷ lệ và không tăng minimum
  size; ẩn chữ đại diện khi có ảnh, phục hồi khi thiếu hoặc đổi tướng.
  Đã nối HeroList, Hub, các ô Formation và MakePortrait dùng chung.
- Mở lại màn hoặc vào trận mới sẽ tra lại dấu thời gian/kích thước file.
  Đổi PNG hỏng giữ texture tốt gần nhất; xóa file xóa cache ở lần tra tiếp.
  Chưa có polling hay cập nhật tức thời trên scene đang mở.
- TestAssetCatalog tạo PNG kỹ thuật trong thư mục test riêng, không sửa
  asset thật. Kiểm tra thiếu/thêm/thay/xóa/hỏng, cache, ID địch, body/pivot,
  portrait ở các màn và thao tác Bày trận. Có chế độ `-- --capture` để kiểm
  tra bằng renderer thật; ảnh kỹ thuật không được coi là art game.
- Đã build và chạy test mới bằng Godot 4.3 .NET; đã xem ảnh render kiểm tra
  điểm chân, lật phe và khung chân dung. Bản xuất desktop sạch/PCK vẫn cần
  kiểm thử trong đợt 5; hiện chưa coi nhánh packed fallback là nghiệm thu export.

Kiểm tra hồi quy đợt 1: đã chạy 59 scene (57 phase cũ + BattleVisuals +
AssetCatalog). Test mới và battle test đều qua; bốn lỗi layout cũ ở
214/216/217/244 và log RID khi thoát 227 giữ nguyên như baseline đã xác
nhận trước đó. Đã xem thêm ảnh render Võ tướng, Hub, Bày trận; portrait
trong ô Bày trận chừa khoảng đệm để không lấn viền vòng tròn hoặc tên.
