using TemporalContract.Core;
using Unity.Netcode;
using UnityEngine;

namespace TemporalContract.Interactions
{
    /// <summary>
    /// The single keycard. The host records exactly one holder; because the host handles requests
    /// one at a time, simultaneous pickups from both players can only ever succeed once.
    /// Only the attacker may pick it up.
    /// </summary>
    public sealed class KeycardPickup : NetworkInteractable
    {
        private const ulong NoHolder = ulong.MaxValue;

        [Tooltip("Mesh/children hidden once collected. Defaults to this GameObject's children.")]
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Collider pickupCollider;

        private readonly NetworkVariable<ulong> holderClientId = new NetworkVariable<ulong>(NoHolder);

        public bool IsCollected => holderClientId.Value != NoHolder;
        public bool IsHeldBy(ulong clientId) => holderClientId.Value == clientId;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                holderClientId.Value = NoHolder;
            }

            holderClientId.OnValueChanged += OnHolderChanged;
            ApplyVisibility();
        }

        public override void OnNetworkDespawn()
        {
            holderClientId.OnValueChanged -= OnHolderChanged;
        }

        private void OnHolderChanged(ulong previous, ulong current) => ApplyVisibility();

        private void ApplyVisibility()
        {
            bool visible = !IsCollected;
            if (visualRoot != null)
            {
                visualRoot.SetActive(visible);
            }

            if (pickupCollider != null)
            {
                pickupCollider.enabled = visible;
            }
        }

        protected override bool TryBuildPrompt(IInteractionActor actor, out InteractionPrompt prompt)
        {
            if (IsCollected)
            {
                prompt = default;
                return false;
            }

            prompt = new InteractionPrompt("Keycard", "E", true);
            return true;
        }

        protected override bool ExecuteOnServer(in InteractionRequest request, NetworkObject player)
        {
            if (IsCollected || !InteractionRoleGate.CanActAsAttacker(request.ActorClientId))
            {
                return false;
            }

            holderClientId.Value = request.ActorClientId;
            BroadcastFeedback(FeedbackKind.KeycardCollected);
            return true;
        }

        protected override void ResetState()
        {
            holderClientId.Value = NoHolder;
        }
    }
}
