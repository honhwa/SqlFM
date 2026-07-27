# build-installer.ps1
# SqlFM installer build script (ASCII only to avoid encoding issues)
# Usage: run from project root or installer\ directory
#
# Prerequisites:
#   1. .NET SDK (verified: dotnet 10 can build net48 on this machine)
#   2. (Optional) Inno Setup 6/7 to also produce SqlFMSetup_vX.X.X.exe
#
# Output:
#   output\SqlFMSetup.exe        <- primary package (self-contained, double-click to install, uninstallable from Windows Settings -> Apps)
#   output\SqlFMSetup_vX.X.X.exe <- (optional) Inno Setup package

$ErrorActionPreference = "Stop"
$ScriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot  = Split-Path -Parent $ScriptDir
$SolutionFile = Join-Path $ProjectRoot "SqlFM.sln"
$SetupProj    = Join-Path $ProjectRoot "setup\SqlFM.Setup.csproj"
$IssFile      = Join-Path $ScriptDir  "SqlFMSetup.iss"
$OutputDir    = Join-Path $ProjectRoot "output"

Write-Host ""
Write-Host "============================================" -ForegroundColor DarkCyan
Write-Host "  SqlFM Installer Build" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor DarkCyan
Write-Host ""

# ----------------------------------------------------------
# Step 1: Build main solution Release (produces SqlFM.vsix)
# ----------------------------------------------------------
Write-Host ">>> Step 1: Build main solution (Release)..." -ForegroundColor Cyan
if (-not (Test-Path $SolutionFile)) {
    Write-Host "ERROR: Solution not found: $SolutionFile" -ForegroundColor Red
    exit 1
}
dotnet build $SolutionFile --configuration Release --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed." -ForegroundColor Red
    exit 1
}

$VsixPath = Join-Path $ProjectRoot "src\SqlFM\bin\Release\net48\SqlFM.vsix"
if (-not (Test-Path $VsixPath)) {
    Write-Host "ERROR: VSIX not found: $VsixPath" -ForegroundColor Red
    exit 1
}
Write-Host "    VSIX: $VsixPath ($([math]::Round((Get-Item $VsixPath).Length/1KB,1)) KB)" -ForegroundColor Green

# ----------------------------------------------------------
# Step 2: Build self-contained installer (embeds the VSIX)
# ----------------------------------------------------------
Write-Host ">>> Step 2: Build self-contained SqlFMSetup.exe..." -ForegroundColor Cyan
if (-not (Test-Path $SetupProj)) {
    Write-Host "ERROR: Setup project not found: $SetupProj" -ForegroundColor Red
    exit 1
}
dotnet build $SetupProj --configuration Release --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Setup build failed." -ForegroundColor Red
    exit 1
}

$SetupExe = Join-Path $OutputDir "SqlFMSetup.exe"
if (-not (Test-Path $SetupExe)) {
    Write-Host "ERROR: Not generated: $SetupExe" -ForegroundColor Red
    exit 1
}
Remove-Item (Join-Path $OutputDir "SqlFMSetup.pdb") -ErrorAction SilentlyContinue
Remove-Item (Join-Path $OutputDir "SqlFMSetup.exe.config") -ErrorAction SilentlyContinue
Write-Host "    Generated: $SetupExe ($([math]::Round((Get-Item $SetupExe).Length/1MB,2)) MB)" -ForegroundColor Green

# ----------------------------------------------------------
# Step 3 (optional): If Inno Setup is present, also build SqlFMSetup_vX.X.X.exe
# ----------------------------------------------------------
$Pf86 = [Environment]::GetEnvironmentVariable("ProgramFiles(x86)")
$Pf   = [Environment]::GetEnvironmentVariable("ProgramFiles")
$IsccCandidates = @(
    (Join-Path $Pf86 "Inno Setup 6\ISCC.exe"),
    (Join-Path $Pf   "Inno Setup 6\ISCC.exe"),
    (Join-Path $Pf86 "Inno Setup 7\ISCC.exe"),
    (Join-Path $Pf   "Inno Setup 7\ISCC.exe")
)
$IsccPath = $IsccCandidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if ($IsccPath) {
    Write-Host ">>> Step 3: Inno Setup detected, building SqlFMSetup_vX.X.X.exe..." -ForegroundColor Cyan
    if (-not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null }
    & $IsccPath $IssFile
    if ($LASTEXITCODE -eq 0) {
        Write-Host "    Inno Setup package built." -ForegroundColor Green
    } else {
        Write-Host "    Inno Setup returned $LASTEXITCODE, skipped (primary package unaffected)." -ForegroundColor Yellow
    }
} else {
    Write-Host ">>> Step 3: Inno Setup not detected, skipped (primary package SqlFMSetup.exe is ready)." -ForegroundColor Gray
}

# ----------------------------------------------------------
# Done
# ----------------------------------------------------------
Write-Host ""
Write-Host "============================================" -ForegroundColor DarkGreen
Write-Host "  Build complete!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor DarkGreen
Write-Host ""
Write-Host "  Primary package: $SetupExe" -ForegroundColor Yellow
Write-Host "  Usage: copy to target machine, double-click to install (SSMS 22 required)." -ForegroundColor Cyan
Write-Host "  Uninstall: Windows Settings -> Apps -> SqlFM - T-SQL Formatter -> Uninstall." -ForegroundColor Cyan
Write-Host ""
