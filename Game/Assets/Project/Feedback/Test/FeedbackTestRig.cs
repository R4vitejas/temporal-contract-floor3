using System.Collections.Generic;
using TemporalContract.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TemporalContract.Feedback.Testing
{
    /// <summary>
    /// TEST-ONLY scene driver for FeedbackTest.unity. Wires a mock source, mock subject positions and a
    /// simple fly camera so direction and distance can be judged by ear and eye. No network, combat or floor.
    /// Keys 1-9 raise events at the subject you point at; F fires the weapon cue; H hit; G damage.
    /// </summary>
    public sealed class FeedbackTestRig : MonoBehaviour
    {
        [SerializeField] private FeedbackPresenter presenter;
        [SerializeField] private CombatFeedbackHooks combat;
        [SerializeField] private MockFeedbackSource source;
        [SerializeField] private Transform[] subjects = new Transform[0];
        [SerializeField] private Camera listenerCamera;

        private readonly DictionarySubjectResolver resolver = new DictionarySubjectResolver();
        private int selected;
        private float yaw;
        private string status = "";

        private static readonly FeedbackKind[] Keys =
        {
            FeedbackKind.DoorMoved, FeedbackKind.LightToggled, FeedbackKind.KeycardCollected,
            FeedbackKind.CoverPickedUp, FeedbackKind.CoverPlaced, FeedbackKind.InteractionAccepted,
            FeedbackKind.InteractionDenied, FeedbackKind.DoorMoved, FeedbackKind.LightToggled
        };

        private void Awake()
        {
            for (int i = 0; i < subjects.Length; i++)
            {
                resolver.Set((ulong)(100 + i), subjects[i]);
            }

            presenter.SetResolver(resolver);
            presenter.Register(source);
        }

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.leftArrowKey.isPressed) yaw -= 90f * Time.deltaTime;
            if (Keyboard.current.rightArrowKey.isPressed) yaw += 90f * Time.deltaTime;
            listenerCamera.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (Keyboard.current.tabKey.wasPressedThisFrame && subjects.Length > 0)
            {
                selected = (selected + 1) % subjects.Length;
            }

            for (int i = 0; i < Keys.Length; i++)
            {
                Key key = Key.Digit1 + i;
                if (Keyboard.current[key].wasPressedThisFrame)
                {
                    float amount = Keys[i] == FeedbackKind.DoorMoved ? (i == 0 ? 1f : 0f) : 0f;
                    source.Raise(Keys[i], (ulong)(100 + selected), amount);
                    status = $"{Keys[i]} at subject {selected}";
                }
            }

            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                combat.NotifyShot(listenerCamera.transform.position + listenerCamera.transform.forward);
                status = "weapon fire";
            }

            if (Keyboard.current.hKey.wasPressedThisFrame) { combat.NotifyHitConfirmed(); status = "hit confirm"; }
            if (Keyboard.current.gKey.wasPressedThisFrame) { combat.NotifyDamageReceived(25f); status = "damage taken"; }
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(10, 10, 900, 22), "FEEDBACK TEST  Tab = select subject | 1 door open, 2 light, 3 card, 4 pick cover, 5 place cover, 6 accepted, 7 denied (2D) | F fire, H hit, G damage | Left/Right = turn");
            GUI.Label(new Rect(10, 32, 900, 22), $"selected subject {selected}   last: {status}   listeners: {FindObjectsByType<AudioListener>().Length}");
            int y = 54;
            IReadOnlyList<FeedbackPresenter.PlayedCue> log = presenter.PlayLog;
            for (int i = Mathf.Max(0, log.Count - 6); i < log.Count; i++)
            {
                GUI.Label(new Rect(10, y, 900, 22), $"{log[i].Kind} {(log[i].Positional ? "@ " + log[i].Position : "(2D local)")}");
                y += 20;
            }
        }
    }
}
