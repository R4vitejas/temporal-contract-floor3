using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace TemporalContract.Environment.EditorTools
{
    /// <summary>Bakes solid wall faces and separate furniture pieces without filling door/aisle openings.</summary>
    public static class FullFloorCollisionBuilder
    {
        const float Cell=.05f;
        sealed class Surface { public int axis,plane; public readonly HashSet<Vector2Int> cells=new HashSet<Vector2Int>(); }
        public static void Build(GameObject root,MeshRenderer[] renderers)
        {
            var group=new GameObject("Baked floor collision").transform;group.SetParent(root.transform,false);
            var surfaces=new Dictionary<Vector2Int,Surface>();
            int meshCount=0,pieces=0;
            foreach(var renderer in renderers)
            {
                var filter=renderer.GetComponent<MeshFilter>();if(!filter || !filter.sharedMesh)continue;
                string name=renderer.name.Substring("Source_".Length).ToLowerInvariant();
                if(name.StartsWith("door") || name.StartsWith("handle_front") || name.StartsWith("handle_back"))continue;
                var b=renderer.bounds;
                bool architecture=(b.size.y>2.2f && b.min.y<.4f && b.max.y<6f) || name.StartsWith("plane") || name.StartsWith("wall") || name=="stalls";
                bool floor=name.StartsWith("floor")||name=="pathway"||name.StartsWith("roof")||name.StartsWith("stairs");
                bool window=renderer.sharedMaterials.Any(m=>m && m.name=="Retro_Glass");
                if(architecture || floor || window)
                {
                    var mesh=filter.sharedMesh;
                    var vertices=mesh.vertices;var triangles=mesh.triangles;
                    // Exact collision supplies floors, ramps, ceilings and non-axis-aligned shapes.
                    // Faces are duplicated in reverse winding so a thin source wall blocks from both sides.
                    var collisionMesh=new Mesh{name="Collision_"+renderer.name,indexFormat=mesh.indexFormat};
                    collisionMesh.vertices=vertices;
                    var both=new int[triangles.Length*2];Array.Copy(triangles,both,triangles.Length);
                    for(int i=0;i<triangles.Length;i+=3){both[triangles.Length+i]=triangles[i];both[triangles.Length+i+1]=triangles[i+2];both[triangles.Length+i+2]=triangles[i+1];}
                    collisionMesh.triangles=both;collisionMesh.RecalculateBounds();
                    string path="Assets/Project/Environment/Collision/"+renderer.name.Replace('/','_')+".asset";
                    var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if(existing){EditorUtility.CopySerialized(collisionMesh,existing);UnityEngine.Object.DestroyImmediate(collisionMesh);collisionMesh=existing;}else AssetDatabase.CreateAsset(collisionMesh,path);
                    var exact=new GameObject("Surface - "+renderer.name);exact.transform.SetParent(group,false);
                    exact.transform.SetPositionAndRotation(renderer.transform.position,renderer.transform.rotation);exact.transform.localScale=renderer.transform.lossyScale;
                    exact.AddComponent<MeshCollider>().sharedMesh=collisionMesh;meshCount++;
                    if(architecture || window)
                        for(int i=0;i<triangles.Length;i+=3)
                        {
                            Vector3 a=renderer.transform.TransformPoint(vertices[triangles[i]]),c=renderer.transform.TransformPoint(vertices[triangles[i+1]]),d=renderer.transform.TransformPoint(vertices[triangles[i+2]]);
                            var normal=Vector3.Cross(c-a,d-a).normalized;
                            if(Mathf.Abs(normal.y)>.02f)continue;
                            int axis=Mathf.Abs(normal.x)>.999f?0:Mathf.Abs(normal.z)>.999f?2:-1;
                            if(axis<0)continue;
                            Raster(surfaces,axis,a,c,d);
                        }
                }
                else if(b.min.y>=-.2f && b.min.y<.2f && b.max.y>.3f && b.max.y<2.5f &&
                    !name.Contains("socket") && Mathf.Max(b.size.x,b.size.z)<12f)
                {
                    // The source joins several benches into one mesh. Weld positions, split connected
                    // pieces, then box each piece; a single renderer box would seal the aisles.
                    foreach(var bounds in ConnectedBounds(filter.sharedMesh,renderer.transform))
                    {
                        if(bounds.size.x<.06f || bounds.size.z<.06f || bounds.size.y<.03f)continue;
                        var piece=new GameObject("Furniture - "+renderer.name);piece.transform.SetParent(group,false);piece.transform.position=bounds.center;
                        piece.AddComponent<BoxCollider>().size=bounds.size;pieces++;
                    }
                }
            }
            int walls=0;
            foreach(var surface in surfaces.Values)
                while(surface.cells.Count>0)
                {
                    var first=surface.cells.OrderBy(p=>p.y).ThenBy(p=>p.x).First();
                    int width=1;while(surface.cells.Contains(new Vector2Int(first.x+width,first.y)))width++;
                    int height=1;bool more=true;
                    while(more){for(int x=0;x<width;x++)if(!surface.cells.Contains(new Vector2Int(first.x+x,first.y+height))){more=false;break;}if(more)height++;}
                    for(int x=0;x<width;x++)for(int y=0;y<height;y++)surface.cells.Remove(new Vector2Int(first.x+x,first.y+y));
                    float u=(first.x+width*.5f)*Cell,v=(first.y+height*.5f)*Cell;
                    var obj=new GameObject("Solid wall "+walls++);obj.transform.SetParent(group,false);
                    obj.transform.position=surface.axis==0?new Vector3(surface.plane*.025f,v,u):new Vector3(u,v,surface.plane*.025f);
                    obj.AddComponent<BoxCollider>().size=surface.axis==0?new Vector3(.07f,height*Cell,width*Cell):new Vector3(width*Cell,height*Cell,.07f);
                }
            Debug.Log($"FULL_FLOOR_COLLISION_BAKE: {meshCount} two-sided source surfaces, {walls} solid wall rectangles, {pieces} separate furniture pieces.");
        }
        static void Raster(Dictionary<Vector2Int,Surface> all,int axis,Vector3 a,Vector3 b,Vector3 c)
        {
            int plane=Mathf.RoundToInt((a[axis]+b[axis]+c[axis])/3f/.025f);var key=new Vector2Int(axis,plane);
            Surface surface;if(!all.TryGetValue(key,out surface)){surface=new Surface{axis=axis,plane=plane};all.Add(key,surface);}
            int uaxis=axis==0?2:0;
            var p=new Vector2(a[uaxis],a.y);var q=new Vector2(b[uaxis],b.y);var r=new Vector2(c[uaxis],c.y);
            float area=Cross(q-p,r-p);if(Mathf.Abs(area)<.000001f)return;
            int x0=Mathf.FloorToInt(Mathf.Min(p.x,Mathf.Min(q.x,r.x))/Cell),x1=Mathf.CeilToInt(Mathf.Max(p.x,Mathf.Max(q.x,r.x))/Cell);
            int y0=Mathf.FloorToInt(Mathf.Min(p.y,Mathf.Min(q.y,r.y))/Cell),y1=Mathf.CeilToInt(Mathf.Max(p.y,Mathf.Max(q.y,r.y))/Cell);
            for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)
            {
                var point=new Vector2((x+.5f)*Cell,(y+.5f)*Cell);
                float s=Cross(q-p,point-p)/area,t=Cross(point-p,r-p)/area;
                if(s>=-.0001f && t>=-.0001f && s+t<=1.0001f)surface.cells.Add(new Vector2Int(x,y));
            }
        }
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        static IEnumerable<Bounds> ConnectedBounds(Mesh mesh,Transform transform)
        {
            var vertices=mesh.vertices;var parent=Enumerable.Range(0,vertices.Length).ToArray();
            var welded=new Dictionary<Vector3Int,int>();
            Func<int,int> find=null;find=i=>{while(parent[i]!=i){parent[i]=parent[parent[i]];i=parent[i];}return i;};
            Action<int,int> join=(a,b)=>parent[find(a)]=find(b);
            for(int i=0;i<vertices.Length;i++)
            {
                var v=vertices[i];var key=new Vector3Int(Mathf.RoundToInt(v.x*10000),Mathf.RoundToInt(v.y*10000),Mathf.RoundToInt(v.z*10000));
                int other;if(welded.TryGetValue(key,out other))join(i,other);else welded.Add(key,i);
            }
            var triangles=mesh.triangles;for(int i=0;i<triangles.Length;i+=3){join(triangles[i],triangles[i+1]);join(triangles[i],triangles[i+2]);}
            var groups=new Dictionary<int,Bounds>();
            for(int i=0;i<vertices.Length;i++){int id=find(i);var v=transform.TransformPoint(vertices[i]);Bounds bounds;if(groups.TryGetValue(id,out bounds)){bounds.Encapsulate(v);groups[id]=bounds;}else groups.Add(id,new Bounds(v,Vector3.zero));}
            return groups.Values;
        }
    }
}
