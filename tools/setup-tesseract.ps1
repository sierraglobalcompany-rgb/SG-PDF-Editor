param(
    [string]$DestinationDirectory,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$version = '5.5.3.20260724'
$releaseTag = '5.5.3'
$installerName = "tesseract-ocr-w64-setup-$version.exe"
$installerUri = "https://github.com/tesseract-ocr/tesseract/releases/download/$releaseTag/$installerName"
$expectedInstallerSha256 = 'bee9e3434bd94fd65387d9be28cd467a41f61b1275383b55b0f59a1331270ae4'
$tessdataCommit = '87416418657359cb625c412a48b6e1d6d41c29bd'
$tessdataBaseUri = "https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/$tessdataCommit"
$languages = @(
    @{
        Name = 'spa'
        Size = 2294433
        GitBlobSha1 = '72e901f13ca52cfe34cf239a368b9ed3c0ddaf26'
        Sha256 = '6f2e04d02774a18f01bed44b1111f2cd7f3ba7ac9dc4373cd3f898a40ea6b464'
    },
    @{
        Name = 'eng'
        Size = 4113088
        GitBlobSha1 = 'bbef4675053b5b468cdb477053e28b1c698ba08e'
        Sha256 = '7d4322bd2a7749724879683fc3912cb542f19906c83bcc1a52132556427170b2'
    }
)

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($DestinationDirectory)) {
    $DestinationDirectory = Join-Path $repoRoot 'third_party/runtime/tesseract'
}
else {
    $DestinationDirectory = [System.IO.Path]::GetFullPath($DestinationDirectory)
}

$destinationExe = Join-Path $DestinationDirectory 'tesseract.exe'
$runtimeManifestPath = Join-Path $DestinationDirectory 'runtime-manifest.json'
if (-not $Force -and (Test-Path $destinationExe) -and (Test-Path $runtimeManifestPath)) {
    $existingManifest = Get-Content $runtimeManifestPath -Raw | ConvertFrom-Json
    if ($existingManifest.version -eq $version -and
        $existingManifest.installerSha256 -eq $expectedInstallerSha256 -and
        $existingManifest.tessdataFastCommit -eq $tessdataCommit -and
        $existingManifest.spaSha256 -eq $languages[0].Sha256 -and
        $existingManifest.engSha256 -eq $languages[1].Sha256) {
        Write-Host "Tesseract $version already staged: $destinationExe"
        return
    }
}

function Get-SevenZipPath {
    $command = Get-Command '7z.exe' -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $candidates = @(
        (Join-Path $env:ProgramFiles '7-Zip/7z.exe'),
        (Join-Path ${env:ProgramFiles(x86)} '7-Zip/7z.exe')
    )
    foreach ($candidate in $candidates) {
        if (-not [string]::IsNullOrWhiteSpace($candidate) -and (Test-Path $candidate)) {
            return $candidate
        }
    }

    throw '7-Zip is required to extract the pinned Tesseract NSIS archive without installing it.'
}

function Get-GitBlobSha1([string]$Path) {
    $fileInfo = Get-Item $Path
    $prefix = [System.Text.Encoding]::ASCII.GetBytes("blob $($fileInfo.Length)`0")
    $sha1 = [System.Security.Cryptography.SHA1]::Create()
    try {
        [void]$sha1.TransformBlock($prefix, 0, $prefix.Length, $null, 0)
        $stream = [System.IO.File]::OpenRead($Path)
        try {
            $buffer = New-Object byte[] 65536
            while (($read = $stream.Read($buffer, 0, $buffer.Length)) -gt 0) {
                [void]$sha1.TransformBlock($buffer, 0, $read, $null, 0)
            }
        }
        finally {
            $stream.Dispose()
        }
        [void]$sha1.TransformFinalBlock([byte[]]::new(0), 0, 0)
        return ([System.BitConverter]::ToString($sha1.Hash)).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha1.Dispose()
    }
}

$cacheDirectory = Join-Path $repoRoot 'third_party/cache/tesseract'
$installerPath = Join-Path $cacheDirectory $installerName
$extractDirectory = Join-Path $cacheDirectory "extract-$version"
New-Item -ItemType Directory -Force -Path $cacheDirectory | Out-Null

if (-not (Test-Path $installerPath)) {
    Invoke-WebRequest -Uri $installerUri -OutFile $installerPath
}

$actualInstallerSha256 = (Get-FileHash $installerPath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualInstallerSha256 -ne $expectedInstallerSha256) {
    Remove-Item $installerPath -Force -ErrorAction SilentlyContinue
    throw "Tesseract installer SHA-256 mismatch. Expected $expectedInstallerSha256 but got $actualInstallerSha256."
}

$sevenZip = Get-SevenZipPath
Remove-Item $extractDirectory -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $extractDirectory | Out-Null
& $sevenZip x -y "-o$extractDirectory" $installerPath | Out-Host
if ($LASTEXITCODE -ne 0) {
    throw "7-Zip failed to extract the pinned Tesseract installer with exit code $LASTEXITCODE."
}

$sourceExe = Get-ChildItem $extractDirectory -Recurse -File -Filter 'tesseract.exe' |
    Where-Object { Test-Path (Join-Path $_.Directory.FullName 'libtesseract-*.dll') } |
    Select-Object -First 1
if (-not $sourceExe) {
    throw 'tesseract.exe with its private libtesseract runtime was not found in the verified installer archive.'
}

$sourceDirectory = $sourceExe.Directory.FullName
Remove-Item $DestinationDirectory -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $DestinationDirectory | Out-Null
Copy-Item (Join-Path $sourceDirectory '*') $DestinationDirectory -Recurse -Force

$tessdataDirectory = Join-Path $DestinationDirectory 'tessdata'
New-Item -ItemType Directory -Force -Path $tessdataDirectory | Out-Null

$languageManifest = @{}
foreach ($language in $languages) {
    $name = $language.Name
    $fileName = "$name.traineddata"
    $languagePath = Join-Path $tessdataDirectory $fileName
    Invoke-WebRequest -Uri "$tessdataBaseUri/$fileName" -OutFile $languagePath

    $actualLength = (Get-Item $languagePath).Length
    if ($actualLength -ne $language.Size) {
        throw "$fileName size mismatch. Expected $($language.Size) but got $actualLength."
    }

    $actualGitBlobSha1 = Get-GitBlobSha1 $languagePath
    if ($actualGitBlobSha1 -ne $language.GitBlobSha1) {
        throw "$fileName Git blob SHA-1 mismatch. Expected $($language.GitBlobSha1) but got $actualGitBlobSha1."
    }

    $actualSha256 = (Get-FileHash $languagePath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualSha256 -ne $language.Sha256) {
        throw "$fileName SHA-256 mismatch. Expected $($language.Sha256) but got $actualSha256."
    }

    Write-Host "$fileName SHA-256: $actualSha256"
    $languageManifest[$name] = @{
        size = $actualLength
        gitBlobSha1 = $actualGitBlobSha1
        sha256 = $actualSha256
    }
}

$stagedExe = Join-Path $DestinationDirectory 'tesseract.exe'
if (-not (Test-Path $stagedExe)) {
    throw 'The staged Tesseract executable is missing.'
}

$versionOutput = (& $stagedExe --version 2>&1 | Out-String)
if ($LASTEXITCODE -ne 0 -or $versionOutput -notmatch [regex]::Escape("tesseract v$version")) {
    throw "The staged Tesseract runtime did not report expected version $version. Output: $versionOutput"
}

$manifest = [ordered]@{
    version = $version
    releaseTag = $releaseTag
    installerName = $installerName
    installerUri = $installerUri
    installerSha256 = $actualInstallerSha256
    tessdataFastCommit = $tessdataCommit
    spaGitBlobSha1 = $languageManifest.spa.gitBlobSha1
    spaSha256 = $languageManifest.spa.sha256
    engGitBlobSha1 = $languageManifest.eng.gitBlobSha1
    engSha256 = $languageManifest.eng.sha256
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content -Path $runtimeManifestPath -Encoding utf8NoBOM

Write-Host "Staged Tesseract runtime files:"
Get-ChildItem $DestinationDirectory -File | Sort-Object Name | ForEach-Object {
    Write-Host "  $($_.Name) ($($_.Length) bytes)"
}
Write-Host "Tesseract $version staged: $stagedExe"

Remove-Item $extractDirectory -Recurse -Force -ErrorAction SilentlyContinue
