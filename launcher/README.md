# QuestCompass Launcher

The Windows Forms launcher installs the public GPLv3 release and can check a static HTTPS release manifest. It does not require an account, process payments, or disable the addon when a paid service expires.

## Build

From this directory, run:

```powershell
dotnet build -c Release
```

To create a portable Windows x64 release that does not require a separately installed .NET runtime:

```powershell
dotnet publish .\QuestCompass.Launcher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\publish\win-x64
```

Distribute the published folder as a ZIP. Keep the launcher executable, `appsettings.json`, `README.md`, and `downloads\QuestCompass-1.8.0.zip` together.

The GPLv3 addon archive is copied beside the launcher under `downloads\QuestCompass-1.8.0.zip`; it is not embedded into the launcher executable. Keep the addon's source and license files in every redistributed addon release.

## Configure the public update feed

1. Publish `..\updates\latest.json` and the referenced ZIP to the website over HTTPS.
2. `appsettings.json` is configured for the GitHub Pages feed at `https://highlylikely2169-lgtm.github.io/QuestCompass/updates/latest.json`; change it if the production URL changes.
3. Publish the launcher and its `appsettings.json` together.
4. For each public release, update `version`, `downloadUrl`, and the lowercase SHA-256 in `latest.json`.

The launcher resolves relative package URLs against the manifest URL and verifies the package checksum before installing it. A checksum protects against a damaged download; it is not a substitute for a signed release manifest if the website or hosting account is compromised.

The selected game path can be the `_classic_beta_` folder, a WoW folder containing `_classic_beta_`, a client folder containing `Wow.exe`, or its `Interface\AddOns` folder.

## Payment and licensing boundary

Payments, subscriber accounts, and protected launcher delivery are not implemented. The current update manifest is public and anyone can access a launcher package hosted at a public URL; it must not be described as payment-gated. A future paid service needs server-side Monobank verification and private package delivery. The GPLv3 addon and source remain available under their license, and this launcher must not disable or restrict copies of the addon.
