# Build and Run XvT Hydrospanner

Write-Host "XvT Hydrospanner - Build Script" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

$solutionPath = Join-Path $PSScriptRoot "XvTHydrospanner.sln"
$projectPath = Join-Path $PSScriptRoot "XvTHydrospanner\XvTHydrospanner.csproj"

# Check if dotnet is installed
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: .NET SDK not found!" -ForegroundColor Red
    Write-Host "Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

# Display .NET version
$dotnetVersion = dotnet --version
Write-Host ".NET SDK Version: $dotnetVersion" -ForegroundColor Green
Write-Host ""

# Restore packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore $solutionPath
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Package restore failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Packages restored successfully!" -ForegroundColor Green
Write-Host ""

# Build solution
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build $solutionPath --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host ""

# Ask if user wants to run
$runApp = Read-Host "Do you want to run the application now? (Y/N)"
if ($runApp -eq "Y" -or $runApp -eq "y") {
    Write-Host "Launching XvT Hydrospanner..." -ForegroundColor Cyan
    dotnet run --project $projectPath --configuration Release
}
else {
    Write-Host ""
    Write-Host "Build complete! To run the application manually, use:" -ForegroundColor Cyan
    Write-Host "  dotnet run --project $projectPath --configuration Release" -ForegroundColor White
    Write-Host ""
    Write-Host "Or navigate to:" -ForegroundColor Cyan
    Write-Host "  .\XvTHydrospanner\bin\Release\net8.0-windows\XvTHydrospanner.exe" -ForegroundColor White
}
