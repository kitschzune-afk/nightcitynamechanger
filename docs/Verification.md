# Verification — version 1.1.0

Recorded 2026-10-19. Game executable file version 3.0.5294808, Steam build 20383525, English base game and Phantom Liberty resources. WolvenKit Console 9.1.0.

## Automated and resource checks

- 30 focused checks passed: whole-word matching, spaces/Unicode/punctuation, possessives, uppercase captions, default name, male/female pronoun descriptions, literal encoded newline boundaries, slash separators, embedded-word protection, formatting tokens, mixed V plus unrelated Vincent text, default expansion selection, validation and missing dependency errors.
- 6 build checks passed: Unicode/punctuation name, single-entry exclusion, repeated-build cache, category-only resources, disabled categories and stale index rejection.
- 8 installer checks passed in isolated fixtures: install, repeat install, uninstall, unrelated-file preservation, archive resource collision, unowned-file protection, externally modified-file protection and game-update invalidation.
- Real installed resources: 3,802 supported resources / 492,106 gender variants / 5,477 candidate variants.
- Default Johnny build: **5,433 selected variants in 839 compiled resources**. This includes **1,639 Phantom Liberty variants**, of which **1,163 are subtitle variants**. Counts include overlays and duplicate on-screen tables.
- 44 full-name-only candidate variants remain unselected; these include unrelated NPC name lists and other characters. The supplied reviewed mapping database covers 272 resource/ID/variant mappings.
- Generated archive unpacked successfully. Every packed binary matched its extracted SHA-256. All localization Data content matched after serialize/deserialize, excluding only the JSON-only root HandleId that WolvenKit renumbers. No text, localization ID or variant was omitted from comparison.
- No conflicting depot resources were detected in the filesystem state at installation. Earlier testing inspected 128 archives; the final count depends on the user's current mod deployment.
- Updated owned archive installed successfully. Original game archives unchanged.

## Exact final resource checks

Read from the archive after compilation, packing, extraction and serialization:

| ID | Text |
|---|---|
| 42833 | Other characters will refer to Johnny with male pronouns (i.e. he/him). |
| 42834 | Other characters will refer to Johnny with female pronouns (i.e. she/her). |
| 43105 | Johnny |
| 43311 | Johnny's Inventory |

Phantom Liberty mappings additionally cover the q303 protagonist dossiers and q302 Reed identification display for Vincent/Valerie. Subtitle resources are included from both expansion quests and expansion overlays.

## Visual/manual status

The initial WPF application was opened and its detection, scan, preview and progress UI were observed. Subsequent changes compiled successfully. The current executable reports 1.1.0.0 and embeds the NcNc multi-resolution icon. A Windows Start menu shortcut exists.

**No in-game display verification is claimed.** The previous computer-use attempt was stopped by the user; this update did not restart the game or resume computer control. Archive and resource checks establish what was packaged, not which resources a particular scene actually displays. Runtime-generated strings, mod-injected strings and text embedded in artwork remain outside the scanner.

## Remaining manual checks

After restarting the game with English text selected:

1. New Game: inspect lifepath, appearance, voice/pronoun explanations and final alias. Back out before beginning a new playthrough if you only want to test.
2. Vendor/inventory: inspect the inventory label and protagonist clothing names.
3. Base/Phantom Liberty: inspect subtitles addressing the protagonist, including an expansion quest rather than only base-game overlays.
4. Journal: inspect a quest description and objective mentioning the protagonist.
5. Messages/shards: inspect a phone conversation and the Phantom Liberty protagonist dossier.
6. Check long/Unicode names for clipping and font glyph coverage. Spoken audio should remain unchanged.

Use per-entry exclusions for unintended standalone initials/Roman numerals. Automatic V selection is intentionally broad at the user's request and does not establish that every selected V is the protagonist.
