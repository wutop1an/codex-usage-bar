# Contributing

Please keep changes small and reproducible. For a bug report, include Windows version, Codex version,
display scaling, theme, expected behavior and the minimum reproduction steps.

Do not attach account credentials, complete diagnostic dumps or conversation screenshots without reviewing them.
The reference document explains every local diagnostic field so you can share only what is necessary.

## Local checks

```powershell
.\build.ps1
.\test.ps1
```

Use C# syntax supported by the .NET Framework compiler (C# 5). Keep one responsibility per module and
name Win32 constants rather than adding unexplained masks. Formatting conventions are in `.editorconfig`
and `.clang-format`; the formatter is optional and is not a build dependency.

For visual changes, generate the offscreen sample with the test executable and check it at
100%, 125%, 150% and 200% scaling. For visibility changes, manually test image open/close, normal dialogs,
minimize/restore and switching applications. Do not claim a new platform works without running it there.

For protocol changes, add synthetic fixtures to `tests/Tests.cs`. Never commit a live account response.
Use `AppSettings.Version` as the source release version and update `app.manifest` and `CHANGELOG.md` together.

macOS contributions need a native implementation and on-device validation. A floating overlay is not
equivalent to the current embedded child-window behavior.
