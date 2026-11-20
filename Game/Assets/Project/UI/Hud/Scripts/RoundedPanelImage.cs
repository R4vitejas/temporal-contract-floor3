using UnityEngine;
using UnityEngine.UI;

namespace TemporalContract.UI.Hud
{
    /// <summary>Small code-authored rounded UI geometry; filled progress images retain normal Image behaviour.</summary>
    public sealed class RoundedPanelImage : Image
    {
        public float radius=6;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            if(type==Type.Filled||radius<=0){base.OnPopulateMesh(mesh);return;}
            var rect=GetPixelAdjustedRect();float r=Mathf.Min(radius,Mathf.Min(rect.width,rect.height)*.5f);
            mesh.Clear();var vertex=UIVertex.simpleVert;vertex.color=color;vertex.position=rect.center;mesh.AddVert(vertex);
            var corners=new[]{new Vector2(rect.xMax-r,rect.yMax-r),new Vector2(rect.xMin+r,rect.yMax-r),new Vector2(rect.xMin+r,rect.yMin+r),new Vector2(rect.xMax-r,rect.yMin+r)};
            for(int corner=0;corner<4;corner++)for(int step=0;step<=6;step++)
            {
                float angle=(corner*90+step*15)*Mathf.Deg2Rad;
                vertex.position=corners[corner]+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*r;mesh.AddVert(vertex);
            }
            int count=mesh.currentVertCount-1;
            for(int i=1;i<=count;i++)mesh.AddTriangle(0,i,i==count?1:i+1);
        }
    }
}
