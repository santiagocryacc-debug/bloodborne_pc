using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace BloodborneMinimalLauncher;

public partial class MainWindow : Window
{
    private string _portDir = "";
    private string _gameDir = "";
    private string _savesDir = "";
    private IniFile? _ini;
    private bool _launchInProgress;
    private bool _backupInProgress;
    private readonly string _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BloodborneMinimalLauncher", "settings.ini");

    private static readonly (string Label, string Value)[] Resolutions = {
        ("1920 1080", "1920x1080"), ("2560 1440", "2560x1440"),
        ("3840 2160", "3840x2160") };
    private static readonly (string Label, string Value)[] Upscalers = {
        ("FSR 4", "fsr4"), ("FSR 4.1.1", "fsr411"), ("FSR 3", "fsr3"),
        ("DLSS", "dlss"), ("TAA", "taa"), ("Off", "off") };
    private static readonly (string Label, string Value)[] Presets = {
        ("Native", "0"), ("Quality", "1"), ("Balanced", "2"), ("Performance", "3"), ("Ultra Performance", "4") };
    // Supported by Lantern Engine 0.9.34 beta 9.6 and newer.
    private static readonly (string Label, string Value)[] FrameGenerationModes = {
        ("Off", "off"), ("FSR 3.1 x2", "fsr3") };
    private static readonly (string Label, string Value)[] FrameRates = {
        ("30 FPS", "30"), ("60 FPS", "60"), ("90 FPS", "90"), ("Unlocked", "uncap") };
    private static readonly (string Label, string Value)[] FrameCaps = {
        ("Auto", ""), ("60 FPS", "60"), ("90 FPS", "90"), ("120 FPS", "120"),
        ("165 FPS", "165"), ("240 FPS", "240"), ("No Limit", "0") };
    private static readonly (string Label, string Value)[] PresentModes = {
        ("Mailbox", "Mailbox"), ("FIFO", "Fifo"), ("FIFO Relaxed", "FifoRelaxed"), ("Immediate", "Immediate") };
    private static readonly (string Label, string Value)[] LiveResolutionModes = {
        ("Auto", "auto"), ("Off", "0"), ("On", "1") };
    private static readonly (string Label, string Value)[] DrawPipes = {
        ("Auto", ""), ("On", "1"), ("Off", "0") };
    private static readonly (string Label, string Value)[] Readbacks = {
        ("Relaxed", ""), ("Off", "0"), ("Precise", "2") };
    private static readonly (string Label, string Value)[] ModelLods = {
        ("As In Game", "0"), ("Highest", "-2"), ("Lower", "1"), ("Lowest", "2") };

    public MainWindow()
    {
        InitializeComponent();
        LoadArtwork();
        Fill(ResolutionBox, Resolutions);
        Fill(UpscalerBox, Upscalers);
        Fill(PresetBox, Presets);
        Fill(FrameRateBox, FrameRates);
        Fill(FrameCapBox, FrameCaps);
        Fill(FrameGenerationBox, FrameGenerationModes);
        Fill(PresentationBox, PresentModes);
        Fill(LiveResolutionBox, LiveResolutionModes);
        Fill(ModelLodBox, ModelLods);
        Fill(DrawPipeBox, DrawPipes);
        Fill(ReadbacksBox, Readbacks);
        LoadSettings();
        RefreshIni();
        AutoDetectGameDirectory();
        UpdateGameDirectoryDisplay();
        UpdateSavesDirectoryDisplay();
    }

    private void LoadArtwork()
    {
        // Optional assets. Missing images never prevent the launcher from starting.
        LoadArtworkImage(HeroArtwork, "Yharnam.jpg");
    }

    private static void LoadArtworkImage(System.Windows.Controls.Image target, string filename)
    {
        try
        {
            var file = Path.Combine(AppContext.BaseDirectory, "Assets", filename);
            if (!File.Exists(file))
                return;
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(file, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            target.Source = image;
        }
        catch (Exception)
        {
            // Keep the fallback background if an asset is invalid.
        }
    }

    private async void Backup_Click(object sender, RoutedEventArgs e)
    {
        if (_launchInProgress || _backupInProgress)
            return;
        // Always back up the user's requested source, independently of the active save profile.
        var source = @"Y:\Games\Library\Standalone\Bloodborne\Install\Game Files\Saves\savedata\1\CUSA00207\SPRJ0005";
        var backupRoot = @"Y:\Games\Library\Standalone\Bloodborne\Install\Game Files\Backups";
        try
        {
            if (!Directory.Exists(source))
                throw new DirectoryNotFoundException("Source folder not found: " + source);
            if (Process.GetProcessesByName("bb-probe").Length > 0)
                throw new InvalidOperationException("Close Bloodborne before creating a backup.");

            var destination = Path.Combine(backupRoot, "SPRJ0005_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            for (var suffix = 1; Directory.Exists(destination); suffix++)
                destination = Path.Combine(backupRoot, "SPRJ0005_" +
                    DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_" + suffix);

            _backupInProgress = true;
            BackupButton.IsEnabled = false;
            PlayButton.IsEnabled = false;
            StatusText.Text = "Creating backup...";

            var copied = await Task.Run(() =>
            {
                // Never modify, delete or move source files. Do not overwrite previous backups.
                Directory.CreateDirectory(destination);
                foreach (var folder in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
                {
                    var relative = Path.GetRelativePath(source, folder);
                    Directory.CreateDirectory(Path.Combine(destination, relative));
                }
                var count = 0;
                foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
                {
                    var relative = Path.GetRelativePath(source, file);
                    var output = Path.Combine(destination, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                    File.Copy(file, output, overwrite: false);
                    count++;
                }
                return count;
            });
            StatusText.Text = "Backup Complete";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Backup Failed: " + ex.Message;
        }
        finally
        {
            _backupInProgress = false;
            BackupButton.IsEnabled = true;
            PlayButton.IsEnabled = !_launchInProgress;
        }
    }

    private void OpenDisplay_Click(object sender, RoutedEventArgs e) => SettingsTabs.SelectedIndex = 0;
    private void OpenGraphics_Click(object sender, RoutedEventArgs e) => SettingsTabs.SelectedIndex = 1;
    private void OpenEffects_Click(object sender, RoutedEventArgs e) => SettingsTabs.SelectedIndex = 2;
    private void OpenSaves_Click(object sender, RoutedEventArgs e) => SettingsTabs.SelectedIndex = 3;

    private static void Fill(System.Windows.Controls.ComboBox box, (string Label, string Value)[] items)
    {
        box.ItemsSource = items.Select(x => new ComboBoxItem { Content = x.Label, Tag = x.Value }).ToList();
    }

    private static string Selected(System.Windows.Controls.ComboBox box, string fallback) =>
        (box.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? fallback;

    private static void Select(System.Windows.Controls.ComboBox box, string value, string fallback)
    {
        foreach (ComboBoxItem item in box.Items)
            if (Equals(item.Tag?.ToString(), value)) { box.SelectedItem = item; return; }
        foreach (ComboBoxItem item in box.Items)
            if (Equals(item.Tag?.ToString(), fallback)) { box.SelectedItem = item; return; }
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return;
            var settings = new IniFile(_settingsPath);
            _portDir = settings.Get("port_dir");
            _gameDir = settings.Get("game_dir");
            _savesDir = settings.Get("saves_dir");
            Select(DrawPipeBox, settings.Get("draw_pipe", ""), "");
            Select(ReadbacksBox, settings.Get("readbacks", ""), "");
            Select(FrameRateBox, settings.Get("fps_mode", "60"), "60");
            Select(FrameCapBox, settings.Get("frame_cap", ""), "");
            Select(PresentationBox, settings.Get("present_mode", "Mailbox"), "Mailbox");
            SharpUiBox.IsChecked = settings.Get("sharp_ui", "1") == "1";
        }
        catch { }
    }

    private void SaveSettings()
    {
        var settings = new IniFile(_settingsPath);
        settings.Set("port_dir", _portDir);
        settings.Set("game_dir", _gameDir);
        settings.Set("saves_dir", _savesDir);
        settings.Set("draw_pipe", Selected(DrawPipeBox, ""));
        settings.Set("readbacks", Selected(ReadbacksBox, ""));
        settings.Set("fps_mode", Selected(FrameRateBox, "60"));
        settings.Set("frame_cap", Selected(FrameCapBox, ""));
        settings.Set("present_mode", Selected(PresentationBox, "Mailbox"));
        settings.Set("sharp_ui", SharpUiBox.IsChecked == true ? "1" : "0");
        settings.Save();
    }

    private string FindIniPath()
    {
        var candidates = new System.Collections.Generic.List<string>();
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; current is not null && depth < 5; depth++, current = current.Parent)
            candidates.Add(current.FullName);
        candidates.Add(Environment.CurrentDirectory);
        if (!string.IsNullOrWhiteSpace(_portDir))
            candidates.Add(_portDir);

        var uniqueCandidates = candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var gameDirs = uniqueCandidates
            .Where(dir => File.Exists(Path.Combine(dir, "blobborne.exe")))
            .ToList();

        foreach (var dir in gameDirs)
        {
            foreach (var name in new[] { "BBPort.ini", "bbport.ini" })
            {
                var candidate = Path.Combine(dir, name);
                if (File.Exists(candidate))
                {
                    _portDir = dir;
                    return candidate;
                }
            }
        }

        foreach (var dir in uniqueCandidates)
        {
            foreach (var name in new[] { "BBPort.ini", "bbport.ini" })
            {
                var candidate = Path.Combine(dir, name);
                if (File.Exists(candidate))
                {
                    _portDir = dir;
                    return candidate;
                }
            }
        }

        _portDir = "";
        return "";
    }

    private void RefreshIni()
    {
        try
        {
            var path = FindIniPath();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                _ini = null;
                StatusText.Text = "BBPort.ini not found. Place this launcher in the game folder, beside blobborne.exe.";
                return;
            }

            _ini = new IniFile(path);
            Select(ResolutionBox, _ini.Get("output_res", "1920x1080"), "1920x1080");
            Select(FrameRateBox, _ini.Get("fps_mode", "60"), "60");
            Select(PresentationBox, _ini.Get("present_mode", "Mailbox"), "Mailbox");
            Select(UpscalerBox, _ini.Get("upscaler", "fsr4"), "fsr4");
            Select(PresetBox, _ini.Get("preset", "1"), "1");
            Select(FrameGenerationBox, _ini.Get("frame_gen", "off"), "off");
            HalfRefreshBox.IsChecked = _ini.Get("frame_gen_cap", "1") == "1";
            Select(ModelLodBox, _ini.Get("model_lod", "0"), "0");
            FullscreenBox.IsChecked = _ini.Get("fullscreen", "1") == "1";
            HideMouseBox.IsChecked = _ini.Get("hide_cursor", "1") == "1";
            PictureTuningBox.IsChecked = _ini.Get("graphics_controls", "1") == "1";
            Select(LiveResolutionBox, _ini.Get("live_resolution", "auto"), "auto");
            SharpenBox.IsChecked = _ini.Get("sharpen", "1") == "1";
            SharpnessSlider.Value = double.TryParse(_ini.Get("sharpness", "0.50"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var sharpness) ? Math.Clamp(sharpness, 0, 2) : 0.5;
            SharpnessValueText.Text = SharpnessSlider.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            ObjectMotionBox.IsChecked = _ini.Get("object_motion", "1") == "1";
            SsaoBox.IsChecked = _ini.Get("effect_ssao", "1") == "1";
            DynamicShadowsBox.IsChecked = _ini.Get("effect_dynamic_shadows", "1") == "1";
            ChromaticAberrationBox.IsChecked = _ini.Get("effect_chromatic_aberration", "1") == "1";
            DepthOfFieldBox.IsChecked = _ini.Get("effect_dof", "1") == "1";
            MotionBlurBox.IsChecked = _ini.Get("effect_motion_blur", "1") == "1";
            GameAaBox.IsChecked = _ini.Get("effect_game_aa", "1") == "1";
            ScreenSpaceReflectionsBox.IsChecked = _ini.Get("effect_ssr", "0") == "1";
            SkipIntroBox.IsChecked = _ini.Get("skip_intro", "0") == "1";
            StatusText.Text = "";
        }
        catch (Exception ex)
        {
            _ini = null;
            StatusText.Text = "Could not read settings: " + ex.Message;
        }
    }

    private void UpdateGameDirectoryDisplay()
    {
        GameDirectoryText.Text = string.IsNullOrWhiteSpace(_gameDir)
            ? "No game folder selected"
            : _gameDir;
    }

    private void BrowseGameDirectory_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select the game files directory",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_gameDir) ? _gameDir : ""
        };
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;

        var candidate = dialog.SelectedPath;
        if (!Directory.Exists(candidate))
        {
            System.Windows.MessageBox.Show(this,
                "The selected game directory does not exist.",
                "Bloodborne", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _gameDir = candidate;
            UpdateGameDirectoryDisplay();
            StatusText.Text = "Game folder selected. Click SAVE FOLDER to remember it.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Could not save the game folder: " + ex.Message;
            System.Windows.MessageBox.Show(this, ex.Message, "Bloodborne",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void UpdateSavesDirectoryDisplay()
    {
        SavesDirectoryText.Text = string.IsNullOrWhiteSpace(_savesDir)
            ? "Default: Game Files\\Saves"
            : _savesDir;
    }

    private void BrowseSavesFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select the folder containing the saves you want to use",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_savesDir) ? _savesDir :
                Directory.Exists(Path.Combine(_gameDir, "Saves")) ? Path.Combine(_gameDir, "Saves") : ""
        };
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;
        if (!Directory.Exists(dialog.SelectedPath))
        {
            System.Windows.MessageBox.Show(this, "Saves folder does not exist.", "Bloodborne",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var previous = _savesDir;
        try
        {
            _savesDir = dialog.SelectedPath;
            SaveSettings();
            UpdateSavesDirectoryDisplay();
            StatusText.Text = "Saves folder selected.";
        }
        catch (Exception ex)
        {
            _savesDir = previous;
            UpdateSavesDirectoryDisplay();
            System.Windows.MessageBox.Show(this, ex.Message, "Bloodborne",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private string? LocateEngineStart()
    {
        var candidates = new System.Collections.Generic.List<string>();
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; directory != null && depth < 5; depth++, directory = directory.Parent)
            candidates.Add(directory.FullName);
        if (!string.IsNullOrWhiteSpace(_portDir))
            candidates.Add(_portDir);
        foreach (var root in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var path = Path.Combine(root, "engine", "out", "bb-start.exe");
            if (File.Exists(path))
                return path;
        }
        return null;
    }

    private void AutoDetectGameDirectory()
    {
        // Prefer the engine-provided path, even when this distribution stores eboot.bin in a subfolder.
        var engineStart = LocateEngineStart();
        if (engineStart is null)
            return;
        var pathFile = Path.Combine(Path.GetDirectoryName(engineStart)!, "game_dir");
        if (!File.Exists(pathFile))
            return;
        try
        {
            var candidate = File.ReadAllText(pathFile).Trim().Trim('"');
            if (Directory.Exists(candidate))
            {
                _gameDir = candidate;
                UpdateGameDirectoryDisplay();
                SaveSettings();
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private async void Play_Click(object sender, RoutedEventArgs e)
    {
        if (_launchInProgress || _backupInProgress)
            return;
        try
        {
            var engineStart = LocateEngineStart()
                ?? throw new FileNotFoundException("engine/out/bb-start.exe was not found beside the port.");
            // Resolve again at launch time: the engine's game_dir is the source of truth.
            var gamePathFile = Path.Combine(Path.GetDirectoryName(engineStart)!, "game_dir");
            if (File.Exists(gamePathFile))
            {
                var fromEngine = File.ReadAllText(gamePathFile)
                    .Trim().TrimStart('\uFEFF').Trim('"').Trim();
                if (Directory.Exists(fromEngine))
                    _gameDir = fromEngine;
            }

            if (string.IsNullOrWhiteSpace(_gameDir) || !Directory.Exists(_gameDir))
                throw new InvalidOperationException(
                    "Game directory not accessible. Engine file: " + gamePathFile +
                    "\nFile exists: " + File.Exists(gamePathFile) +
                    "\nSaved directory: " + (_gameDir.Length == 0 ? "(empty)" : _gameDir) +
                    "\nCheck that drive Y: is available, or select the folder manually.");

            UpdateGameDirectoryDisplay();

            var info = new ProcessStartInfo(engineStart)
            {
                WorkingDirectory = Path.GetDirectoryName(engineStart)!,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            info.ArgumentList.Add("--game-dir");
            info.ArgumentList.Add(_gameDir);
            info.Environment["BB_FPS"] = Selected(FrameRateBox, "60");
            var cap = Selected(FrameCapBox, "");
            if (!string.IsNullOrEmpty(cap)) info.Environment["BB_FPS_LIMIT"] = cap;
            var drawPipe = Selected(DrawPipeBox, "");
            if (drawPipe.Length > 0) info.Environment["BB_DRAW_PIPE"] = drawPipe;
            else info.Environment.Remove("BB_DRAW_PIPE");
            var readbacks = Selected(ReadbacksBox, "");
            if (readbacks.Length > 0) info.Environment["BB_READBACKS"] = readbacks;
            else info.Environment.Remove("BB_READBACKS");
            // Sharp UI is an engine switch, not a bbport.ini setting.
            // Match the upstream launcher: only pass the variable when enabled.
            if (SharpUiBox.IsChecked == true) info.Environment["BB_SHARP_UI"] = "1";
            else info.Environment.Remove("BB_SHARP_UI");

            // Never copy or move saves: point bbhost at the selected existing folder.
            var chosenSaves = string.IsNullOrWhiteSpace(_savesDir)
                ? Path.Combine(_gameDir, "Saves")
                : _savesDir;
            if (!Directory.Exists(chosenSaves))
                throw new DirectoryNotFoundException(
                    "Saves folder not found: " + chosenSaves +
                    ". Select an existing SAVES FOLDER before playing.");
            info.Environment["BB_USER_DIR"] = chosenSaves;
            // The engine is responsible for preparing patches, mods and launching bb-probe.
            // Do not invoke the original Bloodborne Launcher.exe.
            _launchInProgress = true;
            PlayButton.IsEnabled = false;
            StatusText.Text = "Starting Bloodborne...";
            using var process = new Process { StartInfo = info };
            if (!process.Start())
                throw new InvalidOperationException("bb-start.exe could not be started.");

            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BloodborneMinimalLauncher", "last_launch.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            var output = await stdout;
            var errors = await stderr;
            await File.WriteAllTextAsync(logPath,
                $"Exit code: {process.ExitCode}{Environment.NewLine}{output}{Environment.NewLine}{errors}");
            StatusText.Text = process.ExitCode == 0
                ? "Game process finished."
                : $"Game exited with code {process.ExitCode}. Log: {logPath}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Could not launch: " + ex.Message;
            System.Windows.MessageBox.Show(this, ex.Message, "Bloodborne",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _launchInProgress = false;
            PlayButton.IsEnabled = true;
        }
    }

    private void SharpnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SharpnessValueText is not null)
            SharpnessValueText.Text = e.NewValue.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var iniPath = FindIniPath();
            if (string.IsNullOrWhiteSpace(iniPath))
                throw new InvalidOperationException("BBPort.ini was not found. Place this launcher in the game folder, beside blobborne.exe.");
            _ini = new IniFile(iniPath);
            _ini.Set("output_res", Selected(ResolutionBox, "1920x1080"));
            _ini.Set("upscaler", Selected(UpscalerBox, "fsr4"));
            _ini.Set("preset", Selected(PresetBox, "1"));
            var frameGeneration = Selected(FrameGenerationBox, "off");
            var activeUpscaler = Selected(UpscalerBox, "fsr4");
            if (frameGeneration != "off" && (activeUpscaler == "off" || activeUpscaler == "taa"))
                throw new InvalidOperationException("Frame Generation requires an upscaler.");
            _ini.Set("frame_gen", frameGeneration);
            _ini.Set("frame_gen_cap", HalfRefreshBox.IsChecked == true ? "1" : "0");
            _ini.Set("model_lod", Selected(ModelLodBox, "0"));
            _ini.Set("fullscreen", FullscreenBox.IsChecked == true ? "1" : "0");
            _ini.Set("hide_cursor", HideMouseBox.IsChecked == true ? "1" : "0");
            _ini.Set("graphics_controls", PictureTuningBox.IsChecked == true ? "1" : "0");
            _ini.Set("live_resolution", Selected(LiveResolutionBox, "auto"));
            _ini.Set("sharpen", SharpenBox.IsChecked == true ? "1" : "0");
            _ini.Set("sharpness", SharpnessSlider.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            _ini.Set("object_motion", ObjectMotionBox.IsChecked == true ? "1" : "0");
            _ini.Set("effect_ssao", SsaoBox.IsChecked == true ? "1" : "0");
            _ini.Set("effect_dynamic_shadows", DynamicShadowsBox.IsChecked == true ? "1" : "0");
            _ini.Set("effect_chromatic_aberration", ChromaticAberrationBox.IsChecked == true ? "1" : "0");
            _ini.Set("effect_dof", DepthOfFieldBox.IsChecked == true ? "1" : "0");
            _ini.Set("effect_motion_blur", MotionBlurBox.IsChecked == true ? "1" : "0");
            _ini.Set("effect_game_aa", GameAaBox.IsChecked == true ? "1" : "0");
            _ini.Set("effect_ssr", ScreenSpaceReflectionsBox.IsChecked == true ? "1" : "0");
            _ini.Set("skip_intro", SkipIntroBox.IsChecked == true ? "1" : "0");
            _ini.Set("fps_mode", Selected(FrameRateBox, "60"));
            _ini.Set("present_mode", Selected(PresentationBox, "Mailbox"));
            _ini.Save();
            SaveSettings();
            StatusText.Text = "Settings Saved";
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
            System.Windows.MessageBox.Show(this, ex.Message, "Bloodborne", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
