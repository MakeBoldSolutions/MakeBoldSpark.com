#!/usr/bin/env bash
# Collector for bold.build (default and status). Emits deterministic facts —
# per-feature ratified tier, backbone principle enforcement status, detected
# test-runner config, and working-tree cleanliness — so gates are decided
# from ground truth instead of re-derived by reading the tree by hand.
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$script_dir/lib/common.sh"

repo_root="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
docs_dir="$repo_root/bold-docs"
cd "$repo_root"
append_run_log "$repo_root" "build" "collect-gate-status"

bold_sync_git_remote "$repo_root"
collect_git_status_vars "$repo_root"

# Hard gate, before build: a build spends a full cycle of work on top of
# whatever's currently checked out -- if that base has already moved on
# origin, everything built this cycle is built on ground that's about to
# need reconciling. Nothing previously fetched or reported this before a
# build ran; the base moved mid-flight, silently, and the divergence
# surfaced externally instead (bold-docs/patches.md, 2026-07-18).
if [ "$GIT_STATUS_PROCEED" != "true" ]; then
  echo "GATE FAILED: $GIT_STATUS_MESSAGE" >&2
  exit 1
fi

active_features="$(collect_active_features "$docs_dir")"
backbone_principles="$(collect_backbone_principles "$docs_dir")"
stale_references="$(collect_stale_references "$repo_root" "$docs_dir")"

known_test_configs=(jest.config.js jest.config.ts pytest.ini tox.ini .mocharc.json .mocharc.yml karma.conf.js phpunit.xml)
test_config_present=()
for f in "${known_test_configs[@]}"; do
  [ -e "$f" ] && test_config_present+=("$f")
done
while IFS= read -r f; do
  [ -n "$f" ] && test_config_present+=("$f")
done < <(find . -maxdepth 2 -iname '*.tests.csproj' -printf '%P\n' 2>/dev/null; printf '\n')

has_uncommitted_changes=false
if git rev-parse --git-dir >/dev/null 2>&1; then
  [ -n "$(git status --porcelain 2>/dev/null)" ] && has_uncommitted_changes=true
fi

# Hard gate, before build: a Feature-tier feature (matched by the current
# branch name -- bold.plan default checks out a branch named after the
# feature id) can't start bold.build until its pre-flight gates
# (analyze/critic/checklist) exist and are clean. This exits nonzero --
# a hard stop the agent can't reason its way past -- rather than leaving
# the decision to whoever reads the JSON below.
current_branch="$GIT_STATUS_CURRENT_BRANCH"
if [ -n "$current_branch" ] && [ "$current_branch" != "unknown" ] && [ -f "$docs_dir/features/$current_branch/spec.md" ]; then
  feature_tier="$(sed -n 's/^\*\*Tier\*\*: //p' "$docs_dir/features/$current_branch/spec.md" | head -1)"
  if [ "$feature_tier" = "Feature" ]; then
    mapfile -t gate_problems < <(check_feature_gates_clear "$docs_dir" "$current_branch")
    if [ "${#gate_problems[@]}" -gt 0 ] && [ -n "${gate_problems[0]}" ]; then
      {
        echo "GATE FAILED: bold.build cannot start on '$current_branch' (Feature tier) until its pre-flight gates are clean:"
        printf '  - %s\n' "${gate_problems[@]}"
        echo "Run bold.plan analyze / bold.plan critic / bold.plan checklist first. Do not hand-write these files to satisfy this check -- that defeats the point of a pre-flight gate."
      } >&2
      exit 1
    fi
  fi
fi

git_status_json="$(collect_git_status_json_from_vars)"

printf '{"active_features":%s,"backbone_principles":%s,"stale_references":%s,"test_config_present":%s,"has_uncommitted_changes":%s,"git_status":%s}\n' \
  "$active_features" "$backbone_principles" "$stale_references" "$(json_array "${test_config_present[@]}")" "$has_uncommitted_changes" "$git_status_json"
