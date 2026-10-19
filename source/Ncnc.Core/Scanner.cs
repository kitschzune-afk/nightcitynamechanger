using System.Text.Json;
using System.Text.Json.Nodes;
namespace Ncnc;
public sealed class Scanner(Toolchain tool, string cache, string reviewedPath, Action<string> log)
{
    public static JsonNode ReadNode(string path) => JsonNode.Parse(File.ReadAllText(path), documentOptions: new JsonDocumentOptions { MaxDepth = 256 })!;
    public static JsonArray? Entries(JsonNode node) => node["Data"]?["RootChunk"]?["root"]?["Data"]?["entries"] as JsonArray;
    public async Task<IndexData> Scan(string game, CancellationToken ct)
    {
        var fingerprint = Game.Fingerprint(game, tool.Executable);
        var root = Path.Combine(cache, fingerprint);
        var indexFile = Path.Combine(root, "index.json");
        if (File.Exists(indexFile)) { log("Reusing version-matched localization index."); return ApplyReview(Storage.Read<IndexData>(indexFile)); }
        var resources = Path.Combine(root, "resources");
        Directory.CreateDirectory(resources);
        foreach (var source in Game.Sources(game))
        {
            ct.ThrowIfCancellationRequested();
            await tool.Run(ct, "unbundle", source, "-o", resources, "-w", source.EndsWith("lang_en_text.archive") ? "*.json" : "*.journal");
        }
        // No -o: WolvenKit writes adjacent files and preserves depot directories.
        await tool.Run(ct, "convert", "serialize", resources);
        var result = Index(root, fingerprint, log, ct);
        if (result.Resources == 0 || result.Strings == 0) throw new InvalidDataException("No supported localization resources were extracted.");
        if (Game.Fingerprint(game, tool.Executable) != fingerprint) throw new IOException("Game files changed during scanning. Scan again after the update finishes.");
        Storage.Write(indexFile, result);
        return ApplyReview(result);
    }
    public IndexData ApplyReview(IndexData data)
    {
        var reviewed = File.Exists(reviewedPath) ? Storage.Read<List<Reviewed>>(reviewedPath).ToDictionary(x => x.Resource + "|" + x.Id + "|" + x.Field) : [];
        foreach (var entry in data.Entries)
        {
            entry.Include = Replacement.HasStandaloneV(entry.Original);
            entry.ReplaceFullNames = false;
            entry.Review = entry.Include ? "Standalone V selected; not in-game verified" : "Full name needs review";
            if (entry.Include && entry.Category == Category.Unclassified)
            {
                entry.Category = Category.UI;
                entry.Evidence += "; standalone V policy: unclassified on-screen text included under UI";
            }
            if (reviewed.TryGetValue(entry.Identity, out var r) && r.OriginalHash == Storage.Hash(entry.Original))
            { entry.Category = r.Category; entry.Evidence += "; " + r.Note; entry.Review = "Source reviewed; not in-game verified"; entry.Include = true; entry.ReplaceFullNames = true; }
        }
        return data;
    }
    public static IndexData Index(string root, string fingerprint, Action<string> log, CancellationToken ct)
    {
        var resources = Path.Combine(root, "resources");
        var refs = new Dictionary<string, HashSet<(Category Category, string Type)>>();
        foreach (var file in Directory.GetFiles(resources, "*.journal.json", SearchOption.AllDirectories))
            Walk(ReadNode(file), "", "", refs);
        var candidates = new List<Candidate>(); int strings = 0, count = 0;
        foreach (var file in Directory.GetFiles(resources, "*.json.json", SearchOption.AllDirectories).Order())
        {
            ct.ThrowIfCancellationRequested();
            var depot = Path.GetRelativePath(resources, file)[..^5].Replace('/', '\\');
            if (!depot.Contains(@"\localization\en-us\", StringComparison.OrdinalIgnoreCase)) continue;
            var node = ReadNode(file);
            var entries = Entries(node);
            if (entries == null) continue;
            var resourceType = node["Data"]?["RootChunk"]?["root"]?["Data"]?["$type"]?.ToString();
            if (resourceType is not ("localizationPersistenceSubtitleEntries" or "localizationPersistenceOnScreenEntries")) continue;
            count++;
            var subtitle = node["Data"]?["RootChunk"]?["root"]?["Data"]?["$type"]?.ToString() == "localizationPersistenceSubtitleEntries";
            foreach (var entry in entries.OfType<JsonObject>())
            {
                var id = (entry[subtitle ? "stringId" : "primaryKey"] ?? throw new InvalidDataException("Missing localization ID")).ToString();
                var key = entry["secondaryKey"]?.ToString() ?? "";
                var category = subtitle ? Category.Dialogue : Category.Unclassified;
                var evidence = subtitle ? "Subtitle resource / stringId" : "No unique journal link; manual classification required";
                if (!subtitle && refs.TryGetValue(id, out var matches))
                {
                    var categories = matches.Select(m => m.Category).Distinct().ToArray();
                    if (categories.Length == 1) category = categories[0];
                    evidence = string.Join(", ", matches.Select(m => m.Type).Distinct());
                }
                if (!subtitle && key.StartsWith("UI-", StringComparison.OrdinalIgnoreCase)) { category = Category.UI; evidence = "UI localization key"; }
                foreach (var field in new[] { "femaleVariant", "maleVariant" })
                {
                    var original = entry[field]?.ToString() ?? ""; strings++;
                    if (original.Length == 0 || !Replacement.IsCandidate(original)) continue;
                    candidates.Add(new() { Resource = depot, Id = id, SecondaryKey = key, Field = field, Original = original, Category = category, Evidence = evidence });
                }
            }
        }
        log($"Indexed {count:N0} resources / {strings:N0} variants; {candidates.Count:N0} candidate references.");
        return new(fingerprint, root, candidates, count, strings);
    }
    private static void Walk(JsonNode? node, string type, string field, Dictionary<string, HashSet<(Category, string)>> refs)
    {
        if (node is JsonObject obj)
        {
            type = obj["$type"]?.ToString() ?? type;
            foreach (var kv in obj) Walk(kv.Value, type, kv.Key is "value" or "unk1" ? field : kv.Key, refs);
        }
        else if (node is JsonArray array) foreach (var n in array) Walk(n, type, field, refs);
        else if (node is JsonValue value && value.TryGetValue<string>(out var text) && text.StartsWith("LocKey#"))
        {
            // Only textual content fields. Sender/recipient/contact identities are never mapped automatically.
            Category category;
            if (type.StartsWith("gameJournalQuest") && field is "description" or "title" or "text") category = Category.Journal;
            else if ((type is "gameJournalPhoneMessage" or "gameJournalPhoneChoiceEntry" or "gameJournalEmail" or "gameJournalFile" or "gameJournalOnscreen" or "gameJournalInternetText") && field is "text" or "body" or "content" or "title") category = Category.Messages;
            else return;
            var id = text[7..];
            if (!refs.TryGetValue(id, out var set)) refs[id] = set = [];
            set.Add((category, type + "." + field));
        }
    }
}
