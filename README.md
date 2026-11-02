# Ecierge.Uno

[![CI](https://github.com/Ecierge/Ecierge.Uno/actions/workflows/ci.yml/badge.svg)](https://github.com/Ecierge/Ecierge.Uno/actions/workflows/ci.yml)

Libraries for [Uno Platform](https://platform.uno) and WinUI applications.

| Package | Description |
|---|---|
| `Ecierge.Uno.Navigation` | Navigation: routes, regions, navigators and permission based visibility of controls. |
| `Ecierge.Uno.Controls` | Controls, styles and behaviors: grouped combo box, nullable number box, inline edit, loading text box and validation control. |
| `Ecierge.OpenIddict.Client.UnoTokenPersistence` | OpenIddict client token store on top of the Uno key-value storage. |

Every package targets `net10.0`, `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`, `net10.0-windows10.0.28000`, `net10.0-desktop` and `net10.0-browserwasm`.

## Packages

Release versions (`1.0.N`) are published to [nuget.org](https://www.nuget.org) and to the GitHub Packages feed of the organization. Every commit to `main` also publishes a development version (`1.0.N-g<commit hash>`) to that feed: `https://nuget.pkg.github.com/Ecierge/index.json`. GitHub Packages asks for a token with the `read:packages` scope even for public packages, so add the feed with credentials and map only the packages that live there:

```xml
<packageSources>
  <add key="Ecierge" value="https://nuget.pkg.github.com/Ecierge/index.json" protocolVersion="3" />
</packageSources>
<packageSourceMapping>
  <packageSource key="Ecierge">
    <package pattern="Ecierge.Uno.*" />
    <package pattern="Ecierge.OpenIddict.*" />
  </packageSource>
</packageSourceMapping>
```

## Building

Open `Ecierge.Uno.App.slnx` or build from the command line. To build one platform only, copy `solution-config.props.sample` to `solution-config.props` and uncomment a single `OverrideTargetFramework` line, or pass it on the command line:

```powershell
dotnet build Ecierge.Uno.Packages.slnf -c Release -p:OverrideTargetFramework=net10.0-desktop
dotnet test --solution Ecierge.Uno.CI.slnf -c Release
```

The `Android`, `iOS`, `Mac Catalyst` and `WebAssembly` heads need the matching .NET workloads (`dotnet workload install android ios maccatalyst wasm-tools`).

## Versions and releases

Versions come from [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning): `version.json` holds the major and minor number, the patch number is the number of commits since that value was set. GitHub Actions (`.github/workflows/ci.yml`) builds every pull request and every push to `main`, runs the tests and publishes the packages.

To release:

1. Check that the last commit of `main` is green.
2. Run `nbgv tag` on `main`, it creates the tag `v<version>` for the current commit, then push it with `git push origin v<version>`.
3. The workflow pushes the release packages to GitHub Packages and creates a GitHub Release with generated notes. The push to nuget.org waits for the approval of the `nuget.org` environment.

To start the next minor version change `version` in `version.json`, for example to `1.1`.

## License

[Apache License 2.0](LICENSE)
