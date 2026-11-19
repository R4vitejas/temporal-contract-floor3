using System.Collections.Generic;
using TemporalContract.Combat;
using TemporalContract.Rounds;
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
        private bool alive = true;
        private readonly HashSet<Object> inputCaptureOwners = new HashSet<Object>();
        private NetworkPlayerHealth health;

        public static NetworkFpsPlayer LocalOwner { get; private set; }
        public new ulong OwnerClientId => NetworkObject != null ? NetworkObject.OwnerClientId : ulong.MaxValue;
        public Vector3 Position => transform.position;
        public bool IsLocalOwner => IsOwner;
        public bool IsGameplayInputCaptured => inputCaptureOwners.Count > 0
            || !alive || (RoundCoordinator.Instance != null
                ? !RoundCoordinator.Instance.IsGameplayActive
                : LobbyNetworkState.Instance == null || !LobbyNetworkState.Instance.MatchStarted);
        public float CameraPitch => pitch;

        public override void OnNetworkSpawn()
        {
            characterController = GetComponent<CharacterController>();
            if (GameplaySceneLayout.Active) GameplaySceneLayout.Active.Configure(characterController);
            health = GetComponent<NetworkPlayerHealth>();
            alive = health != null && health.IsAlive;
            characterController.enabled = alive;
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>(true);
            }
            if (audioListener == null)
            {
                audioListener = GetComponentInChildren<AudioListener>(true);
            }

            bool ownsPlayer = IsOwner;
            if (IsServer && OwnerClientId == NetworkManager.ServerClientId)
            {
                ApplySpawnPosition(attacker: true);
            }
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
            PruneDestroyedCaptureOwners();
            if (!IsSpawned || !IsOwner || IsGameplayInputCaptured || !Application.isFocused)
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
            MoveHorizontal(move,Time.deltaTime);
        }
        private void MoveHorizontal(Vector2 move,float elapsed)
        {
            Vector3 horizontal = transform.right * move.x + transform.forward * move.y;
            if (GameplaySceneLayout.Active)
                FloorCharacterMotor.Move(characterController, ref verticalSpeed, horizontal*moveSpeed, elapsed, Physics.gravity.y);
            else
            {
                verticalSpeed += Physics.gravity.y * elapsed;
                characterController.Move((horizontal * moveSpeed + Vector3.up * verticalSpeed)*elapsed);
            }
        }

        /// <summary>Acquire or release a modal input lock. Use the owning UI component as the key.</summary>
        public void SetGameplayInputCaptured(Object owner, bool captured)
        {
            if (owner == null)
            {
                return;
            }
            if (captured)
            {
                inputCaptureOwners.Add(owner);
            }
            else
            {
                inputCaptureOwners.Remove(owner);
            }

            if (!IsOwner)
            {
                return;
            }

            if (IsGameplayInputCaptured)
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

        public void PlaceAtSpawn(bool attacker)
        {
            if (!IsServer)
            {
                return;
            }

            if (IsOwner)
            {
                ApplySpawnPosition(attacker);
            }
            else
            {
                PlaceAtSpawnRpc(attacker);
            }
        }

        [Rpc(SendTo.Owner)]
        private void PlaceAtSpawnRpc(bool attacker, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId)
            {
                return;
            }
            ApplySpawnPosition(attacker);
        }

        private void ApplySpawnPosition(bool attacker)
        {
            bool enabled=characterController!=null&&characterController.enabled;
            if(characterController!=null)characterController.enabled=false;
            var pose=GameplaySceneLayout.PlayerPose(attacker);
            transform.SetPositionAndRotation(pose.position,pose.rotation);
            verticalSpeed = 0f;
            pitch=0;
            if(playerCamera)playerCamera.transform.localRotation=Quaternion.identity;
            if(characterController!=null)characterController.enabled=enabled;
        }

        public void SetAliveState(bool isAlive)
        {
            alive = isAlive;
            if (characterController != null)
            {
                characterController.enabled = isAlive;
            }
            if (!isAlive && IsOwner)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void PruneDestroyedCaptureOwners()
        {
            inputCaptureOwners.RemoveWhere(owner => owner == null);
        }
    }
}
