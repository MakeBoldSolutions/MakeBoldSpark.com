#!/usr/bin/env bash
# Collector for bold.ship (default and review). Emits deterministic facts —
# branch position relative to the base branch, changed-file inventory,
# active feature tiers, and backbone status — so drafting or reviewing a PR
# starts from current, structured ground truth.
set -euo pipefail

publish=false
while [ $# -gt 0 ]; do
  case "$1" in
    --publish) publish=true; shift ;;
    *) shift ;;
  esac
done

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$script_dir/lib/common.sh"

repo_root="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
docs_dir="$repo_root/bold-docs"
cd "$repo_root"
append_run_log "$repo_root" "ship" "collect-ship-context"

bold_sync_git_remote "$repo_root"
collect_git_status_vars "$repo_root"
base_branch="$GIT_STATUS_BASE_BRANCH"
current_branch="$GIT_STATUS_CURRENT_BRANCH"
commits_ahead="$GIT_STATUS_COMMITS_AHEAD"
commits_behind="$GIT_STATUS_COMMITS_BEHIND"

changed_files=()
if [ "$current_branch" != "$base_branch" ]; then
  while IFS= read -r f; do
    [ -n "$f" ] && changed_files+=("$f")
  done < <(git diff --name-only "$GIT_STATUS_COMPARED_AGAINST...HEAD" 2>/dev/null; printf '\n')
fi

has_uncommitted_changes=false
[ -n "$(git status --porcelain 2>/dev/null)" ] && has_uncommitted_changes=true

active_features="$(collect_active_features "$docs_dir")"
backbone_principles="$(collect_backbone_principles "$docs_dir")"

# Hard gate, unconditional (applies to bold.ship default AND bold.ship
# review -- both share this collector): a review or a drafted PR against a
# base that's already moved is reviewing/packaging the wrong diff.
# ship/review.md has long claimed this is "the one rule that isn't
# advisory," but nothing enforced it mechanically until now -- see
# bold-docs/patches.md, 2026-07-18.
if [ "$GIT_STATUS_PROCEED" != "true" ]; then
  echo "GATE FAILED: $GIT_STATUS_MESSAGE" >&2
  exit 1
fi

# Hard gate, before publish: only bold.ship (default) passes --publish --
# bold.ship review reuses this same collector but must stay ungated,
# since it runs against work that isn't finished yet by design.
# Blocks opening/updating a PR until: the active feature (matched by the
# current branch name) is Complete with every task checked, its
# Feature-tier pre-flight gates are clean AND were actually committed
# before implementation started (not backfilled after the fact -- the
# exact failure this gate exists to catch), and nothing is sitting
# uncommitted. This exits nonzero rather than leaving the decision to
# whoever reads the JSON below -- and never accepts freshly-authored gate
# files as a fix, only ones that predate the implementation commits.
if [ "$publish" = true ] && [ -n "$current_branch" ] && [ -f "$docs_dir/features/$current_branch/spec.md" ]; then
  spec="$docs_dir/features/$current_branch/spec.md"
  ship_problems=()
  feature_status="$(sed -n 's/^\*\*Status\*\*: //p' "$spec" | head -1)"
  feature_tier="$(sed -n 's/^\*\*Tier\*\*: //p' "$spec" | head -1)"
  [ "$feature_status" = "Complete" ] || ship_problems+=("spec.md status is '${feature_status:-unset}', not Complete")
  grep -qE '^[[:space:]]*-[[:space:]]*\[ \]' "$spec" 2>/dev/null && ship_problems+=("spec.md has unchecked tasks")
  if [ "$feature_tier" = "Feature" ]; then
    mapfile -t clear_problems < <(check_feature_gates_clear "$docs_dir" "$current_branch")
    [ "${#clear_problems[@]}" -gt 0 ] && [ -n "${clear_problems[0]}" ] && ship_problems+=("${clear_problems[@]}")
    mapfile -t order_problems < <(check_feature_gates_predate_impl "$docs_dir" "$current_branch")
    if [ "${#order_problems[@]}" -gt 0 ] && [ "${order_problems[0]}" != "no-baseline" ]; then
      ship_problems+=("${order_problems[@]}")
    fi
  fi
  # Excludes run-log.jsonl: append_run_log above just wrote to it, so it's
  # always "dirty" the moment this collector runs -- that's the collector's
  # own expected bookkeeping, not leftover user work worth blocking on.
  if [ -n "$(git status --porcelain -- . ':!bold-docs/run-log.jsonl' 2>/dev/null)" ]; then
    ship_problems+=("working tree has uncommitted changes")
  fi
  if [ "${#ship_problems[@]}" -gt 0 ]; then
    {
      echo "GATE FAILED: bold.ship cannot publish '$current_branch' until:"
      printf '  - %s\n' "${ship_problems[@]}"
      echo "Do not hand-author missing gate files or edit run history to satisfy this check -- go back to bold.plan / bold.build and do the work for real."
    } >&2
    exit 1
  fi
fi

git_status_json="$(collect_git_status_json_from_vars)"

printf '{"base_branch":"%s","current_branch":"%s","commits_ahead":%s,"commits_behind":%s,"changed_files":%s,"has_uncommitted_changes":%s,"active_features":%s,"backbone_principles":%s,"git_status":%s}\n' \
  "$(json_escape "$base_branch")" "$(json_escape "$current_branch")" "$commits_ahead" "$commits_behind" \
  "$(json_array "${changed_files[@]}")" "$has_uncommitted_changes" "$active_features" "$backbone_principles" "$git_status_json"
