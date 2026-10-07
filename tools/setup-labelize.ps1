param(
    [string]$DestinationDirectory,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$version = '1.7.0'
$archiveUri = 'https://github.com/GOODBOY008/labelize/releases/download/v1.7.0/labelize-x86_64-pc-windows-msvc.zip'
$expectedSha256 = 'cdd4030b0d1a8bad69b93f49866c8dcc5314af8975bb16a76991fe32f92dd21d'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($DestinationDirectory)) {
    $DestinationDirectory = Join-Path $repoRoot 'third_party/runtime/labelize'
}
else {
    $DestinationDirectory = [System.IO.Path]::GetFullPath($DestinationDirectory)
}

$destinationExe = Join-Path $DestinationDirectory 'labelize.exe'
$markerPath = Join-Path $DestinationDirectory 'archive.sha256'

if (-not $Force -and (Test-Path $destinationExe) -and (Test-Path $markerPath)) {
    $marker = (Get-Content $markerPath -Raw).Trim().ToLowerInvariant()
    if ($marker -eq $expectedSha256) {
        Write-Host "Labelize $version already staged: $destinationExe"
        return
    }
}

$cacheDirectory = Join-Path $repoRoot 'third_party/cache/labelize'
$archivePath = Join-Path $cacheDirectory "labelize-$version-win-x64.zip"
$extractDirectory = Join-Path $cacheDirectory "extract-$version"

New-Item -ItemType Directory -Force -Path $cacheDirectory | Out-Null

Invoke-WebRequest -Uri $archiveUri -OutFile $archivePath
$actualSha256 = (Get-FileHash $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualSha256 -ne $expectedSha256) {
    Remove-Item $archivePath -Force -ErrorAction SilentlyContinue
    throw "Labelize $version SHA-256 mismatch. Expected $expectedSha256 but got $actualSha256."
}

Remove-Item $extractDirectory -Recurse -Force -ErrorAction SilentlyContinue
Expand-Archive -Path $archivePath -DestinationPath $extractDirectory -Force
$sourceExe = Get-ChildItem $extractDirectory -Recurse -File -Filter 'labelize.exe' | Select-Object -First 1
if (-not $sourceExe) {
    throw "labelize.exe was not found in the verified Labelize $version archive."
}

New-Item -ItemType Directory -Force -Path $DestinationDirectory | Out-Null
Copy-Item $sourceExe.FullName $destinationExe -Force
Set-Content -Path $markerPath -Value $expectedSha256 -Encoding ascii -NoNewline

Remove-Item $extractDirectory -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Labelize $version staged: $destinationExe"
