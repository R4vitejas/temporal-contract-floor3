# Ravitejas — coordination, floor, and integration

## Immediate actions

1. Invite Samatma, Sanjana, and Adhithya as repository collaborators and send the group message plus their individual brief links.
2. Collect each person's OS, CPU, RAM, GPU, available disk space, installed Unity version, and available work hours. Select two demo machines and use the weaker one as the performance baseline.
3. Give the team access to the supplied assets through the existing shared drive or another agreed file link. A private Git repository invitation does not automatically share the external asset folder. Never paste your local /media path as if teammates can access it.
4. Tell Samatma to begin the bootstrap. Others prepare assets/map and read requirements until it merges.

## Give this implementation brief to your AI

Read README.md, CONTRIBUTING.md, docs/PROJECT_BRIEF.md, and docs/STATUS.md. Own Environment assets, Main.unity, integration, builds, and final acceptance. Wait for the shared bootstrap before creating Unity content; do not create a competing project or modify teammates' owned code without coordinating.

Original assets on this machine: /media/ravitejas/1A21-85E1/PES/SEM 5/Current Stuff/ARVR/Mini Project/3-20261115T095203Z-1-001/3/

Inspect Blender sources and the separate meshes before importing. The lab.obj is approximately 550 MB; inspect complexity rather than assuming it is suitable for runtime. Keep originals untouched. Retain the entire supplied floor, all rooms/corridors and existing props. Choose gameplay locations within it; do not crop the layout. Check scale, normals, duplicated geometry, missing textures, pivots, and collision. Export cleaned runtime FBX assets with separate interactive doors and furniture. Provide Sanjana an accurate top-view floor layout.

After agreed LFS setup, add only cleaned runtime assets. Keep raw source outside ordinary Git. Use simple colliders, restrained material/texture sizes, limited dynamic lights and shadows, and room-level static grouping. Do not add artificial corridor blockers to shrink the playable floor. Do not merge interactive doors into architecture.

Create Main.unity after bootstrap. Integrate Samatma's network/player/round foundation, Adhithya's interaction prefabs, and Sanjana's board/UI/feedback prefabs using documented Inspector references. Resolve integration through the relevant owner when code changes are needed.

## Integration order

Bootstrap → two-player movement → shared door → playable floor → keycard/panel → board → lights → furniture → combat/round integration → polish. Networking and floor preparation can progress independently, but every shared feature must pass a two-client check before it is accepted.

## Release acceptance

Build for the actual demo operating systems; verify required Unity build modules early. Test two real laptops on the intended hotspot/router/Ethernet, including firewall access and campus Wi-Fi isolation. Confirm all checks in PROJECT_BRIEF, stable 30 FPS at 720p on the weaker machine, and three complete matches without restarting the Editor. Preserve the known-good build and commit. Maintain STATUS.md after merges. Freeze Tuesday; use Wednesday for repairs.

Everyone must explain their subsystem and the full interaction path in viva: input → targeting → request → host validation → replicated state → feedback. Keep the demo focused on environmental interactions. No optional temporal feature until all required checks pass.
