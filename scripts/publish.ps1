#requires -Version 7.0
<#
.SYNOPSIS
    Killt blockierende Prozesse und veröffentlicht KnowHowToAI.Server nach publish/.

.DESCRIPTION
    1. Beendet KnowHowToAI.Server.exe, server.exe und testhost.exe, um Dateisperren zu vermeiden.
    2. Führt 'dotnet publish' für src/KnowHowToAI.Server/KnowHowToAI.Server.csproj nach publish/ aus.
       Dabei werden alle Binärdateien, appsettings.json, wwwroot/ und Web-Asset-Manifeste veröffentlicht.
    3. Stellt sicher, dass die ausführbare Datei KnowHowToAI.Server.exe bereitsteht.
#>
[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$AdditionalArgs
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $repoRoot 'src/KnowHowToAI.Server/KnowHowToAI.Server.csproj'
$publishDir = Join-Path $repoRoot 'publish'

# 1. Blockierende Prozesse hart beenden; nicht gefunden ist kein Fehler.
foreach ($processName in @('KnowHowToAI.Server.exe', 'server.exe', 'testhost.exe')) {
    & taskkill /F /IM $processName 2>$null | Out-Null
    $null = $LASTEXITCODE
}

# 2. Publish
Write-Host "[Publish] Veröffentliche KnowHowToAI.Server nach $publishDir..." -ForegroundColor Cyan
$allArgs = @('publish', $projectPath, '-c', 'Release', '-o', $publishDir, '--nologo') + @($AdditionalArgs)
& dotnet @allArgs
$publishExitCode = $LASTEXITCODE

if ($publishExitCode -ne 0) {
    Write-Host ("[Publish] Fehler beim Publish (Exitcode {0})." -f $publishExitCode) -ForegroundColor Red
    exit $publishExitCode
}

# 3. Alte server.exe aufräumen, falls noch vorhanden
$oldServerExe = Join-Path $publishDir 'server.exe'
if (Test-Path $oldServerExe) {
    Remove-Item $oldServerExe -Force -ErrorAction SilentlyContinue
}

$serverExe = Join-Path $publishDir 'KnowHowToAI.Server.exe'

Write-Host "[Publish] Erfolgreich bereitgestellt:" -ForegroundColor Green
Write-Host "  Verzeichnis : $publishDir" -ForegroundColor Green
Write-Host "  Startdatei  : $serverExe" -ForegroundColor Green
Write-Host "  Starten     : & '$serverExe'" -ForegroundColor Green
exit 0
