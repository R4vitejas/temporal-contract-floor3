using System;
using System.Collections;
using System.Linq;
using TemporalContract.Core;
using TemporalContract.Networking;
using TemporalContract.UI.Board;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace TemporalContract.Environment.Testing
{
    /// <summary>Offline environment demonstration only. Never used as a production network actor.</summary>
    public sealed class FloorShowcaseActor : MonoBehaviour, IInteractionActor
    {
        public EnvironmentWalkthrough walkthrough;
        public LocalInputCapture capture;
        public InteractionRouter router;
        public FloorMapPanel map;
        private InteractionPrompt prompt;
        private int resumeFrame = -1;
        private bool walkthroughWasEnabled;
        public ulong OwnerClientId => 0;
        public Vector3 Position => transform.position;
        public bool IsLocalOwner => true;

        private void OnEnable()
        {
            capture.CaptureChanged += OnCapture;
            router.PromptChanged += OnPrompt;
            if (capture.IsGameplayInputCaptured) OnCapture(true);
        }
        private void OnDisable()
        {
            capture.CaptureChanged -= OnCapture;
            router.PromptChanged -= OnPrompt;
            map.ForceClose();
            if (walkthroughWasEnabled) walkthrough.enabled = true;
            resumeFrame = -1;
        }
        private void OnPrompt(InteractionPrompt value) => prompt = value;
        private void OnCapture(bool captured)
        {
            if (captured)
            {
                if (resumeFrame < 0) walkthroughWasEnabled = walkthrough.enabled;
                walkthrough.enabled = false;
                resumeFrame = -1;
            }
            else resumeFrame = Time.frameCount + 1;
        }
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            {
                if (map.IsOpen) map.Close(); else map.Open();
            }
        }
        private void LateUpdate()
        {
            // The Escape/click that closed the modal must not also affect the walkthrough.
            if (resumeFrame >= 0 && Time.frameCount >= resumeFrame && !capture.IsGameplayInputCaptured)
            {
                walkthrough.enabled = walkthroughWasEnabled;
                resumeFrame = -1;
            }
        }
        private void OnGUI()
        {
            if (map.IsOpen) return;
            GUI.Label(new Rect(Screen.width / 2f - 5, Screen.height / 2f - 10, 20, 24), "+");
            if (!string.IsNullOrEmpty(prompt.Label))
                GUI.Box(new Rect(Screen.width / 2f - 180, Screen.height - 85, 360, 42), prompt.Label + " · " + prompt.InputHint);
        }
        private IEnumerator Start()
        {
            if (!System.Environment.GetCommandLineArgs().Contains("--floor-showcase-test")) yield break;
            yield return null;
            bool failed = false;
            try
            {
                if (FindObjectsByType<Unity.Netcode.NetworkManager>(FindObjectsSortMode.None).Length != 0)
                    throw new InvalidOperationException("Offline showcase contains a server.");
                if (map.Layout.rooms.Length != 11 || map.RegionCount != 11)
                    throw new InvalidOperationException("Full-floor map regions missing.");
                var boards = FindObjectsByType<ClassroomBoard>(FindObjectsSortMode.None);
                if (boards.Length != 6) throw new InvalidOperationException("Six classroom boards required.");
                foreach (var board in boards)
                {
                    if (!board.IsLocallyEligible(this)) throw new InvalidOperationException("Board has no panel binding.");
                    board.RequestInteraction(new InteractionRequest(0, null));
                    if (!map.IsOpen || !capture.IsGameplayInputCaptured || walkthrough.enabled)
                        throw new InvalidOperationException("Board failed to capture movement.");
                    for (int i = 0; i < 11; i++) { map.Select(i); if (map.SelectedIndex != i) throw new InvalidOperationException("Region selection failed."); }
                    map.Close();
                    if (capture.IsGameplayInputCaptured) throw new InvalidOperationException("Map did not release capture.");
                }
            }
            catch (Exception e) { Debug.LogException(e); failed = true; }
            yield return null; yield return null;
            if (!walkthrough.enabled) failed = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            var keyboard = InputSystem.AddDevice<Keyboard>("ShowcaseRegressionKeyboard");
            try
            {
                foreach (var board in FindObjectsByType<ClassroomBoard>(FindObjectsSortMode.None))
                {
                    var bounds = board.GetComponent<BoxCollider>().bounds;
                    Vector3 normal = bounds.size.x < bounds.size.z ? Vector3.right : Vector3.forward;
                    // Opposite classroom wings put their boards on opposite wall faces.
                    if (!Physics.Raycast(bounds.center + normal, -normal, out RaycastHit hit, 2f, ~0, QueryTriggerInteraction.Ignore)
                        || hit.collider.GetComponentInParent<ClassroomBoard>() != board) normal = -normal;
                    walkthrough.PlaceForInspection(bounds.center + normal - Vector3.up * walkthrough.view.transform.localPosition.y);
                    walkthrough.view.transform.LookAt(bounds.center);
                    Physics.SyncTransforms();
                    yield return null;
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
                    for (int i = 0; i < 5 && !map.IsOpen; i++) yield return null;
                    if (!map.IsOpen || walkthrough.enabled) { failed = true; Debug.LogError("E targeting failed: " + board.name); }
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                    for (int i = 0; i < 5 && map.IsOpen; i++) yield return null;
                    if (map.IsOpen || capture.IsGameplayInputCaptured) { failed = true; Debug.LogError("Escape release failed: " + board.name); map.Close(); }
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null;
                    if (!walkthrough.enabled) { failed = true; Debug.LogError("Movement did not resume after Escape."); }
                }
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.M));
                for (int i = 0; i < 5 && !map.IsOpen; i++) yield return null;
                if (!map.IsOpen) { failed = true; Debug.LogError("M map shortcut failed."); }
                map.Close();
            }
            finally { InputSystem.RemoveDevice(keyboard); }
            if (!failed) Debug.Log("FLOOR_SHOWCASE_PASS: six real boards via raycast/E, 11 regions, Escape/capture/movement resume, M shortcut; no server dependency.");
            Application.Quit(failed ? 1 : 0);
        }
    }
}
