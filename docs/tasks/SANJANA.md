# Sanjana — classroom board, HUD, feedback, and video

## Give this brief to your AI

Read README.md, CONTRIBUTING.md, docs/PROJECT_BRIEF.md, and docs/STATUS.md. Before implementation, wait for the bootstrap merge and read docs/INTEGRATION.md. Use the shared interfaces/events. Do not create another Unity project, player controller, interaction router, networking stack, or authoritative match state.

Work on feature/ui-feedback. Own Game/Assets/Project/UI, Feedback, their prefabs, and your test scene. Do not edit Main.unity, packages, shared Core/Networking, or Adhithya's interaction implementations.

## Preparation before bootstrap

Ask Ravitejas for a floor-layout screenshot/export; identify a small set of rooms for the board. Prepare a simple readable floor-plan image with room names. Use placeholders until the real layout is available; do not invent an inaccurate final map.

## Deliver in this order

1. Local interaction prompt and HUD: role, health, round/time, objective, keycard status, ammunition, and concise success/denial messages. Read the documented game state/events rather than maintaining a separate timer or score.
2. Classroom-board interaction prefab: E opens a map panel associated with an existing board model, clicking a room highlights it and shows its name, Escape closes it. Capture and restore local movement/look/cursor through the shared input-capture contract. Keep map selection local; test no effects on the other player's controls. Use a floor-plan image, not a realtime camera/minimap.
3. Final Host/Join/Ready/results UI bound to Samatma's session/round API. Retain IP/port entry; no accounts or join-code service.
4. Feedback prefabs/event handlers: positional door/switch/pickup/gunshot sounds, local hit indication, restrained recoil/muzzle flash, and clear lighting response. Use available/licensed assets and record their source/license in docs/ASSET_CREDITS.md. Never add a second active AudioListener to a client.
5. Coordinate lighting settings with Ravitejas; supply reusable components/settings rather than editing the main scene.

## Testing and recording

Test board cursor/focus restoration, remote-player isolation, HUD consistency, spatial sound direction/distance, and feedback when the client initiates an interaction. Report unknowns, including missing audio assets or pending network integration.

After the integrated build passes acceptance, record a 30-second video: 0–4s environment/session; 4–9s board; 9–15s keycard/door; 15–21s lights/furniture; 21–27s encounter/VIP; 27–30s result/reset. Record actual gameplay, keep it readable, and save the finished video outside the source repo unless sharing is coordinated.

Deliver a PR with prefab/setup instructions, event dependencies, test evidence, and known issues. Give Ravitejas the final video file/link separately.
