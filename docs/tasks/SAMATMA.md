# Samatma — shared foundation, networking, and game loop

## Give this brief to your AI

Read README.md, CONTRIBUTING.md, docs/PROJECT_BRIEF.md, and docs/STATUS.md before changing files. Your owned areas are Unity bootstrap, package configuration, Core, Networking, Players, Combat, Rounds, and your test scene. Do not edit the main integration scene or teammates' interaction/UI implementations.

## First delivery: bootstrap

1. Collect/check the agreed Editor version and build targets. Create the single URP project at Game/. Verify the Editor opens and Play Mode works.
2. Install compatible released Input System, NGO, and Unity Transport packages. Commit resolved versions and package-lock.json. Avoid unnecessary cloud services and authentication packages.
3. Enable Visible Meta Files and Force Text; create Project folders for Core, Networking, Players, Combat, Rounds, Interactions, UI, Feedback, Environment, Prefabs, and Scenes.
4. Add actual shared C# contracts with namespaces/signatures for interaction prompts, eligibility, requests, round reset, feedback, and UI input capture. Put these in Core and document Inspector wiring and authority rules in docs/INTEGRATION.md. Adhithya and Sanjana must consume these instead of inventing alternatives.
5. The interaction router raycasts from the local player's camera, identifies a shared target, and requests host execution. Validate actor identity, range, role, and current object state at the host. Local board viewing uses the same targeting entry point but remains local.
6. Coordinate Git LFS setup before anyone commits binary runtime assets; test that another machine can retrieve a tracked asset.
7. Open a bootstrap PR. Report Editor/package versions, how to open the project, compilation/Play Mode evidence, remaining setup, and the contracts ready for teammates. Wait for merge before teammates start dependent implementation.

## Second delivery: playable network core

On feature/network-core, add Host/Join via configurable LAN IP/port, connection status, two-player limit, ready/start, no late join, and clean disconnect handling. Provide a simple temporary menu so networking can be tested before Sanjana's final UI.

Create owner-controlled FPS players with isolated camera/audio listener/input, synchronized movement, one hitscan gun, reload, health, VIP damage, the specified two-round role-swapping loop, results, rematch, and reset orchestration. Host validates shared outcomes. Expose documented values/events for the UI; do not make UI code own the round timer or scores.

Test two local instances first; then coordinate a two-laptop LAN test. Pair with Adhithya to verify an actual door from both sides. Do not implement his other interactions. Keep connection setup in a session component, separate from gameplay.

## Completion evidence

PR with exact launch/connect instructions, network port/firewall requirements, host/client test results, reset/disconnect results, build targets, and known issues. No completion claim based only on generated code or screenshots of an unplayed scene.
