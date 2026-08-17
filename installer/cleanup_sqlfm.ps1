# cleanup_sqlfm.ps1
# Removes all leftover SqlFM extension files/folders left by the previous
# (broken) installer so the new fixed installer can take over cleanly.
# Run in PowerShell (no admin needed for user-level deployment).
$ErrorActionPreference = 'SilentlyContinue'

$guid    = 'B4AB3D7A-F5E7-485D-A68E-F9037042028C'
$extId   = 'SqlFM.B4AB3D7A-F5E7-485D-A68E-F9037042028C'

Write-Host 'Closing SSMS...'
Get-Process -Name Ssms -ErrorAction SilentlyContinue | ForEach-Object { $_.CloseMainWindow() | Out-Null }
Start-Sleep -Seconds 3
Get-Process -Name Ssms -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

$local   = [System.Environment]::GetFolderPath('LocalApplicationData')
$appData = [System.Environment]::GetFolderPath('ApplicationData')

# 1) Remove any SqlFM extension folders under SSMS / VisualStudio user extension roots
$roots = @(
    (Join-Path $local 'Microsoft\SSMS'),
    (Join-Path $local 'Microsoft\VisualStudio')
)
foreach ($root in $roots) {
    if (Test-Path $root) {
        Get-ChildItem -Path $root -Recurse -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -like '*SqlFM*' -or $_.Name -like "*$guid*" } |
            ForEach-Object {
                Write-Host "Removing: $($_.FullName)"
                Remove-Item $_.FullName -Recurse -Force
            }
    }
}

# 2) Remove VSIXInstaller cache entries for SqlFM
$vsixCache = Join-Path $local 'Microsoft\VSIXInstaller'
if (Test-Path $vsixCache) {
    Get-ChildItem -Path $vsixCache -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like '*SqlFM*' -or $_.Name -like "*$guid*" } |
        ForEach-Object {
            Write-Host "Removing cache: $($_.FullName)"
            Remove-Item $_.FullName -Recurse -Force
        }
}

# 3) Try the standard VSIXInstaller uninstall (ignores errors if not present)
$ssmsDirs = @(
    'C:\Program Files\Microsoft SQL Server Management Studio 22\Release\Common7\IDE',
    'C:\Program Files\Microsoft SQL Server Management Studio 21\Release\Common7\IDE',
    'C:\Program Files (x86)\Microsoft SQL Server Management Studio 20\Common7\IDE',
    'C:\Program Files (x86)\Microsoft SQL Server Management Studio 19\Common7\IDE',
    'C:\Program Files (x86)\Microsoft SQL Server Management Studio 18\Common7\IDE'
)
foreach ($d in $ssmsDirs) {
    $vsi = Join-Path $d 'VSIXInstaller.exe'
    if (Test-Path $vsi) {
        Write-Host "Running VSIXInstaller /uninstall on $vsi"
        & $vsi '/quiet' "/uninstall:$extId" | Out-Null
    }
}

# 5) Clear SSMS extension cache files (extensions.configurationchanged)
#    SSMS 22 uses these to track installed extensions; stale entries prevent reloading
Write-Host 'Clearing SSMS extension cache...'
$ssmsExtRoots = @(
    (Join-Path $local 'Microsoft\SSMS')
)
foreach ($root in $ssmsExtRoots) {
    if (Test-Path $root) {
        # Find all extensions.configurationchanged files under SSMS ext roots
        Get-ChildItem -Path $root -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -eq 'extensions.configurationchanged' } |
            ForEach-Object {
                Write-Host "Removing cache file: $($_.FullName)"
                Remove-Item $_.FullName -Force
            }
        # Also remove extension.catalogCache if present (another SSMS cache)
        Get-ChildItem -Path $root -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -eq 'extension.catalogCache' } |
            ForEach-Object {
                Write-Host "Removing catalog cache: $($_.FullName)"
                Remove-Item $_.FullName -Force
            }
    }
}

# 6) Remove the uninstall registry key written by our installer
$reg = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\SqlFM'
if (Test-Path $reg) {
    Write-Host "Removing registry key: $reg"
    Remove-Item $reg -Recurse -Force
}

# 5) (Optional) Remove user config - uncomment for a full wipe
# Remove-Item (Join-Path $appData 'SqlFM') -Recurse -Force

Write-Host 'Cleanup done.'
Write-Host ''
Write-Host 'IMPORTANT SSMS 22 NOTE:' -ForegroundColor Yellow
Write-Host '  After installing the new SqlFMSetup.exe and restarting SSMS:'
Write-Host '  If you still dont see SqlFM in the Tools menu or right-click context menu:'
Write-Host '  Go to Extensions -> Customize Menu... -> tick SqlFM -> Save & Restart'
Write-Host '  (This is a known SSMS 21/22 bug: extension menus are hidden by default)'
Write-Host ''
Write-Host 'Now restart SSMS, then run the new SqlFMSetup.exe to install.'
