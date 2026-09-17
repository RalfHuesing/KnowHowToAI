#Requires -Version 7
<#
.SYNOPSIS
  Startet einen agentischen Eval-Lauf gegen den KnowHowToAI-MCP-Server.

.DESCRIPTION
  Ablauf: alte Server-Instanzen beenden -> Build wärmen -> MCP-Verbindungstest ->
  Prompt aus Template + task.md komponieren -> Eval-Agent (hermes chat -q) starten.
  Der Eval-Agent schreibt sein Journal nach tasks/eval-<Name>/journal.md.

  Das Eval-Verzeichnis tasks/eval-<Name>/ muss existieren und eine task.md
  enthalten (Instruction + Initialzustand + Verifikation) — siehe
  .agents/prompts/evals/orchestrator-prompt.md.

.PARAMETER Name
  Eval-Name (kebab-case). Verzeichnis: tasks/eval-<Name>.

.PARAMETER MaxTurns
  Cap für Tool-Calling-Iterationen des Eval-Agenten (Default 40).

.EXAMPLE
  pwsh -NoProfile -File scripts/eval-run.ps1 -Name smoke-verify
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Name,
    [int]$MaxTurns = 40
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent $PSScriptRoot
$EvalDir = Join-Path $RepoRoot "tasks/eval-$Name"
$TemplatePath = Join-Path $RepoRoot ".agents/prompts/evals/eval-agent-template.md"
$ServerProj = Join-Path $RepoRoot "src/KnowHowToAI.Server/KnowHowToAI.Server.csproj"

if (-not (Test-Path $EvalDir)) { throw "Eval-Verzeichnis fehlt: $EvalDir" }
$taskPath = Join-Path $EvalDir "task.md"
if (-not (Test-Path $taskPath)) { throw "task.md fehlt: $taskPath" }
if (-not (Test-Path $TemplatePath)) { throw "Template fehlt: $TemplatePath" }

function Write-Step([string]$message) { Write-Host "`n=== $message ===" }

# ── 1. Alte Server-Instanzen beenden (DLL-Lock verhindern) ──────────────────
Write-Step "Alte KnowHowToAI-Server-Prozesse beenden"
$procs = Get-CimInstance Win32_Process | Where-Object {
    $_.Name -eq 'KnowHowToAI.Server.exe' -or
    ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match 'KnowHowToAI\.Server')
}
if ($procs) {
    foreach ($p in $procs) {
        Write-Host "  Stoppe PID $($p.ProcessId): $($p.Name)"
        Stop-Process -Id $p.ProcessId -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 2
} else {
    Write-Host "  Keine laufenden Server-Instanzen."
}

# ── 2. Build wärmen (Cold Build sprengt das mcp-test-Timeout) ────────────────
Write-Step "Build wärmen"
dotnet build $ServerProj -v q --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet build fehlgeschlagen (Exit $LASTEXITCODE)" }

# ── 3. MCP-Verbindungstest (Sanity: Server startet, Tools entdeckt) ──────────
Write-Step "MCP-Verbindungstest"
hermes mcp test KnowHowToAI
if ($LASTEXITCODE -ne 0) { throw "hermes mcp test fehlgeschlagen (Exit $LASTEXITCODE)" }

# ── 4. Prompt komponieren ────────────────────────────────────────────────────
Write-Step "Prompt komponieren"
$promptPath = Join-Path $EvalDir "prompt.md"
$template = Get-Content -Raw $TemplatePath
$task = Get-Content -Raw $taskPath
$prompt = $template
$prompt = $prompt.Replace('{{EVAL_NAME}}', $Name)
$prompt = $prompt.Replace('{{DATE}}', (Get-Date -Format 'yyyy-MM-dd'))
$prompt = $prompt.Replace('{{EVAL_DIR}}', $EvalDir)
$prompt = $prompt.Replace('{{TASK}}', $task.Trim())
Set-Content -Path $promptPath -Value $prompt -Encoding utf8
Write-Host "  Geschrieben: $promptPath ($((Get-Item $promptPath).Length) Bytes)"

# ── 5. Eval-Agent starten ────────────────────────────────────────────────────
Write-Step "Eval-Agent starten (hermes chat -q, MaxTurns=$MaxTurns)"
$runLog = Join-Path $EvalDir "run.log"
$promptArg = Get-Content -Raw $promptPath
& hermes chat -q $promptArg --max-turns $MaxTurns --source "eval-mcp-$Name" *> $runLog
$hermesExit = $LASTEXITCODE
Write-Host "  hermes exit: $hermesExit  Log: $runLog"

# ── 6. Zusammenfassung ───────────────────────────────────────────────────────
Write-Step "Zusammenfassung"
$journalPath = Join-Path $EvalDir "journal.md"
Write-Host "  Eval-Verzeichnis : $EvalDir"
Write-Host "  Journal          : $journalPath ($(if (Test-Path $journalPath) { "$((Get-Item $journalPath).Length) Bytes" } else { 'FEHLT - Agent hat nicht geschrieben?' }))"
Write-Host "  Run-Log          : $runLog ($((Get-Item $runLog).Length) Bytes)"
Write-Host "  Session (resume) : $(Select-String -Path $runLog -Pattern 'hermes --resume (\S+)' | ForEach-Object { $_.Matches[0].Groups[1].Value } | Select-Object -First 1)"
if ($hermesExit -ne 0) { Write-Warning "hermes chat endete mit Exit $hermesExit — run.log prüfen." }
