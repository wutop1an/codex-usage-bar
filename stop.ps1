$ErrorActionPreference = 'Stop'
$executable = Join-Path $PSScriptRoot 'bin\CodexUsageBar.exe'
if (Test-Path -LiteralPath $executable) {
    $running = @(Get-Process -Name 'CodexUsageBar' -ErrorAction SilentlyContinue | Where-Object {
        try { $_.Path -eq $executable } catch { $false }
    })
    $process = Start-Process -FilePath $executable -ArgumentList '--stop' -WindowStyle Hidden -PassThru -Wait
    if ($process.ExitCode -ne 0) { throw 'The usage bar did not acknowledge shutdown.' }
    foreach ($instance in $running) {
        try {
            if (-not $instance.WaitForExit(5000)) { throw 'The usage bar is still shutting down.' }
        } finally { $instance.Dispose() }
    }
}

