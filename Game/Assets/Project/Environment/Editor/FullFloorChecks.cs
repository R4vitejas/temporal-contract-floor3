using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TemporalContract.Environment.EditorTools
{
    public static class FullFloorChecks
    {
        public static string CheckClassroomTraversal()
        {
            if(!Application.isPlaying)throw new InvalidOperationException("Run doorway traversal in Play Mode.");
            var player=GameObject.Find("Full floor inspection player");
            var body=player.GetComponent<CharacterController>();
            var walk=player.GetComponent<TemporalContract.Environment.Testing.EnvironmentWalkthrough>();
            var oldPosition=player.transform.position;
            bool oldWalk=walk.enabled;walk.enabled=false;
            var manifest=JsonUtility.FromJson<FullFloorBuilder.Manifest>(File.ReadAllText("Assets/Project/Environment/Data/full-floor-manifest.json"));
            int passed=0;string report="";
            try
            {
                foreach(var door in manifest.objects.Where(o=>o.source.StartsWith("Door.",StringComparison.Ordinal)&&o.collections.Any(c=>c.StartsWith("ClassRoom",StringComparison.Ordinal))))
                {
                    var threshold=new Vector3((door.min[0]+door.max[0])*.5f-38.98f,.08f,(door.min[1]+door.max[1])*.5f+1f);
                    var across=door.max[0]-door.min[0]>door.max[1]-door.min[1]?Vector3.forward:Vector3.right;
                    foreach(float direction in new[]{-1f,1f})
                    {
                        body.enabled=false;player.transform.position=threshold-across*direction*.85f;body.enabled=true;
                        Physics.SyncTransforms();
                        for(int i=0;i<40;i++)body.Move(across*(.0425f*direction)+Vector3.down*.01f);
                        var target=threshold+across*direction*.85f;
                        float error=Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(target.x,target.z));
                        bool ok=error<.12f;
                        if(ok)passed++;
                        report+=door.collections.First(c=>c.StartsWith("ClassRoom"))+" direction="+direction+" "+(ok?"PASS":"BLOCKED")+" error="+error.ToString("F3")+"; ";
                    }
                }
            }
            finally{body.enabled=false;player.transform.position=oldPosition;body.enabled=true;walk.enabled=oldWalk;}
            report="FULL_FLOOR_DOORWAY_CHECK "+passed+"/12: "+report;
            Debug.Log(report);
            return report;
        }
    }
}
