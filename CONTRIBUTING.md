# Team workflow

## Access and tools

Ravitejas invites collaborators through repository Settings → Collaborators. Accept the invitation, authenticate Git on your computer, and clone the repository into a new directory. Do not clone into a folder already containing a Git repository.

If your AI supports a GitHub connector/plugin, connect your own GitHub account and grant it access to this private repository. Verify it can read README.md. A connector's capabilities vary: repository access does not guarantee local file editing, Git push, or Unity Editor control. Use an authenticated local Git client for operations the connector cannot perform. Never paste tokens into prompts or commit credentials.

Use a local coding assistant or Unity MCP for actual local Unity changes. Each person uses their own checkout and their own Editor session. Only one editing agent/operator controls a checkout at a time.

## Startup gate

Samatma first creates the shared `Game/` project, locks package versions, defines the shared interfaces, and merges the bootstrap PR. Other teammates may inspect assets, prepare the floor plan, and read requirements before then, but must not create competing Unity projects or guess interface signatures.

Bootstrap default: Unity 6000.6.8f1, URP, Input System, Netcode for GameObjects, Unity Transport. Samatma verifies availability on the team machines before locking. If this exact Editor cannot be installed, coordinate a single alternative with Ravitejas before project creation. No individual upgrades afterward.

Before adding large runtime assets, install Git LFS on every machine, commit the agreed tracking patterns, and test a fresh pull. Exclude raw source meshes that are not needed by the runtime project. Do not add binary assets before this check.

## Git rules

- `main` is the integrated version. Work through pull requests; Ravitejas reviews and merges them.
- Branches: `setup/unity-bootstrap`, `feature/network-core`, `feature/environment-interactions`, `feature/ui-feedback`, `feature/floor-integration`.
- Start each task from current main. Commit and push small working changes; open a PR with exact test steps and known issues.
- Do not merge your own PR or force-push shared branches. Coordinate dependency PRs before building on unmerged work.
- Commit Unity `.meta` files, `Assets`, `Packages`, and `ProjectSettings`. Enable Visible Meta Files and Force Text serialization. Do not regenerate another person's asset GUIDs.
- Only Ravitejas edits `Game/Assets/Project/Scenes/Main.unity`.
- Samatma owns package manifests, shared Core/Networking code, and player/combat/round systems.
- Adhithya owns Interactions scripts/prefabs and his test scene.
- Sanjana owns UI/Feedback scripts/prefabs and her test scene.
- Ravitejas owns Environment assets, the main scene, and integration.
- Ask the owner before changing their files. Do not resolve scene conflicts by discarding someone else's work.

## Feature completion

A feature is complete only when it compiles, works in Play Mode, and its shared behavior is checked from both host and client. A local-only map/UI feature must be checked for correct input focus and lack of unintended remote effects. Distinguish automated checks from manually observed checks; report untested behavior honestly.

Every PR includes: behavior changed; setup/Inspector requirements; test instructions; host/client results; known issues; integration dependencies. Include the commit hash in the team chat.

Do not claim that a GitHub push proves the game works. Do not add features outside the assigned MVP.
