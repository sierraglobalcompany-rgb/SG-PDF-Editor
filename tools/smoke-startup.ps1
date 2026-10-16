param(
    [Parameter(Mandatory=$true)]
    [string]$Executable,

    [int]$TimeoutSeconds = 30
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $Executable -PathType Leaf)) {
    throw "Executable not found: $Executable"
}
$exe = (Resolve-Path -LiteralPath $Executable).Path
$evidence = Join-Path $env:TEMP ("SGPdf-startup-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $evidence | Out-Null
$stdout = Join-Path $evidence 'stdout.txt'
$stderr = Join-Path $evidence 'stderr.txt'
Write-Host "Timestamp: $((Get-Date).ToString('o'))"
Write-Host "Evidence: $evidence"

$process = Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe) `
    -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
try {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while (-not $process.HasExited -and (Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 250
        $process.Refresh()
    }
    $process.Refresh()
    if ($process.HasExited) {
        Write-Host "FAIL: unexpected exit; ExitCode=$($process.ExitCode)"
        Get-Content -LiteralPath $stderr | Out-Host
        exit 1
    }
    Write-Host "PASS: process survived $TimeoutSeconds seconds."
}
finally {
    if (-not $process.HasExited) {
        $closed = $process.CloseMainWindow()
        if (-not $closed -or -not $process.WaitForExit(3000)) {
            $process.Kill()
            $process.WaitForExit()
        }
    }
    $process.Dispose()
}
exit 0
