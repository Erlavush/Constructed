# Constructed Agent Notes

## Project Intent

`Constructed` is a Unity/C# project for recreating Minecraft plus Create-mod-style mechanics in Unity.

Build from direct study of Minecraft, NeoForge, and the official Create source in this repository. Do not read, port, or copy from any previous prototype. Do not implement gameplay systems from assumptions.

Before implementing a confirmed gameplay subsystem, read only the reference files relevant to that subsystem and record any new findings that affect the Unity design.

## Current State

Unity version from `ProjectSettings/ProjectVersion.txt`:

```text
6000.4.5f1
```

The project is no longer blank. Current implementation is in Phase 1 core foundations, with Step 2 planned as the visual asset pipeline before deeper Create mechanics:

- `Constructed.Core`: resource ids, grid math, registries, tags, deterministic tick clock.
- `Constructed.Minecraft`: block definitions, immutable block states, in-memory world storage, lifecycle callbacks, neighbor updates, scheduled ticks, block entities, items, and inventories.
- `Constructed.Create`: first Create content definitions, single-block Item Vault storage, and the first vertical-slice content catalog.
- `Constructed.Unity`: current placeholder demo presentation for the flat surface and first vertical-slice blocks.
- `Constructed.Tests.EditMode`: focused tests for the current foundation and demo code.

Read `progress_map.md` before starting work. It is the source of truth for current phase, completed steps, verification status, and the next proposed step.

## Reference Source

Official Create repo clone for this Unity project:

```text
Z:\Constructed\References\Create-mc1.21.1-dev
```

Clean local Minecraft and NeoForge reference mirrors for this Unity project:

```text
Z:\Constructed\References\Minecraft-1.21.1-sources
Z:\Constructed\References\Minecraft-1.21.1-resources
Z:\Constructed\References\NeoForge-21.1.219-sources
Z:\Constructed\References\MDK-1.21.1-NeoGradle
```

Use these folders as the primary local reference set:

- `Minecraft-1.21.1-sources`: decompiled mapped vanilla source for browsing mechanics and control flow.
- `Minecraft-1.21.1-resources`: extracted vanilla `assets/minecraft` and `data/minecraft` resources for models, blockstates, textures, recipes, tags, and related data.
- `NeoForge-21.1.219-sources`: extracted official NeoForge source/resources for API, patches, loaders, hooks, and modding-layer behavior.
- `MDK-1.21.1-NeoGradle`: local NeoGradle workspace aligned to this project's target versions; use it only when you need the generated task surface, raw jars, mappings, or NeoForm build artifacts behind the clean mirrors above.

All of these reference folders are local-only under ignored `References/`. They are for study and tooling, not for staging or publishing.

Clone details:

```text
Remote: https://github.com/Creators-of-Create/Create.git
Branch: mc1.21.1/dev
Commit: 9c2f16dd8614544a8d47c6bc514f511be22e865a
```

Use the cloned Create repo's `gradle.properties` as the version source of truth. Current reference versions:

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

High-value entry points when architecture context is needed:

```text
src/main/java/com/simibubi/create/Create.java
src/main/java/com/simibubi/create/CreateClient.java
src/main/java/com/simibubi/create/AllBlocks.java
src/main/java/com/simibubi/create/AllItems.java
src/main/java/com/simibubi/create/AllBlockEntityTypes.java
src/main/java/com/simibubi/create/AllRecipeTypes.java
src/main/java/com/simibubi/create/foundation/events/CommonEvents.java
src/main/java/com/simibubi/create/foundation/events/ClientEvents.java
src/main/java/com/simibubi/create/foundation/blockEntity/SmartBlockEntity.java
src/main/java/com/simibubi/create/content/kinetics/RotationPropagator.java
src/main/java/com/simibubi/create/content/kinetics/KineticNetwork.java
```

High-value entry points for Step 2 visual assets:

```text
src/main/resources/assets/create/textures
src/main/resources/assets/create/models
src/generated/resources/assets/create/blockstates
src/generated/resources/assets/create/models
```

Use both `src/main/resources` and `src/generated/resources`; many Create blockstates and item/block model JSON files are generated, while many PNG textures live under main resources.

Secondary runtime evidence is available from the user's Prism instance:

```text
C:\Users\user\AppData\Roaming\PrismLauncher\instances\1.21.1 Neoforge\minecraft
```

Use the Prism instance only when exact runtime evidence is needed, such as mod jars, configs, logs, or saves. Do not treat it as the primary source for clean mechanics browsing when the local reference mirrors above already provide source/resources directly.

Do not reread broad source areas for every small task. Pick the smallest relevant reference set for the confirmed step.

## Shell and Command Guidance

Use the shell that is active in the session. In this workspace that may be PowerShell, and PowerShell is acceptable.

Do not invoke Git Bash or `bash.exe` by hard-coded path. Do not assume Bash is available just because examples use Bash syntax.

Known local issue: some normal Git commands, especially `git status --short` and `git diff`, can fail with:

```text
C:\Program Files\Git\usr\bin\sh.exe: fatal error - couldn't create signal pipe, Win32 error 5
```

If that happens, do not keep retrying Bash/Git Bash variants. Use commands that avoid the failing path when possible, for example:

```powershell
cmd /c git diff-index --name-status HEAD --
cmd /c git diff-index --cached --name-status HEAD --
cmd /c git ls-files --others --exclude-standard
cmd /c git log --oneline --decorate -8
```

For Unity batchmode on Windows, prefer `Unity.com` rather than `Unity.exe` so logs are emitted to the console. Do not pass `-quit` with `-runTests`; the Test Runner controls shutdown, and `-quit` can make Unity exit after import/compile before tests run.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.com' -batchmode -projectPath 'Z:/Constructed' -runTests -testPlatform EditMode -testResults 'Temp/EditModeResults.xml' -logFile 'Logs/EditModeTests.log'
```

If Unity needs to write outside the workspace, request escalation instead of working around sandbox permissions.

## Implementation Strategy

Keep authoritative gameplay simulation in plain C# data and services. Unity scene objects, renderers, meshes, UI, and input are presentation or tooling layers.

Recommended namespace/module split:

```text
Constructed.Core
  math primitives, ids, registries, tags, serialization, deterministic tick

Constructed.Minecraft
  blocks, block states, items, item stacks, recipes, chunks/worlds, block entities

Constructed.Create
  Create content definitions, kinetics, belts, processing, logistics, contraptions later

Constructed.Unity
  MonoBehaviour bootstrap, asset import, rendering, input, camera, UI, scene glue

Constructed.Tests
  EditMode tests for simulation and importers; PlayMode tests only where Unity runtime is required
```

Prefer data-driven definitions and behavior composition over one-off MonoBehaviours. Use ScriptableObjects only where they help author or inspect data in Unity.

## Implementation Order

Build Minecraft-like foundations before Create machines, then make the visuals trustworthy before adding deeper mechanics:

1. Core primitives: `ResourceLocation`, registries, tags, `BlockPos`, `Direction`, `Axis`, deterministic tick clock.
2. Block model: block definitions, immutable block states, world/chunk storage, neighbor updates, scheduled ticks.
3. Items and persistence: item definitions, item stacks, inventories, item entities, save/load.
4. Block entities: lifecycle, behavior composition, typed behavior lookup, serialization, lazy ticks.
5. Step 2 visual asset pipeline: private Create asset sync, Minecraft/Create JSON model parsing, texture loading, item previews, state-driven block visuals, and a trusted `SampleScene` visual catalog.
6. Correct first machine visual slice: creative motor, shafts/pulleys, belt parts, creative crate, brass funnel, and Item Vault placed with correct visual state and attachment rules.
7. Kinetics: shafts, cogwheels, creative motor, gearbox, propagation, networks, stress.
8. Belts: belt chain/controller/index/length, kinetic propagation, transported item stacks.
9. Processing and logistics: recipes, depot/belt processing, funnels, tunnels, filters.

Large systems such as trains, contraptions, fluids, schematic workflows, fake-player deployers, worldgen, tutorials, and multiplayer should wait until the foundations they depend on are stable.

## Step 2 Visual Asset Pipeline

Step 2 exists because Create blocks cannot be represented as generic cubes. The Unity view must first display real Create items/blocks and then let block state and connection state select the correct visual form.

Step 2 should be implemented in small confirmed increments:

1. Private asset sync for a narrow allowlist of Create assets needed by the first slice. Copy only into `Assets/PrivateTemp/Create`, preserving source structure. Never commit copied Create textures, models, blockstates, generated JSON, OBJ/MTL files, sounds, or other assets.
2. Asset manifest/index for the private copy. It should map `create:*` ids to source-relative files and report missing files clearly, without embedding copied asset contents in committed code.
3. JSON model reader for the Minecraft/Create model subset needed by the first slice. Cover parent inheritance, texture variable resolution, cube `elements`, faces, UVs, rotations, tint/color hooks as placeholders, and item model display metadata where useful.
4. Texture loader for private PNG assets with point filtering and transparent texture support. Missing private assets should produce clear placeholder visuals and editor diagnostics, not silent wrong geometry.
5. Item visual catalog in `SampleScene`. Display selected Create item textures/models one by one before machine assembly: `create:andesite_alloy`, `create:belt_connector`, `create:shaft`, `create:creative_motor`, `create:creative_crate`, `create:brass_funnel`, and `create:item_vault`.
6. Block visual catalog in `SampleScene`. Display selected blockstates and model variants for `create:shaft`, `create:belt`, `create:creative_motor`, `create:creative_crate`, `create:brass_funnel`, and `create:item_vault`.
7. Stateful machine visual slice. Use authoritative block states and derived connection state to select visuals. A standalone shaft should not look the same as a shaft/pulley participating in a belt. Belt visuals should distinguish start, middle, end, and pulley states. Funnel and Item Vault visuals should use facing/axis properties.
8. Verification. Add focused EditMode tests for asset-path resolution, manifest coverage, model JSON parsing, blockstate variant selection, and first-slice visual state selection. Use Unity visual inspection for scene presentation, but keep gameplay simulation tests in plain C#.

Step 2 should not implement full gameplay behavior by itself. It prepares the visual/import layer so later kinetics, belt transport, crate output, funnel transfer, and Item Vault insertion can drive correct visuals instead of placeholder cubes.

## Private Asset Rule

Create code can be studied as an implementation reference because Create is MIT licensed.

For this personal project, copied Create assets may be used only in the ignored private folder:

```text
Assets/PrivateTemp/Create
```

Do not commit, publish, or redistribute copied Create textures, models, blockstates, sounds, generated JSON, or other assets. Do not copy Java source into Unity runtime folders.

Preserve source structure under `Assets/PrivateTemp/Create` when copying reference assets/data needed for a current task.

## Testing and Verification

Use Unity Test Framework. Prefer EditMode tests for deterministic simulation systems.

For each confirmed step, add or update tests at the same scope as the behavior changed. Do not treat future systems in the roadmap as required test coverage for the current small step.

After code changes, verify compilation/tests and record the result in `progress_map.md`.

Preferred verification:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.com' -batchmode -projectPath 'Z:/Constructed' -runTests -testPlatform EditMode -testResults 'Temp/EditModeResults.xml' -logFile 'Logs/EditModeTests.log'
```

Keep verification lightweight. If Unity exits successfully from `-runTests`, do not read or paste full XML/log contents; record the command and pass result. Inspect only a short result summary if needed. Read logs or full XML only when the command fails, exits ambiguously, or the requested result cannot otherwise be determined.

If Unity batchmode compiles but does not produce a test results XML, record that exact limitation. A successful script assembly build is not the same as a completed test run.

Use compile-only fallback only when Unity Test Runner is blocked, and record which assemblies/files were checked and why full tests were not run.

Verification cost matters. Do not burn time or tokens on toolchain archaeology for routine fixes.

If it is already obvious that the Unity editor has this project open, do not launch a doomed batchmode test run just to confirm the lock. Skip straight to the bounded fallback below unless the user explicitly wants a fresh batchmode attempt after closing the editor.

If the batchmode run fails because another Unity instance already has the project open:

1. Stop after that first failed batchmode attempt. Do not try `dotnet`, `msbuild`, raw `csc`, Roslyn, or manual reference-hunting unless the user explicitly asks for deeper compiler investigation.
2. Use at most one cheap fallback:
   - preferred: inspect the live Unity `Editor.log` for a fresh successful recompile of the touched assemblies after the change
   - otherwise: report that verification is blocked until the user closes the editor
3. For narrow runtime or visual fixes, a successful live-editor recompile is sufficient fallback verification. Do not try to reconstruct Unity's compile graph manually.
4. If the cheap fallback is inconclusive, stop and report the limitation instead of exploring more verification paths.

The goal is one real verification attempt plus one bounded fallback, not an open-ended search for alternate compilers.

For documentation-only changes, do not run Unity tests unless the user explicitly asks. Record the verification as documentation-only review.

## Approval Rule

Every gameplay feature or subsystem must be discussed with and confirmed by the user before implementation starts.

Work in small, reviewable increments. Propose one focused step, state the files or systems it would touch, wait for confirmation, then implement only that confirmed scope.

Documentation cleanup, verification, and narrow bug fixes do not need separate gameplay approval unless they change runtime behavior or subsystem scope.

## Progress Map Rule

Maintain `progress_map.md` at the repository root.

At the end of every confirmed implementation step or meaningful maintenance pass, update `progress_map.md` with:

- what was worked on
- what files or systems changed
- what behavior was added or intentionally deferred
- what tests or verification were run
- current status and the next proposed step

Keep current phase/step status accurate. Do not mark a future gameplay step as started unless the user has confirmed it.

Keep `progress_map.md` updates concise. For implementation work, update it once before the checkpoint commit so the implementation and progress record land together. Do not create a second progress-map-only commit just to record the hash of the commit that was just made; report the hash and push result in the final response instead.

## GitHub Checkpoint Rule

After a confirmed implementation step is finished, verified, and recorded in `progress_map.md`, create one Git checkpoint and push it to the project remote when Git is healthy. Documentation-only maintenance does not need a checkpoint or push unless the user explicitly asks.

Project remote:

```text
origin https://github.com/Erlavush/Constructed.git
```

Checkpoint workflow:

1. Review changed files. If `git status --short` fails with the local `sh.exe` error, use `git diff-index --name-status HEAD --` and related fallback commands.
2. Stage only files that belong to the completed step.
3. Never stage or push `Assets/PrivateTemp/Create` or copied Create assets.
4. Commit with a clear message, including the already-updated `progress_map.md` in the same commit.
5. Push to `origin` on the current branch.
6. Report the commit hash and push result in the final response. Do not make a follow-up metadata-only commit solely to record that hash.

Do not force-push, rewrite history, reset the worktree, or revert unrelated user changes unless explicitly asked.
