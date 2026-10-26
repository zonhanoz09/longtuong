# Kế hoạch develop lại game tương tự Long Tướng (龙将 / long.hly.com) bằng AI

Ngày phân tích: 2026-10-26. Nguồn kiểm chứng trực tiếp: repo `github.com/zonhanoz09/longtuong`
(commit mới nhất lúc đọc: `0c01eb8 first commit`, trước đó `b031c37 add skill agent`)
và trang chủ `https://long.hly.com/` (đọc ngày 2026-10-26).

> Cập nhật 2026-10-26 (theo chốt của Huynh): stack đổi từ GDScript sang
> **Godot 4.7+ .NET (C#)**. Các mục §5, §6 dưới đây đã sửa theo chốt này;
> bản đầu dùng GDScript chỉ vì skill `godot-master` trong repo viết cho GDScript.
>
> Cập nhật 2026-10-26: **Phase 1 đã hoàn thành** — 6 tướng, engine 3v3 có seed,
> chọn đội/ải, sao + thưởng, test Phase 0 + Phase 1 headless pass (xem README).
>
> Cập nhật 2026-10-26: **Phase 2 đã hoàn thành** — cấp/EXP/sao, trang bị
> cường hóa/tẩy luyện, Tướng Tinh 12 ô, chiêu mộ công khai tỉ lệ, thưởng trận
> có EXP/mảnh/Kim Nguyên Bảo, test Phase 2 headless pass (xem README).
>
> Cập nhật 2026-10-26: **Phase 2.1 đã hoàn thành** — đăng nhập/đăng ký local
> (hash mật khẩu) -> Hub 7 widget (tướng sở hữu, túi đồ, trang bị, bày trận,
> vào ải, cường hóa, chiêu mộ), save tách theo tài khoản local.
> Cập nhật 2026-10-26: **Phase 2.3 — ĐÃ HOÀN THÀNH** — Các widget thành popup
> trên Hub (khung riêng, kích thước khác nhau từng widget, nút X + Quay lại
> đóng popup, Hub ở lại phía sau). Xem §6.3.
>
> Cập nhật 2026-10-26: **Phase 2.2 — ĐÃ HOÀN THÀNH** — UI/UX thống nhất cho
> toàn bộ widget + Hub dạng thẻ, và seed data tài khoản mới: 1 tướng khởi đầu
> suy ra từ tên đăng nhập + rất nhiều Vàng để test chức năng (xem §6.2 dưới đây).

> Nguyên tắc bản quyền, đọc trước khi code: làm game **tương tự về thể loại và cơ chế**,
> KHÔNG copy code, art, nhạc, text, tên skill/item riêng của game gốc. Nhân vật Tam Quốc
> là nhân vật lịch sử / tiểu thuyết công cộng, nhưng tạo hình, tên hệ thống riêng,
> số liệu và nội dung của bản gốc thì phải tự thiết kế lại. Không rip client Flash cũ,
> không dùng asset không rõ nguồn.

## 1. Kết luận nhanh — 3 sự thật quan trọng

1. **Repo `longtuong` hiện chưa có game.** Ngoài `README.md` (1 dòng `# longtuong`)
   và `skills-lock.json`, toàn bộ 1.738 file còn lại chỉ là skill cho AI agent:
   - `.agents/skills/godot-master/` — thư viện Godot 4.7+ (92 domain skill, 1.273 script
     `.gd` mẫu, tài liệu tham khảo), nguồn `thedivergentai/Game-Dev-Agentic-Skills`.
   - `skills/system/run-workflow/SKILL.md` — cách chạy workflow nhiều agent song song.
   - Không có `project.godot`, không có scene, script game, data, asset nào.
   - Kết luận: đây là repo **chuẩn bị để AI develop**, chưa phải source game gốc.
2. **Game gốc trên long.hly.com đã ngừng vận hành.** Trang chủ hiển thị rõ
   "游戏已停服！" (game đã ngừng phục vụ) ở mục server mới nhất và nút vào game.
   Vì vậy không chơi thử live được; phân tích dưới đây dựa trên mô tả + danh mục
   hệ thống trên trang chủ, không phải gameplay đo trực tiếp trong game.
3. **Đừng remake 1:1 toàn bộ.** Game gốc là webgame vận hành nhiều năm, chồng
   hơn 20 hệ thống lên nhau (xem §3). Remake khả thi = lấy **lõi 20%** làm nên
   cảm giác game, còn lại đưa vào phase sau theo dữ liệu người chơi thật.

## 2. Game gốc là gì?

- Tên: 龙将 (Long Tướng), nhà phát hành Hoan Lạc Viên (欢乐园, Xiamen).
- Định vị trên trang chủ: "以三国题材为背景的大型横版RPG网页游戏" — game RPG
  **màn hình ngang (side-view), đề tài Tam Quốc**, nhấn mạnh "武将系统" (hệ thống võ tướng)
  và "将星玩法" (chơi Tướng Tinh) đi kèm cốt truyện Tam Quốc.
- Thể loại thực tế (suy luận từ danh mục hệ thống, cần xác nhận bằng tư liệu lưu trữ
  nếu muốn bám sát): RPG sưu tầm tướng, đánh theo lượt/tự động theo đội hình,
  leo ải theo cốt truyện, nuôi tướng + trang bị + nhiều hệ thống tăng lực chiến chồng lớp,
  hoạt động PvE/PvP theo giờ và theo mùa, kinh tế free-to-play nặng về nạp.
- Dấu hiệu của webgame đời cũ đã qua đỉnh: tiêu đề trang chủ còn nhắc "新服 / 鬼服 /
  村服" (server mới / server ma / server làng) — tức từng phải gộp/mở server liên tục,
  và cuối vòng đời thì dừng vận hành.

## 3. Phân tích hệ thống (từ danh mục trên trang chủ — đã kiểm chứng tên hệ thống)

### 3.1 Vòng lặp lõi (core loop)

```
Vào game → nhận nhiệm vụ chính / leo ải → đánh trận → nhận EXP, Bạc, trang bị, mảnh tướng
→ nâng cấp tướng / trang bị / Tướng Tinh → lực chiến tăng → mở ải, hoạt động, tính năng mới
→ quay lại leo ải / đấu trường / hoạt động ngày → (nạp: VIP, gói, sự kiện tích nạp)
```

### 3.2 Nhóm hệ thống đã xác nhận tên trên trang chủ

**A. Tiến trình & tân thủ**
- 关卡系统 (hệ thống ải), 新手引导 (hướng dẫn tân thủ), 新手FAQ
- 等级礼包 (gói quà theo cấp), VIP特权详解 (đặc quyền VIP)

**B. Tướng — trái tim của game**
- 武将系统 (hệ thống võ tướng), 将星玩法 (Tướng Tinh — bảng sao tăng chỉ số)
- 军师星图 (Tinh đồ quân sư), 武圣图 (Võ Thánh đồ)
- 100级金将 / 110级金将 (tướng Vàng cấp 100 / 110 — tầng phẩm chất cuối game)
- 隐武所 (Ẩn Võ Sở — nơi chiêu mộ / ẩn tướng), 敬酒与橙魂 (Kính rượu & Hồn Cam)
- Ví dụ tướng showcase: 神威马超 (Thần Uy Mã Siêu)

**C. Nuôi & build**
- 装备洗炼 (tẩy luyện trang bị), 符文系统 (hệ thống Phù Văn)
- 兵法 (Binh Pháp), 100级神铸苍穹套装 (bộ Thần Chú Thương Khung cấp 100)
- 兽灵系统 (Thú Linh — pet/linh thú hỗ trợ)

**D. Hoạt động PvE**
- 过关斩将 (Qua Quan Trảm Tướng — leo tháp/ải liên tiếp)
- 怪物攻城 (Quái vật công thành — event thủ thành)
- 血魂炼狱副本及血魂系统 (phó bản Huyết Hồn Luyện Ngục + hệ thống Huyết Hồn)
- 水月之境 (Thủy Nguyệt Chi Cảnh), 魔物迷宫 (được nhắc ở mục hoạt động)

**E. PvP / liên server / chiến lược**
- 跨服战比赛规则说明 (quy tắc thi đấu liên server)
- 烽火割据玩法 (Phong Hỏa Cát Cứ — chiếm đất / thế lực)
- 运筹帷幄玩法 (Vận Trù Duy Ác — chơi chiến lược/bàn cờ thế lực)

**F. Kinh tế & sự kiện**
- 摇钱树 (Cây Rung Tiền — đổi tài nguyên lấy Bạc), 真龙宝库 (Chân Long Bảo Khố)
- 充值中心 (trung tâm nạp), 活动专区 (sự kiện: 月末累充 — tích nạp cuối tháng, 新服充值活动)
- 随机抽取概率公示 (công khai xác suất rút ngẫu nhiên — tức có gacha/quay)

### 3.3 Điểm mạnh nên học (không copy)

- Một lõi rõ ràng: **sưu tầm tướng Tam Quốc + leo ải + tăng lực chiến** — dễ hiểu
  trong 5 phút đầu, nhưng có chiều sâu build ở cuối game.
- Nhiều "đường tăng trưởng" song song (tướng, sao, phù văn, binh pháp, thú linh,
  trang bị) nên người chơi lúc nào cũng có việc để làm, không bị kẹt một đường.
- Hoạt động theo lịch (công thành, liên server, cát cứ) tạo lý do quay lại hằng ngày,
  và tạo cộng đồng/server identity.
- Công khai xác suất quay — chuẩn tối thiểu nếu remake có gacha.

### 3.4 Điểm yếu / bài học từ cái chết của game gốc

- **Scope phình vô hạn:** mỗi năm thêm một hệ thống chồng chỉ số → người mới không
  đuổi kịp, người cũ chán vì "chỉ số lạm phát", cân bằng vỡ.
- **Pay-to-progress quá nặng:** VIP + tích nạp + nhiều tầng tướng Vàng khiến
  free player cảm thấy tường paywall, server nhanh thành "server ma".
- **Phụ thuộc nền tảng cũ:** webgame Flash-era, không chuyển kịp sang mobile/HTML5
  thì mất người chơi dù nội dung còn tốt.
- Bài học cho remake: khóa scope phase 1 thật chặt, cân bằng free-vs-pay ngay từ
  số liệu, chọn nền tảng còn sống (Godot export HTML5/mobile/PC).

## 4. Định hướng remake: "Long Tướng-like" nhưng là game của mình

- **Tên & bối cảnh:** đặt tên mới (ví dụ `Long Tướng` là tên gốc — không dùng lại
  làm tên thương mại). Cốt truyện Tam Quốc viết lại theo mạch riêng: khởi đầu
  Hoàng Cân → Đổng Trác → Tam phân, mỗi chương 8–12 ải.
- **Góc nhìn:** giữ đúng chất gốc — màn hình ngang 2D, đội hình đối đầu 2 bên,
  đánh tự động theo lượt, người chơi can thiệp bằng: xếp đội hình, chọn thời điểm
  tung tuyệt kỹ, dùng vật phẩm. (Không làm ARPG điều khiển tay — đó là game khác.)
- **Đội hình:** bắt đầu 3 tướng, mở rộng tối đa 5 (chủ tướng + 4). Vai trò:
  Tiên phong (tank), Võ (dame vật lý), Mưu (dame phép/khống chế), Phụ (hồi/buff).
  Khắc chế đơn giản: Võ > Mưu > Phụ > Võ, Tiên phong không khắc ai nhưng chặn đòn.
- **Lõi khác biệt so với tamquoc3d đã có:** tamquoc3d là 3x3 đánh theo lượt có
  chọn skill/đích thủ công; bản này là **side-view auto-battle + can thiệp thời điểm**,
  nặng về sưu tầm/nuôi tướng và hoạt động ngày — gần cảm giác webgame gốc hơn.
- **Chơi đơn trước, online sau** (đúng cam kết đã chốt với Huynh từ trước):
  Phase 1–3 offline/local-save; PvP chỉ làm dạng đấu ảnh (async arena) ở Phase 4,
  realtime/guild để Phase 5 trở đi, nếu có.

## 5. Stack kỹ thuật đề xuất (tận dụng đúng repo hiện có)

| Hạng mục | Chọn | Vì sao |
|---|---|---|
| Engine | Godot 4.7+ .NET (C#) | Chốt của Huynh 2026-10-26; cùng họ stack với tamquoc3d (Godot Mono + C#) nên tái dùng được kinh nghiệm build/test/export .NET, và sau này nối server ASP.NET Core thuận hơn GDScript |
| Ngôn ngữ | C# (.NET), không dùng GDScript cho code game | Code game, combat, data đều viết C#; skill `godot-master` trong repo viết cho GDScript nên chỉ lấy kiến trúc/pattern/checklist của nó rồi dịch sang C#, không copy code GDScript mẫu nguyên xi |
| Toolchain | Godot bản .NET + .NET SDK tương thích, export templates bản .NET | Bắt buộc: bản Godot thường và templates thường **không nạp được C#** (bài học đã gặp ở tamquoc3d 4.3 Mono). VM hiện chỉ có Godot 4.3 Mono + .NET SDK 8.0.426, nên Phase 0 phải cài/xác nhận Godot 4.7+ .NET và SDK tương thích trước khi code |
| Kiến trúc | Data-driven Resources + signal-up/call-down | Theo đúng "Layer Cake" của godot-master: Data (Resource) → Logic → Presentation; UI không sửa data trực tiếp |
| Dữ liệu | `Resource` (.tres) + bảng cân bằng CSV/JSON | Tướng, skill, ải, vật phẩm đều là data; AI sửa data không cần sửa code combat |
| Combat | Scene-scoped BattleManager (C#), RNG có seed | Deterministic để test lại trận, chống "kết quả khác mỗi lần chạy test" |
| Lưu game | Save local JSON (phase 1–3), server sau | Tránh làm server ngay khi lõi chưa vui |
| AI workflow | godot-master skill chain + run-workflow fan-out | Mỗi phase chia agent: combat, data, UI, test — rồi gộp, review bằng headless test |
| CI/test | Build `dotnet` + Godot headless test cho project .NET | Như tamquoc3d đã làm (build 0 lỗi + test scene headless pass mới merge); CI phải dùng đúng Godot bản .NET, không dùng bản thường |

Lưu ý kỹ thuật từ kinh nghiệm tamquoc3d: nếu sau này thêm server, **thưởng phải do
server tính, không tin số client gửi** (lỗi P0 từng gặp ở tamquoc3d), và mọi
endpoint xem dữ liệu phải bắt auth.

## 6. Kế hoạch theo phase (mỗi phase = chơi được, test được, mới sang phase sau)

### Phase 0 — Dựng khung (1 tuần)
- Cài/xác nhận toolchain trước: Godot 4.7+ bản .NET + .NET SDK tương thích
  (theo release notes của đúng bản Godot đó) + export templates bản .NET.
- Tạo `project.godot` Godot 4.7+ .NET (file `.csproj`, code C#), cấu trúc `core/`
  (data, combat, save), `features/`, `assets/` (placeholder hình chữ nhật + màu,
  chưa cần art đẹp), `docs/`.
- Xong khi: `dotnet build` 0 lỗi, mở project chạy được màn hình chính + 1 trận
  placeholder, CI headless (bản .NET) pass.
- **Không làm:** art final, âm thanh, server, gacha, code GDScript.

### Phase 1 — Vertical slice: trận đánh vui được (2 tuần)
- 6 tướng khởi đầu (ví dụ: Quan Vũ, Trương Phi, Triệu Vân, Gia Cát Lượng, Điêu Thuyền,
  Lữ Bố — tạo hình tự vẽ/AI-gen riêng), mỗi tướng 1 đánh thường + 1 tuyệt kỹ + 1 bị động.
- Combat: đội hình 3v3, đánh tự động theo tốc độ, thanh nộ đầy → người chơi bấm tung
  tuyệt kỹ (hoặc auto nếu bật), crit, né, khắc hệ, thắng/thua, sao ải (1–3 sao theo
  HP còn lại / số lượt).
- 1 chương 10 ải, độ khó tăng dần, save/load local.
- **Tiêu chí qua:** người chơi mới hiểu luật trong 3 trận đầu không cần hướng dẫn dài;
  cùng một seed cho cùng một kết quả; 10/10 test combat pass.

### Phase 2 — Nuôi tướng & trang bị (2–3 tuần)
- Tướng: EXP → cấp (tối đa 60 ở phase này), tăng sao bằng mảnh (tối đa 5 sao),
  chỉ số scale theo cấp+sao bằng công thức công khai trong data.
- Trang bị 4 món (vũ khí, giáp, mũ, giày) + cường hóa + tẩy luyện 1 dòng (bản rút gọn
  của 装备洗炼 gốc).
- Tướng Tinh (将星) bản rút gọn: bảng 12 ô sao, mở bằng điểm ải, tăng chỉ số toàn đội.
- Chiêu mộ: quay bằng Bạc/Kim Nguyên Bảo kiếm trong game (chưa nạp tiền thật),
  công khai tỉ lệ ngay trong UI quay.
- **Tiêu chí qua:** leo lại ải cũ thấy mạnh lên rõ rệt; không có đường build nào
  "vô dụng hoàn toàn" hay "một mình cân hết".


### Phase 2.2 — UI/UX toàn bộ widget + Hub + seed data khởi đầu

**Vấn đề trước 2.2:** mỗi màn hình tự vẽ kiểu riêng (nút đủ cỡ, không có header
chung, không thấy Vàng/KNB khi đang ở màn con, Hub là danh sách nút dài), và tài
khoản mới có sẵn cả 6 tướng + 1.000 Vàng nên không test được vòng lặp
khởi đầu → chiêu mộ → mở khóa tướng.

**2.2.1. Hệ thống UI/UX chung (`core/ui/UiTheme.cs`)**
- Khung 1280×720, nền tối, chữ sáng, điểm nhấn Vàng đồng (Tam Quốc) + Đỏ cho địch.
- Quy tắc mọi màn hình: header (tiêu đề + thanh tiền tệ Vàng/KNB ở góc phải),
  nội dung chính, nút Quay lại Hub ở cuối. Nút chính cao tối thiểu 56px, nút bị
  khóa hiển thị xám + lý do (chưa mở ải, chưa sở hữu tướng, chưa đủ tiền).
- Không dùng sprite final ở phase này: tướng vẫn là khối màu + tên, nhưng có
  nhãn cấp/sao/HP/ATK thống nhất, để Phase art sau chỉ thay khối màu bằng sprite.

**2.2.2. Hub redesign (màn hình trung tâm)**
- Header: tên tài khoản + tướng đại diện (tướng khởi đầu) + Vàng/KNB + thắng/ải.
- 7 widget thành thẻ 2 cột (mỗi thẻ: tên + mô tả 1 dòng + trạng thái, ví dụ
  "Túi đồ — 12.340 Vàng, 3 tướng sở hữu"), cộng thẻ Đăng xuất. Bấm thẻ → đúng màn.
- Hub là điểm quay về duy nhất: mọi màn con đều có nút về Hub (trừ trận → về chọn ải).

**2.2.3. Seed data tài khoản mới (chỉ áp cho đăng ký mới từ 2.2)**
- Tướng khởi đầu suy ra **xác định** từ tên đăng nhập: `index = (tổng mã ký tự
  của tên + 31 × độ dài tên) % 6` theo thứ tự 6 tướng trong data. Cùng tên luôn
  ra cùng tướng, tên khác có thể trùng tướng (chỉ có 6 tướng).
- Tài khoản mới: Vàng **100.000**, Kim Nguyên Bảo **10.000**, chỉ sở hữu 1 tướng
  khởi đầu đó, đội hình ban đầu = [tướng khởi đầu]. Số Vàng/KNB này là **seed
  test, không phải cân bằng kinh tế thật** — Phase 5 mới cân bằng lại.
- Tài khoản cũ (đã có save trước 2.2): giữ nguyên sở hữu cả 6 tướng, không bị
  thu hồi, không nhận seed 100.000 (tránh lạm phát save cũ).
- Mở khóa tướng: tướng chưa sở hữu hiển thị khóa; khi đủ **10 mảnh** tướng đó
  (chiêu mộ hoặc thưởng trận) sẽ tự mở khóa và trừ 10 mảnh. Đội hình và chọn ải
  từ 2.2 cho phép **1–3 tướng** (trước đây bắt đúng 3) để tài khoản 1 tướng vẫn
  vào ải được ngay, rồi chiêu mộ mở thêm tướng.

**2.2.4. Tiêu chí qua**
- Đăng ký tên mới → Hub hiện đúng tướng khởi đầu theo công thức + 100.000 Vàng.
- Mọi màn hình đều có header + thanh tiền tệ + nút về Hub, nút khóa có lý do.
- Tài khoản 1 tướng vào ải được, chiêu mộ đủ 10 mảnh thì mở khóa tướng mới.
- Test headless Phase 2.2 pass, các test cũ vẫn pass.


### Phase 2.3 — Widget thành popup (kích thước riêng từng widget)

- Hub ở lại phía sau: bấm thẻ widget mở popup trên nền mờ (dim) + khung viền
  Vàng + nút X góc phải. Nút Quay lại trong widget cũng đóng popup (không
  chuyển scene), Hub tự refresh trạng thái (Vàng/KNB, đội hình) khi đóng popup.
- Kích thước popup theo nội dung, không đồng đều:
  Chiêu mộ 700×360 (nhỏ nhất) | Bày trận 900×540 | Túi đồ 1020×640 |
  Danh sách tướng 1100×620 | Trang bị/Cường hóa 1140×680 |
  Vào ải 1180×680 (lớn nhất, 2 cột tướng + ải).
- Widget mở từ widget khác vẫn là popup: từ Danh sách tướng bấm Xem chi tiết
  mở popup Trang bị đè lên (đóng popup cũ trước). Trận đấu giữ nguyên màn
  hình riêng, không phải popup — vào trận từ popup Vào ải sẽ rời Hub, về lại
  màn chọn ải rồi về Hub như cũ.
- Trong popup, widget không vẽ thêm thanh tiền tệ riêng (Hub đã hiện sau lớp
  mờ) và layout đã thu chiều rộng cho vừa khung, có cắt tràn (clip) trong khung.


> Cập nhật 2026-10-26: **Phase 2.5 — ĐÃ HOÀN THÀNH** — Giao diện Danh sách
> tướng theo ảnh mẫu (danh sách trái + chi tiết giữa + trang bị phải), tướng
> mặc định không có trang bị, tháo/mang bằng click đúp. Xem §6.5 dưới đây.

### Phase 2.4 — Bỏ nút Quay lại + Danh sách tướng dạng tab

- Bỏ nút Quay lại Hub ở tất cả widget (Danh sách tướng, Túi đồ, Trang bị,
  Cường hóa, Bày trận, Vào ải, Chiêu mộ) vì popup đã có nút X để đóng.
  Trận đấu không phải widget nên giữ nút thoát, nhưng nay về thẳng Hub
  (trước đây về màn chọn ải — màn đó giờ chỉ là popup, không có nút quay lại).
- Widget Danh sách tướng làm lại dạng tab: 1 tab = 1 tướng (6 tab), mỗi tab
  hiện chi tiết đầy đủ: cấp/EXP, sao, mảnh, chỉ số hiệu lực, đánh thường/
  tuyệt kỹ/bị động, và 4 trang bị mang theo (tên món + cấp cường hóa +
  dòng tẩy luyện). Tướng chưa sở hữu có tab riêng ghi "Khóa" + số mảnh hiện có.
  Tướng đã sở hữu có nút trong tab để mở popup Trang bị/Cường hóa của tướng đó.


### Phase 2.6 — Trang bị: trái là danh sách sở hữu, hover mới hiện chỉ số, layout fix cứng (ĐÃ HOÀN THÀNH)

- Màn Danh sách tướng làm lại: trái (260×540, cuộn được) là DANH SÁCH TRANG BỊ
  USER ĐANG SỞ HỮU — gồm cả món trong túi lẫn món đang mang trên mọi tướng
  (ghi rõ "Trong túi" hay "Đang mang: <tên tướng>"); giữa (500×580) là chi tiết
  tướng đang chọn (chọn tướng bằng 2 hàng nút × 3, chân dung, 4 ô đang mang,
  Tháo tất cả, EXP, kỹ năng); phải (340×580) là chỉ số tướng + khung tooltip
  cố định 320×200. Popup vẫn 1180×660.
- KHÔNG hiển thị chỉ số trên trang bị: nút món đồ chỉ ghi tên ô + cấp (VD
  "Vũ khí · Lv3 — Trong túi"), nút ô đang mang chỉ ghi tên ô + "Đang mang" /
  "(Trống)". Hover (MouseEntered) vào bất kỳ món đồ nào mới hiện chi tiết đầy
  đủ trong khung tooltip: chỉ số cộng theo cấp, dòng tẩy luyện, trạng thái.
  Click đơn chỉ chọn/tô sáng, không tháo/mang.
- Mang được cho TẤT CẢ các tướng: click đúp món trong túi → mang cho tướng
  đang chọn (kể cả tướng chưa sở hữu), click đúp món đang mang (ở danh sách
  trái hoặc ở ô giữa) → tháo về túi; hoán đổi khi ô đã có món, không mất món.
- Sửa gốc layout vỡ: label có autowrap trong VBoxContainer từng bị tính chiều
  cao theo chiều rộng 1px khiến cột phình tới ~5157px — nay mọi label đều ép
  Fill + chiều rộng đúng cột (480/320/240), cột nào cũng clip_contents, và
  nút chọn tướng dùng 2 hàng × 3 vì 6 tên đầy đủ không vừa 1 hàng 500px.
- Màn Trang bị: dòng trang bị chỉ còn tên ô + cấp + tiền cường hóa (bỏ dòng
  tẩy luyện), hover vào hàng mới hiện chi tiết; Túi đồ chỉ ghi tên ô + cấp.
- Test: `tests/TestPhase26.tscn` (18 check: tooltip hover, không chỉ số trên
  nút, kích thước fix cứng, mang cho tướng khóa, danh sách sở hữu vẫn đủ món
  sau khi mang).

### Phase 2.5 — Giao diện tướng kiểu ảnh mẫu + tháo/mang trang bị (ĐÃ HOÀN THÀNH)

**Nguồn tham khảo:** ảnh giao diện Võ tướng game gốc mà Huynh gửi (chỉ lấy
bố cục và luồng, KHÔNG copy art, text, số liệu của game gốc — toàn bộ nhãn,
tướng, chỉ số trong bản của mình vẫn là tiếng Việt + số liệu của mình).
Ảnh đo được (tọa độ chuẩn hóa 0–1000, đã đo bằng công cụ định vị, không ước):
danh sách tướng trái x22–148 / y130–968; header cấp x172–601 / y124–175;
chân dung giữa x262–502 / y158–642; nút Tháo tất cả dưới chân dung
x363–492 / y642–683; bảng chỉ số chi tiết x604–792 / y224–745;
lưới trang bị phải x795–972 / y188–938; nút X góc phải trên x947–987 / y8–48.
Nút X của ảnh tương ứng với nút X của popup Hub đã có — không vẽ thêm nút X
trong widget.

**2.5.1. Bố cục mới cho widget Danh sách tướng (thay layout tab của 2.4)**
- Layout tab ở 2.4 bị thay bằng layout 3 cột theo ảnh, vì ảnh dùng danh sách
  + chi tiết chứ không dùng tab. Popup Danh sách tướng tăng từ 1100×620 lên
  **1180×660** (rộng bằng popup Vào ải nhưng thấp hơn 20px để kích thước popup mỗi widget vẫn khác nhau) để đủ 3 cột.
- Cột trái (~160px): danh sách 6 tướng dạng nút dọc, tướng đang chọn tô sáng,
  tướng chưa sở hữu xám + ghi "(Khóa)". Dưới danh sách ghi số tướng sở hữu
  (vd 1/6). Bấm tên tướng đổi chi tiết ở giữa, không chuyển màn hình.
- Cột giữa (~500px): header "Cấp X — Loại: <Vai trò>" (map với 等级/类型 của
  ảnh); chân dung là khối màu + tên tướng (chưa có sprite final, giữ nguyên
  quy ước placeholder từ các phase trước); 4 ô trang bị đang mang vây quanh
  chân dung (Vũ khí, Giáp, Mũ, Giày — game mình có 4 ô, ảnh gốc có 6 ô nên
  không copy đủ 6); dưới chân dung nút **"Tháo tất cả trang bị"** (map với
  全部卸装); thanh EXP dạng nhãn "EXP x/y" (có thể thêm ProgressBar nếu gọn);
  lưới chỉ số 2 cột; dòng kỹ năng: Đánh thường / Tuyệt kỹ / Bị động
  (map với 技能/觉醒技能/天赋 của ảnh).
- Cột phải (~440px): trên cùng là chỉ số chi tiết của tướng đang chọn;
  dưới là lưới **"Trang bị có thể dùng"** (map với 能使用的装备): các món trong
  túi, mỗi ô ghi tên ô + cấp cường hóa + dòng tẩy luyện, **click đúp vào
  là mang cho tướng đang chọn** (theo luật click đúp ở §2.5.2). Nút 解雇 (sa thải) và 回收 (thu hồi) trong ảnh và nút
  一键互换 (đổi nhanh) ở cuối danh sách trái KHÔNG làm trong 2.5 (xem §2.5.5).
- Chỉ số hiển thị: chỉ hiện chỉ số engine mình thật sự có (HP/ATK/DEF/Tốc/
  Bạo/Né + Lực chiến ước tính), KHÔNG bịa thêm 11 chỉ số như ảnh gốc
  (vật công/vật phòng/sách lược phòng/ngăn đỡ/hợp kích/viện trợ...). Số liệu
  trong ảnh là số cuối game (hàng triệu) — không dùng làm cân bằng của mình.

**2.5.2. Quy tắc trang bị mới: mặc định không có trang bị, tháo/mang tự do**
- Hiện tại mỗi tướng luôn có sẵn 4 món cấp 0 (kể cả chưa cường hóa vẫn cộng
  chỉ số gốc của món). Từ 2.5, **ô trang bị có trạng thái Rỗng**: tướng mặc
  định (tài khoản mới) có cả 4 ô Rỗng, chỉ số hiệu lực = chỉ số gốc đã scale
  theo cấp/sao, không cộng gì thêm.
- Tháo 1 món: món đó chuyển vào túi, ô trở thành Rỗng, chỉ số tướng giảm ngay.
- Tháo tất cả: cả 4 món đang mang chuyển vào túi.
- Mang từ túi: chọn món trong lưới túi → mang vào ô cùng loại của tướng đang
  chọn. Nếu ô đã có món, món cũ tự trả về túi (hoán đổi, không mất món).
- Không tốn Vàng khi tháo/mang trong 2.5 (ảnh cũng không thể hiện phí cho
  thao tác này). Cường hóa/tẩy luyện giữ nguyên phí cũ, nhưng chỉ làm được
  khi ô đang có món — ô Rỗng thì nút cường hóa/tẩy luyện bị khóa + báo
  "Chưa mang trang bị".
- **Tương tác chốt: click đúp (double-click).** Click đúp vào món đang mang
  (ô quanh chân dung ở cột giữa) → tháo món đó về túi. Click đúp vào món
  trong lưới túi (cột phải) → mang món đó cho tướng đang chọn (ô đã có món
  thì hoán đổi như trên). Click đơn chỉ chọn/tô sáng món và hiện chi tiết,
  KHÔNG tháo cũng KHÔNG mang — để tránh bấm nhầm 1 lần đã đổi đồ.
  Nút "Tháo tất cả" vẫn là nút bấm đơn như thường (không phải món đồ nên
  không áp luật click đúp); trong màn Trang bị, nút Tháo từng dòng vẫn giữ
  làm phương án dự phòng cho người không dùng chuột.
- Lưu ý kỹ thuật khi làm: đừng gắn tháo/mang vào `Button.Pressed` của ô đồ,
  vì Pressed nổ theo từng lần click (click đúp sẽ thành 2 lần bấm đơn).
  Ô đồ phải là control bắt `GuiInput` và chỉ hành động khi
  `InputEventMouseButton.DoubleClick == true`; click đơn chỉ đổi trạng thái
  chọn. Test phải mô phỏng đúng sự kiện click đúp này, không gọi thẳng
  hàm service rồi kết luận UI đã đúng.
- Tướng không trang bị vẫn vào ải/bày trận được bình thường (chỉ yếu hơn),
  không chặn luồng chơi. Chỉ tướng đã sở hữu mới được tháo/mang; tướng khóa
  chỉ xem chi tiết, các nút trang bị bị khóa.

**2.5.3. Thay đổi dữ liệu và code (khi làm)**
- `EquipmentState` thêm cờ `IsEmpty` (mặc định false để save cũ không bị đổi:
  món đã có trong save cũ vẫn tính là đang mang). `SaveData` thêm
  `BagEquipment: List<BagEquipmentItem>` với mỗi món gồm `SlotId, Level,
  RefineIndex` (món trong túi giữ nguyên cấp cường hóa + dòng tẩy luyện).
- `ProgressionService.GetEffectiveHero`: bỏ qua hoàn toàn ô `IsEmpty` (không
  cộng chỉ số gốc, không cộng theo cấp, không áp dòng tẩy luyện của ô đó).
  `TryEnhance`/`TryRefine` trả về false khi ô Rỗng.
- Mới: `EquipmentService` (hoặc method trong ProgressionService) gồm
  `EquipFromBag(save, heroId, bagIndex)`, `Unequip(save, heroId, slotId)`,
  `UnequipAll(save, heroId) -> số món đã tháo`. Mọi màn hình dùng chung
  service này, không tự sửa `HeroEquipment`/`BagEquipment` lẻ tẻ.
- `SeedService.CreateSeededSave`: tướng khởi đầu có 4 ô Rỗng + túi khởi đầu
  có **4 món cơ bản** (mỗi loại 1 món, cấp 0, dòng tẩy luyện mặc định) để
  người chơi test được ngay luồng mang → tháo → mang lại cho tướng khác.
  Số này là seed test, không phải cân bằng rơi đồ thật.
- `EnsureDefaults` + migration: save cũ (chưa có `IsEmpty`/`BagEquipment`)
  giữ nguyên món đang mang, `BagEquipment` khởi tạo rỗng; ô nào thiếu trong
  save cũ thì tạo mới ở trạng thái Rỗng (thay vì món cấp 0 như trước).
- Các màn hình chạm tới: `HeroListScene` làm lại 3 cột (chính); `HeroScreen`
  thêm nút Tháo từng dòng + Tháo tất cả và hiển thị "Trống" cho ô Rỗng;
  `BagScene` thêm mục liệt kê trang bị trong túi (ô/cấp/dòng) + ghi chú
  "Mang trang bị trong màn Danh sách tướng"; `HubScene` đổi
  `HeroListPopupSize` thành 1180×680.

**2.5.4. Tiêu chí qua (test headless `TestPhase25`)**
- Tướng 4 ô Rỗng có chỉ số hiệu lực đúng bằng chỉ số gốc đã scale (không cộng
  trang bị); mang 1 món từ túi thì chỉ số tăng đúng phần của món đó.
- Tháo 1 món → món vào túi, ô Rỗng, chỉ số giảm; mang lại → chỉ số hồi như cũ.
- Mang món mới vào ô đã có món → món cũ về túi, không mất món, tổng số món
  (đang mang + trong túi) không đổi; Tháo tất cả → 4 ô Rỗng, túi tăng đúng 4.
- Cường hóa/tẩy luyện ô Rỗng thất bại; save mới (seed) có tướng khởi đầu
  0 món đang mang + đúng 4 món trong túi; save cũ vẫn giữ nguyên món đang mang.
- Mở popup Danh sách tướng: có đủ 3 cột (danh sách trái, chi tiết giữa,
  lưới túi phải), không còn TabContainer của 2.4, không có nút Quay lại.
- Tương tác click đúp: mô phỏng sự kiện click đúp lên món đang mang → tháo
  về túi; click đúp lên món trong túi → mang cho tướng (hoán đổi nếu ô đã
  có món); click đơn lên cùng các ô đó → không đổi trạng thái mang/tháo,
  chỉ đổi ô đang chọn.

**2.5.5. Ngoài phạm vi 2.5 (làm sau, không lén đưa vào)**
- Sa thải tướng (解雇), thu hồi (回收), đổi nhanh 1 chạm (一键互换): ảnh hưởng
  sở hữu/đội hình, cần thiết kế riêng.
- Rơi trang bị từ ải, shop trang bị, độ hiếm/phẩm chất trang bị, tab Túi đồ
  đầy đủ trong màn tướng (túi đồ vẫn là widget riêng như hiện tại).
- Sprite chân dung final (chân dung vẫn là khối màu + tên như các phase trước).
- Thêm chỉ số mới kiểu ảnh gốc (chỉ làm khi engine combat thật sự dùng chúng).

### Phase 3 — Hoạt động ngày & chiều sâu (3 tuần)
- Qua Quan Trảm Tướng (leo tầng liên tiếp, càng sâu thưởng càng lớn, reset ngày).
- Quái Công Thành (boss theo giờ, đánh chung lấy mốc thưởng — phase này làm boss
  solo theo mốc, chưa cần đồng bộ nhiều người).
- Cây Rung Tiền + Bảo Khố (đổi thể lực/vàng, nhận thưởng treo máy có giới hạn).
- Thú Linh 1 con khởi đầu (buff toàn đội, nâng cấp bằng nguyên liệu ải).
- Nhiệm vụ chính theo chương + nhiệm vụ ngày (nhẹ, không ép nạp).
- **Tiêu chí qua:** người chơi có lý do vào game 15–20 phút/ngày mà không thấy
  "việc vặt"; không thêm hệ thống mới nếu chưa có dữ liệu là hệ thống cũ đã vui.

### Phase 4 — Đấu trường & liên server dạng snapshot (2–3 tuần)
- Arena async: đấu đội hình ảnh của người chơi khác (bot snapshot), xếp hạng tuần,
  thưởng theo hạng — không realtime.
- Phong Hỏa Cát Cứ bản rút gọn: chiếm ô đất theo lượt, đổi điểm lấy vật phẩm.
- Lúc này mới cân nhắc server thật (auth + profile + arena snapshot), và áp dụng
  bài học server-authoritative từ tamquoc3d.
- **Tiêu chí qua:** arena không phá cân bằng PvE; người chơi free vẫn lên hạng được
  bằng đội hình tốt, không chỉ bằng tiền.

### Phase 5 — Kinh tế, VIP nhẹ & xuất bản (2 tuần + vận hành)
- VIP chỉ cho tiện ích (thêm lượt quét ải, thêm ô túi, auto nhanh), **không bán
  chỉ số trực tiếp phá game** — khác với game gốc, đây là lựa chọn cân bằng.
- Gói tháng, gói cấp, sự kiện tích nạp — kèm công khai xác suất mọi loại quay.
- Polish: âm thanh, hiệu ứng tuyệt kỹ, màn hình chính kiểu hub (tận dụng kinh nghiệm
  hub tamquoc3d: icon dock, thẻ người chơi, chat/sự kiện tách bạch).
- Export HTML5 (web) trước, rồi PC; mobile sau nếu hiệu năng đạt.
- **Không làm ở bản này:** Guild realtime, PvP realtime, liên server realtime,
  đấu giá, giao dịch người chơi — để phase sau khi có người chơi thật.

## 7. Rủi ro & cách chặn

| Rủi ro | Chặn bằng |
|---|---|
| Scope phình như game gốc (20+ hệ thống) | Phase gate: không mở phase mới khi phase cũ chưa qua tiêu chí; mỗi hệ thống mới phải thay 1 hệ thống cũ hoặc chứng minh bằng playtest |
| Art là nút cổ chai | Phase 0–2 dùng placeholder + asset pixel tự làm/AI-gen có kiểm duyệt; thuê/vẽ final chỉ cho 6 tướng đầu |
| AI code nhanh nhưng nát kiến trúc | Bắt buộc theo godot-master: data tách code, signal-up, test headless mỗi phase; review diff trước khi merge |
| Cân bằng paywall | Công thức tăng trưởng công khai trong data; test free-run: tài khoản không nạp phải qua hết chương 1–2 |
| Bản quyền | Không dùng tên thương mại, art, nhạc, text gốc; mọi asset ghi nguồn trong `assets/CREDITS.md` |

## 8. Việc cần Huynh chốt trước khi code (chọn 1 mỗi dòng)

- **Đã chốt 2026-10-26:** Godot 4.7+ .NET (C#).
- Còn lại, chọn 1 mỗi dòng:

1. **Nền tảng đầu tiên:** Web (HTML5) / PC / Mobile?
2. **Art:** pixel (tận dụng bộ pixel Tam Quốc đã có) / 2D vẽ tay / AI-gen?
3. **Ngôn ngữ game:** tiếng Việt trước (đề xuất) hay song ngữ Việt–Trung?
4. **Có server ngay không:** không (local-save, đề xuất) / có (đi kèm Phase 4)?
5. **Tướng khởi đầu:** giữ 6 tướng ví dụ ở Phase 1 hay đổi danh sách?

Sau khi chốt 5 dòng trên, bước tiếp theo là: tạo `project.godot` .NET + Phase 0 trong
repo `longtuong`, commit theo từng phase, CI headless xanh mới push (theo ủy quyền
push tự động Huynh đã cho từ 2026-10-23).
