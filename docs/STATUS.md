# Project status

## Current update — November 19

- PR #16 was merged by Adhithya at `bf41861`. Independent Unity 6000.6.8f1 retests now pass the original occupied-baseline/all-rings-blocked scenario and all 18 emitted cover-regression checks. The cover clears ownership and parks hidden/non-solid when no valid drop exists. The original review finding is resolved; interaction subsystem issues #6/#7 are accepted and closed. Author-reported host/client observations remain distinguished from these independent host-side checks. Full production integration is still pending.
- Adhithya (@RealAdhi) is coassigned to Sanjana's #8–#11 because her laptop cannot run Unity. Sanjana remains assigned. Both may work in UI/Feedback within the established contracts; Core/Networking/Players/Combat/Rounds and Main ownership are unchanged.
- Presentation preparation PRs #20–#23 are now merged. Linux re-review passes HUD 35/35 at `07241ad` and board 19/19 at `cc6ae43`; the separate runtime-binding reproduction also passes. All 11 floor-map rectangles and all four PNG/JSON/metadata files match the floor source. The original recoil, Escape-test delivery and HUD-binding findings are resolved. Issues #8–#11 remain open for real provider/actions bindings, actual input/visual/audio/two-client acceptance and final capture. A focused follow-up is posted on Samatma's #17 for the remaining networking/round/eligibility/capsule/spawn API work.
- The seminar bench visual repair is committed on `feature/floor-integration`: original bounds/material slots/UVs retained, 988 bench triangles, all 1,767 source meshes and the accepted collision bake preserved. Its inspection scene and shared prefab are saved. The floor branch includes current main's interaction changes. Colours remain deferred by request. The Linux walkthrough/ZIP is rebuilt from `77cd821`; standalone collision and 48-entrance tests pass and all 305 packaged file checksums verify. It remains a dirty-state environment validation build.
- Adhithya has already merged #1, #17 and the follow-up #25 into main (`12ecab2`), along with reflection-contract test #24. The original startup/F1 fixes are present, and independent host startup plus the 24-check lifecycle suite pass. Two integration blockers are independently reproduced and assigned to Samatma: #26 (dead/between-round interaction requests accepted because the production eligibility hook is unbound) and #27 (production capsule blocked at Classroom 02; initial/reset/VIP poses still use prototype coordinates). Actual host/client acceptance under #3–#5 remains pending. Main stays disabled and the game is not frozen. The prepared presentation root remains available on this floor branch.

Details: [November 19 review record](REVIEW_2026-11-19.md). Earlier sections below are historical and are superseded by this update where they conflict.

## Full-floor scope correction — November 16

Ravitejas requires the entire supplied floor. The four-room export, map and walkthrough in PR #18 and the dependent Main preparation in PR #19 are prototypes that need full-floor revision before acceptance. Their existing validation results apply only to that prototype. The original Blender files remain unchanged. Live Unity MCP is connected. A separate full-floor inspection scene is now available; Main now uses the full floor; production integration and release acceptance remain pending.

## Full-floor retro revision — November 18

Current local work on `feature/floor-integration` preserves every source mesh (1,767/1,767) and the entire layout. The original assignment specifies a retro, low-poly aesthetic. The revised floor uses a consistent muted palette and point-filtered pixel textures. Simplifying 545 detailed props reduces the imported mesh count from 11,131,473 to 2,405,273 triangles without removing objects or changing architecture. Original Blender files remain untouched.

`FullFloorInspection.unity` contains architecture collision, simple furniture/window boxes, and doors held open for inspection. The doorway sticking issue was reproduced: the default 0.3 m step offset lifted the test capsule into low lintels. At 0.05 m step offset, all six named classroom entrances passed in both directions (12/12) in Play Mode and the headless Linux standalone. This verifies the independent 1.8 m/radius 0.3 m test player; the production capsule still needs coordination with Samatma.

The complete labelled map and pixel click regions are in `Environment/Data/FullFloorMap.png` and `full-floor-map-regions.json`. Main now uses the full-floor prefab with proposed gameplay markers and lighting; it remains disabled for production builds and has no test controller. The legacy four-room assembly is blocked. Full production bindings, shared interactions, round/reset integration and final performance remain pending; #12–#14 are not closed.

The Linux inspection build is at `Builds/Floor3-full-floor-inspection-Linux/Floor3.x86_64`. Build success and 12/12 standalone doorway traversal are observed. No match/FPS/two-laptop acceptance is claimed. The real two-laptop hotspot test is deferred to the next team meeting, when hardware details are available.

## Collision acceptance revision — November 18

The local full-floor collision pass was rebuilt after reported wall pass-through and sticking. Geometry now supplies both sides of thin surfaces, missed wall objects and separate furniture pieces. The inspection controller separates gravity from horizontal movement and adapts step height to available headroom. Three source entrances are refined only in runtime exports.

Play Mode passes: 7,646 wall probes; 251 approach/return cases; 158 slides; 4,986 clear floor-grid edges; 48 entrance approach/return cases covering all 24 named doors. No failure remains in these sampled cases. Two toilet cubicles use shorter clear endpoints before their fixtures; the checks do not require crossing a solid toilet. The final Linux standalone repeats both the full wall/grid audit and 48 entrance checks successfully. Rendered 720p view averages are 47.1–82.5 FPS on the local Intel GPU; p99 frame times are 18.6–33.4 ms. This is an environment benchmark with other applications open, not full-game or weaker-laptop acceptance.

Full-floor production handoff: [FULL_FLOOR_HANDOFF.md](FULL_FLOOR_HANDOFF.md). Production capsule/spawn integration and Sanjana's published board/HUD/feedback prefab remain actual dependencies; the local floor audit does not close #13/#14 or certify multiplayer.

## Adhithya review update — November 18

PR #16 advanced to `22c8b32`, adding the missing author-reported host/client evidence and improving normal forced-drop validation. Independent isolated import/compilation passed. A host-only Play Mode regression still places released cover inside an obstruction when every fallback candidate is blocked; the exact-baseline branch remains unsafe. A [formal changes-requested review](https://github.com/R4vitejas/temporal-contract-floor3/pull/16#pullrequestreview-5952096055) is now submitted. Details and ownership: [INTERACTION_REVIEW_2026-11-18.md](INTERACTION_REVIEW_2026-11-18.md). This blocker belongs to Interactions; Sanjana's work is not a prerequisite for fixing or accepting it. No merge/issue closure was performed.

## Foundation and subsystem review

- Bootstrap PR #2 is merged on main: Unity 6000.6.8f1, pinned runtime packages, contracts and router.
- Network PR #1 at `57e1eff` fixes global startup interference and F1 handling. Both original review threads are resolved. Headless import/compilation passed; required interactive host/client/recovery checks remain pending, so #3 and PR #1 remain open.
- Combat/round PR #17 at `30826aa` is stacked on PR #1. Headless import/compilation passed. Combat, round/rematch and real interaction integration acceptance remain pending (#4–#5).
- Interaction PR #16 is now at `22c8b32`; see the review update above. The missing door/feedback checks are recorded as author-observed. One independently reproduced exhausted-fallback safety finding remains (#6–#7).
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
