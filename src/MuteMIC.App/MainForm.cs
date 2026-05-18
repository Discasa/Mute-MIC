using System.Diagnostics;
using System.IO;
using System.Media;
using Microsoft.Win32;

namespace MuteMIC.App;

public sealed class MainForm : Form
{
    private const int ToggleHotkeyId = 1;
    private const int MuteHotkeyId = 2;
    private const int UnmuteHotkeyId = 3;

    private readonly AudioInputMuteService _audioService = new();
    private readonly SettingsStore _settingsStore = new();
    private readonly AppSettings _settings;
    private readonly NotifyIcon _trayIcon;
    private readonly HotkeyTextBox _toggleHotkeyBox = new();
    private readonly HotkeyTextBox _muteHotkeyBox = new();
    private readonly HotkeyTextBox _unmuteHotkeyBox = new();
    private readonly Label _toggleLabel = new();
    private readonly Label _muteLabel = new();
    private readonly Label _unmuteLabel = new();
    private readonly ModernButton _toggleReset = new();
    private readonly ModernButton _muteReset = new();
    private readonly ModernButton _unmuteReset = new();
    private readonly System.Windows.Forms.Timer _refreshTimer = new();
    private readonly AppUpdateService _updateService = new();
    private readonly SoundPlayer _onPlayer;
    private readonly SoundPlayer _offPlayer;
    private readonly MemoryStream _onSoundStream;
    private readonly MemoryStream _offSoundStream;

    private GlobalHotkeys? _globalHotkeys;
    private Icon? _onIcon;
    private Icon? _offIcon;
    private TrayMenuWindow? _trayMenuWindow;
    private bool _exitRequested;
    private bool? _lastAllMuted;

    public MainForm()
    {
        _settings = _settingsStore.Load();
        _onSoundStream = new MemoryStream(EmbeddedResources.LoadBytes("Audio.on.wav"));
        _offSoundStream = new MemoryStream(EmbeddedResources.LoadBytes("Audio.off.wav"));
        _onPlayer = new SoundPlayer(_onSoundStream);
        _offPlayer = new SoundPlayer(_offSoundStream);
        _onPlayer.Load();
        _offPlayer.Load();

        Text = "Mute MIC";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(430, 286);
        ClientSize = new Size(430, 286);
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedSingle;

        _trayIcon = new NotifyIcon
        {
            Visible = true
        };
        _trayIcon.MouseClick += TrayIcon_MouseClick;

        BuildUi();
        LoadSettingsIntoControls();
        ApplyLanguage();
        ApplyTheme();
        RegisterControlEvents();

        _refreshTimer.Interval = 1000;
        _refreshTimer.Tick += (_, _) => RefreshAudioStatus(false);
        _updateService.UpdateInstallerReady += UpdateService_UpdateInstallerReady;

        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _globalHotkeys = new GlobalHotkeys();
        RegisterHotkeys();
        RefreshAudioStatus(false);
        _refreshTimer.Start();
        _updateService.Start();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyTheme();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Hide();
        ShowInTaskbar = false;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_exitRequested && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            ShowInTaskbar = false;
            return;
        }

        SaveSettings();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
            _refreshTimer.Dispose();
            _updateService.UpdateInstallerReady -= UpdateService_UpdateInstallerReady;
            _updateService.Dispose();
            _globalHotkeys?.Dispose();
            _trayIcon.Dispose();
            _trayMenuWindow?.Close();
            _onPlayer.Dispose();
            _offPlayer.Dispose();
            _onSoundStream.Dispose();
            _offSoundStream.Dispose();
            _onIcon?.Dispose();
            _offIcon?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void BuildUi()
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 22, 20, 22),
            ColumnCount = 2,
            RowCount = 7
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        AddHotkeyRow(layout, _toggleLabel, _toggleHotkeyBox, _toggleReset, 0);
        AddHotkeyRow(layout, _muteLabel, _muteHotkeyBox, _muteReset, 2);
        AddHotkeyRow(layout, _unmuteLabel, _unmuteHotkeyBox, _unmuteReset, 4);

        Controls.Add(layout);
    }

    private static void AddHotkeyRow(
        TableLayoutPanel layout,
        Label label,
        HotkeyTextBox textBox,
        ModernButton resetButton,
        int labelRow)
    {
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.AutoSize = false;
        label.Margin = labelRow == 0 ? Padding.Empty : new Padding(0, 10, 0, 0);
        textBox.Dock = DockStyle.None;
        textBox.Margin = Padding.Empty;
        resetButton.Dock = DockStyle.Fill;
        resetButton.Margin = new Padding(12, 4, 0, 4);

        ModernFieldPanel textBoxHost = new()
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4)
        };
        textBoxHost.Host(textBox);

        layout.Controls.Add(label, 0, labelRow);
        layout.SetColumnSpan(label, 2);
        layout.Controls.Add(textBoxHost, 0, labelRow + 1);
        layout.Controls.Add(resetButton, 1, labelRow + 1);
    }

    private void RegisterControlEvents()
    {
        _toggleReset.Click += (_, _) => _toggleHotkeyBox.Hotkey = HotkeyDefinition.None;
        _muteReset.Click += (_, _) => _muteHotkeyBox.Hotkey = HotkeyDefinition.None;
        _unmuteReset.Click += (_, _) => _unmuteHotkeyBox.Hotkey = HotkeyDefinition.None;

        _toggleHotkeyBox.HotkeyChanged += (_, _) => SaveAndRegisterHotkeys();
        _muteHotkeyBox.HotkeyChanged += (_, _) => SaveAndRegisterHotkeys();
        _unmuteHotkeyBox.HotkeyChanged += (_, _) => SaveAndRegisterHotkeys();
    }

    private void LoadSettingsIntoControls()
    {
        _toggleHotkeyBox.Language = _settings.Language;
        _muteHotkeyBox.Language = _settings.Language;
        _unmuteHotkeyBox.Language = _settings.Language;
        _toggleHotkeyBox.Hotkey = _settings.ToggleHotkey;
        _muteHotkeyBox.Hotkey = _settings.MuteHotkey;
        _unmuteHotkeyBox.Hotkey = _settings.UnmuteHotkey;
    }

    private void SaveAndRegisterHotkeys()
    {
        SaveSettings();
        if (IsHandleCreated)
        {
            RegisterHotkeys();
        }
    }

    private void SaveSettings()
    {
        _settings.ToggleHotkey = _toggleHotkeyBox.Hotkey;
        _settings.MuteHotkey = _muteHotkeyBox.Hotkey;
        _settings.UnmuteHotkey = _unmuteHotkeyBox.Hotkey;
        _settingsStore.Save(_settings);
    }

    private void RegisterHotkeys()
    {
        if (_globalHotkeys is null)
        {
            return;
        }

        try
        {
            _globalHotkeys.Register(ToggleHotkeyId, _toggleHotkeyBox.Hotkey, () => ExecuteAudioOperation(() => _audioService.ToggleAll()));
            _globalHotkeys.Register(MuteHotkeyId, _muteHotkeyBox.Hotkey, () => ExecuteAudioOperation(() => _audioService.SetAllMuted(true)));
            _globalHotkeys.Register(UnmuteHotkeyId, _unmuteHotkeyBox.Hotkey, () => ExecuteAudioOperation(() => _audioService.SetAllMuted(false)));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ExecuteAudioOperation(Func<IReadOnlyList<string>> operation)
    {
        IReadOnlyList<string> errors = operation();
        RefreshAudioStatus(true);
        if (errors.Count > 0)
        {
            string message = Strings.Get(_settings.Language, "PartialFailure") + Environment.NewLine + string.Join(Environment.NewLine, errors);
            MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RefreshAudioStatus(bool playSound)
    {
        IReadOnlyList<AudioInputDeviceState> devices = _audioService.GetActiveInputDevices();
        bool? allMuted = devices.Count == 0 ? null : devices.All(device => device.IsMuted);
        bool changed = playSound && _lastAllMuted.HasValue && allMuted.HasValue && _lastAllMuted.Value != allMuted.Value;
        _lastAllMuted = allMuted;

        bool mutedIcon = allMuted != false;
        _trayIcon.Icon = mutedIcon ? _offIcon : _onIcon;
        if (_trayMenuWindow is null)
        {
            _trayIcon.Text = BuildTooltip(devices, allMuted);
        }

        if (changed)
        {
            if (allMuted == true)
            {
                _offSoundStream.Position = 0;
                _offPlayer.Play();
            }
            else
            {
                _onSoundStream.Position = 0;
                _onPlayer.Play();
            }
        }
    }

    private string BuildTooltip(IReadOnlyList<AudioInputDeviceState> devices, bool? allMuted)
    {
        if (devices.Count == 0)
        {
            return Strings.Get(_settings.Language, "NoInputs");
        }

        string action = allMuted == true
            ? Strings.Get(_settings.Language, "ClickToUnmute")
            : Strings.Get(_settings.Language, "ClickToMute");
        string hotkey = _toggleHotkeyBox.Hotkey.IsEmpty
            ? string.Empty
            : $" ({_toggleHotkeyBox.Hotkey.ToDisplayString(_settings.Language)})";
        string tooltip = $"{action}{hotkey}";
        return tooltip[..Math.Min(tooltip.Length, 63)];
    }

    private void ShowSettingsWindow()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void TrayIcon_MouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ExecuteAudioOperation(() => _audioService.ToggleAll());
        }
        else if (e.Button == MouseButtons.Right)
        {
            ShowTrayMenu();
        }
    }

    private void ShowTrayMenu()
    {
        _trayMenuWindow?.Close();

        string language = _settings.Language;
        TrayMenuText menuText = new(
            Strings.Get(language, "Hotkeys"),
            Strings.Get(language, "ColorScheme"),
            Strings.Get(language, "Monochrome"),
            Strings.Get(language, "Colorful"),
            Strings.Get(language, "Language"),
            Strings.Get(language, "English"),
            Strings.Get(language, "Portuguese"),
            Strings.Get(language, "Exit"));

        _trayMenuWindow = new TrayMenuWindow(
            ThemeService.IsLightTheme(),
            menuText,
            _settings.IconColorScheme,
            language,
            Cursor.Position,
            ShowSettingsWindow,
            SetIconColorScheme,
            SetLanguage,
            () =>
            {
                _exitRequested = true;
                Close();
            });
        string previousTooltip = _trayIcon.Text;
        _trayIcon.Text = " ";
        _trayMenuWindow.FormClosed += (_, _) =>
        {
            _trayMenuWindow = null;
            _trayIcon.Text = previousTooltip;
            RefreshAudioStatus(false);
        };
        _trayMenuWindow.Show();
    }

    private void SetLanguage(string language)
    {
        _settings.Language = language;
        SaveSettings();
        ApplyLanguage();
        RefreshAudioStatus(false);
    }

    private void SetIconColorScheme(IconColorScheme colorScheme)
    {
        _settings.IconColorScheme = colorScheme;
        SaveSettings();
        ApplyTheme();
        RefreshAudioStatus(false);
    }

    private void ApplyLanguage()
    {
        string language = _settings.Language;
        Text = Strings.Get(language, "AppName");

        _toggleLabel.Text = Strings.Get(language, "ToggleLabel");
        _muteLabel.Text = Strings.Get(language, "MuteLabel");
        _unmuteLabel.Text = Strings.Get(language, "UnmuteLabel");
        _toggleReset.Text = Strings.Get(language, "Reset");
        _muteReset.Text = Strings.Get(language, "Reset");
        _unmuteReset.Text = Strings.Get(language, "Reset");

        foreach (HotkeyTextBox box in new[] { _toggleHotkeyBox, _muteHotkeyBox, _unmuteHotkeyBox })
        {
            box.Language = language;
            box.RefreshText();
        }
    }

    private void ApplyTheme()
    {
        bool lightTheme = ThemeService.IsLightTheme();
        if (IsHandleCreated)
        {
            ThemeService.ApplyWindowFrame(this, lightTheme);
        }

        Color backColor = lightTheme ? Color.White : Color.FromArgb(32, 32, 32);
        Color foreColor = lightTheme ? Color.FromArgb(24, 24, 24) : Color.White;
        Color inputBackColor = lightTheme ? Color.White : Color.FromArgb(48, 48, 48);
        Color borderColor = lightTheme ? Color.FromArgb(240, 240, 240) : Color.FromArgb(64, 64, 64);

        BackColor = backColor;
        ForeColor = foreColor;

        foreach (Control control in Controls.Cast<Control>().SelectMany(FlattenControls))
        {
            control.ForeColor = foreColor;
            if (control is ModernFieldPanel fieldPanel)
            {
                fieldPanel.BackColor = backColor;
                fieldPanel.FillColor = inputBackColor;
                fieldPanel.BorderColor = borderColor;
                fieldPanel.FocusBorderColor = Color.FromArgb(0, 120, 212);
                fieldPanel.Invalidate();
            }
            else if (control is TextBox textBox)
            {
                textBox.BackColor = inputBackColor;
                textBox.BorderStyle = BorderStyle.None;
            }
            else if (control is ModernButton modernButton)
            {
                modernButton.BackColor = lightTheme ? Color.FromArgb(252, 252, 252) : Color.FromArgb(37, 37, 37);
                modernButton.BorderColor = borderColor;
                modernButton.HoverBackColor = lightTheme ? Color.FromArgb(242, 242, 242) : Color.FromArgb(50, 50, 50);
                modernButton.PressedBackColor = lightTheme ? Color.FromArgb(235, 235, 235) : Color.FromArgb(58, 58, 58);
                modernButton.ForeColor = foreColor;
                modernButton.Invalidate();
            }
            else
            {
                control.BackColor = backColor;
            }

            if (control is Button button && control is not ModernButton)
            {
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = borderColor;
            }
        }

        _onIcon?.Dispose();
        _offIcon?.Dispose();
        string suffix = _settings.IconColorScheme == IconColorScheme.Colorful
            ? "color"
            : lightTheme ? "dark" : "white";
        _onIcon = EmbeddedResources.LoadIcon($"on-{suffix}.ico");
        _offIcon = EmbeddedResources.LoadIcon($"off-{suffix}.ico");
        Icon = EmbeddedResources.LoadIcon($"on-{suffix}.ico");
        RefreshAudioStatus(false);
    }

    private static IEnumerable<Control> FlattenControls(Control root)
    {
        yield return root;
        foreach (Control child in root.Controls)
        {
            foreach (Control nested in FlattenControls(child))
            {
                yield return nested;
            }
        }
    }

    private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.VisualStyle)
        {
            ApplyTheme();
        }
    }

    private void UpdateService_UpdateInstallerReady(object? sender, UpdateInstallerReadyEventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        BeginInvoke(() =>
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.InstallerPath)
                {
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetTempPath(),
                    ArgumentList = { "--silent", "--from-update" }
                });
                _exitRequested = true;
                Close();
            }
            catch
            {
                // Update errors are already logged by the updater service.
            }
        });
    }
}
