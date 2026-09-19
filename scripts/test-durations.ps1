#requires -Version 7.0
<#
.SYNOPSIS
    Wertet Testdauern aus TRX-Dateien aus und gibt Gesamtsumme, Testanzahl sowie die Top-Klassen aus.

.PARAMETER TrxPath
    Pfad(e) zu den auszuwertenden TRX-Dateien.

.PARAMETER Top
    Anzahl der langsamsten Klassen in der Ergebnistabelle (Standard: 15).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string[]]$TrxPath,

    [int]$Top = 15
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

foreach ($path in $TrxPath) {
    if (-not (Test-Path -Path $path -PathType Leaf)) {
        [Console]::Error.WriteLine("Fehler: TRX-Datei '$path' wurde nicht gefunden.")
        exit 1
    }
}

$classByTestId = @{}
$results = [System.Collections.Generic.List[psobject]]::new()

foreach ($path in $TrxPath) {
    [xml]$xml = Get-Content -Path $path -Raw
    $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $ns.AddNamespace('ns', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')

    $unitTests = $xml.SelectNodes('//ns:UnitTest', $ns)
    if ($unitTests -ne $null) {
        foreach ($ut in $unitTests) {
            $id = $ut.GetAttribute('id')
            $methodNode = $ut.SelectSingleNode('ns:TestMethod', $ns)
            $className = if ($methodNode -ne $null) { $methodNode.GetAttribute('className') } else { '' }
            if ($id -and $className) {
                $classByTestId[$id] = $className
            }
        }
    }

    $unitTestResults = $xml.SelectNodes('//ns:UnitTestResult', $ns)
    if ($unitTestResults -ne $null) {
        foreach ($utr in $unitTestResults) {
            $testId = $utr.GetAttribute('testId')
            $durationStr = $utr.GetAttribute('duration')
            $testName = $utr.GetAttribute('testName')

            $duration = [TimeSpan]::Zero
            if ($durationStr -and [TimeSpan]::TryParse($durationStr, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$duration)) {
                $className = if ($classByTestId.ContainsKey($testId)) { $classByTestId[$testId] } else { 'Unbekannt' }
                $results.Add([PSCustomObject]@{
                    TestId    = $testId
                    TestName  = $testName
                    ClassName = $className
                    Duration  = $duration
                })
            }
        }
    }
}

if ($results.Count -eq 0) {
    Write-Host "Keine Testergebnisse in den angegebenen TRX-Dateien gefunden."
    exit 0
}

$totalDuration = [TimeSpan]::Zero
foreach ($r in $results) {
    $totalDuration = $totalDuration.Add($r.Duration)
}

$groups = $results | Group-Object -Property ClassName

$classSummaries = foreach ($g in $groups) {
    $classSum = [TimeSpan]::Zero
    $longest = $null

    foreach ($item in $g.Group) {
        $classSum = $classSum.Add($item.Duration)
        if ($longest -eq $null -or $item.Duration -gt $longest.Duration) {
            $longest = $item
        }
    }

    [PSCustomObject]@{
        Klasse    = $g.Name
        Summe     = $classSum
        SummeSek  = $classSum.TotalSeconds
        Anzahl    = $g.Count
        Laengster = "{0:N2} s ({1})" -f $longest.Duration.TotalSeconds, $longest.TestName
    }
}

$topClasses = $classSummaries | Sort-Object -Property SummeSek -Descending | Select-Object -First $Top

Write-Host ""
Write-Host "=== Testdauer-Auswertung ===" -ForegroundColor Cyan
Write-Host ("Tests gesamt: {0}" -f $results.Count)
Write-Host ("Gesamtsumme : {0:N2} s ({1})" -f $totalDuration.TotalSeconds, $totalDuration.ToString())
Write-Host ""

$tableOutput = $topClasses | Select-Object `
    Klasse,
    @{ Name = 'Summe'; Expression = { "{0:N2} s" -f $_.SummeSek } },
    Anzahl,
    @{ Name = 'Längster Einzeltest'; Expression = { $_.Laengster } }

$tableOutput | Format-Table -AutoSize | Out-String | Write-Host
