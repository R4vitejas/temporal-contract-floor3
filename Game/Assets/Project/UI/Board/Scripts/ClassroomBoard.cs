using TemporalContract.Core;
using UnityEngine;

namespace TemporalContract.UI.Board
{
    /// <summary>
    /// The classroom board in the world. Implements <see cref="IInteractionTarget"/> with a null
    /// <c>NetworkObjectId</c> (local-only), so pressing E on it opens the map on this client only and never sends a
    /// request to the host. Attach to the existing board mesh (which needs a collider on the router's layers).
    /// </summary>
    public sealed class ClassroomBoard : MonoBehaviour, IInteractionTarget
    {
        [SerializeField] private FloorMapPanel panel;
        [SerializeField] private string label = "Classroom board";

        public ulong? NetworkObjectId => null;

        public bool TryGetPrompt(IInteractionActor actor, out InteractionPrompt prompt)
        {
            prompt = new InteractionPrompt(label, "E: floor map", panel != null);
            return true;
        }

        public bool IsLocallyEligible(IInteractionActor actor) => panel != null && actor != null && actor.IsLocalOwner;

        public void RequestInteraction(in InteractionRequest request)
        {
            // Local only: never forward to the host. A null id must not be treated as a shared action.
            if (panel != null)
            {
                panel.Open();
            }
        }
    }
}
