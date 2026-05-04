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

The project is no longer blank. Current implementation is in Phase 1 core foundations:

- `Constructed.Core`: resource ids, grid math, registries, tags, deterministic tick clock.
- `Constructed.Minecraft`: block definitions, immutable block states, in-memory world storage, lifecycle callbacks, neighbor updates.
- `Constructed.Tests.EditMode`: focused tests for the current foundation code.

Read `progress_map.md` before starting work. It is the source of truth for current phase, completed steps, verification status, and the next proposed step.

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

Build Minecraft-like foundations before Create machines:

1. Core primitives: `ResourceLocation`, registries, tags, `BlockPos`, `Direction`, `Axis`, deterministic tick clock.
2. Block model: block definitions, immutable block states, world/chunk storage, neighbor updates, scheduled ticks.
3. Items and persistence: item definitions, item stacks, inventories, item entities, save/load.
4. Block entities: lifecycle, behavior composition, typed behavior lookup, serialization, lazy ticks.
5. Data import: enough Minecraft/Create JSON for blocks, items, tags, recipes, blockstates, models, and textures.
6. Minimal Unity presentation: block mesh display, item display, debug overlays, selection, placement/removal.
7. Kinetics: shafts, cogwheels, creative motor, gearbox, propagation, networks, stress.
8. Belts: belt chain/controller/index/length, kinetic propagation, transported item stacks.
9. Processing and logistics: recipes, depot/belt processing, funnels, tunnels, filters.

Large systems such as trains, contraptions, fluids, schematic workflows, fake-player deployers, worldgen, tutorials, and multiplayer should wait until the foundations they depend on are stable.

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

If Unity batchmode compiles but does not produce a test results XML, record that exact limitation. A successful script assembly build is not the same as a completed test run.

Use compile-only fallback only when Unity Test Runner is blocked, and record which assemblies/files were checked and why full tests were not run.

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

## GitHub Checkpoint Rule

After a confirmed implementation step is finished, verified, and recorded in `progress_map.md`, create a Git checkpoint and push it to the project remote when Git is healthy.

Project remote:

```text
origin https://github.com/Erlavush/Constructed.git
```

Checkpoint workflow:

1. Review changed files. If `git status --short` fails with the local `sh.exe` error, use `git diff-index --name-status HEAD --` and related fallback commands.
2. Stage only files that belong to the completed step.
3. Never stage or push `Assets/PrivateTemp/Create` or copied Create assets.
4. Commit with a clear message.
5. Push to `origin` on the current branch.
6. Record the commit hash and push result in `progress_map.md`.

Do not force-push, rewrite history, reset the worktree, or revert unrelated user changes unless explicitly asked.
