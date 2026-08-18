using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TuinRTX.Launcher;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Length >= 2 && args[0].Equals("--uninstall-worker", StringComparison.OrdinalIgnoreCase))
        {
            UninstallManager.RunWorker(args[1]);
            return;
        }
        if (args.Any(a => a.Equals("--uninstall", StringComparison.OrdinalIgnoreCase)))
        {
            UninstallManager.ConfirmAndStart();
            return;
        }
        Application.Run(new LauncherForm());
    }
}

internal sealed class LauncherForm : Form
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateHardLink(string newFileName, string existingFileName, IntPtr securityAttributes);

    private readonly string installDir = AppContext.BaseDirectory;
    private readonly string baseDir;
    private readonly Label status = new();
    private readonly Button play = new();
    private readonly TextBox sourcePath = new();

    public LauncherForm()
    {
        baseDir = Path.Combine(installDir, "baseq2");
        Text = "TuinRTX Launcher";
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1180, 720);
        MinimumSize = new Size(980, 640);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(12, 14, 18);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10f);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, Padding = Padding.Empty };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var hero = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black };
        root.Controls.Add(hero, 0, 0);
        LoadRandomHero(hero);

        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(32, 26, 32, 24), ColumnCount = 1, RowCount = 12, AutoScroll = true };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 12; i++) panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(panel, 1, 0);

        var title = new Label { Text = "TUINRTX", Font = new Font("Segoe UI Semibold", 30f), AutoSize = true, ForeColor = Color.FromArgb(255, 174, 62), Margin = new Padding(0, 0, 0, 4) };
        var subtitle = new Label { Text = "Classic Quake II. Recompiled for path tracing.", AutoSize = true, MaximumSize = new Size(430, 0), ForeColor = Color.Gainsboro, Margin = new Padding(0, 0, 0, 24) };
        status.AutoSize = true;
        status.MaximumSize = new Size(430, 0);
        status.ForeColor = Color.Gainsboro;
        status.Margin = new Padding(0, 0, 0, 18);

        sourcePath.PlaceholderText = "Quake II installation folder";
        sourcePath.Dock = DockStyle.Fill;
        sourcePath.Margin = new Padding(0, 3, 8, 3);
        var browse = new Button { Text = "Browse…", Dock = DockStyle.Fill, AutoSize = true, MinimumSize = new Size(92, 34), Margin = new Padding(0, 0, 0, 0) };
        browse.Click += (_, _) => BrowseForGame();
        var sourceRow = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = false, Height = 38, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 0, 0, 14) };
        sourceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
        sourceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        sourceRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        sourceRow.Controls.Add(sourcePath, 0, 0);
        sourceRow.Controls.Add(browse, 1, 0);

        var scan = new Button { Text = "SCAN FOR GAME FILES", Dock = DockStyle.Top, Height = 46, BackColor = Color.FromArgb(48, 54, 65), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Margin = new Padding(0, 0, 0, 14) };
        scan.Click += async (_, _) => await ScanAndInstallAsync();

        play.Text = "PLAY TUINRTX";
        play.Dock = DockStyle.Top;
        play.Height = 56;
        play.BackColor = Color.FromArgb(218, 116, 25);
        play.ForeColor = Color.White;
        play.FlatStyle = FlatStyle.Flat;
        play.Font = new Font("Segoe UI Semibold", 13f);
        play.Margin = new Padding(0, 0, 0, 26);
        play.Click += (_, _) => LaunchGame();
        var loadingNote = new Label { Text = "First-start loading time can vary while RTX resources and shaders initialize.", AutoSize = true, MaximumSize = new Size(430, 0), ForeColor = Color.DarkGray, Margin = new Padding(0, -14, 0, 22) };

        var help = new Label {
            Text = "QUICK HELP\n\n/   Change time of day / scenery\nF   Ray-traced flashlight\n~   Console\nF5  Video settings\nF12 Screenshot",
            AutoSize = true, MaximumSize = new Size(430, 0), ForeColor = Color.Gainsboro, Font = new Font("Segoe UI", 10.5f), Margin = new Padding(0, 0, 0, 20)
        };
        var installedAt = new Label { Text = "Installed in:\n" + installDir.TrimEnd(Path.DirectorySeparatorChar), AutoSize = true, MaximumSize = new Size(430, 0), ForeColor = Color.DarkGray, Margin = new Padding(0, 0, 0, 14) };
        var openFolder = new LinkLabel { Text = "Open install folder", AutoSize = true, LinkColor = Color.FromArgb(255, 174, 62), Margin = new Padding(0, 0, 0, 8) };
        openFolder.Click += (_, _) => Process.Start(new ProcessStartInfo("explorer.exe", $"\"{installDir}\"") { UseShellExecute = true });
        var readme = new LinkLabel { Text = "Read README and credits", AutoSize = true, LinkColor = Color.FromArgb(255, 174, 62) };
        readme.Click += (_, _) => Process.Start(new ProcessStartInfo(Path.Combine(installDir, "README.txt")) { UseShellExecute = true });
        var uninstall = new LinkLabel { Text = "Uninstall TuinRTX", AutoSize = true, LinkColor = Color.Silver, Margin = new Padding(0, 10, 0, 0) };
        uninstall.Click += (_, _) => UninstallManager.ConfirmAndStart(this);

        panel.Controls.Add(title, 0, 0);
        panel.Controls.Add(subtitle, 0, 1);
        panel.Controls.Add(status, 0, 2);
        panel.Controls.Add(sourceRow, 0, 3);
        panel.Controls.Add(scan, 0, 4);
        panel.Controls.Add(play, 0, 5);
        panel.Controls.Add(loadingNote, 0, 6);
        panel.Controls.Add(help, 0, 7);
        panel.Controls.Add(installedAt, 0, 8);
        panel.Controls.Add(openFolder, 0, 9);
        panel.Controls.Add(readme, 0, 10);
        panel.Controls.Add(uninstall, 0, 11);
        Shown += async (_, _) => await InitialScanAsync();
    }

    private void LoadRandomHero(PictureBox box)
    {
        var artDir = Path.Combine(installDir, "art");
        var images = Directory.Exists(artDir) ? Directory.GetFiles(artDir, "*.jpg") : [];
        if (images.Length > 0)
        {
            using var source = Image.FromFile(images[Random.Shared.Next(images.Length)]);
            box.Image = new Bitmap(source);
        }
    }

    private bool HasGameData() => File.Exists(Path.Combine(baseDir, "pak0.pak"));

    private async Task InitialScanAsync()
    {
        if (HasGameData()) { SetReady(); return; }
        status.Text = "Looking for your legally installed Quake II data…";
        status.ForeColor = Color.Gold;
        await ScanAndInstallAsync(false);
    }

    private void BrowseForGame()
    {
        using var dialog = new FolderBrowserDialog { Description = "Select your Quake II installation folder", UseDescriptionForTitle = true };
        if (dialog.ShowDialog(this) == DialogResult.OK) sourcePath.Text = dialog.SelectedPath;
    }

    private async Task ScanAndInstallAsync(bool showFailure = true)
    {
        play.Enabled = false;
        status.Text = "Scanning Steam libraries and common folders…";
        status.ForeColor = Color.Gold;
        var explicitPath = sourcePath.Text.Trim();
        var source = await Task.Run(() => FindGameData(explicitPath));
        if (source is null)
        {
            status.Text = "✕ QUAKE II NOT FOUND\nSelect the Quake II folder that contains baseq2\\pak0.pak.\nTypical Steam location: C:\\Program Files (x86)\\Steam\\steamapps\\common\\Quake 2";
            status.ForeColor = Color.FromArgb(255, 110, 110);
            if (showFailure) MessageBox.Show(this, "Select the Quake II installation folder that contains baseq2\\pak0.pak.\n\nTypical Steam location:\nC:\\Program Files (x86)\\Steam\\steamapps\\common\\Quake 2", "Quake II not found", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            status.Text = "Preparing original Quake II game data…";
            status.ForeColor = Color.Gold;
            await Task.Run(() => InstallPaks(source));
            SetReady();
        }
        catch (Exception ex)
        {
            status.Text = "Could not prepare the game data.";
            status.ForeColor = Color.FromArgb(255, 110, 110);
            MessageBox.Show(this, ex.Message, "TuinRTX", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetReady()
    {
        status.Text = "✓ QUAKE II FOUND\nRTX media and original game data are ready.";
        status.ForeColor = Color.FromArgb(100, 225, 130);
        play.Enabled = true;
    }

    private string? FindGameData(string explicitPath)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(explicitPath)) candidates.Add(explicitPath);
        candidates.AddRange([
            @"C:\Program Files (x86)\Steam\steamapps\common\Quake 2",
            @"C:\Program Files\Steam\steamapps\common\Quake 2",
            @"C:\GOG Games\Quake II"
        ]);

        foreach (var steamRoot in SteamRoots())
            candidates.Add(Path.Combine(steamRoot, "steamapps", "common", "Quake 2"));

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (var path in new[] { candidate, Path.Combine(candidate, "baseq2") })
                if (File.Exists(Path.Combine(path, "pak0.pak"))) return path;
        }
        return null;
    }

    private static IEnumerable<string> SteamRoots()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var steam = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam")?.GetValue("SteamPath") as string;
        if (!string.IsNullOrWhiteSpace(steam)) roots.Add(steam.Replace('/', '\\'));
        foreach (var root in roots.ToArray())
        {
            var vdf = Path.Combine(root, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdf)) continue;
            foreach (var line in File.ReadLines(vdf))
            {
                var match = System.Text.RegularExpressions.Regex.Match(line, "\\\"path\\\"\\s+\\\"(.+?)\\\"");
                if (match.Success) roots.Add(match.Groups[1].Value.Replace("\\\\", "\\"));
            }
        }
        return roots;
    }

    private void InstallPaks(string source)
    {
        Directory.CreateDirectory(baseDir);
        foreach (var name in new[] { "pak0.pak", "pak1.pak", "pak2.pak" })
        {
            var from = Path.Combine(source, name);
            var to = Path.Combine(baseDir, name);
            if (!File.Exists(from) || File.Exists(to)) continue;
            try
            {
                if (!CreateHardLink(to, from, IntPtr.Zero))
                    throw new IOException("Hard-link creation failed.");
            }
            catch { File.Copy(from, to, false); }
        }
    }

    private void LaunchGame()
    {
        if (!HasGameData()) { _ = ScanAndInstallAsync(); return; }
        var exe = Path.Combine(installDir, "q2rtx.exe");
        if (!File.Exists(exe)) { MessageBox.Show(this, "q2rtx.exe is missing. Reinstall TuinRTX."); return; }
        Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = installDir, UseShellExecute = true });
        WindowState = FormWindowState.Minimized;
    }
}

internal static class UninstallManager
{
    private const int MoveFileDelayUntilReboot = 0x4;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool MoveFileEx(string existingFileName, string? newFileName, int flags);

    public static void ConfirmAndStart(IWin32Window? owner = null)
    {
        var answer = MessageBox.Show(owner, "Remove TuinRTX, its launcher, and its shortcuts?\n\nYour original Quake II installation will not be changed.", "Uninstall TuinRTX", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;

        var target = Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar);
        if (!File.Exists(Path.Combine(target, "q2rtx.exe")) || !File.Exists(Path.Combine(target, "README.txt")))
        {
            MessageBox.Show(owner, "The TuinRTX installation folder could not be validated.", "Uninstall TuinRTX", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var self = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot locate TuinRTX Launcher.");
        var worker = Path.Combine(Path.GetTempPath(), "TuinRTX-Uninstall-" + Guid.NewGuid().ToString("N") + ".exe");
        File.Copy(self, worker, false);
        var encodedTarget = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(target));
        var start = new ProcessStartInfo(worker) { UseShellExecute = false };
        start.WorkingDirectory = Path.GetTempPath();
        start.ArgumentList.Add("--uninstall-worker");
        start.ArgumentList.Add(encodedTarget);
        Process.Start(start);
        Application.Exit();
    }

    public static void RunWorker(string encodedTarget)
    {
        try
        {
            Thread.Sleep(1800);
            var target = Path.GetFullPath(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedTarget))).TrimEnd(Path.DirectorySeparatorChar);
            var root = Path.GetPathRoot(target);
            if (string.IsNullOrWhiteSpace(root) || target.Equals(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe uninstall target.");
            if (!File.Exists(Path.Combine(target, "q2rtx.exe")) || !File.Exists(Path.Combine(target, "README.txt"))) throw new InvalidOperationException("The TuinRTX installation folder could not be validated.");

            TryRemoveRegistration();

            Exception? lastError = null;
            for (var attempt = 0; attempt < 12 && Directory.Exists(target); attempt++)
            {
                try
                {
                    Directory.Delete(target, true);
                    lastError = null;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    Thread.Sleep(500);
                }
            }

            if (!Directory.Exists(target))
            {
                MessageBox.Show("TuinRTX was removed. Your original Quake II files were left untouched.", "TuinRTX", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                ScheduleTreeForReboot(target);
                MessageBox.Show("Some TuinRTX files are still in use. They are scheduled for removal after Windows restarts.\n\n" + lastError?.Message, "Finish uninstalling TuinRTX", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("TuinRTX could not be completely removed:\n\n" + ex.Message, "Uninstall TuinRTX", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (Environment.ProcessPath is string self) MoveFileEx(self, null, MoveFileDelayUntilReboot);
        }
    }

    private static void TryRemoveRegistration()
    {
        try
        {
            var desktopShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "TuinRTX.lnk");
            if (File.Exists(desktopShortcut)) File.Delete(desktopShortcut);
            var menu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "TuinRTX");
            if (Directory.Exists(menu)) Directory.Delete(menu, true);
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\TuinRTX", false);
        }
        catch { /* Registration cleanup must not turn a successful file removal into an error. */ }
    }

    private static void ScheduleTreeForReboot(string target)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(target, "*", SearchOption.AllDirectories))
                MoveFileEx(file, null, MoveFileDelayUntilReboot);
            foreach (var directory in Directory.EnumerateDirectories(target, "*", SearchOption.AllDirectories).OrderByDescending(p => p.Length))
                MoveFileEx(directory, null, MoveFileDelayUntilReboot);
            MoveFileEx(target, null, MoveFileDelayUntilReboot);
        }
        catch { }
    }
}
