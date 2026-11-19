using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TemporalContract.Environment.Testing
{
    public static class FullFloorCollisionAudit
    {
        [Serializable] public sealed class Report
        {
            public int wallRays,wallRayFailures,wallApproaches,wallEscapeFailures,wallCrossings,slides,slideFailures,gridGroundPoints,gridClearEdges,gridBlockedEdges,gridMovementFailures;
            public List<string> failures=new List<string>();
            public bool passed;
        }
        const int Mask=~(1<<2);
        static CharacterController body;
        static EnvironmentWalkthrough walk;
        static Vector3 Foot(Vector3 position,float ground) => new Vector3(position.x,ground+.065f,position.z);
        static bool Ground(Vector3 p,out float y)
        {
            RaycastHit hit;y=0;
            if(!Physics.Raycast(new Vector3(p.x,2.8f,p.z),Vector3.down,out hit,4f,Mask,QueryTriggerInteraction.Ignore))return false;
            if(hit.normal.y<.8f || hit.point.y<-.65f || hit.point.y>2.4f || !hit.collider.name.StartsWith("Surface -") || hit.collider.name.Contains("Roof"))return false;
            y=hit.point.y;return true;
        }
        static bool Clear(Vector3 foot)
        {
            return Physics.OverlapCapsule(foot+Vector3.up*.31f,foot+Vector3.up*1.49f,.3f,Mask,QueryTriggerInteraction.Ignore).Length==0;
        }
        static bool EdgeClear(Vector3 a,Vector3 b)
        {
            Vector3 delta=b-a;
            return !Physics.CapsuleCast(a+Vector3.up*.31f,a+Vector3.up*1.49f,.305f,delta.normalized,delta.magnitude,Mask,QueryTriggerInteraction.Ignore);
        }
        static void Place(Vector3 p){walk.PlaceForInspection(p);Physics.SyncTransforms();}
        static void Fail(Report r,string description){if(r.failures.Count<100)r.failures.Add(description);}
        public static Report Run()
        {
            if(!Application.isPlaying)throw new InvalidOperationException("Collision movement audit requires Play Mode.");
            body=GameObject.Find("Full floor inspection player").GetComponent<CharacterController>();walk=body.GetComponent<EnvironmentWalkthrough>();
            var oldPosition=body.transform.position;int oldLayer=body.gameObject.layer;bool oldEnabled=walk.enabled;
            walk.enabled=false;body.gameObject.layer=2;body.enabled=false;Physics.SyncTransforms();
            var report=new Report();
            try
            {
                var walls=UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Where(c=>c.name.StartsWith("Solid wall ")).ToArray();
                foreach(var wall in walls)
                {
                    var b=wall.bounds;bool x=b.size.x<b.size.z;Vector3 normal=x?Vector3.right:Vector3.forward,tangent=x?Vector3.forward:Vector3.right;
                    foreach(float sign in new[]{-1f,1f})
                    {
                        RaycastHit hit;report.wallRays++;
                        if(!wall.Raycast(new Ray(b.center+normal*sign*.25f,-normal*sign),out hit,.5f)){report.wallRayFailures++;Fail(report,"wall ray "+wall.name+" "+b.center);}
                        float width=x?b.size.z:b.size.x;
                        if(width<1.2f || b.size.y<1.9f || b.min.y>.25f)continue;
                        Vector3 p=b.center+normal*sign*.65f;float y;
                        if(!Ground(p,out y))continue;p=Foot(p,y);
                        if(!Clear(p))continue;
                        Place(p);for(int i=0;i<20;i++)walk.MoveInspection(-normal*sign*4f,.016f);
                        report.wallApproaches++;
                        if(Vector3.Dot(body.transform.position-b.center,normal*sign)<.15f){report.wallCrossings++;Fail(report,"crossed "+wall.name+" start="+p+" end="+body.transform.position);}
                        Vector3 contact=body.transform.position;
                        for(int i=0;i<10;i++)walk.MoveInspection(normal*sign*4f,.016f);
                        // Return to the known-clear starting position. Extra retreat can correctly
                        // meet a bench or another wall in a narrow aisle; that is not sticking.
                        if(Vector3.Dot(body.transform.position-p,normal*sign)<-.08f){report.wallEscapeFailures++;Fail(report,"escape "+wall.name+" at="+contact+" start="+p+" end="+body.transform.position);}
                        if(width<2.8f)continue;
                        foreach(float along in new[]{-1f,1f})
                        {
                            Vector3 start=Foot(b.center+normal*sign*.4f-tangent*along*.6f,y),end=start+tangent*along*1.1f;
                            body.enabled=false;
                            if(!Clear(start)||!Clear(end)||!EdgeClear(start+normal*sign*.12f,end+normal*sign*.12f))continue;
                            Place(start);
                            for(int i=0;i<20;i++)walk.MoveInspection(tangent*along*4f-normal*sign*1.2f,.016f);
                            report.slides++;
                            if(Vector3.Dot(body.transform.position-start,tangent*along)<.85f){report.slideFailures++;Fail(report,"slide "+wall.name+" start="+start+" end="+body.transform.position);}
                        }
                    }
                }
                body.enabled=false;
                const float spacing=.75f;
                var ground=new Dictionary<Vector2Int,Vector3>();
                for(int z=0;z<64;z++)for(int x=0;x<108;x++)
                {
                    var p=new Vector3(-53f+x*spacing,0,-24f+z*spacing);float y;
                    if(!Ground(p,out y))continue;p=Foot(p,y);
                    if(Clear(p)){ground.Add(new Vector2Int(x,z),p);report.gridGroundPoints++;}
                }
                foreach(var entry in ground)
                    foreach(var step in new[]{Vector2Int.right,Vector2Int.up})
                    {
                        Vector3 target;
                        if(!ground.TryGetValue(entry.Key+step,out target)||Mathf.Abs(target.y-entry.Value.y)>.035f)continue;
                        body.enabled=false;
                        if(!EdgeClear(entry.Value,target)){report.gridBlockedEdges++;continue;}
                        report.gridClearEdges++;
                        Place(entry.Value);Vector3 direction=(target-entry.Value);direction.y=0;
                        for(int i=0;i<12;i++)walk.MoveInspection(direction.normalized*4f,.016f);
                        float error=Vector2.Distance(new Vector2(body.transform.position.x,body.transform.position.z),new Vector2(target.x,target.z));
                        if(error>.12f){report.gridMovementFailures++;Fail(report,"grid "+entry.Value+" to="+target+" end="+body.transform.position+" error="+error);}
                    }
                report.passed=report.wallRays>0 && report.wallApproaches>20 && report.gridClearEdges>1000 && report.wallRayFailures+report.wallCrossings+report.wallEscapeFailures+report.slideFailures+report.gridMovementFailures==0;
                Debug.Log("FULL_FLOOR_COLLISION_AUDIT "+JsonUtility.ToJson(report));return report;
            }
            finally{body.enabled=false;body.gameObject.layer=oldLayer;body.transform.position=oldPosition;body.enabled=true;walk.enabled=oldEnabled;}
        }
    }
}
