# RSP CX Preflight

The Revit half of RSP FlightDeck: in-house QC for CORENET X before IFC export.
A dockable sidebar in Revit 2024–2027, built in C#. Its partner for checking
exported IFCs without Revit is [CX Radar](https://github.com/samuelooi/rsp-cx-radar).

> Status: **P0 skeleton.** The ribbon, the dockable sidebar, the request queue and
> change tracking work. It reads levels and element counts for real; the other
> checks are being ported from the pyRevit version in P1.

## Layout

| Project | What it holds | Needs Revit |
|---|---|---|
| `src/CxPreflight.Core` | Rules, readiness scoring, project data (netstandard2.0) | No |
| `src/CxPreflight.UI` | The WPF sidebar; talks to Revit only through `IPreflightHost` | No |
| `src/CxPreflight.Addin` | Start-up, ribbon, dockable pane, `RevitQueue` (ExternalEvent), model reading | Yes |
| `tools/CxPreflight.Preview` | The sidebar in a plain window with sample data | No |
| `tests/CxPreflight.Core.Tests` | xUnit tests for Core | No |

One codebase builds for each Revit version. Pick it with `-p:RevitVersion=…`:

| Revit | Runtime | Notes |
|---|---|---|
| 2024 | .NET Framework 4.8 | |
| 2025, 2026 | .NET 8 | Autodesk is moving later updates of these to .NET 10; test on updated builds too |
| 2027 | .NET 10 | |

The Revit API comes from the `Nice3point.Revit.Api.*` NuGet packages at compile
time only, so the code builds anywhere, including CI, without Revit installed.

## Set up (Windows)

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download). It builds all four targets.
2. In VS Code, install the recommended extensions (C# Dev Kit, PowerShell).
3. Open this folder in VS Code.

## Everyday loop

| To… | Do this |
|---|---|
| Run the Core tests | Task **Test Core**, or `dotnet test tests/CxPreflight.Core.Tests` |
| Work on the sidebar's look | Run **Sidebar preview (no Revit)** (F5). It opens the panel with sample data and a button that simulates an edit in Revit |
| Try it in Revit | Close that Revit version, then F5 **Revit 2025** (or 2024/2026/2027). This builds, deploys to `%APPDATA%\Autodesk\Revit\Addins\<version>`, and starts Revit under the debugger |
| Build everything | Task **Build all Revit versions**, or `./scripts/build.ps1` |
| Make install zips | `./scripts/build.ps1 -Configuration Release -Package` → `artifacts/dist` |

Notes:
- Revit locks the add-in files while it is open, and a dockable pane can only be
  registered when Revit starts. Changes to the add-in need a Revit restart.
- Revit 2024 uses the `clr` debugger (.NET Framework); 2025+ use `coreclr`.

## How the sidebar talks to Revit

```
PreflightPanel (WPF) ──▶ IPreflightHost.RunChecksAsync()
                              │
                     RevitHost queues the work
                              │
          RevitQueue ── ExternalEvent ──▶ runs on Revit's API thread
                              │
                       ModelReader ──▶ ModelSummary ──▶ back to the panel
```

Revit's `DocumentChanged` event only counts changed ids (no file or network work),
so tracking edits never slows modelling down.

## Testing levels

1. **Core** – unit tests on any machine; CI runs them on every push.
2. **Sidebar** – the preview app, no Revit.
3. **In Revit** – sample models, plus parity checks against the pyRevit version
   (BLK3 must match: 320 missing parameters, 226 proxies, 17 open GFA areas).

CI (`.github/workflows/build.yml`) runs the Core tests, then builds all four Revit
versions and uploads an install zip for each.
