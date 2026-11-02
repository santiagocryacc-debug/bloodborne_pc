using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BloodborneMinimalLauncher.SaveData;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Control = System.Windows.Controls.Control;
using Orientation = System.Windows.Controls.Orientation;
using Panel = System.Windows.Controls.Panel;
using TabControl = System.Windows.Controls.TabControl;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;
using ComboBox = System.Windows.Controls.ComboBox;
using ListBox = System.Windows.Controls.ListBox;
using MessageBox = System.Windows.MessageBox;

namespace BloodborneMinimalLauncher;

internal static class SaveUi
{
    internal static Brush Gold = new SolidColorBrush(Color.FromRgb(216, 182, 106));
    internal static string Label(string text) => CultureInfo.GetCultureInfo("en-US").TextInfo.ToTitleCase(
        Regex.Replace(Regex.Replace(text, @"\([^)]*\)", "").Replace('-', ' '), @"\s+", " ").Trim().ToLowerInvariant());
    internal static void Setup(Window window, Window owner, string title, double width, double height)
    {
        window.Title = title; if (owner.IsVisible) window.Owner = owner; window.Icon = owner.Icon;
        window.Width = width; window.Height = height; window.MinWidth = width - 100; window.MinHeight = height - 80;
        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        window.Background = new SolidColorBrush(Color.FromRgb(20, 16, 15));
        window.Foreground = new SolidColorBrush(Color.FromRgb(240, 228, 208));
        window.FontFamily = new System.Windows.Media.FontFamily("Georgia"); window.FontSize = 14;
        window.Resources.MergedDictionaries.Add(owner.Resources);
        window.Resources.Add(typeof(TextBox), new Style(typeof(TextBox))
        {
            Setters = { new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(56, 37, 31))),
                new Setter(Control.ForegroundProperty, window.Foreground), new Setter(Control.BorderBrushProperty, Gold),
                new Setter(Control.PaddingProperty, new Thickness(8, 5, 8, 5)), new Setter(Control.MinHeightProperty, 30.0) }
        });
        window.Resources.Add(typeof(ListBox), new Style(typeof(ListBox))
        {
            Setters = { new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(28, 23, 21))),
                new Setter(Control.ForegroundProperty, window.Foreground), new Setter(Control.BorderBrushProperty, Gold),
                new Setter(Control.PaddingProperty, new Thickness(5)), new Setter(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled) }
        });
        window.Resources.Add(typeof(ListBoxItem), new Style(typeof(ListBoxItem))
        {
            Setters = { new Setter(Control.PaddingProperty, new Thickness(8)), new Setter(Control.ForegroundProperty, window.Foreground) }
        });
    }
    internal static Button Button(string text, Action action)
    {
        var button = new Button { Content = text, Margin = new Thickness(0, 0, 8, 6), Padding = new Thickness(12, 7, 12, 7), MinWidth = 100 };
        button.Click += (_, _) => action(); return button;
    }
    internal static TextBlock Text(string text, bool heading = false)
    {
        var label = new TextBlock { Text = text, Margin = new Thickness(0, 5, 8, 10), FontSize = heading ? 18 : 14 };
        if (heading) label.Foreground = Gold;
        return label;
    }
    internal static StackPanel Row(params UIElement[] elements)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
        foreach (var item in elements) row.Children.Add(item); return row;
    }
    internal static FrameworkElement Scroll(UIElement content) => new ScrollViewer
    { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Padding = new Thickness(16) };
    internal static string Error(Exception error) => error switch
    {
        SaveFormatException => "Unsupported Save Format",
        UnauthorizedAccessException => "Folder Access Denied",
        IOException => "Save Operation Failed",
        ArgumentException => "Invalid Value",
        InvalidOperationException when error.Message == "The inventory is full." => "Inventory Full",
        InvalidOperationException when error.Message.Contains("no room left", StringComparison.Ordinal) => "Save Capacity Reached",
        InvalidOperationException when Regex.IsMatch(error.Message, @"^[A-Z][A-Za-z .]+$") && error.Message.Length < 65 => Label(error.Message),
        _ => "Save Operation Failed"
    };
}

internal sealed class SavesWindow : Window
{
    private readonly SaveRepository _repository;
    private readonly string _gameDir;
    private readonly Action<string> _activate;
    private readonly ComboBox _profiles = new() { Width = 250, Margin = new Thickness(8, 0, 16, 0) };
    private readonly TextBox _newName = new() { Width = 230, Margin = new Thickness(0, 0, 12, 6) };
    private readonly ListBox _characters = new();
    private readonly ListBox _backups = new();
    private readonly TextBlock _status = SaveUi.Text("Ready");
    private readonly TextBlock _active = SaveUi.Text("");
    private readonly Grid _actions = new();
    private SaveProfile Selected => (SaveProfile?)_profiles.SelectedItem ?? throw new InvalidOperationException("Select A Profile");

    internal SavesWindow(Window owner, SaveRepository repository, string active, string gameDir, Action<string> activate)
    {
        _repository = repository; _gameDir = gameDir; _activate = activate;
        SaveUi.Setup(this, owner, "Save Manager", 1020, 730);
        var root = new DockPanel { Margin = new Thickness(24) };
        var top = new StackPanel(); DockPanel.SetDock(top, Dock.Top);
        top.Children.Add(SaveUi.Text("Save Manager", true));
        top.Children.Add(SaveUi.Row(SaveUi.Text("Profile"), _profiles, SaveUi.Button("Use Profile", () => Run(() =>
        { _repository.RecoverPending(Selected); _activate(Selected.Name); _active.Text = "Active Profile: " + Selected.Name; }, "Profile Selected"))));
        _active.Text = "Active Profile: " + active; top.Children.Add(_active);
        top.Children.Add(SaveUi.Row(SaveUi.Text("Profile Name"), _newName, SaveUi.Button("New Profile", () => Create(false)), SaveUi.Button("Copy Profile", () => Create(true))));
        root.Children.Add(top);
        var bottom = SaveUi.Row(_status); DockPanel.SetDock(bottom, Dock.Bottom); root.Children.Add(bottom);
        var grid = new Grid { Margin = new Thickness(0, 12, 0, 4) };
        grid.ColumnDefinitions.Add(new ColumnDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.Children.Add(SaveUi.Text("Characters", true));
        var title = SaveUi.Text("Backups", true); Grid.SetColumn(title, 1); grid.Children.Add(title);
        _characters.Margin = new Thickness(0, 0, 16, 10); Grid.SetRow(_characters, 1); grid.Children.Add(_characters);
        _backups.Margin = new Thickness(0, 0, 0, 10); Grid.SetColumn(_backups, 1); Grid.SetRow(_backups, 1); grid.Children.Add(_backups);
        var leftButtons = SaveUi.Row(SaveUi.Button("Edit Save", Edit), SaveUi.Button("Import", Import));
        Grid.SetRow(leftButtons, 2); grid.Children.Add(leftButtons);
        var rightButtons = SaveUi.Row(SaveUi.Button("Backup", () => Run(() => _repository.Backup(Selected), "Backup Complete")),
            SaveUi.Button("Restore", Restore), SaveUi.Button("Refresh", Refresh));
        Grid.SetRow(rightButtons, 2); Grid.SetColumn(rightButtons, 1); grid.Children.Add(rightButtons);
        _actions.Children.Add(grid); root.Children.Add(_actions); Content = root;
        _profiles.SelectionChanged += (_, _) => Refresh();
        ReloadProfiles(active);
    }
    private void ReloadProfiles(string selected)
    {
        _profiles.ItemsSource = _repository.Profiles();
        _profiles.SelectedItem = _profiles.Items.Cast<SaveProfile>().FirstOrDefault(p => p.Name == selected) ?? _profiles.Items[0];
    }
    private void Refresh()
    {
        if (_profiles.SelectedItem == null) return;
        try
        {
            _backups.ItemsSource = _repository.Backups(Selected);
            _backups.SelectedIndex = _backups.Items.Count > 0 ? 0 : -1;
            _characters.ItemsSource = _repository.Characters(Selected);
            _characters.SelectedIndex = _characters.Items.Count > 0 ? 0 : -1;
            _backups.ItemsSource = _repository.Backups(Selected);
            _backups.SelectedIndex = _backups.Items.Count > 0 ? 0 : -1;
            _status.Text = "Ready";
        }
        catch (Exception e) { _characters.ItemsSource = null; _status.Text = SaveUi.Error(e); }
    }
    private void Run(Action action, string success)
    {
        try { action(); Refresh(); _status.Text = success; }
        catch (Exception e) { _status.Text = SaveUi.Error(e); }
    }
    private void Create(bool copy) => Run(() =>
    { var name = _newName.Text.Trim(); _repository.CreateProfile(name, copy ? Selected : null); ReloadProfiles(name); _newName.Clear(); }, "Profile Created");
    private void Import()
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog { Description = "Import Save", UseDescriptionForTitle = true };
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
        Run(() => _repository.Import(Selected, dialog.SelectedPath), "Save Imported");
    }
    private void Restore()
    {
        if (_backups.SelectedItem is not SaveBackup backup) { _status.Text = "Select A Backup"; return; }
        Run(() => _repository.Restore(Selected, backup), "Save Restored");
    }
    private void Edit()
    {
        if (_characters.SelectedItem is not SaveCharacter character) { _status.Text = "Select A Character"; return; }
        try
        {
            if (SaveRepository.GameRunning()) throw new InvalidOperationException("Close Bloodborne First");
            new SaveEditorWindow(this, _repository, Selected, character, _gameDir).ShowDialog(); Refresh();
        }
        catch (Exception e) { _status.Text = SaveUi.Error(e); }
    }
}

internal sealed class SaveEditorWindow : Window
{
    private readonly SaveRepository _repository;
    private readonly SaveProfile _profile;
    private readonly SaveCharacter _character;
    private readonly SaveCatalog? _catalog;
    private byte[] _original;
    private SaveSlot _slot;
    private bool _dirty;
    private readonly TabControl _tabs = new();
    private readonly TextBlock _status = SaveUi.Text("Ready");
    private readonly TextBlock _level = SaveUi.Text("");
    private readonly Dictionary<SaveStat, TextBox> _stats = new();
    private TextBox _echoes = null!, _insight = null!;
    private readonly ListBox _items = new() { Height = 275, DisplayMemberPath = "Name" };
    private readonly ComboBox _list = new() { Width = 200, ItemsSource = new[] { "Inventory", "Key Items", "Storage" }, SelectedIndex = 0 };
    private readonly TextBox _search = new() { Width = 280 };
    private readonly TextBox _quantity = new() { Width = 100, Text = "1" };
    private sealed record ItemRow(SaveItem Item, string Name);
    private sealed record Choice(object Item, string Name) { public override string ToString() => Name; }

    internal SaveEditorWindow(Window owner, SaveRepository repository, SaveProfile profile, SaveCharacter character, string gameDir)
    {
        _repository = repository; _profile = profile; _character = character;
        _original = File.ReadAllBytes(character.File); _slot = SaveSlot.Parse(_original);
        _catalog = SaveCatalog.TryLoad(gameDir, "en", out _);
        SaveUi.Setup(this, owner, "Save Editor", 950, 760);
        var root = new DockPanel { Margin = new Thickness(24) };
        var heading = SaveUi.Row(SaveUi.Text(character.Name, true), _level); DockPanel.SetDock(heading, Dock.Top); root.Children.Add(heading);
        var bottom = SaveUi.Row(SaveUi.Button("Save Changes", Save), SaveUi.Button("Discard Changes", Discard), _status);
        DockPanel.SetDock(bottom, Dock.Bottom); root.Children.Add(bottom);
        root.Children.Add(_tabs); Content = root;
        var tabsPanel = new FrameworkElementFactory(typeof(System.Windows.Controls.Primitives.UniformGrid));
        tabsPanel.SetValue(System.Windows.Controls.Primitives.UniformGrid.RowsProperty, 1);
        tabsPanel.SetValue(System.Windows.Controls.Primitives.UniformGrid.ColumnsProperty, 6);
        _tabs.ItemsPanel = new ItemsPanelTemplate(tabsPanel);
        BuildTabs();
        Closing += (_, e) =>
        {
            if (_dirty && MessageBox.Show(this, "Discard Changes?", "Save Editor", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) e.Cancel = true;
        };
    }
    private void Tab(string name, UIElement content) => _tabs.Items.Add(new TabItem { Header = name, Content = SaveUi.Scroll(content) });
    private void BuildTabs()
    {
        var selected = _tabs.SelectedIndex; _tabs.Items.Clear(); _stats.Clear();
        _level.Text = "Level " + _slot.Level;
        var stats = new StackPanel();
        foreach (var stat in Enum.GetValues<SaveStat>())
        {
            var box = Number(_slot.Stat(stat)); _stats[stat] = box;
            stats.Children.Add(Field(stat.ToString(), box));
        }
        _echoes = Number(_slot.Echoes); _insight = Number(_slot.Insight);
        stats.Children.Add(Field("Blood Echoes", _echoes)); stats.Children.Add(Field("Insight", _insight));
        stats.Children.Add(SaveUi.Button("Apply Stats", () => Change(candidate =>
        {
            // Increase first so a legal respec does not temporarily reduce the level below one.
            var values = _stats.ToDictionary(p => p.Key, p => Read(p.Value, 1, SaveSlot.MaxStat));
            foreach (var pair in values.OrderByDescending(p => p.Value - candidate.Stat(p.Key))) candidate.SetStat(pair.Key, pair.Value);
            candidate.SetEchoes(Read(_echoes, 0, SaveSlot.MaxEchoes)); candidate.SetInsight(Read(_insight, 0, SaveSlot.MaxInsight));
        })));
        // Text entered into stats is also treated as unsaved, including edits before Apply Stats.
        foreach (var box in _stats.Values.Append(_echoes).Append(_insight)) box.TextChanged += (_, _) => { _dirty = true; _status.Text = "Unsaved Changes"; };
        Tab("Stats", stats);

        var items = new StackPanel();
        Detach(_list); Detach(_search); Detach(_items); Detach(_quantity);
        items.Children.Add(SaveUi.Row(SaveUi.Text("List"), _list, SaveUi.Text("Search"), _search));
        items.Children.Add(_items);
        items.Children.Add(SaveUi.Row(SaveUi.Text("Quantity"), _quantity, SaveUi.Button("Set Quantity", SetQuantity)));
        _list.SelectionChanged -= ListChanged; _list.SelectionChanged += ListChanged;
        _search.TextChanged -= SearchChanged; _search.TextChanged += SearchChanged;
        _items.SelectionChanged -= ItemChanged; _items.SelectionChanged += ItemChanged;
        RefreshItems(); Tab("Inventory", items);
        Tab("Add Items", CatalogTab("Items"));
        Tab("Equipment", CatalogTab("Equipment"));
        var bosses = new StackPanel();
        foreach (var boss in SaveWorld.Bosses)
        {
            var state = SaveUi.Text(SaveWorld.State(_slot, boss).ToString()); state.Width = 90;
            var title = SaveUi.Text(SaveUi.Label(_catalog?.NpcName(boss.NameId, boss.Name) ?? boss.Name)); title.Width = 350;
            bosses.Children.Add(SaveUi.Row(title, state,
                SaveUi.Button("Fight Again", () => Change(s => { SaveWorld.FightAgain(s, boss); state.Text = "Alive"; })),
                SaveUi.Button("Defeated", () => Change(s => { SaveWorld.MarkDefeated(s, boss); state.Text = "Defeated"; }))));
        }
        Tab("Bosses", bosses);
        var lamps = new StackPanel();
        foreach (var lamp in SaveWorld.Lamps)
        {
            var entry = _catalog?.Lamps.FirstOrDefault(l => l.Flag == lamp.Flag);
            var name = SaveUi.Label(entry?.Name ?? "Lamp " + lamp.Flag);
            var check = new System.Windows.Controls.CheckBox { Content = name, IsChecked = _slot.Flag(lamp.Flag), Margin = new Thickness(0, 6, 0, 6) };
            check.Click += (_, _) => Change(s => s.SetFlag(lamp.Flag, check.IsChecked == true)); lamps.Children.Add(check);
        }
        Tab("Lamps", lamps);
        _tabs.SelectedIndex = Math.Clamp(selected, 0, _tabs.Items.Count - 1);
    }
    private static void Detach(FrameworkElement element) { if (element.Parent is Panel panel) panel.Children.Remove(element); }
    private static TextBox Number(int number) => new() { Text = number.ToString(CultureInfo.InvariantCulture), Width = 180 };
    private static StackPanel Field(string name, TextBox box)
    { var title = SaveUi.Text(name); title.Width = 230; return SaveUi.Row(title, box); }
    private static int Read(TextBox box, int min, int max)
    {
        if (!int.TryParse(box.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < min || value > max)
            throw new InvalidOperationException("Invalid Value");
        return value;
    }
    private void Change(Action<SaveSlot> edit, bool rebuild = false)
    {
        try
        {
            // Failed edits cannot leave half an operation in the in-memory save.
            var candidate = SaveSlot.Parse(_slot.ToBytes()); edit(candidate); SaveSlot.Parse(candidate.ToBytes());
            _slot = candidate; _dirty = true; _level.Text = "Level " + _slot.Level; _status.Text = "Unsaved Changes";
            if (rebuild) BuildTabs(); else RefreshItems();
        }
        catch (Exception e) { _status.Text = SaveUi.Error(e); }
    }
    private void ListChanged(object sender, SelectionChangedEventArgs e) => RefreshItems();
    private void SearchChanged(object sender, TextChangedEventArgs e) => RefreshItems();
    private void ItemChanged(object sender, SelectionChangedEventArgs e)
    { if (_items.SelectedItem is ItemRow row) _quantity.Text = row.Item.Quantity.ToString(CultureInfo.InvariantCulture); }
    private void RefreshItems()
    {
        var selected = (_items.SelectedItem as ItemRow)?.Item;
        var list = (SaveList)Math.Max(0, _list.SelectedIndex);
        _items.ItemsSource = _slot.Items.Where(i => i.List == list).Select(i => new ItemRow(i,
            SaveUi.Label(_catalog?.ItemName(i.ItemId) ?? "Item " + i.Id) + "   |   " + i.Quantity))
            .Where(i => i.Name.Contains(_search.Text, StringComparison.OrdinalIgnoreCase)).ToList();
        _items.SelectedItem = _items.Items.Cast<ItemRow>().FirstOrDefault(i => i.Item.Position == selected?.Position && i.Item.ItemId == selected?.ItemId);
    }
    private void SetQuantity()
    {
        if (_items.SelectedItem is not ItemRow row) { _status.Text = "Select An Item"; return; }
        if (row.Item.Category != 4 || _catalog == null || !_catalog.GoodsById.TryGetValue(row.Item.Id, out var goods))
        { _status.Text = "Item Quantity Unavailable"; return; }
        Change(s => s.SetQuantity(row.Item, Read(_quantity, 1, row.Item.List == SaveList.Storage ? goods.MaxStored : goods.MaxHeld)));
    }
    private StackPanel CatalogTab(string kind)
    {
        var panel = new StackPanel();
        if (_catalog == null) { panel.Children.Add(SaveUi.Text("Catalog Unavailable")); return panel; }
        var search = new TextBox { Width = 360, HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
        var list = new ListBox { Height = 320, Margin = new Thickness(0, 10, 0, 10) };
        var quantity = Number(1);
        var type = new ComboBox { Width = 200, ItemsSource = kind == "Items" ? new[] { "Items" } : new[] { "Weapons", "Armor" }, SelectedIndex = 0 };
        List<Choice> Choices() => (kind == "Items" ? _catalog.Goods.Select(g => new Choice(g, SaveUi.Label(g.Name))) :
            type.SelectedIndex == 0 ? _catalog.Weapons.Select(w => new Choice(w, SaveUi.Label(w.Name))) :
            _catalog.Armor.Select(a => new Choice(a, SaveUi.Label(a.Name))))
            .Where(c => c.Name.Contains(search.Text, StringComparison.OrdinalIgnoreCase)).OrderBy(c => c.Name).ToList();
        void Filter() { list.ItemsSource = Choices(); if (list.Items.Count > 0) list.SelectedIndex = 0; }
        search.TextChanged += (_, _) => Filter(); type.SelectionChanged += (_, _) => Filter();
        panel.Children.Add(SaveUi.Row(type, SaveUi.Text("Search"), search)); panel.Children.Add(list);
        if (kind == "Items") panel.Children.Add(Field("Quantity", quantity));
        panel.Children.Add(SaveUi.Button("Add", () =>
        {
            if (list.SelectedItem is not Choice choice) { _status.Text = "Select An Item"; return; }
            Change(s =>
            {
                switch (choice.Item)
                {
                    case CatalogGoods goods:
                        var amount = Read(quantity, 1, goods.MaxHeld);
                        var existing = s.Items.FirstOrDefault(i => i.Id == goods.Id && i.Category == 4 && i.List == (goods.KeyItem ? SaveList.KeyItems : SaveList.Inventory));
                        if ((long)(existing?.Quantity ?? 0) + amount > goods.MaxHeld) throw new InvalidOperationException("Item Limit Reached");
                        s.AddGoods(goods.Id, amount, goods.Sort, goods.KeyItem); break;
                    case CatalogWeapon weapon: s.AddWeapon(weapon.Id, weapon.Durability, weapon.Sort, weapon.GemSlots); break;
                    case CatalogArmor armor: s.AddArmor(armor.Id, armor.Durability, armor.Sort); break;
                }
            });
        }));
        Filter(); return panel;
    }
    private void Save()
    {
        try
        {
            // Save Changes includes unsubmitted stat fields so no typed edits are silently lost.
            var candidate = SaveSlot.Parse(_slot.ToBytes());
            var values = _stats.ToDictionary(p => p.Key, p => Read(p.Value, 1, SaveSlot.MaxStat));
            foreach (var pair in values.OrderByDescending(p => p.Value - candidate.Stat(p.Key))) candidate.SetStat(pair.Key, pair.Value);
            candidate.SetEchoes(Read(_echoes, 0, SaveSlot.MaxEchoes)); candidate.SetInsight(Read(_insight, 0, SaveSlot.MaxInsight));
            if (!candidate.ToBytes().AsSpan().SequenceEqual(_original)) _repository.Commit(_profile, _character, _original, candidate);
            _original = File.ReadAllBytes(_character.File); _slot = SaveSlot.Parse(_original); _dirty = false;
            BuildTabs(); _status.Text = "Save Updated";
        }
        catch (Exception e) { _status.Text = SaveUi.Error(e); }
    }
    private void Discard()
    { _slot = SaveSlot.Parse(_original); _dirty = false; BuildTabs(); _status.Text = "Changes Discarded"; }
}
