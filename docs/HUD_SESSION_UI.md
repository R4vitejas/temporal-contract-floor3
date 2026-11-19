# HUD and session/result screens

Issue #9. Everything is under `Game/Assets/Project/UI/Hud/`. The UI only renders state and invokes callbacks; it owns no
score, authoritative time, health or win condition, and it has no dependency on the network, combat or round code.
A production adapter attaches later through two small interfaces.

## Contracts (`PresentationContracts.cs`)

- `PresentationState`: `phase` (`Disconnected, Connecting, Lobby, ActiveRound, RoundEnd, MatchEnd, Error`), lobby
  (`isHost, connectedPlayers, localReady, remoteReady`), round (`role, alive, health/maxHealth, ammo/magazineSize,
  reloading, timeRemainingSeconds` (negative = unknown)`, roundNumber, objective, keycardKnown/hasKeycard`), results
  (`roundResult, localScore/remoteScore, matchResult, localRematchReady/remoteRematchReady`), status
  (`statusMessage, errorMessage`).
- `IPresentationProvider`: `event StateChanged` and `bool TryGetState(out PresentationState)`. Return false while there is no
  real state; the UI then shows a neutral "Waiting for game state..." panel and logs one warning. It never shows invented
  live values.
- `IPresentationActions`: `Host(port)`, `Join(address, port)`, `SetReady(bool)`, `StartMatch()`, `Disconnect()`,
  `RequestRematch()`. The authority decides what they do.

## Screens (`HudSessionUi`, prefab `Prefabs/HudSessionUi.prefab`)

Main menu (IPv4 + port, Host, Join, status; input is validated before any callback), lobby (player count, ready states,
Ready toggle, host-only Start enabled only with two ready players, Disconnect), HUD (role, health bar and numbers, ammo or
RELOADING, MM:SS timer, round n/2, objective, keycard status, crosshair, interaction prompt, short messages,
ELIMINATED overlay), round end (result + score), match end (VICTORY/DEFEAT/DRAW, score, rematch with waiting state,
Disconnect), error (message + back to menu).

Other hooks: `BindRouter(InteractionRouter)` shows `PromptChanged` prompts and clears them when the target is lost;
`BindFeedback(IFeedbackEventSource)` turns accepted/denied/keycard events into short messages; `ShowMessage(text)`;
`Bind(provider, actions)` swaps sources at runtime.

## Test scenes and how to run them

- `Scenes/HudMockTest.unity`: a strip of buttons on the right jumps through 14 mock states (no state, disconnected,
  connecting, lobby waiting/ready, attacker, low-health defender, defeated, reloading, round end, win, draw, rematch
  waiting, error). The mock logs the callbacks it receives.
- `Scenes/HudRegression.unity`: headless Play Mode regression. Menu **Temporal Contract > HUD > Run HUD Regression**, or:

```
Unity -batchmode -nographics -projectPath Game -executeMethod TemporalContract.UI.Hud.EditorTools.HudAssetBuilder.RunRegression -logFile hud.log
```

It prints `HUD_REGRESSION PASS/FAIL` and exits 0 on pass, 2 on failure. Regenerate the prefab and scenes with
**Temporal Contract > HUD > Build Prefab And Test Scenes**. The scene needs an `EventSystem` with
`InputSystemUIInputModule` (the project uses the new Input System only).

## Runtime bindings still pending (production adapter, small follow-up)

Not built here on purpose: it depends on unmerged network/combat/round code. From `docs/NETWORK_CORE.md` on the network
branch, a `PresentationState` can be filled from `NetworkPlayerHealth` (`CurrentHealth`, `MaximumHealth`, `Died`),
`NetworkHitscanWeapon` (`Ammunition`, `MagazineSize`, `IsReloading`), `RoundCoordinator` (phase, round, attacker, time
remaining, outcomes, attack wins, rematch readiness), `LobbyNetworkState` (`AttackerClientId`, `IsAttacker`) and
`LanSessionController`, plus the interaction side (`KeycardPickup.HolderClientId` on the interactions branch) for keycard
status. `IPresentationActions` maps to `LanSessionController` host/join/ready/start/disconnect and the coordinator's rematch
call. Until that adapter exists the HUD shows the neutral placeholder in a real scene.

## Verification status

Observed (headless Play Mode, Unity 6000.6.8f1, 32 checks, exit 0): no provider and a provider without state both show the
neutral placeholder; every state renders the right panel and text (menu, connecting, lobby waiting/ready, attacker,
low-health defender with timer rounding, defeated overlay, reloading, round end, match win/draw, rematch waiting, error);
invalid port/address are rejected with a message and no callback; Join/Host/Ready toggle/Start/Disconnect/Rematch invoke the
right callbacks; missing actions are ignored without errors; the prompt shows and clears; messages expire; the canvas uses
a 1280x720 reference and every non-input text is at least 18 px; no error or exception was logged during the run.

NOT verified: how it looks (no screenshots were captured; batch mode has no display), real mouse and keyboard use,
text legibility on an actual 1280x720 window, two clients showing real authoritative health/time/result (needs the
production adapter), and cooperation with the board and menu input-capture binding in a real scene.
