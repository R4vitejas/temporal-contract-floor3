using System.Collections;
using TemporalContract.Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace TemporalContract.UI.Board.Testing
{
    /// <summary>
    /// TEST-ONLY headless Play Mode regression for the board. Prints BOARD_REGRESSION PASS/FAIL and exits 0 on
    /// pass, 2 on failure. Does not click with a mouse; selection goes through the same method the buttons call.
    /// </summary>
    public sealed class BoardRegression : MonoBehaviour
    {
        [SerializeField] private FloorMapPanel panel;
        [SerializeField] private ClassroomBoard board;
        [SerializeField] private LocalInputCapture capture;
        [SerializeField] private MockSessionMenu sessionMenu;
        [SerializeField] private FloorMapLayout alternateLayout;

        private bool failed;

        private IEnumerator Start()
        {
            yield return null;
            var actor = new MockActor();

            // The board is a local target: no network id, no NetworkBehaviour, nothing to send to a host.
            Check("board target has no network id", board.NetworkObjectId == null);
            Check("board is not a NetworkBehaviour", !(board is NetworkBehaviour) && !(panel is NetworkBehaviour));
            Check("board prompt is available", board.TryGetPrompt(actor, out InteractionPrompt prompt) && prompt.CanInteract);

            // open via the interaction path
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            CursorLockMode lockBefore = Cursor.lockState; // read back: headless Editor may not honor Locked
            bool visibleBefore = Cursor.visible;
            int captureEvents = 0;
            panel.CaptureStateChanged += _ => captureEvents++;
            board.RequestInteraction(new InteractionRequest(0, null));
            Check("E opens the panel", panel.IsOpen);
            Check("opening captures gameplay input", capture.IsGameplayInputCaptured);
            Check("opening unlocks the cursor", Cursor.lockState == CursorLockMode.None && Cursor.visible);

            // room selection, repeatedly
            for (int round = 0; round < 3; round++)
            {
                for (int i = 0; i < panel.Layout.rooms.Length; i++)
                {
                    panel.Select(i);
                    if (panel.SelectedIndex != i)
                    {
                        Check($"select room {i} (round {round})", false);
                    }
                }
            }

            panel.Select(2);
            Check("selected room highlighted and named",
                panel.SelectedIndex == 2 && FindText("RoomName") == panel.Layout.rooms[2].displayName);

            // Escape closes it (real Input System key event) and restores cursor + capture
            yield return PressEscape();
            Check("Escape closes the panel", !panel.IsOpen);
            Check("closing releases gameplay input", !capture.IsGameplayInputCaptured);
            Check("closing restores the previous cursor state", Cursor.lockState == lockBefore && Cursor.visible == visibleBefore);
            Check("capture events balanced (open then close)", captureEvents == 2);

            // repeat open/close
            for (int i = 0; i < 5; i++)
            {
                panel.Open();
                panel.Close();
            }

            Check("repeated open/close leaves input free", !panel.IsOpen && !capture.IsGameplayInputCaptured);
            panel.Close();
            Check("double close is harmless", !capture.IsGameplayInputCaptured);

            // overlapping modal: closing the board must not release the session menu's lock
            sessionMenu.SetOpen(true);
            panel.Open();
            panel.Close();
            Check("closing board keeps another modal's capture", capture.IsGameplayInputCaptured);
            Check("closing board keeps cursor free while menu open", Cursor.lockState == CursorLockMode.None);
            sessionMenu.SetOpen(false);
            Check("menu close releases last capture", !capture.IsGameplayInputCaptured);

            // final artwork swap without code changes
            int before = panel.RegionCount;
            panel.SetLayout(alternateLayout);
            Check("layout swap rebuilds regions", panel.RegionCount == alternateLayout.rooms.Length
                && panel.RegionCount != before);

            // force close (disconnect path) and destroy-while-open cleanup
            panel.Open();
            panel.ForceClose();
            Check("ForceClose releases capture", !panel.IsOpen && !capture.IsGameplayInputCaptured);
            panel.Open();
            Destroy(panel.gameObject);
            yield return null;
            Check("destroying an open panel releases capture", !capture.IsGameplayInputCaptured);

            Finish();
        }

        private IEnumerator PressEscape()
        {
            InputSettings settings = InputSystem.settings;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            // Isolated synthetic keyboard: never depends on the native keyboard, focus or Keyboard.current history.
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>("BoardRegressionKeyboard");
            try
            {
                // Queued only: the next frame's input update processes it before the panel's Update runs,
                // so wasPressedThisFrame is true exactly when the panel polls it.
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                // Give the panel's own Update a few frames to observe the press, stopping as soon as it reacts.
                for (int frame = 0; frame < 5 && panel.IsOpen; frame++)
                {
                    yield return null;
                }

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
            }
        }

        private static string FindText(string objectName)
        {
            foreach (UnityEngine.UI.Text text in FindObjectsByType<UnityEngine.UI.Text>(FindObjectsInactive.Include))
            {
                if (text.gameObject.name == objectName)
                {
                    return text.text;
                }
            }

            return null;
        }

        private sealed class MockActor : IInteractionActor
        {
            public ulong OwnerClientId => 0;
            public Vector3 Position => Vector3.zero;
            public bool IsLocalOwner => true;
        }

        private void Check(string name, bool ok)
        {
            if (!ok)
            {
                failed = true;
            }

            Debug.Log($"BOARD_REGRESSION {(ok ? "PASS" : "FAIL")}: {name}");
        }

        private void Finish()
        {
            Debug.Log($"BOARD_REGRESSION RESULT: {(failed ? "FAIL" : "PASS")}");
            int code = failed ? 2 : 0;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(code);
#else
            Application.Quit(code);
#endif
        }
    }
}
