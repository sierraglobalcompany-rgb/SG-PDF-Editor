param(
    [switch]$SkipGraphBuild
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$GsdVersion = '1.15.0'
$GraphifyVersion = '0.9.77'
$GraphifyVenv = Join-Path $RepoRoot '.devtools\graphify-venv'
$GraphifyPython = Join-Path $GraphifyVenv 'Scripts\python.exe'
$GraphifyExe = Join-Path $GraphifyVenv 'Scripts\graphify.exe'

Push-Location $RepoRoot
try {
    Write-Host "== SG PDF Editor development tooling =="

    $nodeVersionText = (& node --version).Trim().TrimStart('v')
    $nodeVersion = [Version]$nodeVersionText
    if ($nodeVersion.Major -lt 24) {
        throw "Node.js 24+ is required for the validated GSD setup. Found: $nodeVersionText"
    }

    & python -c "import sys; assert sys.version_info >= (3, 10), 'Python 3.10+ required'"

    Write-Host "Installing GSD Core $GsdVersion project-scoped for Codex..."
    & npx --yes "@opengsd/gsd-core@$GsdVersion" --codex --local
    if ($LASTEXITCODE -ne 0) { throw "GSD Core install failed with exit code $LASTEXITCODE" }

    if (-not (Test-Path $GraphifyPython)) {
        Write-Host "Creating project-local Graphify virtual environment..."
        & python -m venv $GraphifyVenv
        if ($LASTEXITCODE -ne 0) { throw "Python venv creation failed with exit code $LASTEXITCODE" }
    }

    Write-Host "Installing Graphify $GraphifyVersion into project-local virtual environment..."
    & $GraphifyPython -m pip install --disable-pip-version-check --upgrade "graphifyy==$GraphifyVersion"
    if ($LASTEXITCODE -ne 0) { throw "Graphify install failed with exit code $LASTEXITCODE" }

    & $GraphifyExe --version
    & $GraphifyExe install --project --platform codex
    if ($LASTEXITCODE -ne 0) { throw "Graphify Codex project install failed with exit code $LASTEXITCODE" }

    if (-not $SkipGraphBuild) {
        Write-Host "Building local AST graph (src + tests only via .graphifyignore)..."
        $env:GRAPHIFY_QUERY_LOG_DISABLE = '1'
        & $GraphifyExe . --no-viz
        if ($LASTEXITCODE -ne 0) { throw "Graphify build failed with exit code $LASTEXITCODE" }

        & $GraphifyExe cluster-only .
        if ($LASTEXITCODE -ne 0) { throw "Graphify cluster/report failed with exit code $LASTEXITCODE" }

        Write-Host "Acceptance query: PdfDocumentSession"
        & $GraphifyExe query 'PdfDocumentSession' --graph 'graphify-out/graph.json'
        if ($LASTEXITCODE -ne 0) { throw "Graphify acceptance query failed with exit code $LASTEXITCODE" }
    }

    Write-Host "Development tooling is installed locally."
    Write-Host "GSD: .codex/ (generated, ignored)"
    Write-Host "Graphify venv: .devtools/graphify-venv/ (generated, ignored)"
    Write-Host "Graph: graphify-out/ (generated, ignored)"
    Write-Host "Operational state: .planning/ (versioned)"
}
finally {
    Pop-Location
}
