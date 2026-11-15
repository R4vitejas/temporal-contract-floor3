using TemporalContract.Core;
using TemporalContract.Networking;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TemporalContract.Players
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class NetworkFpsPlayer : NetworkBehaviour, IInteractionActor, IGameplayInputCapture
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private AudioListener audioListener;
        [SerializeField, Min(0.1f)] private float moveSpeed = 4.5f;
        [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.12f;
        [SerializeField, Min(0.1f)] private float jumpHeight = 1.15f;

        private CharacterController characterController;
        private float pitch;
        private float verticalSpeed;
        private bool gameplayInputCaptured = true;

        public static NetworkFpsPlayer LocalOwner { get; private set; }
        public ulong OwnerClientId => NetworkObject != null ? NetworkObject.OwnerClientId : ulong.MaxValue;
        public Vector3 Position => transform.position;
        public bool IsLocalOwner => IsOwner;
        public bool IsGameplayInputCaptured => gameplayInputCaptured;

        public override void OnNetworkSpawn()
        {
            characterController = GetComponent<CharacterController>();
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>(true);
            }
            if (audioListener == null)
            {
                audioListener = GetComponentInChildren<AudioListener>(true);
            }

            bool ownsPlayer = IsOwner;
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(ownsPlayer);
            }
            if (audioListener != null)
            {
                audioListener.enabled = ownsPlayer;
            }

            if (ownsPlayer)
            {
                LocalOwner = this;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                InteractionRouter router = GetComponent<InteractionRouter>();
                router?.Configure(playerCamera, this, this);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                if (LocalOwner == this)
                {
                    LocalOwner = null;
                }
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner || gameplayInputCaptured || !Application.isFocused
                || LobbyNetworkState.Instance == null || !LobbyNetworkState.Instance.MatchStarted)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null || mouse == null)
            {
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            Vector2 look = mouse.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(Vector3.up, look.x, Space.World);
            pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
            if (playerCamera != null)
            {
                playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            Vector2 move = Vector2.zero;
            if (keyboard.wKey.isPressed) move.y += 1f;
            if (keyboard.sKey.isPressed) move.y -= 1f;
            if (keyboard.dKey.isPressed) move.x += 1f;
            if (keyboard.aKey.isPressed) move.x -= 1f;
            move = Vector2.ClampMagnitude(move, 1f);

            if (characterController.isGrounded && verticalSpeed < 0f)
            {
                verticalSpeed = -2f;
            }
            if (characterController.isGrounded && keyboard.spaceKey.wasPressedThisFrame)
            {
                verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
            }
            verticalSpeed += Physics.gravity.y * Time.deltaTime;

            Vector3 horizontal = transform.right * move.x + transform.forward * move.y;
            Vector3 velocity = horizontal * moveSpeed + Vector3.up * verticalSpeed;
            characterController.Move(velocity * Time.deltaTime);
        }

        public void SetGameplayInputCaptured(bool captured)
        {
            gameplayInputCaptured = captured;
            if (!IsOwner)
            {
                return;
            }

            bool inMatch = LobbyNetworkState.Instance != null && LobbyNetworkState.Instance.MatchStarted;
            if (captured || !inMatch)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
