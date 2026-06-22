# Runs backend tests with Coverlet and opens an HTML coverage report.
# Output: backend/TestResults/ (gitignored)
#
# Pasta: raiz do repo
#   powershell -ExecutionPolicy Bypass -File scripts\coverage-backend.ps1
$ErrorActionPreference = "Stop"

$BackendRoot = (Resolve-Path (Join-Path (Join-Path $PSScriptRoot "..") "backend")).Path
$TestResults = Join-Path $BackendRoot "TestResults"
$ReportDir = Join-Path $TestResults "CoverageReport"

Push-Location $BackendRoot
try {
    Write-Host "Restoring dotnet tools (ReportGenerator)..." -ForegroundColor Cyan
    dotnet tool restore

    if (Test-Path $TestResults) {
        Write-Host "Cleaning previous TestResults..." -ForegroundColor DarkGray
        Remove-Item -Recurse -Force $TestResults
    }

    Write-Host "Running tests with coverage..." -ForegroundColor Cyan
    dotnet test OnlinePortfolio.Api.slnx `
        --collect:"XPlat Code Coverage" `
        --results-directory $TestResults

    $coverageFiles = Get-ChildItem -Path $TestResults -Recurse -Filter "coverage.cobertura.xml"
    if (-not $coverageFiles) {
        throw "No coverage.cobertura.xml found under $TestResults"
    }

    $reports = ($coverageFiles | ForEach-Object { $_.FullName }) -join ";"

    Write-Host "Generating HTML report..." -ForegroundColor Cyan
    dotnet reportgenerator `
        "-reports:$reports" `
        "-targetdir:$ReportDir" `
        "-reporttypes:Html"

    $indexPath = Join-Path $ReportDir "index.html"
    Write-Host ""
    Write-Host "Coverage report: $indexPath" -ForegroundColor Green
    Write-Host "TestResults/ is gitignored and is not committed to GitHub." -ForegroundColor DarkGray

    if ($IsWindows -or ($env:OS -match "Windows")) {
        Start-Process $indexPath
    }
}
finally {
    Pop-Location
}
