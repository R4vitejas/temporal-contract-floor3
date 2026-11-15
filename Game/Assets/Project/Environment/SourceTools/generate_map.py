from pathlib import Path
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.patches import Rectangle,Arc
import json
out=Path(__file__).resolve().parents[1] / 'Data'
out.mkdir(parents=True, exist_ok=True)
fig,ax=plt.subplots(figsize=(8,11),dpi=160,facecolor='#101c28');ax.set_facecolor('#101c28')
# Coordinates are source X/Y, translated into the floor's metre-based local plan.
rooms=[('Classroom 02',34.55,-24.24,43.23,-14.82,35.281),('Classroom 03',43.23,-24.24,52.03,-14.82,51.231),('Classroom 06 · VIP',34.55,.20,43.23,9.64,35.397),('Classroom 05',43.23,.18,52.03,9.64,51.212)]
ax.add_patch(Rectangle((34.55,-14.82),17.48,15.02,facecolor='#243745'))
for label,x0,y0,x1,y1,door in rooms:
 ax.add_patch(Rectangle((x0,y0),x1-x0,y1-y0,facecolor='#294f60' if 'VIP' not in label else '#5c4931',edgecolor='#d5e4e6',linewidth=2))
 edge=y1 if y0<0 else y0
 ax.plot([door-.47,door+.47],[edge,edge],color='#243745',linewidth=6)
 ax.add_patch(Arc((door-.47,edge),1.88,1.88,theta1=0 if y0>0 else 270,theta2=90 if y0>0 else 360,color='#75d9c8',linewidth=1.3))
 ax.text((x0+x1)/2,(y0+y1)/2,label.replace(' · ','\n'),ha='center',va='center',color='white',fontsize=10,weight='bold')
 # Existing board positions on the outer classroom walls.
 bx=x0+.28 if label in ('Classroom 02','Classroom 06 · VIP') else x1-.28
 ax.plot([bx,bx],[(y0+y1)/2-2,(y0+y1)/2+2],color='#75d9c8',linewidth=3)
ax.plot([34.55,34.55],[-14.82,.2],color='#e08b77',linewidth=5)
ax.plot([52.03,52.03],[-14.82,.2],color='#e08b77',linewidth=5)
ax.text(43.2,-7.5,'CONNECTING CORRIDOR',ha='center',color='#c7d7df',fontsize=10,weight='bold')
ax.scatter([36.48,41.48],[-1,-1],c=['#75d9c8','#f2bd67'],s=70,zorder=5)
ax.text(36.48,-2.1,'A',ha='center',color='white');ax.text(41.48,-2.1,'D',ha='center',color='white')
ax.scatter([38.98],[3],c='#f2bd67',marker='*',s=180,zorder=5)
ax.text(43.2,-27,'A / D = spawn anchors   ★ = VIP anchor\nGreen = supplied boards / doors   Coral = blocked routes',ha='center',color='#b8cbd5',fontsize=9,linespacing=1.7)
ax.set_xlim(32,54.5);ax.set_ylim(-29,14);ax.set_aspect('equal');ax.axis('off')
ax.text(43.2,12.3,'FLOOR 3',ha='center',color='white',fontsize=21,weight='bold')
ax.text(43.2,10.8,'Selected section · supplied classroom layout',ha='center',color='#9bb2c2',fontsize=9)
fig.savefig(out/'Floor3Map.png',bbox_inches='tight',facecolor=fig.get_facecolor());plt.close(fig)
