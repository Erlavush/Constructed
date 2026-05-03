# Constructed Agent Notes

## Project Intent

`Constructed` is a fresh Unity/C# project for recreating Minecraft + Create-mod-style mechanics in Unity.

Agents must start clean. Do not look for, read, port, or copy from any previous prototype. The implementation must be based on direct study of Minecraft, NeoForge, and the official Create source in this repository.

The user wants agents to spend real time understanding the reference code before building systems. Do not rely on assumptions, summaries, old experiments, or rushed implementation plans.

Do not start gameplay implementation until the relevant reference source has been studied and the Unity design for that subsystem is clear.

## Reference Source

Official Create repo clone for this Unity project:

```text
Z:\Constructed\References\Create-mc1.21.1-dev
```

Clone details:

```text
Remote: https://github.com/Creators-of-Create/Create.git
Branch: mc1.21.1/dev
Commit: 9c2f16dd8614544a8d47c6bc514f511be22e865a
```

## Version Target

Use the cloned Create repo's `gradle.properties` as the source of truth.

Current reference versions:

```text
Create: 6.0.11
Minecraft: 1.21.1
NeoForge: 21.1.219
Java: 21
Registrate: MC1.21-1.3.0+67
Ponder: 1.0.82
Flywheel: 1.0.6
Vanillin: 1.1.3-41
```

## Current Unity State

This repo is currently a Unity 6 URP starter project with no real gameplay runtime code yet.

Treat this as a blank Unity implementation space. Build from the reference behavior, not from previous prototype decisions.

Unity version from `ProjectSettings/ProjectVersion.txt`:

```text
6000.4.5f1
```

Relevant packages from `Packages/manifest.json` include URP 17.4.0, Input System 1.19.0, AI Navigation 2.0.12, UGUI 2.0.0, and Unity Test Framework 1.6.0.

## Source Architecture Map

Before implementing a subsystem, read the relevant source files directly. These are the current high-value entry points:

```text
src/main/java/com/simibubi/create/Create.java
src/main/java/com/simibubi/create/CreateClient.java
src/main/java/com/simibubi/create/foundation/events/CommonEvents.java
src/main/java/com/simibubi/create/foundation/events/ClientEvents.java
src/main/java/com/simibubi/create/foundation/data/CreateRegistrate.java
src/main/java/com/simibubi/create/infrastructure/data/CreateDatagen.java
src/main/java/com/simibubi/create/AllBlocks.java
src/main/java/com/simibubi/create/AllItems.java
src/main/java/com/simibubi/create/AllBlockEntityTypes.java
src/main/java/com/simibubi/create/AllRecipeTypes.java
src/main/java/com/simibubi/create/AllPartialModels.java
src/main/java/com/simibubi/create/AllPackets.java
src/main/java/com/simibubi/create/infrastructure/config/AllConfigs.java
src/main/java/com/simibubi/create/infrastructure/config/CKinetics.java
src/main/java/com/simibubi/create/infrastructure/config/CStress.java
```

Create's main Java packages are organized like this:

```text
api             public extension points and custom registries
compat          optional mod integrations
content         actual gameplay systems: kinetics, logistics, contraptions, trains, fluids, etc.
foundation      reusable Create infrastructure: block entities, behaviours, data, render, recipes, networking
impl            platform implementation glue
infrastructure  config, datagen, commands, gametest, worldgen
```

The largest content areas are `content/kinetics`, `content/logistics`, `content/contraptions`, `content/trains`, `content/equipment`, `content/redstone`, `content/fluids`, `content/decoration`, `content/schematics`, and `content/processing`.

## Minecraft Systems Needed First

Create assumes Minecraft already provides a large deterministic simulation layer. Unity must recreate enough of that layer before Create-like mechanics can be correct.

Implement these Minecraft-like foundations first:

- Resource identity: `namespace:path` ids equivalent to `ResourceLocation`.
- Registries: blocks, items, block entity types, recipe serializers/types, tags, data maps, custom Create registries.
- Block grid: `BlockPos`, `Direction`, `Axis`, integer voxel coordinates, neighbor lookup, loaded chunks/sections.
- Block definitions and block states: immutable block id plus typed state properties such as axis, facing, waterlogged, powered, part, slope, casing.
- Block lifecycle: placement, removal, replacement, neighbor update, scheduled tick, random/lazy tick, shape/collision queries.
- Block entities: per-position stateful runtime objects with initialize, tick, lazy tick, remove, destroy, read/write serialization, and client update boundaries.
- Item model: item definitions, `ItemStack`, count, components/NBT-like data, inventories, item entity drops.
- Capabilities/ports: side-aware item/fluid/component access. NeoForge capabilities are essential to belts, depots, funnels, basins, tanks, and automation.
- Tags: item/block/recipe tags drive behavior more than hard-coded ids in many places.
- Recipes and data packs: recipe manager, recipe lookup by type/input, generated JSON loading, processing outputs with chances, durations, and heat requirements.
- Server-authoritative tick: deterministic 20 TPS-style simulation, independent from render frame rate.
- Client/render side: interpolated visuals, partial models, animation time, overlay/tool UI, separate from authoritative simulation.
- Persistence: save/load for world, block states, block entities, transported items, networks, and runtime configuration.

Do not start with Create machines in isolation. A shaft or belt only behaves correctly when block states, block entities, ticks, registries, tags, recipes, and neighbor updates already exist.

## NeoForge and Create Lifecycle Concepts

Create is structured around NeoForge's mod lifecycle. In Unity, mirror the lifecycle conceptually rather than copying the Java framework.

Important source behavior:

- `Create.java` is the main `@Mod` entry. Its constructor registers Registrate listeners, calls all `All*` registries, registers configs, packets, custom registries, and common setup listeners.
- `CreateClient.java` is the client-only entry. It registers client setup, particles, partial models, model swapping, Flywheel instance types, render caches, Ponder plugin setup, and client handlers.
- `CreateRegistrate.java` wraps Registrate and adds Create-specific block entity builders, entity builders, mounted storage builders, display sources/targets, connected textures, model swapping, tooltip modifiers, and creative tab tracking.
- `AllBlocks.java`, `AllItems.java`, `AllBlockEntityTypes.java`, and similar files are not passive lists. Registration chains attach properties, tags, stress values, data generation transforms, movement behaviors, interaction behaviors, render layers, item models, and custom client models.
- `AllRecipeTypes.java` registers recipe serializers and recipe types via DeferredRegister and expands the crafting grid to 9x9 for mechanical crafting.
- `AllConfigs.java`, `CKinetics.java`, and `CStress.java` provide runtime tuning values and populate `BlockStressValues`.
- `CommonEvents.java` handles server tick, world tick, entity tick, world load/unload, reload listeners, commands, dynamic data packs, and capability registration.
- `ClientEvents.java` handles client tick, world render stages, overlays, tooltips, camera changes, reload listeners, and client-only interaction handlers.
- `create.mixins.json` and `foundation/mixin` patch Minecraft behavior where Create needs deeper hooks. Unity should implement these as explicit engine extension points rather than hidden patches.

Unity should have equivalent phases:

```text
Bootstrap
  register all definitions and ids

Config Load
  apply defaults and user overrides before gameplay starts

Common Setup
  attach behavior maps and dependent registrations after registries exist

World Load
  create world-attached managers such as kinetic networks

Simulation Tick
  tick worlds, block entities, scheduled tasks, entities, recipes, transport, managers

Client Setup
  bind definitions to meshes/materials/animations/UI

Render Tick
  interpolate visuals from simulation state

Data Generation or Import
  ingest Minecraft/Create JSON assets, tags, models, recipes, and sounds
```

## How Create Organizes Gameplay

### Blocks and Items

Study:

```text
AllBlocks.java
AllItems.java
foundation/data/BuilderTransformers.java
foundation/data/BlockStateGen.java
foundation/data/AssetLookup.java
```

Create registers blocks and items with a fluent builder. A block registration can define:

- physical properties and sound/map color
- block state generator
- render layer
- loot/drop behavior
- tags
- item form and item model
- stress impact or capacity
- display source/target behavior
- movement or interaction behavior for contraptions
- connected texture and model swap behavior
- valid block entity type

Unity should represent this as data-driven `BlockDefinition`, `ItemDefinition`, `BlockStateDefinition`, and behavior attachments. Do not hard-code every machine into MonoBehaviours.

### Block Entities and Behaviours

Study:

```text
foundation/blockEntity/SmartBlockEntity.java
foundation/blockEntity/behaviour/BlockEntityBehaviour.java
foundation/blockEntity/behaviour/*
AllBlockEntityTypes.java
```

Create block entities inherit from `SmartBlockEntity`, which composes reusable `BlockEntityBehaviour` instances. Behaviors handle filters, scroll values, inventory tracking, transported item callbacks, advancement ownership, value settings, and similar reusable state.

Unity should use composition:

- `BlockEntity` base for lifecycle and serialization.
- `IBlockEntityBehaviour` for reusable behaviors.
- `BehaviourType<T>` or typed component lookup for cross-machine access.
- Explicit `Initialize`, `Tick`, `LazyTick`, `OnNeighborChanged`, `Read`, `Write`, `OnDestroy`, `OnUnload`.

This is critical for belts, basins, funnels, filters, depots, gauges, deployers, and later contraptions.

### Recipes and Processing

Study:

```text
AllRecipeTypes.java
content/processing/recipe/ProcessingRecipe.java
content/processing/recipe/ProcessingRecipeParams.java
content/processing/recipe/StandardProcessingRecipe.java
foundation/recipe/RecipeApplier.java
content/kinetics/press/MechanicalPressBlockEntity.java
content/kinetics/mixer/MechanicalMixerBlockEntity.java
content/processing/basin/BasinBlockEntity.java
```

Create processing recipes are typed. They support item ingredients, fluid ingredients, weighted outputs, fluid outputs, processing duration, and heat conditions. Machines query the `RecipeManager` by recipe type and input. Automation ignores recipes tagged or named as manual-only.

Unity should load recipes from data, not from per-machine hard-coded tables. The early implementation only needs enough recipe infrastructure for pressing, milling, crushing, mixing, compacting, and belt/depot processing tests.

### Kinetics

Study these before touching kinetic code:

```text
content/kinetics/base/IRotate.java
content/kinetics/base/KineticBlock.java
content/kinetics/base/KineticBlockEntity.java
content/kinetics/base/GeneratingKineticBlockEntity.java
content/kinetics/RotationPropagator.java
content/kinetics/KineticNetwork.java
content/kinetics/TorquePropagator.java
api/stress/BlockStressValues.java
infrastructure/config/CKinetics.java
infrastructure/config/CStress.java
content/kinetics/simpleRelays/ShaftBlock.java
content/kinetics/simpleRelays/CogWheelBlock.java
content/kinetics/gearbox/GearboxBlock.java
content/kinetics/motor/CreativeMotorBlockEntity.java
content/kinetics/waterwheel/WaterWheelBlockEntity.java
```

Core findings from source:

- Rotation speed is represented as RPM-like float values. Sign is direction.
- `IRotate` exposes shaft connectivity, rotation axis, speed requirements, and stress display behavior.
- `KineticBlockEntity` stores `speed`, `source`, `network`, `capacity`, `stress`, `overStressed`, dirty flags, and validation countdown.
- `GeneratingKineticBlockEntity` creates a new network id from its position when it becomes a source.
- `RotationPropagator` walks connected neighboring kinetic block entities on placement/removal and computes speed ratios.
- Shaft-to-shaft transfer is 1:1 unless split shafts/gearboxes change sign or ratio.
- Small cog to small cog reverses speed when axes align as gear contact requires.
- Large-to-small cog ratios are 2:1 or 1:2 depending direction.
- Large-to-large perpendicular gears transfer 1:1 with sign based on relative placement.
- Belts propagate rotation across all segments in the same controller chain.
- Incompatible signs or over-speed can destroy the placed block in Minecraft behavior.
- `KineticNetwork` sums source capacity and member stress. Actual capacity/stress scales by absolute speed.
- Stress defaults are registered through `CStress` during block registration, then exposed through `BlockStressValues`.
- Default kinetic config includes max belt length 20, max rotation speed 256, medium speed 30 RPM, fast speed 100 RPM, and validation every 60 game ticks.

Unity should implement kinetics as a deterministic graph service, not as per-object physics. Use integer block positions, graph traversal, and explicit ratio rules. Rendering should read the computed speed and animate independently.

### Belts and Item Transport

Study:

```text
content/kinetics/belt/BeltBlock.java
content/kinetics/belt/BeltBlockEntity.java
content/kinetics/belt/BeltHelper.java
content/kinetics/belt/BeltPart.java
content/kinetics/belt/BeltSlope.java
content/kinetics/belt/BeltSlicer.java
content/kinetics/belt/item/BeltConnectorItem.java
content/kinetics/belt/transport/BeltInventory.java
content/kinetics/belt/transport/TransportedItemStack.java
content/kinetics/belt/transport/BeltMovementHandler.java
content/kinetics/belt/behaviour/DirectBeltInputBehaviour.java
content/kinetics/belt/behaviour/TransportedItemStackHandlerBehaviour.java
content/kinetics/belt/behaviour/BeltProcessingBehaviour.java
```

Core findings from source:

- A belt is a chain of block states with `SLOPE`, `PART`, `CASING`, waterlogged, and horizontal facing.
- Each segment has a `BeltBlockEntity`, but one controller owns the `BeltInventory`.
- Segments store controller position, belt length, segment index, casing, color, and covered state.
- `BeltBlock.initBelt` finds the chain, assigns controller/index/length, and attaches kinetics.
- Belt movement speed is `getSpeed() / 480f`.
- Transported item positions are floats along the controller chain; spacing is enforced in `BeltInventory`.
- Items can be locked by processing machines, inserted from sides, passed into direct inputs, ejected as item entities, or blocked by solid sides.
- Entity transport is separate from item transport and uses collision/contact info in `BeltMovementHandler`.
- Belts interact with funnels, tunnels, crushing wheels, and processing behaviors through reusable behavior lookups.

Unity should implement belt chain creation and item movement only after kinetic speed, item stacks, inventories, behavior lookup, and block-state geometry exist.

### Rendering, Models, and Assets

Study:

```text
AllPartialModels.java
CreateClient.java
foundation/render/AllInstanceTypes.java
content/kinetics/base/KineticBlockEntityRenderer.java
content/kinetics/base/SingleAxisRotatingVisual.java
content/kinetics/belt/BeltRenderer.java
content/kinetics/belt/BeltVisual.java
content/kinetics/belt/BeltModel.java
foundation/model/ModelSwapper.java
foundation/block/connected/*
```

Create uses normal Minecraft block/item JSON plus many partial models for animated parts. Flywheel instance types drive efficient rotating and scrolling visuals. Rendering is not the source of truth for mechanics; it consumes block state, block entity state, speed, and animation time.

Unity rendering should be a separate layer:

- Import or parse Minecraft model/blockstate JSON into Unity meshes/materials where feasible.
- Keep simulation data independent from GameObjects.
- Use pooled/instanced renderers for repeated shafts, cogs, belts, and partial models.
- Animate rotating parts from kinetic speed and render time.
- Animate belt surfaces by scrolling materials or generated mesh UV offsets, not by moving simulation items.

### Data Generation and Runtime Data

Study:

```text
infrastructure/data/CreateDatagen.java
foundation/data/recipe/*
src/main/resources
src/generated/resources
```

Create has both hand-authored and generated resources. The checked-out tree includes:

```text
src/main/resources/assets/create
src/main/resources/data/create
src/generated/resources/assets/create
src/generated/resources/data/create
```

The generated resources include many blockstates, models, tags, recipes, language entries, loot tables, data maps, and advancements. Future Unity import tools should read both `src/main/resources` and `src/generated/resources`; otherwise many blocks and recipes will appear missing.

## Private Asset Rule

Create code can be studied as an implementation reference because Create is MIT licensed.

For this personal project, agents may copy Create textures, models, blockstates, sounds, generated JSON, or other assets into a private ignored Unity folder for local development and visual accuracy.

Use this folder for copied Create assets:

```text
Assets/PrivateTemp/Create
```

These private copied assets must not be committed, published, or redistributed. This folder is ignored by `.gitignore` through `/[Aa]ssets/[Pp]rivate[Tt]emp/`.

If the project becomes public later, replace these copied assets with original assets or assets with a compatible license.

When copying reference assets for local development, preserve source structure under `Assets/PrivateTemp/Create`, for example:

```text
Assets/PrivateTemp/Create/src/main/resources/assets/create
Assets/PrivateTemp/Create/src/main/resources/data/create
Assets/PrivateTemp/Create/src/generated/resources/assets/create
Assets/PrivateTemp/Create/src/generated/resources/data/create
```

Copy only reference assets/data needed for the current task. Do not copy Java source into Unity runtime folders. Do not move copied assets out of `Assets/PrivateTemp/Create`.

Suggested local copy command pattern, if assets are needed:

```powershell
New-Item -ItemType Directory -Force -Path Assets\PrivateTemp\Create
New-Item -ItemType Directory -Force -Path Assets\PrivateTemp\Create\src\main
New-Item -ItemType Directory -Force -Path Assets\PrivateTemp\Create\src\generated
Copy-Item -Recurse -Force References\Create-mc1.21.1-dev\src\main\resources Assets\PrivateTemp\Create\src\main\
Copy-Item -Recurse -Force References\Create-mc1.21.1-dev\src\generated\resources Assets\PrivateTemp\Create\src\generated\
```

## Unity Implementation Strategy

Build a clean C# simulation core first, then bind it to Unity presentation. Keep runtime logic testable without scenes.

Recommended namespace/module split:

```text
Constructed.Core
  math primitives, ids, registries, serialization, deterministic tick

Constructed.Minecraft
  blocks, block states, items, item stacks, tags, recipes, chunks, worlds, block entities

Constructed.Create
  Create content definitions, kinetics, belts, processing, logistics, contraptions later

Constructed.Unity
  MonoBehaviour bootstrap, asset import, rendering, input, camera, UI, scene glue

Constructed.Tests
  EditMode tests for simulation and importers; PlayMode tests only where Unity runtime is required
```

Use ScriptableObjects only where they help author or inspect data in Unity. The authoritative simulation should be plain C# data and services.

### First Implementation Order

1. Core primitives: `ResourceLocation`, registries, tags, `BlockPos`, `Direction`, `Axis`, deterministic tick clock.
2. Minecraft block model: block definitions, immutable block states, state properties, world/chunk storage, neighbor updates, scheduled ticks.
3. Items and persistence: item definitions, item stacks, simple inventories, serialization, item entities.
4. Block entities: lifecycle, behavior composition, typed behavior lookup, save/load, lazy ticks.
5. Data import: load enough Create/Minecraft JSON for blocks, items, tags, recipes, blockstates, models, and textures from private temp assets.
6. Minimal rendering: block mesh display, basic item display, debug overlays, selection, placement/removal.
7. Kinetics phase 1: shafts, small/large cogwheels, creative motor, water wheel stub, gearbox/clutch/gearshift later; implement `RotationPropagator` and `KineticNetwork` with tests.
8. Kinetics phase 2: stress/capacity config, over-stress behavior, source validation, save/load of networks.
9. Belts phase 1: belt connector placement, belt chain/controller/index/length, pulley parts, kinetic propagation across belt segments.
10. Belts phase 2: transported item stacks, spacing, side insertion, end insertion/ejection, simple entity conveyance.
11. Processing phase 1: recipe manager plus pressing/milling/crushing on depots or belts.
12. Logistics phase 1: depot, funnels, tunnels, filters only after belt and inventory behavior is stable.
13. Later systems: fluids, contraptions, deployers/saws/drills, redstone links, trains, schematics, Ponder-like tutorials, mod compatibility, networking/multiplayer.

### What Should Wait

Do not start with these until the fundamentals above are stable:

- trains and railway graphs
- full moving contraptions
- schematicannon/schematic workflows
- full fluid networks and tanks
- redstone link networks
- deployer fake-player behavior
- world generation
- survival progression and advancements
- Ponder/tutorial UI
- JEI/Curios/ComputerCraft-style compatibility
- multiplayer networking

These systems are large and depend on the same block/entity/registry/tick foundations.

## Testing Requirements

Use Unity Test Framework. Prefer EditMode tests for simulation systems.

Minimum tests before considering a subsystem done:

- `ResourceLocation` parsing, equality, invalid ids.
- Registry registration, lookup, duplicate id rejection, tag matching.
- `Direction`, `Axis`, rotation, opposite, clockwise/counterclockwise, position offsets.
- Block state property defaults, mutation returning new state, serialization round trip.
- World set/remove block, neighbor update dispatch, scheduled tick execution order.
- Block entity initialize/tick/lazyTick/remove/destroy/read/write lifecycle.
- ItemStack copy, count changes, inventory insert/extract behavior.
- Recipe loading and matching for at least one processing recipe type.
- Kinetic shaft/cog/large-cog/gearbox propagation ratios using source examples from `RotationPropagator.java`.
- Kinetic network stress/capacity math and over-stress state.
- Kinetic source removal and re-propagation.
- Belt chain init: controller, length, index, slope, parts, max length.
- Belt kinetic propagation across all segments.
- Belt item movement at `speed / 480f`, item spacing, lock/unlock, insertion, ejection, blocked endings.
- Save/load for kinetic block entities, belt controllers, and transported items.
- Asset import validation for copied Create textures/models/blockstates/recipes needed by the current feature.

For gameplay-facing features, add a small PlayMode smoke test scene only after EditMode simulation tests pass.

### Verification Commands and Compile Checks

After code changes, agents must verify that the changed code compiles and record the result in `progress_map.md`.

Preferred verification is Unity Test Framework through the Unity editor or batchmode. Use the Unity version from `ProjectSettings/ProjectVersion.txt`; this project currently targets `6000.4.5f1`.

Suggested Windows batchmode EditMode test command:

```powershell
$Unity = "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe"
& $Unity -batchmode -quit -projectPath "Z:\Constructed" -runTests -testPlatform EditMode -testResults "Temp\EditModeResults.xml" -logFile "Logs\EditModeTests.log"
```

If batchmode does not run because the project is already open in the Unity editor, run the tests from the open editor's Test Runner or close the editor and rerun batchmode. If neither is possible, do a compile-only fallback with Unity's bundled compiler for the changed runtime assembly and record the limitation clearly.

Compile-only fallback pattern for plain runtime C# files:

```powershell
$UnityRoot = "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor"
$env:MONO_PATH = "$UnityRoot\Data\MonoBleedingEdge\lib\mono\4.5\Facades;$UnityRoot\Data\MonoBleedingEdge\lib\mono\msbuild\Current\bin\Roslyn"
& "$UnityRoot\Data\MonoBleedingEdge\bin\mono.exe" "$UnityRoot\Data\MonoBleedingEdge\lib\mono\msbuild\Current\bin\Roslyn\csc.exe" -nologo -target:library -langversion:latest -out:"Temp\Constructed.Core.compilecheck.dll" <changed-runtime-cs-files>
```

A compile-only fallback is not a substitute for tests. It only proves the selected files compile. When using it, write in `progress_map.md` which assembly/files were checked, whether the command passed, and why full Unity tests were not run.

## Working Rules for Future Agents

- Read this file first.
- Read the relevant Create source files before designing or coding a subsystem.
- Record any new source findings in this file when they affect implementation strategy.
- Keep copied Create assets inside `Assets/PrivateTemp/Create`.
- Keep simulation logic independent from Unity scene objects.
- Prefer data-driven definitions and behavior composition over one-off MonoBehaviours.
- Do not port code blindly. Recreate behavior in idiomatic C# after understanding the source.
- Do not implement unrelated systems early just because a source file references them. Stub boundaries explicitly and return later.
- Keep tests close to the source rules being mirrored.

## Step-by-Step Approval Rule

Every gameplay feature or subsystem must be discussed with and confirmed by the user before implementation starts. Agents must not proceed from research/design into code just because the next step seems obvious.

Work in small, reviewable increments. Propose one focused step at a time, explain what files or systems it would touch, wait for user confirmation, then implement only that confirmed scope. Do not batch many features into one pass. The project should move slowly enough that architecture mistakes, behavioral mismatches, and bugs are caught early.

## Progress Map Rule

Maintain `progress_map.md` at the repository root. This file is the running implementation ledger and must use numbered phases and steps.

Before starting a confirmed feature, check `progress_map.md` to understand the current phase and scope. At the end of every confirmed step or feature, update `progress_map.md` with:

- what phase/step was worked on
- what files or systems changed
- what behavior was added or intentionally deferred
- what tests were added or run
- current status and the next proposed step

Do not leave progress tracking for later. Each agent must update `progress_map.md` as part of finishing its work.

## GitHub Checkpoint Rule

After each confirmed step is finished, verified, and recorded in `progress_map.md`, agents must create a Git checkpoint and push it to the project remote as a step flag.

Project GitHub remote:

```text
https://github.com/Erlavush/Constructed.git
```

Expected remote name:

```text
origin
```

Checkpoint workflow:

1. Run `git status --short` and review the changed files.
2. Stage only the files that belong to the completed step. Do not stage unrelated local changes.
3. Never stage or push `Assets/PrivateTemp/Create` or copied Create assets.
4. Commit with a clear step message such as `Step 1.3 block state foundations`.
5. Push the commit to `origin` on the current branch.
6. Record the commit hash and push result in `progress_map.md`.

Do not force-push, rewrite history, or reset the worktree unless the user explicitly asks. If authentication, remote access, merge conflicts, or unrelated dirty files block the push, stop and report the blocker in `progress_map.md` and to the user.
