# Floor 3 contribution demo

The floor is final for this presentation. The offline showcase works without a server, production player, round coordinator or HUD adapter.

## Run

Linux package: **Floor3-showcase-Linux.zip** on the repository's **Floor 3 offline environment demo** release. Extract the entire folder, then run `Floor3.x86_64` (allow execution if necessary).

- WASD and mouse: walk/look. Click to capture the cursor again.
- M: open/close the complete floor map from anywhere.
- E while aiming at any of the six classroom boards: open the local map.
- Click rooms to select them. Escape closes the map and restores movement.
- Escape outside the map: release cursor. Home: return to the entrance.

Unity scene: `Assets/Project/Environment/Scenes/Floor3Showcase.unity`. Original `FullFloorInspection.unity` and full-floor prefab/collision remain unchanged.

## Main implementation

`Assets/Project/Scenes/Main.unity` contains the reviewed opt-in LAN session, full-floor presentation root, 24 source doors (one restricted), a keycard/access panel, switched classroom fill light, a source-model movable chair and six board targets. `FullFloorSceneBindings` exposes attacker/defender/VIP poses and object references. It binds the spawned local interaction router and scene interaction feedback to the HUD, without mock state or duplicate audio subscriptions.

Main uses a dedicated non-default prefab registration list; the session registers its Resources prefabs. The global auto-generated default list is not required. Scene NetworkObject hashes are generated from the saved scene identity.

The source mesh/material identities and Main source bounds match the final prefab. The tested static collision bake matches exactly in both scenes. The saved-scene validator is `FullFloorAssemblyBuilder.VerifySavedScenes`.

## Remaining dependencies

Main remains disabled for production release until Samatma's [capsule/spawn/reset bindings (#27)](https://github.com/R4vitejas/temporal-contract-floor3/issues/27) and [alive/round interaction eligibility (#26)](https://github.com/R4vitejas/temporal-contract-floor3/issues/26), and Adhithya's real HUD state/actions adapter ([#9](https://github.com/R4vitejas/temporal-contract-floor3/issues/9)), are integrated and verified. The scene poses are references for that adapter; the current owner controller still chooses prototype positions.

Host-only scene smoke: 24 doors and four shared interaction objects spawn; one restricted door starts locked; one owner camera/listener; all six boards capture/release the actual production player in an active-phase binding fixture. This does not certify round transitions or player fit. Two-client/two-laptop input, audio and gameplay testing remains excluded from this contribution freeze.

The offline package demonstrates the environment and local map integration; it is not the frozen multiplayer game.
