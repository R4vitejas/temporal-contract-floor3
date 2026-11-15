using System;

namespace TemporalContract.Core
{
    public enum FeedbackKind
    {
        InteractionDenied,
        InteractionAccepted,
        DoorMoved,
        KeycardCollected,
        LightToggled,
        CoverPickedUp,
        CoverPlaced,
        DamageReceived,
        RoundStarted,
        RoundEnded,
        MatchEnded
    }

    /// <summary>Presentation signal; it carries no authority to mutate replicated state.</summary>
    public readonly struct FeedbackEvent
    {
        public FeedbackEvent(FeedbackKind kind, ulong subjectNetworkObjectId, float amount = 0f)
        {
            Kind = kind;
            SubjectNetworkObjectId = subjectNetworkObjectId;
            Amount = amount;
        }

        public FeedbackKind Kind { get; }
        public ulong SubjectNetworkObjectId { get; }
        public float Amount { get; }
    }

    public readonly struct InteractionPrompt : IEquatable<InteractionPrompt>
    {
        public InteractionPrompt(string label, string inputHint, bool canInteract)
        {
            Label = label ?? string.Empty;
            InputHint = inputHint ?? string.Empty;
            CanInteract = canInteract;
        }

        public string Label { get; }
        public string InputHint { get; }
        public bool CanInteract { get; }

        public bool Equals(InteractionPrompt other) =>
            Label == other.Label && InputHint == other.InputHint && CanInteract == other.CanInteract;

        public override bool Equals(object obj) => obj is InteractionPrompt other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Label, InputHint, CanInteract);
    }
}
