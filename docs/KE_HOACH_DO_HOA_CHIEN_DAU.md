# Kế hoạch đồ họa, đấu trường và hiệu ứng tướng

Ngày lập: 01/11/2026. Trạng thái: đang triển khai bản mẫu bằng code; hướng art bitmap vẫn là đề xuất.

## 1. Hiện trạng và hướng hình ảnh

- Game C# / Godot, khung thiết kế 1280×720, renderer GL Compatibility.
- BattleScene đang hiển thị ô tên, HP/nộ, viền lượt; chạy tự động x1/x2/x3.
- Có 15 danh tướng + tướng tự tạo; địch gồm lính và boss riêng.
- UiTheme và AssetIcons đã có nền tảng màu tối/vàng và hình đại diện bằng code.
- Plan chuẩn hóa asset trước đây chốt làm bằng code. Bản này đề xuất thêm art 2D; chưa coi nguồn art hay phong cách mới là quyết định đã duyệt.

Hướng đề xuất: Tam Quốc 2D bán hiện thực, silhouette rõ, vũ khí đặc trưng,
giáp có chi tiết vừa phải. UI giữ tối/vàng; chiến trường có màu tự nhiên,
giảm tương phản ở hậu cảnh để nổi nhân vật. Tạo thiết kế riêng cho game.
Nếu tiếp tục chỉ dùng code, giữ nguyên lộ trình kỹ thuật và dùng hình tướng
cách điệu, nền nhiều lớp, vệt chém và hình học procedural.

## 2. Đấu trường

Ở đây “đấu trường” là màn trình diễn trận đánh hiện có; PvP/xếp hạng là phạm vi riêng.

- Đội mình bên trái nhìn phải, đội địch bên phải nhìn trái; giữ vị trí Tiền/Trung/Hậu và luật chọn mục tiêu hiện tại.
- Dùng điểm đứng theo slot thay cho việc kéo giãn sprite theo GridContainer. Xếp lớp theo chân nhân vật để tướng hàng dưới che đúng hàng trên.
- Phân lớp: trời/hậu cảnh → kiến trúc xa → mặt đất → bóng chân → tướng → VFX trước mặt → HUD/số sát thương.
- Thanh HP/nộ và tên đặt gần nhân vật, không che mặt/vũ khí; chỉ báo lượt nằm dưới chân. Hai đội phân biệt thêm bằng hướng đứng và ký hiệu.
- Chừa hành lang giữa sân cho lao tới, đạn và tuyệt kỹ. Thử đủ 5v5, boss ×1,3 và vũ khí dài ngay từ bản mẫu.
- Vòng thú cưỡi hiện là placeholder: không thiết kế thành hệ thống thú cưỡi trong đợt này.

Danh mục nền, làm dần theo chương:

| Chương | Chủ đề chiến trường |
|---|---|
| 1 — Hoàng Cân | Đường đất, trại quân, cờ vàng |
| 2 — Thảo phạt Đổng Trác | Cổng thành, chiến lũy |
| 3 — Trường An/Lạc Dương | Thành cổ, cung điện đổ nát |
| 4 — Quan Độ/Ô Sào | Doanh trại, kho lương |
| 5 — Tân Dã/Kinh Châu | Thành ven sông, đồng cỏ |
| 6 — Xích Bích | Sàn chiến thuyền, khói lửa xa |
| 7 — Tây Xuyên/Nam Man | Rừng núi, trại gỗ |
| 8 — Hán Trung | Đường núi, cửa ải |

MVP chỉ cần một nền doanh trại ban ngày. Các chế độ PvE dùng lại nền phù hợp,
sau đó thêm biến thể đêm/sương; ưu tiên rõ đội hình hơn số lượng nền.

## 3. Bộ hình và chuyển động tướng

Mỗi tướng có portrait, ảnh toàn thân nền trong suốt, bóng chân, điểm gắn
vũ khí/phóng đạn/trúng đòn và cấu hình màu hiệu ứng. Dùng chung portrait
cho Võ tướng, Bày trận, Tửu quán và Hub qua catalog.

Quy chuẩn sản xuất đề xuất:

- Portrait gốc 512×512; xuất bản UI 256×256.
- Nhân vật trên canvas 512×512, chân cùng mốc, chừa vùng vũ khí; kích thước hiển thị quyết định sau khi thử 5v5 ở 720p.
- MVP dùng ảnh toàn thân với AnimationPlayer/Tween: chờ, tiến/lùi, đánh thường, tung chiêu, trúng đòn, ngã, chiến thắng.
- Khi cần chuyển động tay/chân thật: tách lớp đầu/thân/tay/vũ khí/áo để rig 2D hoặc làm sprite sheet. Không tạo toàn bộ animation cho mọi tướng trước khi nghiệm thu một mẫu.
- Lật sprite cho hai phe, kiểm tra vũ khí và phụ kiện bất đối xứng; không đưa chữ vào ảnh cần lật.
- Tướng tự tạo có mẫu riêng mặc định; tùy biến ngoại hình để đợt sau.

## 4. Hiệu ứng theo tướng

Đây là mô tả hình ảnh của kỹ năng đang có. Lửa/độc/rồng/hổ chỉ là biểu tượng;
không tự thêm DOT, choáng, khiên hay số lần gây sát thương.

| Tướng | Đánh thường | Tuyệt kỹ đề xuất |
|---|---|---|
| Tướng tự tạo | Vệt chém trắng | Chớp vàng gọn, không dựng đại cảnh cho chiêu placeholder |
| Lưu Bị | Hai vệt kiếm nối tiếp | Song Cổ Kiếm: hai cung kiếm vàng giao nhau |
| Quan Vũ | Cung đao xanh ngọc | Thanh Long Yển Nguyệt: long ảnh chạy theo lưỡi đao |
| Trương Phi | Mũi mâu + bụi đất | Tiếng Thét Trường Bản: sóng âm hướng vào một mục tiêu |
| Triệu Vân | Thương bạc, dư ảnh ngắn | Bạch Long Xuất Động: lao thương, long ảnh trắng |
| Gia Cát Lượng | Luồng gió từ quạt | Bát Quái Trận: trận đồ dưới toàn đội địch, cột sáng theo mục tiêu |
| Điêu Thuyền | Dải lụa/hạt sáng | Khuynh Quốc: cánh hoa và vòng sáng tại đồng đội được hồi máu |
| Lữ Bố | Kích đỏ, tia va chạm | Vô Song Kích: đại trảm đỏ tím, rung nhẹ lúc trúng |
| Tôn Kiên | Đao vàng cam | Mãnh Hổ Giang Đông: hổ ảnh lao vào mục tiêu |
| Điển Vi | Song kích màu thép | Ác Lai Hộ Chủ: hai kích giáng xuống, bụi đá tại mục tiêu |
| Giả Hủ | Luồng khí tím | Ly Gián Liên Hoàn: ấn tím và khói mỏng trên toàn đội địch |
| Trương Liêu | Kích xanh thép | Tiêu Dao Tân Uy Chấn: đường lao nhanh, vệt kích cắt chéo |
| Từ Thứ | Phù văn xanh lam | Thanh Thủy Hồi Xuân: vòng nước và số hồi máu tại một đồng đội |
| Chu Du | Sóng âm đỏ cam | Liên Hoàn Hỏa Công: lửa quét qua toàn đội địch |
| Chúc Dung | Phi đao có đuôi lửa | Hỏa Diệm Phi Đao: phi đao xoáy, nổ lửa cục bộ |
| Mã Siêu | Thương vàng trắng | Cẩm Mã Siêu Xung Kích: lao thương kèm bụi và dư ảnh |

Hiệu ứng dùng chung: trúng đòn, né, đỡ, bạo kích, hồi máu, tăng nộ,
ngã và biến mất. Bạo kích phân biệt bằng cỡ chữ/ký hiệu cùng màu;
tránh chỉ dựa vào màu. Có tùy chọn giảm chớp sáng và tắt rung màn hình.

## 5. Kết nối với engine

- BattleEngine tiếp tục tính kết quả bằng C# thuần, giữ RNG/seed và luật thưởng.
- Bổ sung dữ liệu sự kiện có cấu trúc cho mỗi hành động: người đánh, kỹ năng, danh sách mục tiêu, HP/nộ trước/sau, damage/heal, né/đỡ/bạo và tử trận. Dùng định danh thực thể trong trận, không chỉ heroId vì địch có thể trùng loại.
- Không phân tích chuỗi Log để đoán hiệu ứng. Presentation đọc kết quả đã tính và phát theo hàng đợi.
- Một hành động: chuẩn bị → di chuyển/phóng đạn → chạm mục tiêu → cập nhật HP hiển thị/số damage → trở về. Đòn nhiều vệt vẫn áp đúng tổng damage một lần theo engine.
- UI giữ snapshot hiển thị cho đến mốc chạm; không đọc HP cuối của engine để làm tụt máu trước khi đòn tới.
- Điều phối StepBattle và timer để không phát chồng lượt. x1/x2/x3 thay tốc độ cả animation, projectile và nhịp chờ; không thay kết quả mô phỏng.
- Chỉ hiện popup sau hiệu ứng cuối. Với Tổ Đội, xong animation đội cũ mới đổi đội, giữ HP boss; thoát scene phải hủy tween/hàng đợi.
- Catalog ánh xạ heroId/enemyId/skill sang hình và cấu hình; thiếu art dùng placeholder có sẵn. Nền ánh xạ theo chương/chế độ.
- Dùng Node2D cho sân/tướng, Control cho HUD; điểm đứng quy đổi từ khung thiết kế. Dùng Tween/AnimationPlayer và hiệu ứng tương thích renderer hiện tại, đo thử trước khi chọn shader/particle phức tạp.

Cấu trúc dự kiến:

```text
assets/heroes/{hero_id}/       # portrait, body hoặc spritesheet
assets/enemies/{enemy_id}/
assets/backgrounds/battle/
assets/vfx/{effect_id}/
features/battle/presentation/  # actor, arena, hàng đợi animation
core/ui/                      # catalog và style dùng chung
```

## 6. Lộ trình và điều kiện hoàn thành

| Đợt | Công việc | Điều kiện nghiệm thu |
|---|---|---|
| A — Chốt mẫu | Một bảng phong cách, nền doanh trại, Quan Vũ, một lính, một vệt chém | Thấy rõ tỷ lệ và đội hình 5v5 tại 1280×720; thống nhất nguồn art |
| B — Một trận hoàn chỉnh | Catalog/fallback, actor, sự kiện hành động, hàng đợi, đánh/trúng/ngã, HP và số damage | Chơi từ đầu tới popup; x1/x2/x3 đúng nhịp; seed cũ ra cùng kết quả |
| C — Ba loại kỹ năng | Hoàn thiện Quan Vũ + Gia Cát Lượng + Điêu Thuyền | Một mục tiêu, toàn đội địch và hồi máu đều đúng người/đúng thời điểm |
| D — Đủ roster | 12 danh tướng còn lại + tướng tự tạo; portrait dùng chung các màn | Mọi tướng có nhận diện và tuyệt kỹ riêng; thiếu file vẫn chạy |
| E — Địch và môi trường | 8 mẫu lính, boss ưu tiên chương 1 rồi các chương sau; 8 chủ đề nền | Mọi encounter có hình hoặc fallback rõ ràng; boss không che HUD |
| F — Hoàn thiện | Âm thanh đồng bộ, giảm hiệu ứng, preload/cache, giới hạn particle | Chạy liên tiếp nhiều trận không tồn dư actor/VFX; không khựng lần đầu tung chiêu |

Lính dùng chung bộ chuyển động theo loại vũ khí. Boss có silhouette/phụ kiện
và chiêu đặc trưng; không làm toàn bộ boss thành tướng mới hoàn chỉnh ngay đợt đầu.

Kiểm chứng khi triển khai:

- Build và chạy suite hiện có sau thay đổi engine/timing; bổ sung test có ý nghĩa cho dữ liệu sự kiện, seed, mục tiêu và chuyển đội.
- Kiểm tra hình trực tiếp: 5v5, AoE, hồi máu, né, đỡ, bạo, nhiều tướng ngã cùng lượt, boss lớn, x3 và đổi scene giữa hiệu ứng.
- Mục tiêu 60 FPS trên máy đích sẽ chọn; ghi lại thiết bị và đo trước khi chốt ngân sách texture/particle. Không coi FPS headless là kiểm thử đồ họa.
- Lưu nguồn/license/prompt và bản gốc của art; kiểm tra nền trong suốt, chân, vũ khí, màu và độ đồng nhất trước import.

## 7. Ưu tiên bắt đầu

Làm A → B → C trước: một nền + Quan Vũ/Gia Cát Lượng/Điêu Thuyền + một
lính mẫu đủ kiểm chứng diện mạo và ba kiểu kỹ năng. Sau khi mẫu ổn mới
sản xuất hàng loạt. Ước lượng công sức đợt D/E dựa trên thời gian làm mẫu,
không chốt lịch khi chưa chọn ảnh tĩnh, rig hay sprite sheet.


## 8. Tiến độ bản mẫu — 01/11/2026

Đã thêm sân doanh trại procedural, hình tướng mặc định và ba dáng riêng
Quan Vũ/Gia Cát Lượng/Điêu Thuyền; hiệu ứng trảm, bát quái, hồi máu/cánh hoa.
Engine cung cấp action snapshot và impact; màn trận phát tuần tự và giữ HP
hiển thị đến mốc chạm. Tốc độ ảnh hưởng tween lẫn khoảng chờ; popup/chuyển
đội chờ chiêu cuối kết thúc. Có scene xem thử `tests/PreviewBattle.tscn`.

Đây là lát cắt đầu tiên của A/B/C. Chưa hoàn tất nghiệm thu hình ảnh, portrait,
catalog texture, rig/sprite sheet, chuyển động lao tới mục tiêu, nền theo chương,
art riêng cho toàn roster/boss, âm thanh hoặc đo FPS máy đích.

Kiểm tra: build trên Godot 4.3 .NET (bản cài thực tế), test action/heal/AoE/
trùng loại địch, nhịp HP, đổi x3 và vòng đời hiệu ứng. Suite cũ gồm 57 scene:
52 scene sạch; 4 scene lỗi layout (214/216/217/244) và 227 có cảnh báo rò
rỉ RID khi thoát. Đã tái hiện cùng các lỗi đó trên HEAD trước sửa trong
thư mục riêng với cấu hình project hiện có. Không dùng chúng để khẳng định
chất lượng hình ảnh; môi trường chưa có X11/Wayland để chụp màn hình thật.


## 9. Tiến độ tiếp theo — 01/11/2026

- `BattleArtCatalog` ánh xạ đủ 16 mẫu tướng theo id: màu, vũ khí, họa tiết
  tuyệt kỹ, đòn xa/cận chiến và phụ kiện mũ. Đây là catalog procedural;
  chưa phải catalog texture hay bộ art hoàn thiện. Địch chưa có mẫu riêng
  dùng fallback theo vai trò.
- Thêm song kiếm Lưu Bị, mâu Trương Phi, thương Triệu Vân/Mã Siêu,
  kích Lữ Bố/Trương Liêu, song kích Điển Vi, đàn Chu Du, sách Giả Hủ/Từ Thứ,
  phi đao Chúc Dung. Dáng giáp nền vẫn dùng chung.
- Cận chiến di chuyển đến đối thủ thực tế, nâng lớp vẽ khi đánh và trở về
  đúng điểm đứng. Đòn xa phóng một projectile cho mỗi mục tiêu do engine
  đã chọn; hình ảnh không tạo thêm hit/DOT/buff.
- Các motif gồm trảm/rồng, sóng âm, đâm thương, bát quái, hoa, song trảm,
  vệt hổ, bụi đá, độc tím, nước và lửa; khác biệt qua hình học/màu/tham số.
- Hạ chân trời để hàng tướng trên đứng trên mặt đất; bỏ khung chữ nhật
  quanh tướng, giữ vòng dưới chân cho lượt hiện tại.
- Đã chạy cửa sổ thật với Godot 4.3 .NET, GL Compatibility trên RTX 2060.
  Đã xem [ảnh sân](previews/battle-arena.png) và [bảng đủ roster](previews/battle-art-gallery.png).
  Chạy gallery: `godot --path . res://tests/PreviewBattleArtGallery.tscn`.
- Test bổ sung kiểm tra coverage roster, fallback, projectile, di chuyển
  cận chiến, trở về điểm đứng và thoát scene giữa animation. Test scene
  chờ TreeExited khi kiểm tra queue_free để tránh phụ thuộc ranh giới frame.

Tiếp theo: portrait đồng bộ ở các màn; mẫu lính/boss theo chương; các nền
chiến trường; animation vũ khí/đỡ/ngã chi tiết; âm thanh và đo hiệu năng.
Nguồn art bitmap/rig/sprite sheet vẫn chưa chốt.


## 10. Nguồn art đã chọn — tự vẽ, copy vào thư mục

Người dùng đã chọn tự vẽ và yêu cầu game tự nhận asset. Quy ước hiện hành
ở [assets/README.md](../assets/README.md); thiết kế catalog, reload và bản
xuất desktop ở [kế hoạch bộ nạp](KE_HOACH_ASSET_TU_DONG.md). Đã tạo sẵn
thư mục 16 tướng và bảng ID địch. Chưa triển khai bộ đọc PNG/runtime reload.
Các câu “chưa chốt nguồn art” trong tiến độ trước là trạng thái lịch sử.
