# Changelog

All notable changes to Centazio are listed here. Each release's section becomes its GitHub release notes.

## [Unreleased]

### Fixed
- `centazio` generated Azure and AWS function projects restore again: they use the package versions Centazio is built 
  and tested with, rather than the latest on nuget.org (which had moved to EF Core 11 / net11 and an incompatible 
  ApplicationInsights). Vulnerable transitive SQLite packages are pinned to their patched versions.
- `centazio` commands no longer fail when a tool writes warnings to stderr and exits successfully (eg: `az` update 
  notices).
- `centazio` no longer hangs on Linux waiting for output held open by processes `dotnet build` leaves running.
