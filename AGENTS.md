# Constructed Agent Notes

## Project Intent

`Constructed` is a Unity/C# project for recreating Minecraft plus Create-mod-style mechanics in Unity.

**Mandate:** Build exclusively from direct study of the provided Minecraft and Create source code. Do not implement gameplay systems from memory, assumptions, or "AI judgement." Your role is to accurately translate the authoritative Java logic into C#.

## Project Structure & Key Directories

Maintain the authoritative gameplay simulation in plain C# data and services. Unity scene objects and MonoBehaviours are for presentation only.

- **`Assets/Scripts/Constructed.Core`**: Math primitives (`BlockPos`, `Direction`), `ResourceLocation`, Registries, Tags, and the `SimulationClock`.
- **`Assets/Scripts/Constructed.Minecraft`**: Authoritative logic for `BlockDefinition`, `BlockState`, `BlockWorld` (storage), `NeighborBlockChange`, `ScheduledBlockTick`, `InventoryContainer`, and `ItemStack`.
- **`Assets/Scripts/Constructed.Create`**: Create-specific logic: content definitions, kinetic systems, and the `DemoContentCatalog`.
- **`Assets/Scripts/Constructed.Unity`**: Unity presentation layer: `DemoVerticalSlicePresenter`, rendering logic, input, and scene glue.
- **`Assets/Tests/EditMode`**: Focused C# simulation tests for non-Unity logic.
- **`Z:\Constructed\Docs\Specs\Create`**: Official behavior specs derived from Java source.
- **`Z:\Constructed\References`**: Mandatory Java source and resource references.

## Mandatory: Unity MCP Server

**CRITICAL MANDATE:** You MUST use the `unityMCP` server for all interactions with the Unity Editor (scene inspection, console monitoring, type reflection, and test execution).

- **Hard Gate:** If the `unityMCP` server is unavailable, disconnected, or fails to initialize, you MUST NOT attempt to use shell-based batchmode commands (e.g., `Unity.com`) as a fallback.
- **Protocol:** If the server is missing, stop immediately and instruct the user to start the MCP server before any further work is performed.

## Strict Fidelity Mandate (Java to C#)

**Logic Authority:** All gameplay mechanics, block behaviors, kinetic propagation, and logistical rules MUST be derived directly from the provided reference source:
- `Z:\Constructed\References\Create-mc1.21.1-dev`
- `Z:\Constructed\References\Minecraft-1.21.1-sources`

### Rules for Implementation:
1. **No Assumptions:** Never implement behavior from general knowledge. If the reference code handles a state change, neighbor update, or kinetic calculation in a specific way, you MUST replicate that logic's intent.
2. **Translation, Not Re-imagining:** Do not "improve" the logic. Port the authoritative Java logic into the `Constructed` architecture accurately.
3. **Reference Requirement:** Before implementing or modifying a block/item, you MUST read the corresponding Java class and resource files to confirm the behavior contract.

## Reference Source Locations

Use these folders as the primary local reference set:
- `Minecraft-1.21.1-sources`: Decompiled vanilla source for mechanics and control flow.
- `Minecraft-1.21.1-resources`: Vanilla assets (models, textures, data).
- `Create-mc1.21.1-dev`: Authoritative Create mod source code.

## Block And Item Specs

Source-backed per-block and per-item notes live under:
`Z:\Constructed\Docs\Specs\Create`

When implementing or changing gameplay behavior:
1. Read the spec file first.
2. If source study (Java) changes the understanding, update the spec before or with the gameplay change.
3. If a spec doesn't exist, create it from the Java source before implementing behavior.

## Verification and Testing

All verification must be performed via the live MCP connection:
1. **Compilation Check:** After any code change, verify the editor state via `mcpforunity://editor/state`. Wait for `isCompiling` to be false and `advice.ready_for_tools` to be true.
2. **Console Validation:** Always call `read_console` (types: ["error"]) after a recompile to ensure no new errors were introduced.
3. **Automated Testing:** Trigger and monitor tests using `run_tests` and `get_test_job`.

## Private Asset Rule

Copied Create assets may be used only in the ignored private folder:
`Assets/PrivateTemp/Create`

**NEVER** commit, publish, or redistribute copied Create textures, models, blockstates, sounds, or other assets. Preserve source structure when copying reference assets needed for a task.

## Progress Map Rule

Maintain `progress_map.md` at the repository root.
At the end of every step, update it with:
- Systems/files changed.
- Behavior added or deferred.
- Verification results (via MCP).
- Current status and the next proposed step.

## GitHub Checkpoint Rule

After a step is verified and recorded in `progress_map.md`, create one Git checkpoint.
1. Stage only files belonging to the completed step.
2. **NEVER** stage `Assets/PrivateTemp/Create`.
3. Commit with a clear message and push to `origin`.
