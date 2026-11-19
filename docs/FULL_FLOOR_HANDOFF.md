# Full-floor production handoff

> November 19 network review: #1/#17/#25 are already merged to main, but #26 (authoritative alive/round interaction gate) and #27 (production capsule fit and scene-driven poses) remain assigned to Samatma. A new host-side test reproduces the eligibility issue; the production-size doorway proxy fails while the inspection capsule passes. See [review and reproduction](NETWORK_REVIEW_2026-11-19.md). Keep Main disabled until its real bindings and acceptance checks pass.

The shared environment is `Environment/Prefabs/Floor3FullEnvironment.prefab`. Inspect it in `Environment/Scenes/FullFloorInspection.unity`. The full layout, source props and retro materials remain. Main contains the full environment and proposed gameplay markers; it is not an assembled match.

## Movement and collision

The environment test capsule is height 1.8 m, radius 0.3 m, centre (0, 0.9, 0), skin width 0.03 m. Horizontal and vertical motion are separate, with bounded substeps. The test controller uses a step up to 0.22 m where there is headroom, reduced when a ceiling/lintel would block the lift. This allows stairs without reintroducing low-doorway stalls. The default low-door test step remains 0.05 m.

Samatma's current production capsule (height 2 m, radius 0.5 m) is wider than the original ordinary doors. `NetworkFpsPlayer` also sets its centre to (0,1,0). Production movement needs an owner-approved equivalent capsule/clearance decision and the same floor audit; the environment test controller is not a replacement production controller. No player-owned source has been changed silently.

Collision is baked from geometry, including walls stored under `Wall`/`Cube` names, two-sided thin surfaces, solid axis-aligned wall rectangles, and separate connected furniture pieces. Do not restore the previous name-only selection or one large box around a group of benches. The architecture still has its actual door openings and no artificial corridor blockers.

## Production bindings still required

- Samatma's round coordinator already calls the interaction role/reset/death adapter in his current branch. Verify those calls against the real interaction components; do not duplicate lifecycle ownership in a scene script.
- His current player spawns and VIP position are hardcoded to the prototype coordinates. The new markers in Main are proposals. The round/session owner needs to expose or agree scene-driven positions and use the same positions at reset/role swap.
- Adhithya's ordinary/restricted doors must replace each source door slab/handle visual with source pivots, door clearance volumes and collision. Inspection doors are held open only for walking tests. They do not implement E, lock state or network state.
- The source staff cabin near the HOD room is the proposed VIP room. Bind its restricted door, nearby access panel, single keycard and designated source chair only after the gameplay locations are accepted and capsule fit is verified.
- Presentation source/preparation PRs #20–#23 are merged. Adhithya co-owns #8–#11 with Sanjana. The board uses the full-floor map and all 11 normalized regions. The real HUD/session/combat adapter and combined two-client checks remain pending.
- Final acceptance requires the real player, all shared interactions, combat/round/reset/rematch/disconnect, one local listener/camera, and full-game performance. Two laptops on the hotspot are the last acceptance step at the next team meeting.

The independent collision/performance reports describe environment-only coverage. They do not certify multiplayer or every possible continuous player position.

## Checked candidate locations (Unity coordinates)

The attacker candidate is `(11.02, 0.1, -7)`, defender `(-46.15, 0.1, -16.77)`, and VIP staff-cabin candidate `(-49.98, 0.1, -14.5)`. All three have floor support and no overlap for the independent 1.8 m/radius 0.3 m capsule. Main preparation asserts these checks and refuses an overlapping or unsupported marker. The defender marker was moved out of a source wall. These are candidate coordinates for the owner's round/session API, not changes to hardcoded production spawns.


## Prepared presentation root — November 19

`Environment/Prefabs/Floor3Presentation.prefab` composes the reviewed `FeedbackSystem`, `FloorMapPanel` and `HudSessionUi` prefabs with one EventSystem/InputSystemUIInputModule. Its board capture component is wired to `PlayerInputCaptureAdapter`. It has no mock provider, NetworkManager or AudioListener; the production local player supplies the listener. The real HUD provider/actions and gameplay/router/feedback bindings are still required from the presentation owner.

Regenerate through **Temporal Contract → Floor → Prepare Full Floor Presentation Prefab** (`FullFloorPresentationBuilder.Build`). The builder uses a temporary additive scene and restores the user's active scene. It records the root in `FloorAssemblyConfig.presentationPrefab` and leaves `presentationBindingsVerified` false. It does not assemble Main or certify production input capture. Do not add a second EventSystem if this root is used.

Live composition inspection found one feedback presenter, one map, one HUD, one EventSystem, zero AudioListeners, zero test/mock components and 11 map regions. The inspection scene remains clean with all 1,767 meshes; floor collision and Main are unchanged. Main stays disabled in production builds. Perform combined two-client input/audio and gameplay checks before freezing; record the video from the frozen verified build afterward.
