$paths = @(
    "$env:APPDATA\Microsoft\SQL Server Management Studio",
    "$env:LOCALAPPDATA\Microsoft\SQL Server Management Studio",
    "$env:LOCALAPPDATA\Microsoft\SSMSApps"
)
foreach ($p in $paths) {
    if (Test-Path $p) {
        Write-Host "=== $p ==="
        Get-ChildItem $p -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.Name -like "ActivityLog*" -or $_.Name -like "*.log" } | Select-Object -First 10 -ExpandProperty FullName
    }
}
Write-Host "`n=== Checking extension install location ==="
$extPaths = @(
    "$env:LOCALAPPDATA\Microsoft\SQL Server Management Studio\22.0_213.11806.211\Extensions",
    "$env:LOCALAPPDATA\Microsoft\SSMSApps\Extensions",
    "$env:LOCALAPPDATA\Microsoft\SQL Server Management Studio\Extensions"
)
foreach ($p in $extPaths) {
    if (Test-Path $p) {
        Write-Host "FOUND: $p"
        Get-ChildItem $p -Directory | Select-Object -ExpandProperty Name
    }
}
Write-Host "`n=== Search SqlFM.dll ==="
Get-ChildItem "$env:LOCALAPPDATA" -Recurse -Filter "SqlFM.dll" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
