param(
    [Parameter(Mandatory = $false)]
    [string]$DatabasePath,

    [Parameter(Mandatory = $false)]
    [string]$BackupDirectory = ".migration-backups",

    [Parameter(Mandatory = $false)]
    [string]$Project = "src/MakeBoldSpark.Recipe/MakeBoldSpark.Recipe.csproj",

    [Parameter(Mandatory = $false)]
    [string]$StartupProject = "src/MakeBoldSpark.Api/MakeBoldSpark.Api.csproj",

    [switch]$DryRun,
    [switch]$SkipEfUpdate
)

$ErrorActionPreference = "Stop"

function Resolve-RepoPath([string]$Path) {
    if ([System.IO.Path]::IsPathRooted($Path)) { return $Path }
    return Join-Path (Get-Location) $Path
}

function Write-Step([string]$Message) {
    Write-Host "[recipe-migration] $Message"
}

Write-Step "Starting migration verification."

if ([string]::IsNullOrWhiteSpace($DatabasePath)) {
    if ($DryRun) {
        Write-Step "Dry run: DatabasePath not supplied. Checklist only."
        Write-Step "Required checks: backup copy, rehearsal migration, integrity query, forward-fix note."
        Write-Step "Forward-fix: create a new EF migration; do not edit an applied migration in production."
        exit 0
    }

    throw "DatabasePath is required unless -DryRun is used."
}

$source = Resolve-RepoPath $DatabasePath
$backupRoot = Resolve-RepoPath $BackupDirectory

if (-not (Test-Path -LiteralPath $source)) {
    throw "Database file does not exist: $source"
}

New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$backup = Join-Path $backupRoot "recipe-$timestamp.backup.db"
$rehearsal = Join-Path $backupRoot "recipe-$timestamp.rehearsal.db"

Copy-Item -LiteralPath $source -Destination $backup -Force
Copy-Item -LiteralPath $backup -Destination $rehearsal -Force
Write-Step "Backup created: $backup"
Write-Step "Rehearsal database created: $rehearsal"

if (-not $SkipEfUpdate) {
    $env:ConnectionStrings__RecipeConnection = "Data Source=$rehearsal"
    Write-Step "Running EF migration against rehearsal database."
    dotnet ef database update --project $Project --startup-project $StartupProject --context RecipeDbContext
}
else {
    Write-Step "Skipped EF update by request."
}

$rehearsalInfo = Get-Item -LiteralPath $rehearsal
if ($rehearsalInfo.Length -le 0) {
    throw "Rehearsal database is empty after verification."
}

Write-Step "Post-migration integrity check: rehearsal file exists and is non-empty."
Write-Step "Forward-fix runbook: if production migration fails after backup, preserve the failed database copy, restore the verified backup if service impact requires it, then ship a new forward-only EF migration that repairs the schema/data condition."
Write-Step "Migration verification complete."
