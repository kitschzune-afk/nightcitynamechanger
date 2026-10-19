using Microsoft.Win32;
using System.Diagnostics;
using System.Text.RegularExpressions;
namespace Ncnc;
public static class Game
{
    public static void Validate(string root)
    {
        if (!File.Exists(Path.Combine(root, "bin", "x64", "Cyberpunk2077.exe")) || !File.Exists(Path.Combine(root, "archive", "pc", "content", "lang_en_text.archive")))
            throw new DirectoryNotFoundException("Choose the Cyberpunk 2077 root folder containing bin\\x64\\Cyberpunk2077.exe and archive\\pc\\content\\lang_en_text.archive.");
    }
    public static List<string> Sources(string root)
    {
        Validate(root);
        return new[] { "content/lang_en_text.archive", "ep1/lang_en_text.archive", "content/basegame_4_gamedata.archive", "ep1/ep1_2_gamedata.archive" }
            .Select(x => Path.Combine(root, "archive", "pc", x.Replace('/', Path.DirectorySeparatorChar))).Where(File.Exists).ToList();
    }
    public static string Fingerprint(string root, string tool)
    {
        var sources = Sources(root).Append(Path.Combine(root, "bin", "x64", "Cyberpunk2077.exe")).Append(tool);
        return Storage.Hash("schema-2-standalone-v\n" + string.Join("\n", sources.Select(p => { var f = new FileInfo(p); return $"{f.FullName}|{f.Length}|{f.LastWriteTimeUtc.Ticks}"; })));
    }
    public static string Version(string root) => FileVersionInfo.GetVersionInfo(Path.Combine(root, "bin", "x64", "Cyberpunk2077.exe")).FileVersion ?? "unknown";
    public static List<string> Detect()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (OperatingSystem.IsWindows())
        {
            foreach (var hive in new[] { Registry.LocalMachine, Registry.CurrentUser })
            foreach (var key in new[] { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall" })
            {
                using var k = hive.OpenSubKey(key); if (k == null) continue;
                foreach (var sub in k.GetSubKeyNames()) { using var s = k.OpenSubKey(sub); if ((s?.GetValue("DisplayName") as string)?.Contains("Cyberpunk 2077", StringComparison.OrdinalIgnoreCase) == true && s?.GetValue("InstallLocation") is string p) paths.Add(p); }
            }
            var steam = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam")?.GetValue("SteamPath") as string;
            if (steam != null)
            {
                paths.Add(Path.Combine(steam, "steamapps/common/Cyberpunk 2077"));
                var vdf = Path.Combine(steam, "steamapps/libraryfolders.vdf");
                if (File.Exists(vdf)) foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\"")) paths.Add(Path.Combine(m.Groups[1].Value.Replace("\\\\", "\\"), "steamapps/common/Cyberpunk 2077"));
            }
            var epic = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Epic\EpicGamesLauncher\Data\Manifests");
            if (Directory.Exists(epic)) foreach (var f in Directory.GetFiles(epic, "*.item")) try { using var d = System.Text.Json.JsonDocument.Parse(File.ReadAllText(f)); if (d.RootElement.GetProperty("DisplayName").GetString()?.Contains("Cyberpunk") == true) paths.Add(d.RootElement.GetProperty("InstallLocation").GetString()!); } catch { }
        }
        return paths.Where(p => File.Exists(Path.Combine(p, "bin/x64/Cyberpunk2077.exe"))).ToList();
    }
}
