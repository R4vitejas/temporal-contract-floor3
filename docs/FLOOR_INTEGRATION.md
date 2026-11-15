# Floor 3 main-scene integration

Owner: Ravitejas. Issue #13. Branch `feature/floor-integration`, stacked on the floor-assets PR #18.

## Current scene status

`Game/Assets/Project/Scenes/Main.unity` is prepared with the supplied floor geometry, collision, lights, room/door/board references and spawn/object anchors. It contains no test player, test menu, replacement combat system or duplicate presentation implementation. This prepared scene is deliberately disabled in Build Settings: it is not yet the playable production game.

The independent `Environment/Scenes/EnvironmentTest.unity` remains available while subsystem work continues. Do not wait for Main to test networking, interactions or UI.

## Prerequisites for production assembly

| Dependency | Required evidence before acceptance |
|---|---|
| Floor assets, PR #18 / #12 | Unity import, collision checks, visual walkthrough and fresh-clone LFS retrieval |
| Networking, PR #1 / #3 | Local host/client, valid spawns/input, two-player/late-join rejection and clean recovery |
| Combat/rounds, PR #17 / #4–#5 | Combat, timer, role swap, terminal-event precedence, reset and rematch |
| Interactions, PR #16 / #6–#7 | Safe cover release, door-close safety, feedback identity and lifecycle evidence |
| Sanjana's #8–#10 | Reviewed board/UI/feedback prefab and its documented input/state/event binder |

As of preparation, none of the pending subsystem PRs has been merged for this scene, and no presentation prefab has been supplied on main. Their missing bindings are pending; placeholder implementations are not proof of integration.

## Assembly tools

1. Pull the accepted subsystem changes after review. Install Git LFS and retrieve the floor assets.
2. Choose **Temporal Contract > Floor > Prepare Main Scene** to regenerate only the owned floor/anchors. It replaces the generated Main scene and does not edit teammate prefabs or scripts. Save manual scene work separately before regenerating.
3. Verify the actual production player capsule at every source doorway and record `productionCapsuleFitVerified`. The current network prefab is 2 m tall with radius 0.5 m; the environment inspection controller is 1.8 m tall with radius 0.3 m. A source door slab is 0.934 m wide. The standalone geometry proxy with the production dimensions failed at Classroom 02; the 1.8 m / radius 0.3 m controller traversed all four doors. Do not set this gate until the actual production capsule/door fit has been agreed and retested. Coordinate any player-prefab change with Samatma; do not silently change his source. His current player code also forces the controller center to `(0,1,0)`; if adopting the 1.8 m test height, keep the center/feet alignment consistent (center Y 0.9 m), not only the radius.
4. Select `Environment/Data/FloorAssemblyConfig.asset`. Record the reviewed network/round, interaction and presentation commit hashes. Assign Sanjana's reviewed presentation prefab. Record review acceptance only after that review is complete.
5. Agree and verify the presentation binder against `FloorIntegrationAnchors`: source boards and room labels, `boardMap`, local production player input capture, replicated health/ammo/round data, and interaction feedback subscriptions. Set `presentationBindingsVerified` only after those concrete bindings have been tested. The floor builder does not invent a HUD or infer a missing binder API.
6. Choose **Temporal Contract > Floor > Assemble Reviewed Subsystems**. It copies only the network owner's opt-in session setup, adds reviewed interaction instances, replaces their placeholder door/chair visuals with supplied meshes, wires the panel/card/restricted door, and binds the switch to Classroom 02's light. Main is enabled as the production build scene only after these gates pass.
7. Perform the host/client checklist below. Code compilation or object placement does not close #13.

The assembly command fails before replacing Main if review/configuration prerequisites or prefabs are absent. Review gates are preparation safeguards, not a claim that play tests passed. Re-running the preparation command returns Main to environment-only state, so rerun production assembly afterward if appropriate.

## Specific integration requirements

- Exactly one production NetworkManager; no `EnvironmentWalkthrough`, interaction test player/menu, or `NetworkCombatTestBootstrap` in Main. Player objects are spawned by the production session.
- The source VIP anchor `(0,0,4)` and initial player X positions `-2.5/+2.5` match Samatma's current placeholders. At role swap, the coordinator must respawn both players at the same boundary as the reset. Future spawn customization belongs in his documented production API, not another controller here.
- Samatma's coordinator owns `BindRoleProvider`, `ResetAll`, and `OnPlayerDied` calls. His current branch invokes the interaction lifecycle adapter; verify the real combined behavior rather than duplicating those calls in Main.
- Four network doors replace the static visual doors. Preserve the `NetworkDoor` component and scene clearance reference. Source slabs are 0.93414 m wide, base/hinge pivoted; the wrapper moves the prefab's hinge to the source hinge.
- Only Classroom 06 uses `Door_Restricted`. The panel references the single scene card and that door. Card ownership, access and unlock remain host-owned.
- The switch controls Classroom 02's assigned light; retain ambient illumination and 10% dim intensity.
- The reviewed cover keeps its NetworkObject, server NetworkTransform, MovableCover and footprint reference. Only its visual and footprint size are adapted to the source chair. Remove the static preview/collider to avoid duplicate collision.
- Sanjana's board targets the existing supplied board meshes and the selected-section map. Her binder owns board Escape behavior, overlapping modal input locks, HUD and audio subscriptions. No extra camera or audio listener is placed in Main; only the spawned local owner's listener may be active.

## Host/client walkthrough, not yet passed

- [ ] Both production players can traverse every room; feet/camera align and doors cannot trap a player.
- [ ] Either instance opens/closes ordinary doors; another player in the doorway prevents closing.
- [ ] Wrong-role pickup and panel-without-card requests deny only the requester; feedback subject IDs identify the acted-on object.
- [ ] Attacker collects the single card and unlocks Classroom 06 permanently for that round; defender then uses its door.
- [ ] Both instances toggle Classroom 02 lighting and hear the correctly positioned feedback.
- [ ] Both players can carry/place the source chair; contention, wall/player overlap and death/disconnect release are handled.
- [ ] Each existing board opens the map locally; room selection, Escape and overlapping session/board input capture work.
- [ ] Combat/health/ammo/results agree; defender death does not end the round; attacker death wins simultaneous terminal events.
- [ ] Role swap/rematch restores players, VIP, card/panel, doors, lights and cover; no stale holder/provider remains.
- [ ] Disconnect recovers to a usable menu; three consecutive matches pass before standalone/two-laptop release acceptance.

Record tester, source commit, host/client result and evidence in `DEMO_CHECKLIST.md`. #13 remains open until this checklist has evidence.
