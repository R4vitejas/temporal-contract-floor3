# MVP work board

[All issues](https://github.com/R4vitejas/temporal-contract-floor3/issues) · [Single delivery milestone](https://github.com/R4vitejas/temporal-contract-floor3/milestone/1)

## November 19 update

Adhithya (@RealAdhi) is additionally assigned to #8–#11 because Sanjana's laptop cannot run Unity; she remains assigned. He may implement and test UI/Feedback under the existing presentation contracts. Interactions #6/#7 are accepted and closed after the repaired fallback passed independent retests. Feedback PR #20 is under review; board/HUD work remains unpublished. Full-floor/integration draft #19 supersedes cropped prototype #18. See [current status](STATUS.md).

## Ready foundation

Bootstrap PR #2 is merged on main. Use Unity 6000.6.8f1 and the pinned package lock. Read README, CONTRIBUTING, PROJECT_BRIEF and INTEGRATION before making changes. Do not recreate the project or upgrade packages individually.

## Assignments and start order

| Owner | Issues | Start now | Final integration needs |
|---|---|---|---|
| Samatma (@SamatmaAB) | [#3 LAN corrections](https://github.com/R4vitejas/temporal-contract-floor3/issues/3), [#4 combat](https://github.com/R4vitejas/temporal-contract-floor3/issues/4), [#5 rounds](https://github.com/R4vitejas/temporal-contract-floor3/issues/5) | Correct PR #1 first; use a placeholder arena for combat and test-triggered events/mock reset participants for rounds | Real interaction role/death/reset hooks; presentation bindings |
| Adhithya (@RealAdhi), interactions | #6 interactions and #7 lifecycle — accepted and closed | PR #16 merged; independent release-safety retests pass | Verify production player/round/floor bindings under #13 |
| Sanjana (@sanjanaaravamudhan) and Adhithya (@RealAdhi), presentation | [#8 board](https://github.com/R4vitejas/temporal-contract-floor3/issues/8), [#9 UI](https://github.com/R4vitejas/temporal-contract-floor3/issues/9), [#10 feedback](https://github.com/R4vitejas/temporal-contract-floor3/issues/10), [#11 video](https://github.com/R4vitejas/temporal-contract-floor3/issues/11) | Start board with placeholder image and mock input actor; UI/feedback use mock data/events. Prepare recording setup early | Production input/state adapters and accurate floor artwork; final video needs frozen build |
| Ravitejas (@R4vitejas) | [#12 floor assets](https://github.com/R4vitejas/temporal-contract-floor3/issues/12), [#13 main scene](https://github.com/R4vitejas/temporal-contract-floor3/issues/13), [#14 release/viva](https://github.com/R4vitejas/temporal-contract-floor3/issues/14) | Full-floor draft PR #19; seminar repair and collision verified; Main/release preparation on feature/floor-integration | Final assembly needs reviewed subsystem PRs; release needs real laptops and integrated scene |

All teammates have repository access. Adhithya can develop presentation against mocks and the shared contracts while production integration continues.

## Parallel work rules

- Build the subsystem against agreed interfaces in an isolated test scene. Use local mocks/placeholders for missing inputs; do not make production substitutes for someone else's system.
- Publish public events/methods and Inspector wiring early in an owned handoff document. Coordinate additions to Core through Samatma. Document an unavailable binding as pending rather than waiting silently.
- Test local behavior first. Validate real cross-system behavior once corresponding PRs are available. A mock pass is not proof of network integration.
- Keep test player/menu assets out of production. Keep production session creation opt-in so independent test scenes remain runnable.
- Only Ravitejas edits Main.unity. Each owner keeps code/prefabs in the assigned directories; ask before touching another owner's files.
- Avoid concurrent AI editing sessions in one checkout. Each teammate uses their own branch and local Unity Editor.

## Starting an issue

Accept repository access, authenticate Git, pull main, then create the branch named in the issue. Existing owners first incorporate main into their active branches. Do not discard local work when switching branches.

Give your AI the issue URL and this instruction:

> Read the repository instructions, docs/INTEGRATION.md, docs/WORK_BOARD.md and this issue before editing. Inspect existing implementation and reuse it. Work only within the issue's ownership boundaries, using the requested branch. Keep the subsystem testable with mocks/placeholders for unfinished dependencies. Do not implement another owner's work. Compile and perform the issue's relevant tests; distinguish observed results from pending checks. Push a focused PR with setup, exact tests, known issues, dependency PRs and Closes #<issue-number>. Do not merge your own PR.

Connect a GitHub plugin/connector if your AI supports it and grant this repository access. Verify it can read the issue. A GitHub connector is not automatically a local file editor or Unity controller; use an authenticated local Git client and a local coding assistant/Unity MCP for those capabilities. Never paste access tokens into prompts.

## PR and completion rules

- Bootstrap: PR #2 merged. Network first slice: PR #1 needs fixes under #3 before merge.
- Feature PRs should target main and reference an issue. If a stacked PR is necessary within one owner's subsystem, declare its base/dependency and retarget after the base merges.
- Include behavior, prefab/Inspector setup, test results, screenshots/log summaries where useful, and known limits. Do not claim tests not performed.
- Ready for review is not the same as accepted. Ravitejas reviews/merges; required host/client/integration checks must pass before the linked issue is closed as complete.
- Phase gates are foundation → parallel feature delivery → integration/release. Final assembly, real two-laptop verification and final recording necessarily depend on feature delivery; they are not prerequisites for isolated feature development.
- Tuesday November 17 is the freeze/record/rehearse target; Wednesday is repairs. No extra temporal mechanics before the MVP passes.
