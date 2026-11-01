"""Encode Godot-rendered preview captures as a GIF. Does not edit game artwork.

First run tests/PreviewGiaCatLuong.tscn -- --record to capture /tmp PNGs.
"""
from pathlib import Path
from PIL import Image

root=Path(__file__).resolve().parents[2]
paths=sorted(Path('/tmp/longtuong-gcl-frames').glob('[0-9][0-9][0-9].png'))
if len(paths)!=80:raise SystemExit(f'Expected 80 Godot frames, got {len(paths)}')
frames=[]
for path in paths:
    with Image.open(path) as frame:
        frames.append(frame.convert('RGB'))
out=root/'docs/previews/gia_cat_luong_full.gif'
frames[0].save(out,save_all=True,append_images=frames[1:],duration=50,loop=0,optimize=True)
for frame in frames:frame.close()
print(out,out.stat().st_size)
