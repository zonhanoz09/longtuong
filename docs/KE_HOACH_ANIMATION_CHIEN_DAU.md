# Kế hoạch đăng ký clip và trình diễn chiến đấu

Ngày: 01/11/2026. Trạng thái: đã triển khai loader và timeline cho clip/VFX PNG vật lý; đã có bộ Gia Cát Lượng đầy đủ và đã kiểm tra bằng renderer.
Gia Cát Lượng là mẫu nghiệm thu đầu tiên; cơ chế dùng chung cho mọi tướng/địch.

## 1. Kết quả cần đạt

Người vẽ chỉ xuất PNG vào thư mục đúng ID. Game tự nhận `idle`, `attack`,
`hit`, không yêu cầu đăng ký từng frame, tạo `.tres` hoặc chỉnh code.
Di chuyển tiến/lùi, chuyển frame, projectile, số damage và VFX phải cùng
nhịp ở x1/x2/x3. BattleEngine tiếp tục quyết định mục tiêu và kết quả.

Hiện đã có: ảnh body/portrait, BattleAction với snapshot và impact,
di chuyển cận chiến, projectile đòn xa và VFX bằng code. Đã bổ sung đọc
thư mục frame, phát clip và đăng ký VFX PNG theo timeline chung.

## 2. Hợp đồng thư mục

```text
assets/heroes/gia_cat_luong/
├── body.png
├── portrait.png
├── animations/
│   ├── idle/                 # 000.png … 007.png, khuyến nghị 8 frame
│   ├── attack/               # 000.png … 007.png, khuyến nghị 8 frame
│   ├── hit/                  # 000.png … 003.png, khuyến nghị 4 frame
│   ├── ultimate/             # Tùy chọn, thiếu dùng attack
│   └── death/                # Tùy chọn, thiếu dùng ngã bằng code
├── effects/
│   ├── normal/               # Hiệu ứng trúng đòn thường
│   ├── ultimate/             # Hiệu ứng trúng tuyệt kỹ
│   ├── projectile/           # Đạn, hướng gốc sang phải
│   ├── cast/                 # Tùy chọn: hiệu ứng tại người tung chiêu
│   └── aura/                 # Tùy chọn: vòng sáng khi chờ, làm sau
└── animation.json            # Tùy chọn, không cần cho quy trình mặc định
```

Địch dùng cây tương tự trong `assets/enemies/{id}/`. Đã tạo thư mục trống
idle/attack/hit và normal/ultimate/projectile cho Gia Cát Lượng; đã có sprite sheet 8 frame trong từng clip và từng VFX của Gia Cát Lượng.

| Clip | Nhịp mặc định | Cách phát | Thiếu thì dùng |
|---|---|---|---|
| idle | 8 FPS | Lặp khi chờ | body.png → hình code |
| attack | 12 FPS | Một lần, theo timeline hành động | body/idle + động tác code |
| hit | 12 FPS | Một lần khi thật sự nhận damage | Lùi nhẹ/chớp màu cục bộ |
| ultimate | 12 FPS | Một lần | attack đã phân giải, gồm nhịp/mốc của attack |
| death | 12 FPS | Một lần, giữ frame cuối | Nghiêng/mờ bằng code |
| VFX | 12 FPS | Một lần; projectile có thể lặp khi đang bay | VFX code tương ứng |

Số frame không bắt buộc là 8 hoặc 4; tối thiểu 1. Một frame là hình tĩnh,
không được báo là đã có animation chuyển động hoàn chỉnh. Không nhân bản
frame để giả thành bộ 8 frame.

Các tên frame liên tục từ `000.png`, cùng canvas trong mỗi clip, PNG alpha.
Chuẩn tác giả: 512×512, tâm chân (256,460), nhìn sang phải; không crop từng
frame. Các clip của cùng nhân vật phải giữ cùng tỷ lệ người và điểm chân.
Không vẽ sẵn tên/HP, nền, bóng chân, vòng trận hoặc hit spark vào frame cơ thể.

Ảnh JPEG tham chiếu Gia Cát Lượng là bảng key pose, có chữ và hiệu ứng phủ
lên người; không phải sprite sheet đủ frame sạch. Cần vẽ/tái dựng các frame
trung gian. Body AI hiện tại có canvas 1254px; trước khi làm bộ animation
cần căn lại theo template, tránh nhảy vị trí khi chuyển body ↔ clip.

## 3. Tự đăng ký và cache

Mở rộng AssetCatalog bằng `GetClip(id, clipName)` và `GetEffect(id, name)`.
Chỉ quét nhóm/tên đã hỗ trợ; bỏ qua `.gitkeep`, tài liệu và ảnh tham chiếu.
Đọc PNG vật lý không cần editor import, dùng chung cache texture hiện có.

Một FrameClip chứa frame bất biến, FPS, loop, mốc sự kiện và kích thước
canvas. Frame không mang callback gameplay. Nhiều lính cùng loại chia sẻ
FrameClip/texture; trạng thái phát và thời gian là riêng từng instance.

Thiếu số frame, ảnh hỏng, lệch canvas hoặc cấu hình sai: báo đường dẫn và
dùng clip tốt trước đó/fallback; không nạp nửa bộ clip. Không tìm thấy clip
không phải lỗi và không spam log. Không load/giải mã PNG trong _Draw.

Đợt đầu nạp khi vào scene/trận. Reload sau này thay cả bộ ở ranh giới
hành động, không sửa danh sách frame đang phát. Packaged clip dùng inventory
sinh lúc build theo kế hoạch asset, không giả định PCK là thư mục System.IO.

## 4. Một đồng hồ và một nơi điều phối

Tách luồng trình diễn dài trong BattleScene.PlayNextAction thành
BattlePresentationController. Controller nhận BattleAction đã tính và giữ
snapshot hình ảnh của một hành động; không được tính lại damage hay gọi Hit.

Dùng đồng hồ trình diễn tích lũy `delta × tốc độ` làm nguồn thời gian chung.
Đổi x1/x2/x3 chỉ đổi tốc độ đi tới, không làm nhảy về đầu clip hoặc nhân
tốc độ hai lần. Frame hiện tại, vị trí actor, vị trí đạn và tiến độ VFX đều
được lấy mẫu từ đồng hồ này. Khi dừng trình diễn, tất cả thành phần cùng dừng.

BattleActor tiếp tục là Node2D vẽ texture; bộ phát FrameClip chọn texture
theo thời gian local của trạng thái. Không thêm đồng hồ tự chạy riêng cho
AnimatedSprite2D rồi cố nối bằng timer thứ hai. Nếu đổi sang AnimatedSprite2D
ở tương lai, vẫn phải chọn một bên làm chủ timeline.

Controller là nơi duy nhất đổi vị trí actor. Loại bỏ tween cũ điều khiển
cùng vị trí sau khi chuyển sang controller; không để hai hệ tranh nhau.
Frame nhân vật không tự dịch chuyển chân trong canvas để giả tiến/lùi.
Recoil của Hit là offset hình ảnh cục bộ, tách khỏi root/home anchor. Timer
cũ không được đồng thời gọi lượt mới khi controller đang giữ action; khoảng
chờ cuối cũng lấy từ đồng hồ chung.

## 5. Trạng thái và thứ tự ưu tiên

```text
Idle → Approach → Attack/Windup → Release hoặc Contact → Recover → Return → Idle
Idle/Return → Hit → Idle
Bất kỳ trạng thái còn sống → Death → giữ kết thúc
```

`Approach`/`Return` là trạng thái di chuyển, không bắt buộc có clip chạy.
Khi thiếu clip move, dùng body/idle và dịch vị trí bằng code.

Ưu tiên: Death > Hit > Attack/Ultimate > Idle. Chỉ chuyển sang Hit với mục
tiêu thật sự nhận damage; né không phát hit, hồi máu không phát hit. Đỡ đòn
có thể dùng hit giảm cường độ kèm nhãn ĐỠ. Người chết không quay lại idle.
Hành động hiện tại kết thúc cả phần phục hồi của mục tiêu trước khi mở lượt
mới, tránh actor bắt đầu attack trong lúc vẫn đang bị một hit cũ ghi đè.

HP/Alive dùng để vẽ phải thuộc snapshot trình diễn. Không đọc trực tiếp
Fighter.Alive để làm ngã người trước mốc va chạm, vì engine đã tính HP cuối
ngay khi nhận BattleAction. Commit HP/nộ/Alive hiển thị theo các mốc bên dưới.

## 6. Timeline cận chiến và đánh xa

Các thời gian sau là preset thử ở x1, sẽ điều chỉnh theo mẫu nhìn thực tế:
tiến 0,18 giây, đạn bay 0,18 giây, lùi 0,18 giây, chờ cuối 0,08 giây.
Độ dài clip = số frame/FPS; không ép mọi clip vào tween 0,22/0,42 giây cũ.

**Cận chiến:**

1. Lưu chính xác home anchor và lớp vẽ; nâng lớp khi tiến tới mục tiêu.
2. Tiến đến khoảng cách vũ khí cho phép. Vị trí đích tính từ anchor mục tiêu,
   không dùng frame cơ thể hoặc vị trí đang bị hất của mục tiêu.
3. Bắt đầu attack/ultimate. Đến mốc Contact thì áp snapshot mục tiêu,
   hiện damage/né/đỡ/bạo, phát hit/death và VFX impact đúng một lần.
4. Chờ kết thúc attack và phản ứng của mục tiêu; lùi về home, trả lớp vẽ.
5. Trở lại idle nếu còn sống. Không tích sai số qua nhiều lượt.

**Đánh xa/phép/hồi máu:**

1. Giữ người tung chiêu tại home, phát attack/ultimate tại chỗ.
2. Đến mốc Release mới sinh projectile và cast VFX. Không tạo đạn ngay từ
   lúc chuẩn bị như code hiện tại.
3. Đạn đi từ cast anchor tới hit anchor. Khi tới nơi mới áp HP hiển thị,
   số damage/heal và VFX; không áp damage tại frame phóng.
4. AoE tạo một nhánh trình diễn cho mỗi mục tiêu trong BattleAction. Đợt
   đầu đạn có cùng thời gian bay, impact đồng thời; nếu mở rộng impact lệch
   nhau thì dùng hàng rào chờ tất cả mục tiêu.
5. Mục tiêu hồi máu có thể chính là người tung chiêu; bỏ chuyến bay có
   độ dài bằng 0, vẫn phát heal VFX và không chuyển caster sang Hit.
6. Đợi actor recovery, hit/heal/death và các VFX chặn lượt hoàn tất rồi mới
   mở lượt tiếp hoặc popup. Không đợi idle/aura lặp vô hạn.

Sát thương nhiều mục tiêu được commit theo từng target từ snapshot After,
không Refresh toàn bộ scene về trạng thái cuối trước các mốc impact. Nộ
người tung chiêu được hiển thị theo sự kiện của action (tiêu nộ khi cast,
thưởng nộ đánh thường tại contact); không tự suy ra từ tên animation.

## 7. Mốc frame tùy chọn

Mặc định Contact/Release ở frame `floor(frameCount/2)`, đánh số từ 0 theo
tên PNG. Clip 8 frame có mốc mặc định ở `004.png`. Đánh xa hiểu đây là
Release; cận chiến hiểu đây là Contact. Frame đó chỉ phát sự kiện một lần,
kể cả một bước delta đi qua nhiều frame ở x3.

Không có JSON vẫn chạy với preset. Khi cần chỉnh chính xác, file tùy chọn:

```json
{
  "version": 1,
  "clips": {
    "idle": { "fps": 8 },
    "attack": { "fps": 12, "event_frame": 4 },
    "hit": { "fps": 12 }
  },
  "anchors": {
    "cast": [0.65, 0.55],
    "hit": [0.5, 0.55]
  }
}
```

Anchor là tọa độ chuẩn hóa trong canvas nhân vật; chuyển sang tọa độ thế
giới từ điểm chân và scale, rồi lật cùng hướng nhân vật. Shadow và vòng lượt
không chạy theo độ giật của sprite. Event frame ngoài clip, FPS không dương
hoặc dữ liệu không hữu hạn phải bị từ chối và dùng default có chẩn đoán.

Không cho JSON thay heroId, damage, target, RNG hay buff. Nếu ultimate mượn
attack thì dùng mốc/nhịp của clip attack đã phân giải, không áp mốc trỏ tới
frame không tồn tại của một ultimate khác.

## 8. VFX tách khỏi cơ thể

- `normal`, `ultimate`: đặt tại hit anchor của từng mục tiêu thật sự trúng.
- `projectile`: sprite có hướng gốc sang phải, game xoay theo vector bay;
  không đổi hướng bằng cách xoay ngược cả nhân vật.
- `cast`: tại cast anchor, sinh ở Release; thiếu thì bỏ qua, không dùng nhầm
  impact effect đặt lên người tung chiêu.
- `aura`: tùy chọn làm sau, đặt dưới chân, không chặn kết thúc action.
- Lớp vẽ: nền → shadow/aura → actor → projectile/impact → damage/HUD.
- Hiệu ứng trúng đòn có PNG dùng PNG; thiếu dùng motif code hiện tại. Mỗi
  event chỉ chọn một nguồn, không phát cả PNG và motif code chồng nhau.
- Gia Cát Lượng mẫu áo vàng có thể dùng VFX vàng/cam theo art; màu art không
  đổi loại kỹ năng hoặc thêm đốt máu. Vòng bát quái dưới chân trong bảng
  tham chiếu phải là aura riêng, không vẽ vào từng frame body.
- Mỗi action sở hữu các effect instance của nó; dọn khi xong/hủy/thoát scene.
  Có giới hạn số effect và thời gian tối đa để một asset lỗi không giữ trận mãi.

## 9. Thứ tự triển khai

| Bước | Công việc | Điều kiện hoàn thành |
|---|---|---|
| A — Đã làm | Loader FrameClip + kiểm tra frame + fallback | Copy 000… là catalog nhận; thiếu/hỏng không làm trận dừng |
| B — Đã làm | Idle và bộ phát frame trong BattleActor | Loop mượt, lật phe đúng, chuyển body/idle không nhảy chân |
| C — Đã làm | Controller timeline + attack/release/contact + hit/death | HP, phản ứng và đòn chạm đúng ở x1/x2/x3; không chồng lượt |
| D — Đã làm | Nối chuyển động cận chiến và projectile | Tiến đúng mục tiêu, caster đứng tại chỗ, đạn sinh ở Release, về đúng home |
| E — Đã làm | Nạp PNG VFX và cấu hình tùy chọn | Một nguồn VFX cho mỗi event, anchor đúng, không sót effect |
| F — Đã làm Gia Cát Lượng | Mẫu Gia Cát Lượng và kiểm thử render | Idle 8, attack 8, hit 4 frame sạch; xem được preview và trọn trận |

Loader và controller có thể kiểm chứng bằng fixture kỹ thuật trước khi có
art hoàn chỉnh. Fixture không đặt vào thư mục tướng thật và không trình bày
là frame đã vẽ. Giữ StepBattle là đường mô phỏng đồng bộ cho test engine.

## 10. Kiểm tra nghiệm thu

- Chỉ có body; chỉ có idle; thiếu attack/hit; ultimate mượn attack; frame
  thiếu số, sai canvas, PNG hỏng, JSON sai. Tất cả có fallback rõ ràng.
- Cận chiến, caster, hồi máu bản thân, AoE, né, đỡ, bạo, tử trận nhiều mục
  tiêu, địch trùng ID; nhãn damage chỉ xuất hiện đúng số impact thực tế.
- Đổi x1 → x3 giữa windup, giữa đạn bay và giữa hit; mỗi event đúng một lần.
- Kiểm tra chân đứng yên trên canvas, body không giật ngang khi đổi frame,
  hai phe lật đúng, không lệch home sau nhiều hành động.
- Kiểm tra tử trận không ló idle, cast không bị hit cũ ghi đè, không hiện
  popup hoặc chuyển đội Tổ Đội khi action cuối còn đang chạy.
- Thoát scene giữa tiến/đạn/hit; không còn tween/controller/callback cũ hoặc
  effect tự do. Các clip dùng chung không bị một instance sửa trạng thái.
- Cùng seed/đội/ải vẫn cùng thắng thua, HP cuối và thưởng; art không thay
  luật engine. Chạy lại test battle hiện có và thêm test timeline có bước
  thời gian lớn để bắt lỗi bỏ sót event ở FPS thấp.
- Render cửa sổ thật, chụp/video các phase và xem x1/x3; đo frame time và
  texture memory. Headless không chứng minh clip đẹp hoặc không rung chân.

## 11. Ranh giới với các kế hoạch khác

Đây là chi tiết của đợt 3 trong [kế hoạch asset tự nhận](KE_HOACH_ASSET_TU_DONG.md).
Không bắt buộc chờ runtime reload mới bắt đầu clip: đợt đầu vào trận mới sẽ
nạp bộ mới. Reload, export inventory và các nền/icon tiếp tục theo kế hoạch
đó. Hướng dẫn xuất file dùng chung ở [assets/README.md](../assets/README.md).


## 12. Kết quả triển khai 01/11/2026

- FrameClip bất biến và AssetCatalog.Clips tự đăng ký thư mục PNG liên tục,
  cache cả clip, kiểm tra canvas trước decode và giới hạn 120 frame/10 giây/
  16 triệu pixel. JSON tùy chọn hỗ trợ clips/effects FPS, event_frame và anchors.
- BattleActor phát texture theo thời gian của trạng thái, giữ pivot, lật phe;
  trạng thái sống/chết hiển thị tách khỏi Fighter để không ngã trước impact.
- BattlePresentationController thay tween cũ; BattleScene dùng một clock
  delta×speed cho frame, đạn, di chuyển và VFX, kể cả khoảng chờ. Contact/
  Release/Impact dùng kiểm tra vượt ngưỡng nên không lặp hoặc mất ở FPS thấp.
- Đã nối hit/death, ultimate mượn attack, cast/projectile/normal/ultimate PNG
  và shared hit/heal/projectile; kết thúc đợi cả recovery rồi mới mở lượt,
  popup hoặc chuyển đội. Hủy scene dọn hiệu ứng và không có callback trễ.
- TestBattleClips dùng PNG kỹ thuật trong thư mục tạm, kiểm tra cache, clip
  lỗi, marker/FPS, body vắng, windup không lộ chết, projectile, PNG impact,
  bước thời gian lớn, melee về home, dodge, AoE và self-heal. Đã chạy cả
  headless và renderer GL Compatibility; đã xem ảnh fixture release/impact.
- Đã chạy suite 60 scene: chỉ còn bốn lỗi layout 214/216/217/244 và log RID
  khi thoát 227 như baseline cũ. Test clip/asset/trận mới đều qua.
- Chưa có frame idle/attack/hit art thật cho Gia Cát Lượng, không nhân bản
  body để giả animation. Aura/victory, runtime reload và inventory clip PCK
  chưa triển khai; clip desktop hiện dùng thư mục PNG bên ngoài executable.


## 13. Bộ Gia Cát Lượng hoàn chỉnh — 02/11/2026

Đã tạo 5 clip × 8 pose và 5 VFX × 8 frame bằng image_gen từ mẫu người dùng.
Bộ dùng atlas PNG và metadata vùng UV/canvas/pivot để giữ nguyên pixel nguồn.
Hai sheet đánh/tuyệt kỹ được chỉnh khoảng cách, giới hạn vùng riêng của pose
để không mất đầu quạt hoặc hiện mảnh từ ô khác. Có aura lặp dưới chân, màu
vàng/cam đúng thiết kế; không thay chỉ số hoặc công thức kỹ năng.

Hình ở sân Hub cũng phát idle/aura và quay theo hướng di chuyển; placeholder
vẫn dùng cho tướng chưa có art. Có scene xem thử năm trạng thái, xuất PNG/GIF
bằng renderer và test bộ asset thật. Xem [README bộ đầy đủ](../assets/heroes/gia_cat_luong/README.md).

Đã hỗ trợ atlas chuẩn lưới hoặc vùng tùy chỉnh; PNG rời 000… được ưu tiên
khi tác giả muốn thay atlas bằng tranh tự vẽ. Victory, runtime reload và
inventory PCK vẫn không thuộc bộ hoàn thiện lần này.
