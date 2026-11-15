# Floor 3 runtime assets

Owner: Ravitejas. Issue #12. Branch `feature/floor-assets`.

## Source and selected section

The supplied `3rdfloor-compressed.blend` is the source of the architecture, boards, windows, door slabs/handles, classroom benches and chair. Originals remain outside the repository and were not saved or modified. `Data/floor-source-manifest.json` records the selected source objects and exported triangle counts.

The source contains 1,767 mesh objects and 10,958,379 base-mesh triangles. The separate `Objects/lab.obj` is 576,510,843 bytes, with 3,016,929 vertices and 4,766,537 faces; it is excluded from runtime. `automationlabobj.obj` has 87,604 vertices / 85,290 faces and is also excluded. `chair1.obj` is a single flat quad, so the runtime chair comes from the source Staff Room object `Cube.386` instead. The separate washroom and dais are outside the selected section.

The playable section preserves the source `ClassRoom02`, `ClassRoom03`, `ClassRoom05`, `ClassRoom06` and a clipped portion of the source `Pathway`. Classroom 06 is designated as the VIP room for this game; these labels are source collection names, not independently verified university room numbers. Stairwells, labs, washrooms, other corridors and their expensive fittings are excluded. One bench (`Shorter.029`) is hidden in the generated prefab to leave the VIP anchor clear. Classroom boards remain separate and identifiable.

Source meshes are in metres. Local Unity coordinates are:

```
Unity X = source Blender X - 38.98
Unity Y = source Blender Z
Unity Z = source Blender Y + 1
```

The FBX importer initially reverses the horizontal axes. `FloorAssetBuilder` measures the relative positions of two known source shells and applies a 180-degree model rotation. Do not add another scale/axis correction. A source door is approximately 0.934 m wide and 2.010 m tall; its dedicated FBX is based at its left hinge.

## Files and regeneration

- `Game/Assets/Project/Environment/Models/`: selected-section FBX, four separate source door FBXs, base-pivot source chair FBX. All tracked in Git LFS.
- `Environment/Prefabs/Floor3Environment.prefab`: environment geometry, primitive collision boxes, room lighting, static door visuals and integration anchors. Contains no networking, combat, HUD, or test player.
- `Environment/Scenes/EnvironmentTest.unity`: independent walkthrough, with doors fixed open for collision inspection.
- `Environment/Data/Floor3Map.png`: labelled layout for Sanjana's board. It describes the selected section, not the entire building. Green lines mark boards/doors and coral lines mark blocked routes.
- `Environment/Runtime/FloorIntegrationAnchors.cs`: scene-owned references only; it grants no gameplay authority.

From the repository root, regenerate the exports without changing the source:

```sh
blender --background "$FLOOR3_SOURCE/3rdfloor-compressed.blend" \
  --python Game/Assets/Project/Environment/SourceTools/export_floor.py -- \
  --output Game/Assets/Project/Environment
python3 Game/Assets/Project/Environment/SourceTools/generate_map.py
```

The map script requires matplotlib. Open `Game/` using Unity 6000.6.8f1 and choose **Temporal Contract > Floor > Build Environment Assets**. Alternatively run `-batchmode -nographics -quit -projectPath <Game> -executeMethod TemporalContract.Environment.EditorTools.FloorAssetBuilder.BuildAll`.

The builder edits only environment-owned assets. It never runs teammates' prefab builders. Source image paths refer to missing files on other machines; eight shared URP materials deliberately replace these unavailable textures. No source image dependency remains. The supplied roof sheets have upward-facing normals, so the shared ceiling material renders both sides to remain visible from inside classrooms. Imported model colliders are disabled; the scene uses simple room/floor/bench/door box colliders. Keep the ambient light when binding room switches. Dynamic shadows are disabled in the preparation scene.

## Independent test

Open `Environment/Scenes/EnvironmentTest.unity` and enter Play Mode. WASD/mouse walk and look; Escape releases the cursor, click captures it, Home returns to the attacker anchor. The test controller exists only in this scene and must never be added to Main. Doors are open and non-networked here: passing this scene does not prove interaction or LAN correctness.

The builder's `Verify` check confirms four room/board/light bindings, clear player-sized doorway capsules, supported and unoccupied attacker/defender/VIP anchors, a map texture and no missing scripts. It reports rendered triangle and collider counts in the Unity log. This is automated static geometry/physics validation; visual walking and real multiplayer remain separate checks.

## Integration anchors

| Anchor | Unity position | Use |
|---|---|---|
| AttackerSpawn | (-2.5, 0.05, 0) | Initial host spawn; faces +Z |
| DefenderSpawn | (2.5, 0.05, 0) | Initial client spawn; faces -Z |
| VIPAnchor | (0, 0, 4) | In Classroom 06, with one bench cleared |
| KeycardAnchor | (6.02, 0.85, -7) | Corridor pickup; add the supplied interaction prefab |
| AccessPanelAnchor | (-2.48, 0, 0.98) | Face mounted at its prefab's authored height |
| SwitchAnchor | (-1.98, 0, 0.98) | Bind selected room lights |
| CoverAnchor | (7.02, 0, -4) | Base-pivot source chair |

`FloorIntegrationAnchors.rooms` contains actual per-room hinge, source-board and light references. Door hinge X values are approximately -4.1663, 11.7836, -4.0501 and 11.7650 for Classrooms 02, 03, 06 and 05 respectively. Door slab width is 0.93414; keep that width when adapting the interaction collider. Only Classroom 06 is restricted. The static visuals/colliders must be replaced by the reviewed interaction instances during assembly; do not leave duplicate blocking doors or chairs.

## Sharing and remaining acceptance

Install Git LFS before cloning or pulling this branch:

```sh
git lfs install
git lfs pull
git lfs fsck
```

Fresh remote clone retrieval passed on `6991dd1` after configuring Git LFS: all seven binary assets were downloaded and matched SHA-256; `git lfs fsck` passed. Each teammate still needs Git LFS on their machine. Do not add the original Blender/OBJ sources, builds or recordings to ordinary Git.

Unity 6000.6.8f1 import/compilation and the generated environment checks passed (exit 0): four clear doorways, three supported/unoccupied anchors, zero missing scripts, 46,037 rendered triangles and 68 colliders. `Data/Floor3Preview.png` is a Blender cutaway render of the exported geometry, not a Unity gameplay screenshot. No frame-rate or two-laptop claim is made: measure the standalone integrated game at 1280x720 on the weaker demo laptop. The current machine is an Intel Core i3-1005G1 / Iris Plus G1 with approximately 16 GB RAM on Ubuntu 24.04.5; the second laptop is not yet identified. This issue remains open until the visual walkthrough and remaining acceptance evidence are accepted.
