using System.Text.Json.Nodes;
namespace Ncnc;
public sealed class Builder(Toolchain tool, string builds, Action<string> log)
{
    public async Task<BuildResult> Build(string game, IndexData index, string name, IEnumerable<Category> categories, CancellationToken ct)
    {
        name = Replacement.ValidateName(name);
        if (Game.Fingerprint(game, tool.Executable) != index.Fingerprint) throw new IOException("Game or toolchain changed. Rescan before generating.");
        var chosenCategories = categories.ToHashSet();
        var chosen = index.Entries.Where(e => e.Include && chosenCategories.Contains(e.Category) && e.Category != Category.Unclassified).OrderBy(e => e.Identity).ToList();
        if (chosen.Count == 0) throw new InvalidOperationException("Select at least one reviewed, classified replacement in the preview.");
        var signature = Storage.Hash("builder-2" + index.Fingerprint + name + string.Join("\n", chosen.Select(e => e.Identity + e.ReplaceFullNames + Storage.Hash(e.Original))));
        var buildDir = Path.Combine(builds, signature);
        var resultFile = Path.Combine(buildDir, "build.json");
        if (File.Exists(resultFile))
        {
            var previous = Storage.Read<BuildResult>(resultFile);
            if (File.Exists(previous.Archive) && Storage.FileHash(previous.Archive) == previous.Sha256) { log("Reusing validated identical build."); return previous; }
        }
        // Unique staging directory: failed previous attempts cannot contribute stale files.
        var stage = Path.Combine(buildDir, Guid.NewGuid().ToString("N"));
        var pack = Path.Combine(stage, Installer.ArchiveName[..^8]);
        var exports = Path.Combine(stage, "export");
        Directory.CreateDirectory(pack); Directory.CreateDirectory(exports);
        var changedResources = new List<string>();
        foreach (var group in chosen.GroupBy(e => e.Resource))
        {
            var source = Storage.Child(Path.Combine(index.Root, "resources"), group.Key + ".json");
            var document = Scanner.ReadNode(source);
            var entries = Scanner.Entries(document) ?? throw new InvalidDataException("Missing entries");
            var byId = entries.OfType<JsonObject>().GroupBy(e => (e["stringId"] ?? e["primaryKey"])?.ToString() ?? "").ToDictionary(g => g.Key, g => g.ToArray());
            foreach (var item in group)
            {
                if (!byId.TryGetValue(item.Id, out var matches) || matches.Length != 1) throw new InvalidDataException("Duplicate or missing ID: " + item.Identity);
                var entry = matches[0];
                if (entry[item.Field]?.ToString() != item.Original) throw new InvalidDataException("Original text changed: " + item.Identity);
                entry[item.Field] = Replacement.Replace(item.Original, name, item.ReplaceFullNames);
            }
            var dest = Storage.Child(exports, group.Key + ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.WriteAllText(dest, document.ToJsonString(Storage.Json));
            changedResources.Add(group.Key);
        }
        await tool.Run(ct, "convert", "deserialize", exports);
        foreach (var resource in changedResources)
        {
            var binary = Storage.Child(exports, resource);
            using (var s = File.OpenRead(binary)) { var magic = new byte[4]; s.ReadExactly(magic); if (System.Text.Encoding.ASCII.GetString(magic) != "CR2W") throw new InvalidDataException("Not a compiled REDengine resource: " + resource); }
            var dest = Storage.Child(pack, resource); Directory.CreateDirectory(Path.GetDirectoryName(dest)!); File.Copy(binary, dest);
        }
        var packed = Path.Combine(stage, "packed"); Directory.CreateDirectory(packed);
        await tool.Run(ct, "pack", pack, "-o", packed);
        var archive = Path.Combine(packed, Installer.ArchiveName);
        if (!File.Exists(archive) || new FileInfo(archive).Length < 40) throw new InvalidDataException("WolvenKit did not produce an archive.");
        var verify = Path.Combine(stage, "verify"); Directory.CreateDirectory(verify);
        await tool.Run(ct, "unbundle", archive, "-o", verify);
        var extracted = Directory.GetFiles(verify, "*", SearchOption.AllDirectories);
        if (extracted.Length != changedResources.Count) throw new InvalidDataException("Archive resource count mismatch.");
        foreach (var resource in changedResources)
            if (Storage.FileHash(Storage.Child(pack, resource)) != Storage.FileHash(Storage.Child(verify, resource))) throw new InvalidDataException("Archive round-trip mismatch: " + resource);
        await tool.Run(ct, "convert", "serialize", verify);
        foreach (var resource in changedResources)
        {
            var expected = Scanner.ReadNode(Storage.Child(exports, resource + ".json"))["Data"];
            var actual = Scanner.ReadNode(Storage.Child(verify, resource + ".json"))["Data"];
            NormalizeRootHandle(expected); NormalizeRootHandle(actual);
            if (!JsonNode.DeepEquals(expected, actual)) throw new InvalidDataException("Compiled resource semantic round-trip mismatch: " + resource);
        }
        if (Game.Fingerprint(game, tool.Executable) != index.Fingerprint) throw new IOException("Game files changed during build. Rescan.");
        var result = new BuildResult(archive, Storage.FileHash(archive), index.Fingerprint, Path.GetFullPath(game), changedResources, chosen.Count);
        Storage.Write(resultFile, result);
        Storage.Write(Path.Combine(buildDir, "replacements.json"), chosen.Select(e => new { e.Resource, e.Id, e.Field, e.Category, e.Original, Replacement = Replacement.Replace(e.Original, name, e.ReplaceFullNames), e.Review, InGameVerified = false }));
        log($"Validated archive: {chosen.Count:N0} replacements in {changedResources.Count:N0} resources. In-game verification remains separate.");
        return result;
    }
    private static void NormalizeRootHandle(JsonNode? data)
    {
        // WKit renumbers its JSON-only root handle during serialization (e.g. 1 -> 0).
        // These flat localization arrays have no references to that root handle.
        if (data?["RootChunk"]?["root"] is JsonObject root &&
            root["Data"]?["$type"]?.ToString() is "localizationPersistenceSubtitleEntries" or "localizationPersistenceOnScreenEntries")
            root.Remove("HandleId");
    }
}
