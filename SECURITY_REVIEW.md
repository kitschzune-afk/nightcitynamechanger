# Behavior and security review notes

This is a disclosure based on the supplied source, not an independent malware verdict. No scanner result or Nexus clearance is asserted. Do not disable security software, add exclusions, or bypass quarantine to use this project.

## Observable operations

| Operation | Scope / implementation |
|---|---|
| Network | NcNc application source contains no HTTP client, telemetry, updater, upload or network command. Build-time NuGet restore may access package feeds. Bundled external components must be assessed independently. |
| Game discovery | Reads Windows uninstall registry entries, Steam library paths and Epic manifest files; supports a user-selected root folder. |
| Game reads | Executable metadata, English base/expansion localization archives, gamedata archives for journal extraction, installed mod archives for depot collision listing. |
| Settings/cache/log writes | `%LOCALAPPDATA%/NightCityNameChanger`: settings, approved/excluded choices, extracted resources, indexes, generated builds and operations.log. Logs can contain local paths and mod names; users should review logs before sharing. |
| Temporary/native extraction | Standard .NET single-file extraction may write beneath the user's temporary `.net` bundle location. This is separate from the application data directory. |
| Child processes | Launches the selected WolvenKit CLI with `ProcessStartInfo`, `UseShellExecute=false`, structured `ArgumentList`, redirected output and no console window. It clears inherited DOTNET_ROOT settings or selects the bundled tool runtime. Cancellation can terminate the launched process tree. |
| Game process check | Checks whether a process named Cyberpunk2077 is running before install/uninstall. Does not inject into it or modify process memory. |
| Mod installation | Writes `archive/pc/mod/000_ncnc_6f841e79.archive`, `ncnc_6f841e79.manifest.json`, and temporary/pending commit files. Original game archives are read, not edited. |
| Mod removal | Removes the fixed owned archive only after matching the manifest/hash, then removes its manifest. Changed/unowned archives are refused. |
| Export preview | Writes JSON to a path chosen in the Save dialog. |
| Registry changes / persistence | Application code reads registry keys but does not write them, create services, scheduled tasks, autoruns or startup entries. Optional helper creates a normal current-user Start menu shortcut only. |
| Elevation | App manifest is `asInvoker`; no automatic elevation request. Access-denied errors are surfaced. |
| Native interop | `App.xaml.cs` calls shell32 `SetCurrentProcessExplicitAppUserModelID` for Windows taskbar identity. Toolchain ships additional native DLLs separately. |

## Trust boundaries and limitations worth reviewing

- The chosen tool executable is user-configurable. NcNc does **not** enforce a pinned runtime hash or signature before launching it. Distribution hashes help reviewers compare files but are not runtime enforcement. A substituted toolchain is outside the guarantees of the app source.
- Settings, manifests, caches and builds are writable by the current user. SHA-256 checks detect accidental changes but are not an authenticity boundary against a malicious process running as that same user.
- The installer rejects directory reparse points, but does not implement a full handle-based defense against races or every file-level reparse-point case. Review this before treating it as secure against hostile local actors.
- An application-specific mutex coordinates cooperating installs; unrelated processes and mod managers do not share it. Conflict scanning occurs before final commit and cannot exclude a concurrent filesystem change.
- Install rollback handles ordinary synchronous failures. A power failure can interrupt the archive/manifest two-file commit; a pending record can remain. Automatic recovery of all interrupted states is not implemented.
- Cache invalidation uses source paths, file sizes and timestamps. It is not a cryptographic rehash of every game archive on every operation.
- Conflict detection checks depot-path/hash overlap. It cannot prove that runtime/ArchiveXL string overrides or virtual mod-manager deployments are compatible.
- The code assumes the user chooses a legitimate game installation and toolchain. It does not validate the game's publisher signature.
- JSON and archive input processing relies on .NET/WolvenKit. Malformed input and external native dependencies are separate review surfaces.
- V matching is intentionally broad at the user's request. Unrelated standalone initials/Roman numerals can change. This is a text-correctness limitation, not a demonstrated security finding.

## Unsigned distribution

The supplied NcNc EXE has Authenticode status `NotSigned`. It is a self-contained .NET WPF executable, and the portable ZIP also carries WolvenKit and runtime/native components. These facts may be relevant to a review but do not establish why Nexus quarantined the file. No recommendation to trust or whitelist the binary follows from this document.
