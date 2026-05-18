# Mute MIC Documentation

## Scope

Mute MIC targets active Windows audio capture endpoints. In Windows terms, these
are devices exposed through Core Audio with `DataFlow.Capture` and
`DeviceState.Active`.

Examples:

- USB audio interface input
- Physical microphone
- Virtual microphone such as NVIDIA Broadcast

The app does not target playback devices. Applications that bypass Windows Core
Audio, such as some ASIO-only workflows, may not follow endpoint mute state.

## Architecture

The repository contains three .NET projects:

- `src/MuteMIC.App`: tray application.
- `src/MuteMIC.Installer`: elevated installer.
- `src/MuteMIC.Uninstaller`: elevated uninstaller.

The app is a modern .NET Windows Forms application targeting `net10.0-windows`.
It uses NAudio for Core Audio endpoint enumeration and mute control.

## Audio Behavior

The main service is `AudioInputMuteService`.

- `SetAllMuted(true)` sets every active capture endpoint to muted.
- `SetAllMuted(false)` sets every active capture endpoint to unmuted.
- `ToggleAll()` mutes all inputs when any input is open, otherwise unmutes all
  inputs.

This deterministic toggle avoids the unsafe behavior of flipping devices one by
one into a mixed state.

The app also accepts non-interactive commands for verification and automation:

```powershell
"%LOCALAPPDATA%\Mute MIC\Mute MIC.exe" --mute-all
"%LOCALAPPDATA%\Mute MIC\Mute MIC.exe" --unmute-all
"%LOCALAPPDATA%\Mute MIC\Mute MIC.exe" --toggle-all
```

## Hotkeys

Global hotkeys are implemented with the Windows `RegisterHotKey` API.

Defaults:

- Toggle all inputs: `Ctrl+M`
- Mute all inputs: `None`
- Unmute all inputs: `None`

Settings are stored under:

```text
HKCU\Software\Mute MIC
```

## Theme And Icons

The app reads the Windows app theme from:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme
```

Runtime icon behavior:

- Dark Windows theme: white tray/window icons.
- Light Windows theme: dark gray tray/window icons.

The executable, installer, and uninstaller use the white mic-on icon as their
static file icon.

## Assets

Original user-provided icons and audio are kept under `assets/source`.
Processed assets are kept under `assets/generated` and embedded into the app
from `src/MuteMIC.App/Assets`.

The sound files are embedded WAV resources normalized to 80% of their source
volume.

## Installer

The installer requires administrator privileges because it creates a logon task
with highest privileges. Elevated launch keeps the global hotkey reliable when
games or other elevated applications are focused.

Installed location:

```text
%LOCALAPPDATA%\Mute MIC
```

Scheduled task:

```text
Mute MIC
```

The installer also attempts to remove the legacy task:

```text
MicMute
```
