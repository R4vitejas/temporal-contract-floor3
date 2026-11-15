using UnityEngine;
using UnityEngine.InputSystem;

namespace TemporalContract.Environment.Testing
{
    /// <summary>Environment-only collision inspection. Never added to Main or production player prefabs.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class EnvironmentWalkthrough : MonoBehaviour
    {
        public Camera view;
        private CharacterController body;
        private float pitch;
        private float verticalSpeed;
        private readonly Vector3 start = new Vector3(-2.5f, .05f, 0f);
        private void Awake()
        {
            body = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        private void Update()
        {
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
                transform.position = start;
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
                "FLOOR ASSET TEST · WASD / mouse · Esc cursor · Home respawn\nDoors fixed open for traversal. Production interactions and multiplayer are tested separately.");
        }
    }
}
