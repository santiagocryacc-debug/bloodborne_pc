// Fixture adapted from LanternEngine SaveEditorChecks, GPL-3.0-or-later.
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BloodborneMinimalLauncher;
using BloodborneMinimalLauncher.SaveData;

internal static class SaveChecks
{
static void Name(byte[] bytes, int at)
{
    var text = "Fixture";
    for (var i = 0; i < text.Length; i++) bytes[at + i * 2] = (byte)text[i];
}

static byte[] Fixture()
{
    var bytes = new byte[SaveSlot.FileSize];
    void W(int at, uint value) => BitConverter.TryWriteBytes(bytes.AsSpan(at, 4), value);
    const int table = 0x54;
    var at = table;
    W(at, 0xC0800000); W(at + 4, 0x8000C544); at += 0x28;
    W(at, 0x80800001); W(at + 4, 8000000); W(at + 8, 250); W(at + 16, 1);
    for (var slot = 0; slot < 5; slot++) W(at + 20 + slot * 8, 0x80000000);
    at += 0x3C;
    for (var slot = 2; slot < SaveSlot.TableSlots; slot++) { W(at + 4, 0xFFFFFFFF); at += 8; }
    var player = at;
    W(player + 0x80, 155);
    W(player + 0x84, 12345);
    W(player + 0x74, 40);
    foreach (var (offset, value) in new[] { (0x30, 40u), (0x38, 42u), (0x48, 40u), (0x50, 20u), (0x58, 40u), (0x60, 23u) }) W(player + offset, value);
    Name(bytes, player + 0x98);
    W(player + 0x25C, 2);
    var inventory = player + 0x260;
    W(inventory, 0x80800001); W(inventory + 4, 8000000); W(inventory + 8, 1); W(inventory + 12, 0x7C830040);
    W(inventory + 16, 0xB00003E8); W(inventory + 20, 0x400003E8); W(inventory + 24, 7); W(inventory + 28, 0x02710041);
    var keys = inventory + SaveSlot.InventoryCapacity * 16;
    W(keys, 0);
    var face = keys + 4 + SaveSlot.KeyCapacity * 16 + 64;
    bytes[face] = (byte)'F'; bytes[face + 1] = (byte)'A'; bytes[face + 2] = (byte)'C'; bytes[face + 3] = (byte)'E';
    var storage = face + 0xF0;
    W(storage, 1);
    W(storage + 4, 0xB00003E8); W(storage + 8, 0x400003E8); W(storage + 12, 300); W(storage + 16, 0x02710040);
    var size = (uint)(storage + 4 + SaveSlot.StorageCapacity * 16 + 0x94 - table);
    var header = new uint[]
    {
        0x41, 0x18010000, 5000, table, size + 241, table, size, table + size + 158, 0,
        table + size, 150, table + size + 150, 8, table + size + 241, 150001,
    };
    for (var i = 0; i < header.Length; i++) W(i * 4, header[i]);
    var pool = header[13];
    W(15 * 4, pool + 150001);
    W(16 * 4, 100);
    W(17 * 4, pool + 150001 + 100);
    W(18 * 4, 180);
    W(19 * 4, pool + 150001 + 180);
    W(20 * 4, 0x20004);
    bytes[pool + 150001 + 180 + 0x20003] = 1;
    bytes[pool + 150001 + 180 + 0x20004 + 0x1026] = 0xAB;
    bytes[pool + 150001 + 180 + 0x20004 + 0x1027] = 0xCD;
    return bytes;
}

    private static int _count;
    private static void Check(bool condition, string name)
    { if (!condition) throw new Exception(name); _count++; Console.WriteLine("PASS " + name); }
    private static void Reject(Action action, string name)
    {
        var rejected = false;
        try { action(); } catch (Exception e) when (e is InvalidOperationException or SaveFormatException or ArgumentException or IOException or InvalidDataException) { rejected = true; }
        Check(rejected, name);
    }
    internal static void Run(string root, Application app, string settings, string starter)
    {
        var saves = Path.Combine(root, "save-tests-" + Guid.NewGuid().ToString("N"));
        var backups = Path.Combine(root, "backups-" + Guid.NewGuid().ToString("N"));
        var blocked = false;
        var repository = new SaveRepository(saves, backups, () => blocked);
        var profile = repository.Profile("Default");
        Directory.CreateDirectory(profile.Folder);
        var fixture = Fixture();
        var slotPath = Path.Combine(profile.Folder, "userdata0001");
        File.WriteAllBytes(slotPath, fixture);
        var summary = new byte[SaveSlotSummary.FileSize]; Name(summary, 0x1096 + 0x192);
        File.WriteAllBytes(Path.Combine(profile.Folder, "userdata0010"), summary);
        File.WriteAllText(Path.Combine(profile.Folder, "keep.bin"), "preserve this companion file");
        var slot = SaveSlot.Parse(fixture);
        Check(slot.ToBytes().AsSpan().SequenceEqual(fixture), "Save round trip preserves every byte");
        Reject(() => SaveSlot.Parse(new byte[30]), "Truncated save is rejected");
        var bad = (byte[])fixture.Clone(); bad[0] = 0x42;
        Reject(() => SaveSlot.Parse(bad), "Unsupported save version is rejected");
        Reject(() => slot.SetStat(SaveStat.Vitality, 100), "Stat limit is enforced");
        Reject(() => slot.SetInsight(100), "Insight limit is enforced");
        Reject(() => slot.SetEchoes(-1), "Negative echoes are rejected");
        slot.SetFlag(12401800, true); slot.AddWeapon(7000300, 200, 509700, new[] { 1u, 1u, 0x80000000u, 0x80000000u, 0x80000000u });
        slot.AddArmor(230000, 0, 100000); slot.AddGoods(1200, 2, 40010, false);
        var grown = SaveSlot.Parse(slot.ToBytes());
        Check(grown.Flag(12401800) && grown.Name == "Fixture" && grown.Items.Count == 6, "Added equipment preserves player, inventory and event flags");
        foreach (var boss in SaveWorld.Bosses)
        {
            var world = SaveSlot.Parse(fixture); SaveWorld.MarkDefeated(world, boss);
            Check(SaveWorld.State(world, boss) == BossState.Defeated, "Boss defeated: " + boss.Name);
            SaveWorld.FightAgain(world, boss);
            Check(SaveWorld.State(world, boss) == BossState.Alive, "Boss reset: " + boss.Name);
        }
        foreach (var lamp in SaveWorld.Lamps) slot.SetFlag(lamp.Flag, true);
        Check(SaveWorld.Lamps.All(l => slot.Flag(l.Flag)), "All supported lamp flags round trip");
        var character = repository.Characters(profile).Single();
        Check(character.Slot == 1 && character.Level == 155, "Manager reads character slots");
        var backup = repository.Backup(profile);
        var duplicate = repository.Backup(profile);
        Check(backup != duplicate && repository.Backups(profile).Count == 2, "Backups never overwrite earlier copies");
        var edit = SaveSlot.Parse(fixture); edit.SetStat(SaveStat.Vitality, 50); edit.SetEchoes(55555);
        repository.Commit(profile, character, fixture, edit);
        Check(SaveSlot.Parse(File.ReadAllBytes(slotPath)).Level == 165, "Editor commits a valid edited slot");
        Check(SaveSlotSummary.Level(File.ReadAllBytes(Path.Combine(profile.Folder, "userdata0010")), 1) == 165, "Character summary level stays synchronized");
        Check(File.ReadAllText(Path.Combine(profile.Folder, "keep.bin")) == "preserve this companion file", "Companion files survive editing");
        Reject(() => repository.Commit(profile, character, fixture, edit), "External changes prevent overwriting a stale editor");
        repository.CreateProfile("Test Copy", profile);
        var other = repository.Profile("Test Copy");
        Check(repository.Characters(other).Single().Level == 165, "Profile copy preserves character data");
        Check(repository.Backups(other).Count == 0, "A profile cannot restore another profile's backups");
        Reject(() => repository.Restore(other, new SaveBackup(backup, DateTime.Now)), "Cross profile restore is rejected");
        var restore = repository.Backups(profile).Single(b => b.Folder == backup);
        repository.Restore(profile, restore);
        Check(File.ReadAllBytes(slotPath).AsSpan().SequenceEqual(fixture), "Restore recovers original bytes");
        Check(repository.Backups(profile).Count == 4, "Edit and restore both create safety backups");
        var beforeFailure = File.ReadAllBytes(slotPath);
        repository.BeforeInstall = () => throw new IOException("Simulated failure");
        Reject(() => repository.Import(profile, other.Root), "Failed folder swap is reported");
        Check(File.ReadAllBytes(slotPath).AsSpan().SequenceEqual(beforeFailure), "Failed import rolls back to the original save");
        repository.BeforeInstall = () => blocked = true;
        Reject(() => repository.Import(profile, other.Root), "A game starting during import cancels installation");
        Check(File.ReadAllBytes(slotPath).AsSpan().SequenceEqual(beforeFailure), "A late game launch rolls the save back intact");
        blocked = false;
        repository.BeforeInstall = () =>
        {
            var concurrent = System.Threading.Tasks.Task.Run(() =>
            {
                try { repository.Backup(other); return false; }
                catch (InvalidOperationException e) { return e.Message == "Save Operation In Progress"; }
            }).GetAwaiter().GetResult();
            Check(concurrent, "Concurrent save operations are rejected");
        };
        repository.Import(profile, other.Root);
        repository.BeforeInstall = null;
        Check(repository.Characters(profile).Single().Level == 165, "Folder import installs a verified save");
        File.AppendAllText(Path.Combine(backup, "keep.bin"), "tamper");
        Reject(() => repository.Restore(profile, restore), "Tampered backup fails checksum verification");
        var invalid = Path.Combine(root, "invalid-save-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(invalid);
        File.WriteAllBytes(Path.Combine(invalid, "userdata0010"), new byte[10]);
        Reject(() => repository.Import(profile, invalid), "Invalid imports cannot replace the current save");
        Check(repository.Characters(profile).Single().Level == 165, "Invalid import leaves the existing save intact");
        foreach (var name in new[] { "..", "../bad", "Default", "CON", "A/B" })
            Reject(() => repository.CreateProfile(name, null), "Unsafe profile is rejected: " + name);
        blocked = true;
        Reject(() => repository.Backup(profile), "Backup is blocked while Bloodborne runs");
        Reject(() => repository.Import(profile, other.Root), "Import is blocked while Bloodborne runs");
        Reject(() => repository.CreateProfile("Blocked", profile), "Profile copy is blocked while Bloodborne runs");
        Reject(() => repository.Commit(profile, character, File.ReadAllBytes(slotPath), edit), "Editing is blocked while Bloodborne runs");
        blocked = false;
        Directory.Move(profile.Folder, profile.Folder + ".previous");
        repository.RecoverPending(profile);
        Check(File.Exists(slotPath), "Interrupted swap recovers before launching the game");
        Directory.CreateDirectory(profile.Folder + ".previous");
        foreach (var file in Directory.GetFiles(profile.Folder)) File.Copy(file, Path.Combine(profile.Folder + ".previous", Path.GetFileName(file)));
        repository.RecoverPending(profile);
        Check(File.Exists(slotPath) && !Directory.Exists(profile.Folder + ".previous"), "Interrupted cleanup preserves the installed save and archives its predecessor");
        var legacyFolder = Path.Combine(backups, "SPRJ0005_2026-10-21_23-52-36");
        Directory.CreateDirectory(legacyFolder);
        File.WriteAllBytes(Path.Combine(legacyFolder, "userdata0001"), fixture);
        File.WriteAllBytes(Path.Combine(legacyFolder, "userdata0010"), summary);
        Directory.SetLastWriteTime(legacyFolder, new DateTime(2030, 10, 10));
        var legacyReader = new SaveRepository(SaveRepository.FixedRoot, backups, () => false);
        var legacyList = legacyReader.Backups(legacyReader.Profile("Default"));
        Check(legacyList.Count == 1 && legacyList[0].When == new DateTime(2026, 10, 21, 23, 52, 36), "Legacy backups use their filename date rather than incorrect folder timestamps");
        Check(repository.Backups(other).Count == 0, "Legacy default backups do not leak to another profile");
        var trailing = new SaveRepository(saves + Path.DirectorySeparatorChar, backups + Path.DirectorySeparatorChar, () => false);
        Check(trailing.Backups(trailing.Profile("Default")).Count == repository.Backups(profile).Count, "Trailing separators preserve backup ownership");
        var corrupt = File.ReadAllBytes(slotPath); File.WriteAllBytes(slotPath, new byte[32]);
        var damagedBackup = repository.Backup(profile);
        Check(new FileInfo(Path.Combine(damagedBackup, "userdata0001")).Length == 32, "Manual backup also protects a damaged save");
        repository.Import(profile, other.Root);
        Check(repository.Characters(profile).Count == 1 && Directory.GetFiles(backups, "userdata0001", SearchOption.AllDirectories).Any(p => new FileInfo(p).Length == 32), "Recovery retains a copy of a damaged current save");

        var owner = new MainWindow(settings, starter);
        var manager = new CurrentSaveWindow(owner, repository, profile, "");
        Render(manager, Path.Combine(root, "current-save-picker.png"));
        var pickerButtons = Descendants((DependencyObject)manager.Content).OfType<Button>().Select(b => b.Content?.ToString()).ToArray();
        Check(pickerButtons.SequenceEqual(new[] { "Edit Save" }), "Current save picker exposes only editing without backups, imports or profiles");
        var mainButtons = Descendants((DependencyObject)owner.Content).OfType<Button>().Select(b => b.Content?.ToString()).ToArray();
        Check(mainButtons.Contains("Edit Save") && mainButtons.Count(b => b == "Backup") == 1 && !mainButtons.Contains("Saves"), "Main navigation retains the original Backup alongside Edit Save without the new save manager");
        var editor = new SaveEditorWindow(owner, repository, profile, repository.Characters(profile).Single(), "");
        Render(editor, Path.Combine(root, "save-editor-stats.png"));
        var tabs = Descendants((DependencyObject)editor.Content).OfType<TabControl>().First();
        tabs.SelectedIndex = 1; Render(editor, Path.Combine(root, "save-editor-inventory.png"));
        tabs.SelectedIndex = 4; Render(editor, Path.Combine(root, "save-editor-bosses.png"));
        Check(Descendants((DependencyObject)editor.Content).OfType<FrameworkElement>().All(e => e.ToolTip == null), "Save editor has no hover descriptions");
        editor.Close(); manager.Close(); owner.Close();

        var realRoot = Environment.GetEnvironmentVariable("BLOODBORNE_TEST_SAVES");
        var game = Environment.GetEnvironmentVariable("BLOODBORNE_TEST_GAME");
        if (!string.IsNullOrWhiteSpace(realRoot))
        {
            var count = 0;
            foreach (var file in Directory.GetFiles(realRoot, "userdata*", SearchOption.AllDirectories))
            {
                var bytes = File.ReadAllBytes(file);
                if (bytes.Length != SaveSlot.FileSize || SaveSlot.IsEmptySlot(bytes)) continue;
                var parsed = SaveSlot.Parse(bytes);
                Check(parsed.ToBytes().AsSpan().SequenceEqual(bytes), "Private save copy round trips without modification"); count++;
            }
            Check(count > 0, "At least one real save copy was checked");
            var privateRepository = new SaveRepository(Path.Combine(saves, "private-real-copy"), backups, () => false);
            var privateProfile = privateRepository.Profile("Default");
            privateRepository.Import(privateProfile, realRoot);
            var privateCharacter = privateRepository.Characters(privateProfile).First();
            var privateBytes = File.ReadAllBytes(privateCharacter.File);
            var privateSlot = SaveSlot.Parse(privateBytes);
            var vitality = privateSlot.Stat(SaveStat.Vitality);
            privateSlot.SetStat(SaveStat.Vitality, vitality < 99 ? vitality + 1 : vitality - 1);
            privateRepository.Commit(privateProfile, privateCharacter, privateBytes, privateSlot);
            Check(SaveSlot.Parse(File.ReadAllBytes(privateCharacter.File)).Level == privateSlot.Level, "A real save copy can be edited with its character summary synchronized");
        }
        if (!string.IsNullOrWhiteSpace(game))
        {
            var installedBackups = new SaveRepository(SaveRepository.FixedRoot);
            Check(installedBackups.Backups(installedBackups.Profile("Default")).Count >= 10, "Existing installed backups are recognized without modifying them");
            var catalog = SaveCatalog.TryLoad(game, "en", out var error);
            Check(catalog != null && catalog.Goods.Count > 100 && catalog.Weapons.Count > 50 && catalog.Armor.Count > 50 && catalog.Lamps.Count == 44,
                "Installed game catalog loads: " + error);
            var catalogOwner = new MainWindow(settings, starter);
            var fullEditor = new SaveEditorWindow(catalogOwner, repository, profile, repository.Characters(profile).Single(), game);
            var fullTabs = Descendants((DependencyObject)fullEditor.Content).OfType<TabControl>().First();
            fullTabs.SelectedIndex = 2; Render(fullEditor, Path.Combine(root, "save-editor-items.png"));
            fullTabs.SelectedIndex = 3; Render(fullEditor, Path.Combine(root, "save-editor-equipment.png"));
            fullTabs.SelectedIndex = 5; Render(fullEditor, Path.Combine(root, "save-editor-lamps.png"));
            fullEditor.Close(); catalogOwner.Close();
        }
        File.WriteAllText(Path.Combine(root, "save-checks.txt"), $"Passed {_count} Save Checks\n");
        Console.WriteLine($"Completed {_count} save checks");
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i); yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
    private static void Render(Window window, string path)
    {
        var content = (FrameworkElement)window.Content;
        content.Measure(new Size(window.Width, window.Height)); content.Arrange(new Rect(0, 0, window.Width, window.Height)); content.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)window.Width, (int)window.Height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var stream = File.Create(path); encoder.Save(stream);
    }
}

