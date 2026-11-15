using TemporalContract.Core;
using Unity.Netcode;
using UnityEngine;

namespace TemporalContract.Interactions
{
    /// <summary>
    /// Wall panel for the restricted door. Denies anyone who does not hold the keycard; on success
    /// the host unlocks the door for the rest of the round (cleared by round reset).
    /// </summary>
    public sealed class AccessPanel : NetworkInteractable
    {
        [SerializeField] private KeycardPickup keycard;
        [SerializeField] private NetworkDoor restrictedDoor;

        private readonly NetworkVariable<bool> granted = new NetworkVariable<bool>(false);

        public bool IsGranted => granted.Value;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                granted.Value = false;
            }
        }

        protected override bool TryBuildPrompt(IInteractionActor actor, out InteractionPrompt prompt)
        {
            if (granted.Value)
            {
                prompt = new InteractionPrompt("Access granted", string.Empty, false);
            }
            else if (keycard != null && actor != null && keycard.IsHeldBy(actor.OwnerClientId))
            {
                prompt = new InteractionPrompt("Use keycard", "E", true);
            }
            else
            {
                prompt = new InteractionPrompt("Access panel", "Keycard required", false);
            }

            return true;
        }

        protected override bool ExecuteOnServer(in InteractionRequest request, NetworkObject player)
        {
            if (keycard == null || restrictedDoor == null || granted.Value)
            {
                return false;
            }

            // Denied path (no card) returns false; the base class sends InteractionDenied to the requester.
            if (!keycard.IsHeldBy(request.ActorClientId))
            {
                return false;
            }

            granted.Value = true;
            restrictedDoor.SetLockedServer(false);
            BroadcastFeedback(FeedbackKind.InteractionAccepted);
            return true;
        }

        protected override void ResetState()
        {
            // The door and keycard reset themselves as their own round participants.
            granted.Value = false;
        }
    }
}
