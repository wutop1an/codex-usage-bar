$ErrorActionPreference = 'Stop'
$executable = Join-Path $PSScriptRoot 'bin\CodexUsageBar.exe'
if (-not (Test-Path -LiteralPath $executable)) { & (Join-Path $PSScriptRoot 'build.ps1') | Out-Null }
Start-Process -FilePath $executable -WorkingDirectory $PSScriptRoot -WindowStyle Hidden

