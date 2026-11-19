using TemporalContract.Core;
using TemporalContract.Networking;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TemporalContract.UI.Board.Testing
{
    /// <summary>TEST-ONLY mock modal (stands in for the session menu) to prove overlapping modals keep their own lock.</summary>
    public sealed class MockSessionMenu : MonoBehaviour
    {
        [SerializeField] private LocalInputCapture capture;
        private bool open;

        public bool IsOpen => open;

        public void SetOpen(bool value)
        {
            if (open == value)
            {
                return;
            }

            open = value;
            capture.SetCaptured(this, open);
            if (open)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            {
                SetOpen(!open);
            }
        }

        private void OnGUI()
        {
            if (!open)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(20f, 60f, 260f, 90f), GUI.skin.box);
            GUILayout.Label("Mock session menu (modal)");
            if (GUILayout.Button("Close (M)"))
            {
                SetOpen(false);
            }

            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            if (capture != null)
            {
                capture.SetCaptured(this, false);
            }
        }
    }
}
