param(
    [string]$DestinationDirectory,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$version = '12.4.2'
$archiveUri = 'https://github.com/qpdf/qpdf/releases/download/v12.4.2/qpdf-12.4.2-msvc64.zip'
$expectedSha256 = 'db87077e683630c1217e0e8f9a20a9749d952ab676e881c3689187763a5de25d'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($DestinationDirectory)) {
    $DestinationDirectory = Join-Path $repoRoot 'third_party/runtime/qpdf'
}
else {
    $DestinationDirectory = [System.IO.Path]::GetFullPath($DestinationDirectory)
}

$destinationExe = Join-Path $DestinationDirectory 'qpdf.exe'
$markerPath = Join-Path $DestinationDirectory 'archive.sha256'

if (-not $Force -and (Test-Path $destinationExe) -and (Test-Path $markerPath)) {
    $marker = (Get-Content $markerPath -Raw).Trim().ToLowerInvariant()
    if ($marker -eq $expectedSha256) {
        Write-Host "qpdf $version already staged: $destinationExe"
        return
    }
}

$cacheDirectory = Join-Path $repoRoot 'third_party/cache/qpdf'
$archivePath = Join-Path $cacheDirectory "qpdf-$version-msvc64.zip"
$extractDirectory = Join-Path $cacheDirectory "extract-$version"

New-Item -ItemType Directory -Force -Path $cacheDirectory | Out-Null
Invoke-WebRequest -Uri $archiveUri -OutFile $archivePath

$actualSha256 = (Get-FileHash $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualSha256 -ne $expectedSha256) {
    Remove-Item $archivePath -Force -ErrorAction SilentlyContinue
    throw "qpdf $version SHA-256 mismatch. Expected $expectedSha256 but got $actualSha256."
}

Remove-Item $extractDirectory -Recurse -Force -ErrorAction SilentlyContinue
Expand-Archive -Path $archivePath -DestinationPath $extractDirectory -Force
$sourceExe = Get-ChildItem $extractDirectory -Recurse -File -Filter 'qpdf.exe' | Select-Object -First 1
if (-not $sourceExe) {
    throw "qpdf.exe was not found in the verified qpdf $version archive."
}

# The official Windows binary ZIP is relocatable and includes everything needed
# beside qpdf.exe. Stage that directory as a unit so product runtime stays offline.
$sourceDirectory = $sourceExe.Directory.FullName
Remove-Item $DestinationDirectory -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $DestinationDirectory | Out-Null
Copy-Item (Join-Path $sourceDirectory '*') $DestinationDirectory -Recurse -Force
Set-Content -Path $markerPath -Value $expectedSha256 -Encoding ascii -NoNewline

Remove-Item $extractDirectory -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "qpdf $version staged: $destinationExe"
