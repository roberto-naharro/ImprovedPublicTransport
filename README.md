# IPT Essentials

A lean update of **Improved Public Transport 2** for Cities: Skylines 1.21.x, keeping only
the features that are still genuinely missing from the base game and the modern mod ecosystem.

**IPT Essentials** is a community-maintained fork of the original
[Improved Public Transport 2](https://steamcommunity.com/sharedfiles/filedetails/?id=424106600)
by [BloodyPenguin](https://github.com/bloodypenguin), updated to run on Cities: Skylines 1.21.x.

[Report a bug](https://github.com/roberto-naharro/ImprovedPublicTransport/issues) ·
[GitHub](https://github.com/roberto-naharro/ImprovedPublicTransport)

---

## Requirements

- **[Harmony (Mod Dependency)](https://steamcommunity.com/sharedfiles/filedetails/?id=2040656402)** —
  subscribe and enable this first.

---

## Features

- **Manual vehicle count** — set exactly how many vehicles run on a line, bypassing the
  budget-based calculation. Toggle between manual and budget-driven mode per line at any time.
- **Per-stop passenger stats** — each transit stop shows Passengers In / Out for the current
  week, last week, and a rolling average. Rename stops and browse suggested names from nearby
  buildings.
- **Per-vehicle stats** — click any active transit vehicle to see its passengers, earnings,
  and distance traveled (current week / last week / average), plus the last-stop boarding
  and alighting count.
- **Station stop list** — clicking a transit station building shows all associated stops,
  each clickable to open its stop panel.
- **Line deletion tool** — bulk-delete lines by type (bus, tram, metro, …) from the mod
  options panel.

---

## Companion mods

Some features from the original IPT2 are now better handled by dedicated mods.
We recommend using these alongside IPT Essentials:

| Feature | Mod |
| --- | --- |
| Custom vehicle type assignment per line | [VehicleSelector](https://github.com/algernon-A/VehicleSelector/) by algernon-A |
| Depot assignment per line | [VehicleSelector](https://github.com/algernon-A/VehicleSelector/) by algernon-A |
| Vehicle unbunching | [Public Transport Unstucker](https://github.com/Vectorial1024/PublicTransportUnstucker) by Vectorial1024 |

IPT Essentials is compatible with [Express Bus Services](https://github.com/Vectorial1024/ExpressBusServices)
— EBS was designed with IPT2 awareness and passenger stats remain accurate in all EBS modes.

---

## Mods no longer needed

**[ExpressBusServices-IPT2](https://github.com/Vectorial1024/ExpressBusServices-IPT2)** is not
needed and should be unsubscribed when using IPT Essentials. This mod was a compatibility bridge
between EBS and IPT2's unbunching feature. IPT Essentials has removed unbunching entirely (it is
now handled by [Public Transport Unstucker](https://github.com/Vectorial1024/PublicTransportUnstucker)),
so EBS-IPT2 has nothing to bridge and will fail to load alongside IPT Essentials.

---

## Compatibility

**Compatible with Cities: Skylines 1.21.x.**

Works alongside: TM:PE, Express Bus Services, VehicleSelector, Public Transport Unstucker,
Advanced Stop Selection, Call Again, Transfer Manager CE, Carriage Number Selector,
Elevated Stops Enabler, CSL Show Commuter Destination.

---

## Credits

**BloodyPenguin** — original mod concept, architecture, and all game logic.
IPT Essentials would not exist without their work.

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

### Release workflow

1. Use Conventional Commits (`feat:`, `fix:`, `chore:`, etc.) on every commit.
2. Release Please opens a Release PR automatically.
3. Before merging: run `./deploy.sh --release`, commit `dist/`.
4. Merge the Release PR → tag is created → Workshop deploy fires.
