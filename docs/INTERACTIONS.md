# Environmental interactions: handoff and context

Owner: Adhithya. Branch: `feature/environment-interactions`. Everything lives in
`Game/Assets/Project/Interactions/`. This file is written for teammates and their coding agents:
read it before touching, wiring, or depending on interaction code. Ask Adhithya before editing
anything in `Interactions/`.

## What exists

| Script (`Interactions/Scripts/`) | Purpose |
|---|---|
| `NetworkInteractable` | Abstract base. Implements `IInteractionTarget`, `IInteractionRequestHandler`, `IFeedbackEventSource`, `IRoundResetParticipant`. |
| `NetworkDoor` | Hinged door. Replicated open/locked state. Refuses to close while a player stands in the doorway. |
| `KeycardPickup` | The single keycard. One host-recorded holder, attacker only. |
| `AccessPanel` | Denies without the card. On success unlocks the restricted door for the round. |
| `WallSwitch` | Shared on/off for a group of lights, with a configurable dim level when off. |
| `MovableCover` | Chair/desk. One holder per object, one cover per player, host-validated placement. |
| `InteractionRoleGate` | Static hook so interactions can ask "is this client the attacker?". Unbound = attacker-only actions DENIED. |
| `InteractionLifecycle` | The adapter the match coordinator calls: `BindRoleProvider`, `ResetAll`, `OnPlayerDied`. Host-only. |

Prefabs: `Interactions/Prefabs/` (`Door_Ordinary`, `Door_Restricted`, `Keycard`, `AccessPanel`,
`WallSwitch`, `Cover_Chair`, `Cover_Desk`). They use placeholder cube meshes.
Test scene: `Interactions/Scenes/InteractionsTest.unity` (standalone, uses a test-only player).
Regenerate prefabs and scene: menu **Temporal Contract > Interactions > Build Prefabs And Test Scene**,
or headless `-executeMethod TemporalContract.Interactions.EditorTools.InteractionAssetBuilder.BuildAll`.

`Interactions/Test/` (`TestPlayerController`, `TestNetworkMenu`) is TEST-ONLY. It is not the shared
player. Never reference it from gameplay prefabs or `Main.unity`.

## Authority model (how every interaction works)

1. Client: `InteractionRouter` raycasts, finds the `IInteractionTarget`, and on E calls
   `RequestInteraction`. The target sends `InteractRpc` (server RPC) with no payload.
2. Host: the actor is the RPC **sender** (`rpcParams.Receive.SenderClientId`), never client data.
   The host resolves that client's player `NetworkObject`, checks distance
   (`serverMaxDistance`, default 4.5, measured from the player root), then calls the subclass
   `ExecuteOnServer`, which checks role and object state and mutates replicated state.
3. A `false` result sends `InteractionDenied` to the requester only.
4. Successful changes broadcast a `FeedbackKind` event to all clients through `FeedbackRaised`.

Replicated state is in `NetworkVariable`s (door open/locked, card holder, panel granted, switch on,
cover holder). Late joiners get correct state from the spawn sync.

## Feedback events emitted (for Sanjana's audio/UI)

Subscribe to `IFeedbackEventSource.FeedbackRaised` on each interaction. `SubjectNetworkObjectId`
is the interaction's NetworkObjectId (use it for positional audio).

| Event | Raised by | Notes |
|---|---|---|
| `DoorMoved` | `NetworkDoor` | `Amount` 1 = opened, 0 = closed |
| `KeycardCollected` | `KeycardPickup` | |
| `InteractionAccepted` | `AccessPanel` | unlock granted |
| `InteractionDenied` | any | requester only: locked door, no card, wrong role, etc. |
| `LightToggled` | `WallSwitch` | `Amount` 1 = on, 0 = off |
| `CoverPickedUp` / `CoverPlaced` | `MovableCover` | |

Interactions import no audio/UI. Add the sounds in your own assets and subscribe.

## Integration requirements per teammate

### Samatma (rounds, players, combat)
Three host-side calls, all in `TemporalContract.Interactions.InteractionLifecycle`. You do not need
to find or wire any interaction object:

```csharp
// 1. Once, when roles exist (and again whenever the attacker changes at the round swap).
//    The delegate answers "is this client currently the attacker?". Host only.
InteractionLifecycle.BindRoleProvider(clientId => clientId == currentAttackerClientId);

// 2. At every round boundary (including rematch), after you pick the new roles.
InteractionLifecycle.ResetAll(new RoundResetContext(roundIndex, matchId));

// 3. When a player dies (disconnect is already handled inside MovableCover).
InteractionLifecycle.OnPlayerDied(clientId);
```

- **Unbound provider denies.** Until `BindRoleProvider` is called, nobody may use attacker-only
  actions (keycard pickup). A missing provider never silently makes both players attackers.
  `InteractionRoleGate.AllowAllWhenUnbound` is test-only and must stay false in shipped code.
- Reset restores: doors closed and locked-by-default, keycard uncollected, panel not granted,
  switches to their default, cover back at baseline and released. It sets state rather than
  toggling, so repeating the same context is harmless.
- Death or disconnect drops held cover at the first valid spot among: under the carry point, then a
  ring around the holder's feet, then the baseline pose. The holder lock is always cleared, so the
  object can be picked up again.
- The player prefab needs a `NetworkObject` and a non-trigger collider (a `CharacterController`
  is fine). Door-close safety and cover placement detect players with
  `NetworkObject.IsPlayerObject` on a hit collider.
- Players must have an `InteractionRouter` (your component) and an `IInteractionActor`.
  Interactions read nothing else from the player.
- `Core/Contracts.meta` is rewritten by Unity every time the project is imported. Commit the
  version Unity writes so it stops showing as modified.
- Unity rewrites `Assets/DefaultNetworkPrefabs.asset` locally when prefabs with a `NetworkObject`
  exist. Interaction prefabs are placed in the scene, not spawned at runtime.

### Ravitejas (floor, main scene, integration)
- Place the prefabs in `Main.unity`; swap the placeholder meshes for the real door/furniture meshes.
  Keep the component references when you do (see "Inspector wiring").
- Interactive doors must be separate meshes with the pivot on the hinge side (door slab is a child
  of the `Hinge` transform; the hinge rotates around `hingeAxis`, default Y).
- Cover needs the object's pivot at its base, a non-trigger collider, and the `footprint`
  BoxCollider assigned (size of the footprint is the overlap-check volume).
- `Interactions/Scenes/InteractionsTest.unity` is not in Build Settings. Add it yourself if you want
  it in a build.
- The raw floor models are never committed. `.gitignore` now excludes `*.obj`, `*.blend`,
  `*.blend1`, `*.glb`, and `3/`.

### Sanjana (UI and feedback)
- Use the feedback table above. Reuse `InteractionPrompt` from the router for the on-screen hint.
  Prompt labels: "Open door"/"Close door", "Locked"/"Keycard required", "Keycard",
  "Use keycard"/"Access panel"/"Access granted", "Lights on"/"Lights off", "Pick up"/"Place".
- The wall switch never goes fully dark by default: light intensity drops to `dimIntensityScale`
  (0.1 in the test scene). Coordinate final lighting with Adhithya.

## Inspector wiring

- `NetworkDoor`: `hinge` (child Transform holding the door mesh and collider), `lockedByDefault`,
  `doorwayClearance` (trigger BoxCollider covering the doorway, NOT a child of the hinge),
  optional `interactionPoint`.
- `AccessPanel`: `keycard` (the `KeycardPickup`) and `restrictedDoor` (the `NetworkDoor`).
  Scene references, so wire them per scene.
- `KeycardPickup`: `visualRoot` (hidden when collected) and `pickupCollider`.
- `WallSwitch`: `lights[]`, optional `fixtureRenderers[]`, `onByDefault`, `dimIntensityScale`.
- `MovableCover`: `footprint`, `surfaceLayers`, `blockerLayers`, carry distance/height. The prefab
  also needs a `NetworkTransform` (server authority, the default).
- All of them: `NetworkObject` on the root; no hardcoded scene object names anywhere.

## Verification status (be honest about it)

Observed means a person ran it in Play Mode. Everything else is pending, not passed.

| Check (`InteractionsTest.unity`) | Host only | Host + client |
|---|---|---|
| Scripts and builder compile (Unity 6000.6.8f1) | compiled | n/a |
| Door open/close, locked door denial | observed | PENDING |
| Keycard pickup, panel denial then unlock, restricted door | observed | PENDING |
| Wall switch toggles room lights | observed | PENDING |
| Cover pick up / place / invalid placement | observed | PENDING |
| `R` reset restores all baselines | observed | PENDING |
| `K` drops held cover (simulated death) | observed | PENDING |
| Simultaneous keycard pickup race | n/a | PENDING |
| Door will not close on another player | n/a | PENDING |
| Attacker-only card with roles (`T`/`Y`), denied defender | PENDING | PENDING |
| Unbound roles deny attacker actions (`U`) | PENDING | PENDING |
| Same-context reset repeated (`V`) is harmless | PENDING | PENDING |
| Requester-only denial feedback, positional feedback identity | PENDING | PENDING |

Not covered by this scene at all: combat death with Samatma's code, the real round coordinator,
the real floor, two laptops on a LAN, performance on the demo laptop.

Test hotkeys in the test scene (host-executed, not shipped features): `R` reset with a new round
context, `V` reset with the same context, `K` die while holding, `T` make me the attacker, `Y` make
me the defender (the other client becomes attacker), `U` unbind the role provider (nobody is attacker).
The scene starts with the host bound as attacker. The test role provider lives in `Interactions/Test`.

## Known limitations

- A carried cover stays in front of the holder's camera so pressing E again places it. If the
  carry point ends up behind a wall, the ray hits the wall and placement is not offered. This
  matches "no placement through walls".
- Cover rotation is limited to 90-degree steps; no throwing.
- Placement checks use the footprint axis-aligned to the cover root.
- The test player uses `NetworkTransform` with owner authority and is not production movement.
- No automated tests yet. The multiplayer checks above need two running instances.
