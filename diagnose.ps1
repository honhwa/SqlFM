# 检查新安装的扩展内容
Write-Host "=== Finding SqlFM.dll ==="
$found = Get-ChildItem "$env:LOCALAPPDATA\Microsoft\SSMS" -Recurse -Filter "SqlFM.dll" -ErrorAction SilentlyContinue
foreach ($f in $found) {
    $dir = $f.DirectoryName
    Write-Host "`n=== Extension folder: $dir ==="
    Get-ChildItem $dir | ForEach-Object { Write-Host "  $($_.Name)" }
    
    # 检查 pkgdef 是否存在
    $pkgdef = Get-ChildItem $dir -Filter "*.pkgdef" -ErrorAction SilentlyContinue
    if ($pkgdef) {
        Write-Host "`n=== pkgdef content ==="
        Get-Content $pkgdef.FullName
    } else {
        Write-Host "`n!!! NO PKGDEF FOUND !!!"
    }
}

# 检查最新的 ActivityLog
Write-Host "`n=== ActivityLog errors about SqlFM ==="
$logFile = "$env:APPDATA\Microsoft\SSMS\22.0_94b697ea\ActivityLog.xml"
if (Test-Path $logFile) {
    $content = [System.IO.File]::ReadAllText($logFile, [System.Text.Encoding]::Unicode)
    # 提取包含 SqlFM 或 B4AB3D7A 的行
    $lines = $content -split "`n"
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "SqlFM|B4AB3D7A") {
            # 打印周围几行
            $start = [Math]::Max(0, $i - 2)
            $end = [Math]::Min($lines.Count - 1, $i + 5)
            for ($j = $start; $j -le $end; $j++) {
                Write-Host $lines[$j]
            }
            Write-Host "---"
        }
    }
} else {
    Write-Host "ActivityLog not found at: $logFile"
}
