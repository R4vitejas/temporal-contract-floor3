# 30-second gameplay submission: plan and rehearsal

Issue #11. **Status: plan only. The final capture is PENDING** until a verified integrated build exists. Nothing here
has been recorded, and no mock or Editor footage will be used for the submission.

## Rules for the final video

- Real running gameplay from the frozen build on the real demo setup, not Editor setup screens or fabricated mock footage.
- Exactly 30 seconds; opens on another teammate's device; sound audible, not clipping.
- Matches the frozen build's behavior. Cut the shot list to what actually works at recording time.
- The finished MP4 is saved in the agreed shared location, never in ordinary Git. After recording, add the filename or
  link and the build commit to issue #11.

## Shot list (adjust to working behavior)

| Time | Shot | What must be visible | Depends on |
|---|---|---|---|
| 0-4 s | Recognizable Floor 3, two-player session | Both players in the floor; HUD with role and timer; host + join already connected | Ravitejas's Main scene, Samatma's session |
| 4-9 s | Classroom board map | Walk to the board, E, map opens, click a room (name appears), Esc closes and movement returns | Board in Main (`docs/BOARD_MAP.md`) |
| 9-15 s | Keycard, access panel, door | Attacker picks up the card, panel unlocks the restricted door (accepted message and sound), door opens; a denied attempt by the defender if time allows | Interactions (`docs/INTERACTIONS.md`), feedback sounds |
| 15-21 s | Lights and movable cover | Wall switch toggles lights for both players; carry and place a chair | Interactions |
| 21-27 s | Encounter and VIP objective | Shots, hit feedback, VIP, health and ammo on the HUD | Samatma's combat/rounds, feedback hooks |
| 27-30 s | Result and reset | Round result screen, then the reset to the next round or match result | Round coordinator, HUD result screens |

Keep every cut to a single continuous take where possible; a second-machine view is optional picture-in-picture only if
both machines record cleanly. Keep the HUD at 1280x720 so text is readable after compression.

## Recording setup checklist (to run before the final take)

1. Pick the recording machine (the stronger of the two demo laptops) and confirm it holds 30 FPS at 720p in the build.
2. Install and test screen-capture software (for example OBS Studio) with: 1280x720 canvas, 30 FPS, MP4/H.264, desktop
   audio only, game window capture. Do a 10-second test, play it back on another device, and check audio levels (peaks
   below clipping).
3. Close notifications and overlays; windowed or borderless build at 1280x720; hide the OS cursor except during the board shot.
4. Rehearse the whole 30-second route from the shot list at least twice with the second player, with a stopwatch, until it
   fits in 30 seconds without rushing. Note any shot that fails and drop it rather than faking it.
5. Record several takes; keep the best one that matches the shot list and the frozen build.

## Rehearsal script (two players)

- Host (attacker) starts a match; both mark Ready; Start.
- Attacker: take the keycard, use the panel, open the restricted door. Defender: toggle the light switch, carry a chair.
- Attacker walks to the board, opens the map, selects a room, closes it. Both fight; attacker reaches the VIP.
- Let the round resolve, show the result screen, and cut as the reset begins.

## Acceptance (copy into the issue when done)

- [ ] Capture is actual running gameplay from the frozen build.
- [ ] Environmental interactions are readable; sound is audible without clipping.
- [ ] Duration is exactly 30 seconds; the file opens on another teammate's device.
- [ ] Video matches the frozen build's behavior; the build commit is recorded in the issue.
- [ ] MP4 saved in the agreed shared location (not Git); filename or link posted in #11.

## Open dependencies (not blockers for preparing)

Integrated build and release acceptance (issues #13 and #14), the production HUD adapter, the feedback sounds bound in the
scene, and a second demo player. Until the build is frozen, report the final capture as pending.
