using Ncnc;
using System.Text.Json;
var cli = args.Length > 1 ? args[1] : "";
if (args.Length > 0 && args[0] == "index")
{
    var index = Scanner.Index(args[1], "research", Console.WriteLine, default);
    Storage.Write(args[2], index);
    return;
}
if (args.Length > 0 && args[0] == "integration")
{
    var game = args[2]; var data = args[3]; var reviewed = args[4];
    var tool = new Toolchain(cli, Console.WriteLine);
    var scanner = new Scanner(tool, Path.Combine(data, "cache"), reviewed, Console.WriteLine);
    var index = await scanner.Scan(game, default);
    Console.WriteLine(JsonSerializer.Serialize(index.Entries.GroupBy(e => e.Category).Select(g => new { Category = g.Key.ToString(), Candidates = g.Count(), Reviewed = g.Count(e => e.Include) })));
    Storage.Write(Path.Combine(data, "scan-report.json"), index);
    var build = await new Builder(tool, Path.Combine(data, "builds"), Console.WriteLine).Build(game, index, "Johnny", Enum.GetValues<Category>(), default);
    Storage.Write(Path.Combine(data, "latest-build.json"), build);
    var conflicts = await new Installer(tool, Console.WriteLine).Conflicts(game, build, default);
    Storage.Write(Path.Combine(data, "conflicts.json"), conflicts);
    Console.WriteLine($"CONFLICTS: {conflicts.Count}");
    return;
}
if (args.Length > 0 && args[0] == "install")
{
    var build = Storage.Read<BuildResult>(args[2]);
    await new Installer(new Toolchain(cli, Console.WriteLine), Console.WriteLine).Install(build.Game, build, default);
    return;
}
if (args.Length > 0 && args[0] == "build-tests")
{
    var tool = new Toolchain(cli, Console.WriteLine);
    var scanner = new Scanner(tool, Path.Combine(args[3], "cache"), args[4], Console.WriteLine);
    var index = await scanner.Scan(args[2], default);
    foreach (var row in index.Entries) row.Include = false;
    var selected = index.Entries.First(e => e.Category == Category.Dialogue && e.Original == "Hey, V! C'mere!"); selected.Include = true;
    var builder = new Builder(tool, Path.Combine(args[3], "regression-builds"), Console.WriteLine);
    var first = await builder.Build(args[2], index, "V Élodie O'Neil", [Category.Dialogue], default);
    var repeat = await builder.Build(args[2], index, "V Élodie O'Neil", [Category.Dialogue], default);
    if (first.Archive != repeat.Archive || first.Sha256 != repeat.Sha256 || first.Replacements != 1 || first.Resources.Count != 1) throw new Exception("Repeat/exclusion failure");
    Console.WriteLine("PASS Unicode punctuation name; single entry exclusion; repeated build cache; category-only resources");
    try { await builder.Build(args[2], index, "Jack", [Category.UI], default); throw new Exception("Disabled category included"); } catch (InvalidOperationException) { Console.WriteLine("PASS disabled category rejected"); }
    var changed = index with { Fingerprint = "stale" };
    try { await builder.Build(args[2], changed, "Jack", [Category.Dialogue], default); throw new Exception("Stale index used"); } catch (IOException) { Console.WriteLine("PASS stale index rejected"); }
    Console.WriteLine("6 build tests passed."); return;
}
if (args.Length > 0 && args[0] == "uninstall")
{
    new Installer(new Toolchain(cli, Console.WriteLine), Console.WriteLine).Uninstall(args[2]); return;
}
if (args.Length > 0 && args[0] == "installer-tests")
{
    var root = Path.GetFullPath(args[3]); Directory.CreateDirectory(root);
    var game = Path.Combine(root, Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(Path.Combine(game, "bin/x64")); Directory.CreateDirectory(Path.Combine(game, "archive/pc/content"));
    File.WriteAllText(Path.Combine(game, "bin/x64/Cyberpunk2077.exe"), "fixture");
    File.WriteAllText(Path.Combine(game, "archive/pc/content/lang_en_text.archive"), "fixture");
    var tool = new Toolchain(cli, Console.WriteLine); var installer = new Installer(tool, Console.WriteLine);
    var build = Storage.Read<BuildResult>(args[2]) with { Game = game, Fingerprint = Game.Fingerprint(game, tool.Executable) };
    await installer.Install(game, build, default);
    if (!installer.Status(game).StartsWith("Installed (")) throw new Exception("Install failed");
    await installer.Install(game, build, default);
    var dir = Path.Combine(game, "archive/pc/mod"); var unrelated = Path.Combine(dir, "unrelated.txt"); File.WriteAllText(unrelated, "preserve");
    installer.Uninstall(game);
    if (File.Exists(Path.Combine(dir, Installer.ArchiveName)) || File.ReadAllText(unrelated) != "preserve") throw new Exception("Uninstall failed");
    Console.WriteLine("PASS install, repeat install, uninstall, preserve unrelated file");
    File.Copy(build.Archive, Path.Combine(dir, "conflicting.archive"));
    try { await installer.Install(game, build, default); throw new Exception("Conflict not detected"); } catch (IOException e) when (e.Message.Contains("same resources")) { Console.WriteLine("PASS depot conflict blocked"); }
    File.Delete(Path.Combine(dir, "conflicting.archive"));
    File.WriteAllText(Path.Combine(dir, Installer.ArchiveName), "unowned");
    try { await installer.Install(game, build, default); throw new Exception("Unowned overwrite"); } catch (IOException e) when (e.Message.Contains("unowned")) { Console.WriteLine("PASS unowned archive protected"); }
    File.Delete(Path.Combine(dir, Installer.ArchiveName));
    await installer.Install(game, build, default);
    File.AppendAllText(Path.Combine(dir, Installer.ArchiveName), "external change");
    try { installer.Uninstall(game); throw new Exception("Modified file deleted"); } catch (IOException e) when (e.Message.Contains("changed externally")) { Console.WriteLine("PASS externally modified archive protected"); }
    File.AppendAllText(Path.Combine(game, "archive/pc/content/lang_en_text.archive"), "updated");
    try { await installer.Install(game, build, default); throw new Exception("Stale build installed"); } catch (IOException e) when (e.Message.Contains("version")) { Console.WriteLine("PASS game update invalidates build"); }
    Console.WriteLine("8 installer tests passed."); return;
}
int passed = 0;
void Check(string label, bool condition) { if (!condition) throw new Exception("FAIL " + label); Console.WriteLine("PASS " + label); passed++; }
void Reject(string label, Action action) { try { action(); } catch { Check(label, true); return; } throw new Exception("FAIL " + label); }
Check("single name", Replacement.Replace("V, come here!", "Alex") == "Alex, come here!");
Check("multi word", Replacement.Replace("Hello, V.", "Jack Mercer") == "Hello, Jack Mercer.");
Check("unicode", Replacement.Replace("V's car", "Élodie 李") == "Élodie 李's car");
Check("curly possessive", Replacement.Replace("V’s car", "James") == "James’s car");
Check("punctuation", Replacement.Replace("Hey V!", "O'Neil-Smith") == "Hey O'Neil-Smith!");
Check("case", Replacement.Replace("VINCENT / Valerie", "Jack Mercer") == "JACK MERCER / Jack Mercer");
Check("all caps appearance", Replacement.Replace("SET V'S APPEARANCE", "Johnny") == "SET JOHNNY'S APPEARANCE");
Check("alias", Replacement.Replace("V", "Johnny") == "Johnny");
Check("inventory", Replacement.Replace("V's Inventory", "Johnny") == "Johnny's Inventory");
Check("default name", new Settings().Name == "Johnny");
Check("male pronoun description", Replacement.Replace("Other characters will refer to V with male pronouns (i.e. he/him).", "Johnny", false) == "Other characters will refer to Johnny with male pronouns (i.e. he/him).");
Check("female pronoun description", Replacement.Replace("Other characters will refer to V with female pronouns (i.e. she/her).", "Johnny", false) == "Other characters will refer to Johnny with female pronouns (i.e. she/her).");
Check("literal newline boundaries", Replacement.Replace(@"Hello\nV\nV's jacket\tV\rV", "Johnny", false) == @"Hello\nJohnny\nJohnny's jacket\tJohnny\rJohnny");
Check("slash separated standalone names", Replacement.Replace("V/V, V—V; (V)", "Johnny", false) == "JOHNNY/JOHNNY, JOHNNY—JOHNNY; (JOHNNY)");
Check("preserve embedded unicode word", Replacement.Replace("Violet AVA ÉVé V2 V_foo", "Johnny", false) == "Violet AVA ÉVé V2 V_foo");
Check("mixed unrelated full name", Replacement.Replace("V, meet Vincent Apisa.", "Johnny", false) == "Johnny, meet Vincent Apisa.");
Check("full names need explicit approval", !Replacement.HasStandaloneV("Vincent Apisa and Valerie"));
var defaults = new IndexData("test", "test", [new() { Original = "Other characters will refer to V with male pronouns.", Category = Category.Unclassified }, new() { Resource = @"ep1\localization\en-us\subtitles\quest\q301\test.json", Original = "V, welcome to Dogtown.", Category = Category.Dialogue }, new() { Original = "Vincent Apisa", Category = Category.Messages }], 2, 3);
new Scanner(new Toolchain("unused.exe", _ => {}), "", "nonexistent-review-db.json", _ => {}).ApplyReview(defaults);
Check("pronoun UI automatically selected", defaults.Entries[0].Include && defaults.Entries[0].Category == Category.UI);
Check("Phantom Liberty dialogue automatically selected", defaults.Entries[1].Include && defaults.Entries[1].Category == Category.Dialogue);
Check("unrelated Vincent remains unchecked", !defaults.Entries[2].Include);
Check("unrelated letter", Replacement.Replace("TV V-Tech T.V. V.13 vector V8 IV", "Jack") == "TV V-Tech T.V. V.13 vector V8 IV");
Check("tokens", Replacement.Replace("<Rich name=\"V\">V</> {V} [V] $V$ %V LocKey#123", "Jack") == "<Rich name=\"V\">Jack</> {V} [V] $V$ %V LocKey#123");
Check("repeat from original", Replacement.Replace("V, hi", "V Mercer") == Replacement.Replace("V, hi", "V Mercer"));
Reject("reject markup", () => Replacement.ValidateName("<V>"));
Reject("reject newline", () => Replacement.ValidateName("Jack\nMercer"));
Reject("reject bidi", () => Replacement.ValidateName("Jack\u202e"));
Reject("reject empty", () => Replacement.ValidateName("  "));
Reject("reject traversal", () => Storage.Child(Path.GetTempPath(), "../escape"));
Reject("invalid game", () => Game.Validate(Path.GetTempPath()));
Reject("missing dependency", () => new Toolchain(Path.Combine(Path.GetTempPath(), "absent-ncnc.exe"), _ => {}).Run(default, "--version").GetAwaiter().GetResult());
Console.WriteLine($"{passed} tests passed.");
