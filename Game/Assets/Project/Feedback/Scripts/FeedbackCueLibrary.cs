using System;
using TemporalContract.Core;
using UnityEngine;

namespace TemporalContract.Feedback
{
    /// <summary>Maps feedback events to sounds. Swap clips here; no code changes needed.</summary>
    [CreateAssetMenu(menuName = "Temporal Contract/Feedback Cue Library", fileName = "FeedbackCueLibrary")]
    public sealed class FeedbackCueLibrary : ScriptableObject
    {
        [Serializable]
        public struct Cue
        {
            public FeedbackKind kind;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
            [Tooltip("Played at the subject's world position with distance attenuation. Off = local 2D sound.")]
            public bool positional;
            [Min(0.1f)] public float minDistance;
            [Min(1f)] public float maxDistance;
            [Range(0f, 0.3f)] public float pitchVariance;
        }

        [Header("Shared-world events (positional) and local confirmations (2D)")]
        public Cue[] cues = Array.Empty<Cue>();

        [Header("Combat presentation (not FeedbackKind events)")]
        public Cue weaponFire;
        public Cue hitConfirm;
        public Cue damageTaken;

        public bool TryGet(FeedbackKind kind, out Cue cue)
        {
            foreach (Cue candidate in cues)
            {
                if (candidate.kind == kind && candidate.clip != null)
                {
                    cue = candidate;
                    return true;
                }
            }

            cue = default;
            return false;
        }
    }
}
