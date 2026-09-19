#requires -Version 7.0
<#
.SYNOPSIS
    Killt blockierende Prozesse, baut die Solution und schreibt den kompletten
    Build-Output in die statische Datei temp/build.log.

.DESCRIPTION
    MSB3021/MSB3027 (DLL durch laufende EXE gesperrt) vermeiden: Vor dem Build
    werden KnowHowToAI.Server.exe und testhost.exe hart beendet (laufen sie
    nicht, ist das kein Fehler). Der Build läuft anschließend gegen
    KnowHowToAI.slnx; der Exitcode des Builds wird durchgereicht.
    Der Output steht immer in temp/build.log (wird pro Lauf überschrieben).
#>
[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$AdditionalArgs
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$solutionPath = Join-Path $repoRoot 'KnowHowToAI.slnx'
$logPath = Join-Path $repoRoot 'temp/build.log'
New-Item -ItemType Directory -Path (Split-Path $logPath) -Force | Out-Null

# 1. Blockierende Prozesse hart beenden; nicht gefunden ist kein Fehler.
foreach ($processName in @('KnowHowToAI.Server.exe', 'testhost.exe')) {
    & taskkill /F /IM $processName 2>$null | Out-Null
    $null = $LASTEXITCODE
}

# 2. Build; kompletter Output (stdout + stderr) in die statische Logdatei.
$allArgs = @('build', $solutionPath, '--nologo') + @($AdditionalArgs)
& dotnet @allArgs *> $logPath
$buildExitCode = $LASTEXITCODE

$color = if ($buildExitCode -eq 0) { 'Green' } else { 'Red' }
Write-Host ("[Build] Exitcode {0}; Output: {1}" -f $buildExitCode, $logPath) -ForegroundColor $color
exit $buildExitCode
