# Moderator review guide

## What this submission establishes

This is a source snapshot and behavior disclosure for NcNc 1.1.0. The user reported that Nexus quarantined the mod/download and requested outside review. No detection name, antivirus report, moderator finding, or quarantine rationale was provided to the developer. Do not treat the absence of such information as a clean result.

1. Confirm the file under review matches `docs/RELEASE-HASHES.sha256`.
2. Compare extracted portable-package files against `docs/PORTABLE-FILES.csv` (relative path, byte count and SHA-256).
3. Review the process/file-access inventory in `SECURITY_REVIEW.md`.
4. Inspect `Toolchain.cs`, `Installer.cs`, `Models.cs` and the WPF handlers before executing anything.
5. Build the multi-file app from source with `scripts/Build.ps1`; run `scripts/Test.ps1` independently. Build scripts use the local dotnet SDK and normal restore, not an embedded download/execute bootstrap.
6. Obtain the external toolchain independently from WolvenKit/Microsoft if exercising real game-resource processing.
7. Use an isolated game/test environment for generation and install tests. The application has not received an independent security audit.

## Delivered artifact identity

The executable reports file version 1.1.0.0 and does not have an Authenticode signature. The source-review package is separate from the 144.5 MiB portable ZIP. No binaries need to be trusted to read the source. The delivered ZIP included the app, toolchain/runtime, source, documentation and icon assets; individual hashes are preserved for comparison.

The executable is a standard .NET self-contained single-file WPF publish. Native libraries can be extracted by the .NET host into the user temporary bundle-extraction directory. The app's background WolvenKit processes use hidden windows and redirected output; that behavior is in readable source. There is no obfuscation or custom executable packer in the build scripts.

## Test commands beyond the game-free suite

The console test project also accepts `integration`, `build-tests`, `installer-tests`, `install` and `uninstall`. Inspect `source/Ncnc.Tests/Program.cs` before invoking these. In particular, **install/uninstall explicitly modify the selected game mod directory**. The default test command does not invoke them or touch a real game installation.

The game-free runner uses a simple assertion harness and exits nonzero when a check fails. It is not a `dotnet test`/xUnit suite. Its passing status demonstrates only the assertions in that file.

## Optional shortcut and artwork

`Create-Start-Menu-Shortcut.ps1` creates a current-user Programs-folder shortcut with WScript.Shell. It points to `app/NightCityNameChanger.exe` relative to the script, so place it beside an `app` folder to use it. It does not add a Startup entry. `source/Make-Icon.ps1` converts the included generated PNG into standard PNG-backed ICO sizes using System.Drawing.

The review repository adds documentation and build scripts; it does not silently repair or change the shipped application implementation. Any future remediation should be separately versioned and its binary hashes regenerated.
