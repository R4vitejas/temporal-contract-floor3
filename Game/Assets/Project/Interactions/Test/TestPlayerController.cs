using System.Collections.Generic;
using TemporalContract.Core;
using TemporalContract.Networking;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TemporalContract.Interactions.Testing
{
    /// <summary>
    /// TEST-ONLY stand-in for Samatma's player so the interaction scene can be run on its own.
    /// It is not the shared player. Do not reference it from gameplay prefabs or Main.unity.
    /// WASD + mouse look, owner-only camera/input, on-screen prompt and feedback log.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class TestPlayerController : NetworkBehaviour, IInteractionActor, IGameplayInputCapture
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private AudioListener listener;
        [SerializeField] private InteractionRouter router;
        [SerializeField, Min(0.5f)] private float moveSpeed = 4f;
        [SerializeField, Min(0.01f)] private float lookSensitivity = 0.12f;

        private CharacterController controller;
        private float pitch;
        private float verticalVelocity;
        private string promptText = string.Empty;
        private readonly Queue<string> feedbackLog = new Queue<string>();
        private readonly List<IFeedbackEventSource> subscribed = new List<IFeedbackEventSource>();

        public bool IsLocalOwner => IsOwner;
        public Vector3 Position => transform.position;
        public bool IsGameplayInputCaptured => false;

        public override void OnNetworkSpawn()
        {
            controller = GetComponent<CharacterController>();
            bool mine = IsOwner;
            playerCamera.enabled = mine;
            listener.enabled = mine;
            router.enabled = mine;
            if (!mine)
            {
                return;
            }

            // Disable the scene's fallback camera so only one listener is active.
            foreach (Camera other in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (other != playerCamera)
                {
                    other.enabled = false;
                    AudioListener other_listener = other.GetComponent<AudioListener>();
                    if (other_listener != null)
                    {
                        other_listener.enabled = false;
                    }
                }
            }

            controller.enabled = false;
            transform.position = new Vector3(-3f + 3f * (OwnerClientId % 3), 0.1f, -8f);
            controller.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            router.PromptChanged += OnPromptChanged;
            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (behaviour is IFeedbackEventSource source)
                {
                    source.FeedbackRaised += OnFeedback;
                    subscribed.Add(source);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            if (router != null)
            {
                router.PromptChanged -= OnPromptChanged;
            }

            foreach (IFeedbackEventSource source in subscribed)
            {
                source.FeedbackRaised -= OnFeedback;
            }

            subscribed.Clear();
        }

        private void Update()
        {
            if (!IsOwner || !IsSpawned || Keyboard.current == null || Mouse.current == null)
            {
                return;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else if (Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 look = Mouse.current.delta.ReadValue() * lookSensitivity;
                transform.Rotate(0f, look.x, 0f);
                pitch = Mathf.Clamp(pitch - look.y, -80f, 80f);
                playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            Vector2 move = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) move.y += 1f;
            if (Keyboard.current.sKey.isPressed) move.y -= 1f;
            if (Keyboard.current.dKey.isPressed) move.x += 1f;
            if (Keyboard.current.aKey.isPressed) move.x -= 1f;

            verticalVelocity = controller.isGrounded ? -1f : verticalVelocity - 20f * Time.deltaTime;
            Vector3 velocity = (transform.right * move.x + transform.forward * move.y).normalized * moveSpeed;
            velocity.y = verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }

        private void OnPromptChanged(InteractionPrompt prompt)
        {
            promptText = string.IsNullOrEmpty(prompt.Label)
                ? string.Empty
                : $"{prompt.Label}  [{prompt.InputHint}]{(prompt.CanInteract ? string.Empty : "  (unavailable)")}";
        }

        private void OnFeedback(FeedbackEvent feedback)
        {
            feedbackLog.Enqueue($"{feedback.Kind} (obj {feedback.SubjectNetworkObjectId})");
            while (feedbackLog.Count > 6)
            {
                feedbackLog.Dequeue();
            }
        }

        private void OnGUI()
        {
            if (!IsOwner || !IsSpawned)
            {
                return;
            }

            GUI.Label(new Rect(Screen.width / 2f - 150f, Screen.height / 2f + 20f, 300f, 24f), promptText);
            GUI.Label(new Rect(Screen.width / 2f - 4f, Screen.height / 2f - 10f, 20f, 20f), "+");
            GUI.Label(new Rect(10f, 10f, 420f, 24f),
                $"TEST PLAYER  client {OwnerClientId}  (Esc frees cursor, click to recapture)");
            int y = 34;
            foreach (string line in feedbackLog)
            {
                GUI.Label(new Rect(10f, y, 420f, 22f), line);
                y += 20;
            }
        }
    }
}
