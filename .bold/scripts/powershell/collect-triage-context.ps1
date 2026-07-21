# Collector for bold.plan (default/triage). Emits deterministic JSON facts —
# system doc inventory, active feature tiers/status, backbone principle
# status, and the project genome — so the triage prompt reasons over
# structured ground truth instead of re-deriving it from prose.

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'lib/Common.ps1')

$repoRoot = git rev-parse --show-toplevel 2>$null
if (-not $repoRoot) { $repoRoot = (Get-Location).Path }
$docsDir = Join-Path $repoRoot 'bold-docs'
Add-BoldRunLog -RepoRoot $repoRoot -Command 'plan' -Collector 'collect-triage-context'

Sync-BoldGitRemote -RepoRoot $repoRoot

$systemDocs = Get-SystemDocs -RepoRoot $repoRoot -DocsDir $docsDir
$activeFeatures = Get-ActiveFeatures -DocsDir $docsDir
$backbonePrinciples = Get-BackbonePrinciples -DocsDir $docsDir
$staleReferences = Get-StaleReferences -RepoRoot $repoRoot -DocsDir $docsDir
$knownFeatureIds = Get-KnownFeatureIds -RepoRoot $repoRoot -DocsDir $docsDir
$nextFeatureNumber = Get-NextFeatureNumber -KnownFeatureIds $knownFeatureIds
$gitStatus = Get-GitStatus -RepoRoot $repoRoot

$genome = $null
$genomeFile = Join-Path $docsDir 'project.json'
if (Test-Path $genomeFile) {
  $genome = Get-Content $genomeFile -Raw | ConvertFrom-Json
}

[ordered]@{
  system_docs          = $systemDocs
  active_features      = $activeFeatures
  backbone_principles  = $backbonePrinciples
  stale_references     = $staleReferences
  known_feature_ids    = $knownFeatureIds
  next_feature_number  = $nextFeatureNumber
  git_status           = $gitStatus
  genome               = $genome
} | ConvertTo-Json -Depth 10 -Compress
