"""Label an orthographic Unity capture; use the same bounds for board click regions."""
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
root=Path(__file__).resolve().parents[1]
im=Image.open(root/'Data/FullFloorMapBase.png').convert('RGB')
w,h=im.size
canvas=Image.new('RGB',(w,h+95),'#1f2b30');canvas.paste(im,(0,0));draw=ImageDraw.Draw(canvas)
font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',12)
bold=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',13)
manifest=json.loads((root/'Data/full-floor-manifest.json').read_text())
regions=[]
labels=[('ClassRoom01','C1'),('ClassRoom02','C2'),('ClassRoom03','C3'),('ClassRoom04','C4'),('ClassRoom05','C5'),('ClassRoom06','C6'),('Room1','Classroom'),('PG Students','PG room'),('Seminar','Seminar'),('Laboratory2','Lab'),('Laboratory3','Lab')]
def point(x,y):return (w/2+(x-38.98+13.5)*h/51,h/2-(y+1+.5)*h/51)
def label(text,x,y):
 px,py=point(x,y);b=draw.textbbox((0,0),text,font=bold);tw=b[2];th=b[3]
 draw.rounded_rectangle((px-tw/2-5,py-th/2-4,px+tw/2+5,py+th/2+5),3,fill='#1f2b30',outline='#c8b884')
 draw.text((px-tw/2,py-th/2-2),text,font=bold,fill='#f4e9ce')
for collection,text in labels:
 objects=[o for o in manifest['objects'] if collection in o['collections']]
 low=[min(o['min'][i] for o in objects) for i in range(2)];high=[max(o['max'][i] for o in objects) for i in range(2)]
 label(text,(low[0]+high[0])/2,(low[1]+high[1])/2)
 p0=point(low[0],high[1]);p1=point(high[0],low[1]);regions.append(dict(id=collection,label=text,pixelRect=[*p0,p1[0]-p0[0],p1[1]-p0[1]]))
for text,x,y in [('Lab',3,9),('Lab',12,9),('Washroom',30,19),('Staff',-11,-5),('Cabin',-11,-15),('HOD',-10,-21)]:label(text,x,y)
draw.text((24,h+12),'FLOOR 3 / FULL LAYOUT',font=bold,fill='#eddbac')
draw.text((24,h+36),'C1–C6: named classrooms • Seminar: central tiered hall • Lab: red-chair rooms',font=font,fill='#e3dfd0')
draw.text((24,h+58),'Existing rooms and corridors preserved. Gameplay locations are proposals pending integration.',font=font,fill='#bfc7c5')
canvas.save(root/'Data/FullFloorMap.png')
(root/'Data/full-floor-map-regions.json').write_text(json.dumps(dict(width=w,height=h+95,origin='top-left',regions=regions),indent=2))
