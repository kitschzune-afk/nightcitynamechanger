# 1.2.0 — Character chooser

- 26 character profiles with independent names, enable switches and review selections.
- Longest full-name matching and simultaneous replacements prevent duplicated names and cascades.
- Base-game/Phantom Liberty localization scanning, per-character previews and combined exports/builds.
- Migrates existing V settings and rebuilds old localization indexes.
- 43 game-free assertions passed; see RELEASE-1.2.0.md for integration validation.

# Changelog

## 1.1.0 — source-review snapshot

- Johnny default and NcNc icon retained.
- Standalone uppercase V matches selected by default across base/Phantom Liberty resources; embedded words and markup protected.
- Added literal game newline boundaries and protected unrelated full names in mixed V/name text.
- Added both character-creation pronoun descriptions and reviewed Phantom Liberty protagonist dossier/identification entries.
- Expanded default build to 5,433 variants / 839 resources in the tested installation.
- Normalized WolvenKit's JSON-only root HandleId during semantic validation; localization content and IDs still compared.
- This repository adds review documentation, artifact hashes and build/test scripts without modifying the application implementation.

## Earlier development

Initial implementation included WPF UI, scanning, explicit source-reviewed mappings, preview/approval, real WolvenKit archive generation, installation ownership checks and tests. Development history before this snapshot is not represented as individual Git commits.
