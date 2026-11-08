using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BloodborneMinimalLauncher;

internal static class Program
{
    private static int _checks;

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        _checks++;
        Console.WriteLine("PASS " + name);
    }

    private static Dictionary<string, FrameworkElement> Controls(MainWindow window, string name) =>
        (Dictionary<string, FrameworkElement>)typeof(MainWindow).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(window)!;

    private static void Select(ComboBox box, string value) =>
        box.SelectedItem = box.Items.Cast<ComboBoxItem>().Single(item => (string)item.Tag == value);

    private static void Render(MainWindow window, string path, int tab, bool bottom = false, int width = 1550, int height = 920)
    {
        ((TabControl)window.FindName("SettingsTabs")).SelectedIndex = tab;
        var content = (FrameworkElement)window.Content;
        content.Measure(new Size(width, height));
        content.Arrange(new Rect(0, 0, width, height));
        content.UpdateLayout();
        static IEnumerable<DependencyObject> Descendants(DependencyObject node)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            {
                var child = VisualTreeHelper.GetChild(node, i);
                yield return child;
                foreach (var nested in Descendants(child)) yield return nested;
            }
        }
        foreach (var scroll in Descendants(content).OfType<ScrollViewer>().Where(v => v.ScrollableHeight > 0))
            scroll.ScrollToVerticalOffset(bottom ? scroll.ScrollableHeight : 0);
        content.UpdateLayout();
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(content);
        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        png.Save(stream);
    }

    private static void UltraQualityChecks(string root)
    {
        var folder = Path.Combine(root, "ultra-quality");
        var engine = Path.Combine(folder, "engine", "out"); Directory.CreateDirectory(engine);
        var starter = Path.Combine(engine, "bb-start.exe"); File.WriteAllText(starter, "fixture");
        File.WriteAllText(Path.Combine(folder, "cinematic-engine.json"), JsonSerializer.Serialize(new { tag = EngineRelease.Tag, upstreamCommit = EngineRelease.UpstreamCommit }));
        var iniPath = Path.Combine(folder, "lantern.ini");
        File.WriteAllText(iniPath, "upscaler=dlss\npreset=1\nrender_scale=1.75\ndlss_preset=K\nframe_gen=off\n");
        var settings = Path.Combine(folder, "preferences.ini");
        var window = new MainWindow(settings, starter);
        var presets = (ComboBox)window.FindName("PresetBox");
        var upscalers = (ComboBox)window.FindName("UpscalerBox");
        var ultra = presets.Items.Cast<ComboBoxItem>().Single(i => (string)i.Tag == "ultra_quality");
        Check((string)ultra.Content == "Ultra Quality" && ultra.IsEnabled && ultra.Visibility == Visibility.Visible && ultra.ToolTip == null, "DLSS offers plain Ultra Quality without hover text");
        Select(presets, "ultra_quality");
        Check(!Controls(window, "_graphicsControls")["render_scale"].IsEnabled, "Ultra Quality uses its fixed ratio while Custom remains separate");
        window.SaveCurrentSettings();
        var saved = new IniFile(iniPath);
        Check(saved.Get("preset") == "5" && saved.Get("cinematic_preset") == "ultra_quality", "Ultra Quality maps to the engine-supported Custom mode");
        var ratio = double.Parse(saved.Get("render_scale"), CultureInfo.InvariantCulture);
        Check(Math.Abs(1 / ratio - 0.85) < 0.00000001 && saved.Get("dlss_preset") == "K", "Ultra Quality saves 85 percent without rounding or changing DLSS K");
        var before = File.ReadAllText(iniPath); window.Close();
        window = new MainWindow(settings, starter);
        Check((string)((ComboBoxItem)((ComboBox)window.FindName("PresetBox")).SelectedItem).Tag == "ultra_quality" && File.ReadAllText(iniPath) == before, "Ultra Quality reloads without rewriting the configuration");
        window.SaveCurrentSettings();
        Check(Math.Abs(1 / double.Parse(new IniFile(iniPath).Get("render_scale"), CultureInfo.InvariantCulture) - 0.85) < 0.00000001, "Resaving Ultra Quality keeps the exact fixed scale");
        window.Close();
        saved = new IniFile(iniPath); saved.Set("render_scale", "1.18"); saved.Save();
        window = new MainWindow(settings, starter);
        presets = (ComboBox)window.FindName("PresetBox"); upscalers = (ComboBox)window.FindName("UpscalerBox");
        Check((string)((ComboBoxItem)presets.SelectedItem).Tag == "ultra_quality", "Native two-decimal saves still reload as Ultra Quality");
        var info = new ProcessStartInfo(); MainWindow.ApplyUltraQualityOverride(info, new IniFile(iniPath));
        Check(info.Environment["BB_UPSCALER"] == "dlss" && info.Environment["BB_PRESET"] == "5" && Math.Abs(1 / double.Parse(info.Environment["BB_RENDER_SCALE"]!, CultureInfo.InvariantCulture) - 0.85) < 0.00000001, "Launching Ultra Quality restores 85 percent after native rounding");
        Select(upscalers, "fsr");
        ultra = presets.Items.Cast<ComboBoxItem>().Single(i => (string)i.Tag == "ultra_quality");
        Check((string)((ComboBoxItem)presets.SelectedItem).Tag == "1" && !ultra.IsEnabled && ultra.Visibility == Visibility.Collapsed, "Switching away from DLSS safely leaves Ultra Quality");
        Select(upscalers, "dlss"); Select(presets, "5");
        ((Slider)Controls(window, "_graphicsControls")["render_scale"]).Value = 1.75;
        window.SaveCurrentSettings(); window.Close();
        window = new MainWindow(settings, starter);
        Check((string)((ComboBoxItem)((ComboBox)window.FindName("PresetBox")).SelectedItem).Tag == "5" && new IniFile(iniPath).Get("render_scale") == "1.75", "Custom remains independent and reloads its chosen ratio");
        var customLaunch = new ProcessStartInfo(); customLaunch.Environment.Remove("BB_RENDER_SCALE");
        MainWindow.ApplyUltraQualityOverride(customLaunch, new IniFile(iniPath));
        Check(!customLaunch.Environment.ContainsKey("BB_RENDER_SCALE"), "Custom launches receive no Ultra Quality override");
        Select((ComboBox)window.FindName("PresetBox"), "ultra_quality");
        Render(window, Path.Combine(root, "ultra-quality.png"), 1);
        window.Close();
    }

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            var root = Path.GetFullPath(args[0]);
            Directory.CreateDirectory(root);
            var package = Path.Combine(root, "fixture");
            var engineOut = Path.Combine(package, "engine", "out");
            Directory.CreateDirectory(engineOut);
            var starter = Path.Combine(engineOut, "bb-start.exe");
            File.WriteAllText(starter, "test fixture only");
            File.WriteAllText(Path.Combine(package, "cinematic-engine.json"), JsonSerializer.Serialize(new { tag = EngineRelease.Tag, upstreamCommit = EngineRelease.UpstreamCommit }));
            var path = Path.Combine(package, "lantern.ini");
            File.WriteAllText(path, "# Existing configuration\nupscaler=fsr4\npreset=5\nrender_scale=1.75\ndlss_preset=K\ncustom_future_key=keep-me\nframe_gen_cap=1\n");
            var settings = Path.Combine(root, "settings.ini");
            File.WriteAllText(settings, "# Isolated test preferences\n");
            var app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.InitializeComponent();
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-AR");
            var window = new MainWindow(settings, starter);
            ComboBox Box(string name) => (ComboBox)window.FindName(name);
            var graphics = Controls(window, "_graphicsControls");
            var engine = Controls(window, "_engineControls");
            Check((string)((ComboBoxItem)Box("UpscalerBox").SelectedItem).Tag == "fsr", "Legacy FSR choice migrates in the UI");
            Check(new IniFile(path).Get("upscaler") == "fsr4", "Opening does not rewrite the engine configuration");
            Check(engine.Values.Cast<CheckBox>().All(c => c.IsChecked == true), "New engine components use the upstream on defaults");
            Check(Box("UpscalerBox").Items.Cast<ComboBoxItem>().Any(i => (string)i.Tag == "xess"), "XeSS is selectable");
            Check(graphics["render_scale"].IsEnabled, "Custom render ratio is enabled for Custom");
            Select(Box("PresetBox"), "1");
            Check(!graphics["render_scale"].IsEnabled, "Custom render ratio is disabled for fixed presets");
            Select(Box("UpscalerBox"), "dlss");
            Check(((CheckBox)window.FindName("DlssJitterFadeBox")).IsEnabled, "DLSS controls follow the upscaler");
            Select(Box("UpscalerBox"), "xess");
            Check(!((CheckBox)window.FindName("DlssJitterFadeBox")).IsEnabled, "XeSS disables DLSS-only controls");
            Select(Box("UpscalerBox"), "dlss");
            Select(Box("FrameGenerationBox"), "dlss");
            Check(Box("GeneratedFramesBox").IsEnabled && Box("ReflexBox").IsEnabled, "DLSS FG dependencies remain available");
            Check(GraphicsOptions.Lighting.Where(o => o.Key.StartsWith("effect_")).All(o => ((CheckBox)graphics[o.Key]).IsChecked == false), "New lighting effects default to off");
            ((CheckBox)graphics["effect_ray_tracing"]).IsChecked = true;
            ((CheckBox)graphics["effect_contact_shadows"]).IsChecked = true;
            ((Slider)graphics["graphics_gi_strength"]).Value = 7.25;
            ((Slider)graphics["mip_bias"]).Value = -0.5;
            Select((ComboBox)graphics["graphics_shadow_resolution"], "2");
            ((CheckBox)engine["LANTERN_LIFTER"]).IsChecked = false;
            ((CheckBox)window.FindName("DlssJitterFadeBox")).IsChecked = true;
            window.SaveCurrentSettings();
            var saved = new IniFile(path);
            Check(saved.Get("effect_ray_tracing") == "1" && saved.Get("effect_contact_shadows") == "1", "Lighting switches persist to the real engine keys");
            Check(saved.Get("graphics_gi_strength") == "7.25" && saved.Get("mip_bias") == "-0.50", "Numeric values use decimal points under Spanish culture");
            Check(saved.Get("graphics_shadow_resolution") == "2", "Shadow resolution uses the engine index");
            Check(saved.Get("dlss_preset") == "K" && saved.Get("custom_future_key") == "keep-me", "DLSS K and unknown options survive saving");
            Check(File.ReadAllText(path).Contains("# Existing configuration"), "INI comments survive saving");
            Check(saved.Get("frame_gen_cap") == "0", "Removed Half Refresh Rate remains off");
            Check(new IniFile(settings).Get("LANTERN_LIFTER") == "0", "Native renderer choices persist separately");
            var info = new ProcessStartInfo();
            info.Environment["LANTERN_JOB_SLOTS"] = "0";
            ((CheckBox)engine["LANTERN_GX_DRAWS"]).IsChecked = false;
            window.ApplyNewEngineOptions(info);
            Check(info.Environment["LANTERN_LIFTER"] == "0" && info.Environment["LANTERN_JOB_SLOTS"] == "1", "Engine switches override inherited values in the child only");
            Check(info.Environment["LANTERN_GX_DRAWS"] == "0" && info.Environment["LANTERN_GX_NATIVE"] == "0", "The native draw toggle disables both rendering paths");
            var before = File.ReadAllText(path);
            Select(Box("UpscalerBox"), "off");
            var rejected = false;
            try { window.SaveCurrentSettings(); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected && File.ReadAllText(path) == before, "Invalid Frame Generation combination never writes the INI");
            Select(Box("UpscalerBox"), "dlss");
            window.ApplyLightingPreset("remastered");
            Check(File.ReadAllText(path) == before, "Applying a lighting preset does not save implicitly");
            Check(((CheckBox)graphics["effect_modern_ssr"]).IsChecked == true && ((CheckBox)window.FindName("ScreenSpaceReflectionsBox")).IsChecked == true, "Remastered enables both reflection settings");
            Check(((CheckBox)graphics["effect_ray_tracing"]).IsChecked == false && ((CheckBox)graphics["effect_gtao"]).IsChecked == false, "Built-in presets leave experimental lighting off");
            ((CheckBox)graphics["effect_ray_tracing"]).IsChecked = true;
            ((Slider)graphics["graphics_gi_strength"]).Value = 7.25;
            window.SaveCurrentSettings();
            Check(new IniFile(path).Get("graphics_gi_strength") == "7.25" && new IniFile(path).Get("dlss_preset") == "K", "Manual adjustments override the preset while preserving DLSS K");
            Render(window, Path.Combine(root, "graphics-top.png"), 1);
            Render(window, Path.Combine(root, "graphics-bottom.png"), 1, true);
            Render(window, Path.Combine(root, "effects-top.png"), 2);
            Render(window, Path.Combine(root, "effects-bottom.png"), 2, true);
            Render(window, Path.Combine(root, "effects-small.png"), 2, true, 1280, 820);
            Render(window, Path.Combine(root, "fixes.png"), 4);
            window.Close();
            var reopened = new MainWindow(settings, starter);
            Check(((CheckBox)Controls(reopened, "_graphicsControls")["effect_ray_tracing"]).IsChecked == true, "Lighting settings reload after reopening");
            Check(((CheckBox)Controls(reopened, "_engineControls")["LANTERN_LIFTER"]).IsChecked == false, "Native renderer settings reload after reopening");
            reopened.Close();
            Check(EngineRelease.IsCompatible(package), "Cinematic beta 31 package is recognized");
            File.WriteAllText(Path.Combine(package, "cinematic-engine.json"), "{\"tag\":\"v0.9.39-beta.31\",\"upstreamCommit\":\"e6b943dd969d99561545315f3d801f10e4a6cf8d\"}");
            Check(!EngineRelease.IsCompatible(package), "A beta 31 marker with the wrong commit is rejected");
            File.WriteAllText(Path.Combine(package, "cinematic-engine.json"), "{\"tag\":\"v0.9.39-beta.29\",\"upstreamCommit\":\"e6b943dd969d99561545315f3d801f10e4a6cf8d\"}");
            Check(EngineRelease.IsCompatible(package), "Cinematic beta 29 remains compatible");
            File.WriteAllText(Path.Combine(package, "cinematic-engine.json"), "{\"tag\":\"v0.9.37\",\"upstreamCommit\":\"old\"}");
            Check(!EngineRelease.IsCompatible(package), "An old engine package is rejected");
            var oldPort = Path.Combine(root, "old-port");
            var newPort = Path.Combine(root, "migration-port");
            Directory.CreateDirectory(oldPort);
            Directory.CreateDirectory(Path.Combine(newPort, "engine", "out"));
            var oldIni = Path.Combine(oldPort, "lantern.ini");
            File.WriteAllText(oldIni, "# Previous installation\nupscaler=dlss\ndlss_preset=K\n");
            File.WriteAllText(settings, "port_dir=" + oldPort + "\n");
            var migrated = new MainWindow(settings, Path.Combine(newPort, "engine", "out", "bb-start.exe"));
            Check(File.ReadAllText(Path.Combine(newPort, "lantern.ini")) == File.ReadAllText(oldIni), "First start copies the previous configuration without changing it");
            migrated.Close();
            UltraQualityChecks(root);
            SaveChecks.Run(root, app, settings, starter);
            app.Shutdown();
            Console.WriteLine($"Completed {_checks} checks; previews: {root}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
