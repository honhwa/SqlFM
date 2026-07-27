# Clean SSMS 22 extension registry caches after manually deleting an extension folder.
# This removes leftover references so SSMS stops trying to load SqlFM.

param([switch]$ResetAllSettings)

$extensionGuid = 'B4AB3D7A-F5E7-485D-A68E-F9037042028C'

function Backup-Remove($p) {
    if (Test-Path $p) {
        $backup = "$p.old"
        Move-Item -Path $p -Destination $backup -Force -ErrorAction SilentlyContinue
        Write-Host "Backed up and removed: $p -> $backup"
    }
}

$localSsmsRoot = Join-Path $env:LOCALAPPDATA 'Microsoft\SSMS'
$roamingSsmsRoot = Join-Path $env:APPDATA 'Microsoft\SSMS'

if (-not (Test-Path $localSsmsRoot)) {
    Write-Host "No SSMS local cache found at $localSsmsRoot" -ForegroundColor Yellow
    return
}

Get-ChildItem -Path $localSsmsRoot -Directory -ErrorAction SilentlyContinue | ForEach-Object {
    $base = $_.FullName

    # 1) Remove any leftover SqlFM-named files/folders
    Get-ChildItem -Path $base -Recurse -ErrorAction SilentlyContinue | Where-Object {
        $_.Name -like '*SqlFM*' -or $_.Name -like "*$extensionGuid*"
    } | ForEach-Object {
        Remove-Item -Path $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "Removed leftover: $($_.FullName)"
    }

    # 2) Reset extension registry caches (SSMS will rebuild these on next launch)
    Backup-Remove (Join-Path $base 'privateregistry.bin')
    Backup-Remove (Join-Path $base 'ApplicationPrivateSettings')
    Backup-Remove (Join-Path $base 'ActivityLog.xml')
}

# 3) Optionally reset roaming user settings (window layout, toolbars, etc.)
if ($ResetAllSettings -and (Test-Path $roamingSsmsRoot)) {
    Get-ChildItem -Path $roamingSsmsRoot -Directory -ErrorAction SilentlyContinue | ForEach-Object {
        $base = $_.FullName
        Backup-Remove (Join-Path $base 'user.vsk')
        Backup-Remove (Join-Path $base 'devenv.vsk')
    }
}

Write-Host ''
Write-Host 'Done. Close all SSMS instances and restart SSMS 22 to rebuild caches.' -ForegroundColor Green
Write-Host 'Your SSMS window layout/toolbars may reset to default on next start.' -ForegroundColor Yellow
