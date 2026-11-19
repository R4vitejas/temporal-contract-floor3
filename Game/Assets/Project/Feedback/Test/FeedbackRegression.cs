using System.Collections;
using TemporalContract.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TemporalContract.Feedback.Testing
{
    /// <summary>
    /// TEST-ONLY headless Play Mode regression for the feedback layer (no network, combat or floor).
    /// Prints FEEDBACK_REGRESSION PASS/FAIL lines and exits 0 on pass, 2 on failure.
    /// Audible direction/distance is NOT checked here (no audio device in batch mode); positions are.
    /// </summary>
    public sealed class FeedbackRegression : MonoBehaviour
    {
        [SerializeField] private FeedbackCueLibrary library;

        private bool failed;

        private IEnumerator Start()
        {
            var listenerGo = new GameObject("Listener");
            listenerGo.transform.position = new Vector3(0f, 1.6f, 0f);
            listenerGo.AddComponent<AudioListener>();
            var guard = listenerGo.AddComponent<SingleAudioListenerGuard>();

            var presenterGo = new GameObject("Presenter");
            var presenter = presenterGo.AddComponent<FeedbackPresenter>();
            presenter.SetLibrary(library);
            var resolver = new DictionarySubjectResolver();
            presenter.SetResolver(resolver);

            Transform left = Subject("Left", new Vector3(-6f, 1f, 2f), resolver, 100);
            Transform far = Subject("Far", new Vector3(0f, 1f, -20f), resolver, 101);

            var sourceGo = new GameObject("Source");
            var source = sourceGo.AddComponent<MockFeedbackSource>();
            presenter.Register(source);
            yield return null;

            // 1. positional event plays at the subject's world position, not the listener's
            source.Raise(FeedbackKind.DoorMoved, 100, 1f);
            FeedbackPresenter.PlayedCue door = Last(presenter);
            Check("door event plays at subject position", door.Kind == FeedbackKind.DoorMoved && door.Positional
                && door.Position == left.position && door.Position != listenerGo.transform.position);
            source.Raise(FeedbackKind.LightToggled, 101, 1f);
            Check("far event plays at far position", Last(presenter).Position == far.position);

            // 2. denial is local 2D feedback
            source.Raise(FeedbackKind.InteractionDenied, 100);
            Check("denial is non-positional local sound", Last(presenter).Kind == FeedbackKind.InteractionDenied
                && !Last(presenter).Positional);

            // 3. duplicate (host local + replicated copy) plays once
            int before = presenter.PlayLog.Count;
            source.Raise(FeedbackKind.KeycardCollected, 100);
            source.Raise(FeedbackKind.KeycardCollected, 100);
            Check("duplicate event played once", presenter.PlayLog.Count == before + 1 && presenter.SkippedDuplicates >= 1);

            // 4. despawned/unknown subject is skipped without an exception
            int skipped = presenter.SkippedUnresolved;
            source.Raise(FeedbackKind.CoverPlaced, 999);
            Check("unresolved subject skipped safely", presenter.SkippedUnresolved == skipped + 1);

            // 5. destroyed subject (despawn) is skipped safely
            Destroy(far.gameObject);
            yield return null;
            skipped = presenter.SkippedUnresolved;
            source.Raise(FeedbackKind.DoorMoved, 101, 0f);
            Check("destroyed subject skipped safely", presenter.SkippedUnresolved == skipped + 1);

            // 6. destroyed source is dropped on rescan; no stale subscription
            int sources = presenter.SourceCount;
            Destroy(sourceGo);
            yield return null;
            presenter.Rescan();
            Check("destroyed source unregistered", presenter.SourceCount == sources - 1);

            // 7. disabling the presenter unsubscribes: a live source raising afterwards plays nothing
            var source2Go = new GameObject("Source2");
            var source2 = source2Go.AddComponent<MockFeedbackSource>();
            presenter.Register(source2);
            presenter.enabled = false;
            int logCount = presenter.PlayLog.Count;
            source2.Raise(FeedbackKind.DoorMoved, 100, 1f);
            Check("disabled presenter has no stale subscription", presenter.PlayLog.Count == logCount);
            presenter.enabled = true;
            yield return null;

            // 8. one active listener
            Check("exactly one enabled AudioListener", guard.Check() == 1);

            // 9. combat hooks: shot cue at the muzzle, small recoil that recovers, hit marker fades
            var hooksGo = new GameObject("Hooks");
            var pivot = new GameObject("Pivot").transform;
            var hooks = hooksGo.AddComponent<CombatFeedbackHooks>();
            var canvasGroupGo = new GameObject("HitMarker");
            var hitGroup = canvasGroupGo.AddComponent<CanvasGroup>();
            SetField(hooks, "presenter", presenter);
            SetField(hooks, "library", library);
            SetField(hooks, "recoilPivot", pivot);
            SetField(hooks, "hitMarker", hitGroup);
            Vector3 muzzle = new Vector3(1f, 1.5f, 3f);
            hooks.NotifyShot(muzzle);
            Check("shot cue plays at the muzzle position", Last(presenter).Positional && Last(presenter).Position == muzzle);
            Check("recoil is restrained (<= 3 degrees)", hooks.CurrentRecoilDegrees > 0f && hooks.CurrentRecoilDegrees <= 3f);
            // repeated shots at the maximum supported Inspector setting must never exceed the documented cap
            SetField(hooks, "recoilKickDegrees", CombatFeedbackHooks.MaxRecoilDegrees);
            for (int shot = 0; shot < 5; shot++)
            {
                hooks.NotifyShot(muzzle);
            }

            Check("repeated shots at maximum kick stay within 3 degrees",
                hooks.CurrentRecoilDegrees <= CombatFeedbackHooks.MaxRecoilDegrees + 0.0001f
                && hooks.CurrentRecoilDegrees > 0f);
            SetField(hooks, "recoilKickDegrees", 0.9f);
            hooks.NotifyHitConfirmed();
            yield return new WaitForSecondsRealtime(1.2f);
            Check("recoil recovers to zero", hooks.CurrentRecoilDegrees == 0f
                && Quaternion.Angle(pivot.localRotation, Quaternion.identity) < 0.01f);
            Check("hit marker fades out", hitGroup.alpha < 0.01f);

            Finish();
        }

        private Transform Subject(string name, Vector3 position, DictionarySubjectResolver resolver, ulong id)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            resolver.Set(id, go.transform);
            return go.transform;
        }

        private static FeedbackPresenter.PlayedCue Last(FeedbackPresenter presenter) =>
            presenter.PlayLog.Count > 0 ? presenter.PlayLog[presenter.PlayLog.Count - 1] : default;

        private static void SetField(object target, string field, object value) =>
            target.GetType().GetField(field,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(target, value);

        private void Check(string name, bool ok)
        {
            if (!ok)
            {
                failed = true;
            }

            Debug.Log($"FEEDBACK_REGRESSION {(ok ? "PASS" : "FAIL")}: {name}");
        }

        private void Finish()
        {
            Debug.Log($"FEEDBACK_REGRESSION RESULT: {(failed ? "FAIL" : "PASS")}");
            int code = failed ? 2 : 0;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(code);
#else
            Application.Quit(code);
#endif
        }
    }
}
