using UnityEngine;
using UnityEngine.Events;

namespace TemporalContract.Feedback
{
    /// <summary>
    /// Restrained, configurable combat presentation: muzzle flash, small camera recoil, hit marker and a
    /// damage flash. It never applies damage or touches shared state. Samatma's weapon/health code (or a
    /// small adapter) calls <see cref="NotifyShot"/>, <see cref="NotifyHitConfirmed"/> and
    /// <see cref="NotifyDamageReceived"/> for the LOCAL player only. Recoil is visual feedback, not haptics.
    /// </summary>
    public sealed class CombatFeedbackHooks : MonoBehaviour
    {
        /// <summary>Hard cap for accumulated recoil, regardless of the Inspector kick or the shot rate.</summary>
        public const float MaxRecoilDegrees = 3f;

        [Header("Sound")]
        [SerializeField] private FeedbackPresenter presenter;
        [SerializeField] private FeedbackCueLibrary library;

        [Header("Muzzle flash (optional)")]
        [SerializeField] private GameObject muzzleFlash;
        [SerializeField, Min(0.01f)] private float muzzleFlashSeconds = 0.05f;

        [Header("Recoil (optional): put the camera under this pivot so the player's own pitch is untouched")]
        [SerializeField] private Transform recoilPivot;
        [SerializeField, Range(0f, MaxRecoilDegrees)] private float recoilKickDegrees = 0.9f;
        [SerializeField, Min(1f)] private float recoilRecoverPerSecond = 9f;

        [Header("Hit marker / damage flash (optional)")]
        [SerializeField] private CanvasGroup hitMarker;
        [SerializeField] private CanvasGroup damageFlash;
        [SerializeField, Range(0f, 1f)] private float damageFlashPeakAlpha = 0.35f;
        [SerializeField, Min(0.05f)] private float fadeSeconds = 0.25f;

        [Header("Extra hooks")]
        public UnityEvent shot;
        public UnityEvent hitConfirmed;
        public UnityEvent damageReceived;

        private float recoil;
        private float flashUntil;
        private float hitAlpha;
        private float damageAlpha;

        public float CurrentRecoilDegrees => recoil;

        public void NotifyShot(Vector3 muzzleWorldPosition)
        {
            recoil = Mathf.Min(recoil + recoilKickDegrees, MaxRecoilDegrees);
            flashUntil = Time.unscaledTime + muzzleFlashSeconds;
            if (muzzleFlash != null)
            {
                muzzleFlash.SetActive(true);
            }

            if (presenter != null && library != null)
            {
                presenter.PlayCombat(library.weaponFire, muzzleWorldPosition, true);
            }

            shot?.Invoke();
        }

        public void NotifyHitConfirmed()
        {
            hitAlpha = 1f;
            if (presenter != null && library != null)
            {
                presenter.PlayCombat(library.hitConfirm, Vector3.zero, false);
            }

            hitConfirmed?.Invoke();
        }

        public void NotifyDamageReceived(float amount)
        {
            damageAlpha = damageFlashPeakAlpha;
            if (presenter != null && library != null)
            {
                presenter.PlayCombat(library.damageTaken, Vector3.zero, false);
            }

            damageReceived?.Invoke();
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            if (muzzleFlash != null && muzzleFlash.activeSelf && Time.unscaledTime >= flashUntil)
            {
                muzzleFlash.SetActive(false);
            }

            recoil = Mathf.MoveTowards(recoil, 0f, recoilRecoverPerSecond * dt);
            if (recoilPivot != null)
            {
                recoilPivot.localRotation = Quaternion.Euler(-recoil, 0f, 0f);
            }

            hitAlpha = Mathf.MoveTowards(hitAlpha, 0f, dt / fadeSeconds);
            damageAlpha = Mathf.MoveTowards(damageAlpha, 0f, dt * damageFlashPeakAlpha / fadeSeconds);
            if (hitMarker != null)
            {
                hitMarker.alpha = hitAlpha;
            }

            if (damageFlash != null)
            {
                damageFlash.alpha = damageAlpha;
            }
        }
    }
}
