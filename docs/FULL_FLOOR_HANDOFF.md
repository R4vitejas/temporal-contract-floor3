# Full-floor production handoff

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
- Sanjana can use `Environment/Data/FullFloorMap.png` and `full-floor-map-regions.json`. Her published board/HUD/feedback prefab and binder are still required; no production presentation PR was available at the latest check.
- Final acceptance requires the real player, all shared interactions, combat/round/reset/rematch/disconnect, one local listener/camera, and full-game performance. Two laptops on the hotspot are the last acceptance step at the next team meeting.

The independent collision/performance reports describe environment-only coverage. They do not certify multiplayer or every possible continuous player position.
