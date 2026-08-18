using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using Microsoft.Win32;

namespace TuinRTX.Installer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new InstallerForm());
    }
}

internal sealed class InstallerForm : Form
{
    private const string PayloadMagic = "TUINRTX_PAYLOAD1";
    private readonly Panel content = new();
    private readonly PictureBox hero = new();
    private readonly TextBox destination = new();
    private readonly CheckBox desktopShortcut = new();
    private readonly ProgressBar progress = new();
    private readonly Label progressText = new();
    private readonly Button back = new();
    private readonly Button next = new();
    private TableLayoutPanel? pageStack;
    private int page;

    public InstallerForm()
    {
        Text = "TuinRTX Setup";
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1080, 660);
        MinimumSize = new Size(940, 590);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(13, 15, 19);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10f);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Margin = Padding.Empty, Padding = Padding.Empty };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        Controls.Add(root);

        hero.Dock = DockStyle.Fill;
        hero.SizeMode = PictureBoxSizeMode.Zoom;
        hero.BackColor = Color.Black;
        hero.Image = LoadRandomArtwork();
        root.Controls.Add(hero, 0, 0);
        root.SetRowSpan(hero, 2);

        content.Dock = DockStyle.Fill;
        content.AutoScroll = true;
        root.Controls.Add(content, 1, 0);

        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Color.FromArgb(22, 25, 31), Padding = new Padding(18, 16, 18, 16) };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115));
        back.Dock = DockStyle.Fill;
        back.Margin = new Padding(0, 0, 10, 0);
        back.Click += (_, _) => { if (page == 0) Close(); else if (page < 2) { page--; RenderPage(); } };
        next.Dock = DockStyle.Fill;
        next.Margin = Padding.Empty;
        next.BackColor = Color.FromArgb(218, 116, 25);
        next.ForeColor = Color.White;
        next.FlatStyle = FlatStyle.Flat;
        next.Click += async (_, _) => await NextAsync();
        AcceptButton = next;
        footer.Controls.Add(back, 1, 0);
        footer.Controls.Add(next, 2, 0);
        root.Controls.Add(footer, 1, 1);

        destination.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "TuinRTX");
        desktopShortcut.Text = "Create a desktop shortcut";
        desktopShortcut.Checked = true;
        RenderPage();
    }

    private Image? LoadRandomArtwork()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resources = assembly.GetManifestResourceNames().Where(n => n.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (resources.Length == 0) return null;
        using var stream = assembly.GetManifestResourceStream(resources[Random.Shared.Next(resources.Length)]);
        using var source = stream is null ? null : Image.FromStream(stream);
        return source is null ? null : new Bitmap(source);
    }

    private static Label PageLabel(string text, float size, Color? color = null, Padding? margin = null)
        => new() { Text = text, AutoSize = true, MaximumSize = new Size(430, 0), Font = new Font("Segoe UI Semibold", size), ForeColor = color ?? Color.White, Margin = margin ?? new Padding(0, 0, 0, 18) };

    private void RenderPage()
    {
        content.Controls.Clear();
        pageStack = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(32, 28, 32, 24), ColumnCount = 1, RowCount = 8 };
        pageStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 8; i++) pageStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.Controls.Add(pageStack);
        back.Text = page == 0 ? "Exit" : "Back";
        back.Enabled = page < 2;
        next.Enabled = true;
        if (page == 0)
        {
            pageStack.Controls.Add(PageLabel("TUINRTX", 32f, Color.FromArgb(255, 174, 62), new Padding(0, 0, 0, 6)));
            pageStack.Controls.Add(PageLabel("Classic Quake II.\nRecompiled for path tracing.", 17f));
            pageStack.Controls.Add(new Label {
                Text = "Original 1997 artwork and gameplay, rebuilt with real-time path-traced sunlight, bounce lighting, shadows, reflections, and flashlight.\n\nThe launcher will locate your legally installed Quake II game files.",
                AutoSize = true, MaximumSize = new Size(430, 0), ForeColor = Color.Gainsboro, Margin = new Padding(0, 10, 0, 0)
            });
            next.Text = "Next";
        }
        else if (page == 1)
        {
            pageStack.Controls.Add(PageLabel("Choose install folder", 22f, Color.FromArgb(255, 174, 62)));
            pageStack.Controls.Add(new Label { Text = "TuinRTX will be installed to:", AutoSize = true, ForeColor = Color.Gainsboro, Margin = new Padding(0, 6, 0, 7) });
            destination.Dock = DockStyle.Top;
            destination.Margin = new Padding(0, 0, 0, 9);
            var browse = new Button { Text = "Choose another folder…", Dock = DockStyle.Top, AutoSize = false, Height = 38, Margin = new Padding(0, 0, 0, 18) };
            browse.Click += (_, _) => BrowseDestination();
            pageStack.Controls.Add(destination);
            pageStack.Controls.Add(browse);
            desktopShortcut.AutoSize = true;
            desktopShortcut.Margin = new Padding(0, 0, 0, 20);
            pageStack.Controls.Add(desktopShortcut);
            var note = new Label { Text = "The desktop shortcut opens TuinRTX Launcher.\n\nCommercial Quake II PAK files are not included; the launcher scans for them on first start.", AutoSize = true, MaximumSize = new Size(430, 0), ForeColor = Color.Gainsboro };
            pageStack.Controls.Add(note);
            next.Text = "Install";
        }
        else
        {
            pageStack.Controls.Add(PageLabel("Installing TuinRTX", 22f, Color.FromArgb(255, 174, 62)));
            progress.Dock = DockStyle.Top;
            progress.Height = 26;
            progress.Margin = new Padding(0, 20, 0, 15);
            progressText.ForeColor = Color.Gainsboro;
            progressText.AutoSize = true;
            progressText.MaximumSize = new Size(430, 0);
            pageStack.Controls.Add(progress);
            pageStack.Controls.Add(progressText);
            next.Text = "Please wait…";
            next.Enabled = false;
        }
    }

    private async Task NextAsync()
    {
        if (page == 0) { page = 1; RenderPage(); return; }
        if (page != 1) return;
        var target = Path.GetFullPath(destination.Text.Trim());
        if (string.IsNullOrWhiteSpace(destination.Text)) return;
        page = 2;
        RenderPage();
        try
        {
            await ExtractPayloadAsync(target);
            CreateShortcuts(target);
            progress.Value = 100;
            progressText.Text = "Installation complete.\n\nInstalled in:\n" + target + "\n\nStarting TuinRTX Launcher…";
            var openFolder = new Button { Text = "Open install folder", Dock = DockStyle.Top, Height = 40, Margin = new Padding(0, 18, 0, 0) };
            openFolder.Click += (_, _) => Process.Start(new ProcessStartInfo("explorer.exe", $"\"{target}\"") { UseShellExecute = true });
            pageStack?.Controls.Add(openFolder);
            next.Text = "Launch again";
            next.Enabled = true;
            next.Click += (_, _) => LaunchInstalled(target);
            BeginInvoke(new Action(() => LaunchInstalled(target)));
        }
        catch (Exception ex)
        {
            progressText.Text = "Installation failed: " + ex.Message;
            next.Text = "Close";
            next.Enabled = true;
            next.Click += (_, _) => Close();
        }
    }

    private void LaunchInstalled(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo(Path.Combine(target, "TuinRTX Launcher.exe")) { WorkingDirectory = target, UseShellExecute = true });
            Close();
        }
        catch (Exception ex)
        {
            progressText.Text = "Installation complete.\n\nInstalled in:\n" + target + "\n\nThe launcher could not start automatically. Click Launch again to retry.";
            MessageBox.Show(this, ex.Message, "Could not start TuinRTX Launcher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void BrowseDestination()
    {
        using var dialog = new FolderBrowserDialog { Description = "Choose the exact folder where TuinRTX will be installed", UseDescriptionForTitle = true, SelectedPath = destination.Text };
        if (dialog.ShowDialog(this) == DialogResult.OK) destination.Text = dialog.SelectedPath;
    }

    private async Task ExtractPayloadAsync(string target)
    {
        Directory.CreateDirectory(target);
        var self = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot locate setup executable.");
        await using var file = new FileStream(self, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (file.Length < 24) throw new InvalidDataException("Setup payload is missing.");
        file.Seek(-24, SeekOrigin.End);
        var offsetBytes = new byte[8];
        await file.ReadExactlyAsync(offsetBytes);
        var magicBytes = new byte[16];
        await file.ReadExactlyAsync(magicBytes);
        if (Encoding.ASCII.GetString(magicBytes) != PayloadMagic) throw new InvalidDataException("Setup payload signature is invalid.");
        var offset = BitConverter.ToInt64(offsetBytes);
        var payloadLength = file.Length - 24 - offset;
        using var payload = new SliceStream(file, offset, payloadLength);
        using var archive = new ZipArchive(payload, ZipArchiveMode.Read, leaveOpen: true);
        var entries = archive.Entries.Where(e => !string.IsNullOrEmpty(e.Name)).ToArray();
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            var output = Path.GetFullPath(Path.Combine(target, entry.FullName));
            if (!output.StartsWith(target + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Unsafe payload path.");
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            await using var input = entry.Open();
            await using var destinationStream = new FileStream(output, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, true);
            await input.CopyToAsync(destinationStream);
            progress.Value = Math.Min(99, (i + 1) * 100 / Math.Max(1, entries.Length));
            progressText.Text = $"Extracting {entry.FullName}";
            Application.DoEvents();
        }
    }

    private void CreateShortcuts(string target)
    {
        var launcher = Path.Combine(target, "TuinRTX Launcher.exe");
        if (desktopShortcut.Checked) CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "TuinRTX.lnk"), launcher, target);
        var menu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "TuinRTX");
        Directory.CreateDirectory(menu);
        CreateShortcut(Path.Combine(menu, "TuinRTX.lnk"), launcher, target);
        CreateShortcut(Path.Combine(menu, "Uninstall TuinRTX.lnk"), launcher, target, "--uninstall");

        using var uninstallKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\TuinRTX");
        uninstallKey?.SetValue("DisplayName", "TuinRTX");
        uninstallKey?.SetValue("DisplayVersion", "1.0.5");
        uninstallKey?.SetValue("Publisher", "TuinRTX Project");
        uninstallKey?.SetValue("InstallLocation", target);
        uninstallKey?.SetValue("DisplayIcon", launcher);
        uninstallKey?.SetValue("UninstallString", $"\"{launcher}\" --uninstall");
        uninstallKey?.SetValue("NoModify", 1, RegistryValueKind.DWord);
        uninstallKey?.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }

    private static void CreateShortcut(string shortcutPath, string target, string workingDirectory, string arguments = "")
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType is null) return;
        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = target;
        shortcut.WorkingDirectory = workingDirectory;
        shortcut.Arguments = arguments;
        shortcut.Description = "Launch TuinRTX";
        shortcut.Save();
    }
}

internal sealed class SliceStream(Stream source, long offset, long length) : Stream
{
    private long position;
    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => length;
    public override long Position { get => position; set => Seek(value, SeekOrigin.Begin); }
    public override int Read(byte[] buffer, int bufferOffset, int count)
    {
        source.Position = offset + position;
        var read = source.Read(buffer, bufferOffset, (int)Math.Min(count, length - position));
        position += read;
        return read;
    }
    public override int Read(Span<byte> buffer)
    {
        source.Position = offset + position;
        var read = source.Read(buffer[..(int)Math.Min(buffer.Length, length - position)]);
        position += read;
        return read;
    }
    public override long Seek(long value, SeekOrigin origin)
    {
        position = origin switch { SeekOrigin.Begin => value, SeekOrigin.Current => position + value, SeekOrigin.End => length + value, _ => position };
        if (position < 0 || position > length) throw new IOException("Seek outside payload.");
        return position;
    }
    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
