# System Sweep - Publish Script
# Creates a single-file release executable

param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release"
)

Write-Host "=== System Sweep Publisher ===" -ForegroundColor Cyan
Write-Host "Building $Configuration single-file executable..." -ForegroundColor Yellow

# Clean old build
dotnet clean cleaner1/cleaner1.csproj -c $Configuration -q

# Build and publish single-file
dotnet publish cleaner1/cleaner1.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=embedded `
    -o ./publish

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Copy version.json alongside the exe
Copy-Item version.json ./publish/version.json -Force

Write-Host ""
Write-Host "=== Build Successful! ===" -ForegroundColor Green
Write-Host "Output: ./publish/SystemSweep.exe" -ForegroundColor Green
Write-Host "Size: " -NoNewline
$file = Get-Item ./publish/SystemSweep.exe
Write-Host "$([math]::Round($file.Length / 1MB, 1)) MB" -ForegroundColor Yellow

# Create release zip
$zipPath = "./publish/SystemSweep-v2.0.0.zip"
Compress-Archive -Path ./publish/* -DestinationPath $zipPath -Force
Write-Host "Zip: $zipPath" -ForegroundColor Green

Write-Host ""
Write-Host "NOTE: Run as Administrator for full functionality!" -ForegroundColor Magenta
