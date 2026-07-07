; SqlFM 安装脚本
; 使用 Inno Setup 6.x 编译
; 方案：内嵌 .vsix 文件，通过 VSIXInstaller.exe 完成安装/卸载

#define MyAppName "SqlFM - T-SQL 格式化工具"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "SqlFM"
#define MyAppURL "https://github.com/SqlFM"
; VSIX 扩展 ID（来自 extension.vsixmanifest 中的 Identity Id）
#define MyExtensionId "SqlFM.B4AB3D7A-F5E7-485D-A68E-F9037042028C"

[Setup]
AppId={{B4AB3D7A-F5E7-485D-A68E-F9037042028C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
; 安装目录用于存放卸载信息，不是扩展实际目录（扩展由 VSIXInstaller 管理）
DefaultDirName={localappdata}\Programs\SqlFM
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; 不创建开始菜单快捷方式（纯扩展，无可执行程序）
DisableStartupPrompt=yes
OutputDir=..\output
OutputBaseFilename=SqlFMSetup_v{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
; 不需要管理员权限（VSIX 安装到用户目录）
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\uninstall_icon.ico
UninstallDisplayName={#MyAppName}
; 安装向导外观
WizardStyle=modern
; 显示安装完成页
DisableFinishedPage=no

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
; 中文界面下覆盖部分默认消息
chinesesimplified.BeveledLabel=SqlFM v{#MyAppVersion}

[Files]
; 将 VSIX 文件临时释放到 {tmp}，安装后自动删除
Source: "..\src\SqlFM\bin\Release\net48\SqlFM.vsix"; DestDir: "{tmp}"; Flags: deleteafterinstall
; 在安装目录存放一个版本标记文件，用于卸载时参考
Source: "version.txt"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Run]
; 安装时：调用 VSIXInstaller 静默安装 VSIX
Filename: "{code:GetVSIXInstallerPath}"; \
  Parameters: "/quiet ""{tmp}\SqlFM.vsix"""; \
  StatusMsg: "正在安装 SqlFM 扩展，请稍候..."; \
  Flags: waituntilterminated; \
  Check: VsixInstallerExists

[UninstallRun]
; 卸载时：调用 VSIXInstaller 静默卸载扩展
Filename: "{code:GetVSIXInstallerPath}"; \
  Parameters: "/quiet /uninstall:{#MyExtensionId}"; \
  Flags: waituntilterminated; \
  RunOnceId: "UninstallVsix"

[Code]
// -------------------------------------------------------
// 全局变量
// -------------------------------------------------------
var
  VsixInstallerExePath: String;
  VsixInstallerFound: Boolean;

// -------------------------------------------------------
// 查找 VSIXInstaller.exe
// SSMS 22 可能安装在 Program Files 或自定义路径
// -------------------------------------------------------
function FindVSIXInstaller(): String;
var
  Candidates: TArrayOfString;
  i: Integer;
  Path: String;
begin
  Result := '';
  SetArrayLength(Candidates, 6);
  // SSMS 22 标准安装路径（x64）
  Candidates[0] := ExpandConstant('{pf}\Microsoft SQL Server Management Studio 22\Release\VSIXInstaller.exe');
  Candidates[1] := ExpandConstant('{pf}\Microsoft SQL Server Management Studio 22\Common7\IDE\VSIXInstaller.exe');
  // Program Files (x86)
  Candidates[2] := ExpandConstant('{pf32}\Microsoft SQL Server Management Studio 22\Release\VSIXInstaller.exe');
  Candidates[3] := ExpandConstant('{pf32}\Microsoft SQL Server Management Studio 22\Common7\IDE\VSIXInstaller.exe');
  // 部分机器可能装在 C:\Program Files\Microsoft SQL Server Management Studio 22
  Candidates[4] := 'C:\Program Files\Microsoft SQL Server Management Studio 22\Release\VSIXInstaller.exe';
  Candidates[5] := 'C:\Program Files\Microsoft SQL Server Management Studio 22\Common7\IDE\VSIXInstaller.exe';

  for i := 0 to GetArrayLength(Candidates) - 1 do
  begin
    if FileExists(Candidates[i]) then
    begin
      Result := Candidates[i];
      Exit;
    end;
  end;
end;

// -------------------------------------------------------
// [Run] / [UninstallRun] 的 {code:...} 回调
// -------------------------------------------------------
function GetVSIXInstallerPath(Param: String): String;
begin
  Result := VsixInstallerExePath;
end;

// -------------------------------------------------------
// [Run] Check 回调：VSIXInstaller 存在时才执行安装步骤
// -------------------------------------------------------
function VsixInstallerExists(): Boolean;
begin
  Result := VsixInstallerFound;
end;

// -------------------------------------------------------
// 安装向导初始化：查找 VSIXInstaller，找不到则警告
// -------------------------------------------------------
function InitializeSetup(): Boolean;
begin
  VsixInstallerExePath := FindVSIXInstaller();
  VsixInstallerFound := (VsixInstallerExePath <> '');

  if not VsixInstallerFound then
  begin
    MsgBox(
      '警告：未找到 VSIXInstaller.exe。' + #13#10 + #13#10 +
      '请确认已安装 SQL Server Management Studio 22（SSMS 22）。' + #13#10 +
      '下载地址：https://aka.ms/ssmsfullsetup' + #13#10 + #13#10 +
      '若 SSMS 已安装但路径不在默认位置，请手动安装 VSIX 文件：' + #13#10 +
      '  ..\src\SqlFM\bin\Release\net48\SqlFM.vsix',
      mbError, MB_OK
    );
    Result := False; // 阻止安装继续
    Exit;
  end;

  Result := True;
end;

// -------------------------------------------------------
// 安装完成后：提示用户重启 SSMS
// -------------------------------------------------------
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssDone then
  begin
    MsgBox(
      'SqlFM 已成功安装！' + #13#10 + #13#10 +
      '请重启 SQL Server Management Studio 22 以使扩展生效。' + #13#10 + #13#10 +
      '安装成功后，在 SSMS 编辑器中右键菜单将出现"SqlFM 格式化"选项。',
      mbInformation, MB_OK
    );
  end;
end;
