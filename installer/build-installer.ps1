# build-installer.ps1
# SqlFM 安装包自动化构建脚本
# 用法：在 installer\ 目录下运行，或直接运行此脚本
# 
# 前置条件：
#   1. 已安装 .NET Framework 4.8 SDK（随 Visual Studio 附带，用于构建 net48 项目）
#   2. 已安装 Inno Setup 6.x（https://jrsoftware.org/isdl.php）

$ErrorActionPreference = "Stop"
$ScriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$SolutionFile = Join-Path $ProjectRoot "SqlFM.sln"
$IssFile      = Join-Path $ScriptDir  "SqlFMSetup.iss"
$OutputDir    = Join-Path $ProjectRoot "output"

Write-Host ""
Write-Host "============================================" -ForegroundColor DarkCyan
Write-Host "  SqlFM 安装包构建脚本" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor DarkCyan
Write-Host ""

# ----------------------------------------------------------
# Step 1: 构建 Release
# ----------------------------------------------------------
Write-Host ">>> Step 1: 构建 Release 版本..." -ForegroundColor Cyan

if (-not (Test-Path $SolutionFile)) {
    Write-Host "ERROR: 找不到解决方案文件: $SolutionFile" -ForegroundColor Red
    exit 1
}

dotnet build $SolutionFile --configuration Release --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: 构建失败，请先修复编译错误。" -ForegroundColor Red
    exit 1
}

# 验证 VSIX 文件是否生成
$VsixPath = Join-Path $ProjectRoot "src\SqlFM\bin\Release\net48\SqlFM.vsix"
if (-not (Test-Path $VsixPath)) {
    Write-Host "ERROR: 未找到 VSIX 文件: $VsixPath" -ForegroundColor Red
    Write-Host "       请确认项目已正确配置 VSIX 输出。" -ForegroundColor Yellow
    exit 1
}

$VsixSize = [math]::Round((Get-Item $VsixPath).Length / 1KB, 1)
Write-Host "    VSIX 文件：$VsixPath ($VsixSize KB)" -ForegroundColor Green
Write-Host "    构建成功。" -ForegroundColor Green
Write-Host ""

# ----------------------------------------------------------
# Step 2: 查找 Inno Setup 编译器
# ----------------------------------------------------------
Write-Host ">>> Step 2: 查找 Inno Setup 编译器..." -ForegroundColor Cyan

$IsccCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)

$IsccPath = $IsccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $IsccPath) {
    Write-Host ""
    Write-Host "ERROR: 未找到 Inno Setup 6 编译器（ISCC.exe）。" -ForegroundColor Red
    Write-Host ""
    Write-Host "请安装 Inno Setup 6：" -ForegroundColor Yellow
    Write-Host "  下载地址：https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    Write-Host "  安装后重新运行此脚本。" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "也可以手动编译：打开 Inno Setup，加载脚本：" -ForegroundColor Yellow
    Write-Host "  $IssFile" -ForegroundColor Yellow
    exit 1
}

Write-Host "    找到：$IsccPath" -ForegroundColor Green
Write-Host ""

# ----------------------------------------------------------
# Step 3: 创建 output 目录
# ----------------------------------------------------------
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    Write-Host "    已创建输出目录：$OutputDir" -ForegroundColor Gray
}

# ----------------------------------------------------------
# Step 4: 编译安装包
# ----------------------------------------------------------
Write-Host ">>> Step 3: 编译 Inno Setup 安装包..." -ForegroundColor Cyan
Write-Host "    脚本：$IssFile" -ForegroundColor Gray
Write-Host ""

& $IsccPath $IssFile
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "ERROR: 安装包编译失败（ISCC 返回代码 $LASTEXITCODE）。" -ForegroundColor Red
    exit 1
}

# ----------------------------------------------------------
# Step 5: 输出结果
# ----------------------------------------------------------
$OutputExe = Get-ChildItem $OutputDir -Filter "SqlFMSetup*.exe" |
             Sort-Object LastWriteTime -Descending |
             Select-Object -First 1

Write-Host ""
Write-Host "============================================" -ForegroundColor DarkGreen
Write-Host "  构建完成！" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor DarkGreen

if ($OutputExe) {
    $SizeMB = [math]::Round($OutputExe.Length / 1MB, 2)
    Write-Host ""
    Write-Host "  安装包路径：" -ForegroundColor White
    Write-Host "  $($OutputExe.FullName)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  文件大小：$SizeMB MB" -ForegroundColor Gray
    Write-Host "  生成时间：$($OutputExe.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Gray
} else {
    Write-Host "  输出目录：$OutputDir" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "将生成的 .exe 文件复制到目标机器后，双击即可安装。" -ForegroundColor Cyan
Write-Host "前提：目标机器已安装 SQL Server Management Studio 22。" -ForegroundColor Cyan
Write-Host ""
