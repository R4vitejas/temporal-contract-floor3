using System;
using TemporalContract.Core;
using UnityEngine;

namespace TemporalContract.Feedback.Testing
{
    /// <summary>TEST-ONLY: raises feedback events like a real interaction would. Not for production scenes.</summary>
    public sealed class MockFeedbackSource : MonoBehaviour, IFeedbackEventSource
    {
        public event Action<FeedbackEvent> FeedbackRaised;

        public void Raise(FeedbackKind kind, ulong subjectId, float amount = 0f) =>
            FeedbackRaised?.Invoke(new FeedbackEvent(kind, subjectId, amount));
    }
}
