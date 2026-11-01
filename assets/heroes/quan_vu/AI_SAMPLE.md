# Mẫu Quan Vũ

Tạo ngày 01/11/2026 bằng công cụ image_gen tích hợp. Đây là mẫu AI để thử
phong cách và đường nạp asset, không phải tranh do người dùng tự vẽ.

- body.png: ảnh toàn thân RGBA, nền trong suốt, nhìn sang phải.
- portrait.png: chân dung cùng thiết kế, nền xanh đậm.
- Giữ nguyên PNG gốc do công cụ trả về (1254×1254); bộ nạp tự thu theo canvas.
  Không phải template căn pixel 512×512; khi tự vẽ hãy dùng chuẩn điểm chân
  trong assets/README.md. Hình hiện là ảnh tĩnh, chưa có bộ frame animation.

Copy vào hai tên này là game tự dùng khi mở màn/vào trận; không đổi code.
Nguồn ảnh được lưu trong workspace, không phụ thuộc thư mục sinh ảnh bên ngoài.

## Prompt ảnh toàn thân

Use case: stylized-concept.
Asset type: production 2D RPG full-body character cutout, one single character, square canvas.
Create an original sample of Guan Yu / Quan Vũ for a Vietnamese Three Kingdoms side-view battle game.
Subject: imposing mature Chinese warrior with a dignified reddish warm complexion, long straight black beard, green head wrap with small gold ornament, emerald-green layered robe and cape, dark jade lamellar armor, restrained antique gold trim, dark leather boots. His signature long guandao has a clearly readable silver crescent blade and dark wooden shaft. Historical fantasy, original design, no borrowed game costume.
Style: polished hand-painted 2D game illustration, semi-realistic but simplified chunky forms and bold readable silhouettes at small sprite sizes; crisp clean edges, broad light/shadow shapes, rich emerald green, ivory steel, muted gold. Not photorealistic, not pixel art, not a 3D render, not chibi.
Composition: a complete full-body idle battle pose, three-quarter side view facing screen RIGHT; face and gaze clearly point RIGHT. Both boots visible. Hold the entire guandao upright in front/right of the body, blade fully visible, weapon not covering face. Body centered. Entire character, cape, beard, weapon and accessories fit inside a square 1024 x 1024 transparent canvas with generous clear margins. The midpoint between the boot soles MUST be at x=512, y=920 (normalized 0.5, 0.8984375), with both feet resting on this invisible baseline. Helmet around y=150. Weapon blade stays below y=90 and entirely inside the canvas.
Background: true alpha transparency, absolutely no background, ground, floor, pedestal, frame, cast shadow or contact shadow. The game supplies the foot shadow.
Lighting: soft upper-left key light, readable face, controlled highlights, no glow or particles.
Constraints: ONLY ONE character, one pose, no portrait panel, no duplicate views, no text, no watermark, no numbers, no logos, no scenery. Export as transparent PNG with actual alpha.

## Prompt chỉnh bố cục

Use case: precise-object-edit. Edit the supplied Guan Yu character cutout for game-asset framing only. Preserve exactly the character identity, costume, face, green/gold palette, painted style, weapon, pose and direction facing right. Do not redesign.
Reframe onto a square transparent PNG canvas with generous transparent padding. Scale the entire existing cutout uniformly down to about 82 percent of canvas width/height so that NO cape, blade or boot touches the image edges. Translate it so the midpoint between the two boot soles is at 50 percent of canvas width and approximately 89.8 percent of canvas height. Leave a transparent strip of about 10 percent canvas height BELOW both boots. Top of the weapon must be at least 7 percent below the top edge. No ground or shadow. Use the full square canvas including padding; do not tightly crop to subject. Actual alpha transparency. No text, no labels, no extra objects. Aim for a 512x512 usable square PNG.

## Prompt chân dung

Use case: stylized-concept. Asset type: square RPG hero portrait PNG.
The supplied image is a CHARACTER DESIGN REFERENCE, not a layout to preserve. Create ONE NEW close-up portrait of exactly the same Quan Vũ / Guan Yu warrior: mature stern Chinese face with reddish warm complexion, strong thick eyebrows, long straight black beard, emerald green headwrap with a small gold ornament, dark jade armor with gold dragon shoulder guard, green robes, hand-painted semi-realistic 2D game art.
Composition: head and shoulders/chest, face large and readable at a 64px icon size, three-quarter view looking slightly RIGHT. Full headwrap visible, centered face, beard flows to lower edge; head occupies upper-center of frame. Crop the body at chest, not the head. A hint of the silver guandao blade at the far right edge is optional. Preserve costume colors, facial features and painting style from the reference.
Backdrop: simple opaque dark jade/teal painted gradient, subtle warm rim light, no scenery, no geometric frame. Rich emerald and antique gold, controlled contrast, crisp facial features, broad readable shading rather than tiny details.
Square image, target 512x512. One portrait only. No labels, no letters, no logos, no watermark, no decorative borders or UI, no multiple panels.

