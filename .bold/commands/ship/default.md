---
command: bold.ship
subcommand: default
description: Draft a PR (or finalize the deliverable, for non-code domains) for the active feature.
collector: collect-ship-context
args:
  - name: --draft
    description: Open as a draft PR regardless of readiness.
---

# bold.ship

Most of this command is advisory: an open waiver, or a dirty tree once the hard gates below have passed, is something to surface and explain — never a hard block. The human decides whether to ship anyway, fix first, or stop. **Two things aren't advisory.** First, unconditionally, the collector itself hard-fails — nonzero exit, no JSON — if the branch is behind `base_branch` (`git_status.proceed` would read `false`); drafting a PR against a base that's already moved packages the wrong diff, and this is the same check `bold.ship review` shares (§ below). Second, run the collector with `--publish`/`-Publish`: on a Feature-tier feature (matched by the current branch name), it additionally hard-fails unless the spec is `Complete` with every task checked, `gates/analyze.md`/`critic.md`/`checklist.md` are clean, those gate files were actually committed *before* implementation started (not backfilled after the fact once something noticed they were missing), and the tree is otherwise clean. Either way, if the collector exits nonzero, stop immediately, show the human the exact failure, and route back to `bold.plan` / `bold.build`. **Never hand-author the missing gate files, merge/rebase blindly just to clear the check, or otherwise patch the check into passing** — that defeats the entire point of a pre-flight gate. (The `--publish` gate exists because exactly its failure happened live — see `bold-docs/patches.md`, 2026-07-16; the branch-sync gate exists because two independently-planned features collided on the same id after shipping against a base that had already moved — see `bold-docs/patches.md`, 2026-07-18.)

## Definition of Done

Done when the human has explicitly chosen create, update, adjust, or stop — and, for create/update, the PR actually reflects the confirmed draft. Drafting a body and never asking, or acting before confirmation, is not done.

## Before you begin

Run the collector with `--publish`/`-Publish` (only this subcommand passes that flag — `bold.ship review` reuses the same collector but must stay ungated for the `--publish`-specific checks, since it runs against work that isn't finished yet by design; the branch-sync gate above applies to both regardless). Read its output. If `has_uncommitted_changes` is true, say so as a warning, not a refusal — `bold.ship` drafts from committed history, so an uncommitted change simply won't be in the PR yet.

## Draft

Open the PR body with the Product Owner TL;DR (per §8) — a PR is a generated artifact like any other. Then:

- **Summary** — the TL;DR's *What*, expanded to a paragraph if needed
- **Changes** — from `changed_files` and `commits_ahead`; scope to what actually changed, don't re-describe the whole feature if this is the third PR against it
- **Task completion** — N/M tasks done
- **Quality gates** — `bold.plan checklist`/`analyze`/`critic` status, or "no gate artifacts found" if the tier didn't require them
- **Waivers** — every entry in `active_features[].waivers`, plainly stated (a waiver is a visible tradeoff, not a way to make a gate quietly disappear) — omit the section if there are none
- **Reference** — path to the feature's spec

For non-code domains, "draft a PR" means finalize the deliverable document instead — same content bar (what changed, why, what's still open), different container.

## Confirm before acting

Show the drafted title and body and ask explicitly: create, update the existing PR, adjust the draft, or stop. Don't open or update anything until the human confirms.

## Boundary

This step packages what's already been built. It doesn't re-run `bold.build`'s gates itself — the `--publish` hard gate above checks the *artifacts* those gates should have produced, not the gates in motion — so if `bold.build` hasn't been run at all, route back to it rather than shipping ungated work.
