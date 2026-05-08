Manifest build notes

Current effective build path

- The active solution (`Easy Shortcut for UMPC.sln`) currently includes only the root UWP project:
  - `Easy Shortcut for UMPC.csproj`
- Therefore, the effective manifest for current builds is:
  - `Package.appxmanifest` (repo root)

Packaging project status

- `_deprecated/EasyShortcut.Package/Package.appxmanifest` exists for archived WAP/Desktop Bridge packaging.
- It is not part of the active shipping path and should be treated as non-shipping/reference only.
- To avoid split-brain behavior, always build and submit from the root UWP project flow.

Store submission guidance

- If submitting with the current UWP-only build flow, verify final package metadata from the installed package manifest:
  - `%ProgramFiles%\\WindowsApps\\<PackageName>\\AppxManifest.xml`
- If switching to WAP flow later, first adopt WAP as the single shipping path and then re-align manifests before submission.
