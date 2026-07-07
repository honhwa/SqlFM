Write-Host "=== Looking for pkgdef in build output ==="
$rootDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$outDir = Join-Path $rootDir "src\SqlFM\bin\Release\net48"
if (Test-Path $outDir) {
    Get-ChildItem $outDir -Filter "*.pkgdef" -Recurse | ForEach-Object { 
        Write-Host $_.FullName
        Get-Content $_.FullName | Select-Object -First 30
    }
} else {
    Write-Host "Release output not found, checking Debug..."
    $outDir = Join-Path $rootDir "src\SqlFM\bin\Debug\net48"
    if (Test-Path $outDir) {
        Get-ChildItem $outDir -Filter "*.pkgdef" -Recurse | ForEach-Object { 
            Write-Host $_.FullName
            Get-Content $_.FullName | Select-Object -First 30
        }
    }
}
Write-Host "`n=== Check obj for pkgdef ==="
Get-ChildItem (Join-Path $rootDir "src\SqlFM\obj") -Filter "*.pkgdef" -Recurse -ErrorAction SilentlyContinue | ForEach-Object { 
    Write-Host $_.FullName
}
