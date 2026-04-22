# Changelog

## v1.3.0

### New

- **Pinned items now survive restart.** Pinned text and images (and app settings) are saved to `%APPDATA%\CtrlCV\CtrlCV.db` (LiteDB). Writes are debounced and atomic; corrupted DB files are auto-renamed and replaced on next launch.
- **Settings moved into the DB.** An existing `settings.json` is migrated on first run and renamed to `settings.json.migrated-<timestamp>` as a backup.
- **Size caps.** Pinned text up to 5 MB and images up to 32 MB are persisted; larger items stay in memory for the session with a one-time warning. Images over 1 MB go into LiteDB's GridFS so the main document stays compact.
- **Extract text from images (OCR).** Right-click any image slot (main list *or* floating widget) and choose *Extract Text* to run Windows OCR and copy the recognized text. A new *Auto-extract text from screenshots (OCR)* setting runs this automatically after every screenshot.
- **Per-monitor V2 DPI.** Main window, floating widget, preview popup, and settings dialog scale correctly across mixed-DPI displays and reflow when dragged between monitors.
- **New Settings buttons:** *Forget Persisted Pins* (wipes the on-disk copy and unpins items in memory) and *Reset Settings* (restores all defaults and clears the Windows startup registry entry).

### Changed

- **Clear All now keeps pinned items.** The *Clear All* button and menu only remove unpinned items; pinned items and their on-disk copy are preserved. A tray balloon confirms what was kept. To delete a pinned item, right-click it → *Remove*, or use *Settings → Forget Persisted Pins*.
- Clear All and Remove Selected buttons now show tooltips explaining their behavior.
- **Single-file publish** now uses Brotli compression (~75 MB output). First launch extracts native libraries to `%LOCALAPPDATA%\Temp\.net\CtrlCV\` and caches them.

### Fixed

- **GDI+ thumbnail leak:** the main list view leaked one bitmap handle per image slot on every refresh, eventually crashing the process.
- **Widget image slots rendered empty on startup** when pinned images were rehydrated from the DB.
- **Reset Settings** no longer leaves the *Run at Windows startup* registry entry behind when the user disables it via reset, and now also resets the *Auto-extract text from screenshots* flag.
- **Shutdown race** in the persistence store could throw `NullReferenceException` if a background write landed during `Dispose`.
- **Preview popup** could crash with "parameter is not valid" when a slot was removed while its preview was on screen.
- **Widget OCR duplicated the extracted text** as two slots because clipboard-monitoring suppression was released before `WM_CLIPBOARDUPDATE` was dispatched. The widget now also surfaces a tray balloon on OCR failure or when no text is detected, matching the main-list behavior.
- Minor cleanup of unreachable code in the clipboard add path.

### Security reminder

The database is **not encrypted**. Anything you pin is stored in plaintext inside your user profile. Don't pin passwords, tokens, or private data. Use *Settings → Forget Persisted Pins* to wipe the on-disk copy.
