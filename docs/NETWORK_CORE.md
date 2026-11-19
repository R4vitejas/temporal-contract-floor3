# LAN network core and combat

This branch adds an opt-in LAN session menu, a strict two-player limit, Ready/Start gating, owner-controlled FPS movement, synchronized transforms, server-validated hitscan combat, replicated health/ammo/reload state, a stationary VIP, and disconnect recovery. It also adds two 90-second rounds, role swapping, attacker-win scoring, round resets, result screens, and a rematch gate requiring both players. The session controller owns connection setup; gameplay systems do not call Unity Transport directly.

## Run the LAN test scene

1. Open `Game/` in Unity `6000.6.8f1`.
2. Run `Assets/Project/Scenes/NetworkCoreTest.unity` from Build Settings. The scene contains the `Network Core Session (Opt In)` object with its own manager, transport, and LAN controller. It is independent of teammate test scenes.
3. Enter Play Mode or launch a Windows build. On one instance choose port `7777` and click **Host**.
4. On the second instance enter the host IPv4 address and port, then click **Join**. Permit inbound UDP on the selected port at the host firewall.
5. Both players select **Mark Ready**. The host can start only with exactly two connected, Ready players. The host is the initial attacker; late joins are rejected after Start.
6. Each round lasts 90 seconds. The attacker wins by eliminating the VIP; the defender wins by eliminating the attacker or surviving the timer. Roles swap for round two. After results, both players must select **Ready for rematch** to start again.
7. During active combat, use WASD/mouse to move and aim, left click to fire, and R to reload. The temporary menu displays local health, ammo, round, timer, outcome, and rematch readiness when opened with F1.

## Authority and state

The owner sends fire intent and aim pitch; the host checks ownership, living state, active session, fire interval, ammo, reload state, and aim bounds. The host computes the ray from the server-observed player position and resolves against current colliders, so walls block hits and clients cannot send a target or damage value. Players have 100 health; shots deal 25 damage, the magazine holds 12 rounds, the fire interval is 0.25 seconds, and reload takes 1.5 seconds. Both players can damage each other; only the host-designated attacker can damage the VIP. A defeated player cannot move, interact, or fire.

The VIP is spawned by `RoundCoordinator` when a match begins. Round participants implement `IRoundResetParticipant`; interaction systems can bind their role provider and handle player death/reset using the API documented in `docs/INTERACTIONS.md`.

`NetworkPlayerHealth` exposes `MaximumHealth`, `CurrentHealth`, `HealthChanged`, and the server-side `Died(clientId)` event. `NetworkHitscanWeapon` exposes `MagazineSize`, `Ammunition`, `IsReloading`, `ReloadSecondsRemaining`, `StateChanged`, and `ShotFired`. `VipHealth` exposes health properties, `HealthChanged`, and `Died(attackerClientId)`. `LobbyNetworkState` exposes `AttackerClientId` and `IsAttacker(clientId)`. `RoundCoordinator` exposes replicated phase, round number, attacker, time remaining, outcomes, per-player attack wins, and rematch readiness, plus player/VIP death notifications. These values/events are read-only presentation inputs.

## Input capture and test setup

`NetworkFpsPlayer` implements `IGameplayInputCapture`. A board or menu acquires a local modal lock with `SetGameplayInputCaptured(this, true)` and releases the same owner with `SetGameplayInputCaptured(this, false)` on close or destroy. Multiple owners can hold input at once.

Use **Temporal Contract → Build Network Core Assets** to regenerate the player, lobby, and VIP prefabs under `Assets/Project/Prefabs/Resources/NetworkCore/` and the isolated test scene. The network test scene has a dedicated `NetworkManager`, `UnityTransport`, and `LanSessionController`; no global startup hook modifies unrelated scenes.

## Verification status

Unity 6000.6.8f1 headless asset generation, import, and C# compilation succeeded for the network and combat slice. A Windows Development build also succeeded. The round coordinator was integrated after that build; this updated combined version still needs a fresh Unity compile and build. Host/client Play Mode checks for connection, shooting from both owners, wall blocking, reload, deaths, round timing, role swap, rematch, rejection, and re-hosting remain pending; two-laptop LAN checks are also pending.
