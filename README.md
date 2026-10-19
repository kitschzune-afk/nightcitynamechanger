# NcNc — Night City Name Changer

**Source and review repository for the Windows x64 application, version 1.1.0.**

NcNc generates an English text-localization mod for Cyberpunk 2077 and Phantom Liberty. The default protagonist name is Johnny. The application scans the user's installed game, presents a replacement preview, builds a real REDengine archive with WolvenKit, and installs or removes its own archive using an ownership manifest.

This repository is provided for independent review following a reported Nexus quarantine. **The quarantine reason has not been supplied or independently established. This repository is not a claim of antivirus clearance or a request to bypass quarantine.**

## For Nexus moderators

Start with [REVIEWER_GUIDE.md](REVIEWER_GUIDE.md), [SECURITY_REVIEW.md](SECURITY_REVIEW.md), and [release hashes](docs/RELEASE-HASHES.sha256). The repository contains application source, the mapping database, icon source, build/test scripts and a file-by-file hash inventory of the delivered portable ZIP. It deliberately excludes executable binaries, original game assets, generated mods, caches, credentials and personal operation logs.

The delivered application is **unsigned**, uses .NET single-file self-contained publishing, and includes native/runtime libraries plus a separate WolvenKit CLI. These are disclosed packaging facts, not an explanation of the quarantine. Reviewers can build a normal multi-file version to inspect the managed assemblies without using the delivered executable.

## Features

- C# / WPF desktop UI; custom name, four category switches, preview and per-entry exclusions.
- English base-game and Phantom Liberty subtitles, journal-linked text, messages/shards and on-screen text.
- Standalone uppercase V candidates selected by default, including character-creation pronoun descriptions. Embedded letters, common markup and compound names are protected.
- Source-reviewed Vincent/Valerie mappings; unrelated NPC full names remain unselected by default. Approve all intentionally broadens selection and displays a warning.
- Version-aware scanning cache, repeated-build reuse and original-text-based replacement.
- WolvenKit conversion, archive packing, extraction/hash verification and semantic round-trip checks.
- Install/uninstall limited to an owned archive and manifest; original game archives are not modified.

Spoken audio is unchanged. Runtime-generated or mod-injected strings and text baked into artwork are outside scanning scope. Default matching is intentionally broad and may replace unrelated standalone initials or Roman numerals. Full in-game coverage is not claimed.

## Build and test

Prerequisites: Windows x64, PowerShell 7 and **.NET SDK 8.0.426**. `global.json` pins the SDK used for the delivered build. NuGet restore downloads Microsoft runtime/reference/build packages as needed. No third-party NuGet package is referenced by the NcNc projects.

```powershell
./scripts/Test.ps1
./scripts/Build.ps1
# Optional: reproduce the single-file packaging style of the delivered application
./scripts/Build.ps1 -SingleFile
```

Output is `artifacts/app`. The default build is a self-contained multi-file application. Both builds copy `reviewed.json` alongside it. They do **not** download or launch WolvenKit, read the game, install a mod or change system configuration.

For actual game-resource generation, download WolvenKit Console **9.1.0** from its [official release](https://github.com/WolvenKit/WolvenKit/releases/tag/9.1.0), verify the ZIP hash in `docs/RELEASE-HASHES.sha256`, and extract it to `artifacts/app/tools`. WolvenKit requires .NET 10 x64; install it from Microsoft or put its runtime in `artifacts/app/tools/runtime`. The shipped portable package included runtime 10.0.13. See [dependencies](docs/DEPENDENCIES.md).

Run `artifacts/app/NightCityNameChanger.exe`, select the game folder, scan, inspect the preview, generate and then install with the game closed. The Start menu helper is optional and is documented separately; it does not run at application startup.

## Source map

| Component | Location |
|---|---|
| Desktop interface, saved selections, progress/logging | `source/Ncnc.App` |
| Name validation and token-aware matching | `source/Ncnc.Core/Replacement.cs` |
| Game detection and cache fingerprint | `source/Ncnc.Core/Game.cs` |
| Archive extraction, localization/journal indexing | `source/Ncnc.Core/Scanner.cs` |
| Compiling, packaging, validating generated resources | `source/Ncnc.Core/Builder.cs` |
| External process execution | `source/Ncnc.Core/Toolchain.cs` |
| Installation ownership, collision checks, uninstall | `source/Ncnc.Core/Installer.cs` |
| Data models and JSON/path/hash helpers | `source/Ncnc.Core/Models.cs` |
| Console tests and optional integration commands | `source/Ncnc.Tests/Program.cs` |
| Source-reviewed resource/ID/hash approvals | `reviewed.json` |
| Icon PNG/ICO and conversion script | `source/Ncnc.App/Assets`, `source/Make-Icon.ps1` |

## Evidence and reproducibility limits

The development verification record reports 30 focused tests, 6 build checks and 8 installer checks. The English installed-game build selected 5,433 variants in 839 resources, including 1,163 Phantom Liberty subtitle variants. See [verification](docs/Verification.md) for exactly what was checked and what remains unverified.

Application sources were copied without editing their implementation from the development output used for 1.1.0. The project version is explicit in source; publishing the delivered binary also passed `Version=1.1.0`. A fresh build has not been shown to be byte-for-byte identical to the delivered EXE. SDK/runtime versions, source paths, PDB content and build properties can affect binary identity. Hashes identify the specific supplied artifacts, not a proof of safety or reproducible equivalence.

## Licensing

No new license grant for the original NcNc application source is selected by this review repository. It is published for inspection; contact the repository owner regarding reuse/distribution permission. Third-party components retain their upstream licenses. WolvenKit's GPL-3.0 text and source link are provided under `licenses`; binaries are not included here. The source repository itself contains no CDPR game localization tables.
