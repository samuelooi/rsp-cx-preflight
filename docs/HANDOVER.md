# CX Preflight: handover (27 Nov 2026)

Where the design and build stand, for whoever picks this up next.

## 1. Purpose

FlightDeck is RSP's two-part QAQC suite for CORENET X submissions:

- **CX Preflight** (this repo): inside Revit, used by the in-house BIM team while modelling.
  Prepare and check the model so the exported IFC has no technical issues.
- **CX Radar** (`samuelooi/rsp-cx-radar`): in the browser, no Revit needed. Final IFC-SG check
  for the PA team and external projects, BCF, readiness report.

Preflight started as a pyRevit toolbar (`RSP_CXFlightDeck.extension`, 25 buttons, check engine in
`lib/cxfd/`: `config.py`, `revitutil.py`, `ifcclass.py` and others, installed under
`%APPDATA%\pyRevit\Extensions\` on Samuel's PC). It is being rebuilt in C# as a dockable sidebar.

Design references:
- Design canvas (mockups, scenarios, decisions): https://claude.ai/artifact/AGx7fjSKrmVdJXqFmP8PNQ
- Miro copy (editable): https://miro.com/app/board/uXjVEJR_hyM=/

## 2. Decision log

Newest last. Replace a decision by adding a row; don't edit old ones.

| # | Date | Decision |
|---|---|---|
| 1 | 26 Nov | Preflight is a dockable sidebar. Ribbon keeps four buttons: Preflight Panel, Run Checks, Project Setup, Project Log. |
| 2 | 26 Nov | Tabs mirror CX Radar: dark panel, vertical tab strip with Readiness, Inspect, Parameters, Warnings, Compliance, Issues; User Guide and Feedback at the foot. Inspect and Parameters are Preflight-only. |
| 3 | 26 Nov | No IFC export in Preflight. Export uses Revit's own IFC export; Preflight only checks the export settings. No gate, no override. |
| 4 | 26 Nov | Full run when the panel or model opens; targeted re-check right after a fix made in the panel; prompt after edits made in Revit. |
| 5 | 27 Nov | Automatic re-checks only when they don't affect performance: small edits re-checked while Revit is idle (target under 1 s, to confirm); large change sets, new levels, links and syncs prompt instead. |
| 6 | 27 Nov | Readiness weights: Foundations 10 (5 items × 2), Classification 10, Data completeness 50, Spaces and areas 20, Agency rules 10. |
| 7 | 27 Nov | A project is several files. Project Setup lists every file (Revit and consultant IFC); Readiness shows each file and the project total, weighted by exported elements. |
| 8 | 27 Nov | The project folder lives on SharePoint / Teams, synced through OneDrive, so both tools read it as a normal folder. Models may stay on ACC. Direct ACC/SharePoint access is for later. |
| 9 | 27 Nov | The BIM Lead owns the project: only BIM Leads set up and edit it (folder permissions + role list in the tools). Modellers see progress read-only. |
| 10 | 27 Nov | Files can be split, merged, renamed or retired after setup; history carries over. To keep IfcGUIDs, split with Save As and delete, never copy-paste. |
| 11 | 27 Nov | The project folder is the main project log: effort, runs, fixes, issues, readiness at each submission. |
| 12 | 27 Nov | WDs are not logged in FlightDeck; they're tracked separately per project. Submission snapshots hold a reference so the two can be compared, to show the tool reduces WDs. |
| 13 | 27 Nov | Logging must never slow Revit: events held in memory, written while idle, failures silent. |
| 14 | 27 Nov | Build in C#, not pyRevit. Support Revit 2024–2027 from one codebase. |

Open: how the Next step card picks its step (per file and project) · matching submission snapshots to
WD records · where effectiveness numbers are viewed (dashboard vs in each tool) · what the panel does with
a Revit file that isn't in the project yet · confirm the 1 s auto re-check limit · code-signing
certificate (ask RSP IT for an internal-CA code-signing cert; until then users click "Always Load").

## 3. Readiness scoring (agreed)

| Group / item | Weight | Scores as |
|---|---|---|
| A Foundations | 10 | |
| A1 Project setup (info, gateway, authorities) | 2 | pass/fail |
| A2 IFC export settings match RSP standard | 2 | pass/fail |
| A3 Building storeys set | 2 | pass/fail |
| A4 Coordinates and survey point | 2 | pass/fail |
| A5 CX parameters loaded and bound | 2 | loaded ÷ required |
| B Classification | 10 | correct IFC class & subtype ÷ exported elements |
| C Data completeness | 50 | (filled + marked not required) ÷ required, for the gateway |
| D Spaces and areas | 20 | valid rooms/spaces/GFA ÷ total |
| E Agency rules | 10 | URA/BCA rules passed ÷ run |

Rules: only exported elements count · "not required" counts as met (logged with who/when) · a blocked
check scores 0 (C and E are blocked until parameters are loaded) · READY needs 100% with nothing blocked;
review-only warnings (e.g. GUID stability) don't change the % · weights live in the shared ruleset so
Radar scores the IFC the same way.

Project % = each file's % weighted by its share of exported elements (BIM Lead can override a weight).

Implemented in `src/CxPreflight.Core/Readiness.cs`; worked examples in `tests/`.

## 4. Sidebar (see the canvas for every state)

- **Header (always):** this file's %, file name + gateway, verdict pill, segmented bar (5 weighted
  segments), "Project N% · k files" link, status line (up to date / running / model changed / fix applied).
- **Readiness:** project card (each file %, weight, status; cross-file checks: survey point, level heights
  across files, duplicate IfcGUIDs, stale consultant IFC) · Next step card · this file's groups and items.
- **Inspect:** selected element: IFC class (with source), CX parameters by agency with inline input,
  apply to same type, mark not required, why required.
- **Parameters:** load / audit CX parameters, bulk fill (selection, a Compliance finding, or empty values
  in the whole model), check schedules, not-required marks.
- **Warnings:** what makes the IFC fail: storeys, parameters loaded, elements hidden from the export view,
  GUID changes since last export; expandable, with fixes in place.
- **Compliance:** per-check findings with % met, authority filter; e.g. proxies grouped by family type with a
  class picker; GFA not enclosed with zoom/step through; To BCF, CSV.
- **Issues:** BCF topics (Takeoff, CX Model Checker) with viewpoint, elements to fill, comments, status;
  Takeoff edits matched by IfcGUID with conflict detection (changed in Revit after export).
- **User Guide / Feedback.**

Run behaviour: decisions 4 and 5.

## 5. Project folder (shared with Radar)

```
ABCD.cxproject/
  project.json              BIM Lead · setup, files, roles, history
  models/<model-id>.json    Preflight (Revit file) or Radar (IFC) · one record per model
  log/<yyyy-mm>/<user>.jsonl  each person's Preflight · append-only events
  submissions/<date>-<gateway>.json  BIM Lead · snapshot at each submission
```

One writer per file, so OneDrive never merges two people's edits. Builds on Radar's existing format
(`rsp-ifcsg-project`, version 2: see `js/project.js` and `wiki/02-Project-File.md` in the Radar repo).
Model records are matched by content (IfcGUID fingerprint), not file name.

- `project.json`: project, submission (gateway, authorities), roles (new), files[] (new: name, Revit/IFC,
  checked in Preflight/Radar, part, IFCs it exports to, owner, weight auto/set, active/retired, history),
  checks, ruleset stamp.
- `models/*.json`: identity, source (new), checkedAt/by, readiness (new), checks[], findings[] (cap 3000),
  storeys, georef, notRequired[] (new: by IfcGUID + parameter, by, date, reason).
- `log/*.jsonl` events: session (active minutes, values written panel vs Revit, by category), run (kind,
  duration, readiness after), fix (action, elements, readiness before/after), issue (state changes),
  overhead (logging cost; target < 5 ms per event).
- `submissions/*.json`: submittedAt, gateway, files[] (IFC, Preflight %, Radar %), project %, open issues,
  wdRef (reference to the project's own WD record only).

Effectiveness measures: caught before export vs found after · escapes with no check · readiness at
submission · last-mile fixes · input effort · time to READY. Per-person timing is staff monitoring:
tell the team first.

Radar changes needed: read the folder layout as well as the single file; write a model record per IFC
with readiness; respect notRequired marks; adopt the same weighted readiness.

## 6. Build plan

- **P0 (done, branch `p0-csharp-skeleton`):** solution for 2024–2027, ribbon, dockable pane, `RevitQueue`,
  DocumentChanged tracking, reads levels + element count, sidebar shell, preview app, Core scoring +
  tests, build/deploy script, VS Code F5, CI. Not yet run inside Revit; first job is to load it in
  Revit 2025 and fix anything that only shows at run time (pane registration, pack-URI icons, net48 on 2024).
- **P1:** port the pyRevit checks from `lib/cxfd` into Core + Revit readers: parameters loaded/audit,
  IFC class mapping (read the export setup's category mapping), values completeness, levels &
  coordinates, rooms/spaces/GFA, export settings, will-it-export. Feed `ReadinessInputs`; show the score,
  verdict and Next step. Load the IFC-SG ruleset shared with Radar. Parity tests on ABCD BLK3: 320 of 338
  parameters missing, 226 IfcBuildingElementProxy without subtype, 17 GFA areas not enclosed, no level
  set as Building Story (pyRevit run took ~45 s for ~26k elements; aim faster).
- **P2:** Warnings and Compliance tabs with fixes in place; Inspect (SelectionChanged, Revit 2023+);
  automatic re-check of small change sets when idle (decision 5).
- **P3:** project folder (Project Setup dialog for BIM Leads, file list, split/merge, roles), project
  readiness and cross-file checks, logging, Issues (BCF, Takeoff edits), submission snapshots.
- Later: code signing, Radar changes, effectiveness dashboard.

## 7. Notes on the Revit versions

- 2024 runs .NET Framework 4.8; 2025/2026 run .NET 8 (Autodesk is moving later updates to .NET 10);
  2027 runs .NET 10 and adds add-in isolation. Test the 2025/2026 builds on updated Revit too.
- Revit API references come from `Nice3point.Revit.Api.*` (compile-time only, `ExcludeAssets=runtime`).
