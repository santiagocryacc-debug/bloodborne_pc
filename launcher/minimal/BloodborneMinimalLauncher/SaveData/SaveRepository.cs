using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BloodborneMinimalLauncher.SaveData;

public sealed record SaveProfile(string Name, string Root)
{
    public string Folder => Path.Combine(Root, "savedata", "1", "CUSA00207", "SPRJ0005");
    public override string ToString() => Name;
}
public sealed record SaveCharacter(int Slot, string Name, int Level, string File)
{
    public override string ToString() => $"{Slot + 1}   {Name}   |   Level {Level}";
}
public sealed record SaveBackup(string Folder, DateTime When)
{
    public override string ToString() => When.ToString("yyyy/MM/dd   HH:mm:ss", CultureInfo.InvariantCulture);
}

/// <summary>All writes are staged and verified. Previous backups are never pruned.</summary>
public sealed class SaveRepository
{
    public const string FixedRoot = @"Y:\Games\Library\Standalone\Bloodborne\Install\Game Files\Saves";
    public const string FixedBackups = @"Y:\Games\Library\Standalone\Bloodborne\Install\Game Files\Backups";
    private const string Metadata = ".bloodborne-backup.json";
    private readonly Func<bool> _running;
    private readonly string _root;
    private readonly string _backups;
    // Tests can simulate a failed swap without touching the user's files.
    internal Action? BeforeInstall { get; set; }
    private sealed record Manifest(int Version, string ProfileKey, Dictionary<string, string> Files);

    public SaveRepository(string root, string? backups = null, Func<bool>? running = null)
    {
        _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        _backups = Path.TrimEndingDirectorySeparator(Path.GetFullPath(backups ?? FixedBackups));
        _running = running ?? GameRunning;
    }

    public static bool GameRunning()
    {
        foreach (var name in new[] { "bb-start", "bb-probe" })
        {
            var processes = Process.GetProcessesByName(name);
            var found = processes.Length > 0;
            foreach (var process in processes) process.Dispose();
            if (found) return true;
        }
        return false;
    }

    public SaveProfile Profile(string name)
    {
        if (name == "Default") return new(name, _root);
        if (string.IsNullOrWhiteSpace(name) || name.Length > 40 || name != name.Trim() ||
            !Regex.IsMatch(name, @"^[\p{L}\p{N}][\p{L}\p{N} _]*$") ||
            Regex.IsMatch(name, @"^(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])$", RegexOptions.IgnoreCase))
            throw new InvalidOperationException("Invalid Profile Name");
        return new(name, Path.Combine(_root, "profiles", name));
    }

    private void Own(SaveProfile profile)
    {
        if (!Equal(Profile(profile.Name).Root, profile.Root)) throw new InvalidOperationException("Invalid Profile");
        Safe(profile.Folder);
    }

    public List<SaveProfile> Profiles()
    {
        Safe(_root);
        var result = new List<SaveProfile> { Profile("Default") };
        var folder = Path.Combine(_root, "profiles");
        if (!Directory.Exists(folder)) return result;
        Safe(folder);
        foreach (var dir in Directory.GetDirectories(folder).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
        {
            Safe(dir);
            try { result.Add(Profile(Path.GetFileName(dir))); }
            catch (InvalidOperationException) { }
        }
        return result;
    }

    public void CreateProfile(string name, SaveProfile? copy)
    {
        using var guard = Guard();
        var profile = Profile(name);
        Own(profile);
        if (name == "Default" || Directory.Exists(profile.Root)) throw new InvalidOperationException("Profile Already Exists");
        if (copy == null) { Directory.CreateDirectory(profile.Root); return; }
        Own(copy);
        Validate(copy.Folder);
        var stage = profile.Root + ".creating." + Guid.NewGuid().ToString("N");
        try
        {
            CopyVerified(copy.Folder, Path.Combine(stage, "savedata", "1", "CUSA00207", "SPRJ0005"));
            EnsureStopped();
            Directory.CreateDirectory(Path.GetDirectoryName(profile.Root)!);
            Directory.Move(stage, profile.Root);
        }
        finally { RemoveOwned(stage); }
    }

    public List<SaveCharacter> Characters(SaveProfile profile)
    {
        Own(profile);
        var list = new List<SaveCharacter>();
        for (var slot = 0; slot < 10; slot++)
        {
            var path = Path.Combine(profile.Folder, $"userdata{slot:D4}");
            if (!File.Exists(path)) continue;
            Safe(path);
            var bytes = File.ReadAllBytes(path);
            if (SaveSlot.IsEmptySlot(bytes)) continue;
            var save = SaveSlot.Parse(bytes);
            list.Add(new(slot, save.Name, save.Level, path));
        }
        return list;
    }

    public string Backup(SaveProfile profile)
    {
        using var guard = Guard();
        Own(profile);
        RecoverCore(profile);
        return BackupUnvalidated(profile);
    }

    private string BackupCore(SaveProfile profile)
    {
        Validate(profile.Folder);
        Safe(_backups);
        Directory.CreateDirectory(_backups);
        var prefix = profile.Name == "Default" && Equal(_root, FixedRoot) ? "SPRJ0005_" : "SPRJ0005_" + profile.Name + "_";
        var target = Path.Combine(_backups, prefix + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture));
        var initial = target;
        for (var i = 1; Directory.Exists(target); i++) target = initial + "_" + i;
        var stage = target + ".creating." + Guid.NewGuid().ToString("N");
        try
        {
            CopyVerified(profile.Folder, stage);
            var manifest = new Manifest(1, Key(profile), Fingerprint(stage));
            File.WriteAllText(Path.Combine(stage, Metadata), JsonSerializer.Serialize(manifest));
            Directory.Move(stage, target);
        }
        finally { RemoveOwned(stage); }
        return target;
    }

    public List<SaveBackup> Backups(SaveProfile profile)
    {
        Own(profile);
        Safe(_backups);
        if (!Directory.Exists(_backups)) return new();
        var list = new List<SaveBackup>();
        foreach (var dir in Directory.GetDirectories(_backups))
        {
            var match = Regex.Match(Path.GetFileName(dir), @"^SPRJ0005_.*?(\d{4}-\d\d-\d\d_\d\d-\d\d-\d\d)(?:_\d+)?$");
            if (!match.Success) continue;
            Safe(dir);
            var metadata = Path.Combine(dir, Metadata);
            if (File.Exists(metadata))
            {
                try { if (ReadManifest(dir).ProfileKey != Key(profile)) continue; }
                catch (Exception e) when (e is IOException or JsonException or InvalidDataException) { continue; }
            }
            else if (profile.Name != "Default" || !Equal(_root, FixedRoot) ||
                     !Regex.IsMatch(Path.GetFileName(dir), @"^SPRJ0005_\d{4}-\d\d-\d\d_\d\d-\d\d-\d\d(?:_\d+)?$")) continue;
            if (DateTime.TryParseExact(match.Groups[1].Value, "yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var when)) list.Add(new(dir, when));
        }
        return list.OrderByDescending(b => b.When).ThenByDescending(b => b.Folder).ToList();
    }

    public void Restore(SaveProfile profile, SaveBackup backup)
    {
        using var guard = Guard();
        Own(profile);
        RecoverCore(profile);
        if (!Backups(profile).Any(b => Equal(b.Folder, backup.Folder))) throw new InvalidOperationException("Invalid Backup");
        var metadata = Path.Combine(backup.Folder, Metadata);
        if (File.Exists(metadata) && !Same(Fingerprint(backup.Folder), ReadManifest(backup.Folder).Files))
            throw new InvalidDataException("Backup Verification Failed");
        Install(profile, backup.Folder);
    }

    public void Import(SaveProfile profile, string source)
    {
        using var guard = Guard();
        Own(profile);
        RecoverCore(profile);
        source = Path.GetFullPath(source);
        Safe(source);
        var candidates = new[] { source, Path.Combine(source, "SPRJ0005"),
            Path.Combine(source, "savedata", "1", "CUSA00207", "SPRJ0005"),
            Path.Combine(source, "1", "CUSA00207", "SPRJ0005") };
        var folder = candidates.FirstOrDefault(p => File.Exists(Path.Combine(p, "userdata0010")))
            ?? throw new InvalidDataException("Save Folder Not Found");
        if (Equal(folder, profile.Folder)) throw new InvalidOperationException("Save Already Selected");
        Install(profile, folder);
    }

    public void Commit(SaveProfile profile, SaveCharacter character, byte[] original, SaveSlot edited)
    {
        using var guard = Guard();
        Own(profile);
        RecoverCore(profile);
        var expected = Path.Combine(profile.Folder, $"userdata{character.Slot:D4}");
        if (character.Slot is < 0 or > 9 || !Equal(character.File, expected)) throw new InvalidOperationException("Invalid Character");
        if (!File.ReadAllBytes(expected).AsSpan().SequenceEqual(original)) throw new InvalidOperationException("Save Changed. Reopen Editor");
        var parsed = SaveSlot.Parse(edited.ToBytes());
        var before = SaveSlot.Parse(original);
        if (parsed.Name != before.Name) throw new InvalidDataException("Save Verification Failed");
        var backup = BackupCore(profile);
        var stage = NewStage(profile);
        try
        {
            CopyVerified(backup, stage);
            WriteChecked(Path.Combine(stage, Path.GetFileName(expected)), parsed.ToBytes());
            if (parsed.Level != before.Level)
            {
                var summary = Path.Combine(stage, "userdata0010");
                var updated = SaveSlotSummary.WithLevel(File.ReadAllBytes(summary), character.Slot, parsed.Name, parsed.Level)
                    ?? throw new InvalidDataException("Character Summary Mismatch");
                WriteChecked(summary, updated);
            }
            Validate(stage);
            if (!Same(Fingerprint(profile.Folder), ReadManifest(backup).Files)) throw new InvalidOperationException("Save Changed. Reopen Editor");
            Swap(profile, stage);
        }
        finally { RemoveOwned(stage); }
    }

    private void Install(SaveProfile profile, string source)
    {
        Validate(source);
        var stage = NewStage(profile);
        try
        {
            CopyVerified(source, stage);
            Validate(stage);
            Dictionary<string, string>? before = null;
            if (Directory.Exists(profile.Folder))
            {
                // Even a damaged current save is retained before recovery.
                var backup = BackupUnvalidated(profile);
                before = ReadManifest(backup).Files;
            }
            if (before != null && !Same(Fingerprint(profile.Folder), before)) throw new InvalidOperationException("Save Changed. Try Again");
            Swap(profile, stage);
        }
        finally { RemoveOwned(stage); }
    }

    private string BackupUnvalidated(SaveProfile profile)
    {
        // BackupCore validates normally; recovery must also protect corrupt saves.
        if (!Directory.EnumerateFiles(profile.Folder).Any()) return BackupRaw(profile);
        try { Validate(profile.Folder); return BackupCore(profile); }
        catch (SaveFormatException) { return BackupRaw(profile); }
        catch (InvalidDataException) { return BackupRaw(profile); }
    }

    private string BackupRaw(SaveProfile profile)
    {
        Safe(_backups);
        Directory.CreateDirectory(_backups);
        var target = Path.Combine(_backups, "SPRJ0005_Recovery_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture) + "_" + Random.Shared.Next(10000, 99999));
        CopyVerified(profile.Folder, target);
        File.WriteAllText(Path.Combine(target, Metadata), JsonSerializer.Serialize(new Manifest(1, Key(profile), Fingerprint(target))));
        return target;
    }

    public void RecoverPending(SaveProfile profile)
    {
        using var guard = Guard(); Own(profile); RecoverCore(profile);
    }

    private void RecoverCore(SaveProfile profile)
    {
        var previous = profile.Folder + ".previous";
        Safe(previous);
        if (!Directory.Exists(previous)) return;
        if (!Directory.Exists(profile.Folder)) Directory.Move(previous, profile.Folder);
        else
        {
            // The new directory was installed before interruption. Retain the previous
            // directory as a normal recovery backup before finishing the transaction.
            Validate(profile.Folder);
            Safe(_backups);
            Directory.CreateDirectory(_backups);
            var target = Path.Combine(_backups, "SPRJ0005_Recovery_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture) + "_" + Random.Shared.Next(10000, 99999));
            CopyVerified(previous, target);
            File.WriteAllText(Path.Combine(target, Metadata), JsonSerializer.Serialize(new Manifest(1, Key(profile), Fingerprint(target))));
            RemoveOwned(previous);
        }
    }

    private void Swap(SaveProfile profile, string stage)
    {
        EnsureStopped();
        var previous = profile.Folder + ".previous";
        Safe(previous);
        if (Directory.Exists(previous)) throw new InvalidOperationException("Save Recovery Pending");
        Directory.CreateDirectory(Path.GetDirectoryName(profile.Folder)!);
        var hadSave = Directory.Exists(profile.Folder);
        if (hadSave) Directory.Move(profile.Folder, previous);
        try
        {
            BeforeInstall?.Invoke();
            EnsureStopped();
            Directory.Move(stage, profile.Folder);
        }
        catch
        {
            if (hadSave && !Directory.Exists(profile.Folder)) Directory.Move(previous, profile.Folder);
            throw;
        }
        // Keep a recoverable directory if cleanup is interrupted; never hide the failure.
        if (hadSave) RemoveOwned(previous);
    }

    private string NewStage(SaveProfile profile)
    {
        var stage = profile.Folder + ".staging." + Guid.NewGuid().ToString("N");
        Safe(stage); return stage;
    }

    public static void Validate(string folder)
    {
        Safe(folder);
        if (!Directory.Exists(folder)) throw new DirectoryNotFoundException("Save Folder Not Found");
        var files = Fingerprint(folder);
        if (!files.ContainsKey("userdata0010") || new FileInfo(Path.Combine(folder, "userdata0010")).Length != SaveSlotSummary.FileSize)
            throw new InvalidDataException("Invalid Save Summary");
        var count = 0;
        for (var i = 0; i < 10; i++)
        {
            var path = Path.Combine(folder, $"userdata{i:D4}");
            if (!File.Exists(path)) continue;
            var bytes = File.ReadAllBytes(path);
            if (!SaveSlot.IsEmptySlot(bytes)) SaveSlot.Parse(bytes);
            count++;
        }
        if (count == 0) throw new InvalidDataException("No Save Slots Found");
    }

    private static Manifest ReadManifest(string folder)
    {
        Safe(Path.Combine(folder, Metadata));
        var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(Path.Combine(folder, Metadata)));
        if (manifest == null || manifest.Version != 1 || manifest.Files == null) throw new InvalidDataException("Invalid Backup");
        return manifest;
    }

    private static string Key(SaveProfile profile) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(profile.Root).ToUpperInvariant())));
    private static bool Equal(string a, string b) => string.Equals(Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
    private static bool Same(Dictionary<string, string> a, Dictionary<string, string> b) => a.Count == b.Count && a.All(p => b.TryGetValue(p.Key, out var hash) && hash == p.Value);

    private static Dictionary<string, string> Fingerprint(string folder)
    {
        Safe(folder);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        void Walk(string dir)
        {
            foreach (var child in Directory.EnumerateFileSystemEntries(dir))
            {
                Safe(child);
                if (Directory.Exists(child)) { Walk(child); continue; }
                if (Path.GetFileName(child) == Metadata) continue;
                using var stream = File.OpenRead(child);
                result.Add(Path.GetRelativePath(folder, child), Convert.ToHexString(SHA256.HashData(stream)));
            }
        }
        Walk(folder); return result;
    }

    private static void CopyVerified(string source, string target)
    {
        Safe(source); Safe(target);
        var from = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var to = Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (to.StartsWith(from, StringComparison.OrdinalIgnoreCase) || from.StartsWith(to, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Overlapping Save Folders");
        var before = Fingerprint(source);
        Directory.CreateDirectory(target);
        foreach (var file in before.Keys)
        {
            var path = Path.Combine(target, file);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.Copy(Path.Combine(source, file), path, overwrite: false);
        }
        if (!Same(before, Fingerprint(target)) || !Same(before, Fingerprint(source))) throw new InvalidDataException("Copy Verification Failed");
    }

    private static void WriteChecked(string path, byte[] bytes)
    {
        Safe(path);
        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
        { stream.Write(bytes); stream.Flush(flushToDisk: true); }
        if (!File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes)) throw new InvalidDataException("Save Verification Failed");
    }

    private void EnsureStopped() { if (_running()) throw new InvalidOperationException("Close Bloodborne First"); }
    private IDisposable Guard()
    {
        EnsureStopped();
        var mutex = new Mutex(false, "Local\\BloodborneSaves_" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(_backups.ToUpperInvariant()))));
        bool acquired;
        try { acquired = mutex.WaitOne(0); }
        catch (AbandonedMutexException) { acquired = true; }
        if (!acquired) { mutex.Dispose(); throw new InvalidOperationException("Save Operation In Progress"); }
        return new Lease(mutex);
    }
    private sealed class Lease(Mutex mutex) : IDisposable { public void Dispose() { mutex.ReleaseMutex(); mutex.Dispose(); } }

    private static void Safe(string path)
    {
        var current = Path.GetFullPath(path);
        while (!string.IsNullOrEmpty(current))
        {
            if ((Directory.Exists(current) || File.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Linked Save Folders Are Unsupported");
            current = Path.GetDirectoryName(current)!;
        }
    }

    private static void RemoveOwned(string path)
    {
        // Only called with exact staging/previous directories created by this operation.
        Safe(path);
        if (!Directory.Exists(path)) return;
        Fingerprint(path); // Reject nested junctions before recursive deletion.
        Directory.Delete(path, recursive: true);
    }
}
