#requires -Version 7.0
<#
.SYNOPSIS
    Fuehrt IntegrationTests fuer KnowHowToAI aus und schreibt das Ergebnis statisch nach TestResults/IntegrationTests.trx.

.PARAMETER Filter
    xUnit-Filter fuer dotnet test (Standard: Category!=Stress).
#>
[CmdletBinding()]
param(
    [string]$Filter = 'Category=Integration',
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

$trxFile = 'IntegrationTests.trx'
$projectPath = Join-Path $repoRoot 'tests/KnowHowToAI.IntegrationTests/KnowHowToAI.IntegrationTests.csproj'

Write-Host '[INFO] SQL-Preflight: Verbindung zur manuell bereitgestellten DatabaseConnection aus appsettings.json' -ForegroundColor Cyan
& dotnet test $projectPath '--filter' 'FullyQualifiedName~SqlIntegrationPreflightTests' '--nologo'
if ($LASTEXITCODE -ne 0) {
    throw 'SQL-Preflight fehlgeschlagen. Prüfe DatabaseConnection in src/KnowHowToAI.Server/appsettings.json sowie Erreichbarkeit der manuell bereitgestellten Datenbank.'
}

Write-Host "[INFO] IntegrationTests (Filter: $Filter) -> TestResults/$trxFile" -ForegroundColor Cyan

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

$browserProjectPath = Join-Path $repoRoot 'tests/KnowHowToAI.BrowserTests/KnowHowToAI.BrowserTests.csproj'
& dotnet test $browserProjectPath '--filter' $Filter '--logger' 'trx;LogFileName=IntegrationTests.Browser.trx' '--results-directory' $resultsDir
exit $LASTEXITCODE
