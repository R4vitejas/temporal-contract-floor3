using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TemporalContract.Environment.Testing
{
    /// <summary>Environment-only collision inspection. Never added to Main or production player prefabs.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class EnvironmentWalkthrough : MonoBehaviour
    {
        public Camera view;
        [Serializable] public sealed class EntranceProbe { public string source;public Vector3 threshold,direction;public float lowDistance,highDistance; }
        public EntranceProbe[] entrances=Array.Empty<EntranceProbe>();
        public Vector3[] doorwayProbes = Array.Empty<Vector3>();
        public Vector3[] doorwayDirections = Array.Empty<Vector3>();
        private CharacterController body;
        private float pitch;
        private float verticalSpeed;
        private bool automated;
        public Vector3 homePosition = new Vector3(-2.5f, .05f, 0f);
        public string instructions = "FLOOR ASSET TEST · WASD / mouse · Esc cursor · Home respawn\nDoors fixed open for traversal. Production interactions and multiplayer are tested separately.";
        private void Awake()
        {
            body = GetComponent<CharacterController>();
            body.minMoveDistance=0f;
            body.enableOverlapRecovery=true;
            var args=System.Environment.GetCommandLineArgs();
            automated=Array.IndexOf(args,"--floor-smoke-test")>=0||Array.IndexOf(args,"--floor-production-capsule-test")>=0||Array.IndexOf(args,"--full-floor-smoke-test")>=0||Array.IndexOf(args,"--full-floor-collision-audit")>=0||Array.IndexOf(args,"--full-floor-performance")>=0;
            Cursor.lockState = automated?CursorLockMode.None:CursorLockMode.Locked;
            Cursor.visible = automated;
        }
        private void Start()
        {
            var args=System.Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"--full-floor-collision-audit")>=0){StartCoroutine(CheckCollisionAudit());return;}
            if(Array.IndexOf(args,"--full-floor-performance")>=0){StartCoroutine(MeasurePerformance());return;}
            if(Array.IndexOf(args,"--full-floor-smoke-test")>=0){StartCoroutine(CheckFullFloorTraversal());return;}
            bool productionSize=Array.IndexOf(args,"--floor-production-capsule-test")>=0;
            if(productionSize){body.radius=.5f;body.height=2f;body.center=Vector3.up;}
            if(productionSize||Array.IndexOf(args,"--floor-smoke-test")>=0)
                StartCoroutine(CheckStandaloneTraversal());
        }
        private IEnumerator CheckCollisionAudit()
        {
            yield return new WaitForFixedUpdate();
            var report=FullFloorCollisionAudit.Run();
            string output=System.Environment.GetEnvironmentVariable("FLOOR_COLLISION_AUDIT_OUTPUT");
            if(!string.IsNullOrEmpty(output))System.IO.File.WriteAllText(output,JsonUtility.ToJson(report,true));
            Application.Quit(report.passed?0:1);
        }
        private IEnumerator MeasurePerformance()
        {
            QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            var positions=new[]{new Vector3(-13,1.7f,-7),new Vector3(-14,3.2f,-3),new Vector3(-1,1.7f,-19),new Vector3(-35,1.7f,9)};
            var targets=new[]{new Vector3(10,1.6f,-6),new Vector3(-14,1.5f,7),new Vector3(-3,1.4f,-14),new Vector3(-28,1.5f,9)};
            var names=new[]{"corridor","seminar","classroom","lab"};
            string results="";
            for(int i=0;i<positions.Length;i++)
            {
                body.enabled=false;view.transform.position=positions[i];view.transform.LookAt(targets[i]);
                for(int warm=0;warm<60;warm++)yield return null;
                var frames=new float[180];for(int frame=0;frame<frames.Length;frame++){yield return null;frames[frame]=Time.unscaledDeltaTime;}
                Array.Sort(frames);
                results+=names[i]+" averageFPS="+(frames.Length/frames.Sum()).ToString("F1")+" p99ms="+(frames[178]*1000).ToString("F1")+"; ";
            }
            Debug.Log("FULL_FLOOR_PERFORMANCE "+Screen.width+"x"+Screen.height+" "+SystemInfo.graphicsDeviceName+" "+results);
            Application.Quit(0);
        }
        private IEnumerator CheckFullFloorTraversal()
        {
            yield return new WaitForFixedUpdate();
            try
            {
                var result=FullFloorEntranceAudit.Run(this);
                if(result.passed!=result.total || result.total!=48)throw new InvalidOperationException("Full-floor entrance audit failed.");
                Debug.Log("FULL_FLOOR_STANDALONE_PASS: 48/48 entrance approach/return checks; two toilet cubicles use shorter clear endpoints before their fixtures.");
                Application.Quit(0);
            }
            catch(Exception error){Debug.LogException(error);Application.Quit(1);}
        }
        private IEnumerator CheckStandaloneTraversal()
        {
            yield return new WaitForFixedUpdate();
            string screenshot=System.Environment.GetEnvironmentVariable("FLOOR_SCREENSHOT_OUTPUT");
            if(!string.IsNullOrEmpty(screenshot)&&SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                body.enabled=false;transform.position=new Vector3(4f,.05f,-8f);body.enabled=true;
                transform.rotation=Quaternion.LookRotation(new Vector3(-7f,0f,9f));
                yield return new WaitForEndOfFrame();
                bool captureFailed=false;
                try
                {
                    var captured=ScreenCapture.CaptureScreenshotAsTexture();
                    System.IO.File.WriteAllBytes(screenshot,captured.EncodeToPNG());
                    UnityEngine.Object.Destroy(captured);
                }
                catch(Exception error){Debug.LogException(error);Application.Quit(1);captureFailed=true;}
                if(captureFailed)yield break;
            }
            try
            {
                Physics.SyncTransforms();
                var anchors=UnityEngine.Object.FindFirstObjectByType<FloorIntegrationAnchors>();
                if(anchors==null||anchors.rooms.Length!=4||anchors.boardMap==null)
                    throw new InvalidOperationException("Missing standalone floor references.");
                if(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a=>a.enabled)!=1)
                    throw new InvalidOperationException("The walkthrough must have one active listener.");
                foreach(var room in anchors.rooms)
                {
                    if(room.board==null||room.roomLight==null)throw new InvalidOperationException("Missing room binding.");
                    bool north=room.sourceRoom=="ClassRoom06"||room.sourceRoom=="ClassRoom05";
                    var direction=north?Vector3.forward:Vector3.back;
                    var threshold=room.doorHinge.position+new Vector3(.46707f,.03f,0f);
                    body.enabled=false;transform.position=threshold-direction;body.enabled=true;
                    for(int step=0;step<40;step++)body.Move(direction*.05f);
                    if(Vector3.Distance(transform.position,threshold+direction)>.15f)
                        throw new InvalidOperationException("Standalone traversal blocked: "+room.sourceRoom);
                }
                Debug.Log($"FLOOR_STANDALONE_SMOKE_PASS: four doorways traversed; controller height={body.height:F2} radius={body.radius:F2}; room/map/listener references valid.");
                Application.Quit(0);
            }
            catch(Exception error)
            {
                Debug.LogException(error);Application.Quit(1);
            }
        }
        private void Update()
        {
            if(automated)return;
            var keys = Keyboard.current;
            var mouse = Mouse.current;
            if (keys == null || mouse == null) return;
            if (keys.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            if (keys.homeKey.wasPressedThisFrame)
            {
                body.enabled = false;
                transform.position = homePosition;
                body.enabled = true;
                verticalSpeed = 0f;
            }
            if (Cursor.lockState != CursorLockMode.Locked || !Application.isFocused) return;
            var look = mouse.delta.ReadValue() * .12f;
            transform.Rotate(0f, look.x, 0f);
            pitch = Mathf.Clamp(pitch - look.y, -80f, 80f);
            view.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            var move = new Vector2((keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0),
                (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0));
            move = Vector2.ClampMagnitude(move, 1f);
            MoveInspection((transform.right*move.x+transform.forward*move.y)*4f,Time.deltaTime);
            if(transform.position.y < -5f)Respawn();
        }
        public void MoveInspection(Vector3 horizontalVelocity,float elapsed)
        {
            if(!body)body=GetComponent<CharacterController>();
            // Keep gravity separate from sideways motion: a downward diagonal against a wall
            // otherwise repeatedly invokes step-up at floor/wall seams and low lintels.
            elapsed=Mathf.Clamp(elapsed,0f,.1f);
            int count=Mathf.Max(1,Mathf.CeilToInt(elapsed/.016f));float step=elapsed/count;
            for(int i=0;i<count;i++)
            {
                float allowed=.32f;
                Vector3 head=transform.position+body.center+Vector3.up*(body.height*.5f-body.radius);
                foreach(var origin in new[]{head,head+horizontalVelocity*step})
                    foreach(var hit in Physics.SphereCastAll(origin,body.radius,Vector3.up,.42f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                        if(hit.collider!=body && hit.normal.y<-.1f)allowed=Mathf.Min(allowed,Mathf.Max(0f,hit.distance-.04f));
                body.stepOffset=allowed;
                bool grounded=body.isGrounded;
                body.Move(new Vector3(horizontalVelocity.x,0f,horizontalVelocity.z)*step);
                verticalSpeed=grounded?-2f:Mathf.Max(-30f,verticalSpeed-20f*step);
                var flags=body.Move(Vector3.up*verticalSpeed*step);
                if((flags&CollisionFlags.Above)!=0 && verticalSpeed>0f)verticalSpeed=0f;
            }
        }
        public void Respawn()
        {
            PlaceForInspection(homePosition);
        }
        public void PlaceForInspection(Vector3 position)
        {
            if(!body)body=GetComponent<CharacterController>();
            body.enabled=false;transform.position=position;body.enabled=true;verticalSpeed=0f;body.stepOffset=.05f;
        }
        private void OnGUI()
        {
            GUI.Box(new Rect(12f, 12f, 670f, 54f),
                instructions);
        }
    }
}
