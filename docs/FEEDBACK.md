# Positional feedback, audio and combat presentation

Issue #10. Everything is under `Game/Assets/Project/Feedback/`. It is presentation only: it never applies
damage or changes shared state, and it has no dependency on the network, combat or floor code.

## Pieces

| Piece | Purpose |
|---|---|
| `FeedbackPresenter` | Subscribes to every `IFeedbackEventSource`, resolves the event's subject to a world position and plays a pooled `AudioSource` there (or a local 2D sound). Discovers sources automatically each 1.5 s (spawn/despawn safe) and drops destroyed ones. Skips duplicate events inside 0.08 s. |
| `FeedbackCueLibrary` | ScriptableObject mapping `FeedbackKind` to clip, volume, positional or 2D, min/max distance. Swap sounds here without code changes. |
| `ISubjectPositionResolver` | `NetworkSubjectResolver` (default) looks the subject id up among spawned NetworkObjects; `DictionarySubjectResolver` is the mock for test scenes. A missing/despawned subject is skipped, never an exception. |
| `CombatFeedbackHooks` | Muzzle flash, restrained camera recoil, hit marker, damage flash, plus weapon/hit/damage sounds. Called by combat code for the LOCAL player only. |
| `SingleAudioListenerGuard` | Logs an error if more than one `AudioListener` is enabled. It reports, it does not silently change anything. |
| `Prefabs/FeedbackSystem.prefab` | Presenter + guard + hooks with the cue library assigned. One per client. |

## Event mapping

| `FeedbackKind` | Sound | Where it plays |
|---|---|---|
| `DoorMoved`, `LightToggled`, `KeycardCollected`, `CoverPickedUp`, `CoverPlaced` | door move, switch click, card chime, cover scrape | Subject's world position, distance attenuated (`SubjectNetworkObjectId`) |
| `InteractionAccepted`, `InteractionDenied` | ding, buzz | Local 2D. Interactions raise `InteractionDenied` only for the requester. |
| `DamageReceived` | thud | Local 2D |
| `RoundStarted`, `RoundEnded`, `MatchEnded` | tone | Local 2D |
| Weapon fire (not a `FeedbackKind`) | gunshot | Muzzle world position via `CombatFeedbackHooks.NotifyShot(position)` |

Interactions on `feature/environment-interactions` already raise `DoorMoved`, `KeycardCollected`,
`InteractionAccepted`, `InteractionDenied`, `LightToggled`, `CoverPickedUp`, `CoverPlaced` with the
interaction's NetworkObjectId as the subject (see `docs/INTERACTIONS.md`).

## Wiring (per scene, for Ravitejas/Samatma)

1. Drop `FeedbackSystem.prefab` into the scene once per client. Nothing else is required for the
   interaction sounds: sources are found automatically.
2. The local player's camera must be the only enabled `AudioListener`.
3. For combat visuals call, for the LOCAL player only: `NotifyShot(muzzleWorldPos)` from the weapon's
   `ShotFired` event, `NotifyHitConfirmed()` when the owner's shot hits, `NotifyDamageReceived(amount)` when
   local health drops. Assign the optional muzzle flash, hit marker and damage flash `CanvasGroup`s.
4. Recoil: put the player camera under a child pivot and assign it as `recoilPivot` so the player's own pitch
   code is untouched. Recoil is visual, never accumulates past an absolute 3 degrees (`CombatFeedbackHooks.MaxRecoilDegrees`) however high the Inspector kick or the fire rate, and recovers in about a quarter second. It is not haptics.
5. Missing subject ids, destroyed sources and a disabled presenter are all handled without exceptions.

## Test scenes and how to run them

- `Scenes/FeedbackTest.unity`: no network. Tab selects a subject (left near, right near, ahead, far behind),
  keys 1-7 raise door/light/card/cover/accepted/denied events, F/H/G raise weapon, hit and damage
  presentation, Left/Right turns the listener. Use it to judge direction and distance by ear.
- `Scenes/FeedbackRegression.unity` + `FeedbackAssetBuilder.RunRegression`: headless Play Mode regression.
  Menu **Temporal Contract > Feedback > Run Feedback Regression**, or:

```
Unity -batchmode -nographics -projectPath Game -executeMethod TemporalContract.Feedback.EditorTools.FeedbackAssetBuilder.RunRegression -logFile feedback.log
```

It prints `FEEDBACK_REGRESSION PASS/FAIL` lines and exits 0 on pass, 2 on failure. Regenerate sounds, library,
prefab and scenes with **Temporal Contract > Feedback > Build Sounds, Library And Test Scenes**.

## Verification status

Observed (headless Play Mode, Unity 6000.6.8f1, 14 checks, exit 0): a door event plays at the subject's
position, not the listener's; a far subject plays at the far position; denial is a local 2D sound; duplicate
events play once; unresolved and destroyed subjects are skipped safely; destroyed sources are unregistered and
a disabled presenter has no stale subscription; exactly one enabled listener is detected; the weapon cue plays
at the muzzle; recoil stays at or below 3 degrees, including repeated shots at the maximum kick setting, and recovers to zero; the hit marker fades out.

NOT verified: hearing direction/distance (batch mode has no audio device; judge it in `FeedbackTest.unity`),
readability at 1280x720, host and client duplicate playback in a real network session, binding to Samatma's
weapon/health events, positions of the real floor, performance.
