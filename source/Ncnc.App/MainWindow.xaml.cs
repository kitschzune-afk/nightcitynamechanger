using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using System.IO;
namespace Ncnc;
public partial class MainWindow : Window
{
    private readonly string data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NightCityNameChanger");
    private Settings settings = new();
    private IndexData? index;
    private BuildResult? build;
    private CancellationTokenSource? cancellation;
    private bool ready;
    private string SelectedCharacter => (CharacterBox.SelectedItem as Character)?.Id ?? "v";
    private void StoreCharacter()
    {
        var rename = settings.Renames[SelectedCharacter];
        rename.Name = NameBox.Text; rename.Enabled = RenameEnabled.IsChecked == true;
        settings.SelectedCharacter = SelectedCharacter;
        settings.Name = settings.Renames["v"].Name;
    }
    private string Config => Path.Combine(data, "settings.json");
    public MainWindow()
    {
        InitializeComponent(); Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/Assets/NcNc.ico")); Directory.CreateDirectory(data);
        try { if (File.Exists(Config)) settings = Storage.Read<Settings>(Config); } catch (Exception e) { Log("Could not read settings: " + e.Message); }
        if (string.IsNullOrEmpty(settings.Tool)) settings.Tool = Path.Combine(AppContext.BaseDirectory, "tools", "WolvenKit.CLI.exe");
        if (string.IsNullOrEmpty(settings.Game)) settings.Game = Game.Detect().FirstOrDefault() ?? "";
        Characters.Initialize(settings);
        CharacterBox.ItemsSource = Characters.All;
        CharacterBox.SelectedItem = Characters.All.FirstOrDefault(c => c.Id == settings.SelectedCharacter) ?? Characters.All[0];
        LoadCharacter();
        GameBox.Text = settings.Game; ToolBox.Text = settings.Tool;
        Dialogue.IsChecked = settings.Categories.Contains(Category.Dialogue); Journal.IsChecked = settings.Categories.Contains(Category.Journal); Messages.IsChecked = settings.Categories.Contains(Category.Messages); UI.IsChecked = settings.Categories.Contains(Category.UI);
        AssignCategory.ItemsSource = new[] { Category.Dialogue, Category.Journal, Category.Messages, Category.UI }; AssignCategory.SelectedIndex = 0;
        ready = true; UpdateStatus();
        Closing += (_, e) => { if (cancellation != null) { e.Cancel = true; cancellation.Cancel(); Log("Cancelling operation. Close again when it finishes."); } else Save(); };
    }
    private List<Category> Categories() => new[] { (Dialogue, Category.Dialogue), (Journal, Category.Journal), (Messages, Category.Messages), (UI, Category.UI) }.Where(x => x.Item1.IsChecked == true).Select(x => x.Item2).ToList();
    private Toolchain Tool() => new(ToolBox.Text, Log);
    private void Save()
    {
        StoreCharacter(); settings.Game = GameBox.Text; settings.Tool = ToolBox.Text; settings.Categories = Categories();
        if (index != null) settings.Excluded = index.Entries.Where(e => !e.Include).Select(e => e.SelectionIdentity).ToList();
        try { Storage.Write(Config, settings); } catch (Exception e) { Log("Settings could not be saved: " + e.Message); }
    }
    private void Log(string text)
    {
        if (!Dispatcher.CheckAccess()) { Dispatcher.BeginInvoke(() => Log(text)); return; }
        var line = $"[{DateTime.Now:HH:mm:ss}] {text}";
        LogBox.AppendText(line + Environment.NewLine); LogBox.ScrollToEnd();
        try { File.AppendAllText(Path.Combine(data, "operations.log"), line + Environment.NewLine); } catch { }
    }
    private async Task Operation(string title, Func<CancellationToken, Task> action)
    {
        if (cancellation != null) return;
        cancellation = new(); Inputs.IsEnabled = CategoriesPanel.IsEnabled = Actions.IsEnabled = Preview.IsEnabled = false; CancelButton.IsEnabled = true;
        Progress.IsIndeterminate = true; Status.Text = title; Log(title);
        try { Save(); await action(cancellation.Token); Status.Text = "Completed • " + title; }
        catch (OperationCanceledException) { Status.Text = "Operation cancelled. Original game archives unchanged."; Log(Status.Text); }
        catch (Exception e) { Status.Text = e.Message; Log(e.ToString()); MessageBox.Show(this, e.Message, "Night City Name Changer", MessageBoxButton.OK, MessageBoxImage.Warning); }
        finally { cancellation.Dispose(); cancellation = null; Inputs.IsEnabled = CategoriesPanel.IsEnabled = Actions.IsEnabled = Preview.IsEnabled = true; CancelButton.IsEnabled = false; Progress.IsIndeterminate = false; Progress.Value = 100; UpdateStatus(); }
    }
    private async void ScanClick(object sender, RoutedEventArgs e) => await Operation("Scanning English localization…", async ct =>
    {
        var game = GameBox.Text; var tool = Tool();
        index = await Task.Run(() => new Scanner(tool, Path.Combine(data, "cache"), Path.Combine(AppContext.BaseDirectory, "reviewed.json"), Log).Scan(game, ct), ct);
        foreach (var approval in settings.Approvals)
        {
            var row = index.Entries.FirstOrDefault(r => r.CharacterId == approval.CharacterId && r.Resource == approval.Resource && r.Id == approval.Id && r.Field == approval.Field && Storage.Hash(r.Original) == approval.OriginalHash);
            if (row != null) { row.Include = true; row.ReplaceFullNames = true; row.Category = approval.Category; row.Review = "User approved; not in-game verified"; }
        }
        foreach (var row in index.Entries)
            if (settings.Excluded.Contains(row.SelectionIdentity) || (row.CharacterId == "v" && settings.Excluded.Contains(row.Identity))) row.Include = false;
        build = null; Preview.ItemsSource = index.Entries; Refresh();
    });
    private async void GenerateClick(object sender, RoutedEventArgs e) => await Operation("Generating and validating archive…", async ct =>
    {
        if (index == null) throw new InvalidOperationException("Scan and review the replacements first.");
        Preview.CommitEdit(); StoreCharacter(); var game = GameBox.Text; var categories = Categories(); var tool = Tool();
        build = await Task.Run(() => new Builder(tool, Path.Combine(data, "builds"), Log).Build(game, index, settings.Renames, categories, ct), ct);
        Log("Output: " + build.Archive);
    });
    private async void InstallClick(object sender, RoutedEventArgs e) => await Operation("Checking conflicts and installing…", async ct =>
    {
        if (build == null) throw new InvalidOperationException("Generate a mod with the current name and selection first.");
        var game = GameBox.Text; var installer = new Installer(Tool(), Log);
        await Task.Run(() => installer.Install(game, build, ct), ct);
    });
    private async void UninstallClick(object sender, RoutedEventArgs e) => await Operation("Uninstalling owned files…", async ct =>
    { var game = GameBox.Text; var installer = new Installer(Tool(), Log); await Task.Run(() => installer.Uninstall(game), ct); });
    private void UpdateStatus() { try { InstallStatus.Text = new Installer(Tool(), Log).Status(GameBox.Text); } catch { InstallStatus.Text = "Select a game installation"; } }
    private void Refresh()
    {
        if (!ready || index == null) return;
        string name;
        try { name = Replacement.ValidateName(NameBox.Text); } catch (Exception e) { Status.Text = e.Message; return; }
        foreach (var row in index.Entries.Where(r => r.CharacterId == SelectedCharacter)) row.Replacement = Replacement.ReplaceCharacter(row.Original, name, row.CharacterId, row.ReplaceFullNames);
        var categories = Categories(); var query = SearchBox.Text;
        var view = CollectionViewSource.GetDefaultView(Preview.ItemsSource);
        view.Filter = o => o is Candidate c && c.CharacterId == SelectedCharacter && (categories.Contains(c.Category) || (c.Category == Category.Unclassified && ShowUnclassified.IsChecked == true)) && (c.Original.Contains(query, StringComparison.OrdinalIgnoreCase) || c.Id.Contains(query) || c.Resource.Contains(query, StringComparison.OrdinalIgnoreCase) || c.SecondaryKey.Contains(query, StringComparison.OrdinalIgnoreCase));
        view.Refresh();
        CountText.Text = $"{index.Entries.Count(e => e.CharacterId == SelectedCharacter && e.Include && categories.Contains(e.Category)):N0} selected for this character • {settings.Renames.Count(p => p.Value.Enabled)} characters enabled";
    }
    private void NameChanged(object sender, TextChangedEventArgs e) { if (!ready) return; RenameEnabled.IsChecked = true; StoreCharacter(); build = null; Refresh(); }
    private void LoadCharacter()
    {
        var wasReady = ready; ready = false;
        var c = Characters.Get(SelectedCharacter); var rename = settings.Renames[c.Id];
        NameBox.Text = rename.Name; RenameEnabled.IsChecked = rename.Enabled;
        AliasText.Text = "Matches: " + string.Join(", ", c.Aliases) + ". Review ambiguous names in context.";
        ready = wasReady;
    }
    private void CharacterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ready) return;
        LoadCharacter(); settings.SelectedCharacter = SelectedCharacter; Detail.Text = ""; Refresh(); Save();
    }
    private void EnabledChanged(object sender, RoutedEventArgs e) { if (!ready) return; StoreCharacter(); build = null; Refresh(); Save(); }
    private void GameChanged(object sender, TextChangedEventArgs e) { if (!ready) return; index = null; build = null; Preview.ItemsSource = null; CountText.Text = "Installation/tool changed — scan again."; UpdateStatus(); }
    private void CategoryChanged(object sender, RoutedEventArgs e) { build = null; Refresh(); }
    private void FilterChanged(object sender, RoutedEventArgs e) => Refresh();
    private void Edited(object sender, DataGridCellEditEndingEventArgs e)
    {
        build = null;
        Dispatcher.BeginInvoke(() =>
        {
            if (e.Row.Item is Candidate c && c.Include && !Replacement.HasStandaloneV(c.Original)) { c.ReplaceFullNames = true; c.Review = "User approved; not in-game verified"; }
            PersistApprovals(); Save(); Refresh();
        });
    }
    private void PersistApprovals()
    {
        if (index == null) return;
        settings.Approvals = index.Entries.Where(c => c.Include && c.ReplaceFullNames && c.Category != Category.Unclassified && (c.Review.StartsWith("User approved") || c.Review.StartsWith("Bulk approved"))).Select(c => new Reviewed(c.Resource, c.Id, c.Field, Storage.Hash(c.Original), c.Category, "User approved", c.CharacterId)).ToList();
    }
    private void SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Preview.SelectedItem is not Candidate c) return;
        Detail.Text = $"{c.Resource}\nID {c.Id} • {c.Field} • {c.SecondaryKey}\n{c.Evidence}\nORIGINAL: {c.Original}\nREPLACEMENT: {c.Replacement}";
        if (c.Category != Category.Unclassified) AssignCategory.SelectedItem = c.Category;
    }
    private void ApproveClick(object sender, RoutedEventArgs e)
    {
        if (cancellation != null || Preview.SelectedItem is not Candidate c) return;
        c.Category = (Category)AssignCategory.SelectedItem; c.Include = true; c.ReplaceFullNames = true; c.Review = "User approved; not in-game verified"; build = null; PersistApprovals(); Save(); Refresh();
    }
    private void DetectGame(object sender, RoutedEventArgs e) { var paths = Game.Detect(); if (paths.Count > 0) GameBox.Text = paths[0]; else Log("No installation detected. Use Browse to select it."); }
    private void BrowseGame(object sender, RoutedEventArgs e) { var d = new OpenFolderDialog { Title = "Select Cyberpunk 2077 root folder" }; if (d.ShowDialog(this) == true) GameBox.Text = d.FolderName; }
    private void BrowseTool(object sender, RoutedEventArgs e) { var d = new OpenFileDialog { Filter = "WolvenKit CLI|WolvenKit.CLI.exe" }; if (d.ShowDialog(this) == true) ToolBox.Text = d.FileName; }
    private void Cancel(object sender, RoutedEventArgs e) => cancellation?.Cancel();
    private void RestoreClick(object sender, RoutedEventArgs e)
    {
        ready = false; settings.Renames.Clear(); settings.Name = "Johnny"; Characters.Initialize(settings);
        CharacterBox.SelectedItem = Characters.All[0]; LoadCharacter(); ready = true;
        Dialogue.IsChecked = Journal.IsChecked = Messages.IsChecked = UI.IsChecked = true;
        settings.Excluded.Clear(); settings.Approvals.Clear(); if (index != null) new Scanner(Tool(), "", Path.Combine(AppContext.BaseDirectory, "reviewed.json"), Log).ApplyReview(index);
        build = null; Refresh(); Save(); Log("Default name, categories and review selections restored. Use Uninstall to remove an installed mod.");
    }
    private void ExportClick(object sender, RoutedEventArgs e)
    {
        if (index == null) return; var d = new SaveFileDialog { Filter = "JSON report|*.json", FileName = "replacement-preview.json" };
        if (d.ShowDialog(this) == true) try
        {
            StoreCharacter();
            var categories = Categories();
            var rows = index.Entries.Where(c => c.Include && categories.Contains(c.Category) && settings.Renames[c.CharacterId].Enabled);
            Storage.Write(d.FileName, rows.GroupBy(c => c.Identity).Select(g => new { g.First().Resource, g.First().Id, g.First().Field, g.First().Original, Replacement = Replacement.ReplaceMany(g.First().Original, g.Select(c => (c.CharacterId, settings.Renames[c.CharacterId].Name, c.ReplaceFullNames))), Characters = g.Select(c => c.CharacterId) }));
            Log("Combined preview exported to " + d.FileName);
        } catch (Exception ex) { Log(ex.Message); }
    }
    private void ApproveAllClick(object sender, RoutedEventArgs e)
    {
        if (cancellation != null || index == null) return;
        var categories = Categories();
        foreach (var c in index.Entries)
        {
            if (c.CharacterId != SelectedCharacter) continue;
            if (c.Category == Category.Unclassified && categories.Contains(Category.UI)) { c.Category = Category.UI; c.Evidence += "; bulk approval: unclassified text assigned to UI"; }
            if (!categories.Contains(c.Category)) continue;
            c.Include = true;
            c.ReplaceFullNames = true;
            if (!c.Review.StartsWith("Source reviewed")) c.Review = "Bulk approved; not context verified";
        }
        build = null; PersistApprovals(); Save(); Refresh();
        Log("Bulk-approved current character in enabled categories. Unrelated name references may change. Unclassified text assigned to UI.");
    }
}
