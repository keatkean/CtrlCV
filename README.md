# CtrlCV - Multi-Slot Clipboard Manager

A lightweight Windows clipboard manager that stores up to 10 copied items (text and images) and lets you paste any of them instantly with a keyboard shortcut.

<a href="https://get.microsoft.com/installer/download/9ngvnm1prrkb?referrer=appbadge" target="_self">
  <img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200" alt="Get it from Microsoft Store"/>
</a>

## How It Works

1. **Copy anything as usual** (Ctrl+C) -- each copied item is automatically saved into a numbered slot.
2. **Paste a specific slot** using your configured hotkey (default: Ctrl+1 through Ctrl+0).
3. **Take screenshots** with a global hotkey (default: Ctrl+Alt+PrintScreen) -- choose full screen, active window, or drag-to-select a region.

When all slots are full, the oldest unpinned item is replaced and you're notified via a system tray balloon. Pin important items to protect them from being evicted.

## Features

- **Multi-slot clipboard** -- stores up to 10 text and image items with FIFO rotation
- **Pin items** -- pin important items so they aren't replaced when slots are full
- **Pinned persistence** -- pinned items and settings survive restarts (stored locally via LiteDB)
- **Global hotkeys** -- paste from any slot in any application
- **Screenshot tool** -- full screen, active window, or region selection
- **Extract text from images (OCR)** -- right-click any image slot to extract text using built-in Windows OCR, or enable automatic extraction after every screenshot
- **Right-click context menu** -- pin, remove, or clear items directly from the list
- **Multi-select** -- select multiple items with Ctrl+click or Shift+click, then delete them in one go (or press Delete)
- **Configurable settings** -- change hotkey modifiers, max slots, startup behavior
- **System tray** -- minimize to tray, right-click context menu for quick actions
- **DPI-aware UI** -- scales correctly across different displays and scaling settings
- **Single instance** -- prevents multiple copies from running and conflicting
- **Start with Windows** -- optional auto-start at login
- **Restore defaults** -- one-click reset in settings
- **Custom app icon** -- branded icon in title bar, taskbar, and system tray
- **Microsoft Store version** -- available natively as an MSIX app in the Store (100% offline, no internet capability required)
- **Check for updates** -- check for new versions from GitHub Releases (optional setting for the standalone `.exe` version; completely stripped from the Store version)
- **Floating clipboard widget** -- always-on-top toolbar showing thumbnails of all slots
  - Drag-and-drop from the widget into any app
  - Hover preview shows full text or full-size image
  - Compact mode with color-coded numbered circles
  - Configurable opacity, position, auto-hide behavior
  - Horizontal or vertical orientation

## Default Hotkeys

| Shortcut | Action |
|---|---|
| Ctrl+1 ... Ctrl+9 | Paste slot 1 through 9 |
| Ctrl+0 | Paste slot 10 |
| Ctrl+Alt+PrintScreen | Take a screenshot (opens mode picker) |

Hotkey modifiers are configurable in Settings (Ctrl, Ctrl+Alt, or Ctrl+Shift).

## Screenshot Modes

When you press the screenshot hotkey, a context menu appears with three options:

- **Full Screen** -- captures all monitors
- **Active Window** -- captures the currently focused window
- **Select Region** -- opens a crosshair overlay where you drag to select an area

Captured screenshots are stored in the next available slot and placed on the clipboard.

After capturing a screenshot, right-click the image slot and select **Extract Text** to run OCR and copy the recognized text to the clipboard. The extracted text is also stored as a new slot.

To skip the manual step, enable **Auto-extract text from screenshots (OCR)** in Settings. When enabled, every screenshot automatically runs through OCR in the background and the recognized text is added as a new slot and placed on the clipboard (if any text is found). The right-click **Extract Text** option remains available for images that were copied from elsewhere.

## Settings

Open Settings from the toolbar button or tray right-click menu. Options include:

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

Settings and pinned items are saved to `%APPDATA%\CtrlCV\CtrlCV.db` (a LiteDB file). A one-time migration moves any existing `settings.json` into the DB and renames the old file to `settings.json.migrated-<timestamp>` for rollback.

## Persistence

- **Pinned items survive restart.** Text and images you pin are written to `%APPDATA%\CtrlCV\CtrlCV.db` on change (debounced) and rehydrated on the next launch.
- **Unpinned items are session-only.** The regular clipboard history is kept only in memory and is lost when the app exits -- pin anything you want to keep.
- **Large images are handled.** Images up to 32 MB are persisted. Small images (< 1 MB PNG) are embedded in the document; larger images go into LiteDB's GridFS bucket so the main document stays compact.
- **Text cap.** Pinned text longer than 5 MB is kept in memory for the session but not saved to disk.
- **Clear All keeps pinned items.** The *Clear All* button and menu only remove unpinned items; pinned items (and their on-disk copy) are preserved. To delete a pinned item, right-click it and choose *Remove*.
- **Wipe on demand.** *Settings -> Forget Persisted Pins* deletes every pinned item stored on disk in one click.

### Security warning

The database is **not encrypted**. Pinned items and settings are stored in plaintext inside your user profile (`%APPDATA%\CtrlCV\CtrlCV.db`). Any process running as your Windows user -- including malware or other apps under the same account -- can read this file. A user with administrator rights can read it across user profiles. Backup and sync tools (OneDrive Known Folder Move, roaming profiles, imaging software) will copy the file in plaintext.

**Do not pin passwords, tokens, private images, or any other confidential data.** Use *Settings -> Forget Persisted Pins* to wipe the on-disk copy.

## Requirements

- Windows 10 or later (x64)
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (LTS) -- not needed if using the self-contained single-file EXE

## Build & Run

```bash
dotnet build
dotnet run
```

Or open `CtrlCV.sln` in Visual Studio 2022 and press F5.

To build the Microsoft Store (MSIX) compliant version, use the `ReleaseStore` configuration, which physically strips out the manual auto-updater and ensures the app requires no internet capabilities:

```bash
dotnet build -c ReleaseStore
```

## Publish (Microsoft Store MSIX bundle)

The Store submission accepts a single `.msixupload` file that bundles every architecture you ship. The repo contains a wrapper script that builds it via MSBuild on `CtrlCV.Package.wapproj` — preferred over the Visual Studio "Create App Packages" wizard, which has a known issue on .NET 8 where `obj\wappublish\<RID>\project.assets.json` is not pre-populated and the build fails with *"Assets file not found. Run a NuGet package restore to generate this file."*.

```powershell
# from the repo root, in PowerShell
.\scripts\build-store-package.ps1                  # build using the manifest's current version
.\scripts\build-store-package.ps1 -BumpRevision    # bump the 4th version component first
.\scripts\build-store-package.ps1 -BumpRevision -RunWack  # also run the Windows App Certification Kit
```

The script:

- Locates MSBuild via `vswhere` (no hard-coded paths).
- Cleans stale `obj\wappublish\` to avoid half-built per-RID restore state from a prior failed run.
- Builds with `Configuration=ReleaseStore` so the `STORE` compile constant strips the GitHub `UpdateChecker` from the binary (Microsoft Store policy 10.8.1 forbids third-party auto-update mechanisms).
- Bundles **x64 + ARM64** into a single submission. (x86 and 32-bit ARM are intentionally omitted; the `net8.0-windows10.0.19041.0` target framework already requires Windows 10 20H1+, where x86 desktops are essentially extinct, and 32-bit ARM was deprecated long ago.)
- Sets `UapAppxPackageBuildMode=StoreUpload` to produce a `.msixupload` (the file Partner Center accepts), not a sideload-only `.msix`.
- Leaves `AppxPackageSigningEnabled=false` (matching `CtrlCV.Package.wapproj`); the Microsoft Store re-signs the package server-side with the publisher certificate, so no local signing key is required.

The output lands at:

```
AppPackages\CtrlCV.Package_<version>_ReleaseStore_Test\CtrlCV.Package_<version>_x64_arm64_ReleaseStore.msixbundle
AppPackages\CtrlCV.Package_<version>_x64_arm64_bundle_ReleaseStore.msixupload
```

Drag the `.msixupload` into the **Packages** page of the app's Partner Center submission and tick **Windows 10/11 Desktop** under *Device family availability* (other device families don't support `runFullTrust` packaged Win32 apps).

Equivalent raw MSBuild command (if you'd rather not use the script):

```powershell
$msbuild = (& "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
    -latest -requires Microsoft.Component.MSBuild `
    -find "MSBuild\**\Bin\MSBuild.exe")

& $msbuild .\CtrlCV.Package\CtrlCV.Package.wapproj `
    /restore `
    /p:Configuration=ReleaseStore `
    /p:Platform=x64 `
    /p:AppxBundle=Always `
    /p:AppxBundlePlatforms="x64|arm64" `
    /p:UapAppxPackageBuildMode=StoreUpload `
    /p:AppxPackageSigningEnabled=false `
    /p:AppxPackageDir="$PWD\AppPackages\\"
```

The Store-reserved Package Identity values (`Identity/Name`, `Publisher`, `PublisherDisplayName`) live in `CtrlCV.Package\Package.appxmanifest` and are tied to the publisher's Partner Center account. They are committed to the repository as the values for this app's listing; if you fork CtrlCV and want to publish your own Store build, replace those three fields with your own from Partner Center → *App identity*.

## Publish (Single-File EXE)

To create a self-contained single-file EXE that runs on any Windows x64 machine without .NET installed:

```bash
dotnet publish -p:PublishProfile=SingleFileExe
```

The output is a single `CtrlCV.exe` in `bin\Publish\` (~75 MB). Copy it to any Windows 10+ (x64) machine and run -- no installation or runtime required. The bundle is Brotli-compressed; the first launch extracts native libraries to `%LOCALAPPDATA%\Temp\.net\CtrlCV\` and caches them for subsequent runs.

To publish manually without the profile:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

## Project Structure

```
CtrlCV/
├── Forms/
│   ├── FloatingWidgetForm.cs     # Floating clipboard toolbar (thumbnails, drag-and-drop)
│   ├── Form1.cs                  # Main form UI shell (ListView, tray, settings)
│   ├── Form1.Designer.cs         # Designer-generated UI layout
│   ├── Form1.resx                # Form resources
│   ├── PreviewPopupForm.cs       # Hover preview popup for widget
│   ├── ScreenshotOverlayForm.cs  # Region selection overlay
│   └── SettingsForm.cs           # Settings dialog UI
├── Helpers/
│   ├── NativeMethods.cs          # Win32 P/Invoke declarations
│   ├── ScreenshotHelper.cs       # Screen capture utilities
│   └── UpdateChecker.cs          # GitHub Releases update checker and self-updater
├── Images/
│   ├── Logo.ico                  # App icon (multi-size)
│   └── Logo.png                  # Source logo
├── Models/
│   ├── AppSettings.cs            # Settings model, routed through CtrlCvStore
│   ├── ClipboardItem.cs          # Text/image clipboard slot with IDisposable
│   └── PersistedClipboardItem.cs # LiteDB DTO for persisted pinned items
├── Properties/
│   └── PublishProfiles/
│       └── SingleFileExe.pubxml
├── Services/
│   ├── ClipboardManager.cs       # Clipboard monitoring, slot storage, eviction, pinned persistence hooks
│   ├── CtrlCvStore.cs            # LiteDB wrapper: pinned items + settings, debounced writes, GridFS, corruption recovery
│   ├── HotkeyManager.cs         # Global hotkey registration and dispatch
│   └── PasteService.cs           # Clipboard paste simulation (set + Ctrl+V)
├── CtrlCV.csproj                 # Project file (.NET 8 WinForms)
├── CtrlCV.sln                    # Solution file
└── Program.cs                    # Entry point, single-instance mutex
```

## Keyboard Shortcuts (in main window)

| Shortcut | Action |
|---|---|
| Delete | Remove selected item(s) |
| Ctrl+Click | Add/remove items from selection |
| Shift+Click | Select a range of items |

## Known Limitations

- **Hotkey conflicts**: Global hotkeys may override shortcuts in other apps (e.g., browser tab switching). Change the modifier in Settings to avoid conflicts.
- **Elevated apps**: Pasting into applications running as Administrator requires CtrlCV to also run as Administrator (Windows UIPI restriction).
- **Only pinned items persist**: Unpinned clipboard items are session-only and are lost when the app exits. Pin anything you want to keep.
- **Unencrypted DB**: See the *Security warning* above -- don't pin secrets.
- **Text and images only**: Other clipboard formats (files, rich text, etc.) are not captured.
- **OCR accuracy**: Text extraction uses the built-in Windows OCR engine. Accuracy depends on image quality and installed Windows language packs.

## Privacy

CtrlCV runs entirely on your device. The developer does not collect any personal data, clipboard contents, screenshots, OCR results, telemetry, or analytics. The only outbound network call is an optional update check to the public GitHub Releases API (this update check is completely removed from the Microsoft Store version, making it a 100% offline utility).

See the full [Privacy Policy](docs/privacy.md) for details (also published at <https://keatkean.github.io/CtrlCV/privacy/>).

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE).

You are free to use, modify, and distribute this software, provided that any modified versions are also distributed under the same license. See the [LICENSE](LICENSE) file for the full text.
