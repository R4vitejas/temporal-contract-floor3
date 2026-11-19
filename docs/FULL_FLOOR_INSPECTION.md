# Full-floor inspection

Open `Game/` in Unity 6000.6.8f1. In Project, open `Assets/Project/Environment/Scenes/FullFloorInspection.unity`, then Play. Click the Game view; WASD moves, mouse looks, Esc releases the cursor and Home returns to the corridor start. Stop Play before editing. Unity MCP can stay running.

This scene includes every mesh object from the original floor. Classrooms correspond to the roof light strips, the central tiered benches identify the seminar hall, red chairs identify labs, and staff/cabin/cabinet spaces remain present. The source already includes a washroom; the separate washroom file has not been duplicated. No floor cropping, furniture deletion or artificial corridor blockers are used.

The original `.blend` is read only. `SourceTools/export_full_floor.py` exports into a staging folder outside Assets, repairs image paths where a supplied texture matches, and writes a provenance/material/missing-image manifest. Example:

```
blender --background ORIGINAL.blend --python Game/Assets/Project/Environment/SourceTools/export_full_floor.py -- --output STAGING --low-poly
```

Copy `Models/Floor3Full.fbx`, `Data/full-floor-manifest.json` and the staged textures to `Environment/Textures/FullFloor/`. Model and textures use Git LFS. Then run **Temporal Contract → Floor → Build Full Floor Inspection**. Save any current scene first. The builder regenerates only the owned full-floor prefab/inspection scene/material directory. It verifies imported/source mesh identity, retains distinct door meshes, and maps source materials into a coherent retro palette with point-filtered 16×16 textures. Detailed props are simplified in the runtime export; original architecture and every mesh object remain.

The retro inspection uses warm daylight, neutral ambient fill, double-sided materials and architecture MeshColliders. Doors are held open only in the inspection scene; the production prefab retains their closed source placement. They have no interaction behaviour here. Simple furniture/window boxes now supplement architecture collision. All six named classroom entrances passed both directions; complete room/prop collision coverage remains pending. The inspection player is independent of the production network player; its capsule is 1.8 m high and radius 0.3 m. It must not be used as evidence that Samatma's 2 m/radius 0.5 m player fits every door.

Missing images and unsupported Blender shader graphs are recorded rather than claimed repaired. Detailed fixtures account for much of the roughly 2.4 million runtime triangles; optimise their geometry/LOD, material sharing and rendering while retaining the layout and visible props. Actual 720p frame rate on the demo laptops is still required.

Next integration work: choose and label gameplay locations within the whole floor, provide the full board map, validate door/furniture collision and pivots, replace the old Main preparation, then bind reviewed production subsystems and run host/client/reset/release checks. Do not merge this scene's test controller into Main.

## Observed checks, November 18

- Unity import/compilation completed. Scene generation retained 1,767/1,767 mesh object identities and 2,405,273 imported triangles, with architecture collision plus simple furniture/window boxes.
- Full overhead capture matched the supplied layout. Available texture recovery supplies colour maps for 206 source materials; 48 image references remain unresolved.
- An independent 1.8 m/radius 0.3 m CharacterController traversed the straight corridor from Unity (-28.98, 0.1, -9) towards (11.02, 0.1, -9) using collision-aware Move calls in Play Mode. End X error was below 0.00001 m; Console errors were absent during this check. This is one route, not a complete floor acceptance pass.
- Final triangulated export avoids Unity discarding the self-intersecting source n-gon during import. Source material copies map into the retro palette instead of 1,137 independent materials.
- No original Blender file was saved, no production subsystem was changed, and no release build or multiplayer acceptance is claimed.

## Doorway correction and local build

The original default 0.3 m CharacterController step offset lifted the inspection capsule into low source lintels and stopped forward motion. The initial correction used a 0.05 m step baseline and 0.03 m skin width. The current collision revision adapts the step allowance to headroom, up to 0.32 m, as described below. The 1.8 m/radius 0.3 m capsule passed all six named classroom entrances in both directions (12/12) in Play Mode and the Linux standalone smoke test. This does not change or certify Samatma's production player settings.

Build through **Temporal Contract → Floor → Build Linux Environment Walkthrough**. The current output is `Builds/Floor3-full-floor-inspection-Linux/Floor3.x86_64`; it contains the full-floor inspection scene. Automated standalone check: `Floor3.x86_64 -batchmode -nographics --full-floor-smoke-test` (expects exit 0 and `FULL_FLOOR_STANDALONE_PASS`). Run without those arguments for manual inspection.

`Data/FullFloorMap.png` labels the complete layout; `full-floor-map-regions.json` gives classroom/seminar/lab image rectangles in pixels with a top-left origin. Sanjana can use them for her board. These are map references, not a replacement for her board UI implementation.

Main now contains the full-floor prefab, lighting and proposed spawn/VIP/keycard markers. It has no test player and stays disabled in production build settings until reviewed systems are wired. The old four-room assembly routine is explicitly blocked from assembling the full-floor scene.

The final Linux inspection build also passed the 12/12 doorway smoke test with graphics enabled on the local Mesa Intel GPU. This is not an FPS benchmark. The map label script requires Pillow and reads the saved orthographic base capture (`FullFloorMapBase.png`); the capture camera is at Unity (-13.5, 70, -0.5), rotation (90, 0, 0), orthographic size 25.5.

## Collision revision and coverage

The earlier name-based collider pass omitted `Wall`/`Cube` architecture and used broad furniture boxes that could fill empty aisle space. The replacement bakes 223 two-sided source surfaces, 3,823 solid wall rectangles sampled from the actual vertical faces on a 5 cm grid, and 1,234 separate connected furniture pieces. Floor boundaries and room layouts are preserved; wall rectangles do not intentionally fill door openings. Exact source surfaces retain floors, ceilings, slopes and angled geometry.

The inspection player separates horizontal movement from gravity and subdivides long frames. Its step allowance adapts up to 0.32 m with a sphere cast limiting the lift under low headroom. This allows stairs/thresholds while preserving door clearance. Grounded state is read before the horizontal move, and placement/reset clears vertical velocity. Falling below the floor returns to the inspection spawn. Production player code is not changed.

Runtime exports refine the openings for `Door.012`, `Door.014` and `Door.024`; the original source files remain unchanged. The two washroom doors were too narrow, and the seminar opening was both low and obstructed by the joined stage/backdrop mesh. The export records original bounds and refined source meshes in `entranceAdjustments`. All 1,767 mesh objects remain, with 2,413,528 runtime triangles.

Latest Play Mode evidence: 7,646 two-direction wall ray probes, 251 approach/return checks, 158 diagonal slides, and 4,986 open floor-grid edges from 2,977 supported clear positions, with zero crossings, return failures, sliding failures or clear-route movement failures. Grid spacing is 0.75 m across the full model bounds. Twenty-two edges intersecting solid geometry are classified as blocked rather than expected walk routes. The audit deliberately excludes invalid spawn positions inside furniture. Returning to the known-clear start is the retreat criterion; it does not demand walking through a second obstacle behind the player.

All 24 named doors have 48 passing directional approach/return checks. Doors 012/014 use shorter cubicle endpoints before the toilets; this verifies approach and retreat through the opening, not walking through a toilet or having enough space to close its door behind the capsule. The final evidence is in `Data/full-floor-collision-evidence.json` and `full-floor-entrance-evidence.json`.

Standalone audit: `Floor3.x86_64 -batchmode -nographics --full-floor-collision-audit`. Optional `FLOOR_COLLISION_AUDIT_OUTPUT` supplies an absolute JSON output path. The same runtime checks are used in Play Mode and the player build. `--full-floor-smoke-test` now checks all 48 entrance approaches/returns.

Performance command: run **without** batch mode or `-nographics`: `Floor3.x86_64 -screen-width 1280 -screen-height 720 -screen-fullscreen 0 --full-floor-performance`. It warms up each corridor/seminar/classroom/lab view for 60 frames, then samples 180 frames with v-sync disabled. Batch-mode timing does not demonstrate rendered performance. This is environment-only measurement with Editor/background applications open, not full-match or second-laptop acceptance.

These are bounded, reproducible checks. They do not prove every continuous position/angle on the floor. Walls and furniture should stop forward movement; the regression checks require sliding and backing away to remain usable. Final production bindings/remaining ownership dependencies are documented in [FULL_FLOOR_HANDOFF.md](FULL_FLOOR_HANDOFF.md).

Final Linux standalone evidence repeats the 7,646 wall probes, 251 approach/return cases, 158 slides and 4,986 clear grid edges with zero failures, plus all 48 entrance approach/returns. Rendered 1280×720 measurements: corridor 82.5 FPS / p99 18.6 ms; seminar 57.4 / 23.8 ms; classroom 47.9 / 33.4 ms; lab 47.1 / 33.2 ms. Average frame rate meets the floor-only 30 FPS target on this laptop; a few frame times approach/exceed the 33.3 ms budget. Full-game and weaker-laptop measurement remain pending.

## Seminar bench geometry refinement — November 18

The thin joined seminar benches (`Cube.384`) were damaged by collapse simplification of face-split source vertices. A separate visual mesh now welds coincident source positions and dissolves coplanar edges before triangulation. It retains the original bench bounds, material slots and UVs, with 988 triangles. Colours, all 1,767 mesh objects, the stage, tiered floor, room layout and accepted collision bake remain intact. The original Blender file is unchanged.

`Models/SeminarBenches.asset` is applied by `SeminarGeometryRefinement` after collision generation, so rebuilding the inspection scene preserves the visual repair without replacing tested collision. The shared full-floor prefab also contains this mesh override. To regenerate the asset, run Blender against the original floor with `SourceTools/export_seminar_benches.py -- --output STAGING/bench.json`, then call `SeminarGeometryRefinement.Import(jsonPath, floorRoot)` in Edit Mode. Save the inspection scene and apply the same override to the shared prefab.

Live Unity compilation and three rendered inspection angles were checked. Play Mode reran 7,646 wall rays, 251 approaches/returns, 158 slides, 4,986 clear floor-grid edges and all 48 entrance cases with zero failures. Local evidence is in `.local-work/seminar/`; serialized collider snapshots before/after the repair have matching SHA-256. The existing standalone build predates this visual refinement; no new standalone build or multiplayer acceptance is claimed.
