# Cogwheel

## Source Audit

- Block/item Java class: `content/kinetics/simpleRelays/CogWheelBlock.java`, `CogwheelBlockItem.java`, `ICogWheel.java`.
- Block entity Java class: `content/kinetics/simpleRelays/SimpleKineticBlockEntity.java`, `BracketedKineticBlockEntity.java`.
- Renderer/visual Java class: `BracketedKineticBlockEntityRenderer.java`, `BracketedKineticBlockEntityVisual.java`, `KineticBlockEntityRenderer.java`, `KineticBlockEntityVisual.java`.
- Blockstate JSON: `src/generated/resources/assets/create/blockstates/cogwheel.json`.
- Model JSON files, including parent models: `models/block/cogwheel.json`, `models/block/cogwheel_shaftless.json`, `models/block/cogwheel_shaft.json`, `models/block/large_wheels.json`, `models/item/cogwheel.json`.
- Partial model registrations: `AllPartialModels.COGWHEEL`, `SHAFTLESS_COGWHEEL`, `COGWHEEL_SHAFT`.
- Sprite-shift registrations: not found for uncased cogwheel.
- Texture files: `textures/block/cogwheel.png`, `textures/block/cogwheel_axis.png`, `textures/block/axis_top.png`, plus `minecraft:block/stripped_spruce_log_top` particle.
- Ponder scene: no dedicated cogwheel scene found in the current audit; gearbox Ponder references cog/shaft usage.

## Ported Slice

- Registers `create:cogwheel` with an `axis` state matching the generated blockstate variants. The shared rotated-pillar default is `axis=y` from `RotatedPillarKineticBlock`.
- Kinetic propagation mirrors the relevant `RotationPropagator` rules for shaft-axis connections and small-cog to small-cog side meshing: same-axis small cogs adjacent perpendicular to their axis transfer `-1` speed.
- Small cog diagonal propagation locations follow `KineticBlockEntity.addPropagationLocations`: offsets with squared distance `2` and zero component along the cog axis.
- Runtime visual uses the blockstate model and rotates the whole cog around its axis. Rotation offset follows `KineticBlockEntityVisual.rotationOffset`: `22.5` degrees on alternating positions, otherwise `0`.

## Deferred

- Waterlogging is noted from `AbstractShaftBlock` but not yet represented in the current C# shaft/cog runtime state because the existing shaft slice also omits it and the generated visual variants are keyed only by `axis`.
- Encasing/brackets, advanced item placement helper behavior, survival-breaking updates, particles, sound, stress, and advancement logic are deferred.
