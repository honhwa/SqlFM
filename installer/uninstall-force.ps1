# SqlFM force uninstall - removes extension by any means available
# Works even when VSIXInstaller.exe is missing from standard locations.

param(
    [switch]$Quiet,
    [string]$VsixInstaller
)

$extensionId = 'SqlFM.B4AB3D7A-F5E7-485D-A68E-F9037042028C'
$extensionGuid = 'B4AB3D7A-F5E7-485D-A68E-F9037042028C'
$localDir = Join-Path $env:LOCALAPPDATA 'Programs\SqlFM'
$regPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\SqlFM'

function Log($msg) { if (-not $Quiet) { Write-Host $msg } }

function Find-VsixInstaller {
    if ($VsixInstaller -and (Test-Path $VsixInstaller)) { return $VsixInstaller }

    $candidates = @()

    # Registry-based SSMS/VS locations
    $keys = @(
        @{Path='HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server Management Studio\22.0'; Props=@('InstallDir','Path')},
        @{Path='HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server Management Studio\22.0'; Props=@('InstallDir','Path')},
        @{Path='HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server Management Studio\21.0'; Props=@('InstallDir','Path')},
        @{Path='HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server Management Studio\21.0'; Props=@('InstallDir','Path')},
        @{Path='HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server Management Studio\20.0'; Props=@('InstallDir','Path')},
        @{Path='HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server Management Studio\20.0'; Props=@('InstallDir','Path')},
        @{Path='HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server Management Studio\19.0'; Props=@('InstallDir','Path')},
        @{Path='HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server Management Studio\19.0'; Props=@('InstallDir','Path')},
        @{Path='HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server Management Studio\18.0'; Props=@('InstallDir','Path')},
        @{Path='HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server Management Studio\18.0'; Props=@('InstallDir','Path')},
        @{Path='HKLM:\SOFTWARE\Microsoft\VisualStudio\SxS\VS7'; Props=@('17.0','16.0','15.0')},
        @{Path='HKLM:\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\SxS\VS7'; Props=@('17.0','16.0','15.0')}
    )
    foreach ($k in $keys) {
        if (Test-Path $k.Path) {
            $props = Get-ItemProperty $k.Path -ErrorAction SilentlyContinue
            foreach ($p in $k.Props) {
                $dir = $props.$p
                if ($dir) {
                    $candidates += $dir
                    $candidates += (Join-Path $dir 'Common7\IDE')
                    $candidates += (Join-Path $dir 'Release')
                }
            }
        }
    }

    # Common file system guesses
    $roots = @()
    if ($env:ProgramFiles) { $roots += $env:ProgramFiles }
    if (${env:ProgramFiles(x86)}) { $roots += ${env:ProgramFiles(x86)} }
    foreach ($root in $roots) {
        if (-not (Test-Path $root)) { continue }
        Get-ChildItem -Path $root -Filter 'Microsoft SQL Server Management Studio *' -Directory -ErrorAction SilentlyContinue | ForEach-Object { $candidates += $_.FullName }
        Get-ChildItem -Path $root -Filter 'Microsoft Visual Studio*' -Directory -ErrorAction SilentlyContinue | ForEach-Object { $candidates += $_.FullName }
    }

    $subPaths = @('VSIXInstaller.exe', 'Common7\IDE\VSIXInstaller.exe', 'Release\VSIXInstaller.exe', 'IDE\VSIXInstaller.exe')
    foreach ($base in ($candidates | Select-Object -Unique)) {
        foreach ($sub in $subPaths) {
            $p = Join-Path $base $sub
            if (Test-Path $p) { return $p }
        }
    }

    return $null
}

function Get-ExtensionFolders {
    $folders = @()
    $progFiles = @()
    if ($env:ProgramFiles) { $progFiles += $env:ProgramFiles }
    if (${env:ProgramFiles(x86)}) { $progFiles += ${env:ProgramFiles(x86)} }

    # Fixed paths
    foreach ($pf in $progFiles) {
        $folders += Join-Path $pf 'Microsoft SQL Server Management Studio 22\Common7\IDE\Extensions'
        $folders += Join-Path $pf 'Microsoft SQL Server Management Studio 22\Common7\IDE\Extensions2'
        $folders += Join-Path $pf 'Microsoft SQL Server Management Studio 21\Common7\IDE\Extensions'
        $folders += Join-Path $pf 'Microsoft SQL Server Management Studio 20\Common7\IDE\Extensions'
        $folders += Join-Path $pf 'Microsoft SQL Server Management Studio 19\Common7\IDE\Extensions'
        $folders += Join-Path $pf 'Microsoft SQL Server Management Studio 18\Common7\IDE\Extensions'
    }

    $local = $env:LOCALAPPDATA
    if ($local) {
        $base = Join-Path $local 'Microsoft\SQL Server Management Studio'
        if (Test-Path $base) {
            Get-ChildItem -Path $base -Directory -ErrorAction SilentlyContinue | ForEach-Object {
                $folders += Join-Path $_.FullName 'Extensions'
            }
        }
        $base2 = Join-Path $local 'Microsoft\VisualStudio'
        if (Test-Path $base2) {
            Get-ChildItem -Path $base2 -Directory -ErrorAction SilentlyContinue | ForEach-Object {
                $folders += Join-Path $_.FullName 'Extensions'
            }
        }
    }

    return $folders | Select-Object -Unique | Where-Object { Test-Path $_ }
}

function Find-ExtensionByPkgdef {
    $result = @()

    $searchRoots = @()
    if ($env:ProgramFiles) { $searchRoots += $env:ProgramFiles }
    if (${env:ProgramFiles(x86)}) { $searchRoots += ${env:ProgramFiles(x86)} }
    if ($env:LOCALAPPDATA) { $searchRoots += $env:LOCALAPPDATA }
    if ($env:APPDATA) { $searchRoots += $env:APPDATA }

    foreach ($root in $searchRoots) {
        if (-not (Test-Path $root)) { continue }
        # Look for SqlFM.pkgdef under known IDE / Extensions trees
        $subRoots = @(
            (Join-Path $root 'Microsoft SQL Server Management Studio 22'),
            (Join-Path $root 'Microsoft SQL Server Management Studio 21'),
            (Join-Path $root 'Microsoft SQL Server Management Studio 20'),
            (Join-Path $root 'Microsoft SQL Server Management Studio 19'),
            (Join-Path $root 'Microsoft SQL Server Management Studio 18'),
            (Join-Path $root 'Microsoft Visual Studio'),
            (Join-Path $root 'Microsoft\SQL Server Management Studio'),
            (Join-Path $root 'Microsoft\VisualStudio')
        )
        foreach ($sr in $subRoots) {
            if (-not (Test-Path $sr)) { continue }
            Get-ChildItem -Path $sr -Filter 'SqlFM.pkgdef' -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
                $result += $_.DirectoryName
            }
        }
        # Also look for SqlFM named files under Microsoft local appdata as fallback
        if ($root -eq $env:LOCALAPPDATA) {
            $ms = Join-Path $root 'Microsoft'
            if (Test-Path $ms) {
                Get-ChildItem -Path $ms -Filter 'SqlFM.pkgdef' -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
                    $result += $_.DirectoryName
                }
                Get-ChildItem -Path $ms -Filter 'SqlFM.dll' -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
                    $result += $_.DirectoryName
                }
            }
        }
    }

    return $result | Select-Object -Unique
}

function Remove-ExtensionFolders {
    $removed = $false

    # Method 1: fixed paths
    foreach ($folder in (Get-ExtensionFolders)) {
        Get-ChildItem -Path $folder -Directory -ErrorAction SilentlyContinue | Where-Object {
            $_.Name -like '*SqlFM*' -or $_.Name -like "*$extensionGuid*"
        } | ForEach-Object {
            Remove-Item -Path $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
            Log "Removed extension folder: $($_.FullName)"
            $removed = $true
        }
    }

    # Method 2: locate by SqlFM.pkgdef
    foreach ($folder in (Find-ExtensionByPkgdef)) {
        if (Test-Path $folder) {
            Remove-Item -Path $folder -Recurse -Force -ErrorAction SilentlyContinue
            Log "Removed pkgdef folder: $folder"
            $removed = $true
        }
    }

    return $removed
}

# --- main ---

# 1) Persistent installer
$setup = Join-Path $localDir 'SqlFMSetup.exe'
if (Test-Path $setup) {
    Log "Found persistent installer: $setup"
    & $setup /uninstall /quiet
}

# 2) VSIXInstaller
$vsix = Find-VsixInstaller
if ($vsix) {
    Log "VSIXInstaller found: $vsix"
    & $vsix /uninstall:$extensionId /quiet
    Log "VSIXInstaller exit code: $LASTEXITCODE"
} else {
    Log "VSIXInstaller.exe not found, will delete extension folders directly."
}

# 3) Extension folders (fallback / reinforcement)
$removed = Remove-ExtensionFolders
if ($removed) { Log "Extension folders cleaned." } else { Log "No SqlFM extension folder found." }

# 4) Local installer dir
if (Test-Path $localDir) {
    Remove-Item -Path $localDir -Recurse -Force -ErrorAction SilentlyContinue
    Log "Removed: $localDir"
}

# 5) Registry
if (Test-Path $regPath) {
    Remove-Item -Path $regPath -Recurse -Force -ErrorAction SilentlyContinue
    Log "Removed registry: $regPath"
}

Log "Done. Close all SSMS instances and restart SSMS to verify removal."
