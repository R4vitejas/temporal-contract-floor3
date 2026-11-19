# Playable Floor 3 MVP

Open `Assets/Project/Scenes/Main.unity`, press Play, then **Solo practice**. This starts the real host/player/weapon/VIP/round systems with one player. The ordinary **Host / Join** path still requires two connected, ready players before a LAN match starts.

- WASD/mouse: move and aim. Left click: fire. R: reload. Space: jump.
- E: interact with doors, the keycard, access panel, light switch, cover or classroom boards.
- Find the keycard in the west corridor, use the staff-cabin panel and eliminate the VIP.
- Escape closes the floor map. F1 opens/closes the session menu.
- Practice runs for 90 seconds. Results offer **Practice again** or **Disconnect**.

Linux executable: `Builds/Floor3-playable-mvp-Linux/Floor3.x86_64`. The repository release **Floor 3 playable MVP** includes the complete ZIP and checksum. Extract the whole folder before running.

The HUD now reads actual health, ammo/reload, role, timer, card ownership and results. Input capture, combat audio/hit/damage feedback, local weapon geometry and scene-based initial/reset/VIP poses are connected. Server interactions reject dead and between-round actors. The original floor meshes/materials and static collision remain unchanged; moving-door/panel clearance is corrected.

Verification: 117 production single-host checks pass in isolated Play Mode and graphical Linux standalone, including all 48 production-player doorway approaches/returns, the actual Solo practice button, reload, card/panel/doors, death/result/reset, four confirmed hits killing the real VIP, and disconnect back to menu. The opt-in CLI regression is `--mvp-smoke-test`; no test controller is serialized into Main.

Full two-client/two-laptop input, audio, role-swap/network-state and match acceptance remains pending. This build is a playable single-host MVP with the LAN path retained; those multiplayer checks are not implied by the solo results.
