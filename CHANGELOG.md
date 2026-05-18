# Changelog

## 1.0.4 - 2026-05-18

- Replaced the WPF tray popup with a custom layered WinForms popup to keep the smooth Windows 11 look without WPF runtime crashes in the installed single-file app.
- Reworked installer and uninstaller windows with cleaner spacing, rounded buttons, rounded location fields, and a Windows 11 style blue progress bar.
- Improved setup QA behavior by keeping progress/status text separated from final action buttons.
- Made uninstallation remove the installed application folder reliably after the app and uninstaller release their file locks.

## 1.0.3 - 2026-05-18

- Added a guided installer window with install location, confirmation, progress, and completion states.
- Added a guided uninstaller window with confirmation, progress, completion, and already-uninstalled feedback.
- Replaced the tray context menu with a WPF popup for smoother Windows 11 style corners, shadow, spacing, and submenu rendering.
- Reworked the hotkey editor fields and reset buttons with thinner borders, rounded corners, and Windows 11 themed spacing.

## 1.0.2 - 2026-05-18

- Improved the installer to create a dedicated Start Menu folder with app and uninstaller shortcuts.
- Added a Windows Installed Apps entry through the current-user uninstall registry key.
- Added the app icon as an installed `.ico` file and use it for Start Menu shortcuts and Windows Installed Apps.
- Updated the uninstaller to remove the Start Menu folder and Installed Apps entry.

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
