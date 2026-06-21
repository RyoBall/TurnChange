# TurnChange Cursor + Unity MCP setup verification
# Only validates Cursor IDE config; does not modify Assets/ or Packages/.

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot

Write-Host "=== TurnChange Cursor Setup ===" -ForegroundColor Cyan
Write-Host "Project: $ProjectRoot"
Write-Host ""

$requiredFiles = @(
    ".cursorignore",
    ".cursor/mcp.json",
    ".cursor/rules/unity-core.mdc",
    ".cursor/rules/unity-csharp.mdc",
    ".cursor/rules/lobby-system.mdc",
    ".cursor/rules/battle-system.mdc",
    ".cursor/rules/unity-editor-tools.mdc",
    ".cursor/skills/unity-mcp-setup/SKILL.md",
    ".vscode/mcp.json"
)

$allOk = $true
foreach ($rel in $requiredFiles) {
    $path = Join-Path $ProjectRoot $rel
    if (Test-Path $path) {
        Write-Host "[OK] $rel" -ForegroundColor Green
    } else {
        Write-Host "[MISSING] $rel" -ForegroundColor Red
        $allOk = $false
    }
}

Write-Host ""
if ($allOk) {
    Write-Host "Cursor-side config is ready." -ForegroundColor Green
} else {
    Write-Host "Some config files are missing." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== One-time Unity step (manual, not in repo) ===" -ForegroundColor Cyan
Write-Host "1. Window -> Package Manager -> + -> Add package from git URL"
Write-Host "2. URL: https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main"
Write-Host "3. Window -> MCP for Unity -> Configure Selected -> check Cursor"
Write-Host "4. Cursor Settings -> MCP -> enable unityMCP"
Write-Host "5. Restart Cursor with Unity Editor open"

Write-Host ""
Write-Host "=== Connection check ===" -ForegroundColor Cyan
try {
    Invoke-WebRequest -Uri "http://localhost:8080/mcp" -Method GET -TimeoutSec 2 -ErrorAction Stop | Out-Null
    Write-Host "[OK] Unity MCP server responding on localhost:8080" -ForegroundColor Green
} catch {
    Write-Host "[--] Unity MCP not running yet (install MCP package in Unity first)" -ForegroundColor DarkYellow
}

Write-Host ""
Write-Host "Done." -ForegroundColor Cyan
