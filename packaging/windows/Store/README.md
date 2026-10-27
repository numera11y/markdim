# Microsoft Store package

The Windows Store upload is a self-contained x64 MSIX. The package is unsigned on purpose; Microsoft signs it after Partner Center certification.

```powershell
./packaging/windows/Build-StorePackage.ps1 `
  -PackageVersion 1.0.1.0 `
  -Publisher 'CN=<exact Publisher value from Partner Center>'
```

- App identity name is reserved as `numera11y.markdim` in Partner Center.
- `Publisher` must exactly match the **Publisher** value shown in the app's Product identity details. Do not substitute the organization display name.
- The first submitted package is `1.0.0.0`; the next update uses `1.0.1.0`. Check Partner Center's latest submitted package before future updates.
- Use a four-part numeric package version. Windows Store reserves the fourth component, so it must remain `0`; increase one of the first three components for future Store updates. This package version is separate from the app's semantic version (`0.3.0-beta.7`).
- If package image files are absent, the build script creates `StoreLogo.png`, `Square44x44Logo.png`, `Square150x150Logo.png`, and `Wide310x150Logo.png` from the current application icon without changing its artwork.
- The resulting package is written to `release/store/markdim_<version>_x64.msix`.

Upload that MSIX in Partner Center under the reserved markdim app's **Packages** page. Then complete any requested listing details, run certification, and submit the app.

Suggested English product listing text is in [`ListingCopy.md`](ListingCopy.md). The product listing is maintained separately in Partner Center; editing the package manifest or this copy file does not change an existing submission.
