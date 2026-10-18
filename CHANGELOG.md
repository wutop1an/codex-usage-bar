# Changelog

## 2.2.0 — 2026-10-18

First public source release.

- Split quota parsing, RPC access, window management, rendering and visibility monitoring into focused modules.
- Exclude self-tests and preview generation from the production executable.
- Replace repeated full accessibility-tree scans with a bounded focus-ancestor walk and event notifications.
- Add a 180 ms reveal animation; hide without fading when a dialog is detected.
- Honor the Windows client-area animation preference.
- Centralize timing defaults; remove historical probes and duplicated implementations from the public tree.
- Add Windows build/test automation, release packaging, documentation and retained third-party notices.

Earlier 2.1.x builds were local prototypes, not public releases. macOS support is not included.

