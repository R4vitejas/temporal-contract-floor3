# Floor 3 demo and viva checklist

Owner: Ravitejas. Issue #14. This is a preparation record; unobserved checks remain pending.

## Machines and network

| Item | Laptop A | Laptop B |
|---|---|---|
| Owner | Ravitejas | Pending selection |
| Demo OS | Ubuntu 24.04.5 LTS proposed; Windows dual boot is also available | Pending |
| CPU | Intel Core i3-1005G1, 4 logical CPUs | Pending |
| GPU | Intel Iris Plus Graphics G1 | Pending |
| RAM | Approximately 16 GB | Pending |
| Unity | 6000.6.8f1 | Pending |
| Build module | Linux Standalone installed locally | Match the selected OS |
| Standalone acceptance | Pending | Pending |

User-selected network plan: hotspot. Record the actual hotspot device, host IPv4 and UDP port after connecting both laptops. Port default is 7777. Both machines must use the same build/source version and port. A successful local two-instance run is not two-laptop evidence. Linux and Windows clients may be built for their respective OS; do not assume a Windows binary runs natively on Ubuntu.

## Launch and hotspot setup

1. Copy the complete standalone build directory, not only its executable. Verify its `build-manifest.json` source commit and executable SHA-256. On Ubuntu, give `Floor3.x86_64` execute permission if the transfer removed it.
2. Connect both laptops to the intended hotspot. Record their IPv4 addresses (`ip -4 address` on Ubuntu, `ipconfig` on Windows). Do not use `127.0.0.1` between laptops.
3. Start the host build, choose the agreed UDP port, then join its hotspot IPv4 from the second laptop. If the host firewall is active, allow the selected game's UDP port on that interface/network. Document the actual rule used; do not disable the firewall globally.
4. If the devices cannot communicate through the hotspot, use a tested alternative router/Ethernet path. Record that actual path. Do not switch to untested campus Wi-Fi on demo day.
5. Both players Ready; host Start. Use F1 for the session menu. Test recovery before rehearsing the walkthrough.

| Network evidence | Result |
|---|---|
| Hotspot device / network | Pending |
| Host OS / IPv4 / UDP port | Pending |
| Joining laptop OS / IPv4 | Pending |
| Firewall configuration actually used | Pending |
| Successful two-laptop connection evidence | Pending |
| Tested fallback network | Pending |

## Builds

**Environment inspection build:** choose **Temporal Contract > Floor > Build Linux Environment Walkthrough**. Output: `Builds/Floor3-environment-only-Linux/`. It defaults to a 1280x720 window. This is a development build of EnvironmentTest, with fixed-open doors and a test controller. It is not the multiplayer demo.

Run its automated headless geometry check on Ubuntu with `./Floor3.x86_64 -batchmode -nographics --floor-smoke-test -logFile smoke.log`. The `--floor-production-capsule-test` option repeats the same traversal using the current network prefab's 2 m / radius 0.5 m capsule dimensions. These are geometry checks, not the real network player or GPU/performance tests.

**Integrated candidate:** after the reviewed Main assembly and clean source commit, choose **Temporal Contract > Floor > Build Linux Integrated Release Candidate**. Output: `Builds/Floor3-integrated-candidate-Linux/`. The build command checks scene readiness, records source commit/Unity version/target, executable hash and working-tree state. It does not mark any gameplay acceptance row passed.

The current Editor normalizes shared Core metadata and local SDK/URP settings during import/build. Those automatic changes were kept out of the floor PRs; agree the shared package/metadata/settings state with Samatma before the clean-source release freeze. The environment development manifest records its dirty import state honestly.

Windows build support is not installed in the current local Editor. If Laptop B uses Windows, arrange the matching Windows module or build the same accepted commit on a teammate's configured Windows Editor. Keep Unity and package versions agreed across the team.

After building, package and checksum the entire directory (the executable alone is only Unity's launcher):

```sh
python3 Game/Assets/Project/Environment/SourceTools/package_build.py Builds/Floor3-environment-only-Linux
```

This creates a ZIP and adjacent ZIP SHA-256, plus per-file hashes and a content hash in the build folder. After extraction, run the same helper with `--verify <extracted-build-directory>` to check all packaged data. The current local environment package is `Builds/Floor3-environment-only-Linux.zip`; its manifest records its exact source commit. It is not the frozen multiplayer release.

Do not commit Builds or recordings to ordinary Git. Preserve the frozen source commit, full build directory, manifest and final video together in the team's agreed shared storage. Add usable shared links here once available.

## Preparation evidence

The environment-only Linux development build succeeded on Unity 6000.6.8f1. Its standalone headless check traversed all four doors with a 1.8 m / radius 0.3 m CharacterController, validated room/map/listener references and exited 0. One short run took 3.59 s with peak resident memory 182,480 KiB (about 178 MiB). This includes startup and teardown, uses no graphics device and says nothing about sustained gameplay FPS.

A graphical 1280x720 run on the local Intel GPU also completed the geometry check and produced a Unity screenshot. Materials and the supplied geometry were visually checked. It was not a timed performance run or multiplayer test.

**Concrete pending handoff:** repeating the same standalone geometry proxy at the current network player's dimensions (2 m tall / radius 0.5 m) failed at Classroom 02, exit 1. Samatma and Ravitejas must agree capsule/door clearance and verify the actual production player. This result is not hidden or labelled complete; Main assembly requires explicit capsule-fit acceptance.

## Acceptance matrix

For every row record source/build, tester, which laptop hosted, what each instance observed and evidence. Repeat actions initiated by both host and client.

| Check | Host observation | Client observation | Evidence / commit |
|---|---|---|---|
| Two players connect; only owner input/camera/listener active | Pending | Pending | |
| Two Ready players required; third/late joins rejected without ending valid session | Pending | Pending | |
| Recognizable floor, room access, collision and visible blocked routes | Pending | Pending | |
| Ordinary doors open/close; refuse closing on another player | Pending | Pending | |
| Attacker-only card; denied panel; one card owner in pickup race | Pending | Pending | |
| Restricted door unlock persists for round; defender can use after unlock | Pending | Pending | |
| Lights toggle consistently; enough ambient light; positional sound identity | Pending | Pending | |
| Chair exclusive hold, one per player, invalid wall/player placement denied | Pending | Pending | |
| Death/disconnect while carrying, including blocked carry point | Pending | Pending | |
| Board local selection/highlight; Escape and overlapping modal input capture | Pending | Pending | |
| Hitscan walls, damage, health, ammo, empty/reload/dead-player restrictions | Pending | Pending | |
| VIP/attacker deaths, timeout and simultaneous terminal precedence | Pending | Pending | |
| Role swap and complete reset of all scene/gameplay state | Pending | Pending | |
| Two rounds, successful-attack scores, draw/win and two-ready rematch | Pending | Pending | |
| Disconnect then new host/join without restarting the application | Pending | Pending | |

| Consecutive standalone match | Source/build | Outcome / rematch / faults |
|---|---|---|
| 1 | Pending | Pending |
| 2 | Pending | Pending |
| 3 | Pending | Pending |

## Performance on actual hardware

Run at 1280x720 on both laptops with recorded quality/vsync/shadow settings. Measure the weaker laptop during actual gameplay, including lights, furniture, both players and gunfire. Record FPS observations over a timed run, frame-time spikes and memory; do not infer performance from triangle count or a headless build. Minimum target: stable 30 FPS; aim for 60. The preparation scene uses shared materials, simple collision and no dynamic light shadows.

| Measurement | Laptop A | Laptop B |
|---|---|---|
| Build / scene / resolution / quality | Pending | Pending |
| Timed run duration and measurement method | Pending | Pending |
| Typical and worst sustained FPS / spikes | Pending | Pending |
| Memory observations | Pending | Pending |
| Optimization made and retest | Pending | Pending |

## Freeze and recording

- [ ] All acceptance rows have actual evidence; known limitations are concrete and agreed.
- [ ] Frozen source commit and clean working tree recorded.
- [ ] Full standalone builds and manifests archived outside Git; retrieval checked on both laptops.
- [ ] Final 30-second recording uses the same frozen source/build and shows environmental interactions clearly.
- [ ] Recording and build links added below; Sanjana coordinates the final video.
- [ ] Team rehearses launch, walkthrough and fallback on actual machines.

Frozen commit: **Pending**. Build links: **Pending**. Video link/version: **Pending**. Target freeze/record/rehearse: November 17, 2026; November 18 is the repair buffer.

## Viva rehearsal

Each person should demonstrate and explain their owned work and this complete path: local input/camera ray -> target interface -> intent RPC -> host sender/range/role/state checks -> replicated state -> local positional audio/HUD feedback.

- Ravitejas: supplied source selection, measured complexity, metres/FBX axis correction, hinge/base pivots, simple collision, LFS retrieval, map provenance, scene assembly and hardware measurements.
- Adhithya: host-owned interactions, card/cover contention, placement validation, reset/death/disconnect behavior and feedback subject identity.
- Samatma: opt-in LAN session, ownership/input, connection approval, hitscan/health, terminal precedence, two-round scoring, roles and reset orchestration.
- Sanjana: local board selection, mouse/input capture, replicated-state display, event-driven spatial audio and recording version.
- Everyone: explain MVP simplifications and pending limitations accurately. Physical haptics waiver is not confirmed by the professor; visual recoil must not be described as physical haptics.

Keep #14 open until the real two-laptop run, performance, three matches, frozen build/video and rehearsal have been completed.
