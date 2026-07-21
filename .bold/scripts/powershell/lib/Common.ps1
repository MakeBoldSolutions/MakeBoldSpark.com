# Shared helpers for bold's PowerShell collector scripts.

# Collector stdout is JSON consumed by prompts. Force UTF-8 (no BOM) so
# non-ASCII content (em-dashes, section marks in spec metadata) survives
# redirection on Windows, whose OEM codepage otherwise mangles it into
# bare control bytes — observed for real during feature 0007 verification
# (U+00A7 "§" emitted as 0x15 via CP437, producing invalid JSON).
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)

# Filesystem-safe slug identifying the current contributor for
# .bold-user/{slug}/ (bold-tool-plan.md §17 #4 — committed, per-user
# tier). Falls back from git user.name to the email's local part, then
# to "shared" if neither is configured. -Root (optional) scopes the git
# config lookup to a specific repo instead of the caller's cwd -- required,
# not cosmetic: a script invoked with -Root pointing elsewhere must not
# silently read the invoking shell's own git identity instead.
function Get-BoldUserSlug {
  param([string]$Root)
  if ($Root) {
    $name = git -C $Root config user.name 2>$null
  } else {
    $name = git config user.name 2>$null
  }
  if (-not $name) {
    if ($Root) {
      $email = git -C $Root config user.email 2>$null
    } else {
      $email = git config user.email 2>$null
    }
    if ($email) { $name = $email.Split('@')[0] }
  }
  if (-not $name) { $name = 'shared' }
  $slug = ($name.ToLower() -replace '[^a-z0-9]+', '-').Trim('-')
  if (-not $slug) { $slug = 'shared' }
  return $slug
}

# Appends one JSONL invocation line to bold-docs/run-log.jsonl — Bold's
# minimal run observability (bold-tool-plan.md §16, Industry-Alignment
# item 2). Invocation facts only: a collector runs before the work and
# cannot know outcomes. No-op when the repo has no bold-docs/ workspace,
# or when BOLD_NO_RUN_LOG=1 (fixture tests, parity checks). Uses
# File.AppendAllText (not Add-Content) so the line ending is \n, matching
# the bash helper byte for byte.
function Add-BoldRunLog {
  param([string]$RepoRoot, [string]$Command, [string]$Collector)
  if ($env:BOLD_NO_RUN_LOG -eq '1') { return }
  $docsDir = Join-Path $RepoRoot 'bold-docs'
  if (-not (Test-Path $docsDir)) { return }
  $line = [ordered]@{
    ts        = [DateTime]::UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'")
    command   = $Command
    collector = $Collector
    user      = Get-BoldUserSlug -Root $RepoRoot
  } | ConvertTo-Json -Compress
  [System.IO.File]::AppendAllText((Join-Path $docsDir 'run-log.jsonl'), "$line`n")
}

# Emits an array of {principle,reason,ratified_by,date}, one per
# `- Waiver: ...` line in the given spec file. See
# source/commands/WAIVERS.md for the line format.
function Get-WaiversForSpec {
  param([string]$SpecPath)
  $waivers = @()
  if (Test-Path $SpecPath) {
    Get-Content $SpecPath | Select-String '^- Waiver: (.+)$' | ForEach-Object {
      $line = $_.Matches.Groups[1].Value
      $principleMatch = [regex]::Match($line, 'principle=(\d+)')
      $reasonMatch = [regex]::Match($line, 'reason="([^"]*)"')
      $ratifiedByMatch = [regex]::Match($line, 'ratified_by="([^"]*)"')
      $dateMatch = [regex]::Match($line, 'date=([\d-]+)')
      $waivers += [ordered]@{
        principle   = if ($principleMatch.Success) { [int]$principleMatch.Groups[1].Value } else { $null }
        reason      = $reasonMatch.Groups[1].Value
        ratified_by = $ratifiedByMatch.Groups[1].Value
        date        = $dateMatch.Groups[1].Value
      }
    }
  }
  return ,$waivers
}

# Best-effort `git fetch` -- silently skipped without a remote or network
# (offline, sandboxed CI, a repo with no `origin`). Call once per collector
# invocation before any function below that reasons about `origin/*` refs;
# those functions assume the fetch already happened rather than each
# fetching redundantly.
function Sync-BoldGitRemote {
  param([string]$RepoRoot)
  git -C $RepoRoot fetch --quiet origin 2>$null | Out-Null
}

# Returns every feature id Bold knows about, gathered from three sources so
# id allocation sees features in flight on other branches, not just the
# current working tree: (1) bold-docs/features/ in the current checkout
# (same scan as Get-ActiveFeatures), (2) every local and remote branch name
# shaped like a feature id (NNN+-slug), and (3) bold-docs/features/ as it
# exists on the remote base branch, in case main has moved since this
# branch was cut. Call Sync-BoldGitRemote first -- without a fresh fetch,
# "all active branches" only means "as of whenever anyone last happened to
# fetch," which is exactly the gap that let two features both land on id
# 0003 (bold-docs/patches.md, 2026-07-18).
function Get-KnownFeatureIds {
  param([string]$RepoRoot, [string]$DocsDir, [string]$BaseBranch = 'main')

  $ids = [System.Collections.Generic.HashSet[string]]::new()

  $featuresDir = Join-Path $DocsDir 'features'
  if (Test-Path $featuresDir) {
    Get-ChildItem -Path $featuresDir -Directory | ForEach-Object { [void]$ids.Add($_.Name) }
  }

  foreach ($ref in @(git -C $RepoRoot for-each-ref --format='%(refname:short)' refs/heads refs/remotes 2>$null)) {
    $short = $ref -replace '^.*/', ''
    if ($short -match '^\d{3,}-[a-z0-9-]+$') { [void]$ids.Add($short) }
  }

  $remoteBase = "origin/$BaseBranch"
  $remoteBaseExists = (git -C $RepoRoot rev-parse --verify $remoteBase 2>$null)
  if ($remoteBaseExists) {
    foreach ($path in @(git -C $RepoRoot ls-tree -d --name-only "${remoteBase}:bold-docs/features" 2>$null)) {
      if ($path) { [void]$ids.Add($path) }
    }
  }

  return ,@($ids | Sort-Object)
}

# Next unused feature number across every id Get-KnownFeatureIds found,
# zero-padded to the widest numeric prefix already in use (4 digits
# minimum). Never derive this by counting bold-docs/features/ entries in
# the current checkout alone -- see Get-KnownFeatureIds.
function Get-NextFeatureNumber {
  param([string[]]$KnownFeatureIds)
  $width = 4
  $max = 0
  foreach ($id in $KnownFeatureIds) {
    if ($id -match '^(\d+)-') {
      $n = [int]$Matches[1]
      if ($n -gt $max) { $max = $n }
      if ($Matches[1].Length -gt $width) { $width = $Matches[1].Length }
    }
  }
  return ($max + 1).ToString('D' + $width)
}

# One consolidated snapshot of git state for a command prompt to reason
# over without re-deriving branch/ahead-behind mechanics itself: current
# and base branch, commits ahead/behind (measured against `origin/<base>`
# when a remote exists -- so staleness is caught even if the local base
# branch itself was never fetched-and-merged, falling back to the local
# base ref when there's no remote), an uncommitted-changes flag, and a
# pre-computed proceed/message verdict so every command doesn't have to
# restate "if commits_behind > 0, stop" in its own prose. Deliberately
# doesn't skip the comparison when the current branch *is* the base branch
# -- that's exactly the case that matters before cutting a new feature
# branch (local main can be behind origin/main with nothing else to signal
# it). Call Sync-BoldGitRemote first.
function Get-GitStatus {
  param([string]$RepoRoot, [string]$BaseBranch = 'main')

  $currentBranch = git -C $RepoRoot rev-parse --abbrev-ref HEAD 2>$null
  if (-not $currentBranch) { $currentBranch = 'unknown' }

  $remoteBase = "origin/$BaseBranch"
  $compareBase = $BaseBranch
  $remoteBaseExists = (git -C $RepoRoot rev-parse --verify $remoteBase 2>$null)
  if ($remoteBaseExists) { $compareBase = $remoteBase }

  $baseExists = [bool](git -C $RepoRoot rev-parse --verify $compareBase 2>$null)
  $commitsAhead = 0
  $commitsBehind = 0
  if ($baseExists) {
    $commitsAhead = [int](git -C $RepoRoot rev-list --count "$compareBase..HEAD" 2>$null)
    $commitsBehind = [int](git -C $RepoRoot rev-list --count "HEAD..$compareBase" 2>$null)
  }

  $hasUncommittedChanges = [bool](git -C $RepoRoot status --porcelain 2>$null)

  $proceed = $true
  if (-not $baseExists) {
    $message = "Couldn't verify $compareBase (no such ref) -- proceeding without a staleness check."
  } elseif ($commitsBehind -gt 0) {
    $proceed = $false
    if ($currentBranch -eq $BaseBranch) {
      $message = "Local $BaseBranch is $commitsBehind commit(s) behind $compareBase -- pull before branching (git pull origin $BaseBranch)."
    } else {
      $message = "$commitsBehind commit(s) behind $compareBase -- sync before continuing (git merge $compareBase or git rebase $compareBase)."
    }
  } else {
    $message = "Up to date with $compareBase."
  }

  return [ordered]@{
    current_branch          = $currentBranch
    base_branch             = $BaseBranch
    compared_against        = $compareBase
    commits_ahead           = $commitsAhead
    commits_behind          = $commitsBehind
    has_uncommitted_changes = $hasUncommittedChanges
    proceed                 = $proceed
    message                 = $message
  }
}

function Get-ActiveFeatures {
  param([string]$DocsDir)
  $features = @()
  $featuresDir = Join-Path $DocsDir 'features'
  if (Test-Path $featuresDir) {
    Get-ChildItem -Path $featuresDir -Directory | ForEach-Object {
      $specPath = Join-Path $_.FullName 'spec.md'
      if (Test-Path $specPath) {
        $content = Get-Content $specPath
        $status = ($content | Select-String '^\*\*Status\*\*: (.+)$' | Select-Object -First 1).Matches.Groups[1].Value
        $tier   = ($content | Select-String '^\*\*Tier\*\*: (.+)$' | Select-Object -First 1).Matches.Groups[1].Value
        $features += [ordered]@{
          id      = $_.Name
          status  = if ($status) { $status } else { 'unknown' }
          tier    = if ($tier) { $tier } else { 'unknown' }
          waivers = Get-WaiversForSpec -SpecPath $specPath
        }
      }
    }
  }
  return ,$features
}

# Returns $true if a gates/{analyze,critic}.md report has an unresolved
# finding bullet under its "## Findings" heading. A bullet counts as
# resolved only if it says so inline ("resolved"/"waiver") -- see
# WAIVERS.md. Used by the pre-build and pre-publish hard gates below.
function Test-GateReportHasOpenFindings {
  param([string]$GateFile)
  if (-not (Test-Path $GateFile)) { return $false }
  $inFindings = $false
  foreach ($line in Get-Content $GateFile) {
    if ($line -match '^## Findings') { $inFindings = $true; continue }
    if ($line -match '^## ') { $inFindings = $false; continue }
    if ($inFindings -and $line -match '^- ') {
      $lower = $line.ToLower()
      if ($lower -notmatch 'resolved' -and $lower -notmatch 'waiv') { return $true }
    }
  }
  return $false
}

# Returns $true if a gates/checklist.md has any unchecked `- [ ]` item.
function Test-ChecklistHasUnchecked {
  param([string]$ChecklistFile)
  if (-not (Test-Path $ChecklistFile)) { return $false }
  return [bool](Get-Content $ChecklistFile | Select-String '^\s*-\s*\[ \]')
}

# Pre-build / pre-publish hard gate, part 1: for a Feature-tier feature,
# returns one problem string per missing or still-open gate file, empty
# when clear. Callers hard-fail on any output -- this never guesses
# "probably fine".
function Get-FeatureGateProblems {
  param([string]$DocsDir, [string]$FeatureId)
  $problems = @()
  $gatesDir = Join-Path $DocsDir "features/$FeatureId/gates"
  $analyze = Join-Path $gatesDir 'analyze.md'
  $critic = Join-Path $gatesDir 'critic.md'
  $checklist = Join-Path $gatesDir 'checklist.md'

  if (-not (Test-Path $analyze)) { $problems += 'gates/analyze.md is missing' }
  elseif (Test-GateReportHasOpenFindings -GateFile $analyze) { $problems += 'gates/analyze.md has an unresolved finding' }
  if (-not (Test-Path $critic)) { $problems += 'gates/critic.md is missing' }
  elseif (Test-GateReportHasOpenFindings -GateFile $critic) { $problems += 'gates/critic.md has an unresolved finding' }
  if (-not (Test-Path $checklist)) { $problems += 'gates/checklist.md is missing' }
  elseif (Test-ChecklistHasUnchecked -ChecklistFile $checklist) { $problems += 'gates/checklist.md has an unchecked item' }
  return ,$problems
}

# Pre-build / pre-publish hard gate, part 2 ("ready-fire-aim" detector):
# for a Feature-tier feature on its own branch, returns one problem string
# per gates/*.md report that was committed at or after the first commit
# that touched a non-Bold-owned path on this branch -- i.e. the gate ran
# after implementation started, so it reviewed a spec, not the build it
# claims to gate. Empty when clear; "no-baseline" (never a hard-fail
# signal on its own) when it can't be determined -- e.g. no merge-base
# with main, or HEAD is main itself.
function Get-FeatureGateOrderProblems {
  param([string]$DocsDir, [string]$FeatureId)
  $base = 'main'
  $baseExists = (git rev-parse --verify $base 2>$null)
  if (-not $baseExists) { return ,@('no-baseline') }
  $mergeBase = (git merge-base $base HEAD 2>$null)
  if (-not $mergeBase) { return ,@('no-baseline') }
  $head = (git rev-parse HEAD 2>$null)
  if ($mergeBase -eq $head) { return ,@('no-baseline') }

  $boldOwned = '^(bold-docs/|\.bold/|\.bold-user/|\.claude/|\.agents/|\.github/prompts/|AGENTS\.md$)'
  $commits = @(git log --reverse --format=%H "$mergeBase..HEAD" 2>$null)
  $firstImplCommit = $null
  foreach ($c in $commits) {
    $touched = @(git show --name-only --format= $c 2>$null | Where-Object { $_ -and $_ -notmatch $boldOwned })
    if ($touched.Count -gt 0) { $firstImplCommit = $c; break }
  }
  if (-not $firstImplCommit) { return ,@() }

  $problems = @()
  $gatesDir = Join-Path $DocsDir "features/$FeatureId/gates"
  foreach ($name in @('analyze', 'critic', 'checklist')) {
    $f = Join-Path $gatesDir "$name.md"
    if (-not (Test-Path $f)) { continue }
    $addCommit = (git log --reverse --format=%H --diff-filter=A "$mergeBase..HEAD" -- $f 2>$null | Select-Object -First 1)
    if (-not $addCommit) {
      $problems += "gates/$name.md wasn't added in a commit on this branch -- can't confirm it ran before implementation started"
      continue
    }
    $isAncestor = $false
    if ($addCommit -ne $firstImplCommit) {
      git merge-base --is-ancestor $addCommit $firstImplCommit 2>$null
      $isAncestor = ($LASTEXITCODE -eq 0)
    }
    if ($addCommit -eq $firstImplCommit -or -not $isAncestor) {
      $problems += "gates/$name.md was committed after implementation began ($addCommit is not before $firstImplCommit) -- looks like a retroactive backfill, not a real pre-flight gate"
    }
  }
  return ,$problems
}

function Get-SystemDocs {
  param([string]$RepoRoot, [string]$DocsDir)
  $docs = @()
  $systemDir = Join-Path $DocsDir 'system'
  if (Test-Path $systemDir) {
    $docs = @(Get-ChildItem -Path $systemDir -File -Recurse |
      Where-Object { $_.Name -ne '.gitkeep' } |
      ForEach-Object { $_.FullName.Substring($RepoRoot.Length + 1) -replace '\\', '/' } |
      Sort-Object)
  }
  return ,$docs
}

# Emits an array of {doc,reference}, one per backtick-quoted, path-shaped
# reference in a bold-docs/system/ doc that doesn't resolve to a real file.
# Scoped to system/ only (§13 ambient staleness detection) -- feature specs
# are expected to reference code that doesn't exist yet.
function Get-StaleReferences {
  param([string]$RepoRoot, [string]$DocsDir)
  $stale = @()
  $systemDir = Join-Path $DocsDir 'system'
  if (Test-Path $systemDir) {
    Get-ChildItem -Path $systemDir -File -Recurse | Where-Object { $_.Name -ne '.gitkeep' } | ForEach-Object {
      $relDoc = $_.FullName.Substring($RepoRoot.Length + 1) -replace '\\', '/'
      $refs = [regex]::Matches((Get-Content $_.FullName -Raw), '`([A-Za-z0-9_.-]+(?:/[A-Za-z0-9_.-]+)+)`') |
        ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
      foreach ($ref in $refs) {
        if ($ref -match '^https?://' -or $ref -match '\*') { continue }
        $refPath = Join-Path $RepoRoot $ref
        if (-not (Test-Path $refPath)) {
          $stale += [ordered]@{ doc = $relDoc; reference = $ref }
        }
      }
    }
  }
  return ,$stale
}

function Get-BackbonePrinciples {
  param([string]$DocsDir)
  $principles = @()
  $backboneFile = Join-Path $DocsDir 'backbone.md'
  if (Test-Path $backboneFile) {
    $n = 0
    Get-Content $backboneFile | Select-String '^\s*\*\*Status\*\*: (.+)$' | ForEach-Object {
      $n++
      $principles += [ordered]@{ n = $n; status = $_.Matches.Groups[1].Value }
    }
  }
  return ,$principles
}
