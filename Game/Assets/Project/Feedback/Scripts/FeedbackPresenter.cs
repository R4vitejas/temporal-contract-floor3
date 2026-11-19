using System;
using System.Collections.Generic;
using TemporalContract.Core;
using UnityEngine;

namespace TemporalContract.Feedback
{
    /// <summary>
    /// Presentation-only listener for <see cref="IFeedbackEventSource"/> events. It plays positional sounds
    /// at the subject's world position and local 2D confirmations for the requester. It never changes
    /// authoritative state. Sources are discovered automatically (spawn/despawn safe) or registered by hand.
    /// </summary>
    public sealed class FeedbackPresenter : MonoBehaviour
    {
        public readonly struct PlayedCue
        {
            public PlayedCue(FeedbackKind kind, bool positional, Vector3 position, float time)
            {
                Kind = kind;
                Positional = positional;
                Position = position;
                Time = time;
            }

            public FeedbackKind Kind { get; }
            public bool Positional { get; }
            public Vector3 Position { get; }
            public float Time { get; }
        }

        [SerializeField] private FeedbackCueLibrary library;
        [SerializeField, Range(2, 32)] private int voices = 12;
        [Tooltip("Identical events inside this window are played once (host sees local plus replicated copies).")]
        [SerializeField, Min(0f)] private float duplicateWindowSeconds = 0.08f;
        [SerializeField] private bool autoDiscoverSources = true;
        [SerializeField, Min(0.25f)] private float rescanSeconds = 1.5f;
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;

        private readonly List<IFeedbackEventSource> sources = new List<IFeedbackEventSource>();
        private readonly List<PlayedCue> playLog = new List<PlayedCue>();
        private readonly Dictionary<(FeedbackKind, ulong, float), float> recent = new Dictionary<(FeedbackKind, ulong, float), float>();
        private AudioSource[] pool;
        private int nextVoice;
        private float nextRescan;
        private ISubjectPositionResolver resolver = new NetworkSubjectResolver();

        public IReadOnlyList<PlayedCue> PlayLog => playLog;
        public int SkippedUnresolved { get; private set; }
        public int SkippedDuplicates { get; private set; }
        public int SourceCount => sources.Count;

        public void SetResolver(ISubjectPositionResolver newResolver) =>
            resolver = newResolver ?? new NetworkSubjectResolver();

        public void SetLibrary(FeedbackCueLibrary newLibrary) => library = newLibrary;

        private void Awake()
        {
            pool = new AudioSource[voices];
            for (int i = 0; i < pool.Length; i++)
            {
                var go = new GameObject($"FeedbackVoice{i}");
                go.transform.SetParent(transform, false);
                AudioSource source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                pool[i] = source;
            }
        }

        private void OnEnable()
        {
            if (autoDiscoverSources)
            {
                Rescan();
            }
        }

        private void OnDisable()
        {
            foreach (IFeedbackEventSource source in sources)
            {
                source.FeedbackRaised -= Handle;
            }

            sources.Clear();
        }

        private void Update()
        {
            if (!autoDiscoverSources || Time.unscaledTime < nextRescan)
            {
                return;
            }

            nextRescan = Time.unscaledTime + rescanSeconds;
            Rescan();
        }

        public void Register(IFeedbackEventSource source)
        {
            if (source == null || sources.Contains(source))
            {
                return;
            }

            sources.Add(source);
            source.FeedbackRaised += Handle;
        }

        public void Unregister(IFeedbackEventSource source)
        {
            if (source == null || !sources.Remove(source))
            {
                return;
            }

            source.FeedbackRaised -= Handle;
        }

        /// <summary>Drop destroyed sources, then subscribe to any new ones (handles spawn/despawn and scene reloads).</summary>
        public void Rescan()
        {
            for (int i = sources.Count - 1; i >= 0; i--)
            {
                if (sources[i] is UnityEngine.Object unityObject && unityObject == null)
                {
                    sources.RemoveAt(i);
                }
            }

            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>())
            {
                if (behaviour is IFeedbackEventSource source)
                {
                    Register(source);
                }
            }
        }

        public void Handle(FeedbackEvent feedback)
        {
            if (library == null || !library.TryGet(feedback.Kind, out FeedbackCueLibrary.Cue cue))
            {
                return;
            }

            if (IsDuplicate(feedback))
            {
                SkippedDuplicates++;
                return;
            }

            if (!cue.positional)
            {
                PlayLocal(feedback.Kind, cue);
                return;
            }

            if (!resolver.TryGetPosition(feedback.SubjectNetworkObjectId, out Vector3 position))
            {
                SkippedUnresolved++; // subject despawned or not spawned yet: skip, never throw
                return;
            }

            Play(feedback.Kind, cue, position, true);
        }

        public void PlayCombat(FeedbackCueLibrary.Cue cue, Vector3 position, bool positional)
        {
            if (cue.clip == null)
            {
                return;
            }

            if (positional)
            {
                Play(FeedbackKind.DamageReceived, cue, position, true);
            }
            else
            {
                PlayLocal(FeedbackKind.DamageReceived, cue);
            }
        }

        private bool IsDuplicate(FeedbackEvent feedback)
        {
            float now = Time.unscaledTime;
            var key = (feedback.Kind, feedback.SubjectNetworkObjectId, feedback.Amount);
            if (recent.TryGetValue(key, out float last) && now - last < duplicateWindowSeconds)
            {
                return true;
            }

            recent[key] = now;
            if (recent.Count > 64)
            {
                recent.Clear();
            }

            return false;
        }

        private void PlayLocal(FeedbackKind kind, FeedbackCueLibrary.Cue cue)
        {
            AudioListener listener = FindListener();
            Play(kind, cue, listener != null ? listener.transform.position : Vector3.zero, false);
        }

        private static AudioListener FindListener()
        {
            foreach (AudioListener listener in FindObjectsByType<AudioListener>())
            {
                if (listener.enabled && listener.gameObject.activeInHierarchy)
                {
                    return listener;
                }
            }

            return null;
        }

        private void Play(FeedbackKind kind, FeedbackCueLibrary.Cue cue, Vector3 position, bool positional)
        {
            AudioSource voice = pool[nextVoice];
            nextVoice = (nextVoice + 1) % pool.Length;
            voice.transform.position = position;
            voice.clip = cue.clip;
            voice.volume = Mathf.Clamp01(cue.volume) * masterVolume;
            voice.spatialBlend = positional ? 1f : 0f;
            voice.minDistance = Mathf.Max(0.1f, cue.minDistance);
            voice.maxDistance = Mathf.Max(voice.minDistance + 0.1f, cue.maxDistance);
            voice.pitch = 1f + UnityEngine.Random.Range(-cue.pitchVariance, cue.pitchVariance);
            voice.Play();

            playLog.Add(new PlayedCue(kind, positional, position, Time.unscaledTime));
            if (playLog.Count > 64)
            {
                playLog.RemoveAt(0);
            }
        }

        public void ClearLog()
        {
            playLog.Clear();
            SkippedDuplicates = 0;
            SkippedUnresolved = 0;
        }
    }
}
