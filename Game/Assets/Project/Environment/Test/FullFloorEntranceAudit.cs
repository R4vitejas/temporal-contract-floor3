using System;
using System.Linq;
using UnityEngine;

namespace TemporalContract.Environment.Testing
{
    public static class FullFloorEntranceAudit
    {
        [Serializable] public sealed class Result { public int passed,total;public string[] failures; }
        public static Result Run(EnvironmentWalkthrough walk)
        {
            if(!Application.isPlaying)throw new InvalidOperationException("Run entrances in Play Mode.");
            var body=walk.GetComponent<CharacterController>();
            if(walk.entrances.Length!=24)throw new InvalidOperationException("Expected 24 full-floor entrance probes.");
            var old=body.transform.position;bool enabled=walk.enabled;int oldLayer=body.gameObject.layer;walk.enabled=false;body.gameObject.layer=2;
            var failures=new System.Collections.Generic.List<string>();var result=new Result();
            try
            {
                foreach(var door in walk.entrances)
                {
                    var threshold=door.threshold;
                    var direction=door.direction;
                    foreach(float sign in new[]{-1f,1f})
                    {
                        // Two small toilet cubicles end at a solid toilet. Probe their clear
                        // approach/return space rather than teleporting the capsule into a fixture.
                        float low=door.lowDistance;
                        float high=door.highDistance;
                        var start=threshold+direction*(sign>0?-low:high);
                        var target=threshold+direction*(sign>0?high:-low);
                        body.enabled=false;
                        RaycastHit ground;
                        if(Physics.Raycast(start+Vector3.up*.8f,Vector3.down,out ground,1.5f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&ground.normal.y>.8f&&ground.collider.name.StartsWith("Surface -",StringComparison.Ordinal))start.y=ground.point.y+.065f;
                        if(Physics.Raycast(target+Vector3.up*.8f,Vector3.down,out ground,1.5f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&ground.normal.y>.8f&&ground.collider.name.StartsWith("Surface -",StringComparison.Ordinal))target.y=ground.point.y+.065f;
                        bool clear=Physics.OverlapCapsule(start+Vector3.up*.31f,start+Vector3.up*1.49f,.3f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Length==0 && Physics.OverlapCapsule(target+Vector3.up*.31f,target+Vector3.up*1.49f,.3f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Length==0;
                        if(!clear){result.total++;failures.Add(door.source+" direction="+sign+" invalid endpoint overlaps geometry");continue;}
                        walk.PlaceForInspection(start);Physics.SyncTransforms();
                        float distance=low+high;
                        for(int i=0;i<30;i++)walk.MoveInspection(direction*sign*(distance/(30f*.016f)),.016f);
                        float error=Vector2.Distance(new Vector2(body.transform.position.x,body.transform.position.z),new Vector2(target.x,target.z));
                        result.total++;if(error<.12f)result.passed++;else failures.Add(door.source+" direction="+sign+" error="+error+" position="+body.transform.position);
                    }
                }
            }
            finally{body.gameObject.layer=oldLayer;walk.PlaceForInspection(old);walk.enabled=enabled;}
            result.failures=failures.ToArray();Debug.Log("FULL_FLOOR_ALL_ENTRANCES "+JsonUtility.ToJson(result));return result;
        }
    }
}
