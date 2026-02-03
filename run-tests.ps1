#!/usr/bin/env pwsh
# Run Xbox Gamepad Tests

Write-Host "====================================" -ForegroundColor Cyan
Write-Host "Xbox Wireless Controller Tests" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan

# Build the project
Write-Host "`nBuilding project..." -ForegroundColor Yellow
dotnet build -c Release | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green

# Run tests
Write-Host "`nRunning tests...`n" -ForegroundColor Yellow
dotnet run --project xbox_gamepad.csproj --configuration Release -- --test

$testResult = $LASTEXITCODE

if ($testResult -eq 0) {
    Write-Host "`nAll tests passed!" -ForegroundColor Green
} else {
    Write-Host "`nSome tests failed!" -ForegroundColor Red
}

exit $testResult
