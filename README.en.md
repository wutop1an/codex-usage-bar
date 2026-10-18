# Codex Usage Bar

A compact usage meter embedded in the **Windows Codex desktop title bar**.

[中文](README.md) · [Reference](docs/REFERENCE.md) · [Architecture](docs/ARCHITECTURE.md)

![Sample quotas rendered by the application; no account data](docs/assets/preview.png)

## Features

- Remaining quota and reset time, using the locally signed-in Codex CLI account.
- A fixed red-left / blue-right gradient. The colored remainder shrinks from right to left.
- A native Windows child window that follows its parent, not an always-on-top desktop overlay.
- Read-only dialog detection, immediate hiding when detected, and a 180 ms reveal animation.
- No patching of the Codex installation, no telemetry, and no bundled credentials.
- Separate production and test builds with no NuGet dependencies.

**Experimental community project. Windows 11 x64 only; macOS and Linux are not implemented.**
The current interface labels are Chinese. `周限额 75%` means **75% remaining** in the weekly window.

## Run

Install and sign in to the Windows Codex desktop app. .NET Framework 4.8 and Windows PowerShell
are required; a separate .NET SDK, Node.js, and Python are not needed.

```powershell
git clone https://github.com/wutop1an/codex-usage-bar.git
cd codex-usage-bar
.\build.ps1
.\start.ps1
```

Alternatively, download and extract the source ZIP, then double-click `Start.cmd`.
The first launch compiles locally. Use `Stop.cmd` or the tray menu to exit.
The command launcher uses a process-scoped PowerShell execution policy; it does not change system policy.

Click the bar to refresh; right-click to refresh or quit. Automatic refresh runs every two minutes.
Dates use the computer's local time zone. Stale values are gray and marked with an asterisk.
If no account data is available, the bar shows an unavailable state rather than an invented percentage.

## Build and test

```powershell
.\test.ps1
.\package.ps1
```

Tests run without an account or network calls. They cover parsing, drawing, transitions, DPI alignment,
and native child-window properties. Live compatibility still requires manual testing in Codex.

## Limitations and privacy

The host detector currently targets `ChatGPT.exe` inside an `OpenAI.Codex_` Windows app installation.
The bar is dark-theme only and hides in narrow windows. Dialog detection depends on Windows UI
Automation and Chromium behavior. Other app versions may need adaptation.

The helper starts the installed `codex.exe app-server --stdio` to read `account/rateLimits/read`.
It does not read authentication files directly or send model prompts. It displays the **CLI account's**
limits, which may differ from the desktop account. API billing and per-task costs are not supported.

Local diagnostics are stored in `%LOCALAPPDATA%\CodexUsageBar\status.json`; remove account usage and
window/process details before sharing. No runtime account data is committed to this repository.

MIT licensed. The reset icon is from Microsoft's Fluent UI System Icons, with its license retained.
This project is not affiliated with or endorsed by OpenAI.

