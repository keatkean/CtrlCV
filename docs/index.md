---
layout: default
title: CtrlCV - Multi-Slot Clipboard Manager
---

<style>
.download-btn {
  display: inline-block;
  padding: 14px 36px;
  font-size: 1.25rem;
  font-weight: bold;
  color: #fff !important;
  background-color: #1e6f28;
  border-radius: 8px;
  text-decoration: none !important;
  margin: 8px 8px 8px 0;
  transition: background-color 0.2s;
}
.download-btn:hover { background-color: #165a20; }
.github-btn {
  display: inline-block;
  padding: 14px 36px;
  font-size: 1.25rem;
  font-weight: bold;
  color: #fff !important;
  background-color: #333;
  border-radius: 8px;
  text-decoration: none !important;
  margin: 8px 8px 8px 0;
  transition: background-color 0.2s;
}
.github-btn:hover { background-color: #555; }
.download-box {
  text-align: center;
  margin: 32px 0;
  padding: 24px;
  background: #f6f8fa;
  border-radius: 10px;
}
.warning-box {
  margin: 24px 0;
  padding: 16px 20px;
  background: #fff8e1;
  border-left: 4px solid #f4a100;
  border-radius: 6px;
  color: #4a3a00;
}
.warning-box strong { color: #7a5b00; }
</style>

# Multi-Slot Clipboard Manager for Windows

Copy up to 10 items, paste any of them instantly with a hotkey, and capture screenshots -- all from one lightweight app.

<div class="download-box">
  <a href="ms-windows-store://pdp/?productid=YOUR_PRODUCT_ID" class="download-btn">Get it from Microsoft Store</a>
  <a href="https://github.com/keatkean/CtrlCV/releases/latest" class="github-btn">Download from GitHub</a>
  <br><small>Windows 10+ (x64) &middot; Available as Store App or Single-file EXE</small>
</div>

---

## How It Works

1. **Copy anything as usual** (Ctrl+C) -- each copied item is automatically saved into a numbered slot.
2. **Paste a specific slot** using a hotkey (default: Ctrl+1 through Ctrl+0).
3. **Take a screenshot** with a global hotkey (default: Ctrl+Alt+PrintScreen) -- choose full screen, active window, or drag-to-select a region.

When all slots are full, the oldest unpinned item is replaced. Pin important items to protect them from being evicted.

![CtrlCV in action](assets/screenshots/demo.gif)

---

## Features

| Feature | Description |
|---|---|
| **Multi-slot clipboard** | Stores up to 10 text and image items with FIFO rotation |
| **Pin items** | Protect important items from being replaced |
| **Global hotkeys** | Paste from any slot in any application |
| **Screenshot tool** | Full screen, active window, or region selection |
| **Extract text (OCR)** | Right-click any image slot to extract text, or enable automatic extraction after every screenshot |
| **Context menu** | Pin, remove, or clear items directly from the list |
| **Multi-select** | Select multiple items with Ctrl+click or Shift+click, then delete in one go |
| **Configurable** | Change hotkey modifiers, max slots, startup behavior |
| **System tray** | Minimize to tray with quick-action context menu |
| **DPI-aware** | Scales correctly across different displays and scaling settings |
| **Single instance** | Prevents multiple copies from running |
| **Start with Windows** | Optional auto-start at login |
| **Check for updates** | Optional auto-update for the GitHub release |
| **Floating widget** | Always-on-top toolbar with slot thumbnails, drag-and-drop, hover preview, compact mode, and auto-hide |
| **Pinned persistence** | Pinned items and settings survive restarts, stored locally via LiteDB (large images via GridFS) |

---

## Default Hotkeys

| Shortcut | Action |
|---|---|
| Ctrl+1 ... Ctrl+9 | Paste slot 1 through 9 |
| Ctrl+0 | Paste slot 10 |
| Ctrl+Alt+PrintScreen | Take a screenshot (opens mode picker) |

Hotkey modifiers are configurable in Settings (Ctrl, Ctrl+Alt, or Ctrl+Shift).

---

## Screenshot Modes

When you press the screenshot hotkey, a context menu appears with three options:

- **Full Screen** -- captures all monitors
- **Active Window** -- captures the currently focused window
- **Select Region** -- opens a crosshair overlay where you drag to select an area

Captured screenshots are stored in the next available slot and placed on the clipboard.

After capturing a screenshot, right-click the image slot and select **Extract Text** to run OCR and copy the recognized text to the clipboard. The extracted text is also stored as a new slot.

To skip the manual step, enable **Auto-extract text from screenshots (OCR)** in Settings. When enabled, every screenshot is automatically run through OCR in the background and the recognized text is added as a new slot and placed on the clipboard. The right-click **Extract Text** option stays available for any image copied from elsewhere.

---

## Settings

| Setting | Default | Description |
|---|---|---|
| Paste hotkey modifier | Ctrl | Modifier for paste hotkeys (Ctrl, Ctrl+Alt, Ctrl+Shift) |
| Maximum slots | 10 | Number of clipboard slots (1-10) |
| Screenshot hotkey modifier | Ctrl+Alt | Modifier for screenshot hotkey |
| Start minimized | Off | Launch minimized to system tray |
| Run at Windows startup | Off | Auto-start when you log in |
| Auto-extract text from screenshots (OCR) | Off | Run OCR automatically after every screenshot |
| Enable floating widget | Off | Show the floating clipboard toolbar |
| Compact mode | Off | Use small color-coded circles instead of thumbnails |
| Widget opacity | 85% | Opacity of the floating widget (20-100%) |
| Auto-hide | On | Fade the widget when the mouse leaves |
| Auto-hide delay | 3s | Seconds before the widget fades (1-10) |
| Widget orientation | Horizontal | Horizontal or vertical layout |

Settings and pinned items are saved to `%APPDATA%\CtrlCV\CtrlCV.db` (a LiteDB file). A one-time migration moves any existing `settings.json` into the DB and renames the old file to `settings.json.migrated-<timestamp>`.

---

## Persistence

- **Pinned items survive restart.** Text and images you pin are written to `%APPDATA%\CtrlCV\CtrlCV.db` on change (debounced) and rehydrated on the next launch.
- **Unpinned items are session-only.** The regular clipboard history stays in memory and is cleared when the app exits -- pin anything you want to keep.
- **Large images are handled.** Images up to 32 MB are persisted. Small images (< 1 MB PNG) are embedded in the document; larger images go into LiteDB's GridFS bucket so the main document stays compact.
- **Text cap.** Pinned text over 5 MB stays in memory for the session but is not saved to disk.
- **Clear All keeps pinned items.** The *Clear All* button and menu only remove unpinned items; pinned items (and their on-disk copy) are preserved. To delete a pinned item, right-click it and choose *Remove*.
- **Wipe on demand.** *Settings -> Forget Persisted Pins* deletes every pinned item stored on disk in one click.

<div class="warning-box">
  <strong>Security note:</strong> The database is <em>not</em> encrypted. Pinned items and settings are stored in plaintext inside your user profile. Any process running as your Windows user -- including malware or other apps under the same account -- can read this file. Backup and sync tools (OneDrive Known Folder Move, roaming profiles, imaging software) will copy it in plaintext.
  <br><br>
  <strong>Do not pin passwords, tokens, private images, or any other confidential data.</strong> Use <em>Settings -> Forget Persisted Pins</em> to wipe the on-disk copy.
</div>

---

## Requirements

- Windows 10 or later (x64)
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (LTS) -- not needed if using the self-contained single-file EXE

---

## Build from Source

```bash
dotnet build
dotnet run
```

Or open `CtrlCV.sln` in Visual Studio 2022 and press F5.

To create a self-contained single-file EXE:

```bash
dotnet publish -p:PublishProfile=SingleFileExe
```

The output is a single `CtrlCV.exe` in `bin\Publish\` (~75 MB). Copy it to any Windows 10+ (x64) machine and run -- no installation or runtime required. The bundle is Brotli-compressed; the first launch extracts native libraries to `%LOCALAPPDATA%\Temp\.net\CtrlCV\` and caches them for subsequent runs.

---

## Known Limitations

- **Hotkey conflicts** -- Global hotkeys may override shortcuts in other apps. Change the modifier in Settings to avoid conflicts.
- **Elevated apps** -- Pasting into apps running as Administrator requires CtrlCV to also run as Administrator.
- **Only pinned items persist** -- Unpinned clipboard history is session-only and is lost when the app exits. Pin items you want to keep.
- **Unencrypted DB** -- See the *Security note* above: don't pin secrets.
- **Text and images only** -- Other clipboard formats (files, rich text, etc.) are not captured.
- **OCR accuracy** -- Text extraction uses the built-in Windows OCR engine. Accuracy depends on image quality and installed Windows language packs.

---

## License

This project is licensed under the [GNU General Public License v3.0](https://github.com/keatkean/CtrlCV/blob/main/LICENSE).

---

<div class="download-box">
  <strong style="font-size: 1.1rem;">Ready to try CtrlCV?</strong><br><br>
  <a href="ms-windows-store://pdp/?productid=YOUR_PRODUCT_ID" class="download-btn">Get it from Microsoft Store</a>
  <a href="https://github.com/keatkean/CtrlCV/releases/latest" class="github-btn">Download Latest Release</a>
  <br><small>Windows 10+ (x64) &middot; Available as Store App or Single-file EXE</small>
</div>

<p style="text-align: center; color: #888; font-size: 0.85em;">
  Made by <a href="https://github.com/keatkean">keatkean</a> &middot;
  <a href="https://github.com/keatkean/CtrlCV">Source Code</a> &middot;
  <a href="https://github.com/keatkean/CtrlCV/releases/latest">Releases</a> &middot;
  <a href="{{ '/privacy/' | relative_url }}">Privacy Policy</a>
</p>
