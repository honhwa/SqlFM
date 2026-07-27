#Requires -Version 3.0
<#
.SYNOPSIS
    查找本机 VSIXInstaller.exe 的候选路径（用于 SqlFM 安装包）。
.DESCRIPTION
    搜索注册表、Program Files、所有可用盘符根目录，输出找到的 VSIXInstaller.exe 路径。
    找到后可直接复制路径，命令行安装：
        SqlFMSetup.exe /vsixinstaller:"<路径>"
#>

$found = @()

function Test-AndReport($path) {
    if ($path -and (Test-Path $path)) {
        $script:found += $path
        Write-Host "[FOUND] $path"
    }
}

# 1) 从注册表读 SSMS 安装目录
$regPaths = @(
    'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server Management Studio\22.0',
    'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server Management Studio\22.0',
    'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server Management Studio\21.0',
    'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server Management Studio\21.0',
    'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server Management Studio\20.0',
    'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server Management Studio\20.0',
    'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server Management Studio\19.0',
    'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server Management Studio\19.0'
)
foreach ($rp in $regPaths) {
    $key = Get-ItemProperty -Path $rp -ErrorAction SilentlyContinue
    if ($key) {
        $dir = $key.InstallDir
        if (-not $dir) { $dir = $key.Path }
        if ($dir) {
            Test-AndReport (Join-Path $dir 'Common7\IDE\VSIXInstaller.exe')
            Test-AndReport (Join-Path $dir 'Release\VSIXInstaller.exe')
            Test-AndReport (Join-Path $dir 'IDE\VSIXInstaller.exe')
        }
    }
}

# 2) 从 Visual Studio 2022 注册表读取
$vsRegPaths = @(
    'HKLM:\SOFTWARE\Microsoft\VisualStudio\Setup',
    'HKLM:\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\Setup'
)
foreach ($rp in $vsRegPaths) {
    $key = Get-ItemProperty -Path $rp -ErrorAction SilentlyContinue
    if ($key -and $key.CachePath) {
        $base = $key.CachePath
        Get-ChildItem -Path (Join-Path $base '*') -Filter 'VSIXInstaller.exe' -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
            Test-AndReport $_.FullName
        }
    }
}

# 3) 枚举 Program Files / (x86)
$programFilesPaths = @(
    $env:ProgramFiles,
    ${env:ProgramFiles(x86)},
    'C:\Program Files',
    'C:\Program Files (x86)'
) | Select-Object -Unique

foreach ($pf in $programFilesPaths) {
    if (-not $pf -or -not (Test-Path $pf)) { continue }
    Get-ChildItem -Path $pf -Filter 'Microsoft SQL Server Management Studio *' -Directory -ErrorAction SilentlyContinue | ForEach-Object {
        $base = $_.FullName
        Test-AndReport (Join-Path $base 'Common7\IDE\VSIXInstaller.exe')
        Test-AndReport (Join-Path $base 'Release\VSIXInstaller.exe')
        Test-AndReport (Join-Path $base 'IDE\VSIXInstaller.exe')
    }
}

# 4) 枚举 Visual Studio 2022 目录
$vsRoots = @(
    $env:ProgramFiles,
    ${env:ProgramFiles(x86)},
    'C:\Program Files',
    'C:\Program Files (x86)'
) | Select-Object -Unique

foreach ($pf in $vsRoots) {
    if (-not $pf -or -not (Test-Path $pf)) { continue }
    $vsBase = Join-Path $pf 'Microsoft Visual Studio\2022'
    if (Test-Path $vsBase) {
        Get-ChildItem -Path $vsBase -Directory -ErrorAction SilentlyContinue | ForEach-Object {
            Test-AndReport (Join-Path $_.FullName 'Common7\IDE\VSIXInstaller.exe')
        }
    }
}

# 5) 全磁盘搜索（只搜文件系统根下的 Program Files，避免过慢）
Get-PSDrive -PSProvider FileSystem -ErrorAction SilentlyContinue | ForEach-Object {
    $root = $_.Root
    foreach ($pf in @('Program Files', 'Program Files (x86)')) {
        $base = Join-Path $root $pf
        if (Test-Path $base) {
            Get-ChildItem -Path $base -Filter 'Microsoft SQL Server Management Studio *' -Directory -ErrorAction SilentlyContinue | ForEach-Object {
                Test-AndReport (Join-Path $_.FullName 'Common7\IDE\VSIXInstaller.exe')
                Test-AndReport (Join-Path $_.FullName 'Release\VSIXInstaller.exe')
                Test-AndReport (Join-Path $_.FullName 'IDE\VSIXInstaller.exe')
            }
        }
    }
}

# 6) 如果还没找到，直接全盘精确查找 VSIXInstaller.exe（可能较慢，但最彻底）
if ($found.Count -eq 0) {
    Write-Host '正在全磁盘精确查找 VSIXInstaller.exe，请稍候...' -ForegroundColor Cyan
    Get-PSDrive -PSProvider FileSystem -ErrorAction SilentlyContinue | ForEach-Object {
        $root = $_.Root
        try {
            Get-ChildItem -Path $root -Filter 'VSIXInstaller.exe' -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
                Test-AndReport $_.FullName
            }
        }
        catch { }
    }
}

Write-Host ''
if ($found.Count -eq 0) {
    Write-Host '未找到 VSIXInstaller.exe。' -ForegroundColor Red
    Write-Host ''
    Write-Host '可能原因：' -ForegroundColor Yellow
    Write-Host '  1) SSMS 22 安装时未包含扩展开发/安装组件。' -ForegroundColor Yellow
    Write-Host '  2) 本机没有安装 Visual Studio 2022。' -ForegroundColor Yellow
    Write-Host '  3) SSMS 22 采用了新的扩展安装机制，不再使用 VSIXInstaller.exe。' -ForegroundColor Yellow
    Write-Host ''
    Write-Host '建议：' -ForegroundColor Yellow
    Write-Host '  - 安装 Visual Studio 2022 Community（免费）后重试。' -ForegroundColor Yellow
    Write-Host '  - 或改用 SSMS 内置的"扩展 -> 管理扩展"界面，从 VSIX 文件手动安装。' -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "共找到 $($found.Count) 个候选。" -ForegroundColor Green
    Write-Host ''
    Write-Host '推荐顺序（优先试第一个 SSMS 22 或 VS 2022 的路径）：' -ForegroundColor Green
    $found | ForEach-Object { Write-Host "  $_" }
    Write-Host ''
    Write-Host '使用示例：'
    Write-Host "  SqlFMSetup.exe /vsixinstaller:`"$($found[0])`""
    exit 0
}
