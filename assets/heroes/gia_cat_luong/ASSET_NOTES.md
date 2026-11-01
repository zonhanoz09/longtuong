# Asset Gia Cát Lượng từ ảnh tham chiếu

Ngày: 01/11/2026. Nguồn tham chiếu do người dùng cung cấp:
[Gemini_Generated_Image.jpeg](Gemini_Generated_Image.jpeg). Giữ nguyên file gốc.

## Bản game đang dùng

- body.png: PNG RGBA, nền trong suốt, một tư thế đứng nhìn sang phải.
- portrait.png: PNG chân dung vuông, nền đỏ nâu sẫm.
- Cả hai giữ áo vàng, vai đỏ, mũ quân sư và quạt lông đỏ cam theo ảnh nguồn.
- Ảnh được tái dựng bằng image_gen tích hợp từ mẫu người dùng, không phải
  cắt nguyên frame từ JPEG. Đã bỏ chữ, số, mũi tên, vòng trận và khói quanh người.
- Giữ canvas gốc công cụ trả về (1254×1254), game tự thu theo canvas. Đây
  không phải template căn pixel 512×512 cho animation.

Mở lại game/vào trận mới để nhận body; mở lại màn Võ tướng/Bày trận hoặc
vào lại Hub để nhận portrait. Không cần chỉnh mã hay import thủ công.

## Cập nhật bộ đầy đủ — 02/11/2026

Đã có 5 clip và 5 VFX, mỗi nhóm 8 frame thật, cùng metadata căn vùng và
điểm chân. Xem [README bộ asset](README.md), [prompt animation](ANIMATION_PROMPTS.json)
và [ảnh động](../../../docs/previews/gia_cat_luong_full.gif). Body/portrait
vẫn giữ PNG gốc ở trên; JPEG tham chiếu không bị sửa.

Đã chạy renderer GL Compatibility thực tế, xem lại các tư thế và giữ đầy
đủ đầu quạt ở các frame vươn tay. Hình tướng ở sân Hub cũng dùng idle/aura
và quay theo hướng đi. Ghi chú/prompt dưới đây là của lần tạo body/portrait
ban đầu, không phải danh sách frame hiện tại.

## Prompt body

Use case: identity-preserve. Asset type: ONE isolated full-body transparent PNG sprite for a side-view 2D RPG.
Input image is the user's Gia Cát Lượng / Zhuge Liang character reference sheet. Use ONLY the first standing idle pose in the top row as the character design reference. Preserve this exact character identity and outfit: mature Chinese scholar strategist, narrow black moustache and long black goatee, dark hair, tall cylindrical patterned gold scholar crown, rich yellow-gold flowing layered robe with ivory inner skirt, red waist sash and ornate red shoulder mantle with gold details, long floating dark/gold cloth ribbons, red-and-gold feather fan held at the chest. Same stylized hand-painted fantasy game illustration style, bold clean dark outlines and sculpted folds, simplified readable details for a small game sprite. Do NOT replace with a generic white-robed Zhuge Liang. Do NOT use the green Guan Yu design.
Composition: one full-body idle character only, three-quarter view facing screen RIGHT, dignified upright stance, fan held naturally before chest, free hand relaxed. Entire crown, fan, ribbons and both boots fit inside a square canvas. Character centered with balanced transparent margins. Put the midpoint of the boot soles near normalized (0.5, 0.8984375), leaving around 10% clear transparent space below the feet. Target 512x512 square canvas; preserve the padding rather than cropping tightly. Both boots on the same invisible baseline.
Remove all sheet layout, text, numbering, arrows, dividers, other poses and all scenery. Remove the golden circular rune platform, floor, contact shadow, smoke, impact sparks and external fire trails. The feather fan itself stays warm red/orange/gold but NO free-floating fire or particle effects; the game supplies VFX and foot shadow.
Actual alpha transparency, clean antialiased edges. No checkerboard baked in, no ground, no background, no text or watermark. Exactly ONE character, not a sheet.

## Prompt portrait

Use case: identity-preserve. Asset type: ONE square head-and-shoulders RPG portrait.
The user's supplied sheet is the character design reference for Gia Cát Lượng / Zhuge Liang. Create a close-up portrait of the SAME strategist: mature intelligent Chinese face, narrow black moustache and long pointed black goatee, black hair, distinctive tall patterned cylindrical gold scholar crown, ornate red-and-gold shoulder mantle, gold/yellow robes, red sash details, iconic red-orange and gold feather fan across the lower chest. Preserve the outfit and face from the first idle character in the upper row. This is NOT Guan Yu and NOT a white-robed generic strategist.
Hand-painted stylized fantasy 2D RPG artwork matching the reference: bold clean outlines, rich crimson and antique gold, controlled broad shading, crisp face. Three-quarter head view looking screen RIGHT. Framing head and upper torso; crown completely visible, face large and clear, beard visible, fan partly in lower foreground. Single cohesive portrait, no full-body figure.
Background: subtle opaque dark burgundy/charcoal painted gradient to contrast the gold costume. No rune circle, scenery, attack trails, smoke or platforms. Square canvas, target 512x512. No decorative frame, no letters, numbering, arrows, labels, watermark or UI.

