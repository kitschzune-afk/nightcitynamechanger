namespace Ncnc;

public sealed record Character(string Id, string Display, params string[] Aliases);
public sealed class CharacterRename
{
    public string Name { get; set; } = "";
    public bool Enabled { get; set; }
}
public static class Characters
{
    public static readonly Character[] All =
    [
        new("v", "V — protagonist", "V", "Vincent", "Valerie"),
        new("johnny", "Johnny Silverhand", "Johnny Silverhand", "Johnny", "Silverhand"),
        new("rogue", "Rogue Amendiares", "Rogue Amendiares", "Rogue", "Amendiares"),
        new("panam", "Panam Palmer", "Panam Palmer", "Panam"),
        new("judy", "Judy Alvarez", "Judy Alvarez", "Judy"),
        new("jackie", "Jackie Welles", "Jackie Welles", "Jackie"),
        new("kerry", "Kerry Eurodyne", "Kerry Eurodyne", "Kerry", "Eurodyne"),
        new("river", "River Ward", "River Ward", "River"),
        new("takemura", "Goro Takemura", "Goro Takemura", "Goro", "Takemura"),
        new("vik", "Viktor Vektor", "Viktor Vektor", "Viktor", "Vik"),
        new("misty", "Misty Olszewski", "Misty Olszewski", "Misty"),
        new("evelyn", "Evelyn Parker", "Evelyn Parker", "Evelyn"),
        new("dexter", "Dexter DeShawn", "Dexter DeShawn", "Dexter", "DeShawn", "Dex"),
        new("tbug", "T-Bug", "T-Bug"),
        new("hanako", "Hanako Arasaka", "Hanako Arasaka", "Hanako"),
        new("yorinobu", "Yorinobu Arasaka", "Yorinobu Arasaka", "Yorinobu"),
        new("saburo", "Saburo Arasaka", "Saburo Arasaka", "Saburo"),
        new("smasher", "Adam Smasher", "Adam Smasher", "Smasher"),
        new("alt", "Alt Cunningham", "Alt Cunningham", "Alt"),
        new("saul", "Saul Bright", "Saul Bright", "Saul"),
        new("mitch", "Mitch Anderson", "Mitch Anderson", "Mitch"),
        new("songbird", "Songbird / Song So Mi", "Song So Mi", "Songbird", "So Mi"),
        new("reed", "Solomon Reed", "Solomon Reed", "Solomon", "Reed"),
        new("alex", "Alex / Alena Xenakis", "Alena Xenakis", "Alena", "Alex"),
        new("myers", "Rosalind Myers", "Rosalind Myers", "Rosalind", "Myers"),
        new("hansen", "Kurt Hansen", "Kurt Hansen", "Hansen")
    ];
    public static Character Get(string id) => All.FirstOrDefault(c => c.Id == id) ?? throw new ArgumentException("Unknown character: " + id);
    public static void Initialize(Settings settings)
    {
        foreach (var c in All)
            settings.Renames.TryAdd(c.Id, new() { Name = c.Id == "v" ? settings.Name : c.Aliases[0], Enabled = c.Id == "v" });
    }
}
