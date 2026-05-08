Build notes

Active shipping path

- This repository ships from the root UWP project only:
  - `Easy Shortcut for UMPC.csproj`
- The effective manifest for build/package/output is:
  - `Package.appxmanifest`
- `EasyShortcut.Package` (WAP/Desktop Bridge) is not used in the active shipping flow.
- Game Bar folder policy:
  - Widget UI is rendered from XAML (`MainPage.xaml`), not HTML.
  - Legacy `GameBar/Widget.html` was removed to avoid duplicate maintenance drift.
  - Keep only actual packaged runtime assets under `GameBar/` (for example `GameBar/assets` and `GameBar/Icons`).

Helper packaging behavior

- Legacy C# helper project is archived at `_deprecated/ShortcutHelper` and is non-shipping.
- Active helper implementation is native C++ at `ShortcutHelperNative`.
- `ShortcutHelper` binaries are not committed.
- The UWP project publishes `ShortcutHelper/ShortcutHelper.csproj` automatically before packaging.
- Packaged helper files are taken from:
  - `ShortcutHelperNative/x64/Release/`

Release x64 build

```powershell
msbuild "Easy Shortcut for UMPC.csproj" /t:Build /p:Configuration=Release /p:Platform=x64 /p:PackageCertificatePassword=<password>
```

Output package

- `AppPackages/Easy Shortcut for UMPC_<version>_x64_Test/`
- Install via `Add-AppDevPackage.ps1` in that folder.

Certificate and signing notes

Local sideload/testing builds

- `PackageCertificateKeyFile` points to a local developer test certificate (`EasyShortcut_TemporaryKey.pfx`).
- The PFX is intentionally not committed (`.gitignore` excludes `*.pfx` and `*TemporaryKey*`).
- On a clean machine, create/import a local test certificate before building sideload packages.
- If using command-line build, pass:
  - `/p:PackageCertificatePassword=<password>`

Store submission builds

- Store submission packages are signed through the Microsoft Store pipeline.
- For Store upload workflow, use Visual Studio's Store packaging flow and generate the upload package (`.msixupload`/`.appxupload`) for Partner Center.
- Do not rely on committing a shared temporary PFX for Store submission.

Clean clone onboarding

1. Restore/build helper and app in `Release|x64`.
2. Ensure local test certificate exists for sideload package generation.
3. Use `Add-AppDevPackage.ps1` only for local test install.
4. Use Store packaging/upload flow for certification submission.
