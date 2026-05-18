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
    private readonly ContextMenuStrip _trayMenu = new();
    private readonly ToolStripMenuItem _toggleAllItem = new();
    private readonly ToolStripMenuItem _muteAllItem = new();
    private readonly ToolStripMenuItem _unmuteAllItem = new();
    private readonly ToolStripMenuItem _hotkeysItem = new();
    private readonly ToolStripMenuItem _languageItem = new();
    private readonly ToolStripMenuItem _englishItem = new();
    private readonly ToolStripMenuItem _portugueseItem = new();
    private readonly ToolStripMenuItem _exitItem = new();
    private readonly HotkeyTextBox _toggleHotkeyBox = new();
    private readonly HotkeyTextBox _muteHotkeyBox = new();
    private readonly HotkeyTextBox _unmuteHotkeyBox = new();
    private readonly Label _toggleLabel = new();
    private readonly Label _muteLabel = new();
    private readonly Label _unmuteLabel = new();
    private readonly Button _toggleReset = new();
    private readonly Button _muteReset = new();
    private readonly Button _unmuteReset = new();
    private readonly System.Windows.Forms.Timer _refreshTimer = new();
    private readonly SoundPlayer _onPlayer;
    private readonly SoundPlayer _offPlayer;
    private readonly MemoryStream _onSoundStream;
    private readonly MemoryStream _offSoundStream;

    private GlobalHotkeys? _globalHotkeys;
    private Icon? _onIcon;
    private Icon? _offIcon;
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
        MinimumSize = new Size(390, 250);
        ClientSize = new Size(430, 255);
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedSingle;

        _trayIcon = new NotifyIcon
        {
            ContextMenuStrip = _trayMenu,
            Visible = true
        };
        _trayIcon.MouseClick += TrayIcon_MouseClick;

        BuildMenu();
        BuildUi();
        LoadSettingsIntoControls();
        ApplyLanguage();
        ApplyTheme();
        RegisterControlEvents();

        _refreshTimer.Interval = 1000;
        _refreshTimer.Tick += (_, _) => RefreshAudioStatus(false);

        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _globalHotkeys = new GlobalHotkeys(Handle);
        RegisterHotkeys();
        RefreshAudioStatus(false);
        _refreshTimer.Start();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Hide();
        ShowInTaskbar = false;
    }

    protected override void WndProc(ref Message m)
    {
        if (_globalHotkeys?.HandleMessage(ref m) == true)
        {
            return;
        }

        base.WndProc(ref m);
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
            _globalHotkeys?.Dispose();
            _trayIcon.Dispose();
            _trayMenu.Dispose();
            _onPlayer.Dispose();
            _offPlayer.Dispose();
            _onSoundStream.Dispose();
            _offSoundStream.Dispose();
            _onIcon?.Dispose();
            _offIcon?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void BuildMenu()
    {
        _toggleAllItem.Click += (_, _) => ExecuteAudioOperation(() => _audioService.ToggleAll());
        _muteAllItem.Click += (_, _) => ExecuteAudioOperation(() => _audioService.SetAllMuted(true));
        _unmuteAllItem.Click += (_, _) => ExecuteAudioOperation(() => _audioService.SetAllMuted(false));
        _hotkeysItem.Click += (_, _) => ShowSettingsWindow();
        _exitItem.Click += (_, _) =>
        {
            _exitRequested = true;
            Close();
        };

        _englishItem.Click += (_, _) => SetLanguage("en");
        _portugueseItem.Click += (_, _) => SetLanguage("pt-BR");
        _languageItem.DropDownItems.AddRange([_englishItem, _portugueseItem]);

        _trayMenu.Items.AddRange([
            _toggleAllItem,
            _muteAllItem,
            _unmuteAllItem,
            new ToolStripSeparator(),
            _hotkeysItem,
            _languageItem,
            new ToolStripSeparator(),
            _exitItem
        ]);
    }

    private void BuildUi()
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 2,
            RowCount = 7
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));

        for (int i = 0; i < 7; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, i % 2 == 0 ? 28 : 42));
        }

        AddHotkeyRow(layout, _toggleLabel, _toggleHotkeyBox, _toggleReset, 0);
        AddHotkeyRow(layout, _muteLabel, _muteHotkeyBox, _muteReset, 2);
        AddHotkeyRow(layout, _unmuteLabel, _unmuteHotkeyBox, _unmuteReset, 4);

        Controls.Add(layout);
    }

    private static void AddHotkeyRow(
        TableLayoutPanel layout,
        Label label,
        HotkeyTextBox textBox,
        Button resetButton,
        int labelRow)
    {
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.AutoSize = false;
        textBox.Dock = DockStyle.Fill;
        resetButton.Dock = DockStyle.Fill;
        resetButton.Margin = new Padding(8, 3, 0, 3);

        layout.Controls.Add(label, 0, labelRow);
        layout.SetColumnSpan(label, 2);
        layout.Controls.Add(textBox, 0, labelRow + 1);
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
        _trayIcon.Text = BuildTooltip(devices, allMuted);

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

        string status = allMuted == true
            ? Strings.Get(_settings.Language, "AllMuted")
            : Strings.Get(_settings.Language, "InputsOpen");
        string count = string.Format(Strings.Get(_settings.Language, "DeviceCount"), devices.Count);
        string firstDevice = devices[0].Name;
        string tooltip = $"{status} - {count} - {firstDevice}";
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
    }

    private void SetLanguage(string language)
    {
        _settings.Language = language;
        SaveSettings();
        ApplyLanguage();
        RefreshAudioStatus(false);
    }

    private void ApplyLanguage()
    {
        string language = _settings.Language;
        Text = Strings.Get(language, "AppName");
        _toggleAllItem.Text = Strings.Get(language, "ToggleAll");
        _muteAllItem.Text = Strings.Get(language, "MuteAll");
        _unmuteAllItem.Text = Strings.Get(language, "UnmuteAll");
        _hotkeysItem.Text = Strings.Get(language, "Hotkeys");
        _languageItem.Text = Strings.Get(language, "Language");
        _englishItem.Text = Strings.Get(language, "English");
        _portugueseItem.Text = Strings.Get(language, "Portuguese");
        _exitItem.Text = Strings.Get(language, "Exit");
        _englishItem.Checked = language == "en";
        _portugueseItem.Checked = language == "pt-BR";

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
        ThemeService.ApplyTitleBar(this, lightTheme);

        Color backColor = lightTheme ? Color.White : Color.FromArgb(32, 32, 32);
        Color foreColor = lightTheme ? Color.FromArgb(24, 24, 24) : Color.White;
        Color inputBackColor = lightTheme ? Color.White : Color.FromArgb(48, 48, 48);
        Color borderColor = lightTheme ? Color.FromArgb(240, 240, 240) : Color.FromArgb(64, 64, 64);

        BackColor = backColor;
        ForeColor = foreColor;
        _trayMenu.BackColor = backColor;
        _trayMenu.ForeColor = foreColor;

        foreach (Control control in Controls.Cast<Control>().SelectMany(FlattenControls))
        {
            control.BackColor = control is TextBox ? inputBackColor : backColor;
            control.ForeColor = foreColor;
            if (control is Button button)
            {
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = borderColor;
            }
        }

        _onIcon?.Dispose();
        _offIcon?.Dispose();
        string suffix = lightTheme ? "dark" : "white";
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
}
