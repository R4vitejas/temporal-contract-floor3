using TemporalContract.Core;
using TemporalContract.Networking;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TemporalContract.UI.Board.Testing
{

    /// <summary>
    /// TEST-ONLY mock actor for BoardTest.unity: a minimal offline first-person player that honors the shared
    /// gameplay-input capture (no move/look/interact while a modal is open). It is not the shared player.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class BoardTestPlayer : MonoBehaviour, IInteractionActor
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private InteractionRouter router;
        [SerializeField] private LocalInputCapture capture;

        private CharacterController controller;
        private float pitch;
        private float vertical;
        private string prompt = string.Empty;

        public ulong OwnerClientId => 0;
        public Vector3 Position => transform.position;
        public bool IsLocalOwner => true;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            router.PromptChanged += OnPrompt;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            router.PromptChanged -= OnPrompt;
        }

        private void OnPrompt(InteractionPrompt p) =>
            prompt = string.IsNullOrEmpty(p.Label) ? string.Empty : $"{p.Label}  [{p.InputHint}]";

        private void Update()
        {
            if (Keyboard.current == null || Mouse.current == null || capture.IsGameplayInputCaptured)
            {
                return; // modal open: no move, look or interact (the router also hides its prompt)
            }

            Vector2 look = Mouse.current.delta.ReadValue() * 0.12f;
            transform.Rotate(0f, look.x, 0f);
            pitch = Mathf.Clamp(pitch - look.y, -80f, 80f);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            Vector2 move = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) move.y += 1f;
            if (Keyboard.current.sKey.isPressed) move.y -= 1f;
            if (Keyboard.current.dKey.isPressed) move.x += 1f;
            if (Keyboard.current.aKey.isPressed) move.x -= 1f;
            vertical = controller.isGrounded ? -1f : vertical - 20f * Time.deltaTime;
            Vector3 velocity = (transform.right * move.x + transform.forward * move.y).normalized * 4f;
            velocity.y = vertical;
            controller.Move(velocity * Time.deltaTime);
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(Screen.width / 2f - 150f, Screen.height / 2f + 20f, 300f, 24f), prompt);
            GUI.Label(new Rect(Screen.width / 2f - 4f, Screen.height / 2f - 10f, 20f, 20f), capture.IsGameplayInputCaptured ? string.Empty : "+");
            GUI.Label(new Rect(10f, 10f, 900f, 22f),
                $"BOARD TEST (offline mock actor) | input captured: {capture.IsGameplayInputCaptured} | E on the board opens the map, Esc closes, M = mock session menu");
        }
    }
}
