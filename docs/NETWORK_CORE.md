# Network core first slice

The first network-core pass provides a temporary LAN menu, two-player connection approval, replicated Ready/Start state, owner-only FPS movement, and disconnect recovery. The session controller owns connection setup; gameplay systems do not call Unity Transport directly.

## Run the LAN test scene

1. Open the project at `Game/` with Unity `6000.6.8f1`.
2. In Build Settings, use `Assets/Project/Scenes/NetworkCoreTest.unity` (already first in the build list). It contains a simple floor for the network player.
3. Enter Play Mode or launch a Windows build. The temporary `Temporal Contract — LAN` menu appears.
4. On the host, choose port `7777` and click **Host**. Share the LAN IPv4 address shown in the menu with the second laptop.
5. On the joining laptop, enter the host IPv4 address and matching port, then click **Join**. Allow inbound UDP on the host firewall for that port.
6. Both players select **Mark Ready**. The host can start only when exactly two players are connected and both are ready. F1 toggles the session menu during the match.

The menu rejects a third player and all late joins after Start. A disconnect closes the session and returns both running instances to a usable host/join menu. The default port is configurable in the menu; use the same UDP port on both devices.

## Generated network assets

Use **Temporal Contract → Build Network Core Assets** to regenerate the network player and lobby prefabs under `Assets/Project/Prefabs/Resources/NetworkCore/` and the test scene. The runtime session controller loads these resources and configures `NetworkManager` plus `UnityTransport`.

## Current scope

This slice does not yet include damage, weapons, VIP logic, round timers/results, rematch, or reset orchestration. Host/client behavior and real two-laptop LAN connectivity still need testing before those systems can be called complete.
