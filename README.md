# Custom Workers

Custom worker roster extension for `TCG Card Shop Simulator`.

## Build

The project expects install-derived game binaries to be provided through `GameRoot`.

Local example:

`dotnet test CustomWorkers.Tests/CustomWorkers.Tests.csproj /p:GameRoot="/path/to/TCG Card Shop Simulator"`

`dotnet build CustomWorkers.csproj --configuration Release /p:GameRoot="/path/to/TCG Card Shop Simulator"`

## Private dependency pipeline

Anything copied from a local install stays private.

- Public source repository: this repo
- Private GitHub release fallback: `DemonBigj781/custom-workers-private-deps`
- Optional private Google Drive backend: supported in parallel through CI scripts

## Devcontainer workflow

The devcontainer is for acquiring private dependency bundles from Steam without committing install-derived files.

1. Open the repo in the devcontainer.
2. Set Steam access environment variables if you want `steamcmd` download mode:
   - `STEAM_USERNAME`
   - `STEAM_PASSWORD`
   - optional `STEAM_GUARD_CODE_FILE`
3. Run:

`tools/deps/acquire-deps.sh tcg-card-shop-simulator`

Optional publish targets:

- GitHub release fallback:
  - `PRIVATE_DEPS_REPO`
  - `PRIVATE_DEPS_TAG`
- Google Drive:
  - `GOOGLE_SERVICE_ACCOUNT_JSON`
  - `GOOGLE_DRIVE_FOLDER_ID`

The acquisition flow waits for `steam-ready`, packages only the required DLLs, and can publish the resulting archive to one or both private backends.

## CI secrets

Current GitHub Actions supports parallel private backends.

- `PRIVATE_DEPS_TOKEN`
- `GOOGLE_DRIVE_FILE_ID` (optional)
- `GOOGLE_SERVICE_ACCOUNT_JSON_B64` (optional)
