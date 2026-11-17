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

The original default 0.3 m CharacterController step offset lifted the inspection capsule into low source lintels and stopped forward motion. The inspection player now uses step offset 0.05 m and skin width 0.03 m. The 1.8 m/radius 0.3 m capsule passed all six named classroom entrances in both directions (12/12) in Play Mode and the Linux standalone smoke test. This does not change or certify Samatma's production player settings.

Build through **Temporal Contract → Floor → Build Linux Environment Walkthrough**. The current output is `Builds/Floor3-full-floor-inspection-Linux/Floor3.x86_64`; it contains the full-floor inspection scene. Automated standalone check: `Floor3.x86_64 -batchmode -nographics --full-floor-smoke-test` (expects exit 0 and `FULL_FLOOR_STANDALONE_PASS`). Run without those arguments for manual inspection.

`Data/FullFloorMap.png` labels the complete layout; `full-floor-map-regions.json` gives classroom/seminar/lab image rectangles in pixels with a top-left origin. Sanjana can use them for her board. These are map references, not a replacement for her board UI implementation.

Main now contains the full-floor prefab, lighting and proposed spawn/VIP/keycard markers. It has no test player and stays disabled in production build settings until reviewed systems are wired. The old four-room assembly routine is explicitly blocked from assembling the full-floor scene.

The final Linux inspection build also passed the 12/12 doorway smoke test with graphics enabled on the local Mesa Intel GPU. This is not an FPS benchmark. The map label script requires Pillow and reads the saved orthographic base capture (`FullFloorMapBase.png`); the capture camera is at Unity (-13.5, 70, -0.5), rotation (90, 0, 0), orthographic size 25.5.
