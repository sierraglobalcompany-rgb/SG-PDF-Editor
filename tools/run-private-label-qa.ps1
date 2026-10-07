param(
    [string]$Root = "tests/PrivateFixtures/labels"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
    Write-Host "PRIVATE_QA: NOT RUN — no private fixture directory"
    exit 2
}

$resolvedRoot = (Resolve-Path -LiteralPath $Root).Path
$previousRoot = [Environment]::GetEnvironmentVariable("SGPDF_PRIVATE_QA_ROOT", "Process")
$exitCode = 1

try {
    & "$PSScriptRoot/setup-labelize.ps1"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "PRIVATE_QA: FAIL"
        $exitCode = $LASTEXITCODE
    }
    else {
        [Environment]::SetEnvironmentVariable("SGPDF_PRIVATE_QA_ROOT", $resolvedRoot, "Process")

        & dotnet test "tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj" `
            --configuration Release `
            --filter "FullyQualifiedName~PrivateCorpus_FromEnvironment_MatchesExpectations"
        $exitCode = $LASTEXITCODE

        if ($exitCode -eq 0) {
            Write-Host "PRIVATE_QA: PASS"
        }
        else {
            Write-Host "PRIVATE_QA: FAIL"
        }
    }
}
finally {
    [Environment]::SetEnvironmentVariable("SGPDF_PRIVATE_QA_ROOT", $previousRoot, "Process")
}

exit $exitCode
