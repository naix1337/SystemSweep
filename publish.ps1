# System Sweep — Secure Publishing Pipeline v2.0
# Builds, hardens, and packages the release executable

param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [switch]$Sign = $false,
    [string]$CertificatePath = ""
)

Write-Host @"
╔══════════════════════════════════════════════╗
║     SYSTEM SWEEP — SECURE PUBLISH v2.0      ║
╚══════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

# ===== Step 1: Clean =====
Write-Host "`n[1/5] Cleaning..." -ForegroundColor Yellow
dotnet clean cleaner1/cleaner1.csproj -c $Configuration -q 2>$null

# ===== Step 2: Publish with hardening flags =====
Write-Host "[2/5] Publishing hardened build..." -ForegroundColor Yellow
dotnet publish cleaner1/cleaner1.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -p:EnableUnsafeBinaryFormatterSerialization=false `
    -p:SuppressTfmSupportBuildWarnings=true `
    -o ./publish 2>&1 | Out-Null

if ($LASTEXITCODE -ne 0) { Write-Host "❌ Build failed!" -ForegroundColor Red; exit 1 }

$exePath = "./publish/SystemSweep.exe"
$file = Get-Item $exePath
Write-Host "   ✅ Built: $([math]::Round($file.Length / 1MB, 1)) MB" -ForegroundColor Green

# ===== Step 3: Remove unnecessary files =====
Write-Host "[3/5] Removing metadata artifacts..." -ForegroundColor Yellow
Get-ChildItem ./publish -Recurse -Include *.pdb,*.xml,*.config,*.json -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -ne "version.json" } | Remove-Item -Force -ErrorAction SilentlyContinue
Write-Host "   ✅ Cleaned" -ForegroundColor Green

# ===== Step 4: Anti-tamper integrity check =====
Write-Host "[4/5] Computing integrity hash..." -ForegroundColor Yellow
$hash = Get-FileHash $exePath -Algorithm SHA256
$hashFile = "./publish/integrity.sha256"
"$($hash.Hash)  SystemSweep.exe" | Out-File -FilePath $hashFile -Encoding ASCII
Write-Host "   ✅ SHA256: $($hash.Hash.Substring(0, 16))..." -ForegroundColor Green

# Copy version.json
Copy-Item version.json ./publish/version.json -Force

# ===== Step 5: Package =====
Write-Host "[5/5] Creating release package..." -ForegroundColor Yellow
$zipPath = "./publish/SystemSweep-v2.0.0.7z"
if (Get-Command "7z" -ErrorAction SilentlyContinue) {
    7z a -tzip "$zipPath" ./publish/* -mx=9 -x!*.7z | Out-Null
} else {
    Compress-Archive -Path ./publish/* -DestinationPath "./publish/temp.zip" -Force
    Move-Item -Force "./publish/temp.zip" $zipPath
}
Write-Host "   ✅ Package: $zipPath" -ForegroundColor Green

Write-Host @"

╔══════════════════════════════════════════════╗
║       ✅ SECURE BUILD COMPLETE              ║
╠══════════════════════════════════════════════╣
║  $($file.Name)        ║
║  Size: $([math]::Round($file.Length / 1MB, 1)) MB                         ║
║  Debug: NONE                               ║
║  Integrity: SHA256                          ║
╠══════════════════════════════════════════════╣
║  RECOMMENDED POST-BUILD STEPS:             ║
║  1. Run ConfuserEx obfuscator              ║
║  2. Code-sign with Authenticode             ║
║  3. VirusTotal scan before distribution     ║
╚══════════════════════════════════════════════╝
"@ -ForegroundColor Cyan
