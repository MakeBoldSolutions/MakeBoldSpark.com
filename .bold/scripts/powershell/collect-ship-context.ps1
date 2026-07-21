# Collector for bold.ship (default and review). Emits deterministic facts —
# branch position relative to the base branch, changed-file inventory,
# active feature tiers, and backbone status — so drafting or reviewing a PR
# starts from current, structured ground truth.

param([switch]$Publish)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'lib/Common.ps1')

$repoRoot = git rev-parse --show-toplevel 2>$null
if (-not $repoRoot) { $repoRoot = (Get-Location).Path }
$docsDir = Join-Path $repoRoot 'bold-docs'
Set-Location $repoRoot
Add-BoldRunLog -RepoRoot $repoRoot -Command 'ship' -Collector 'collect-ship-context'

Sync-BoldGitRemote -RepoRoot $repoRoot
$gitStatus = Get-GitStatus -RepoRoot $repoRoot
$baseBranch = $gitStatus.base_branch
$currentBranch = $gitStatus.current_branch
$commitsAhead = $gitStatus.commits_ahead
$commitsBehind = $gitStatus.commits_behind

$changedFiles = @()
if ($currentBranch -ne $baseBranch) {
  $changedFiles = @(git diff --name-only "$($gitStatus.compared_against)...HEAD" 2>$null | Where-Object { $_ })
}

$hasUncommittedChanges = [bool](git status --porcelain 2>$null)

$activeFeatures = Get-ActiveFeatures -DocsDir $docsDir
$backbonePrinciples = Get-BackbonePrinciples -DocsDir $docsDir

# Hard gate, unconditional (applies to bold.ship default AND bold.ship
# review -- both share this collector): a review or a drafted PR against a
# base that's already moved is reviewing/packaging the wrong diff.
# ship/review.md has long claimed this is "the one rule that isn't
# advisory," but nothing enforced it mechanically until now -- see
# bold-docs/patches.md, 2026-07-18.
if (-not $gitStatus.proceed) {
  Write-Error "GATE FAILED: $($gitStatus.message)" -ErrorAction Continue
  exit 1
}

# Hard gate, before publish: only bold.ship (default) passes -Publish --
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
if ($Publish -and $currentBranch) {
  $specPath = Join-Path $docsDir "features/$currentBranch/spec.md"
  if (Test-Path $specPath) {
    $content = Get-Content $specPath
    $statusMatch = $content | Select-String '^\*\*Status\*\*: (.+)$' | Select-Object -First 1
    $tierMatch = $content | Select-String '^\*\*Tier\*\*: (.+)$' | Select-Object -First 1
    $featureStatus = if ($statusMatch) { $statusMatch.Matches.Groups[1].Value } else { 'unset' }
    $featureTier = if ($tierMatch) { $tierMatch.Matches.Groups[1].Value } else { '' }
    $shipProblems = @()
    if ($featureStatus -ne 'Complete') { $shipProblems += "spec.md status is '$featureStatus', not Complete" }
    if ($content | Select-String '^\s*-\s*\[ \]') { $shipProblems += 'spec.md has unchecked tasks' }
    if ($featureTier -eq 'Feature') {
      $clearProblems = Get-FeatureGateProblems -DocsDir $docsDir -FeatureId $currentBranch
      $shipProblems += $clearProblems
      $orderProblems = Get-FeatureGateOrderProblems -DocsDir $docsDir -FeatureId $currentBranch
      if ($orderProblems.Count -gt 0 -and $orderProblems[0] -ne 'no-baseline') {
        $shipProblems += $orderProblems
      }
    }
    # Excludes run-log.jsonl: Add-BoldRunLog above just wrote to it, so it's
    # always "dirty" the moment this collector runs -- that's the
    # collector's own expected bookkeeping, not leftover user work worth
    # blocking on.
    $dirtyExcludingRunLog = git status --porcelain -- . ':!bold-docs/run-log.jsonl' 2>$null
    if ($dirtyExcludingRunLog) { $shipProblems += 'working tree has uncommitted changes' }
    if ($shipProblems.Count -gt 0) {
      Write-Error "GATE FAILED: bold.ship cannot publish '$currentBranch' until:" -ErrorAction Continue
      foreach ($p in $shipProblems) { Write-Error "  - $p" -ErrorAction Continue }
      Write-Error "Do not hand-author missing gate files or edit run history to satisfy this check -- go back to bold.plan / bold.build and do the work for real." -ErrorAction Continue
      exit 1
    }
  }
}

[ordered]@{
  base_branch             = $baseBranch
  current_branch          = $currentBranch
  commits_ahead           = $commitsAhead
  commits_behind          = $commitsBehind
  changed_files           = $changedFiles
  has_uncommitted_changes = $hasUncommittedChanges
  active_features         = $activeFeatures
  backbone_principles     = $backbonePrinciples
  git_status              = $gitStatus
} | ConvertTo-Json -Depth 10 -Compress
