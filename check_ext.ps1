Write-Host "=== Extension folder contents ==="
$extDir = "C:\Users\Administrator\AppData\Local\Microsoft\SSMS\22.0_94b697ea\Extensions\3oyzv5wb.q1i"
Get-ChildItem $extDir -Recurse | ForEach-Object { Write-Host $_.FullName }

Write-Host "`n=== Check for ActivityLog ==="
$ssmsBase = "C:\Users\Administrator\AppData\Local\Microsoft\SSMS\22.0_94b697ea"
Get-ChildItem $ssmsBase -Filter "ActivityLog*" -ErrorAction SilentlyContinue | ForEach-Object { Write-Host $_.FullName }
Get-ChildItem $ssmsBase -Filter "*.log" -ErrorAction SilentlyContinue | ForEach-Object { Write-Host $_.FullName }

Write-Host "`n=== Check pkgdef ==="
Get-ChildItem $extDir -Filter "*.pkgdef" -Recurse | ForEach-Object { 
    Write-Host $_.FullName
    Get-Content $_.FullName
}
