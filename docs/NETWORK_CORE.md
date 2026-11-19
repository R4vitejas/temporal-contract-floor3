# LAN network core

This review slice adds an opt-in LAN session menu, a strict two-player limit, Ready/Start gating, owner-controlled FPS movement, synchronized player transforms, and disconnect recovery. Session startup is attached only to the network test scene, so opening another teammate's test scene does not replace its `NetworkManager`, player prefab, or approval callback.

## Run the LAN test scene

1. Open `Game/` in Unity `6000.6.8f1`.
2. Run `Assets/Project/Scenes/NetworkCoreTest.unity` from Build Settings. The scene contains the `Network Core Session (Opt In)` object with its own `NetworkManager`, `UnityTransport`, and `LanSessionController`.
3. Enter Play Mode or launch a Windows build. On one instance choose port `7777` and click **Host**.
4. On the second instance enter the host IPv4 address and port, then click **Join**. Permit inbound UDP on the selected port at the host firewall.
5. Both players select **Mark Ready**. The host can start only with exactly two connected, Ready players. F1 toggles the in-match session menu once per key press.

The host and joining player receive separate spawn points. The CharacterController is centered one meter above the root while the visible capsule and camera align with the floor. The player prefab enables only the owning client's camera, audio listener, and input.

The menu rejects a third player and all late joins after Start. A disconnect closes the session and returns both instances to a usable host/join menu. Per-session connection tracking is cleared so the same instance can host or join again. The default port is editable; use the same UDP port on both devices.

## Input capture

`NetworkFpsPlayer` implements `IGameplayInputCapture`. A board or menu acquires a local modal lock with `SetGameplayInputCaptured(this, true)` and releases the same owner with `SetGameplayInputCaptured(this, false)` on close or destroy. Multiple owners can hold input at once; releasing one does not unlock the others.

## Generated assets and checks

Use **Temporal Contract → Build Network Core Assets** to regenerate the network player and lobby prefabs under `Assets/Project/Prefabs/Resources/NetworkCore/` and the isolated test scene. Unity 6000.6.8f1 headless asset generation, project import, and C# compilation have succeeded. Host/client Play Mode, third/late-join rejection, F1, movement, disconnect/re-host, and two-laptop LAN checks remain pending.

Combat/VIP and round/rematch implementation are stacked separately in #4 and #5; this PR does not claim those systems are complete.
