# Technical design and compatibility

## Observed architecture

Research used installed Steam archives, WolvenKit Console 9.1.0, and the upstream documentation linked below. The game executable reported file version `3.0.5294808`; the Steam manifest reported build `20383525`; WolvenKit's CR2W JSON header reported `GameVersion: 2310`. These identifiers are recorded as observed, not treated as interchangeable version numbers.

English text is in `archive/pc/content/lang_en_text.archive` and, when installed, `archive/pc/ep1/lang_en_text.archive`. Resources use depot paths beneath `base/localization/en-us` and `ep1/localization/en-us`. A resource with the extension `.json` is actually a CR2W binary. WolvenKit's editable serialization adds another `.json` suffix.

The `JsonResource` root handle contains either `localizationPersistenceSubtitleEntries` (`stringId`) or `localizationPersistenceOnScreenEntries` (`primaryKey`, `secondaryKey`). Both use `femaleVariant` and optional `maleVariant`. Other JSON resource types, including index structures, are not treated as text entries. IDs remain strings, preserving full 64-bit precision.

The cooked journal is a different CR2W resource (`gameJournalResource`), extracted from base/expansion gamedata archives. Its typed `LocKey#...` references classify text fields. Sender, recipient, contact and other identity fields are not automatically mapped as message content. Classification can remain unknown when links are missing or ambiguous. UI mappings reviewed in this project have explicit resource/ID/variant/original-hash records.

## Workflow validated against installed resources

```text
WolvenKit.CLI unbundle <archive> -o <resources> -w *.json
WolvenKit.CLI unbundle <gamedata-archive> -o <resources> -w *.journal
WolvenKit.CLI convert serialize <resources>
WolvenKit.CLI convert deserialize <edited-serialization-directory>
WolvenKit.CLI pack <000_ncnc_6f841e79-directory> -o <output>
WolvenKit.CLI unbundle <generated-archive> -o <verification-directory>
WolvenKit.CLI convert serialize <verification-directory>
```

Serialization is deliberately done **without `-o`**, because the tested CLI flattens output paths when that argument is used. Adjacent serialization preserves depot paths. Only modified compiled resources go into the packed directory. Binary extraction hashes and the serialized `Data` tree are compared after the round trip; IDs, unrelated entries and metadata must match. Header export timestamps and the JSON-only root HandleId are excluded from semantic comparisons. WolvenKit can renumber that root handle (observed 1 to 0 in q113_06_saburo_office); these flat localization arrays have no references to it. No localization IDs, text, variants or other Data properties are excluded.

The mod overrides original depot paths using the legacy `archive/pc/mod` mechanism. This avoids adding a framework dependency, at the cost of whole-resource conflicts. It does not use ArchiveXL merge injection. No journal progression structures or voice recordings are written.

## Replacement and review

English matching is Unicode-boundary-aware and case-sensitive for V, Vincent, Valerie and uppercase full names. It protects common markup tags, placeholders, localization tokens and identifiers. Apostrophe suffixes remain intact; all-uppercase strings produce uppercase names. Original strings are always the input, so repeated generation does not apply replacement to already edited text. Arbitrary names are serialized as JSON values and never used as paths or shell commands. Control/bidi characters and markup delimiters are rejected.

Whole-word matching alone cannot establish identity. As requested, version 1.1 selects every standalone uppercase V candidate automatically across base and expansion resources; unknown on-screen categories fall under UI. This can include standalone initials or Roman numerals. V within words, model names/abbreviations such as V-Tech or T.V., and formatting tokens is protected. Literal escaped newline/tab boundaries are recognized without altering their encoding.

`reviewed.json` binds full-name approvals to the exact resource, ID, gender variant and original-text SHA-256. Auto-selected V entries do not implicitly authorize Vincent/Valerie substitutions in the same sentence. Full-name-only candidates remain unchecked unless source-reviewed or approved by the user. Individual approvals and exclusions persist. Bulk approval explicitly permits all candidate name substitutions; it never replaces V inside ordinary words. Cache/build schema versions prevent reuse of results from the old selection rules.

## Cache and installation

Cache identity includes absolute game/tool paths, source sizes and modification timestamps, executable metadata, and schema revision. Build identity additionally includes name and selected entry identities/original hashes. Same selection reuses an already validated archive after rechecking its SHA-256. Metadata-based invalidation does not detect an adversarial same-size/same-timestamp change; ordinary game updates invalidate it.

The builder uses a unique staging directory per attempt and never packs stale partial outputs. External processes use `ProcessStartInfo.ArgumentList`, asynchronous output drains, hidden windows, exit/error checks and cancellation with process-tree termination.

Install/uninstall use an application-specific per-game mutex, ownership manifest and hashes. Archive replacement is staged in the target directory and renamed atomically. A `.pending` record is written during commit; synchronous failures attempt rollback. If a power loss leaves a pending record or inconsistent manifest, retain it for diagnosis; this release does not automatically recover a torn two-file commit. The installer refuses unknown ownership or changed hashes rather than deleting uncertain files.

Directory junctions/symlinks are rejected for installation. Game updates and a running game block installation. The tool does not request elevation automatically; access-denied failures are logged and shown. Steam registry/libraryfolders, GOG uninstall registration and Epic manifests are detection inputs; only the local Steam installation was exercised here.

## Source references

- [WolvenKit CLI commands](https://wiki.redmodding.org/wolvenkit/wolvenkit-cli/usage/command-list)
- [Translation resource structure](https://wiki.redmodding.org/cyberpunk-2077-modding/for-mod-creators-theory/files-and-what-they-do/file-formats/translation-files--json)
- [Localization editing workflow](https://wiki.redmodding.org/cyberpunk-2077-modding/modding-guides/everything-else/how-to-translate-a-mod)
- [Archive load order](https://wiki.redmodding.org/cyberpunk-2077-modding/for-mod-users/users-modding-cyberpunk-2077/load-order)
- [CDPR REDmod documentation, including legacy archive support](https://cdn-l-cyberpunk.cdprojektred.com/REDmod-docs.pdf)
- [WolvenKit 9.1.0 source and release](https://github.com/WolvenKit/WolvenKit/releases/tag/9.1.0)
