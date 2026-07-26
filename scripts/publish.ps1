param(
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$OutputDir = "publish"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src\KnowHowToAI.Cli\KnowHowToAI.Cli.csproj"
$output = Join-Path $repoRoot $OutputDir

# Tests muessen gruen sein, bevor veroeffentlicht wird
Write-Host "Build + Tests..."
dotnet test $repoRoot -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0)
{
    throw "Tests rot -- Publish abgebrochen."
}

# Output-Verzeichnis vorher leeren, damit keine stale Files zurueckbleiben
if (Test-Path $output)
{
    Write-Host "Leere $output..."
    Remove-Item -Path $output -Recurse -Force
}

dotnet publish $project `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $output `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true

Write-Host "Veroeffentlicht nach: $output\KnowHowToAI.Cli.exe"
