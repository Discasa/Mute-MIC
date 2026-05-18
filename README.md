# Mute MIC

Mute MIC is a Windows tray utility that toggles every active audio input device
at the same time. It is designed for setups with multiple capture endpoints,
such as a physical audio interface plus a virtual microphone.

## Features

- Mutes or unmutes all active Windows capture endpoints together.
- Default toggle hotkey: `Ctrl+M`.
- Mute-only and unmute-only hotkeys default to `None`.
- Tray icon changes with the Windows app theme.
- White icons are used on dark theme; dark gray icons are used on light theme.
- English UI by default, with a Portuguese option in the tray menu.
- Sound feedback is embedded in the app at 80% volume.
- Installer and uninstaller are included.
- No microphone selection UI. The app always targets all active inputs.

## Install

Build the release package:

```powershell
.\tools\build.ps1
```

Run:

```powershell
.\release\Mute MIC Installer.exe
```

The installer copies the app to `%LOCALAPPDATA%\Mute MIC`, creates Start Menu
shortcuts, removes the legacy `MicMute` scheduled task when elevated, and creates
a new `Mute MIC` logon task with highest privileges.

## Use

- Left-click the tray icon to toggle all audio inputs.
- Right-click the tray icon for mute, unmute, hotkeys, language, and exit.
- Open `Hotkeys` to change or clear the three global hotkeys.
- Press `Delete`, `Backspace`, or `Esc` inside a hotkey field to clear it.

## Uninstall

Use the Start Menu shortcut named `Uninstall Mute MIC`, or run:

```powershell
%LOCALAPPDATA%\Mute MIC\Mute MIC Uninstaller.exe
```

The uninstaller removes the app, Start Menu shortcuts, scheduled tasks, and user
settings.
