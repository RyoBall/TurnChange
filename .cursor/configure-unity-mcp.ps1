# One-click Unity MCP configuration for Cursor (stdio transport — reliable on Windows)
$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot

function Find-UvxPath {
    $candidates = @(
        "$env:LOCALAPPDATA\Microsoft\WinGet\Links\uvx.exe",
        "$env:USERPROFILE\.local\bin\uvx.exe"
    )
    foreach ($p in $candidates) {
        if (Test-Path $p) { return $p }
    }
    $cmd = Get-Command uvx -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    return $null
}

function Get-UnityMcpVersion {
    $cacheRoot = Join-Path $ProjectRoot "Library\PackageCache"
    if (-not (Test-Path $cacheRoot)) { return "9.7.3" }
    $pkg = Get-ChildItem $cacheRoot -Filter "com.coplaydev.unity-mcp@*" -Directory -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $pkg) { return "9.7.3" }
    try {
        $json = Get-Content (Join-Path $pkg.FullName "package.json") -Raw | ConvertFrom-Json
        return $json.version
    } catch {
        return "9.7.3"
    }
}

function Write-McpJson($path, $config) {
    $dir = Split-Path $path -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    $json = $config | ConvertTo-Json -Depth 6
    [System.IO.File]::WriteAllText($path, $json + "`n", [System.Text.UTF8Encoding]::new($false))
    Write-Host "[OK] $path" -ForegroundColor Green
}

$uvxPath = Find-UvxPath
if (-not $uvxPath) {
    Write-Host "[ERROR] uvx not found. Install: irm https://astral.sh/uv/install.ps1 | iex" -ForegroundColor Red
    exit 1
}

$version = Get-UnityMcpVersion
Write-Host "=== Unity MCP Configure ===" -ForegroundColor Cyan
Write-Host "Project: $ProjectRoot"
Write-Host "uvx:     $uvxPath"
Write-Host "Package: mcpforunityserver==$version"
Write-Host "Transport: stdio (Cursor launches server; no Unity HTTP needed)"
Write-Host ""

$unityMcp = @{
    command = $uvxPath.Replace("/", "\")
    args    = @(
        "--from", "mcpforunityserver==$version",
        "mcp-for-unity",
        "--transport", "stdio"
    )
    type    = "stdio"
}

$cursorConfig = @{
    mcpServers = @{
        unityMCP = $unityMcp
    }
}

$projectMcp = Join-Path $ProjectRoot ".cursor\mcp.json"
$globalMcp  = Join-Path $env:USERPROFILE ".cursor\mcp.json"

Write-McpJson $projectMcp $cursorConfig
Write-McpJson $globalMcp $cursorConfig

$rootMcp = Join-Path $ProjectRoot ".mcp.json"
$rootConfig = @{
    mcpServers = @{
        UnityMCP = $unityMcp
    }
}
Write-McpJson $rootMcp $rootConfig

Write-Host ""
Write-Host "=== Unity side (2 clicks) ===" -ForegroundColor Cyan
Write-Host "1. Window -> MCP for Unity -> set Transport to stdio (if HTTP is selected)"
Write-Host "2. Click 'Start Session' if status is not Connected"
Write-Host ""
Write-Host "=== Cursor side ===" -ForegroundColor Cyan
Write-Host "1. Settings -> MCP -> enable unityMCP"
Write-Host "2. Fully restart Cursor (Unity Editor stays open)"
Write-Host ""

$unityCount = (Get-Process -Name "Unity" -ErrorAction SilentlyContinue | Measure-Object).Count
if ($unityCount -gt 1) {
    Write-Host "[WARN] $unityCount Unity processes detected — close extras to avoid bridge conflicts" -ForegroundColor Yellow
}

Write-Host "Done." -ForegroundColor Green
