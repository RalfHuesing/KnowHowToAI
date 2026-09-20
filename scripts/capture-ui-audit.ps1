#requires -Version 7.0
[CmdletBinding()]
param(
    [string]$OutputRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot 'temp/ui-audit'
}

$outputRootFull = [System.IO.Path]::GetFullPath($OutputRoot)
if ([System.IO.Path]::GetFileName($outputRootFull) -eq '') {
    throw "OutputRoot ist ungültig: $outputRootFull"
}

$timestamp = Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'
$runDirectory = Join-Path $outputRootFull $timestamp
if (Test-Path -LiteralPath $runDirectory) {
    throw "Das UiAudit-Ausgabeverzeichnis existiert bereits; der Lauf würde überschreiben: $runDirectory"
}
New-Item -ItemType Directory -Path $runDirectory | Out-Null

$env:KNOWHOWTOAI_UI_AUDIT_ENABLED = '1'
$env:KNOWHOWTOAI_UI_AUDIT_OUTPUT_DIR = $runDirectory

$projectPath = Join-Path $repoRoot 'tests/KnowHowToAI.BrowserTests/KnowHowToAI.BrowserTests.csproj'
Write-Host "[UiAudit] Ausgabe: $runDirectory" -ForegroundColor Cyan
Write-Host '[UiAudit] Viewport: Desktop 1280x800' -ForegroundColor Cyan

& dotnet test $projectPath --nologo --filter 'Category=UiAudit'
$exitCode = $LASTEXITCODE
Write-Host "[UiAudit] Exitcode: $exitCode; Run-Verzeichnis: $runDirectory" -ForegroundColor $(if ($exitCode -eq 0) { 'Green' } else { 'Red' })
exit $exitCode
