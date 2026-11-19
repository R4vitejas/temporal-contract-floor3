# Classroom board and floor map

Issue #8. Everything is under `Game/Assets/Project/UI/Board/`. The board is local-only: no `NetworkBehaviour`, no RPC,
no host request. Opening it on one client cannot pause, move or affect the other player.

## Pieces

| Piece | Purpose |
|---|---|
| `ClassroomBoard` | `IInteractionTarget` with `NetworkObjectId == null`. Pressing E on it (via the shared `InteractionRouter`) opens the map on this client only. Put it on the existing board mesh; the mesh needs a collider on the router's layers. |
| `FloorMapPanel` | The UGUI map. `Open()`, `Close()`, `ForceClose()`, `Select(i)`, `SetLayout(layout)`; events `Opened`, `Closed`, `RoomSelected(int)`, `CaptureStateChanged(bool)`. Escape and the Close button close it. |
| `FloorMapLayout` | ScriptableObject: artwork sprite plus rooms (`id`, `displayName`, normalized `Rect` with origin bottom-left) and a label toggle. This is how the final artwork is swapped in. |
| `IModalInputCapture` / `LocalInputCapture` | Owner-keyed, reference-counted capture. Two overlapping modals never release each other's lock. |
| `PlayerInputCaptureAdapter` | Production binding to `NetworkFpsPlayer.LocalOwner.SetGameplayInputCaptured(owner, captured)` by reflection, so this folder compiles without that unmerged code. Missing player = no-op, never throws. |
| `Prefabs/FloorMapPanel.prefab` | Canvas (1280x720 reference), dimmer, artwork, clickable regions, room name, hint and Close button. |

## Behavior

- Open: saves cursor lock/visibility, acquires capture (move/look/fire/interact are disabled by the player and the router
  hides its prompt), unlocks and shows the cursor, clears the selection.
- Click a room: only that region highlights, its name shows in the bottom label.
- Close (Escape, button, `ForceClose`, disabling, destroying, or losing the network session while open): releases this
  panel's own capture and restores the saved cursor state, unless another modal (for example the session menu) still holds
  capture, in which case the cursor stays free.

## Inspector binding

1. Place `FloorMapPanel.prefab` in the scene (one per client scene). Assign its `captureComponent` to a
   `PlayerInputCaptureAdapter` (production) or `LocalInputCapture` (test).
2. Add `ClassroomBoard` to the board mesh and assign `panel`.
3. The scene needs an `EventSystem` with `InputSystemUIInputModule` (the project uses the new Input System only).
4. Final artwork: import the floor plan as a Sprite, duplicate `PlaceholderFloorLayout`, assign the sprite, edit the room
   rectangles and names, and assign it to the panel's `layout`. No code changes. Turn `showRegionLabels` off if the
   artwork already carries its own labels.

## Test scenes and how to run them

- `Scenes/BoardTest.unity`: offline mock actor (WASD + mouse), a placeholder board on the north wall, a mock session
  menu on `M` to test overlapping modals. Walk up, press E, click rooms, Esc.
- `Scenes/BoardRegression.unity`: headless Play Mode regression. Menu **Temporal Contract > Board > Run Board Regression**,
  or:

```
Unity -batchmode -nographics -projectPath Game -executeMethod TemporalContract.UI.Board.EditorTools.BoardAssetBuilder.RunRegression -logFile board.log
```

It prints `BOARD_REGRESSION PASS/FAIL` and exits 0 on pass, 2 on failure. Regenerate the placeholder art, layouts, prefab
and scenes with **Temporal Contract > Board > Build Placeholder Art, Prefab And Test Scenes**.

## Verification status

Observed (headless Play Mode, Unity 6000.6.8f1, 19 checks, exit 0): the board has no network id and is not a
`NetworkBehaviour`; E opens the panel, captures input and unlocks the cursor; repeated room selection highlights and names
the right room; a real Input System Escape key press closes it, releases capture and restores the cursor; repeated
open/close and double close are harmless; closing the board while the mock menu is open keeps the menu's capture and a
free cursor; swapping the layout rebuilds the regions without code changes; `ForceClose` and destroying an open panel
release capture.

NOT verified: mouse clicks on regions (the regression calls the same `Select` method the buttons call), host and client
running together (the board is local by construction, but no two-instance run was done), the production
`PlayerInputCaptureAdapter` against the merged player and session menu, accurate Floor 3 artwork (the image is a labelled
placeholder), look at 1280x720 in a build.
