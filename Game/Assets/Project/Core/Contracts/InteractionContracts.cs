using UnityEngine;

namespace TemporalContract.Core
{
    /// <summary>Identity and local state for the player currently using an interaction router.</summary>
    public interface IInteractionActor
    {
        ulong OwnerClientId { get; }
        Vector3 Position { get; }
        bool IsLocalOwner { get; }
    }

    /// <summary>A shared target can present local hints and route requests to its authority.</summary>
    public interface IInteractionTarget
    {
        /// <summary>Null for local-only targets such as the classroom board.</summary>
        ulong? NetworkObjectId { get; }
        bool TryGetPrompt(IInteractionActor actor, out InteractionPrompt prompt);
        bool IsLocallyEligible(IInteractionActor actor);
        void RequestInteraction(in InteractionRequest request);
    }

    /// <summary>Implemented by the authoritative object after transport/RPC delivery.</summary>
    public interface IInteractionRequestHandler
    {
        bool TryHandleInteraction(in InteractionRequest request);
    }

    /// <summary>Participates in the host's round-boundary reset orchestration.</summary>
    public interface IRoundResetParticipant
    {
        void ResetForRound(in RoundResetContext context);
    }

    /// <summary>Publishes presentation-only feedback. Subscribers must not decide game outcomes.</summary>
    public interface IFeedbackEventSource
    {
        event System.Action<FeedbackEvent> FeedbackRaised;
    }

    /// <summary>UI uses this contract to tell local gameplay systems to release input.</summary>
    public interface IGameplayInputCapture
    {
        bool IsGameplayInputCaptured { get; }
    }
}
