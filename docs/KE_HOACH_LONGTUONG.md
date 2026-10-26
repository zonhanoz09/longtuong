# Kế hoạch develop lại game tương tự Long Tướng (龙将 / long.hly.com) bằng AI

Ngày phân tích: 2026-10-26. Nguồn kiểm chứng trực tiếp: repo `github.com/zonhanoz09/longtuong`
(commit mới nhất lúc đọc: `0c01eb8 first commit`, trước đó `b031c37 add skill agent`)
và trang chủ `https://long.hly.com/` (đọc ngày 2026-10-26).

> Cập nhật 2026-10-26 (theo chốt của Huynh): stack đổi từ GDScript sang
> **Godot 4.7+ .NET (C#)**. Các mục §5, §6 dưới đây đã sửa theo chốt này;
> bản đầu dùng GDScript chỉ vì skill `godot-master` trong repo viết cho GDScript.

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
