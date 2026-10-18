$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1') -TestBuild | Out-Null
$executable = Join-Path $PSScriptRoot 'bin\CodexUsageBar.Tests.exe'
$process = Start-Process -FilePath $executable -ArgumentList '--self-test' -WindowStyle Hidden -PassThru -Wait
$report = Join-Path $env:LOCALAPPDATA 'CodexUsageBar\self-test.txt'
if (Test-Path -LiteralPath $report) { Get-Content -LiteralPath $report }
if ($process.ExitCode -ne 0) { throw 'Self-tests failed.' }

