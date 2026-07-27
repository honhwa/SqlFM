# Locate SqlFM extension folders on this machine
$extensionId = 'B4AB3D7A-F5E7-485D-A68E-F9037042028C'
$found = [System.Collections.ArrayList]::new()

function Add-Path($p) {
    if ($p -and (Test-Path $p) -and ($script:found -notcontains $p)) {
        $script:found.Add($p) | Out-Null
    }
}

# 1) Targeted name-match scan under known SSMS/VS roots
$roots = @()
if ($env:ProgramFiles) { $roots += $env:ProgramFiles }
if (${env:ProgramFiles(x86)}) { $roots += ${env:ProgramFiles(x86)} }
if ($env:LOCALAPPDATA) { $roots += $env:LOCALAPPDATA }
if ($env:APPDATA) { $roots += $env:APPDATA }

$basePatterns = @(
    'Microsoft SQL Server Management Studio 22\Common7\IDE',
    'Microsoft SQL Server Management Studio 21\Common7\IDE',
    'Microsoft SQL Server Management Studio 20\Common7\IDE',
    'Microsoft SQL Server Management Studio 19\Common7\IDE',
    'Microsoft SQL Server Management Studio 18\Common7\IDE',
    'Microsoft Visual Studio\2022\Professional\Common7\IDE',
    'Microsoft Visual Studio\2022\Enterprise\Common7\IDE',
    'Microsoft Visual Studio\2022\Community\Common7\IDE',
    'Microsoft Visual Studio\2019\Professional\Common7\IDE',
    'Microsoft Visual Studio\2019\Enterprise\Common7\IDE',
    'Microsoft Visual Studio\2019\Community\Common7\IDE',
    'Microsoft\SQL Server Management Studio',
    'Microsoft\VisualStudio'
)

foreach ($root in $roots) {
    foreach ($bp in $basePatterns) {
        $full = Join-Path $root $bp
        if (-not (Test-Path $full)) { continue }
        Get-ChildItem -Path $full -Directory -Recurse -ErrorAction SilentlyContinue | Where-Object {
            $_.Name -like '*SqlFM*' -or $_.Name -like "*$extensionId*"
        } | ForEach-Object { Add-Path $_.FullName }
    }
}

# 2) File-marker scan for SqlFM.* files (deeper / odd paths)
$searchRoots = @()
if ($env:ProgramFiles) { $searchRoots += $env:ProgramFiles }
if (${env:ProgramFiles(x86)}) { $searchRoots += ${env:ProgramFiles(x86)} }
if ($env:LOCALAPPDATA) { $searchRoots += Join-Path $env:LOCALAPPDATA 'Microsoft' }

$markers = @('SqlFM.pkgdef', 'SqlFM.dll', 'SqlFM.Core.dll')
foreach ($sr in $searchRoots) {
    if (-not (Test-Path $sr)) { continue }
    foreach ($m in $markers) {
        Get-ChildItem -Path $sr -Filter $m -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
            Add-Path $_.DirectoryName
        }
    }
}

# 3) Fallback: any *SqlFM* named file under LOCALAPPDATA
if ($found.Count -eq 0 -and $env:LOCALAPPDATA) {
    Get-ChildItem -Path $env:LOCALAPPDATA -Filter '*SqlFM*' -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
        Add-Path $_.DirectoryName
    }
}

if ($found.Count -eq 0) {
    Write-Host 'No SqlFM extension folder found on this machine.' -ForegroundColor Red
    Write-Host 'If SSMS is installed, share its install path and we will scan it directly.' -ForegroundColor Yellow
} else {
    Write-Host "Found $($found.Count) SqlFM location(s):" -ForegroundColor Green
    $found | ForEach-Object { Write-Host $_ -ForegroundColor Yellow }
}
