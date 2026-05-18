# Changelog

## 1.0.1 - 2026-05-17

- Fixed global hotkey handling by moving registration to a dedicated native message window.
- Removed toggle, mute, and unmute actions from the tray menu.
- Reworked tray tooltip text to show only the click action and configured toggle hotkey.
- Removed the white tray menu margin and added a themed rounded menu renderer.
- Applied Windows 11 frame colors and rounded-corner preference to the hotkey window.

## 1.0.0 - 2026-05-17

- Rebuilt the utility as a modern .NET Windows Forms app.
- Added simultaneous mute, unmute, and toggle for all active audio input devices.
- Removed the legacy single-device selection workflow.
- Added dynamic tray/window icons for dark and light Windows app themes.
- Embedded 80% volume sound feedback in the executable.
- Added English UI with Portuguese language option.
- Added elevated installer and uninstaller.
- Organized the folder as a Git repository with documentation and preserved
  legacy files for reference.
