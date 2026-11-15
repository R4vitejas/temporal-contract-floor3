# Shared Unity integration contracts

This is the bootstrap contract set for Unity `6000.6.8f1`. The project uses Netcode for GameObjects `2.14.0`, the Editor-bundled Unity Transport `6.6.0`, Input System `1.20.1`, and URP `17.6.0`. The manifest requests NGO; Unity Transport is a core package bundled with this Editor. `Packages/packages-lock.json` records the resolved dependency graph.

**Package release note:** NGO `2.14.0` resolved and compiled locally with this Editor. At bootstrap, Unity's `6000.6` package catalog listed it under pre-release packages, while the `6000.5` released-package catalog listed `2.14.0` as released. If the project requires a package formally listed as released specifically for `6000.6`, confirm one team-wide Editor/package combination before merge. Unity Transport `6.6.0` is provided as a core package by this Editor. See [NGO packages for Unity 6000.6](https://docs.unity.com/en-us/engine/6000.6/manual/packages-list/packages-all/pack-preview/com-unity-netcode-gameobjects), [NGO released packages for Unity 6000.5](https://docs.unity.com/en-us/engine/6000.5/manual/packages-list/packages-all/pack-safe/com-unity-netcode-gameobjects), and [Unity Transport core packages for Unity 6000.6](https://docs.unity.com/en-us/engine/6000.6/manual/packages-list/packages-all/pack-core/com-unity-transport).

## Namespaces and ownership

- Shared interfaces and data types: `TemporalContract.Core` under `Assets/Project/Core/Contracts/`.
- Local camera interaction router: `TemporalContract.Networking.InteractionRouter` under `Assets/Project/Networking/`.
- Samatma owns `Core`, `Networking`, `Players`, `Combat`, and `Rounds`. Adhithya owns `Interactions` and his test scene. Sanjana owns `UI`, `Feedback`, and her test scene. Ravitejas owns `Environment` and `Assets/Project/Scenes/Main.unity`.
- Do not edit the main integration scene; it is owned by Ravitejas.

## Interaction contracts

```csharp
public interface IInteractionActor
{
    ulong OwnerClientId { get; }
    Vector3 Position { get; }
    bool IsLocalOwner { get; }
}

public interface IInteractionTarget
{
    ulong? NetworkObjectId { get; }
    bool TryGetPrompt(IInteractionActor actor, out InteractionPrompt prompt);
    bool IsLocallyEligible(IInteractionActor actor);
    void RequestInteraction(in InteractionRequest request);
}

public readonly struct InteractionRequest
{
    public InteractionRequest(ulong actorClientId, ulong? targetNetworkObjectId);
    public ulong ActorClientId { get; }
    public ulong? TargetNetworkObjectId { get; }
}

public interface IInteractionRequestHandler
{
    bool TryHandleInteraction(in InteractionRequest request);
}
```

`InteractionRouter` raycasts from the local player's camera, finds an `IInteractionTarget` on the hit collider or its parents, publishes `PromptChanged`, and invokes the target on E. Assign its local player camera and a component implementing `IInteractionActor` in the Inspector. Assign a component implementing `IGameplayInputCapture` when available; while it reports capture, the router hides the prompt and sends no request. Set `maxDistance` and `targetLayers` for local targeting. Local eligibility only controls the hint and key press; it is not authoritative.

A shared object implements `IInteractionTarget` on a `NetworkBehaviour` associated with its `NetworkObject`. Its client request method sends intent to the host. The target's server RPC/modern RPC handler calls `TryHandleInteraction` only after validation. Validate the sender from NGO's RPC receive metadata against the player's spawned `NetworkObject.OwnerClientId`; do not trust a client-supplied actor ID. Resolve the target by its spawned `NetworkObjectId`, then check server-observed distance, role, object state, and any per-object lock before changing replicated state. Reject null/unknown IDs, stale requests, unauthorized senders, and requests outside allowed range. The host owns shared outcomes.

For the local classroom board, implement the same target interface with `NetworkObjectId == null`; `RequestInteraction` opens/closes the local board directly and does not send an RPC. This keeps board selection and UI input local. Never interpret a null target ID as a shared action on the host.

## Round reset

```csharp
public readonly struct RoundResetContext
{
    public RoundResetContext(uint roundIndex, ulong matchId);
    public uint RoundIndex { get; }
    public ulong MatchId { get; }
}

public interface IRoundResetParticipant
{
    void ResetForRound(in RoundResetContext context);
}
```

The host's round/session coordinator calls each spawned participant once at a round boundary. Implementations reset only their owned state (for example, player health/ammo, doors, keycard, lights, cover, and VIP). Reset methods must be idempotent for the same context. Keep score and timer ownership in the round coordinator, not UI.

## Feedback and UI input capture

```csharp
public interface IFeedbackEventSource
{
    event Action<FeedbackEvent> FeedbackRaised;
}

public interface IGameplayInputCapture
{
    bool IsGameplayInputCaptured { get; }
}
```

Feedback event payloads are presentation-only and cannot mutate authoritative state. UI components implement `IGameplayInputCapture` and report true while a modal board/menu owns keyboard or mouse input. UI subscribes to round/player state for displayed scores and timers; it does not own those values or resolve round outcomes.

## Package and project settings

Visible Meta Files is enabled in `ProjectSettings/VersionControlSettings.asset`; Force Text is enabled in `ProjectSettings/EditorSettings.asset`. Commit `.meta` files with their assets. Do not regenerate GUIDs for another owner's assets. No large runtime binaries have been added; coordinate Git LFS and verify a fresh clone before adding them.
