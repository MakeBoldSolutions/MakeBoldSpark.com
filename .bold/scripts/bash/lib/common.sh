#!/usr/bin/env bash
# Shared helpers for bold's bash collector scripts.

bold_user_slug() {
  # Filesystem-safe slug identifying the current contributor for
  # .bold-user/{slug}/ (bold-tool-plan.md §17 #4 — committed, per-user
  # tier). Falls back from git user.name to the email's local part, then
  # to "shared" if neither is configured. $1 (optional) scopes the git
  # config lookup to a specific repo root instead of the caller's cwd --
  # required, not cosmetic: a script invoked with --root pointing elsewhere
  # must not silently read the invoking shell's own git identity instead.
  local root="${1:-}" name email slug
  if [ -n "$root" ]; then
    name="$(git -C "$root" config user.name 2>/dev/null || true)"
  else
    name="$(git config user.name 2>/dev/null || true)"
  fi
  if [ -z "$name" ]; then
    if [ -n "$root" ]; then
      email="$(git -C "$root" config user.email 2>/dev/null || true)"
    else
      email="$(git config user.email 2>/dev/null || true)"
    fi
    name="${email%%@*}"
  fi
  [ -n "$name" ] || name="shared"
  slug="$(printf '%s' "$name" | tr '[:upper:]' '[:lower:]' | sed -E 's/[^a-z0-9]+/-/g; s/^-+//; s/-+$//')"
  [ -n "$slug" ] || slug="shared"
  printf '%s' "$slug"
}

# Appends one JSONL invocation line to bold-docs/run-log.jsonl — Bold's
# minimal run observability (bold-tool-plan.md §16, Industry-Alignment
# item 2). Invocation facts only: a collector runs before the work and
# cannot know outcomes. No-op when the repo has no bold-docs/ workspace,
# or when BOLD_NO_RUN_LOG=1 (fixture tests, parity checks).
append_run_log() {
  local repo_root="$1" command="$2" collector="$3"
  [ "${BOLD_NO_RUN_LOG:-}" = "1" ] && return 0
  [ -d "$repo_root/bold-docs" ] || return 0
  printf '{"ts":"%s","command":"%s","collector":"%s","user":"%s"}\n' \
    "$(date -u +%Y-%m-%dT%H:%M:%SZ)" \
    "$(json_escape "$command")" \
    "$(json_escape "$collector")" \
    "$(json_escape "$(bold_user_slug "$repo_root")")" \
    >> "$repo_root/bold-docs/run-log.jsonl"
}

json_escape() {
  # Escapes backslash/quote first (order matters), then newlines, CR, and
  # tabs -- found via compose-layers.sh, the first caller to ever pass
  # multi-line content (backbone.md fragments) through this helper.
  printf '%s' "$1" | sed 's/\\/\\\\/g; s/"/\\"/g' | sed ':a;N;$!ba;s/\n/\\n/g; s/\r/\\r/g; s/\t/\\t/g'
}

json_array() {
  local parts=()
  for item in "$@"; do
    parts+=("\"$(json_escape "$item")\"")
  done
  if [ "${#parts[@]}" -eq 0 ]; then
    echo "[]"
  else
    (IFS=,; echo "[${parts[*]}]")
  fi
}

# Emits a JSON array of repo-relative paths under bold-docs/system/
collect_system_docs() {
  local repo_root="$1"
  local docs_dir="$2"
  local files=()
  if [ -d "$docs_dir/system" ]; then
    mapfile -t files < <(find "$docs_dir/system" -type f ! -name '.gitkeep' | sed "s|^$repo_root/||" | sort)
  fi
  json_array "${files[@]}"
}

# Emits a JSON array of {principle,reason,ratified_by,date}, one per
# `- Waiver: ...` line in the given spec file. See source/commands/WAIVERS.md
# for the line format.
collect_waivers_for_spec() {
  local spec="$1"
  local entries=()
  local line principle reason ratified_by date
  while IFS= read -r line; do
    [ -n "$line" ] || continue
    principle="$(printf '%s' "$line" | sed -E 's/.*principle=([0-9]+).*/\1/')"
    reason="$(printf '%s' "$line" | sed -E 's/.*reason="([^"]*)".*/\1/')"
    ratified_by="$(printf '%s' "$line" | sed -E 's/.*ratified_by="([^"]*)".*/\1/')"
    date="$(printf '%s' "$line" | sed -E 's/.*date=([0-9-]+).*/\1/')"
    entries+=("{\"principle\":${principle:-null},\"reason\":\"$(json_escape "$reason")\",\"ratified_by\":\"$(json_escape "$ratified_by")\",\"date\":\"$(json_escape "$date")\"}")
  done < <(grep -oE '^- Waiver: .*' "$spec" 2>/dev/null; printf '\n')
  if [ "${#entries[@]}" -eq 0 ]; then
    echo "[]"
  else
    (IFS=,; echo "[${entries[*]}]")
  fi
}

# Emits a JSON array of {id,status,tier,waivers}, one per
# bold-docs/features/*/spec.md
collect_active_features() {
  local docs_dir="$1"
  local entries=()
  if [ -d "$docs_dir/features" ]; then
    for dir in "$docs_dir/features"/*/; do
      [ -d "$dir" ] || continue
      local id spec status tier waivers
      id="$(basename "$dir")"
      spec="$dir/spec.md"
      [ -f "$spec" ] || continue
      status="$(sed -n 's/^\*\*Status\*\*: //p' "$spec" | head -1)"
      tier="$(sed -n 's/^\*\*Tier\*\*: //p' "$spec" | head -1)"
      waivers="$(collect_waivers_for_spec "$spec")"
      entries+=("{\"id\":\"$(json_escape "$id")\",\"status\":\"$(json_escape "${status:-unknown}")\",\"tier\":\"$(json_escape "${tier:-unknown}")\",\"waivers\":$waivers}")
    done
  fi
  if [ "${#entries[@]}" -eq 0 ]; then
    echo "[]"
  else
    (IFS=,; echo "[${entries[*]}]")
  fi
}

# Best-effort `git fetch` -- silently skipped without a remote or network
# (offline, sandboxed CI, a repo with no `origin`). Call once per collector
# invocation before any function below that reasons about `origin/*` refs;
# those functions assume the fetch already happened rather than each
# fetching redundantly.
bold_sync_git_remote() {
  local repo_root="$1"
  git -C "$repo_root" fetch --quiet origin >/dev/null 2>&1 || true
}

# Emits, one per line, every feature id Bold knows about, gathered from
# three sources so id allocation sees features in flight on other branches,
# not just the current working tree: (1) bold-docs/features/ in the current
# checkout, (2) every local and remote branch name shaped like a feature id
# (NNN+-slug), and (3) bold-docs/features/ as it exists on the remote base
# branch, in case main has moved since this branch was cut. Call
# bold_sync_git_remote first -- without a fresh fetch, "all active
# branches" only means "as of whenever anyone last happened to fetch,"
# which is exactly the gap that let two features both land on id 0003
# (bold-docs/patches.md, 2026-07-18).
collect_known_feature_ids() {
  local repo_root="$1" docs_dir="$2" base_branch="${3:-main}"

  local ids=()
  local dir ref short path remote_base

  if [ -d "$docs_dir/features" ]; then
    for dir in "$docs_dir/features"/*/; do
      [ -d "$dir" ] || continue
      ids+=("$(basename "$dir")")
    done
  fi

  while IFS= read -r ref; do
    [ -n "$ref" ] || continue
    short="${ref##*/}"
    if [[ "$short" =~ ^[0-9]{3,}-[a-z0-9-]+$ ]]; then
      ids+=("$short")
    fi
  done < <(git -C "$repo_root" for-each-ref --format='%(refname:short)' refs/heads refs/remotes 2>/dev/null)

  remote_base="origin/$base_branch"
  if git -C "$repo_root" rev-parse --verify "$remote_base" >/dev/null 2>&1; then
    while IFS= read -r path; do
      [ -n "$path" ] || continue
      ids+=("$path")
    done < <(git -C "$repo_root" ls-tree -d --name-only "$remote_base:bold-docs/features" 2>/dev/null)
  fi

  if [ "${#ids[@]}" -gt 0 ]; then
    printf '%s\n' "${ids[@]}" | sort -u
  fi
}

# Next unused feature number, read from a newline-separated id list on
# stdin (collect_known_feature_ids' output), zero-padded to the widest
# numeric prefix already in use (4 digits minimum). Never derive this by
# counting bold-docs/features/ entries in the current checkout alone -- see
# collect_known_feature_ids.
next_feature_number() {
  local width=4 max=0 id n len
  while IFS= read -r id; do
    [ -n "$id" ] || continue
    if [[ "$id" =~ ^([0-9]+)- ]]; then
      n=$((10#${BASH_REMATCH[1]}))
      len=${#BASH_REMATCH[1]}
      [ "$n" -gt "$max" ] && max=$n
      [ "$len" -gt "$width" ] && width=$len
    fi
  done
  printf "%0${width}d" "$((max + 1))"
}

# Computes one consolidated snapshot of git state into GIT_STATUS_* global
# variables, for a caller to read directly rather than round-tripping
# through JSON: current and base branch, commits ahead/behind (measured
# against `origin/<base>` when a remote exists -- so staleness is caught
# even if the local base branch itself was never fetched-and-merged,
# falling back to the local base ref when there's no remote), an
# uncommitted-changes flag, and a pre-computed proceed/message verdict so
# every command doesn't have to restate "if commits_behind > 0, stop" in
# its own prose. Deliberately doesn't skip the comparison when the current
# branch *is* the base branch -- that's exactly the case that matters
# before cutting a new feature branch (local main can be behind origin/main
# with nothing else to signal it). Call bold_sync_git_remote first. See
# collect_git_status for the JSON-emitting wrapper.
collect_git_status_vars() {
  local repo_root="$1" base_branch="${2:-main}"
  local remote_base base_exists

  GIT_STATUS_CURRENT_BRANCH="$(git -C "$repo_root" rev-parse --abbrev-ref HEAD 2>/dev/null || true)"
  [ -n "$GIT_STATUS_CURRENT_BRANCH" ] || GIT_STATUS_CURRENT_BRANCH="unknown"
  GIT_STATUS_BASE_BRANCH="$base_branch"

  remote_base="origin/$base_branch"
  GIT_STATUS_COMPARED_AGAINST="$base_branch"
  if git -C "$repo_root" rev-parse --verify "$remote_base" >/dev/null 2>&1; then
    GIT_STATUS_COMPARED_AGAINST="$remote_base"
  fi

  base_exists=false
  GIT_STATUS_COMMITS_AHEAD=0
  GIT_STATUS_COMMITS_BEHIND=0
  if git -C "$repo_root" rev-parse --verify "$GIT_STATUS_COMPARED_AGAINST" >/dev/null 2>&1; then
    base_exists=true
    GIT_STATUS_COMMITS_AHEAD="$(git -C "$repo_root" rev-list --count "$GIT_STATUS_COMPARED_AGAINST..HEAD" 2>/dev/null || echo 0)"
    GIT_STATUS_COMMITS_BEHIND="$(git -C "$repo_root" rev-list --count "HEAD..$GIT_STATUS_COMPARED_AGAINST" 2>/dev/null || echo 0)"
  fi

  GIT_STATUS_HAS_UNCOMMITTED=false
  [ -n "$(git -C "$repo_root" status --porcelain 2>/dev/null)" ] && GIT_STATUS_HAS_UNCOMMITTED=true

  GIT_STATUS_PROCEED=true
  if [ "$base_exists" != true ]; then
    GIT_STATUS_MESSAGE="Couldn't verify $GIT_STATUS_COMPARED_AGAINST (no such ref) -- proceeding without a staleness check."
  elif [ "$GIT_STATUS_COMMITS_BEHIND" -gt 0 ]; then
    GIT_STATUS_PROCEED=false
    if [ "$GIT_STATUS_CURRENT_BRANCH" = "$base_branch" ]; then
      GIT_STATUS_MESSAGE="Local $base_branch is $GIT_STATUS_COMMITS_BEHIND commit(s) behind $GIT_STATUS_COMPARED_AGAINST -- pull before branching (git pull origin $base_branch)."
    else
      GIT_STATUS_MESSAGE="$GIT_STATUS_COMMITS_BEHIND commit(s) behind $GIT_STATUS_COMPARED_AGAINST -- sync before continuing (git merge $GIT_STATUS_COMPARED_AGAINST or git rebase $GIT_STATUS_COMPARED_AGAINST)."
    fi
  else
    GIT_STATUS_MESSAGE="Up to date with $GIT_STATUS_COMPARED_AGAINST."
  fi
}

# JSON string built from the GIT_STATUS_* variables collect_git_status_vars
# just set -- call that first.
collect_git_status_json_from_vars() {
  printf '{"current_branch":"%s","base_branch":"%s","compared_against":"%s","commits_ahead":%s,"commits_behind":%s,"has_uncommitted_changes":%s,"proceed":%s,"message":"%s"}' \
    "$(json_escape "$GIT_STATUS_CURRENT_BRANCH")" "$(json_escape "$GIT_STATUS_BASE_BRANCH")" "$(json_escape "$GIT_STATUS_COMPARED_AGAINST")" \
    "$GIT_STATUS_COMMITS_AHEAD" "$GIT_STATUS_COMMITS_BEHIND" "$GIT_STATUS_HAS_UNCOMMITTED" "$GIT_STATUS_PROCEED" "$(json_escape "$GIT_STATUS_MESSAGE")"
}

# One-shot convenience: collect_git_status_vars then emit its JSON. Use
# this when a caller only wants the blob embedded in its own output (e.g.
# collect-triage-context); a caller that also needs individual fields for
# its own logic (e.g. collect-ship-context's branch-sync gate) should call
# collect_git_status_vars directly and read GIT_STATUS_* itself.
collect_git_status() {
  collect_git_status_vars "$@"
  collect_git_status_json_from_vars
}

# Echoes "open" if a gates/{analyze,critic}.md report has an unresolved
# finding bullet under its "## Findings" heading, nothing otherwise. A
# bullet counts as resolved only if it says so inline ("resolved"/"waiver")
# -- see WAIVERS.md. Used by the pre-build and pre-publish hard gates below.
_gate_report_open_findings() {
  local file="$1"
  awk '
    /^## Findings/ { infindings=1; next }
    /^## / { infindings=0 }
    infindings && /^- / {
      line=tolower($0)
      if (line !~ /resolved/ && line !~ /waiv/) { print "open"; exit }
    }
  ' "$file" 2>/dev/null
}

# Echoes "open" if a gates/checklist.md has any unchecked `- [ ]` item.
_checklist_has_unchecked() {
  grep -qE '^[[:space:]]*-[[:space:]]*\[ \]' "$1" 2>/dev/null && echo "open"
}

# Pre-build / pre-publish hard gate, part 1: for a Feature-tier feature,
# echoes one problem per line if gates/{analyze,critic,checklist}.md are
# missing or still have open items, nothing when clear. Callers hard-fail
# on any output -- this never guesses "probably fine".
check_feature_gates_clear() {
  local docs_dir="$1" feature_id="$2"
  local gates_dir="$docs_dir/features/$feature_id/gates"
  local analyze="$gates_dir/analyze.md" critic="$gates_dir/critic.md" checklist="$gates_dir/checklist.md"

  [ -f "$analyze" ] || echo "gates/analyze.md is missing"
  [ -f "$critic" ] || echo "gates/critic.md is missing"
  [ -f "$checklist" ] || echo "gates/checklist.md is missing"
  [ -f "$analyze" ] && [ "$(_gate_report_open_findings "$analyze")" = "open" ] && echo "gates/analyze.md has an unresolved finding"
  [ -f "$critic" ] && [ "$(_gate_report_open_findings "$critic")" = "open" ] && echo "gates/critic.md has an unresolved finding"
  [ -f "$checklist" ] && [ "$(_checklist_has_unchecked "$checklist")" = "open" ] && echo "gates/checklist.md has an unchecked item"
  return 0
}

# Pre-build / pre-publish hard gate, part 2 ("ready-fire-aim" detector):
# for a Feature-tier feature on its own branch, echoes one problem per line
# if a gates/*.md report was committed at or after the first commit that
# touched a non-Bold-owned path on this branch -- i.e. the gate ran after
# implementation started, so it reviewed a spec, not the build it claims to
# gate. Echoes nothing when clear, "no-baseline" (never a hard-fail signal
# on its own) when it can't be determined -- e.g. no merge-base with main,
# or HEAD is main itself.
check_feature_gates_predate_impl() {
  local docs_dir="$1" feature_id="$2"
  local base="main"
  git rev-parse --verify "$base" >/dev/null 2>&1 || { echo "no-baseline"; return 0; }
  local merge_base
  merge_base="$(git merge-base "$base" HEAD 2>/dev/null)" || { echo "no-baseline"; return 0; }
  [ "$merge_base" = "$(git rev-parse HEAD 2>/dev/null)" ] && { echo "no-baseline"; return 0; }

  local bold_owned='^(bold-docs/|\.bold/|\.bold-user/|\.claude/|\.agents/|\.github/prompts/|AGENTS\.md$)'
  local first_impl_commit=""
  while IFS= read -r c; do
    [ -n "$c" ] || continue
    if git show --name-only --format= "$c" 2>/dev/null | grep -vE "$bold_owned" | grep -q .; then
      first_impl_commit="$c"
      break
    fi
  done < <(git log --reverse --format=%H "$merge_base..HEAD" 2>/dev/null; printf '\n')

  [ -n "$first_impl_commit" ] || return 0

  local gates_dir="$docs_dir/features/$feature_id/gates"
  local name f add_commit
  for name in analyze critic checklist; do
    f="$gates_dir/$name.md"
    [ -f "$f" ] || continue
    add_commit="$(git log --reverse --format=%H --diff-filter=A "$merge_base..HEAD" -- "$f" 2>/dev/null | head -1)"
    if [ -z "$add_commit" ]; then
      echo "gates/$name.md wasn't added in a commit on this branch -- can't confirm it ran before implementation started"
      continue
    fi
    if [ "$add_commit" = "$first_impl_commit" ] || ! git merge-base --is-ancestor "$add_commit" "$first_impl_commit" 2>/dev/null; then
      echo "gates/$name.md was committed after implementation began ($add_commit is not before $first_impl_commit) -- looks like a retroactive backfill, not a real pre-flight gate"
    fi
  done
  return 0
}

# Emits a JSON array of {doc,reference}, one per backtick-quoted, path-shaped
# reference in a bold-docs/system/ doc that doesn't resolve to a real file.
# Scoped to system/ only (§13 ambient staleness detection) -- feature specs
# are expected to reference code that doesn't exist yet.
collect_stale_references() {
  local repo_root="$1"
  local docs_dir="$2"
  local system_dir="$docs_dir/system"
  local entries=()
  [ -d "$system_dir" ] || { echo "[]"; return; }
  local doc rel_doc ref
  while IFS= read -r doc; do
    [ -n "$doc" ] || continue
    rel_doc="${doc#"$repo_root"/}"
    while IFS= read -r ref; do
      [ -n "$ref" ] || continue
      case "$ref" in
        http://*|https://*|*\**) continue ;;
      esac
      if [ ! -e "$repo_root/$ref" ]; then
        entries+=("{\"doc\":\"$(json_escape "$rel_doc")\",\"reference\":\"$(json_escape "$ref")\"}")
      fi
    done < <(grep -oE '`[A-Za-z0-9_.-]+(/[A-Za-z0-9_.-]+)+`' "$doc" 2>/dev/null | tr -d '`' | sort -u; printf '\n')
  done < <(find "$system_dir" -type f ! -name '.gitkeep' 2>/dev/null | sort; printf '\n')
  if [ "${#entries[@]}" -eq 0 ]; then
    echo "[]"
  else
    (IFS=,; echo "[${entries[*]}]")
  fi
}

# Emits a JSON array of {n,status}, one per numbered principle in backbone.md
collect_backbone_principles() {
  local docs_dir="$1"
  local backbone_file="$docs_dir/backbone.md"
  local entries=()
  local n=0
  if [ -f "$backbone_file" ]; then
    while IFS= read -r status; do
      [ -n "$status" ] || continue
      n=$((n+1))
      entries+=("{\"n\":$n,\"status\":\"$(json_escape "$status")\"}")
    done < <(sed -n 's/^[[:space:]]*\*\*Status\*\*: //p' "$backbone_file"; printf '\n')
  fi
  if [ "${#entries[@]}" -eq 0 ]; then
    echo "[]"
  else
    (IFS=,; echo "[${entries[*]}]")
  fi
}
