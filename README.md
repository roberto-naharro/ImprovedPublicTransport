# IPT Essentials

A lean update of **Improved Public Transport 2** for Cities: Skylines 1.21.x, keeping only
the features that are still genuinely missing from the base game and the modern mod ecosystem.

**IPT Essentials** is a community-maintained fork of the original
[Improved Public Transport 2](https://github.com/bloodypenguin/ImprovedPublicTransport)
by [BloodyPenguin](https://github.com/bloodypenguin), updated to run on Cities: Skylines 1.21.x.

[Report a bug](https://github.com/roberto-naharro/ImprovedPublicTransport/issues) ·
[GitHub](https://github.com/roberto-naharro/ImprovedPublicTransport)

---

## Design philosophy

The Cities: Skylines mod ecosystem has excellent dedicated mods for vehicle stats, stop
placement, and advanced unbunching. IPT Essentials is built to work *alongside* those
specialists, each mod doing its job, all fitting together cleanly.

We focus on the line-management things no other mod provides together:

- **Precise vehicle count control.** Set exactly how many vehicles run on a line, independent
  of budget, and switch between manual and budget-driven mode per line.
- **Per-line vehicle type selection.** Choose which vehicle models can serve a given line.
  Opens a floating panel (from the line info panel) listing available and selected models with
  thumbnail previews. Mixed-fleet lines are preserved across load.
- **Depot selection per line.** Pin a line to a specific depot so its vehicles always spawn
  from the depot you choose, with a "go to depot" button to find it on the map.
- **Per-line ticket price.** Set a custom fare per line (free up to the type cap, in 0.10
  steps), with an optional happiness consequence so expensive fares carry a real downside.
- **Copy / paste line settings.** Duplicate a line's vehicle count and mode, colour, vehicle
  types, ticket price, and depot onto another line in two clicks.
- **Per-stop unbunching control.** Enable or disable the vanilla dwell wait at individual
  stops. Stop circles in the line panel are tinted red (unbunching on) or green (unbunching off)
  so the state of every stop is visible at a glance. [Express Bus Services](https://github.com/Vectorial1024/ExpressBusServices) reads
  these flags and applies its own rubberbanding where unbunching is enabled.
- **Granular statistics.** Per-stop, per-vehicle, and per-line breakdowns (current week, last
  week, rolling average) that let you actually understand how your network is performing.

For everything else (vehicle stats and colours, stop placement, advanced spacing), the right
tool already exists and is actively maintained. IPT Essentials defers to it, and works better
because of it.

---

## Requirements

- **[Harmony (Mod Dependency)](https://steamcommunity.com/sharedfiles/filedetails/?id=2040656402)**.
  Subscribe and enable this first.

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
random model from the set rather than the game's default, so a mixed-fleet line keeps
spawning the models you chose. The vanilla `PublicTransportLineVehicleSelector` is suppressed; IPT's
panel replaces it entirely.

**Bus lines** accept Level 1 and Level 2 bus assets interchangeably (matching the behaviour
of `ClassMatchesPatch` for depot compatibility).

Selection data is persisted to the save file (schema `v008`).

### Manual vehicle count

Each transit line has a toggle between **budget-driven** mode (vanilla behaviour) and
**manual** mode. In manual mode you set an exact target vehicle count; the mod intercepts
`TransportLine.SimulationStep` via a Harmony transpiler and replaces the game's
`CalculateTargetVehicleCount` call with its own version that returns the stored count.
Switching back to budget mode restores vanilla calculation instantly.

The target count persists across save/load (see [Persistence](#persistence)).

### Depot selection per line

Each line panel has a **depot dropdown** listing every depot that can serve the line's transport
type, plus an **Auto** entry (the default, vanilla nearest-depot behaviour). Pick a depot and the
line's vehicles always spawn from it; a **go to depot** button to the right of the dropdown moves
the camera to the chosen depot (shift-click to zoom in) so you can confirm which one it is when
several share a name.

The redirect is a prefix on `DepotAI.StartTransfer` that runs *before* the vehicle selector and
re-points the spawn to the pinned depot, with no extra spawn throttle. Depotless transport types
(metro, monorail, train) hide the selector. The pinned depot persists across save/load (schema
`v007`). If a pinned depot is later demolished or can no longer serve the line, the line falls back
to Auto automatically.

### Per-line ticket price

The line panel includes a **ticket-price slider** below the vehicle block, running from **free
(₡0) up to the transport type's cap in 0.10 increments**, with the current fare shown beside it and
a **Reset** button that restores the line's type default. The slider writes only the vanilla
per-line `TransportLine.m_ticketPrice` field (never the shared `TransportInfo` prefab), so it is
fully reversible and cannot corrupt other lines or types. Fare income flows automatically because
the vanilla fare AIs read this field.

**Happiness consequence (optional, default on).** Raising a line's fare slowly lowers the happiness
of the people who ride it (and lowering or freeing the fare raises it), as if their home tax had
changed. The effect is indirect and builds up over time, so a fare-funded transit network that
overcharges its regular riders pays for it in citizen happiness. It is implemented by nudging only
the **internal tax rate the game reads while computing a consequence** (citizen wellbeing in
`ResidentAI.UpdateWellbeing` and the "We pay too much tax!" building check in
`ResidentialBuildingAI.SimulationStepActive`), scaled by the real fare burden each building's
residents pay versus their income. The player's actual tax rate, the budget panel, the financial
window, and income collection are never touched. Toggle it off in the mod options for a pure income
lever (which also skips the per-building accounting). The per-line price and the customised flag
persist across save/load (schema `v008`).

This restores per-line fares that the per-type-only **Ticket Price Customizer** cannot do; TPC
still works as the per-type fallback and coexists (IPTE's per-line price overrides it where set).

### Copy / paste line settings

A **Copy** and **Paste** button at the bottom of the line panel duplicate a line's IPTE-owned
settings onto another line: **vehicle count and budget/manual mode, line colour, selected vehicle
types, ticket price, and depot**. Stop/unbunching configuration is intentionally not copied (it is
per-stop and cannot map cleanly across lines with different stops). Paste is disabled until you
have copied a line. On paste, a copied depot that does not serve the target line falls back to Auto,
and the colour is applied through the vanilla line-colour path.

### Per-stop passenger statistics

Every transit stop shows:

- **Passengers In / Out.** Boarding and alighting counts for the current week and last week,
  plus a rolling average calculated from the weekly history.
- **Custom stop name.** Rename any stop from the stop info panel; the mod suggests names
  from nearby buildings automatically.

Stats are collected by Harmony prefix/postfix hooks on `LoadPassengers` and `UnloadPassengers`
across all nine vehicle AI types (bus, trolleybus, tram, train, plane, helicopter, blimp,
ferry, ship). The pre-hook captures the passenger count before the event; the post-hook
computes the delta and writes it to `CachedNodeData`.

Weekly boundaries are detected in the `SimulationStep` postfix: when
`(m_currentFrameIndex & 4095) >= 3840`, each stop on the line calls `StartNewWeek()`,
rolling the current week's totals into the historical average.

### Per-stop unbunching toggle

Each transit stop has a toggle in its stop info panel to enable or disable unbunching at that
stop. When **enabled** (the default), the vanilla game dwell logic applies. Vehicles wait at the
stop long enough to space out from the vehicle ahead (`TransportLine.CanLeaveStop`). When
**disabled**, the vehicle leaves as soon as boarding is complete, without any forced wait, which
avoids blocking road traffic at intermediate stops.

An **Apply to all stops** button propagates the current stop's setting to every stop on the same
line via `CachedNodeData.SetUnbunchingForLine`.

Stop circles in the line info panel are color-coded to reflect each stop's state: **red** tint
when unbunching is enabled (vehicles wait/space out here), **green** tint when disabled (vehicles
depart immediately). Hovering a circle also shows "Unbunching enabled" or "Unbunching disabled"
in the tooltip.

**Without EBS**, the `CanLeaveStopPatch` prefix on `TransportLine.CanLeaveStop` is the sole
control: Unbunching=false forces the stop to return `true` immediately (skip vanilla dwell),
Unbunching=true lets vanilla dwell run normally. This is the original IPT2 per-stop behavior.

**When [Express Bus Services](https://github.com/Vectorial1024/ExpressBusServices) is active**,
IPTE additionally patches `DepartureChecker.StopIsConsideredAsTerminus` (the single method EBS
consults for both its departure-timing and stop-skipping decisions):

- `Unbunching=true` → terminus → EBS applies rubberbanding until vehicles are spaced, and the
  stop is never skipped even if nobody is waiting.
- `Unbunching=false` → non-terminus → EBS instant-departs once boarding is done, and will skip
  the stop entirely if no passengers are waiting or alighting.

EBS registers its own patches at `Priority.LowerThanNormal` (300) to give way to IPT2; IPTE
uses `Priority.Normal` (400). **Do not install ExpressBusServices-IPT2**. It conflicts with
this direct integration.

Per-stop unbunching state is persisted to the save file (see [Persistence](#persistence)). All
stops default to enabled (unbunching active) on first load or missing data.

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

- **Passengers.** Total boarding count across all vehicles on the line.
- **Earnings.** Gross fare revenue collected by vehicles on the line (before maintenance
  deduction).
- **Maintenance cost.** Per-vehicle maintenance, computed from
  `TransportInfo.m_maintenanceCostPerVehicle` and `m_maintenanceCostPerPassenger` (the game's
  full formula: `totalVehicles × costPerVehicle + totalCapacity × costPerPassenger`). Shown in
  red as a negative value.
- **Cost per line.** The line's equal share of total transport-type expenses as reported by
  `EconomyManager.GetIncomeAndExpenses` (covering vehicles, depots, and any other infrastructure
  for that transport category), divided by the number of active lines of the same type. Shown in
  red as a negative value. Use this alongside Earnings to judge whether a line is profitable.

### Vehicles in this line

When the line info panel is open, two side panels appear to its right:

- **Vehicles in this line.** One row per vehicle currently serving the line. Each row displays the asset name plus a global sequential index (e.g. `City Bus #2`), or the player-given name if the vehicle has been renamed via the vehicle info panel. Hovering shows the vehicle's passenger capacity; right-clicking focuses the camera and opens the vehicle info panel. Left-clicking selects or deselects a row; Ctrl+A selects or deselects all.
- **Vehicles queued.** Stacked below, showing vehicles that have spawned at a depot but not yet arrived at their first stop. A vehicle transitions to the active list automatically the moment it serves its first stop.

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
IPT Essentials does **not** use `harmony.PatchAll()`. Every patch is registered explicitly
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
| `StartTransferPatch` | `DepotAI.StartTransfer` | prefix | Re-point a line's spawn to its pinned depot (before the vehicle selector). Stands down when TLM is present |
| `EnterVehiclePatch` | `HumanAI.EnterVehicle` | postfix | Record each rider's fare premium (paid − type default) against their home building, for the happiness consequence |
| `GetTaxRatePatch` | `EconomyManager.GetTaxRate` (leaf overload) | postfix | Add the fare-driven rate bump to the internal tax rate, but only inside a consequence window |
| `UpdateWellbeingPatch` | `ResidentAI.UpdateWellbeing` | prefix + postfix | Open/close the consequence window so vanilla derives wellbeing from the bumped rate |
| `SimulationStepActivePatch` | `ResidentialBuildingAI.SimulationStepActive` | prefix + postfix | Same window for the "taxes too high" building-problem / happiness check (income-safe; that read drives only the tax-problem timer) |
| `GetLineVehiclePatch` | `TransportLine.GetLineVehicle` | prefix | Returns a random model from the line's selected set (falls through to vanilla when no selection) |
| `GetVehicleInfoPatch` | `PublicTransportLineVehicleSelector.GetVehicleInfo` | prefix | Suppresses vanilla per-line vehicle selector; IPT's panel replaces it |
| `CanLeaveStopPatch` | `TransportLine.CanLeaveStop` | prefix | Returns `true` immediately (skips vanilla dwell) when unbunching is disabled at that stop. No-op when EBS is detected. |
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

#### `CachedTransportLineData` (persisted)

Stored under save-game key `"ImprovedPublicTransport"` (preserving compatibility with original
IPT2 saves), schema version `v008`. Holds an array of 256 `LineData` structs (one per
`TransportManager` line slot):

| Field | Type | Meaning |
| --- | --- | --- |
| `TargetVehicleCount` | `int` | Manual vehicle target (ignored when `BudgetControl` is true) |
| `BudgetControl` | `bool` | `true` = let the game calculate; `false` = use `TargetVehicleCount` |
| `Prefabs` | `HashSet<string>` | Vehicle prefab names selected for this line (null = use default) |
| `Depot` | `ushort` | Pinned depot building ID (0 = Auto / vanilla nearest depot). Added in `v007` |
| `TicketPrice` | `ushort` | Custom per-line fare in vanilla `m_ticketPrice` units. Added in `v008` |
| `TicketPriceCustomised` | `bool` | `true` once the player sets a custom fare; gates enforcement. Added in `v008` |

On first load (no existing save data), `TargetVehicleCount` is initialised from the current
active vehicle count on each line, `BudgetControl` from the mod option default, `Prefabs`
is null (vanilla vehicle selection), `Depot` is 0 (Auto), and the ticket-price fields are default
(not customised).

Older saves load correctly through the version-guarded reader: `v005` and earlier read the fixed
fields with `Prefabs` null; `v006` adds `Depot`; `v007` adds the ticket-price fields. `SetLineDefaults`
resets every field, including the new ones.

#### `CachedVehicleData` (runtime only)

Sized to `VehicleManager.MAX_VEHICLE_COUNT` (16 384) by default; the size is read from the live `VehicleManager` vehicle buffer at load time. If the
[More Vehicles](https://steamcommunity.com/sharedfiles/filedetails/?id=1764208250) mod
raises that limit, the larger buffer size is detected automatically (no hard-coded mod ID) and the array is sized to match (65 536).

Tracks per-vehicle: passengers boarded and alighted at the last stop, current-week and
last-week totals, rolling average, total earnings and distance traveled.

Also holds a `bool[]` join-state array (`HasJoined`) sized to `MaxVehicleCount`. A vehicle's
entry is `false` while it is traveling from the depot to its first stop, and flips to `true`
when `UnloadPassengers` fires for the first time (even with zero alighting passengers). It is
cleared back to `false` when the vehicle is released. This is used to split vehicles between
the **Vehicles in this line** and **Vehicles queued** side panels. All vehicles already active
at level-load are pre-marked as joined via `MarkAllExistingJoined()`.

#### `CachedNodeData` (persisted)

Stored under save-game key `"IPT_NodeData"`, schema version `v005`. One entry per net node
index. Tracks per-stop:

| Field | Type | Meaning |
| --- | --- | --- |
| `PassengersIn` / `PassengersOut` | `int` | Boarding and alighting counts for the current week |
| `LastWeekPassengersIn` / `LastWeekPassengersOut` | `int` | Previous week totals |
| `PassengerInData` / `PassengerOutData` | `float[]` | Rolling history used to compute the weekly average |
| `Unbunching` | `bool` | `true` = vanilla dwell applies; `false` = vehicle leaves immediately without forced wait |

Nodes where all fields are at their default (`PassengersTotal == 0`, no history data, and
`Unbunching == true`) are omitted from the save file (`NodeData.IsEmpty`), keeping saves compact.

Saves from schema versions before `v005` load correctly: passenger data is read and `Unbunching`
defaults to `true` for all stops. The old `v003` legacy unbunching bool is read and discarded.

### Persistence

`SerializableDataExtension` implements `ISerializableDataExtension`. On save it fires
`EventSaveData`, which both `CachedTransportLineData` and `CachedNodeData` subscribe to,
each serialising its array to a flat byte stream using `BitConverter`.

**`CachedTransportLineData`**, save key `"ImprovedPublicTransport"`, schema `v008` (adds `Depot`
in `v007`, then `TicketPrice` + `TicketPriceCustomised` in `v008`). On load, the version string is
checked; older versions are upgraded field-by-field and unknown versions are discarded and
re-initialised from the live game state.

**`CachedNodeData`**, save key `"IPT_NodeData"`, schema `v005`. Nodes where all fields are at
their default are omitted (`NodeData.IsEmpty`). On load, older schema versions are handled:
`v003` reads and discards a legacy unbunching bool; `v004` reads only passenger data; `v005`
reads passenger data plus the per-stop `Unbunching` bool. Missing data defaults all stops to
unbunching enabled.

---

## Companion mods

These mods handle things IPT Essentials deliberately does not. Each is a dedicated, actively
maintained tool that does its job better than a bundled solution can, and IPTE is designed to work
alongside all of them. (Links point to each mod's source repository.)

| Feature | Mod | Notes |
| --- | --- | --- |
| Vehicle stats and colours | [Advanced Vehicle Options](https://github.com/CityGecko/CS-AdvancedVehicleOptions) by **CityGecko** | Capacity, maintenance, speed, acceleration and colour per vehicle asset. IPTE never writes those fields, so the two do not conflict. |
| Enhanced unbunching timing | [Express Bus Services](https://github.com/Vectorial1024/ExpressBusServices) by **Vectorial1024** | IPTE patches EBS's `DepartureChecker.StopIsConsideredAsTerminus` directly: Unbunching=true → EBS rubberbands and won't skip the stop; Unbunching=false → EBS instant-departs and may skip the stop if nobody is waiting. Works natively. **Do not install ExpressBusServices-IPT2** alongside this mod. |
| Stuck citizen fix | [Public Transport Unstucker](https://github.com/Vectorial1024/PublicTransportUnstucker) by **Vectorial1024** | Fixes the "Citizen Runaway Problem" (rogue passengers flagged as boarding who never actually board, preventing vehicles from departing). Completely unrelated to vehicle spacing; compatible and complementary. |
| Commuter destination routing fix | [Commuter Destination (Unofficial Bugfix)](https://github.com/Jameskmonger/CSL-ShowCommuterDestination) | Fixes vanilla commuter/passenger destination routing on transit. Orthogonal bugfix; pairs cleanly with IPTE. |
| Advanced stop selection | [Advanced Stop Selection Revisited](https://github.com/MacSergey/ImprovedStopSelection) by **MacSergey** | Dedicated stop-platform selection logic, maintained independently from any transit feature mod. |
| Elevated stop placement | [Elevated Stops Enabler](https://github.com/MacSergey/ElevatedStopsEnabler) by **MacSergey** | Unlocks stop placement on elevated road segments, a focused infrastructure tool. |
| Alternate vehicle-type control | [VehicleSelector](https://github.com/algernon-A/VehicleSelector) by **algernon-A** | Building-level control of which vehicle models a depot spawns. Optional now that IPTE selects vehicle types per line, but still compatible if you prefer the building-level workflow. |
| Per-type ticket price | [Ticket Price Customizer](https://github.com/bloodypenguin/Skylines-TicketPriceCustomizer) by **BloodyPenguin** | Sets fares per transport type. Coexists with IPTE's per-line price, which overrides it where set; TPC stays the per-type fallback. |

### Incompatible mods

Do not run these alongside IPT Essentials. They are rival full-suite line managers that replace the
public-transport line panel and take over vehicle spawning, the same job IPTE does. Enable only one.

- [Transport Lines Manager](https://github.com/t1a2l/TransportLinesManager) (TLM). IPTE detects TLM
  at load, logs an incompatibility warning, and stands its depot redirect down to reduce conflicts,
  but the line panel and spawning will still fight. Remove one of the two.
- [Improved Public Transport 3](https://github.com/TheMadisonian/ImprovedPublicTransport3) (IPT3).
  A different continuation of IPT2 with the same scope as IPTE; pick one.

---

## Migrating from IPT2

If you are coming from the original Improved Public Transport 2, two compatibility notes apply:

**[ExpressBusServices-IPT2](https://github.com/Vectorial1024/ExpressBusServices-IPT2) must be
unsubscribed.** IPT Essentials integrates with EBS directly by patching
`DepartureChecker.StopIsConsideredAsTerminus`. EBS_IPT2 patches the same code paths and will
conflict, producing double departure-timing calls and unpredictable vehicle behavior. Unsubscribe
EBS_IPT2 before using IPT Essentials with Express Bus Services.

Manual vehicle counts and line settings from an existing IPT2 save are loaded automatically.
The save-data key and serialization format are backward-compatible up to schema `v005`. Vehicle
type selections made in the original IPT2 will not be loaded (different data structure), but
no data is lost: the fields are skipped during migration and all other settings transfer cleanly.

**Depot and vehicle-type selection are now built in.** IPT Essentials restores both per-line depot
pinning and per-line vehicle-type selection itself, so you no longer need a separate mod for them.
VehicleSelector remains compatible if you prefer its building-level workflow, but it is now optional.

---

## Compatibility

**Compatible with Cities: Skylines 1.21.x.**

Works alongside: TM:PE, Advanced Vehicle Options, Express Bus Services, VehicleSelector, Public
Transport Unstucker, Advanced Stop Selection Revisited, Call Again, Transfer Manager CE, Carriage
Number Selector, Elevated Stops Enabler, Commuter Destination (Unofficial Bugfix), Ticket Price
Customizer, More Vehicles (65 536-vehicle array is allocated automatically when an extended vehicle
limit is detected at load time).

**Incompatible** with Transport Lines Manager (TLM) and Improved Public Transport 3 (IPT3): both are
rival full-suite line managers that replace the line panel and take over spawning. Enable only one.

---

## Credits

**[BloodyPenguin](https://github.com/bloodypenguin)**. Original mod concept, architecture,
and all game logic. IPT Essentials would not exist without their work.

**[algernon-A](https://github.com/algernon-A)**. VehicleSelector, the building-level companion
for depot vehicle assignment, and the UI patterns the vehicle-type panel is based on.

**[CityGecko](https://github.com/CityGecko)**. Advanced Vehicle Options, the recommended companion
for per-vehicle stats and colours.

**[Vectorial1024](https://github.com/Vectorial1024)**. Express Bus Services and Public
Transport Unstucker, both of which IPT Essentials is designed to work alongside.

**[MacSergey](https://github.com/MacSergey)**. Elevated Stops Enabler and Advanced Stop Selection
Revisited, two infrastructure mods that complement IPT Essentials' stop management.

**roberto-naharro**. Compatibility update, feature pruning, and maintenance of this fork.

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
uploads the new description to the Workshop page via steamcmd. No DLL rebuild needed.

### Release workflow

1. Use Conventional Commits (`feat:`, `fix:`, `chore:`, etc.) on every commit.
2. Release Please opens a Release PR automatically.
3. Before merging: run `./deploy.sh --release`, commit `dist/`.
4. Merge the Release PR → tag is created → Workshop deploy fires.
