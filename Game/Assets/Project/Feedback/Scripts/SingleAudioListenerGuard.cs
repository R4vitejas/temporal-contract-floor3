using UnityEngine;

namespace TemporalContract.Feedback
{
    /// <summary>Reports (never silently fixes) more than one enabled AudioListener, which breaks spatial audio.</summary>
    public sealed class SingleAudioListenerGuard : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float checkSeconds = 2f;
        private float next;
        private int lastReported = -1;

        public int EnabledListeners { get; private set; }

        private void Update()
        {
            if (Time.unscaledTime < next)
            {
                return;
            }

            next = Time.unscaledTime + checkSeconds;
            Check();
        }

        public int Check()
        {
            int count = 0;
            foreach (AudioListener listener in FindObjectsByType<AudioListener>())
            {
                if (listener.enabled && listener.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            EnabledListeners = count;
            if (count > 1 && count != lastReported)
            {
                Debug.LogError($"[Feedback] {count} enabled AudioListeners; only the local player's may be active.", this);
            }

            lastReported = count;
            return count;
        }
    }
}
