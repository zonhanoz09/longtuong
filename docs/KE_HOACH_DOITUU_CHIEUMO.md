# Kế hoạch: Tửu quán — Đối tửu + Chiêu mộ theo cơ chế game gốc (longtuong)

> Trạng thái: **ĐÃ CHỐT TOÀN BỘ — ĐÃ TRIỂN KHAI XONG PHASE 2.23**
> (2026-10-28, xem TestPhase223).
>
> Cập nhật Phase 2.24 (2026-10-28, theo ảnh phác + chốt của Huynh):
> **hồn dùng chung theo pool phẩm** (không còn hồn riêng từng tướng;
> hồn cũ 2.23 bỏ luôn, không quy đổi); **roll thắng còn 1–3 hồn**
> (trước 1–5); popup Tửu quán có **2 màn**: Chiêu mộ (lưới tướng
> theo hàng phẩm, bấm ô là chiêu mộ luôn nếu đủ hồn phẩm đó, tướng
> đã sở hữu ghi "đã chiêu mộ", dòng "Hồn Lam: X | Hồn Tím: Y |
> Hồn Cam: Z" trên đầu, 2 nút đối tửu góc dưới phải) và Đối tửu
> (3 tướng rút được, mỗi tướng ghi số hồn thắng được từ tướng đó,
> nút Kéo / Búa / Bao, nút "Quay lại" về Chiêu mộ; quay lại giữa
> chừng thì hồn đã thắng giữ nguyên, lượt chưa đánh coi như bỏ).
> Các mục §1.4/§2.1/§2.2 dưới đây bị thay thế ở những điểm mâu
> thuẫn với cập nhật này (hồn riêng từng tướng, roll 1–5, 1 màn).
>
> Cập nhật Phase 2.25 (2026-10-28, theo ảnh phác mới + chốt của Huynh):
> **tiền tệ tách đôi** — đối tửu Thường/Cao tốn **Bạc** (150.000 /
> 300.000, đúng giá gốc), **tất thắng (x10)** tốn phẳng **200 Vàng**:
> trừ 1 lần upfront, auto 10 lần rót rượu = 30 tướng, thắng cả 30,
> rút theo tỉ lệ mức Cao + khóa Cam cấp 70, mỗi thắng 1–3 hồn vào
> pool phẩm, xong hiện tổng kết "+X hồn Lam, +Y hồn Tím, +Z hồn Cam"
> ở dòng trạng thái dưới lưới. **Layout đúng ảnh**: title căn giữa,
> lưới 6 cột đều (pitch 152, hàng Lam 2 / Tím 2 / Cam 3 theo roster
> hiện tại), ô tướng chỉ có tên + **chấm màu phẩm + số giá** (không
> chữ phẩm), dòng số dư hồn cũng dạng chấm màu + số, 3 nút thẳng
> hàng dưới phải (Tất thắng / Thường / Cao, đều ghi giá), dòng
> tiền tệ Vàng | Bạc trên đầu. Tài khoản mới nhận 1.000.000
> Bạc khởi đầu (số Pax tự setup); save cũ được cấp 1 lần duy nhất
> qua cờ SilverSeeded.
> (viết lại 2026-10-28 theo lệnh mới: "plan copy 100% từ cơ chế
> game gốc"; Huynh chốt nốt điểm cuối 2026-10-28: bỏ nút chọn
> phẩm, tỉ lệ ra ngẫu nhiên theo setup).
> Bản nháp trước (chiêu mộ kiểu gacha + hảo cảm) đã bị thay thế hoàn
> toàn bằng bản này, vì trong game gốc **đối tửu và chiêu mộ là MỘT
> hệ thống duy nhất trong Tửu quán**, không phải 2 tính năng riêng.
>
> Ranh giới mới theo lệnh Huynh: **copy 100% cơ chế** game gốc.
> Art, code và text vẫn tự làm (không lấy asset/code/text gốc) trừ
> khi Huynh nói khác. Các mục ghi "CHƯA XÁC MINH" là những số mà
> nguồn công khai còn lại không ghi rõ — cần Huynh điền từ trí nhớ
> chơi game gốc, không tự bịa số rồi ghi là "gốc".

## 0. Cơ chế gốc (đã xác minh từ nguồn còn lại về game Long Tướng)

Nguồn: bài hướng dẫn chiêu mộ Long Tướng trên GameLandVN và bài
review Long Tướng trên Việt Giải Trí (game gốc đã ngừng vận hành,
chỉ còn các bài viết này).

1. **Tửu quán là nơi duy nhất để chiêu mộ.** Vào tửu quán để đối
   tửu, đối tửu xong mới chiêu mộ được tướng.
2. **Đối tửu = uống rượu + oẳn tù tì với tướng.** Mỗi lần đối tửu
   tốn bạc, xuất hiện 1 tướng, người chơi oẳn tù tì (búa/kéo/bao)
   với tướng đó. **Thắng thì nhận Võ Hồn**, thua thì không (theo
   bài review: "chiến thắng trò chơi Oẳn Tù Xì trước các võ tướng"
   mới nhận Võ Hồn).
3. **Võ Hồn là đơn vị chiêu mộ.** Mỗi tướng cần **một lượng Võ Hồn
   khác nhau** mới chiêu mộ được. Tích đủ Võ Hồn của tướng đó thì
   chiêu mộ tướng đó về đội.
4. **Phẩm chất tướng: Lam → Tím → Cam → Đỏ.** Ban đầu chỉ có
   lựa chọn đối tửu tướng Lam và tướng Tím (theo bài review).
   **Võ Hồn cần để chiêu mộ (ĐÃ CHỐT theo Huynh 2026-10-28):
   Lam 100 hồn, Tím 500 hồn, Cam 1500 hồn, Đỏ chỉ nhận ở các
   event** (không có trong tửu quán). **Đối tửu Cam mở ở cấp
   người chơi 70 (ĐÃ CHỐT theo Huynh 2026-10-28)** — tức "tướng
   Vàng từ cấp 70" trong bài viết chính là phẩm Cam, không có
   phẩm Vàng riêng trong tửu quán.
5. **2 mức đối tửu theo bạc:** thường **150.000 bạc/lần**, cao cấp
   **300.000 bạc/lần**. Bài gốc lưu ý: mức thường cũng có tỉ lệ
   xuất hiện tướng Vàng khá cao nên hiệu quả bạc tốt hơn, đừng mù
   quáng chọn cao cấp nếu không thật sự nhiều bạc.
6. **Tướng chiêu mộ được khóa theo cấp người chơi** (bài gốc):
   chưa tới cấp 50 chỉ chiêu mộ được tướng Lam (Hoàng Cái, Vu Cấm,
   Từ Thứ, Liêu Hóa); lên cấp 50 chiêu mộ thêm tướng Tím (Tôn Sách,
   Hạ Hầu Uyên, Bàng Thống, Hạ Hầu Đôn, Lỗ Túc, Thái Văn Cơ...);
   cấp 70 đội hình cơ bản đã đủ, tập trung đối tửu cao cấp.
7. **Nguồn bạc chính:** rung cây nhận bạc 10 lần/ngày (bài gốc tính
   tích lũy khoảng 6.000.000 bạc khi lên cấp 50). Bạc là tiền đối
   tửu; vàng (tiền nạp) dùng để chiêu mộ tướng Tím ở cấp 30 nếu
   muốn đi nhanh.
8. **Đội hình gốc:** chủ tướng + 4 tướng (tổng 5 người) — khớp với
   giới hạn 5 tướng của Bày trận hiện tại.
9. **Ví dụ kỹ năng tướng gốc** (để đối chiếu khi gán tướng):
   Tôn Sách né rất tốt (tướng thủ); Hạ Hầu Uyên đánh trung quân rồi
   phối hợp chủ tướng đánh trung quân thêm lần nữa; Bàng Thống khống
   chế trung quân, thành công thì trung quân địch không đánh lại
   trong 3 hiệp; Hạ Hầu Đôn tỉ lệ sát thương cao; Lỗ Túc khống chế;
   Vu Cấm giảm phòng ngự địch; Liêu Hóa giảm đỡ đòn địch.

## 1. Chuyển thể sang longtuong (giữ cơ chế, đổi đơn vị cho khớp game)

### 1.1. Bảng đối chiếu

| Cơ chế gốc | Bản remake |
|---|---|
| Tửu quán (1 nơi duy nhất chiêu mộ) | Popup Tửu quán, mở từ nút "Chiêu mộ" sẵn có ở FunctionBar Hub (popup `RecruitScene` hiện đang khóa sẽ viết lại thành Tửu quán) |
| Bạc (tiền đối tửu) | Vàng (tiền mềm hiện tại của game) |
| Vàng nạp (đi nhanh) | KNB (chưa dùng trong tửu quán ở bản này) |
| Cấp người chơi khóa tướng / khóa đối tửu Cam | Xem §1.2 — game hiện chưa có hệ cấp người chơi |
| Phẩm chất Lam / Tím / Cam / Đỏ | Gán cho 7 tướng trong pool (đề xuất ở §1.3); Đỏ chỉ từ event |
| Võ Hồn từng tướng | `Dictionary<string, int> GeneralSouls` — Võ Hồn từng tướng, xem §2.2 |
| Oẳn tù tì thắng → Võ Hồn | Giữ nguyên: chọn Búa/Kéo/Bao, thắng nhận Võ Hồn |
| Chủ tướng + 4 tướng | Giữ nguyên: Bày trận tối đa 5 tướng đã làm ở 2.20 |

### 1.2. Hai điểm bắt buộc phải đổi đơn vị (Huynh chốt)

**a) Tiền đối tửu: đổi đơn vị cho khớp kinh tế game (ĐÃ CHỐT).**
Số gốc là 150.000 / 300.000 bạc một lần, nhưng kinh tế game hiện
tại nhỏ hơn khoảng 1000 lần (thắng ải 1 chỉ được 150 Vàng, seed test
100.000 Vàng). Giữ nguyên số 150.000/300.000 sẽ khiến không ai đủ
tiền đối tửu. **Chốt: thường 100 Vàng/lần, cao cấp 300 Vàng/lần**
(tỉ lệ 1:3 theo Huynh, khác tỉ lệ 1:2 của gốc).

**b) Khóa theo cấp người chơi: game hiện chưa có cấp người chơi.**
`SaveData.PlayerLevel` có sẵn nhưng chưa hệ thống nào dùng. Đề
xuất (chọn 1):
- **ĐÃ CHỐT: cấp người chơi = cấp của tướng mặc định mới tạo
  game** (tức tướng tự tạo — tướng cố định, không phải
  `SelectedHeroId` có thể đổi bằng cách bấm tab). Vì vậy KHÔNG
  cần hệ EXP người chơi riêng: tướng tự tạo luôn xuất trận (luật
  2.20: main luôn ở Trung quân) nên lên cấp theo EXP trận thắng
  như mọi tướng.
- **Cấp tối đa (ĐÃ CHỐT): 180.** `MaxLevel` nâng từ 60 lên 180
  (mọi tướng) thì mốc Cam 70 mới tới được. Công thức EXP giữ
  nguyên kiểu tăng dần, nên lên 180 là đường cày rất dài đúng
  kiểu game gốc.
- **Công thức EXP (ĐÃ CHỐT): cấp càng cao thì EXP cần để lên cấp
  càng nhiều** — đúng công thức hiện có của game
  (`ExpToNext = 100 + 50×(cấp − 1)`), dùng luôn cho tướng tự tạo,
  không làm công thức mới.
- Tướng Lam/Tím xuất hiện từ đầu; **tướng Cam chỉ vào pool rút
  khi tướng tự tạo đạt cấp 70** (dưới 70 tỉ lệ Cam = 0, đúng gốc:
  ban đầu chỉ có Lam và Tím).

### 1.3. Phân phẩm cho 7 tướng trong pool (ĐÃ CHỐT — Huynh nói "giữ")

Tướng tự tạo không có trong tửu quán (đã sở hữu sẵn, không chiêu mộ).

- **Lam (100 hồn):** Lưu Bị, Trương Phi.
- **Tím (500 hồn):** Quan Vũ, Triệu Vân.
- **Cam (1500 hồn):** Điêu Thuyền, Gia Cát Lượng, Lữ Bố.
- **Đỏ (chỉ từ event):** chưa có tướng nào trong roster hiện tại
  (tướng Đỏ sẽ là tướng event sau này, không xuất hiện trong tửu
  quán).
- **Quy luật phẩm chất (ĐÃ CHỐT): phẩm càng cao thì chỉ số càng
  cao và tỉ lệ trưởng thành càng cao.** Áp dụng: mỗi phẩm có hệ
  số nhân chỉ số gốc và hệ số nhân tốc độ tăng trưởng theo cấp
  (áp vào `GetEffectiveHero`, thay chỗ hệ số sao vì sao tướng đã
  bị loại bỏ — xem §2.2). **Hệ số tạm thời do Pax tự setup
  (ĐÃ CHỐT theo Huynh): Lam ×1.0 / ×1.0, Tím ×1.15 / ×1.15,
  Cam ×1.35 / ×1.35, Đỏ ×1.6 / ×1.6** (chỉ số gốc / tăng trưởng
  mỗi cấp). Huynh sửa số sau tùy ý.

### 1.4. Vòng chơi Tửu quán (copy cơ chế gốc)

1. Mở popup Tửu quán từ Hub.
2. Chọn mức đối tửu: **Thường 100 Vàng / Cao cấp 300 Vàng (ĐÃ
   CHỐT)**. **Không còn nút chọn phẩm (ĐÃ CHỐT)** — 3 tướng rút
   ngẫu nhiên theo tỉ lệ phẩm của mức (bước 3). Đỏ không có trong
   tửu quán.
3. Bấm "Đối tửu" → trừ tiền → **xuất hiện ngẫu nhiên 3 tướng**
   (ĐÃ CHỐT theo Huynh 2026-10-28), mỗi tướng rút **ngẫu nhiên
   theo setup tỉ lệ phẩm của mức (ĐÃ CHỐT)**: **Thường Lam 70%
   / Tím 25% / Cam 5%, Cao cấp Lam 60% / Tím 30% / Cam 10%** —
   vì vậy tướng Cam khó chiêu mộ (vừa hiếm xuất hiện, vừa cần
   1500 hồn). Dưới cấp 70, tỉ lệ Cam = 0, phần còn lại chia theo
   tỉ lệ gốc của Lam/Tím. Roster hiện tại mỗi phẩm chỉ có 2–3
   tướng nên 3 tướng rút ra có thể trùng nhau (rút có hoàn lại).
4. **Oẳn tù tì lần lượt với cả 3 tướng (ĐÃ CHỐT theo Huynh
   2026-10-28: "thắng tướng nào nhận tướng đó"):** với mỗi tướng,
   người chơi chọn Búa / Kéo / Bao, tướng ra ngẫu nhiên. Thắng
   tướng nào thì nhận hồn tướng đó (1–5 hồn); hòa tướng nào thì
   cộng 1 lượt miễn phí với tướng đó; thua tướng nào thì thôi
   tướng đó. Một lần đối tửu tối đa 3 lần nhận hồn (mỗi tướng
   tối đa 1 lần thắng có hồn, chưa tính lượt hòa cộng thêm).
5. **Kết quả ván oẳn tù tì (ĐÃ CHỐT theo Huynh 2026-10-28):**
   **thắng → nhận ngẫu nhiên 1–5 hồn** của tướng đó, và nhận đủ
   100% số hồn đã roll (không random thiếu); **hòa → cộng 1 lượt
   oẳn tù tì miễn phí** với cùng tướng (không tốn thêm bạc, hòa
   tiếp thì lại cộng tiếp); **thua → thôi** (không nhận Võ Hồn,
   kết thúc lượt, bạc đã mất).
6. Mở tab "Võ Hồn" xem từng tướng đang có bao nhiêu Võ Hồn / cần
   bao nhiêu. **Đủ Võ Hồn → nút "Chiêu mộ" sáng → bấm là tướng về
   đội** (vào danh sách sở hữu, dùng được ở Bày trận ngay).
   Ngưỡng chiêu mộ (ĐÃ CHỐT): **Lam 100, Tím 500, Cam 1500 hồn**.
7. **Võ Hồn chỉ để chiêu mộ tướng (ĐÃ CHỐT).** Tướng đã sở hữu
   vẫn có thể xuất hiện khi đối tửu, nhưng Võ Hồn của tướng đó
   không dùng vào việc khác (sao tướng đã loại bỏ khỏi game nên
   không còn đường tăng sao).

## 2. Dữ liệu & code (Phase 2.23 — Tửu quán)

### 2.1. UI popup Tửu quán (1000×620 — gần bằng popup Võ tướng
1000×600 nhưng lệch 20px để mỗi popup có kích thước riêng theo
test 2.3)

- Hàng trên: 2 nút mức **Thường 100 Vàng / Cao cấp 300 Vàng
  (ĐÃ CHỐT)**, nhãn Vàng hiện có và nhãn cấp tướng tự tạo hiện
  tại (cấp 70 mở Cam trong pool rút). Không có nút chọn phẩm —
  3 tướng rút theo tỉ lệ phẩm của mức (§1.4 bước 3).
- Giữa: khung **3 tướng xuất hiện ngẫu nhiên** (tên + phẩm + Võ
  Hồn hiện có / cần của từng tướng), 3 nút **Búa / Kéo / Bao**,
  dòng kết quả ván oẳn tù tì.
- Dưới: danh sách Võ Hồn từng tướng (tên — đã có/cần — nút Chiêu
  mộ khi đủ), dòng trạng thái.
- Đối tửu xong tự lưu game. Không đủ Vàng thì báo thiếu, không trừ
  tiền. Hết tiền thì nút Đối tửu khóa.

### 2.2. Dữ liệu mới

- `SaveData.GeneralSouls: Dictionary<string, int>` — Võ Hồn từng
  tướng (khởi tạo 0 cho mọi tướng trong `EnsureDefaults` để save
  cũ load được).
- Bảng data mới `TavernData` (tĩnh, giống `HeroDatabase`):
  phẩm của từng tướng (§1.3), Võ Hồn cần để chiêu mộ theo phẩm
  (**Lam 100, Tím 500, Cam 1500** — ĐÃ CHỐT), Võ Hồn nhận khi
  thắng (**ngẫu nhiên 1–5 hồn** — ĐÃ CHỐT), tỉ lệ phẩm khi rút
  theo mức (**Thường 70/25/5, Cao cấp 60/30/10 — ĐÃ CHỐT**),
  điều kiện Cam vào pool (§1.2: tướng tự tạo cấp 70).
- Mới: `TavernService`: `StartDrink(save, grade)` (trừ tiền theo
  mức: Thường 100 / Cao cấp 300, rút ngẫu nhiên 3 tướng theo tỉ
  lệ phẩm của mức: Thường 70/25/5, Cao cấp 60/30/10 — ĐÃ CHỐT;
  dưới cấp 70 tỉ lệ Cam = 0),
  `PlayRps(save, heroId, playerMove, rng)` (chơi 1 ván với 1 tướng:
  tướng ra ngẫu nhiên; thắng cộng ngẫu nhiên 1–5 hồn đủ 100%, hòa
  cộng 1 lượt miễn phí với tướng đó, thua kết thúc ván với tướng
  đó; một lần đối tửu chơi lần lượt cả 3 tướng),
- Cấp người chơi = cấp tướng tự tạo: không thêm hệ EXP riêng;
  `PlayerLevel` trong save (nếu còn dùng) đồng bộ bằng cấp tướng
  tự tạo. **`MaxLevel` nâng từ 60 lên 180 (ĐÃ CHỐT)**,
- Phẩm chất: `TavernData` thêm hệ số chỉ số + hệ số trưởng thành
  theo phẩm (số tạm do Pax setup ở §1.3); `GetEffectiveHero`
  nhân thêm 2 hệ số này,
- **Sao tướng loại bỏ khỏi game (ĐÃ CHỐT):** xóa
  `HeroProgress.Stars`, hệ số sao trong `GetScaledBase` /
  `GetEffectiveHero`, và mọi hiển thị "Sao X/5" (màn Trang bị/Cường
  hóa, Túi đồ). Phạm vi này chỉ là sao tướng; **sao ải (Sao X/3
  trong popup thắng) và Tướng Tinh (12 ô) giữ nguyên** trừ khi
  Huynh nói khác — vì đó là 2 hệ thống khác tên, không phải "sao
  tướng".
  `CanRecruit(save, heroId)`, `Recruit(save, heroId)` (trừ Võ Hồn,
  thêm vào `OwnedHeroIds`).
- `RecruitService` cũ (gacha 100 Vàng, đã khóa ở 2.22) **bỏ hẳn**,
  thay bằng `TavernService`. Nút "Chiêu mộ" ở Hub và ở màn Trang
  bị/Cường hóa đều mở popup Tửu quán.

### 2.3. Kiểm chứng

- `tests/TestPhase223.tscn`: đối tửu Thường trừ 100 Vàng, Cao
  cấp trừ 300 Vàng; không đủ Vàng không trừ tiền; mỗi lần đối tửu
  xuất hiện đúng 3 tướng; dưới cấp 70 không tướng Cam nào xuất
  hiện, từ cấp 70 tướng Cam xuất hiện theo tỉ lệ (test bằng seed
  cố định: Thường 5%, Cao cấp 10%); chơi lần lượt cả 3 tướng,
  thắng tướng nào cộng 1–5 hồn tướng đó, hòa cộng 1 lượt miễn phí
  với tướng đó, thua không cộng; cấp người chơi bằng đúng cấp
  tướng tự tạo; cấp tối đa là 180; tướng phẩm cao có chỉ số và
  trưởng thành cao hơn (theo hệ số §1.3); sao tướng không còn tồn
  tại (không field Stars, không hiển thị Sao X/5, không hệ số sao
  trong scaling); đủ 100/500/1500 hồn (Lam/Tím/Cam) thì nút Chiêu
  mộ sáng, bấm xong tướng vào danh sách sở hữu và Võ Hồn trừ đúng
  số; save cũ không có `GeneralSouls` vẫn load được; popup Tửu
  quán có đủ nút mức, nút Búa/Kéo/Bao.

## 3. Ngoài phạm vi phase này (không làm)

- Rung cây nhận bạc mỗi ngày (nguồn bạc chính của gốc) — bản này
  tiền đối tửu lấy từ thưởng thắng ải; rung cây để Phase 3 hoạt
  động ngày quyết sau.
- Sao tướng (đã loại bỏ khỏi game, xem §2.2).
- Hệ cấp người chơi riêng (EXP người chơi, bảng xếp hạng cấp, quà
  lên cấp...) — cấp người chơi đã chốt là cấp tướng tự tạo nên
  không làm hệ riêng.
- Thêm tướng mới (Hoàng Cái, Tôn Sách... của gốc) — roster giữ
  nguyên 8 tướng hiện tại ở bản này (tỉ lệ phẩm khi rút thì thuộc
  phase 2.23, xem §1.4).

## 4. Điểm CHƯA XÁC MINH — cần Huynh điền/xác nhận (trả lời theo số)

Nguồn công khai còn lại không ghi rõ các số này. **Pax không bịa
số gốc.** Huynh điền từ trí nhớ chơi game gốc, hoặc chọn đề xuất
tạm để code trước rồi sửa số sau.

1. **Ngưỡng chiêu mộ và hồn mỗi lần thắng — ĐÃ CHỐT:** Lam 100,
   Tím 500, Cam 1500 hồn; Đỏ chỉ từ event. Mỗi lần đối tửu xuất
   hiện ngẫu nhiên 3 tướng, thắng oẳn tù tì nhận ngẫu nhiên 1–5
   hồn (đủ 100% số đã roll).
2. **Hòa và thua oẳn tù tì thì sao? — ĐÃ CHỐT:** hòa cộng 1 lượt
   oẳn tù tì miễn phí (cùng tướng), thua thì thôi (không Võ Hồn).
3. **Oẳn tù tì với 1 trong 3 tướng hay lần lượt cả 3? — ĐÃ CHỐT:**
   lần lượt cả 3 tướng, thắng tướng nào nhận hồn tướng đó.
4. **Võ Hồn của tướng ĐÃ sở hữu dùng để làm gì? — ĐÃ CHỐT:**
   Võ Hồn chỉ để chiêu mộ tướng. Và **sao tướng loại bỏ khỏi
   game (ĐÃ CHỐT)** nên không còn câu hỏi đường tăng sao nữa:
   xóa `HeroProgress.Stars`, hệ số sao và hiển thị Sao X/5.
   Phạm vi xóa chỉ là sao tướng; sao ải và Tướng Tinh giữ nguyên
   trừ khi Huynh nói khác (xem §2.2).
5. **Mức Thường / Cao cấp — ĐÃ CHỐT:** Thường 100 Vàng, Cao
   cấp 300 Vàng; **tỉ lệ ra ngẫu nhiên theo setup: Thường Lam
   70% / Tím 25% / Cam 5%, Cao cấp Lam 60% / Tím 30% / Cam 10%**
   (vì vậy tướng Cam khó chiêu mộ); **bỏ nút chọn phẩm**.
6. **Khóa đối tửu Cam và cấp tối đa — ĐÃ CHỐT:** mở đối tửu Cam
   khi **tướng tự tạo đạt cấp 70**; cấp người chơi chính là cấp
   tướng tự tạo (không làm hệ EXP riêng); công thức EXP giữ
   nguyên kiểu "cấp càng cao cần càng nhiều EXP"; **cấp tối đa
   là 180** (mọi tướng).
7. **Phân phẩm §1.3 — ĐÃ CHỐT (Huynh nói "giữ"):** Lam: Lưu Bị,
   Trương Phi; Tím: Quan Vũ, Triệu Vân; Cam: Điêu Thuyền, Gia
   Cát Lượng, Lữ Bố; Đỏ: chưa có.
8. **Tiền đối tửu — ĐÃ CHỐT: Thường 100 Vàng, Cao cấp 300
   Vàng** (thay cho 150.000/300.000 bạc trong bài viết).
9. **Hệ số phẩm chất — ĐÃ CHỐT là tạm thời do Pax tự setup:**
   Lam ×1.0 / ×1.0, Tím ×1.15 / ×1.15, Cam ×1.35 / ×1.35,
   Đỏ ×1.6 / ×1.6 (chỉ số gốc / tăng trưởng mỗi cấp). Huynh sửa
   số sau tùy ý.

## 5. Nguồn tham khảo (game gốc đã ngừng vận hành)

- GameLandVN, "Tuyệt chiêu giúp chiêu mộ võ tướng trong Long
  Tướng": https://gamelandvn.com/tuyet-chieu-giup-chieu-mo-vo-tuong-xin-trong-long-tuong/
- Việt Giải Trí, "Long Tướng: Bắt người chơi đọc quá nhiều":
  http://Vietgiaitri.com/long-tuong-bat-nguoi-choi-doc-qua-nhieu-20120429i381953/
