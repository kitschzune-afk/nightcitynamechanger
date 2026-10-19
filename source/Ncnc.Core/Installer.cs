using System.Diagnostics;
namespace Ncnc;
public sealed class Installer(Toolchain tool, Action<string> log)
{
    public const string ArchiveName = "000_ncnc_6f841e79.archive";
    private const string Owner = "NightCityNameChanger/6f841e79";
    private const string ManifestName = "ncnc_6f841e79.manifest.json";
    private static string ModDir(string game) => Path.Combine(Path.GetFullPath(game), "archive", "pc", "mod");
    private static void SafeDirectory(string dir)
    {
        for (var p = new DirectoryInfo(dir); p != null; p = p.Parent)
            if (p.Exists && (p.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Installation through a directory junction or symbolic link is not supported: " + p.FullName);
    }
    private static void GameClosed()
    {
        if (Process.GetProcessesByName("Cyberpunk2077").Length > 0) throw new IOException("Close Cyberpunk 2077 before installing or uninstalling.");
    }
    private static InstallManifest? Owned(string game)
    {
        var dir = ModDir(game); var path = Path.Combine(dir, ManifestName);
        if (!File.Exists(path)) return null;
        var m = Storage.Read<InstallManifest>(path);
        if (m.Owner != Owner || m.FileName != ArchiveName || !string.Equals(m.Game, Path.GetFullPath(game), StringComparison.OrdinalIgnoreCase)) throw new IOException("Installation manifest does not belong to this application/game.");
        return m;
    }
    public string Status(string game)
    {
        try
        {
            var m = Owned(game); var file = Path.Combine(ModDir(game), ArchiveName);
            if (m == null) return File.Exists(file) ? "Conflict: unowned file uses the reserved mod name" : "Not installed";
            return !File.Exists(file) ? "Manifest found; archive missing" : Storage.FileHash(file) == m.Sha256 ? "Installed (hash verified)" : "Installed file changed externally — protected";
        }
        catch (Exception e) { return e.Message; }
    }
    public async Task<List<string>> Conflicts(string game, BuildResult build, CancellationToken ct)
    {
        var archives = new List<string>();
        var legacy = ModDir(game);
        if (Directory.Exists(legacy)) archives.AddRange(Directory.GetFiles(legacy, "*.archive", SearchOption.AllDirectories).Where(p => !string.Equals(p, Path.Combine(legacy, ArchiveName), StringComparison.OrdinalIgnoreCase)));
        var redmods = Path.Combine(game, "mods");
        if (Directory.Exists(redmods)) archives.AddRange(Directory.GetFiles(redmods, "*.archive", SearchOption.AllDirectories));
        if (archives.Count == 0) return [];
        log($"Checking {archives.Count} existing archives for depot resource collisions.");
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Bound command line size and avoid loading every mod into memory simultaneously.
        foreach (var batch in archives.Chunk(30))
        {
            var output = await tool.Run(ct, new[] { "archive" }.Concat(batch).Concat(new[] { "-l" }).ToArray());
            foreach (var resource in build.Resources)
                if (output.Contains(resource, StringComparison.OrdinalIgnoreCase) || output.Split('\n').Any(l => l.Trim() == Fnv(resource).ToString())) found.Add(resource);
        }
        return found.ToList();
    }
    public static ulong Fnv(string value) { ulong hash = 14695981039346656037; foreach (byte b in System.Text.Encoding.UTF8.GetBytes(value.ToLowerInvariant().Replace('/', '\\'))) { hash ^= b; hash *= 1099511628211; } return hash; }
    public async Task Install(string game, BuildResult build, CancellationToken ct)
    {
        Game.Validate(game); GameClosed();
        if (!string.Equals(Path.GetFullPath(game), build.Game, StringComparison.OrdinalIgnoreCase) || Game.Fingerprint(game, tool.Executable) != build.Fingerprint) throw new IOException("Build belongs to another game installation/version. Rescan and regenerate.");
        if (Storage.FileHash(build.Archive) != build.Sha256) throw new IOException("Generated archive changed after validation.");
        var conflicts = await Conflicts(game, build, ct);
        if (conflicts.Count > 0) throw new IOException("Existing mods override these same resources. Resolve the conflict before installing:\n" + string.Join("\n", conflicts));
        var dir = ModDir(game); SafeDirectory(dir); Directory.CreateDirectory(dir);
        var target = Path.Combine(dir, ArchiveName); var manifest = Path.Combine(dir, ManifestName);
        using var mutex = new Mutex(false, "Local\\NCNC_" + Storage.Hash(Path.GetFullPath(game)));
        bool locked; try { locked = mutex.WaitOne(0); } catch (AbandonedMutexException) { locked = true; }
        if (!locked) throw new IOException("Another installation operation is running.");
        try
        {
            GameClosed(); ct.ThrowIfCancellationRequested();
            var old = Owned(game);
            if (File.Exists(target) && (old == null || Storage.FileHash(target) != old.Sha256)) throw new IOException("Refusing to overwrite an unowned or externally changed archive.");
            // Keep a recovery record until both atomic file replacements are complete.
            var next = new InstallManifest(Owner, Path.GetFullPath(game), ArchiveName, build.Sha256, DateTime.UtcNow);
            var staged = Path.Combine(dir, ArchiveName + "." + Guid.NewGuid().ToString("N") + ".tmp");
            File.Copy(build.Archive, staged);
            if (Storage.FileHash(staged) != build.Sha256) { File.Delete(staged); throw new IOException("Staged archive checksum mismatch."); }
            var oldBytes = File.Exists(target) ? File.ReadAllBytes(target) : null;
            try
            {
                Storage.Write(manifest + ".pending", new { Old = old, Next = next });
                File.Move(staged, target, true);
                Storage.Write(manifest, next);
                File.Delete(manifest + ".pending");
            }
            catch
            {
                if (oldBytes != null) File.WriteAllBytes(target, oldBytes); else if (File.Exists(target) && Storage.FileHash(target) == next.Sha256) File.Delete(target);
                if (old != null) Storage.Write(manifest, old); else if (File.Exists(manifest)) File.Delete(manifest);
                throw;
            }
            finally { if (File.Exists(staged)) File.Delete(staged); }
            log("Installed owned archive; original game archives untouched.");
        }
        finally { mutex.ReleaseMutex(); }
    }
    public void Uninstall(string game)
    {
        Game.Validate(game); GameClosed(); var dir = ModDir(game); SafeDirectory(dir);
        using var mutex = new Mutex(false, "Local\\NCNC_" + Storage.Hash(Path.GetFullPath(game)));
        bool locked; try { locked = mutex.WaitOne(0); } catch (AbandonedMutexException) { locked = true; }
        if (!locked) throw new IOException("Another installation operation is running.");
        try
        {
            var old = Owned(game); if (old == null) { log("No owned installation found."); return; }
            var target = Path.Combine(dir, ArchiveName);
            if (File.Exists(target)) { if (Storage.FileHash(target) != old.Sha256) throw new IOException("Archive changed externally; refusing to delete it."); File.Delete(target); }
            File.Delete(Path.Combine(dir, ManifestName));
            log("Uninstalled only the owned archive and manifest.");
        }
        finally { mutex.ReleaseMutex(); }
    }
}
