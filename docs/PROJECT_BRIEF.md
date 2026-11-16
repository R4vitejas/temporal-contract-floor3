# Agreed MVP

## Priority

Environmental interactions on the supplied Floor 3 model are the main assessment focus. Keep two-player desktop LAN multiplayer and clear audio/UI/lighting feedback. Use the entire supplied Floor 3 layout in the runtime scene. Refine materials, collision, organization and performance while preserving all rooms, corridors and recognizable architecture. Retain existing source props unless a specific removal is agreed with Ravitejas. Do not crop the layout or add artificial corridor blockers to reduce the playable area. Originals remain unchanged; designate a VIP room and gameplay locations within the full floor.

## Required interactions

1. Doors: look at an ordinary door, press E, open/close with collision and positional sound. One restricted door requires a keycard.
2. Existing classroom board: E opens a simple clickable floor map; click a room to highlight it and display its name. Escape closes it and restores movement/mouse-look. Selection is local; no live camera minimap.
3. Keycard and panel: attacker collects a single card; panel denies access without it, then unlocks the restricted door permanently for the current round. Card ownership and unlock state are shared. Defender can use ordinary doors and the restricted door after unlock.
4. Light switch: both players can toggle designated lights; state and sound are shared. Keep enough ambient light to navigate safely.
5. Movable cover: both players can carry one designated chair/small desk at a time and place it on a valid surface. One holder per object; reject placement through walls or intersecting players. No freeform rotation or unrestricted dragging physics required. No throwing required.

## Match

Host/join menu, Ready state, exactly two players. No late joining after a round starts. Owner-only input and camera. One gun, basic hitscan damage, reload, health, crosshair, and restrained recoil. Stationary VIP.

Two 90-second rounds, swapping attacker/defender after the first. Attacker wins a round by killing the VIP; defender wins if attacker dies or time expires. Defender death alone does not end the round. Compare successful attacks after both rounds; equal scores are a draw. Host resolves terminal events once; live attacker death takes precedence over VIP death in the same simulation tick. Rematch resets both rounds.

Reset player positions, health, ammo, keycard, doors, cover, lights, VIP, timer, and results at round boundaries. Disconnect ends the session and returns to a usable menu.

## Architecture

Netcode for GameObjects + Unity Transport. Host validates damage, world interactions, inventory, furniture, and round outcomes. Owner-driven player movement is acceptable. Persistent states replicate to clients; local UI and camera effects remain local. Isolate direct-IP connection setup from gameplay so Relay can be added later without embedding service calls into doors/weapons.

Shared interfaces and their concrete signatures must be published by Samatma in docs/INTEGRATION.md during bootstrap: interaction target prompt/eligibility/request; round-reset participant; feedback event; local UI input capture. Use stable network object identities for shared targets. Use server-side range/role/state checks, including a single-holder lock for cover.

## Excluded

NPC guards, echo recording/replay, Prime Lock, teleportation, internet services, accounts/login, custom network framework, advanced physics, advanced stealth, and destruction. A short cosmetic movement echo is optional only after all MVP acceptance checks pass.

Keyboard/mouse desktop play is assumed. Physical haptics is assumed waived based on the team's understanding; recoil is visual feedback, not physical haptics. Do not represent a waiver as confirmed by the professor.

## Performance and acceptance

Minimum target: stable 30 FPS at 720p on the weaker demo laptop; aim for 60. Measure actual hardware. Use simple colliders, modest textures, limited realtime lights/shadows, and sensible static geometry separation.

Acceptance: two real laptops connect; host and client can each operate shared interactions; keycard denial/unlock work; board input exits correctly; cover contention/invalid placement are handled; both clients agree on damage and results; every object resets; disconnect recovers; three consecutive matches complete without Editor restart. Test standalone builds and the actual demo network.
