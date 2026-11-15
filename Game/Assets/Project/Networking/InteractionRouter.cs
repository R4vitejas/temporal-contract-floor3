using System;
using TemporalContract.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TemporalContract.Networking
{
    /// <summary>
    /// Local camera targeting and intent routing. This component never grants shared outcomes;
    /// the target's host-side handler must validate actor, distance, role, and object state.
    /// </summary>
    public sealed class InteractionRouter : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private MonoBehaviour actorComponent;
        [SerializeField] private MonoBehaviour inputCaptureComponent;
        [SerializeField, Min(0.1f)] private float maxDistance = 3f;
        [SerializeField] private LayerMask targetLayers = ~0;

        private IInteractionActor actor;
        private IGameplayInputCapture inputCapture;
        private IInteractionTarget currentTarget;
        private InteractionPrompt currentPrompt;
        private bool hasPrompt;

        public event Action<InteractionPrompt> PromptChanged;

        public void Configure(Camera camera, MonoBehaviour actorComponent, MonoBehaviour inputCaptureComponent)
        {
            playerCamera = camera;
            this.actorComponent = actorComponent;
            this.inputCaptureComponent = inputCaptureComponent;
            actor = actorComponent as IInteractionActor;
            inputCapture = inputCaptureComponent as IGameplayInputCapture;
        }

        private void Awake()
        {
            actor = actorComponent as IInteractionActor;
            inputCapture = inputCaptureComponent as IGameplayInputCapture;
        }

        private void Update()
        {
            if (actor == null || !actor.IsLocalOwner || playerCamera == null || IsInputCaptured())
            {
                ClearTarget();
                return;
            }

            UpdateTarget();

            if (currentTarget != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
                && currentTarget.IsLocallyEligible(actor))
            {
                currentTarget.RequestInteraction(
                    new InteractionRequest(actor.OwnerClientId, currentTarget.NetworkObjectId));
            }
        }

        private bool IsInputCaptured() => inputCapture != null && inputCapture.IsGameplayInputCaptured;

        private void UpdateTarget()
        {
            IInteractionTarget nextTarget = null;
            InteractionPrompt nextPrompt = default;

            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward,
                    out RaycastHit hit, maxDistance, targetLayers, QueryTriggerInteraction.Ignore))
            {
                MonoBehaviour[] behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>(true);
                foreach (MonoBehaviour behaviour in behaviours)
                {
                    if (behaviour is IInteractionTarget candidate
                        && candidate.TryGetPrompt(actor, out nextPrompt))
                    {
                        nextTarget = candidate;
                        break;
                    }
                }
            }

            bool changed = !ReferenceEquals(currentTarget, nextTarget)
                || !hasPrompt || !currentPrompt.Equals(nextPrompt);
            currentTarget = nextTarget;
            currentPrompt = nextPrompt;
            hasPrompt = nextTarget != null;
            if (changed)
            {
                PromptChanged?.Invoke(nextTarget == null ? default : nextPrompt);
            }
        }

        private void ClearTarget()
        {
            if (currentTarget == null && !hasPrompt)
            {
                return;
            }

            currentTarget = null;
            hasPrompt = false;
            currentPrompt = default;
            PromptChanged?.Invoke(default);
        }
    }
}
