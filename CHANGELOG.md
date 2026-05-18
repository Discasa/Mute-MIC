# Changelog

## 1.2.1 - 2026-05-18

- Reduced tray menu shadow and removed the dead gap between the main menu and submenus.
- Improved tray menu placement near the screen edge so submenus remain reachable.
- Hardened outside-click detection while the custom tray menu is open.
- Changed automatic updates to check only once after app startup instead of polling every two minutes.

## 1.2.0 - 2026-05-18

- Added a tray `Color scheme` submenu with monochromatic and colorful icon modes.
- Restored theme-aware monochromatic tray icons while keeping green/orange icons as the colorful mode.
- Added system-language detection for the app, installer, and uninstaller defaults.
- Improved Portuguese translations across the app and setup flows.
- Hardened the custom tray menu so it closes reliably when clicking outside it.

## 1.1.3 - 2026-05-18

- Changed tray state icons: unmuted/on now uses green and muted/off now uses orange.
- Preserved the previous theme-based tray icons under `assets/alternative/tray-icons-original`.

## 1.1.2 - 2026-05-18

- Fixed automatic update installation when the old app launches the installer from inside the installed app folder.
- Added retry logic while replacing the previous installation folder during silent updates.
- Set the updater-launched installer working directory to the temp folder for future updates.

## 1.1.1 - 2026-05-18

- Fixed hotkey editor input alignment by letting the custom field panel size and center the native text boxes.
- Fixed focused hotkey field borders being clipped by the hosted text box.

## 1.1.0 - 2026-05-18

- Added automatic per-user updates from GitHub Releases, including package download, optional SHA256 digest verification, silent installer launch, app restart, and update error logging.
- Changed startup from an elevated scheduled task to `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
- Changed the installer and uninstaller manifests to run as the current user instead of requesting administrator privileges.
- Removed the run-as-administrator shortcut flag while keeping cleanup for legacy scheduled tasks.
- Updated documentation for the per-user install, startup, uninstall, and update flow.

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
