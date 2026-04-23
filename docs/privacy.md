---
layout: default
title: CtrlCV - Privacy Policy
permalink: /privacy/
---

# Privacy Policy for CtrlCV

*Last updated: April 23, 2026*

CtrlCV ("the app") is a local clipboard manager for Windows. This policy explains exactly what the app does and does not do with your data. The developer of CtrlCV does not operate any server, account system, or analytics backend.

---

## 1. Information we collect

The developer of CtrlCV does **not** collect, transmit, sell, or share any personal information, clipboard contents, screenshots, OCR results, usage data, telemetry, crash reports, advertising identifiers, or device identifiers.

---

## 2. Data processed locally on your device

To provide its functionality, CtrlCV processes the following data **entirely on your own device**. None of it is sent to the developer or any third party.

- **Clipboard contents (text and images).** When you copy content (Ctrl+C), the app reads the system clipboard and keeps up to 10 recent items in memory so you can paste them later. Unpinned items exist only in memory and are discarded when the app closes.
- **Screenshots.** If you use the screenshot hotkey, the captured image is stored in a clipboard slot on your device.
- **OCR (text recognition) on images.** If you use *Extract Text* or enable auto-OCR, images are processed locally by the built-in Windows OCR engine provided by your operating system. No image or text leaves your device for OCR.
- **Pinned items and settings.** Items you explicitly pin, along with your app settings, are saved to a local LiteDB database at `%APPDATA%\CtrlCV\CtrlCV.db`. This file is stored in plaintext and is **not encrypted**. Do not pin passwords, tokens, private images, or other confidential data. You can wipe this data at any time using *Settings -> Forget Persisted Pins*, or by deleting the `%APPDATA%\CtrlCV` folder.

---

## 3. Network connections

CtrlCV makes only one type of outbound network request: an optional update check to the public GitHub Releases API (`https://api.github.com/repos/keatkean/CtrlCV/releases/latest`) to determine whether a newer version of the app is available, and to download the new version if you choose to update. These requests contain no personal information beyond what is inherent to any HTTPS request (such as your IP address, which is seen by GitHub under [GitHub's own Privacy Statement](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement)).

The app does **not** send your clipboard data, screenshots, settings, or any other user content over the network.

---

## 4. Third-party services

The app relies on the following components, whose behavior is governed by their own providers:

- **Windows OCR engine** -- runs locally as part of your Windows installation.
- **GitHub Releases** -- used only for update checks and downloads, as described above.
- **Microsoft Store** -- if you installed CtrlCV from the Microsoft Store, Microsoft may collect installation, licensing, and diagnostic data under the [Microsoft Privacy Statement](https://privacy.microsoft.com/privacystatement). This is independent of the app.

---

## 5. Children's privacy

CtrlCV is a general-purpose utility and is not directed at children. The app does not knowingly collect any personal information from anyone, including children under 13.

---

## 6. Data retention and deletion

All user data remains on your device. You can remove it at any time by:

- Clicking *Settings -> Forget Persisted Pins* to delete pinned items from disk, or
- Deleting the `%APPDATA%\CtrlCV` folder, or
- Uninstalling the app.

---

## 7. Security

Because the local database is not encrypted, any process or user account with access to your Windows user profile can read it. Please do not pin confidential information. The app follows Windows single-user file permissions and does not expose any network listening port.

---

## 8. Changes to this policy

If this policy changes, the updated version will be published with the app's release notes on GitHub and in the Microsoft Store listing. The *Last updated* date above will be revised accordingly.

---

## 9. Contact

For privacy questions or requests, open an issue at [github.com/keatkean/CtrlCV/issues](https://github.com/keatkean/CtrlCV/issues) or contact the developer via the support contact listed on the Microsoft Store product page.
