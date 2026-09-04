# Changelog

## 0.3.0 - 2026-09-04

- Replaced the PowerShell WinForms startup chain with a native .NET 8 Windows executable.
- Embedded all 18 ENS styles, the 64-reference RIS set, test matrix, reference-type configuration, and third-party notices into one self-contained EXE.
- Added native search, category filters, previews, install-state detection, per-style and bulk installation, recoverable uninstall, and support-file export.
- Added an executable self-test covering embedded resources, hashes, installation, replacement backup, uninstall recovery, and support-file export.
- Added GitHub Actions Windows builds that launch the published EXE and block releases unless its self-test passes.

## 0.2.3 - 2026-09-04

- Normalized top-level JSON arrays for Windows PowerShell 5.1 instead of passing an Object[] as Join-Path ChildPath.
- Removed the duplicate global trap so startup failures produce one useful diagnostic dialog.
- Added regression assertions for the PowerShell 5.1 manifest normalization path.

## 0.2.2 - 2026-09-04

- Removed typographic quote characters that Windows PowerShell parses as string delimiters.
- Added a regression test that rejects smart-quote tokens in all PowerShell sources.
- Fixed the parse error reported at LaunchToolbox.ps1 line 19 and the same latent error in the uninstall dialog.

## 0.2.1 - 2026-09-04

- Fixed the asynchronous hidden launcher that could appear to flash and exit without an error.
- Added a startup wrapper, Chinese error dialog, and a diagnostic log in the temporary directory.
- Added startup validation for the Styles directory, manifest, and all bundled ENS files.
- Kept the command window open on startup failure so the error can be reported.

## 0.2.0 - 2026-09-04

- Added a Windows PowerShell WinForms visual style manager.
- Added search, category filtering, previews, installation-state detection, per-style and bulk install/uninstall.
- Made uninstall recoverable by moving styles to timestamped backup folders.
- Added 18 distinct ENS files across humanities, social sciences, law, publishing, and GB/T note formats.
- Added 64 test references and a 108-case regression matrix.
- Adopted China Social Sciences 2026 Alpha 2.7 as the stable baseline.
- Disabled special consecutive-citation replacement to avoid isolated-period output.

## 0.1.0 - 2026-09-04

- Initial multi-style command-line installation package.
