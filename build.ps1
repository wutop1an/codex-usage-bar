param([switch]$TestBuild)
$ErrorActionPreference = 'Stop'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'The 64-bit .NET Framework compiler is required. Install .NET Framework 4.8.'
}
$binaryDir = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $binaryDir -Force | Out-Null
$name = if ($TestBuild) { 'CodexUsageBar.Tests.exe' } else { 'CodexUsageBar.exe' }
$outputFile = Join-Path $binaryDir $name
$sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object FullName)
$references = @('System.Windows.Forms.dll', 'System.Drawing.dll', 'System.Web.Extensions.dll', 'System.Core.dll')
$references += @('UIAutomationClient.dll', 'UIAutomationTypes.dll', 'WindowsBase.dll') | ForEach-Object {
    Join-Path $framework "WPF\$_"
}
$arguments = @('/nologo', '/target:winexe', '/optimize+', '/platform:x64', '/warnaserror+')
$arguments += $references | ForEach-Object { "/r:$_" }
$arguments += "/win32manifest:$(Join-Path $PSScriptRoot 'app.manifest')"
$arguments += "/resource:$(Join-Path $PSScriptRoot 'assets\reset.png'),CodexUsageBar.Reset.png"
$arguments += "/out:$outputFile"
if ($TestBuild) {
    $arguments += '/define:TEST_BUILD'
    $sources += (Join-Path $PSScriptRoot 'tests\Tests.cs')
}
$compilerOutput = & $compiler @arguments @sources 2>&1
if ($LASTEXITCODE -ne 0) { throw ("Build failed:`n" + ($compilerOutput -join "`n")) }
Write-Output $outputFile

