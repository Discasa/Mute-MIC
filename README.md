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
- Automatic per-user updates from GitHub Releases.
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

The installer shows the install location, asks for confirmation, displays
progress, copies the app to `%LOCALAPPDATA%\Mute MIC`, creates a `Mute MIC`
folder in the Start Menu with app and uninstaller shortcuts, registers Mute MIC
in Windows Installed Apps, removes legacy startup tasks when possible, and
creates a current-user startup entry under
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.

The release installer is a standalone package file with the app and uninstaller
embedded. It is still a framework-dependent .NET build and expects the .NET 10
Desktop Runtime on the target machine. The `payload` folder under `release/` is
kept only as a build artifact for inspection.

## Use

- Left-click the tray icon to toggle all audio inputs.
- Right-click the tray icon for hotkeys, language, and exit.
- Open `Hotkeys` to change or clear the three global hotkeys.
- Press `Delete`, `Backspace`, or `Esc` inside a hotkey field to clear it.

## Updates

When the app is running and the computer is online, Mute MIC checks the latest
GitHub Release periodically. If a newer stable version exists, it downloads the
release package, verifies the GitHub SHA256 digest when available, starts the
installer in silent mode, closes the running app, replaces the installed files,
and starts the new version again.

## Uninstall

Use Windows Settings > Apps > Installed apps, the Start Menu shortcut named
`Uninstall Mute MIC`, or run:

```powershell
%LOCALAPPDATA%\Mute MIC\Mute MIC Uninstaller.exe
```

The uninstaller shows a confirmation and progress window, then removes the app,
Start Menu shortcuts, startup entries, Windows Installed Apps entry, and user
settings.
