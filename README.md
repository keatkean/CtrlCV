# CtrlCV - Multi-Slot Clipboard Manager

A lightweight Windows clipboard manager that stores up to 10 copied items (text and images) and lets you paste any of them instantly with a keyboard shortcut.

## How It Works

1. **Copy anything as usual** (Ctrl+C) -- each copied item is automatically saved into a numbered slot (1-10).
2. **Paste a specific slot** using Ctrl+1 through Ctrl+9 (slots 1-9) or Ctrl+0 (slot 10).
3. **Take screenshots** with Ctrl+Shift+PrintScreen -- choose full screen, active window, or drag-to-select a region.

When all 10 slots are full, the oldest item is replaced and you're notified via a system tray balloon.

## Hotkeys

| Shortcut | Action |
|---|---|
| Ctrl+1 ... Ctrl+9 | Paste slot 1 through 9 |
| Ctrl+0 | Paste slot 10 |
| Ctrl+Shift+PrintScreen | Take a screenshot (opens mode picker) |

## Screenshot Modes

When you press Ctrl+Shift+PrintScreen, a context menu appears with three options:

- **Full Screen** -- captures all monitors
- **Active Window** -- captures the currently focused window
- **Select Region** -- opens a crosshair overlay where you drag to select an area

Captured screenshots are stored in the next available slot and placed on the clipboard.

## Features

- Monitors the system clipboard for text and image copies
- 10 numbered slots with FIFO rotation when full
- Global hotkeys work from any application
- Built-in screenshot tool (full screen, active window, region select)
- System tray icon -- minimize to tray, right-click for quick actions
- Tray notifications when slots are full or errors occur
- Deduplicates consecutive identical text copies
- ListView UI showing slot number, type, and preview for each item

## Requirements

- Windows 10 or later
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (LTS)

## Build

```bash
dotnet build
```

## Run

```bash
dotnet run
```

Or open `CtrlCV.sln` in Visual Studio 2022 and press F5.

## Known Limitations

- **Hotkey conflicts**: Ctrl+1 through Ctrl+0 are global hotkeys and will override shortcuts in other apps (e.g., browser tab switching). A future version may allow custom modifier keys.
- **Elevated apps**: Pasting into applications running as Administrator requires CtrlCV to also run as Administrator (Windows UIPI restriction).
- **No persistence**: Clipboard slots are stored in memory only. They are lost when the app exits.
- **Text and images only**: Other clipboard formats (files, rich text, etc.) are not captured.

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE).

You are free to use, modify, and distribute this software, provided that any modified versions are also distributed under the same license. See the [LICENSE](LICENSE) file for the full text.
