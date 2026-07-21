# Collector for bold.build (default and status). Emits deterministic facts —
# per-feature ratified tier, backbone principle enforcement status, detected
# test-runner config, and working-tree cleanliness — so gates are decided
# from ground truth instead of re-derived by reading the tree by hand.

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'lib/Common.ps1')

$repoRoot = git rev-parse --show-toplevel 2>$null
if (-not $repoRoot) { $repoRoot = (Get-Location).Path }
$docsDir = Join-Path $repoRoot 'bold-docs'
Set-Location $repoRoot
Add-BoldRunLog -RepoRoot $repoRoot -Command 'build' -Collector 'collect-gate-status'

Sync-BoldGitRemote -RepoRoot $repoRoot
$gitStatus = Get-GitStatus -RepoRoot $repoRoot

# Hard gate, before build: a build spends a full cycle of work on top of
# whatever's currently checked out -- if that base has already moved on
# origin, everything built this cycle is built on ground that's about to
# need reconciling. Nothing previously fetched or reported this before a
# build ran; the base moved mid-flight, silently, and the divergence
# surfaced externally instead (bold-docs/patches.md, 2026-07-18).
if (-not $gitStatus.proceed) {
  Write-Error "GATE FAILED: $($gitStatus.message)" -ErrorAction Continue
  exit 1
}

$activeFeatures = Get-ActiveFeatures -DocsDir $docsDir
$backbonePrinciples = Get-BackbonePrinciples -DocsDir $docsDir
$staleReferences = Get-StaleReferences -RepoRoot $repoRoot -DocsDir $docsDir

$knownTestConfigs = @('jest.config.js', 'jest.config.ts', 'pytest.ini', 'tox.ini', '.mocharc.json',
  '.mocharc.yml', 'karma.conf.js', 'phpunit.xml')
$testConfigPresent = @($knownTestConfigs | Where-Object { Test-Path $_ })
$testConfigPresent += @(Get-ChildItem -Path . -Filter '*.Tests.csproj' -Depth 1 -File -ErrorAction SilentlyContinue |
  ForEach-Object { $_.Name })

$hasUncommittedChanges = [bool](git status --porcelain 2>$null)

# Hard gate, before build: a Feature-tier feature (matched by the current
# branch name -- bold.plan default checks out a branch named after the
# feature id) can't start bold.build until its pre-flight gates
# (analyze/critic/checklist) exist and are clean. This exits nonzero -- a
# hard stop the agent can't reason its way past -- rather than leaving the
# decision to whoever reads the JSON below.
$currentBranch = $gitStatus.current_branch
if ($currentBranch -and $currentBranch -ne 'unknown') {
  $specPath = Join-Path $docsDir "features/$currentBranch/spec.md"
  if (Test-Path $specPath) {
    $tierMatch = Get-Content $specPath | Select-String '^\*\*Tier\*\*: (.+)$' | Select-Object -First 1
    $featureTier = if ($tierMatch) { $tierMatch.Matches.Groups[1].Value } else { '' }
    if ($featureTier -eq 'Feature') {
      $gateProblems = Get-FeatureGateProblems -DocsDir $docsDir -FeatureId $currentBranch
      if ($gateProblems.Count -gt 0) {
        Write-Error "GATE FAILED: bold.build cannot start on '$currentBranch' (Feature tier) until its pre-flight gates are clean:" -ErrorAction Continue
        foreach ($p in $gateProblems) { Write-Error "  - $p" -ErrorAction Continue }
        Write-Error "Run bold.plan analyze / bold.plan critic / bold.plan checklist first. Do not hand-write these files to satisfy this check -- that defeats the point of a pre-flight gate." -ErrorAction Continue
        exit 1
      }
    }
  }
}

[ordered]@{
  active_features         = $activeFeatures
  backbone_principles     = $backbonePrinciples
  stale_references        = $staleReferences
  test_config_present     = $testConfigPresent
  has_uncommitted_changes = $hasUncommittedChanges
  git_status              = $gitStatus
} | ConvertTo-Json -Depth 10 -Compress
