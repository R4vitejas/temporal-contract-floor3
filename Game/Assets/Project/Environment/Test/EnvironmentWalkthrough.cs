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
            var args=System.Environment.GetCommandLineArgs();
            automated=Array.IndexOf(args,"--floor-smoke-test")>=0||Array.IndexOf(args,"--floor-production-capsule-test")>=0||Array.IndexOf(args,"--full-floor-smoke-test")>=0;
            Cursor.lockState = automated?CursorLockMode.None:CursorLockMode.Locked;
            Cursor.visible = automated;
        }
        private void Start()
        {
            var args=System.Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"--full-floor-smoke-test")>=0){StartCoroutine(CheckFullFloorTraversal());return;}
            bool productionSize=Array.IndexOf(args,"--floor-production-capsule-test")>=0;
            if(productionSize){body.radius=.5f;body.height=2f;body.center=Vector3.up;}
            if(productionSize||Array.IndexOf(args,"--floor-smoke-test")>=0)
                StartCoroutine(CheckStandaloneTraversal());
        }
        private IEnumerator CheckFullFloorTraversal()
        {
            yield return new WaitForFixedUpdate();
            try
            {
                if(doorwayProbes.Length!=6 || doorwayDirections.Length!=6)throw new InvalidOperationException("Missing full-floor doorway probes.");
                Physics.SyncTransforms();
                for(int i=0;i<doorwayProbes.Length;i++)foreach(float sign in new[]{-1f,1f})
                {
                    Vector3 direction=doorwayDirections[i]*sign;
                    body.enabled=false;transform.position=doorwayProbes[i]-direction*.85f;body.enabled=true;
                    for(int step=0;step<40;step++)body.Move(direction*.0425f+Vector3.down*.01f);
                    Vector3 target=doorwayProbes[i]+direction*.85f;
                    if(Vector2.Distance(new Vector2(transform.position.x,transform.position.z),new Vector2(target.x,target.z))>.12f)
                        throw new InvalidOperationException("Full-floor standalone doorway blocked: "+i+" direction="+sign);
                }
                Debug.Log("FULL_FLOOR_STANDALONE_PASS: 12/12 classroom doorway traversals; inspection capsule height=1.8 radius=0.3 step=0.05.");
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
            verticalSpeed = body.isGrounded ? -1f : verticalSpeed - 20f * Time.deltaTime;
            body.Move(((transform.right * move.x + transform.forward * move.y) * 4f
                + Vector3.up * verticalSpeed) * Time.deltaTime);
        }
        private void OnGUI()
        {
            GUI.Box(new Rect(12f, 12f, 670f, 54f),
                instructions);
        }
    }
}
