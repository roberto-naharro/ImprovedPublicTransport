# IPT Essentials

A lean update of **Improved Public Transport 2** for Cities: Skylines 1.21.x, keeping only
the features that are still genuinely missing from the base game and the modern mod ecosystem.

**IPT Essentials** is a community-maintained fork of the original
[Improved Public Transport 2](https://steamcommunity.com/sharedfiles/filedetails/?id=424106600)
by [BloodyPenguin](https://github.com/bloodypenguin), updated to run on Cities: Skylines 1.21.x.

[Report a bug](https://github.com/roberto-naharro/ImprovedPublicTransport/issues) ·
[GitHub](https://github.com/roberto-naharro/ImprovedPublicTransport)

---

## Design philosophy

The Cities: Skylines mod ecosystem has excellent dedicated mods for vehicle assignment, depot
selection, stop selection, and unbunching. IPT Essentials is built to work *alongside* those
specialists — each mod doing its job, all fitting together cleanly.

We focus on the things no other mod provides together:

- **Precise vehicle count control** — set exactly how many vehicles run on a line, independent
  of budget, and switch between manual and budget-driven mode per line.
- **Per-line vehicle type selection** — choose which vehicle models can serve a given line.
  Opens a floating panel (from the line info panel) listing available and selected models with
  thumbnail previews. Mixed-fleet lines are preserved across load.
- **Granular statistics** — per-stop, per-vehicle, and per-line breakdowns (current week, last
  week, rolling average) that let you actually understand how your network is performing.

For everything else, the right tool already exists and is actively maintained. IPT Essentials
defers to it — and works better because of it.

---

## Requirements

- **[Harmony (Mod Dependency)](https://steamcommunity.com/sharedfiles/filedetails/?id=2040656402)** —
  subscribe and enable this first.

---

## Features

### Per-line vehicle type selection

Click **Select Vehicle Types** in the line info panel to open a floating selection panel.
The panel shows two lists side-by-side: selected vehicle models (left) and available models
for this line's service class (right), with thumbnail sprites and Steam Workshop badges.
Add or remove individual models, or add/remove all at once. A preview renderer shows the
selected/highlighted model in 3D.

The selection panel UI is based on code patterns from
[VehicleSelector](https://github.com/algernon-A/VehicleSelector/) by **algernon-A**
(icon-button middle section, hover tooltip interaction, and related UX flow), adapted here
for line-level vehicle model selection.

When vehicle types are configured for a line, `TransportLine.GetLineVehicle` returns a
random model from the set rather than the game's default. `TransportManager.CheckTransportLineVehicles`
is skipped for those lines, preventing the game from despawning non-default models in a
mixed-fleet line. The vanilla `PublicTransportLineVehicleSelector` is suppressed; IPT's
panel replaces it entirely.

**Bus lines** accept Level 1 and Level 2 bus assets interchangeably (matching the behaviour
of `ClassMatchesPatch` for depot compatibility).

Selection data is persisted to the save file (schema `v006`).

### Manual vehicle count

Each transit line has a toggle between **budget-driven** mode (vanilla behaviour) and
**manual** mode. In manual mode you set an exact target vehicle count; the mod intercepts
`TransportLine.SimulationStep` via a Harmony transpiler and replaces the game's
`CalculateTargetVehicleCount` call with its own version that returns the stored count.
Switching back to budget mode restores vanilla calculation instantly.

The target count persists across save/load (see [Persistence](#persistence)).

### Per-stop passenger statistics

Every transit stop shows:

- **Passengers In / Out** — boarding and alighting counts for the current week and last week,
  plus a rolling average calculated from the weekly history.
- **Custom stop name** — rename any stop from the stop info panel; the mod suggests names
  from nearby buildings automatically.

Stats are collected by Harmony prefix/postfix hooks on `LoadPassengers` and `UnloadPassengers`
across all nine vehicle AI types (bus, trolleybus, tram, train, plane, helicopter, blimp,
ferry, ship). The pre-hook captures the passenger count before the event; the post-hook
computes the delta and writes it to `CachedNodeData`.

Weekly boundaries are detected in the `SimulationStep` postfix: when
`(m_currentFrameIndex & 4095) >= 3840`, each stop on the line calls `StartNewWeek()`,
rolling the current week's totals into the historical average.

### Per-vehicle statistics

Clicking any active transit vehicle opens a panel showing:

- Passengers boarded / alighted at the last stop
- Total passengers this week and last week (rolling average)
- Earnings and distance traveled (current week / last week / average)

Data is written by the same `LoadPassengers` / `UnloadPassengers` hooks to `CachedVehicleData`.
Maintenance costs are charged in the `SimulationStep` postfix (the transpiler replaces the
game's bulk `FetchResource` call with a stub; the postfix re-bills using the game's own formula:
`totalVehicles × m_maintenanceCostPerVehicle + totalCapacity × m_maintenanceCostPerPassenger`,
where `totalCapacity` is the sum of `vehicleAI.GetPassengerCapacity(true)` across all vehicles
on the line including trailing cars).

### Line earnings and costs

The line info panel includes a stats table showing, for the current week, last week, and rolling
average:

- **Passengers** — total boarding count across all vehicles on the line.
- **Earnings** — gross fare revenue collected by vehicles on the line (before maintenance
  deduction).
- **Maintenance cost** — per-vehicle maintenance, computed from
  `TransportInfo.m_maintenanceCostPerVehicle` and `m_maintenanceCostPerPassenger` (the game's
  full formula: `totalVehicles × costPerVehicle + totalCapacity × costPerPassenger`). Shown in
  red as a negative value.
- **Cost per line** — the line's equal share of total transport-type expenses as reported by
  `EconomyManager.GetIncomeAndExpenses` (covering vehicles, depots, and any other infrastructure
  for that transport category), divided by the number of active lines of the same type. Shown in
  red as a negative value. Use this alongside Earnings to judge whether a line is profitable.

### Vehicles in this line

When the line info panel is open, two side panels appear to its right:

- **Vehicles in this line** — one row per vehicle currently serving the line. Each row displays the asset name plus a global sequential index (e.g. `City Bus #2`), or the player-given name if the vehicle has been renamed via the vehicle info panel. Hovering shows the vehicle's passenger capacity; right-clicking focuses the camera and opens the vehicle info panel. Left-clicking selects or deselects a row; Ctrl+A selects or deselects all.
- **Vehicles queued** — stacked below, showing vehicles that have spawned at a depot but not yet arrived at their first stop. A vehicle transitions to the active list automatically the moment it serves its first stop.

When one or more rows in the active panel are selected, the **Remove Vehicle** button removes those specific vehicles. If nothing is selected it falls back to removing the last active vehicle (existing behavior).

### Station stop list

Clicking a transit station building opens a panel listing all associated stops by name.
Each entry is clickable and opens the stop's detail panel.

### Line deletion tool

The mod options panel includes a bulk line deletion tool. Select a transport type (bus, tram,
metro, monorail, ferry, blimp, cable car, helicopter) and all lines of that type are removed.
Useful for cleaning up an entire transit type without hunting stops individually.

---

## Technical internals

### Harmony patching

All patches are applied manually at `OnLevelLoaded` and removed at `OnLevelUnloading`.
IPT Essentials does **not** use `harmony.PatchAll()` — every patch is registered explicitly
via `PatchUtil.Patch()`, which wraps `HarmonyInstance.Patch()` with typed `MethodDefinition`
descriptors. This makes the patch list deterministic and avoids scanning the entire assembly.

| Patch class | Target | Type | Purpose |
| --- | --- | --- | --- |
| `SimulationStepPatch` | `TransportLine.SimulationStep` | transpiler + prefix + postfix | Vehicle count override; weekly stat reset; per-vehicle maintenance billing |
| `LoadPassengersPatch` | `*AI.LoadPassengers` (9 types) | prefix + postfix | Record boarding counts per stop and per vehicle |
| `UnloadPassengersPatch` | `*AI.UnloadPassengers` (9 types) | prefix + postfix | Record alighting counts per stop and per vehicle; marks the vehicle as joined (`CachedVehicleData.MarkJoined`) on first stop arrival |
| `ReleaseNodePatch` | `NetManager.ReleaseNode` | postfix | Clear `CachedNodeData` entry when a stop is deleted |
| `ReleaseWaterSourcePatch` | `VehicleManager` | postfix | Clear `CachedVehicleData` entry when a vehicle is despawned; clears the join flag (`CachedVehicleData.MarkLeft`) |
| `ClassMatchesPatch` | `DepotAI.ClassMatches` | prefix | Depot accepts both Level1 and Level2 bus assets |
| `GetLineVehiclePatch` | `TransportLine.GetLineVehicle` | prefix | Returns a random model from the line's selected set (falls through to vanilla when no selection) |
| `CheckTransportLineVehiclesPatch` | `TransportManager.CheckTransportLineVehicles` | prefix | Skips vanilla vehicle-type enforcement for lines with a custom model set |
| `GetVehicleInfoPatch` | `PublicTransportLineVehicleSelector.GetVehicleInfo` | prefix | Suppresses vanilla per-line vehicle selector; IPT's panel replaces it |
| `OnMouseDownPatch` (stop) | `PublicTransportStopButton` | prefix | Open IPT stop detail panel on click |
| `OnMouseDownPatch` (vehicle) | `PublicTransportVehicleButton` | prefix | Open IPT vehicle detail panel on click |
| `UpdateStopButtonsPatch` | `PublicTransportWorldInfoPanel` | postfix | Refresh stop button visibility and labels |

### Transpiler: vehicle count and maintenance

`SimulationStepPatch.Transpile` walks the IL of `TransportLine.SimulationStep` and replaces
two call sites:

1. `TransportLine.CalculateTargetVehicleCount()` → `SimulationStepPatch.CalculateTargetVehicleCount(lineID)`:
   checks `LineData.BudgetControl`; if true, delegates to the original game calculation and
   stores the result; if false, returns the manually stored count.
2. `EconomyManager.FetchResource(Maintenance, amount, class)` → a stub that returns 0:
   the postfix then re-bills maintenance correctly on a per-vehicle basis, which enables
   accurate per-vehicle earnings tracking.

### Data model

#### `CachedTransportLineData` — persisted

Stored under save-game key `"ImprovedPublicTransport"` (preserving compatibility with original
IPT2 saves), schema version `v006`. Holds an array of 256 `LineData` structs — one per
`TransportManager` line slot:

| Field | Type | Meaning |
| --- | --- | --- |
| `TargetVehicleCount` | `int` | Manual vehicle target (ignored when `BudgetControl` is true) |
| `BudgetControl` | `bool` | `true` = let the game calculate; `false` = use `TargetVehicleCount` |
| `Prefabs` | `HashSet<string>` | Vehicle prefab names selected for this line (null = use default) |

On first load (no existing save data), `TargetVehicleCount` is initialised from the current
active vehicle count on each line, `BudgetControl` from the mod option default, and `Prefabs`
is null (vanilla vehicle selection).

Saves from schema `v005` and earlier load correctly: the fixed fields are read and `Prefabs`
stays null.

#### `CachedVehicleData` — runtime only

Sized to `VehicleManager.MAX_VEHICLE_COUNT` (16 384) by default. If the
[More Vehicles](https://steamcommunity.com/sharedfiles/filedetails/?id=1764208250) mod
(Workshop ID 1764208250) is active, the array is sized to `ushort.MaxValue + 1` (65 536).

Tracks per-vehicle: passengers boarded and alighted at the last stop, current-week and
last-week totals, rolling average, total earnings and distance traveled.

Also holds a `bool[]` join-state array (`HasJoined`) sized to `MaxVehicleCount`. A vehicle's
entry is `false` while it is traveling from the depot to its first stop, and flips to `true`
when `UnloadPassengers` fires for the first time (even with zero alighting passengers). It is
cleared back to `false` when the vehicle is released. This is used to split vehicles between
the **Vehicles in this line** and **Vehicles queued** side panels. All vehicles already active
at level-load are pre-marked as joined via `MarkAllExistingJoined()`.

#### `CachedNodeData` — runtime only

One entry per net node index. Tracks per-stop: current-week and last-week passenger in/out
counts, rolling average, custom stop name.

### Persistence

`SerializableDataExtension` implements `ISerializableDataExtension`. On save it fires
`EventSaveData`, which `CachedTransportLineData` subscribes to, serialising its array to a
flat byte stream using `BitConverter`. On load, the stream is read back and version-checked.
If the version string does not match `v005` (e.g. upgrading from an old save), the data is
discarded and re-initialised from the live game state.

---

## Companion mods

These mods handle features that IPT2 used to include. Each is a dedicated, actively maintained
tool that does its job better than a bundled solution can. IPT Essentials is designed to work
alongside all of them.

| Feature | Mod | Notes |
| --- | --- | --- |
| Depot assignment per line | [VehicleSelector](https://github.com/algernon-A/VehicleSelector/) by **algernon-A** | VS controls which depot a line draws from (building-level). IPT Essentials controls which vehicle models a line can spawn (line-level). The two are complementary and fully compatible. |
| Vehicle unbunching | [Public Transport Unstucker](https://github.com/Vectorial1024/PublicTransportUnstucker) by **Vectorial1024** | Purpose-built unbunching that integrates with TM:PE and handles edge cases a transit mod's side-feature never would. |
| Advanced stop selection | [Advanced Stop Selection](https://steamcommunity.com/sharedfiles/filedetails/?id=442167376) | Dedicated stop-selection logic maintained independently from any transit feature mod. |
| Elevated stop placement | [Elevated Stops Enabler](https://github.com/MacSergey/ElevatedStopsEnabler) by **MacSergey** | Unlocks stop placement on elevated road segments — a focused infrastructure tool. |

IPT Essentials is compatible with [Express Bus Services](https://github.com/Vectorial1024/ExpressBusServices)
by **Vectorial1024** — EBS was designed with IPT2 awareness and passenger stats remain accurate
in all EBS modes.

---

## Migrating from IPT2

If you are coming from the original Improved Public Transport 2, two compatibility notes apply:

**[ExpressBusServices-IPT2](https://github.com/Vectorial1024/ExpressBusServices-IPT2)** is no
longer needed and should be unsubscribed. That mod was a compatibility bridge between EBS and
IPT2's unbunching feature. IPT Essentials has removed unbunching entirely (now handled by
[Public Transport Unstucker](https://github.com/Vectorial1024/PublicTransportUnstucker)),
so EBS-IPT2 has nothing to bridge and will fail to load alongside IPT Essentials.

Manual vehicle counts and line settings from an existing IPT2 save are loaded automatically —
the save-data key and serialization format are backward-compatible up to schema `v005`. Vehicle
type selections made in the original IPT2 will not be loaded (different data structure), but
no data is lost: the fields are skipped during migration and all other settings transfer cleanly.

**VehicleSelector** users: IPT Essentials adds line-level vehicle type selection (which model
spawns per line), while VehicleSelector adds depot-level assignment (which depot a line draws
from). The two features operate at different layers and are compatible.

---

## Compatibility

**Compatible with Cities: Skylines 1.21.x.**

Works alongside: TM:PE, Express Bus Services, VehicleSelector, Public Transport Unstucker,
Advanced Stop Selection, Call Again, Transfer Manager CE, Carriage Number Selector,
Elevated Stops Enabler, CSL Show Commuter Destination, More Vehicles (65 536-vehicle array
is allocated automatically when More Vehicles is detected at load time).

---

## Credits

**[BloodyPenguin](https://github.com/bloodypenguin)** — original mod concept, architecture,
and all game logic. IPT Essentials would not exist without their work.

**[algernon-A](https://github.com/algernon-A)** — VehicleSelector, the recommended companion
for vehicle type and depot assignment.

**[Vectorial1024](https://github.com/Vectorial1024)** — Express Bus Services and Public
Transport Unstucker, both of which IPT Essentials is designed to work alongside.

**[MacSergey](https://github.com/MacSergey)** — Elevated Stops Enabler and Improved Stop
Selection, two infrastructure mods that complement IPT Essentials' stop management.

**roberto-naharro** — compatibility update, feature pruning, and maintenance of this fork.

---

## Development

### Prerequisites

- Linux (or WSL) with Mono / xbuild
- Cities: Skylines game files accessible (locally or via SMB share)

### Build and deploy

```bash
./mount-cities.sh      # mount Windows game share (one-time per session)
./deploy.sh            # debug build + copy to game Mods folder
./deploy.sh --release  # release build
```

### Publishing to Steam Workshop

```bash
./deploy.sh --release
./publish.sh "Fix: description of change"
```

### Updating the Workshop description

Edit `Workshop/description.txt` and push to `master`. The
`.github/workflows/workshop-update-description.yml` workflow triggers automatically and
uploads the new description to the Workshop page via steamcmd — no DLL rebuild needed.

### Release workflow

1. Use Conventional Commits (`feat:`, `fix:`, `chore:`, etc.) on every commit.
2. Release Please opens a Release PR automatically.
3. Before merging: run `./deploy.sh --release`, commit `dist/`.
4. Merge the Release PR → tag is created → Workshop deploy fires.
