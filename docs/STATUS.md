# Project status

## Full-floor scope correction — November 16

Ravitejas requires the entire supplied floor. The four-room export, map and walkthrough in PR #18 and the dependent Main preparation in PR #19 are prototypes that need full-floor revision before acceptance. Their existing validation results apply only to that prototype. The original Blender files remain unchanged. Live Unity MCP is connected. A separate full-floor inspection scene is now available; Main now uses the full floor; production integration and release acceptance remain pending.

## Full-floor retro revision — November 18

Current local work on `feature/floor-integration` preserves every source mesh (1,767/1,767) and the entire layout. The original assignment specifies a retro, low-poly aesthetic. The revised floor uses a consistent muted palette and point-filtered pixel textures. Simplifying 545 detailed props reduces the imported mesh count from 11,131,473 to 2,405,273 triangles without removing objects or changing architecture. Original Blender files remain untouched.

`FullFloorInspection.unity` contains architecture collision, simple furniture/window boxes, and doors held open for inspection. The doorway sticking issue was reproduced: the default 0.3 m step offset lifted the test capsule into low lintels. At 0.05 m step offset, all six named classroom entrances passed in both directions (12/12) in Play Mode and the headless Linux standalone. This verifies the independent 1.8 m/radius 0.3 m test player; the production capsule still needs coordination with Samatma.

The complete labelled map and pixel click regions are in `Environment/Data/FullFloorMap.png` and `full-floor-map-regions.json`. Main now uses the full-floor prefab with proposed gameplay markers and lighting; it remains disabled for production builds and has no test controller. The legacy four-room assembly is blocked. Full production bindings, shared interactions, round/reset integration and final performance remain pending; #12–#14 are not closed.

The Linux inspection build is at `Builds/Floor3-full-floor-inspection-Linux/Floor3.x86_64`. Build success and 12/12 standalone doorway traversal are observed. No match/FPS/two-laptop acceptance is claimed. The real two-laptop hotspot test is deferred to the next team meeting, when hardware details are available.

## Foundation and subsystem review

- Bootstrap PR #2 is merged on main: Unity 6000.6.8f1, pinned runtime packages, contracts and router.
- Network PR #1 at `57e1eff` fixes global startup interference and F1 handling. Both original review threads are resolved. Headless import/compilation passed; required interactive host/client/recovery checks remain pending, so #3 and PR #1 remain open.
- Combat/round PR #17 at `30826aa` is stacked on PR #1. Headless import/compilation passed. Combat, round/rematch and real interaction integration acceptance remain pending (#4–#5).
- Interaction PR #16 at `8af16b1` includes independent prefabs/test scene/lifecycle hooks. Its reported host/client tests are distinct from independently observed tests. Cover-release safety findings and explicit door/feedback/lifecycle evidence remain pending (#6–#7).
- No presentation PR is available on main yet. Sanjana can continue #8–#10 against mocks and the shared contracts without waiting for Main.

## Ravitejas's authorized work

Ravitejas explicitly authorized #12–#14 on November 15, 2026.

- Floor PR #18 (`feature/floor-assets`, `f9e794c`) selects source Classrooms 02/03/05/06 and their corridor, retaining supplied architecture/boards/windows/doors/chair. A labelled map and cutaway preview are included.
- Unity import/compilation and static geometry checks passed: four clear doorways, three supported/unoccupied anchors, zero missing scripts, 46,037 rendered triangles and 68 colliders. Fresh remote clone/LFS retrieval passed with all seven binary assets matching SHA-256. Interactive walkthrough and actual hardware performance are not claimed.
- `feature/floor-integration` prepares Main with environment geometry/anchors and no test controller. Main stays disabled in Build Settings until reviewed production assembly; this is not the complete playable game.
- Production assembly needs accepted networking/round/interaction prefabs and Sanjana's documented presentation binder. Setup: [FLOOR_INTEGRATION.md](FLOOR_INTEGRATION.md). No teammate implementation is replaced or silently modified.
- The Linux environment development build and standalone 1.8 m / radius 0.3 m traversal check passed, including a graphical Intel-GPU run. The 2 m / radius 0.5 m production-size geometry proxy failed at Classroom 02; actual player/door clearance needs owner coordination before Main acceptance. No full-game FPS or LAN pass is claimed.
- Release/hotspot/build/viva preparation is in [DEMO_CHECKLIST.md](DEMO_CHECKLIST.md). Ravitejas's Ubuntu laptop and installed Linux build module are recorded. Second-laptop selection, the actual two-laptop run, measured FPS, three matches, frozen build/video and rehearsal remain pending.

## Work tracking and next steps

Use [WORK_BOARD.md](WORK_BOARD.md) for assignments. Ready for review is not accepted, and an issue stays open until its required evidence passes. Preserve standalone test scenes for independent development.

1. Owners finish their subsystem fixes and host/client evidence; no one needs Main to do this.
2. Review and merge accepted PRs in dependency order. The floor/integration PRs are stacked and must be retargeted after their base merges.
3. Assemble reviewed prefabs into Main and verify real role/reset/death, input, feedback and collision behavior.
4. Run standalone and actual hotspot/two-laptop acceptance before claiming #13–#14 complete. Freeze/record/rehearse target is November 17; November 18 is repairs.
