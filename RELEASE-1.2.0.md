# NcNc 1.2.0 — character chooser

Released 2026-10-20. This update adds 26 character profiles and simultaneous name replacements. The original 1.1.0 quarantine/review evidence is retained separately in Git history and the original hash/inventory files.

## Use

1. Extract the entire portable ZIP, keeping the app/tools folder with the executable.
2. Open app/NightCityNameChanger.exe. Choose a character, enter its new name and enable it. Repeat for any other characters. Only V is enabled initially, with Johnny as its default replacement.
3. Scan, select each character to review its text, and exclude unrelated matches. Approve all applies to the current character in enabled categories.
4. Generate combines every enabled character in one archive. Install uses the existing ownership/conflict checks. A new install replaces the previous owned mod, so include every wanted rename before generating.

The visible preview concerns one character. Export preview writes combined substitutions for all enabled characters. Names are replaced from original text in one pass, without cascading. The aliases displayed beside the chooser define what gets matched; full names are consumed before short aliases. Shared surnames and unknown nicknames are not automatically inferred.

## Validation

- 43 game-free assertions passed, including the existing V regression tests and new tests for Johnny/Rogue aliases, uppercase/possessives, embedded words, markup, literal newlines, simultaneous swaps, shared surname protection, expansion aliases, settings migration and independent selection identities.
- Release WPF build and self-contained Windows x64 publish succeeded.
- Real game extraction indexed 3,802 resources and 492,106 variants, producing 18,348 character/reference candidates. A single field may be a candidate for more than one character.
- Johnny: 1,477 candidates (264 in Phantom Liberty). Rogue: 596 (55 in Phantom Liberty).
- Combined V -> Johnny, Johnny -> Robert, Rogue -> Jane build changed 7,453 distinct text fields in 910 resources. Packed archive extraction hashes and JSON semantic round trips passed. Repeating the same build reused the validated result.
- Integration archive SHA-256: 2AA521BE00971D7175D8413959B55D331D4F6E5A861CD701380965C2C3549C36. This is a test archive; the portable download does not contain game-derived resources.
- No new test archive was installed into the live game. Full in-game visual coverage is not verified.

## Limits

This changes supported English text only. It does not replace recorded speech, names baked into artwork, other languages, unknown aliases or runtime-generated text. Title-case and uppercase aliases match; lowercase ordinary words are left alone. Names in markup are protected. Capitalized names can still refer to unrelated people or ordinary words; review the matches. This does not change character appearance, pronouns, voice or quest identity.

The application remains unsigned. This feature release does not establish antivirus clearance or resolve the reported Nexus quarantine. Dependencies are unchanged; see REVIEWER_GUIDE.md and SECURITY_REVIEW.md.

Desktop UI smoke test passed: Johnny selection, name editing/auto-enable, scanning and item-name preview, and switching to Rogue's independent 596-entry preview. Original user settings were restored afterward.

