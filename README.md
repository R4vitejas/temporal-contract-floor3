# Temporal Contract — Floor 3

Two-player desktop LAN game built around environmental interactions in the supplied university third-floor model.

## Start here

1. Read [the project brief](docs/PROJECT_BRIEF.md).
2. Read [the team workflow](CONTRIBUTING.md).
3. Read your task brief: [Ravitejas](docs/tasks/RAVITEJAS.md), [Samatma](docs/tasks/SAMATMA.md), [Sanjana](docs/tasks/SANJANA.md), [Adhithya](docs/tasks/ADHITHYA.md).
4. Check [project status](docs/STATUS.md) before starting implementation.

This repository initially contains planning and handoff documents only. The Unity project has not been created. Samatma owns the bootstrap; teammates must not independently create replacement projects.

## Schedule

- Sunday, November 15, 2026: bootstrap, floor inspection, two-player test, synchronized door.
- Monday, November 16: integrate environmental interactions and the game loop.
- Tuesday, November 17: build, test, record, rehearse, freeze.
- Wednesday, November 18: repair buffer.

## Repository layout

- `Game/`: shared Unity project, created during bootstrap.
- `docs/`: requirements, status, task briefs, and integration instructions.
- Raw supplied source assets stay outside Git until their size and sharing strategy are agreed. Never push the 550 MB raw lab OBJ through ordinary Git.

## Demo

Two laptops, each with keyboard and mouse. One hosts; the other joins its LAN IP. Test on one laptop using two instances during development, then verify the actual two-laptop connection. Internet matchmaking and join codes are not required for the MVP.
