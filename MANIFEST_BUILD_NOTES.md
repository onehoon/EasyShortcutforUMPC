Manifest build notes

Current effective build path

- The active solution (`Easy Shortcut for UMPC.sln`) currently includes only the root UWP project:
  - `Easy Shortcut for UMPC.csproj`
- Therefore, the effective manifest for current builds is:
  - `Package.appxmanifest` (repo root)

Packaging project status

- `EasyShortcut.Package/Package.appxmanifest` exists for WAP/Desktop Bridge packaging.
- It is kept synchronized for:
  - Identity
  - Display names
  - Game Bar widget settings (including Window size)
- It is not the active output path unless the WAP project is explicitly built.

Store submission guidance

- If submitting with the current UWP-only build flow, verify final package metadata from the installed package manifest:
  - `%ProgramFiles%\\WindowsApps\\<PackageName>\\AppxManifest.xml`
- If switching to WAP flow later, ensure WAP becomes the only shipping path and keep one source-of-truth manifest strategy.
