# Operations

This document covers CI/CD pipelines, NuGet publishing, configuration, and known operational considerations.

Related: [Developer Guide](./developer-guide.md) | [Architecture](./architecture.md)

---

## Business View

### How Does the Software Reach Users?

Actus-Insurance.Core is distributed as **NuGet packages** — the standard packaging format for .NET libraries. When a new version is released:

1. A developer tags the repository with a version number (e.g. `v1.2.3`).
2. An automated pipeline builds, tests, and publishes the packages to [nuget.org](https://www.nuget.org/).
3. Any project that references the package will see the new version available.

There is also a **preview channel**: every commit to the `main` branch automatically publishes a preview package (e.g. `1.0.0-preview.42`). Teams that want to test the latest changes can reference the preview version before a stable release is made.

### Risks

- If the `NUGET_API_KEY` secret expires or is revoked, publishing will fail and no new versions will reach users.
- If a CI test failure is ignored and a broken package is published, downstream systems will receive incorrect calculations.

---

## Technical View

### CI/CD Pipelines

Both workflows are in `.github/workflows/`.

#### Preview Pipeline (`preview.yml`)

**Trigger:** Push to `main` branch

**Steps:**
1. Checkout repository
2. Set up .NET 9
3. `dotnet restore`
4. `dotnet build --configuration Release --no-restore`
5. `dotnet test --configuration Release --no-build --verbosity normal`
6. `dotnet pack --configuration Release --no-build -p:Version="1.0.0-preview.<run_number>" --output ./nupkgs`
7. `dotnet nuget push ./nupkgs/*.nupkg --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate`

The preview version format is `1.0.0-preview.<github_run_number>`.

#### Stable Release Pipeline (`release.yml`)

**Trigger:** GitHub Release published

**Steps:**
1. Checkout repository
2. Set up .NET 9
3. Extract semver from release tag (strips leading `v`; validates `major.minor.patch` format)
4. `dotnet restore`
5. `dotnet build --configuration Release --no-restore`
6. `dotnet test --configuration Release --no-build --verbosity normal`
7. `dotnet pack --configuration Release --no-build -p:Version="<extracted_version>" --output ./nupkgs`
8. `dotnet nuget push ./nupkgs/*.nupkg --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate`

**Tag format requirement:** `v<major>.<minor>.<patch>` — e.g. `v1.0.0`. The pipeline validates this with a regex and exits with an error if the format is wrong.

### Secrets

| Secret | Usage |
|---|---|
| `NUGET_API_KEY` | API key for pushing packages to nuget.org |

No other secrets are used. No connection strings, database credentials, or service endpoints are required.

### Configuration

#### Shared Build Properties

`Directory.Build.props` applies to all projects in the repository:

```xml
<Project>
  <PropertyGroup>
    <Authors>ActusLabs</Authors>
    <Company>ActusLabs</Company>
    <Copyright>Copyright © ActusLabs 2025</Copyright>
    <PackageTags>actus insurance actuarial finance contracts</PackageTags>
    <PackageRequireLicenseAcceptance>false</PackageRequireLicenseAcceptance>
    <RepositoryType>git</RepositoryType>
    <Version>1.0.0</Version>
  </PropertyGroup>
</Project>
```

The `Version` field is the fallback for local builds. CI overrides it with `-p:Version=...`.

#### Project-Level Settings

| Setting | Core | CPU |
|---|---|---|
| `TargetFramework` | `net9.0` | `net9.0` |
| `ImplicitUsings` | enabled | enabled |
| `Nullable` | enabled | enabled |
| `IsPackable` | true | true |
| `PackageId` | `ActusInsurance.Core` | `ActusInsurance.Core.CPU` |

The test project has no `IsPackable` override — `dotnet pack` skips it because it does not emit a `PackageId` intended for distribution.

### Environments

| Environment | Description |
|---|---|
| Local | Developer machine; `dotnet build` and `dotnet test` |
| CI (preview) | Ubuntu-latest runner on every push to `main` |
| CI (release) | Ubuntu-latest runner on every published GitHub Release |
| Distribution | nuget.org public feed |

There is no staging or production server environment. The library has no runtime process.

### Logging and Monitoring

The library itself produces no logs. It is an in-process library. Callers are responsible for any logging around their use of the library.

CI build and test logs are available in the GitHub Actions run history.

### Performance Considerations

- The library is designed for **in-process, synchronous** use.
- `StateSpace` is a `struct` — evaluation avoids heap allocations in the event loop.
- `ContractEvent` list is pre-allocated with capacity 50 (`new List<ContractEvent>(50)`) to reduce re-allocations for typical contract lifetimes.
- `RiskFactorModel` uses `Dictionary` lookups (O(1) average). The fallback "previous date" scan is O(n) on the number of time-series points for a given market object code, where n is typically small.
- No parallelism is implemented within the library. Callers can parallelise across multiple contracts externally.

### Maximum Lifetimes (from Constants)

```csharp
// src/ActusInsurance.Core/Util/Constants.cs
public static readonly TimeSpan MAX_LIFETIME     = TimeSpan.FromDays(365 * 50); // 50 years
public static readonly TimeSpan MAX_LIFETIME_STK = TimeSpan.FromDays(365 * 10); // 10 years
public static readonly TimeSpan MAX_LIFETIME_UMP = TimeSpan.FromDays(365 * 10); // 10 years
```

These constants are defined but are not currently enforced by the scheduling logic. They serve as documentation of intended contract lifetime limits.

### Evidence from Code

- `.github/workflows/preview.yml`
- `.github/workflows/release.yml`
- `Directory.Build.props`
- `src/ActusInsurance.Core/ActusInsurance.Core.csproj`
- `src/ActusInsurance.Core.CPU/ActusInsurance.Core.CPU.csproj`
- `src/ActusInsurance.Core/Util/Constants.cs`
