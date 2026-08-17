# SqlFM Full Diagnostic Script for SSMS 22 + SQL Server 2008 R2 coexistence
# Run as Administrator in PowerShell
# Usage: .\diagnose_full.ps1

$ErrorActionPreference = "Continue"
Write-Host "=== SqlFM Full Environment Diagnostic ===" -ForegroundColor Cyan
Write-Host "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host ""

# ── 1. All SSMS installations ──
Write-Host "─── 1. SSMS / SQL Server Installations ───" -ForegroundColor Yellow

# SSMS 17+ (standalone) installations
$ssmsPaths = @(
    "${env:ProgramFiles}\Microsoft SQL Server Management Studio",
    "${env:ProgramFiles(x86)}\Microsoft SQL Server Management Studio"
)
foreach ($root in $ssmsPaths) {
    if (Test-Path $root) {
        Get-ChildItem $root -Directory | ForEach-Object {
            $verDir = $_.FullName
            $exe = Join-Path $verDir "Common7\IDE\Ssms.exe"
            if (Test-Path $exe) {
                $verInfo = (Get-Item $exe).VersionInfo
                Write-Host "[STANDALONE SSMS] $verDir" -ForegroundColor Green
                Write-Host "  Ssms.exe version: $($verInfo.FileVersion)"
                Write-Host "  Product version: $($verInfo.ProductVersion)"
            }
            # Also check for older layout
            $exe2 = Join-Path $verDir "IDE\Ssms.exe"
            if (Test-Path $exe2) {
                $verInfo2 = (Get-Item $exe2).VersionInfo
                Write-Host "[LEGACY LAYOUT] $verDir (IDE\Ssms.exe)" -ForegroundColor Green
                Write-Host "  Version: $($verInfo2.FileVersion)"
            }
        }
    }
}

# SQL Server (engine) installed instances
Write-Host ""
Write-Host "[SQL Server Engine Instances] (registry):" -ForegroundColor Yellow
$instanceRegPaths = @(
    "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server",
    "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL"
)
foreach ($rp in $instanceRegPaths) {
    if (Test-Path $rp) {
        Get-ItemProperty $rp -ErrorAction SilentlyContinue | Format-List
        Get-ChildItem $rp -ErrorAction SilentlyContinue | ForEach-Object {
            Write-Host "  $($_.PSPath) = $((Get-ItemProperty $_.PSPath -ErrorAction SilentlyContinue).'(default)')"
        }
    }
}

# Check for SQL Server 2008 R2 specifically
Write-Host ""
$sql08Paths = @(
    "${env:ProgramFiles}\Microsoft SQL Server",
    "${env:ProgramFiles(x86)}\Microsoft SQL Server"
)
foreach ($root in $sql08Paths) {
    if (Test-Path $root) {
        Get-ChildItem $root -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -match 'MSSQL|10\.|11\.|12\.' } | ForEach-Object {
            Write-Host "[SQL SERVER DIR] $($_.FullName)" -ForegroundColor Magenta
            # Check for old SSMS inside SQL Server installation
            $oldBinn = Join-Path $_.FullName "Tools\Binn\VSShell\Common7\IDE\Ssms.exe"
            $oldBinn2 = Join-Path $_.FullName "Tools\Binn\VSShell\IDE\Ssms.exe"
            $oldBinn3 = Join-Path $_.FullName "90\Tools\Binn\VSShell\Common7\IDE\Ssms.exe"
            $oldBinn4 = Join-Path $_.FullName "100\Tools\Binn\VSShell\Common7\IDE\Ssms.exe"
            $oldBinn5 = Join-Path $_.FullName "110\Tools\Binn\VSShell\Common7\IDE\Ssms.exe"
            foreach ($oe in @($oldBinn, $oldBinn2, $oldBinn3, $oldBinn4, $oldBinn5)) {
                if (Test-Path $oe) {
                    Write-Host "  [OLD SSMS EXE FOUND] $oe" -ForegroundColor Red
                    Write-Host "    Version: $((Get-Item $oe).VersionInfo.FileVersion)"
                }
            }
        }
    }
}

# ── 2. ALL SqlFM extension locations ──
Write-Host ""
Write-Host "─── 2. SqlFM Extension Locations ───" -ForegroundColor Yellow

# VSIX installer shared extensions (all versions)
$sharedExt = "${env:LocalAppData}\Microsoft\VisualStudio"
if (Test-Path $sharedExt) {
    Get-ChildItem $sharedExt -Directory -ErrorAction SilentlyContinue | ForEach-Object {
        $extDir = Join-Path $_.FullName "Extensions"
        if (Test-Path $extDir) {
            Get-ChildItem $extDir -Recurse -Filter "SqlFM*" -ErrorAction SilentlyContinue | ForEach-Object {
                Write-Host "[VS EXTENSION] $($_.FullName)" -ForegroundColor Green
                Write-Host "  Size: $($_Length) bytes, Modified: $($_.LastWriteTime)"
            }
        }
        # Also check for extensions.cache / extensions.configurationchanged
        $cacheFile = Join-Path $_.FullName "extensions.cache"
        $cfgChanged = Join-Path $_.FullName "extensions.configurationchanged"
        if (Test-Path $cacheFile) { Write-Host "  [CACHE] $cacheFile ($((Get-Item $cacheFile).Length) bytes)" }
        if (Test-Path $cfgChanged) { Write-Host "  [CONFIG CHANGED] $cfgChanged ($((Get-Item $cfgChanged).Length) bytes)" -ForegroundColor Red }
    }
}

# SSMS-specific extension locations
$ssmsExtBase = "${env:LocalAppData}\Microsoft\SSMS"
if (Test-Path $ssmsExtBase) {
    Get-ChildItem $ssmsExtBase -Directory -ErrorAction SilentlyContinue | ForEach-Object {
        $extDir = Join-Path $_.FullName "Extensions"
        if (Test-Path $extDir) {
            Get-ChildItem $extDir -Recurse -Filter "SqlFM*" -ErrorAction SilentlyContinue | ForEach-Object {
                Write-Host "[SSMS EXTENSION] $($_.FullName)" -ForegroundColor Green
                Write-Host "  Size: $($_.Length) bytes, Modified: $($_.LastWriteTime)"
            }
        }
        $cfgChanged = Join-Path $_.FullName "extensions.configurationchanged"
        if (Test-Path $cfgChanged) { Write-Host "  [SSMS CONFIG CHANGED] $cfgChanged" -ForegroundColor Red }
    }
}

# Old SQL Server VS extension locations
$oldVsExt = "${env:AppData}\Microsoft\VisualStudio"
if (Test-Path $oldVsExt) {
    Get-ChildItem $oldVsExt -Directory -ErrorAction SilentlyContinue | ForEach-Object {
        $extDir = Join-Path $_.FullName "Extensions"
        if (Test-Path $extDir) {
            $sqlfm = Get-ChildItem $extDir -Recurse -Filter "SqlFM*" -ErrorAction SilentlyContinue
            if ($sqlfm) {
                $sqlfm | ForEach-Object { Write-Host "[OLD VS EXT Roaming] $($_.FullName)" -ForegroundColor Magenta }
            }
        }
    }
}

# ── 3. Registry: Uninstall keys & VS extensions ──
Write-Host ""
Write-Host "─── 3. Registry Entries ───" -ForegroundColor Yellow

# Add/Remove Programs
$uninstallPaths = @(
    "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
    "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
    "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"
)
foreach ($regPath in $uninstallPaths) {
    if (Test-Path $regPath) {
        Get-ChildItem $regPath -ErrorAction SilentlyContinue | ForEach-Object {
            $props = Get-ItemProperty $_.PSPath -ErrorAction SilentlyContinue
            if ($props.DisplayName -and ($props.DisplayName -match "SqlFM|sqlfm|SSMSFormatter")) {
                Write-Host "[UNINSTALL KEY] $($_.PSChildName)" -ForegroundColor Green
                Write-Host "  DisplayName: $($props.DisplayName)"
                Write-Host "  InstallLocation: $($props.InstallLocation)"
                Write-Host "  UninstallString: $($props.UninstallString)"
                Write-Host "  Version: $($props.DisplayVersion)"
            }
        }
    }
}

# VS Extensions registry
$vsExtReg = @(
    "HKCU:\SOFTWARE\Microsoft\VisualStudio\17.0_*_Exp3_Config\InstalledExtensions",
    "HKCU:\SOFTWARE\Microsoft\VisualStudio\*\InstalledExtensions",
    "HKCU:\SOFTWARE\Microsoft\VisualStudio\*\Configuration"
)
Write-Host ""
Write-Host "[VS REGISTRY EXTENSIONS] (searching for SqlFM):"
$vsBase = "HKCU:\SOFTWARE\Microsoft\VisualStudio"
if (Test-Path $vsBase) {
    Get-ChildItem $vsBase -ErrorAction SilentlyContinue | ForEach-Object {
        # Check InstalledExtensions subkey
        $instExt = Join-Path $_.PSPath "InstalledExtensions"
        if (Test-Path $instExt) {
            Get-ChildItem $instExt -ErrorAction SilentlyContinue | Where-Object { $_.PSChildName -match "SqlFM" } | ForEach-Object {
                Write-Host "  [INSTALLED EXT] $($_.PSPath)" -ForegroundColor Green
            }
        }
        # Check Configuration subkey for disabled extensions
        $configKey = Join-Path $_.PSPath "Configuration"
        if (Test-Path $configKey) {
            # DisabledExtensions value might contain SqlFM
            $disabled = (Get-ItemProperty $configKey -ErrorAction SilentlyContinue).DisabledExtensions
            if ($disabled -and $disabled -match "SqlFM") {
                Write-Host "  [DISABLED in Configuration] $($_.PSPath)" -ForegroundColor Red
                Write-Host "    DisabledExtensions contains: $disabled"
            }
        }
    }
}

# ── 4. GAC check for SqlFM assemblies ──
Write-Host ""
Write-Host "─── 4. Global Assembly Cache (GAC) ───" -ForegroundColor Yellow
$gacPath = "${env:WINDOWS}\assembly"
if (Test-Path $gacPath) {
    $gacSqlFm = Get-ChildItem $gacPath -Recurse -Directory -Filter "SqlFM*" -ErrorAction SilentlyContinue
    if ($gacSqlFm) {
        $gacSqlFm | ForEach-Object { Write-Host "[GAC] $($_.FullName)" -ForegroundColor Red }
    } else {
        Write-Host "  No SqlFM assemblies found in GAC (good)"
    }
}
$gacMsil = "${env:WINDOWS}\Microsoft.NET\assembly\GAC_MSIL"
if (Test-Path $gacMsil) {
    $gac4 = Get-ChildItem $gacMsil -Directory -Filter "SqlFM*" -ErrorAction SilentlyContinue
    if ($gac4) {
        $gac4 | ForEach-Object { Write-Host "[GAC_MSIL] $($_.FullName)" -ForegroundColor Red }
    } else {
        Write-Host "  No SqlFM in GAC_MSIL (good)"
    }
}

# ── 5. Running processes ──
Write-Host ""
Write-Host "─── 5. Running SQL/SSMS Processes ───" -ForegroundColor Yellow
@("Ssms", "SqlFM", "devenv") | ForEach-Object {
    $procs = Get-Process -Name $_ -ErrorAction SilentlyContinue
    if ($procs) {
        $procs | ForEach-Object {
            Write-Host "  [$($_.ProcessName)] PID=$($_.Id), Path=$($_.Path)"
        }
    } else {
        Write-Host "  No $_ process running"
    }
}

# ── 6. VSIXInstaller availability ──
Write-Host ""
Write-Host "─── 6. VSIXInstaller Locations ───" -ForegroundColor Yellow
$vsixSearch = @(
    "${env:ProgramFiles}\Microsoft Visual Studio\2022",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer",
    "${env:ProgramFiles}\Microsoft SQL Server Management Studio"
)
foreach ($root in $vsixSearch) {
    if (Test-Path $root) {
        Get-ChildItem $root -Recurse -Filter "VSIXInstaller.exe" -ErrorAction SilentlyContinue | ForEach-Object {
            Write-Host "  [VSIXInstaller] $($_.FullName)"
        }
    }
}

# ── Summary ──
Write-Host ""
Write-Host "=== Diagnostic Complete ===" -ForegroundColor Cyan
Write-Host "Please share this output to help identify the conflict."
Write-Host ""
Write-Host "If you see BOTH SSMS 22 AND SQL Server 2008 R2 paths above," -ForegroundColor White
Write-Host "that confirms the mixed-environment hypothesis." -ForegroundColor White
