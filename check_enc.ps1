$rootDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$path = Join-Path $rootDir "src\SqlFM\SqlFM.pkgdef"
$bytes = [System.IO.File]::ReadAllBytes($path)
Write-Host "File size: $($bytes.Length)"
Write-Host "First 4 bytes: $($bytes[0]),$($bytes[1]),$($bytes[2]),$($bytes[3])"
