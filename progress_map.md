# Constructed Progress Map

This file tracks implementation phases, approved steps, completed work, and the current project state. Future agents must update it at the end of every confirmed step or feature.

## Current Status

Current phase: Phase 1 - Core Foundations
Current step: Step 1.6 - Scheduled block ticks
Status: Complete; waiting for user confirmation before Step 1.7

## Recent Maintenance

- 2026-05-04 - Cleaned `AGENTS.md` to remove stale blank-project wording, shorten broad Create source-study instructions, replace Bash-first command guidance with active-shell guidance, document the local Git `sh.exe` failure fallback, and prefer the Windows `Unity.com` batchmode command. No runtime behavior changed. Verification: documentation-only review; Unity tests not run. Git checkpoint: `e453a43` (`Clean agent guidance`) pushed to `origin/main`.

## GitHub Checkpoints

- `c3827c9` - `Checkpoint core foundations through step 1.3` pushed to `origin/main` at `https://github.com/Erlavush/Constructed.git`.
- `63acdfc` - `Step 1.4 minimal block world storage` pushed to `origin/main` at `https://github.com/Erlavush/Constructed.git`.
- `b155c44` - `Prefer Git Bash in agent instructions` pushed to `origin/main` at `https://github.com/Erlavush/Constructed.git`.
- `ebc0d50` - `Remove hard-coded Bash path guidance` pushed to `origin/main` at `https://github.com/Erlavush/Constructed.git`.
- `22cd4c2` - `Step 1.5 block lifecycle and neighbor updates` pushed to `origin/main` at `https://github.com/Erlavush/Constructed.git`.
- `30a3e2f` - `Step 1.6 scheduled block ticks` pushed to `origin/main` at `https://github.com/Erlavush/Constructed.git`.

## Phase 0 - Source Research and Project Rules

Status: Complete

Completed work:

- Read the project rules and confirmed this is a blank Unity 6 URP project.
- Verified the official Create reference checkout at `References/Create-mc1.21.1-dev`.
- Confirmed source versions from `gradle.properties`: Create 6.0.11, Minecraft 1.21.1, NeoForge 21.1.219, Java 21.
- Studied Create architecture directly from source entry points including `Create.java`, `CreateClient.java`, `AllBlocks.java`, `AllBlockEntityTypes.java`, `AllRecipeTypes.java`, `RotationPropagator.java`, `KineticNetwork.java`, belt source files, config, events, datagen, rendering, and assets.
- Expanded `AGENTS.md` with researched architecture notes, Unity implementation strategy, private asset rules, testing expectations, step-by-step approval rule, and this progress map rule.
- Added `AGENTS.md` verification guidance requiring Unity Test Framework or batchmode attempts after code changes, with a Unity-bundled compiler fallback only when full test execution is blocked.
- Added `AGENTS.md` GitHub checkpoint guidance requiring each completed step to be committed and pushed to `https://github.com/Erlavush/Constructed.git` after verification and progress-map updates.
- Revised `AGENTS.md` shell guidance to use the active Bash terminal without hard-coded `bash.exe` paths.

## Phase 1 - Core Foundations

Goal: Build the minimal Minecraft-like plain C# foundation needed before any gameplay machines or showcase scenes.

### Step 1.1 - Core Primitives

Status: Complete

Approved scope:

- Add `ResourceLocation` style ids.
- Add `Direction`, `Axis`, and direction helper behavior.
- Add `BlockPos` integer grid positions and deterministic offsets.
- Add a minimal deterministic tick clock.
- Add focused EditMode tests.

Implemented files:

- `Assets/Scripts/Constructed.Core/Constructed.Core.asmdef`
- `Assets/Scripts/Constructed.Core/Axis.cs`
- `Assets/Scripts/Constructed.Core/AxisDirection.cs`
- `Assets/Scripts/Constructed.Core/Direction.cs`
- `Assets/Scripts/Constructed.Core/DirectionExtensions.cs`
- `Assets/Scripts/Constructed.Core/BlockPos.cs`
- `Assets/Scripts/Constructed.Core/ResourceLocation.cs`
- `Assets/Scripts/Constructed.Core/SimulationClock.cs`
- `Assets/Tests/EditMode/Constructed.Tests.EditMode.asmdef`
- `Assets/Tests/EditMode/BlockPosTests.cs`
- `Assets/Tests/EditMode/DirectionTests.cs`
- `Assets/Tests/EditMode/ResourceLocationTests.cs`
- `Assets/Tests/EditMode/SimulationClockTests.cs`

Behavior added:

- `ResourceLocation` supports `namespace:path` ids, default namespace parsing, validation, equality, and stable string formatting.
- `Direction`, `Axis`, and `AxisDirection` model the Minecraft-style six-direction grid with opposite, axis, axis direction, horizontal rotation, normals, and step offsets.
- `BlockPos` provides immutable integer voxel positions with offset and directional movement helpers.
- `SimulationClock` provides a deterministic integer tick counter for later server-authoritative simulation work.

Verification:

- Added focused EditMode tests for resource ids, grid positions, directions, and tick clock behavior.
- Runtime core compiled successfully using Unity 6000.4.5f1's bundled C# compiler.
- Unity batchmode EditMode execution did not produce test results while the project was already open in the Unity editor. Re-run the Unity Test Runner after closing the editor or from inside the open editor.

Out of scope:

- No gameplay machines.
- No block/item registries yet.
- No world grid storage yet.
- No Unity scenes or rendering.

### Step 1.2 - Registry and Tag Foundations

Status: Complete

Approved scope:

- Add a minimal generic registry keyed by `ResourceLocation`.
- Add stable registry entries and duplicate/frozen registration checks.
- Add typed tag keys and tag membership storage by id.
- Add focused EditMode tests.

Implemented files:

- `Assets/Scripts/Constructed.Core/RegistryEntry.cs`
- `Assets/Scripts/Constructed.Core/Registry.cs`
- `Assets/Scripts/Constructed.Core/TagKey.cs`
- `Assets/Scripts/Constructed.Core/TagCollection.cs`
- `Assets/Tests/EditMode/RegistryTests.cs`
- `Assets/Tests/EditMode/TagKeyTests.cs`
- `Assets/Tests/EditMode/TagCollectionTests.cs`

Behavior added:

- `Registry<T>` stores values by `ResourceLocation`, assigns stable insertion indices, supports lookup by id and value, rejects duplicate ids/values, rejects uninitialized ids, and can be frozen after bootstrap.
- `RegistryEntry<T>` records id, index, and value as a stable entry snapshot.
- `TagKey<T>` identifies a tag by both registry id and tag id so block/item/recipe tags cannot be accidentally mixed.
- `TagCollection<T>` stores tag membership by resource id, preserves insertion order, deduplicates replacements, resolves registered values, and rejects tags from the wrong registry.

Verification:

- Added focused EditMode tests for registry registration, lookup, duplicate rejection, freeze behavior, tag-key equality, tag membership, tag replacement, and wrong-registry rejection.
- Runtime core compiled successfully using Unity 6000.4.5f1's bundled C# compiler.
- EditMode test source compiled successfully against the runtime compile-check assembly and Unity's bundled NUnit assembly.
- Full Unity Test Runner execution was not run because active Unity processes are already attached to this project. Run the tests from the open editor's Test Runner, or close Unity and rerun batchmode.

Out of scope:

- No concrete block, item, recipe, or Create content definitions yet.
- No data-pack or JSON import yet.
- No world storage, rendering, scenes, or gameplay machines.

### Step 1.3 - Block Definitions and Immutable Block States

Status: Complete

Approved scope:

- Add minimal block definitions keyed by `ResourceLocation`.
- Add typed state properties with valid values and defaults.
- Add immutable block states with typed get/set helpers.
- Add deterministic block-state serialization to id plus property strings.
- Add focused EditMode tests.

Implemented files:

- `Assets/Scripts/Constructed.Minecraft/Constructed.Minecraft.asmdef`
- `Assets/Scripts/Constructed.Minecraft/IStateProperty.cs`
- `Assets/Scripts/Constructed.Minecraft/StateProperty.cs`
- `Assets/Scripts/Constructed.Minecraft/BlockDefinition.cs`
- `Assets/Scripts/Constructed.Minecraft/BlockState.cs`
- `Assets/Scripts/Constructed.Minecraft/BlockStatePropertyValue.cs`
- `Assets/Scripts/Constructed.Minecraft/SerializedBlockState.cs`
- `Assets/Tests/EditMode/Constructed.Tests.EditMode.asmdef`
- `Assets/Tests/EditMode/StatePropertyTests.cs`
- `Assets/Tests/EditMode/BlockDefinitionTests.cs`
- `Assets/Tests/EditMode/BlockStateTests.cs`

Behavior added:

- Created the `Constructed.Minecraft` assembly for Minecraft-like simulation systems above the core primitives.
- `StateProperty<T>` defines typed block-state properties with lowercase Minecraft-style names, finite valid values, defaults, parsing, and deterministic string serialization.
- `BlockDefinition` owns a block id, declared state properties, and a default immutable `BlockState`.
- `BlockState` supports typed reads, typed immutable mutation, untyped validation for deserialization paths, equality, and deterministic serialization.
- `SerializedBlockState` and `BlockStatePropertyValue` provide a simple id-plus-property-string snapshot for later save/load and data import work.

Verification:

- Added focused EditMode tests for state property defaults/validation/serialization, block definition defaults, immutable state mutation, invalid property rejection, and serialization round trip.
- `Constructed.Core` runtime assembly compiled successfully using Unity 6000.4.5f1's bundled C# compiler.
- `Constructed.Minecraft` runtime assembly compiled successfully against the core compile-check assembly.
- EditMode test source compiled successfully against the core and Minecraft compile-check assemblies plus Unity's bundled NUnit assembly.
- Full Unity Test Runner execution was not run because active Unity processes are already attached to this project. Run the tests from the open editor's Test Runner, or close Unity and rerun batchmode.

Out of scope:

- No world storage, chunks, neighbor updates, scheduled ticks, or block lifecycle yet.
- No concrete Minecraft/Create block catalog yet.
- No items, recipes, rendering, scenes, placement, or gameplay machines.

### Step 1.4 - Minimal In-Memory World Block Storage

Status: Complete

Approved scope:

- Add an explicit air/default block state boundary for empty positions.
- Add plain C# in-memory storage keyed by `BlockPos`.
- Add `Get`, `Set`, `Remove`, occupancy checks, and stable enumeration of stored non-air states.
- Add focused EditMode tests.

Implemented files:

- `Assets/Scripts/Constructed.Minecraft/BlockWorld.cs`
- `Assets/Scripts/Constructed.Minecraft/WorldBlockEntry.cs`
- `Assets/Tests/EditMode/BlockWorldTests.cs`

Behavior added:

- `BlockWorld` stores non-air `BlockState` values by `BlockPos` in memory.
- Missing positions return the explicit configured air state.
- Setting an air state removes storage for that position.
- `SetBlockState` and `RemoveBlock` return the previous state for later lifecycle/event use.
- `HasStoredBlock`, `StoredBlockCount`, `Clear`, and deterministic `GetStoredBlocks` enumeration support simple inspection and tests.
- Air recognition uses the configured air block id, not object reference identity, so equivalent `minecraft:air` states clear storage.

Verification:

- Added focused EditMode tests for missing-position air fallback, set/get behavior, setting air as removal, explicit removal, stable stored-block ordering, id-based air recognition, and null-state rejection.
- Initial compile-check output attempts under Unity `Temp` and `Library` were blocked by sandbox/write permissions, not by source errors.
- `Constructed.Core` runtime assembly compiled successfully using Unity 6000.4.5f1's bundled C# compiler with outputs under `C:\Users\user\.codex\memories\ConstructedCompile`.
- `Constructed.Minecraft` runtime assembly compiled successfully against the core compile-check assembly.
- EditMode test source compiled successfully against the core and Minecraft compile-check assemblies plus Unity's bundled NUnit assembly.
- Full Unity Test Runner execution was not run because active Unity processes are already attached to this project. Run the tests from the open editor's Test Runner, or close Unity and rerun batchmode.

Out of scope:

- No chunks, sections, neighbor updates, scheduled ticks, random ticks, or block lifecycle hooks yet.
- No block entities, entities, items, recipes, rendering, scenes, placement tools, or Create machines.

### Step 1.5 - Block Lifecycle Callbacks and Neighbor Updates

Status: Complete

Approved scope:

- Add a minimal block lifecycle callback interface attached to `BlockDefinition`.
- Dispatch placement, removal, and replacement callbacks from `BlockWorld` set/remove operations.
- Dispatch neighbor-update callbacks to stored non-air neighbors around changed positions.
- Add focused EditMode tests.

Implemented files:

- `Assets/Scripts/Constructed.Minecraft/IBlockLifecycle.cs`
- `Assets/Scripts/Constructed.Minecraft/BlockLifecycle.cs`
- `Assets/Scripts/Constructed.Minecraft/BlockStateChange.cs`
- `Assets/Scripts/Constructed.Minecraft/NeighborBlockChange.cs`
- `Assets/Scripts/Constructed.Minecraft/BlockDefinition.cs`
- `Assets/Scripts/Constructed.Minecraft/BlockWorld.cs`
- `Assets/Tests/EditMode/BlockWorldLifecycleTests.cs`

Behavior added:

- `BlockDefinition` now owns an `IBlockLifecycle` handler, defaulting to `BlockLifecycle.None`.
- `BlockWorld.SetBlockState` ignores no-op state writes, canonicalizes air writes to the configured air state, and dispatches callbacks after real storage changes.
- Placement callbacks are sent to the new non-air block state.
- Removal callbacks are sent to the previous non-air block state.
- Replacement from one non-air state to another dispatches removal for the old state and placement for the new state.
- Neighbor updates are sent to stored non-air blocks in the six adjacent positions, with direction and old/new changed-state context.

Verification:

- Added focused EditMode tests for placement callbacks, removal callbacks, replacement callbacks, no-op writes, and neighbor-update direction/context.
- `Constructed.Core` runtime assembly compiled successfully using Unity 6000.4.5f1's bundled C# compiler with outputs under `C:\Users\user\.codex\memories\ConstructedCompile`.
- `Constructed.Minecraft` runtime assembly compiled successfully against the core compile-check assembly.
- EditMode test source compiled successfully against the core and Minecraft compile-check assemblies plus Unity's bundled NUnit assembly.
- Full Unity Test Runner execution was not run because active Unity processes are already attached to this project. Run the tests from the open editor's Test Runner, or close Unity and rerun batchmode.

Out of scope:

- No chunks, sections, scheduled ticks, random ticks, block entities, entities, items, recipes, rendering, scenes, placement tools, or Create machines.

### Step 1.6 - Scheduled Block Ticks

Status: Complete

Approved scope:

- Add a minimal scheduled block tick queue to `BlockWorld`.
- Track deterministic world tick time and execute due scheduled block ticks in stable order.
- Add `hasScheduled`-style duplicate checks for position plus block id.
- Add focused EditMode tests.

Reference findings:

- Create uses Minecraft scheduled block ticks as deferred server-side followups in systems such as water wheels, gearshifts, chutes, pipes, tracks, and redstone.
- Create commonly guards scheduling with `getBlockTicks().hasScheduledTick(pos, block)` before calling `scheduleTick`.
- Some Create calls pass tick priorities, so the Unity queue includes a minimal priority enum and sorts by trigger tick, priority, then insertion order.

Implemented files:

- `Assets/Scripts/Constructed.Minecraft/ScheduledTickPriority.cs`
- `Assets/Scripts/Constructed.Minecraft/ScheduledTickPriority.cs.meta`
- `Assets/Scripts/Constructed.Minecraft/ScheduledBlockTick.cs`
- `Assets/Scripts/Constructed.Minecraft/ScheduledBlockTick.cs.meta`
- `Assets/Scripts/Constructed.Minecraft/IBlockLifecycle.cs`
- `Assets/Scripts/Constructed.Minecraft/BlockLifecycle.cs`
- `Assets/Scripts/Constructed.Minecraft/BlockWorld.cs`
- `Assets/Tests/EditMode/BlockWorldLifecycleTests.cs`
- `Assets/Tests/EditMode/BlockWorldScheduledTickTests.cs`
- `Assets/Tests/EditMode/BlockWorldScheduledTickTests.cs.meta`

Behavior added:

- `BlockWorld` now owns deterministic tick time through `CurrentTick`.
- `ScheduleBlockTick` schedules block-definition or block-state ticks by position, delay, and priority.
- `HasScheduledBlockTick` reports pending ticks by position plus scheduled block id.
- Duplicate pending scheduled ticks for the same position plus block id are ignored.
- `Tick()` advances one world tick and runs due scheduled block ticks.
- `Tick(long)` advances multiple world ticks in deterministic one-tick steps.
- `RunScheduledBlockTicks()` executes currently due ticks without advancing time.
- Due ticks execute by trigger tick, priority, and insertion order.
- Scheduled callbacks dispatch through `IBlockLifecycle.OnScheduledTick` only when the current block at the position still matches the scheduled block id.
- `Clear()` now clears pending scheduled block ticks as well as stored block states.

Verification:

- Unity 6000.4.5f1 batchmode compiled the project successfully and regenerated script assemblies, with `Tundra build success` in `Logs/EditModeTests.log`.
- Unity batchmode still did not create `EditModeResults.xml` or log a Test Runner summary, so full Unity Test Runner execution remains blocked.
- Fallback verification ran a temporary reflection harness against the Unity-compiled `Constructed.Tests.EditMode.dll`; all 51 reflected NUnit `[Test]` methods passed.

Out of scope:

- No chunks, chunk tick containers, random ticks, fluid ticks, block entities, entities, items, recipes, rendering, scenes, placement tools, or Create machines.

## Planned Phase Outline

1. Phase 1 - Core foundations: ids, grid math, registries, tags, block states, tick basics.
2. Phase 2 - Minecraft world model: world/chunk storage, block lifecycle, scheduled ticks, block entities.
3. Phase 3 - Items, inventories, recipes, and data import.
4. Phase 4 - Minimal Unity presentation layer and debug placement.
5. Phase 5 - First kinetic showcase slice: creative motor, shaft, cogwheels, gearbox.
6. Phase 6 - Belts and simple item transport.
7. Phase 7 - Processing machines and depot/belt processing.
8. Phase 8 - Logistics basics: funnels, tunnels, filters.
9. Phase 9 - Larger systems: fluids, contraptions, trains, schematics, progression, multiplayer.

## Next Proposed Step

Discuss and confirm Step 1.7 before implementation. Proposed scope: minimal block entity foundation with per-position runtime objects, initialize/tick/lazy-tick/remove/destroy callbacks, behavior composition entry points, and focused EditMode lifecycle tests. Defer items, inventories, recipes, rendering, scenes, networking, and Create machines.
