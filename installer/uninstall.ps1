# SqlFM uninstall helper - pure ASCII to avoid encoding issues
# Removes SqlFM extension via VSIXInstaller, registry and local files.

param(
    [switch]$Quiet,
    [string]$VsixInstaller
)

$extensionId = 'SqlFM.B4AB3D7A-F5E7-485D-A68E-F9037042028C'
$localDir = Join-Path $env:LOCALAPPDATA 'Programs\SqlFM'
$regPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\SqlFM'

function Log($msg) {
    if (-not $Quiet) { Write-Host $msg }
}

# 0) Warn if SSMS is running (locked files cannot be removed)
$ssmsRunning = Get-Process -Name 'Ssms' -ErrorAction SilentlyContinue
if ($ssmsRunning -and -not $Quiet) {
    Write-Host "WARNING: SSMS (Ssms.exe) is running. Close it before uninstall to avoid locked-file leftovers." -ForegroundColor Yellow
}

function Find-VsixInstaller {
    if ($VsixInstaller -and (Test-Path $VsixInstaller)) { return $VsixInstaller }

    # Registry: SSMS 22/21/20/19
    $regRoots = @(
        'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server Management Studio\22.0',
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server Management Studio\22.0',
        'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server Management Studio\21.0',
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server Management Studio\21.0',
        'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server Management Studio\20.0',
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server Management Studio\20.0',
        'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server Management Studio\19.0',
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server Management Studio\19.0'
    )
    foreach ($r in $regRoots) {
        if (Test-Path $r) {
            $dir = (Get-ItemProperty $r -ErrorAction SilentlyContinue).InstallDir
            if (-not $dir) { $dir = (Get-ItemProperty $r -ErrorAction SilentlyContinue).Path }
            if ($dir) {
                foreach ($sub in @('Common7\IDE\VSIXInstaller.exe', 'Release\VSIXInstaller.exe', 'IDE\VSIXInstaller.exe')) {
                    $p = Join-Path $dir $sub
                    if (Test-Path $p) { return $p }
                }
            }
        }
    }

    # Visual Studio 2022
    $vs = @(
        'HKLM:\SOFTWARE\Microsoft\VisualStudio\SxS\VS7'
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\SxS\VS7'
    )
    foreach ($r in $vs) {
        if (Test-Path $r) {
            $dir = (Get-ItemProperty $r -ErrorAction SilentlyContinue).'17.0'
            if ($dir) {
                $p = Join-Path $dir 'Common7\IDE\VSIXInstaller.exe'
                if (Test-Path $p) { return $p }
            }
        }
    }

    # File system scan under Program Files
    $roots = @()
    if ($env:ProgramFiles) { $roots += $env:ProgramFiles }
    if (${env:ProgramFiles(x86)}) { $roots += ${env:ProgramFiles(x86)} }
    foreach ($root in $roots) {
        if (-not (Test-Path $root)) { continue }
        $candidates = Get-ChildItem -Path $root -Filter 'Microsoft SQL Server Management Studio *' -Directory -ErrorAction SilentlyContinue
        foreach ($c in $candidates) {
            foreach ($sub in @('Common7\IDE\VSIXInstaller.exe', 'Release\VSIXInstaller.exe', 'IDE\VSIXInstaller.exe')) {
                $p = Join-Path $c.FullName $sub
                if (Test-Path $p) { return $p }
            }
        }
    }

    return $null
}

# 1) Try persistent installer if present
$setup = Join-Path $localDir 'SqlFMSetup.exe'
if (Test-Path $setup) {
    Log "Found persistent installer: $setup"
    & $setup /uninstall /quiet
    if ($LASTEXITCODE -eq 0) { Log "Uninstalled via persistent installer." }
}

# 2) Direct VSIX uninstall
$vsix = Find-VsixInstaller
if ($vsix) {
    Log "Using VSIXInstaller: $vsix"
    & $vsix /uninstall:$extensionId /quiet
    Log "VSIXInstaller exit code: $LASTEXITCODE"
} else {
    Log "WARNING: VSIXInstaller.exe not found. Extension may still be present in SSMS."
}

# 3) Remove local persistent directory
if (Test-Path $localDir) {
    Remove-Item -Path $localDir -Recurse -Force -ErrorAction SilentlyContinue
    Log "Removed: $localDir"
}

# 3.5) Remove user roaming config (%AppData%\SqlFM: custom styles + settings.xml)
$appDataDir = Join-Path $env:APPDATA 'SqlFM'
if (Test-Path $appDataDir) {
    Remove-Item -Path $appDataDir -Recurse -Force -ErrorAction SilentlyContinue
    Log "Removed user config: $appDataDir"
}

# 4) Remove registry uninstall key
if (Test-Path $regPath) {
    Remove-Item -Path $regPath -Recurse -Force -ErrorAction SilentlyContinue
    Log "Removed registry: $regPath"
}

Log "SqlFM cleanup finished."
