# Adhithya — interactions and shared presentation work

## Give this brief to your AI

Read README.md, CONTRIBUTING.md, docs/PROJECT_BRIEF.md, and docs/STATUS.md. Before implementation, wait for the bootstrap merge and read docs/INTEGRATION.md. Use the established interaction/reset/feedback contracts. Do not create a new Unity project, networking stack, player controller, or interaction framework.

Work on feature/environment-interactions. Own Game/Assets/Project/Interactions and its prefabs, plus your own interaction test scene. Do not edit Main.unity, shared Core/Networking, package configuration, or production player/round systems. As additionally assigned on November 19, you may also implement UI/Feedback issues #8–#11 alongside Sanjana; follow docs/tasks/SANJANA.md and the issue-specific branches/contracts. Reuse existing work and coordinate asset edits. Interactions #6/#7 are accepted; PR #20 review and remaining presentation work are current priorities.

## Deliver in this order

1. Networked ordinary door: E open/close, valid collision, stable pivot, clear prompt, host-authoritative shared state. Use a placeholder door if the final floor door is not separated yet. Coordinate the first two-client test with Samatma.
2. Keycard + access panel + restricted door: attacker-only pickup, host inventory validation, denied/success feedback events, unlock persistent for the round. Both players can use the door once unlocked. Only one pickup succeeds if requests race.
3. Wall switch + assigned room lights: shared on/off state, usable by either player, feedback events. Coordinate final light styling with Sanjana.
4. Movable chair/desk: request exclusive hold, attach visually to the holder, host-check carry/placement, reject wall/player overlap, release/reset safely when holder dies or disconnects. Keep physics controlled. No throwing or freeform rotation.
5. Register all owned state with the shared round-reset mechanism. Reset to baseline transforms, locks, inventory, and light states.

Emit the agreed feedback events for sound/UI instead of importing another teammate's feedback assets. Temporary debug feedback is fine until integrated. Each prefab must work independently of the final floor scene through serialized references/components, without hardcoded scene object names.

## Test and handoff

Check each interaction initiated by host and by client, denied access, repeated use, simultaneous pickup/hold requests, invalid placement, player death while holding, and full round reset. Provide a PR with prefab locations, required Inspector references, test steps/results, screenshots if useful, and known issues. Ravitejas places your prefabs into the floor scene. Do not build echoes, AI guards, or additional interaction categories.
