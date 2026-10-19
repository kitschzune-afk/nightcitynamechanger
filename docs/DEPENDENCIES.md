# Build and runtime dependencies

| Component | Version / provenance | Included in this source repo? |
|---|---|---|
| .NET SDK used for NcNc | 8.0.426, Microsoft dotnet-install script | No; install independently |
| NcNc target/runtime | net8.0-windows, Windows x64; Microsoft runtime packs 8.0.32 | No binary runtime |
| WolvenKit Console | 9.1.0, official GitHub release | No binaries; GPL text and source link included |
| WolvenKit runtime in portable ZIP | Microsoft .NET 10.0.13 x64 | No binary runtime |
| WPF | Microsoft WindowsDesktop runtime | No binary runtime |
| NcNc NuGet package references | None beyond SDK/runtime build dependencies; projects reference Ncnc.Core | Source projects included |

Official WolvenKit ZIP: https://github.com/WolvenKit/WolvenKit/releases/download/9.1.0/WolvenKit.Console-9.1.0.zip

Corresponding source: https://github.com/WolvenKit/WolvenKit/archive/refs/tags/9.1.0.zip

Microsoft .NET download: https://dotnet.microsoft.com/download/dotnet

Runtime package used for bundled CLI runtime: https://builds.dotnet.microsoft.com/dotnet/Runtime/10.0.13/dotnet-runtime-10.0.13-win-x64.zip

The delivered WolvenKit package contains managed and native libraries (including compression, SQLite and ClearScript components) supplied upstream. `PORTABLE-FILES.csv` inventories each distributed file with its SHA-256 and size. This is a file inventory, not a complete transitive-license SBOM or vulnerability scan. Inspect upstream package licenses/source before redistributing binary dependencies.

The application invokes WolvenKit as a separate process. No CET, redscript, RED4ext or ArchiveXL installation is required for its generated legacy archive. Existing runtime mods can still affect game behavior.
