#Requires -Version 3.0
<#
.SYNOPSIS
    从 SqlFMSetup.exe 中提取内嵌的 SqlFM.vsix。
.DESCRIPTION
    SqlFMSetup.exe 将 VSIX 作为托管资源内嵌。此脚本加载该程序集并把资源流保存为独立的 .vsix 文件。
    提取后可直接双击安装，或在 SSMS "扩展 -> 管理扩展" 中从文件安装。
#>
param(
    [string]$SetupExe = "$PSScriptRoot\..\output\SqlFMSetup.exe",
    [string]$OutputVsix = "$PSScriptRoot\..\output\SqlFM.vsix"
)

$SetupExe = Resolve-Path $SetupExe -ErrorAction Stop
$OutputDir = Split-Path $OutputVsix -Parent
if (-not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null }

# 必须用 Reflection 加载程序集
$asm = [System.Reflection.Assembly]::LoadFrom($SetupExe)
$resourceName = $null
foreach ($n in $asm.GetManifestResourceNames()) {
    if ($n -like '*.vsix') { $resourceName = $n; break }
}

if (-not $resourceName) {
    Write-Error '未在 SqlFMSetup.exe 中找到 .vsix 资源。安装包可能已损坏。'
    exit 1
}

$stream = $asm.GetManifestResourceStream($resourceName)
$fs = [System.IO.File]::Create($OutputVsix)
try {
    $stream.CopyTo($fs)
} finally {
    $fs.Dispose()
    $stream.Dispose()
}

Write-Host "已提取: $OutputVsix" -ForegroundColor Green
Write-Host "大小: $((Get-Item $OutputVsix).Length) 字节" -ForegroundColor Green
