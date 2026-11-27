# RSP CX Preflight: instructions for Claude

C# Revit add-in (Revit 2024–2027): a dockable sidebar for in-house QC of models
before IFC export to CORENET X. Part 1 of RSP FlightDeck; part 2 is CX Radar
(browser IFC checker, repo `samuelooi/rsp-cx-radar`).

Read `docs/HANDOVER.md` first: it holds the agreed design, decisions, data
formats and the build plan. Keep it up to date when decisions change
(add to the decision log; don't rewrite old entries).

## Build and test

- .NET 10 SDK builds every target. Pick the Revit version with `-p:RevitVersion=2024|2025|2026|2027`
  (2024 → net48, 2025/2026 → net8.0-windows, 2027 → net10.0-windows). Default 2025.
- Core tests: `dotnet test tests/CxPreflight.Core.Tests` (must stay green).
- All versions: `./scripts/build.ps1` · install into Revit: `./scripts/build.ps1 -Versions 2025 -Deploy`
  (close that Revit first; it locks the files and the pane registers only at start-up).
- Sidebar without Revit: launch config "Sidebar preview (no Revit)".
- After any change to `src/CxPreflight.Addin`, build all four versions; API differences between versions
  surface only at compile time.

## Rules for the code

- `CxPreflight.Core` and `CxPreflight.UI` must never reference the Revit API. The UI reaches Revit only
  through `IPreflightHost`; Revit work goes through `RevitQueue` (ExternalEvent).
- Revit event handlers (DocumentChanged, Idling, SelectionChanged) do only cheap in-memory work:
  no file, network or heavy collectors (decision 13).
- Core stays netstandard2.0 and dependency-light, because the same DLL loads in four Revit runtimes.
- Every ported check gets a parity test against the pyRevit version on the same model before the
  pyRevit tool is retired.
- Branch per phase; don't commit to `main` directly.
