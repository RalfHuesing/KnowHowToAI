#requires -Version 7.0
<#
.SYNOPSIS
    Fuehrt FastTests fuer KnowHowToAI aus und schreibt je Testprojekt eine statische TRX-Datei.

.PARAMETER Filter
    xUnit-Filter fuer dotnet test (Standard: Category=Unit).
#>
[CmdletBinding()]
param(
    [string]$Filter = 'Category=Unit',
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$AdditionalArgs
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $root = git rev-parse --show-toplevel 2>$null
    if (-not $root) {
        $candidate = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
        if (Test-Path (Join-Path $candidate 'KnowHowToAI.slnx')) {
            return $candidate
        }
        throw 'Repository-Root konnte nicht ermittelt werden.'
    }
    return (Resolve-Path $root).Path
}

$repoRoot = Get-RepoRoot
$resultsDir = Join-Path $repoRoot 'TestResults'
if (-not (Test-Path $resultsDir)) {
    New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null
}

$testProjects = @(
    'tests/KnowHowToAI.Core.Tests/KnowHowToAI.Core.Tests.csproj',
    'tests/KnowHowToAI.IntegrationTests/KnowHowToAI.IntegrationTests.csproj',
    'tests/KnowHowToAI.Web.Tests/KnowHowToAI.Web.Tests.csproj'
)

foreach ($relativeProjectPath in $testProjects) {
    $projectPath = Join-Path $repoRoot $relativeProjectPath
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectPath)
    $trxFile = "FastTests.$projectName.trx"

    Write-Host "[INFO] FastTests (Filter: $Filter) -> TestResults/$trxFile" -ForegroundColor Cyan

    $allArgs = @(
        'test',
        $projectPath,
        '--filter', $Filter,
        '--logger', "trx;LogFileName=$trxFile",
        '--results-directory', $resultsDir
    )
    if ($AdditionalArgs) {
        $allArgs += $AdditionalArgs
    }

    & dotnet @allArgs
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    try {
        $trxFilePath = Join-Path $resultsDir $trxFile
        & (Join-Path $PSScriptRoot 'test-durations.ps1') -TrxPath $trxFilePath
    }
    catch {
        Write-Warning "Testdauer-Auswertung fehlgeschlagen: $_"
    }
}
