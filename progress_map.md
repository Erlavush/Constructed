# Constructed Progress Map

Concise source of truth for project progress. `AGENTS.md` holds standing rules, reference versions, command guidance, approval rules, and private asset rules. Keep this file short enough for agents to read every turn.

## Current Status

- Phase: Phase 1 - Core Foundations.
- Latest completed step: Step 1.11 - demo content catalog and one-chunk flat surface bootstrap.
- Latest verification: Unity batchmode was blocked on 2026-05-06 because another Unity instance had this project open. Fallback compile passed for `Constructed.Core`, `Constructed.Minecraft`, `Constructed.Create`, and `Constructed.Tests.EditMode`.
- Next proposed step: Step 1.12 - minimal Unity presentation for the flat surface and placeholder vertical-slice blocks.
- Approval state: wait for user confirmation before starting the next gameplay/subsystem step.

## Completed Steps

| Step | Status | What Exists Now | Verification / Checkpoint |
| --- | --- | --- | --- |
| Phase 0 | Complete | Official Create reference checkout confirmed at `References/Create-mc1.21.1-dev`; architecture and project rules captured in `AGENTS.md`. | Reference versions and rules in `AGENTS.md`. |
| 1.1 Core primitives | Complete | `ResourceLocation`, `Direction`, `Axis`, `AxisDirection`, `BlockPos`, `SimulationClock`. | Included in checkpoint `c3827c9`. Early batchmode test XML was blocked by editor state. |
| 1.2 Registries and tags | Complete | `Registry<T>`, `RegistryEntry<T>`, `TagKey<T>`, `TagCollection<T>`, duplicate/frozen checks, typed tag membership. | Included in checkpoint `c3827c9`; compile/test-source checks passed. |
| 1.3 Block definitions and states | Complete | `Constructed.Minecraft` assembly, `StateProperty<T>`, `BlockDefinition`, immutable `BlockState`, deterministic `SerializedBlockState`. | Included in checkpoint `c3827c9`; compile/test-source checks passed. |
| 1.4 In-memory block world | Complete | `BlockWorld` stores non-air block states by `BlockPos`, explicit air fallback, set/remove/clear, stable stored-block enumeration. | Checkpoint `63acdfc`. |
| 1.5 Lifecycle and neighbor updates | Complete | `IBlockLifecycle`, placement/removal/replacement callbacks, six-direction neighbor update callbacks. | Checkpoint `22cd4c2`. |
| 1.6 Scheduled block ticks | Complete | Deterministic world tick time, scheduled block tick queue, priority ordering, duplicate guard by position plus block id. | Reflection/editor verification passed 51/51; checkpoint `30a3e2f`. |
| 1.7 Block entity foundation | Complete | `BlockEntityType`, `BlockEntity`, typed behaviors, create/tick/lazy-tick/state-change/neighbor/unload/destroy lifecycle. | Unity EditMode passed 59/59; checkpoint `d1a48e5`. |
| 1.8 Block entity serialization | Complete | `BlockEntityData`, behavior read/write hooks, `SerializedBlockWorld`, deterministic world snapshot serialize/load. | Unity EditMode passed 63/63; checkpoint `97a29f7`. |
| 1.9 Item definitions and stacks | Complete | `ItemDefinition`, immutable `ItemStack`, `SerializedItemStack`, empty stack, count validation, split/merge helpers, registry-based restore. | Unity EditMode passed 73/73; checkpoint `f50c779`. |
| 1.10 Inventories and Item Vault | Complete | `InventoryContainer`, `SerializedInventorySlot`, `Constructed.Create` assembly, single-block `ItemVaultBlockEntity`, `ItemVaultBlock` definition/factory, 20-slot vault storage, slot serialization through item registries. | Unity EditMode passed 83/83; checkpoint `031af49`. |
| 1.11 Demo catalog and flat chunk | Complete | `DemoContentCatalog` registers fixed first-slice ids; `DemoVerticalSliceBootstrap` creates a 16x16 flat surface and deterministic placeholder layout for motor, shafts, belt, creative crate, brass funnel, and Item Vault. | Unity blocked by open editor; fallback compile passed; checkpoint pending this turn. |

## Current Boundaries

- Simulation code should stay in plain C# services/data; Unity objects remain presentation/tooling until a presentation step is confirmed.
- Use official Minecraft/NeoForge/Create source in this repo only for the smallest relevant reference set before implementing confirmed gameplay behavior.
- Do not implement from assumptions, previous prototypes, or copied Java source.
- Do not commit copied Create assets; private copied assets belong only under `Assets/PrivateTemp/Create`.
- Keep verification and checkpointing lightweight: one relevant Unity run after code changes, concise result recording, one implementation checkpoint when requested by the step workflow.

## Reference Notes To Preserve

- Scheduled ticks: Create often guards `scheduleTick` with `hasScheduledTick(pos, block)` and may pass priorities; the Unity queue already models duplicate checks and stable priority ordering.
- Block entities: Create `SmartBlockEntity` initializes on first tick, runs an immediate lazy tick, and lets attached behaviors participate in tick/lazy-tick/read/write/lifecycle callbacks; the Unity foundation mirrors that shape.
- Serialization: behavior data currently shares the parent block entity payload as deterministic string key/value data; no JSON/NBT/file save format is chosen yet.
- Items: Create registers items centrally in `AllItems.java`; most use default stack size, while selected items use `stacksTo(1)` or `stacksTo(16)`. Unity item stacks are intentionally immutable until inventories/transport need mutation boundaries.
- Item Vault: Create's per-block vault capacity defaults to 20 stacks and full vaults form controller-based multiblocks. Unity currently implements only one block of Item Vault storage; multiblock connectivity, manual restrictions, capabilities, and rendering are deferred.
- First vertical slice ids now match Create registrations: `create:creative_motor`, `create:shaft`, `create:belt`, `create:creative_crate`, `create:brass_funnel`, `create:item_vault`, and item `create:andesite_alloy`.

## Not Started Yet

- Item entities, dropped item simulation, item components, durability, recipes, JSON/NBT save files, data import, full chunk streaming, rendering, scenes, input, networking, kinetics, belts, funnels, logistics, processing machines, Item Vault multiblocks, contraptions, trains, fluids, worldgen, tutorials, and multiplayer.

## Recent Maintenance

- 2026-05-06: Compacted this progress map to remove repeated file lists and old verbose verification details without dropping current state, step outcomes, checkpoints, or reference findings. Verification: documentation-only review; Unity tests not run.
- 2026-05-06: Updated `AGENTS.md` for faster low-overhead verification/checkpointing: no Unity tests for docs-only work, no full XML/log reads after a clean test exit, no duplicate progress-map-only commits, and no docs-only checkpoint unless requested.
- 2026-05-04: Cleaned `AGENTS.md` shell and Unity command guidance, including the local Git `sh.exe` fallback and removing `-quit` from Unity `-runTests`.

## Next Step

Discuss and confirm Step 1.12 before implementation: minimal Unity presentation for the flat surface and placeholder vertical-slice blocks. Target: render the bootstrapped one-chunk layout with simple meshes/materials and a camera for inspection. Defer kinetics, belt transport behavior, funnel transfer behavior, recipes, networking, and full chunk streaming.
