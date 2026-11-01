"""Read generated PNG alpha and write atlas metadata; never edits source pixels.

Requires Pillow for offline art inspection, not at game runtime.
The metadata exposes existing pixels through Godot AtlasTexture regions/margins.
"""
import json
from itertools import groupby
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2] / 'assets/heroes/gia_cat_luong'


def actor_spec(name):
    image = Image.open(ROOT / 'animations' / name / 'atlas.png')
    if image.size!=(1254,1254):raise ValueError('This calibration is specific to the shipped 1254px GCL sheets.')
    width, height = image.size
    channel = image.getchannel('A')
    alpha = list(channel.get_flattened_data() if hasattr(channel, 'get_flattened_data') else channel.getdata())
    bounds = []
    for col in range(3):
        x0, x1 = round(col * width/3), round((col+1) * width/3)
        count = [sum(value >= 64 for value in alpha[y*width+x0:y*width+x1]) for y in range(height)]
        cuts = [0]
        for k in (1, 2):
            target = height*k/3
            candidates = range(max(cuts[-1]+1, round(target-height*0.13)), min(height, round(target+height*0.13)))
            gaps = [list(group) for empty, group in groupby(candidates, key=lambda y:count[y] < 4) if empty]
            if gaps:
                gap = max(gaps, key=lambda ys:(len(ys), -abs((ys[0]+ys[-1])/2-target)))
                cuts.append(round((gap[0]+gap[-1])/2))
            else:
                cuts.append(min(candidates, key=lambda y: (int(count[y]), abs(y-target))))
        cuts.append(height)
        bounds.append(cuts)
    frames = []
    for i in range(8):
        col, row = i % 3, i // 3
        x0, x1 = round(col*width/3), round((col+1)*width/3)
        y0, y1 = bounds[col][row:row+2]
        if name in ('attack','ultimate') and i==4:
            x1=min(width,884)
        def owned(x,y):
            if name in ('attack','ultimate'):
                if i==4 and x>=836 and y>=650:return False
                if i==5 and x<880 and y<650:return False
            return True
        bx0, by0, bx1, by1 = x1, y1, x0, y0
        occupied = 0
        ground = y0
        for y in range(y0,y1):
            visible = [x for x in range(x0,x1) if alpha[y*width+x] >= 64 and owned(x,y)]
            if visible:
                occupied += len(visible)
                bx0, bx1 = min(bx0,visible[0]), max(bx1,visible[-1]+1)
                by0, by1 = min(by0,y), max(by1,y+1)
            if sum(alpha[y*width+x]>=128 and owned(x,y) for x in range(x0,x1)) >= 5:
                ground = y+1
        if occupied < 100:
            raise ValueError(f'Empty actor frame {name}/{i}')
        bx0, bx1, by0, by1 = max(x0,bx0-2), min(x1,bx1+2), max(y0,by0-2), min(y1,by1+2)
        feet_x = [x for y in range(max(y0,ground-22),ground) for x in range(x0,x1) if alpha[y*width+x] >= 128 and owned(x,y)]
        foot_x = (min(feet_x)+max(feet_x)+1)/2
        if name == 'death' and i >= 4:
            foot_x = (bx0+bx1)/2
        frames.append(([bx0, by0, bx1-bx0, by1-by0], foot_x, ground))
    cw = max(512, max(f[0][2] for f in frames)+32)
    ch = max(512, max(f[0][3] for f in frames)+54)
    ground_y = ch-52
    regions, offsets, pivots, polygons = [], [], [], []
    for i, (region, fx, gy) in enumerate(frames):
        x, y, w, h = region
        left = int((cw-w)//2)
        top = int(ground_y-(gy-y))
        assert top >= 0 and top+h <= ch, (name, region, top)
        regions.append(region)
        offsets.append([left, top])
        pivots.append([round((left+fx-x)/cw, 6), round(ground_y/ch, 6)])
        shape = None
        if name in ('attack','ultimate') and i==4 and x+w>836:
            shape=[[x,y],[x+w,y],[x+w,650],[836,650],[836,y+h],[x,y+h]]
        if name in ('attack','ultimate') and i==5 and x<880:
            shape=[[880,y],[x+w,y],[x+w,y+h],[x,y+h],[x,650],[880,650]]
        polygons.append(shape)
    return dict(sheet='atlas.png', columns=3, rows=3, frames=8,
                fps={'idle':8,'attack':12,'hit':12,'ultimate':10,'death':10}[name],
                event_frame=4, canvas=[cw,ch], regions=regions,
                offsets=offsets, pivots=pivots, source_polygons=polygons, display_scale=round(1.1*ch/512, 6))


def effect_spec(name):
    image = Image.open(ROOT / 'effects' / name / 'atlas.png')
    w,h = image.size
    # Small inset removes irrelevant near-zero-alpha sheet-boundary pixels.
    regions = [[round(i%3*w/3)+3, round(i//3*h/3)+3,
                round((i%3+1)*w/3)-round(i%3*w/3)-6,
                round((i//3+1)*h/3)-round(i//3*h/3)-6] for i in range(8)]
    pivot = [0.9,0.5] if name == 'projectile' else [0.5,0.5]
    return dict(sheet='atlas.png', columns=3, rows=3, frames=8,
                fps={'projectile':12,'normal':16,'ultimate':14,'cast':14,'aura':8}[name],
                regions=regions, pivots=[pivot]*8)


def main():
    config = dict(version=1,
                  clips={name: actor_spec(name) for name in ['idle','attack','hit','ultimate','death']},
                  effects={name: effect_spec(name) for name in ['normal','ultimate','projectile','cast','aura']},
                  anchors=dict(cast=[0.79,0.42], hit=[0.5,0.5]))
    reference_height=config['clips']['idle']['regions'][0][3]
    for clip in config['clips'].values():
        clip['display_scale']=round(clip['display_scale']*reference_height/clip['regions'][0][3],6)
    path = ROOT/'animation.json' 
    path.write_text(json.dumps(config, indent=2)+'\n')
    for name, clip in config['clips'].items():
        print(name, 'canvas',clip['canvas'],'regions',clip['regions'],'pivots',clip['pivots'])
    print('Wrote',path)


if __name__ == '__main__':
    main()
