$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1') | Out-Null
& (Join-Path $PSScriptRoot 'test.ps1')
$settings = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'src\AppSettings.cs') -Raw
$versionMatch = [regex]::Match($settings, 'Version\s*=\s*"([\d.]+)"')
if (-not $versionMatch.Success) { throw 'Could not read source version.' }
$version = $versionMatch.Groups[1].Value
$distribution = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $distribution -Force | Out-Null
$packageName = "codex-usage-bar-$version-windows-x64"
$zipPath = Join-Path $distribution "$packageName.zip"

# Stage into a unique directory. No recursive deletion or broad workspace copying.
$staging = Join-Path ([IO.Path]::GetTempPath()) ('codex-usage-bar-package-' + [guid]::NewGuid().ToString('N'))
$package = Join-Path $staging $packageName
New-Item -ItemType Directory -Path $package -Force | Out-Null
$files = @('README.md', 'README.en.md', 'LICENSE', 'THIRD_PARTY_NOTICES.md', 'CHANGELOG.md',
    'CONTRIBUTING.md', 'app.manifest', 'build.ps1', 'start.ps1', 'stop.ps1', 'test.ps1', 'package.ps1',
    'Start.cmd', 'Stop.cmd', '.editorconfig', '.clang-format', '.gitignore')
foreach ($file in $files) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination $package }
foreach ($folder in @('src', 'tests', 'assets', 'docs', '.github')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $folder) -Destination $package -Recurse
}
$binaryDir = Join-Path $package 'bin'
New-Item -ItemType Directory -Path $binaryDir -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'bin\CodexUsageBar.exe') -Destination $binaryDir

# ZipFile includes dotfiles consistently in both Windows PowerShell and PowerShell 7.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$temporaryZip = Join-Path $staging "$packageName.zip"
[IO.Compression.ZipFile]::CreateFromDirectory($staging + '\' + $packageName, $temporaryZip)
Copy-Item -LiteralPath $temporaryZip -Destination $zipPath -Force
$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText($zipPath + '.sha256', "$hash  $packageName.zip`n", [Text.UTF8Encoding]::new($false))
Write-Output $zipPath
Write-Output "SHA256 $hash"

