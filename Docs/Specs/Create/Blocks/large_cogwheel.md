# Large Cogwheel

## Source Audit

- Block/item Java class: `content/kinetics/simpleRelays/CogWheelBlock.java`, `CogwheelBlockItem.java`, `ICogWheel.java`.
- Block entity Java class: `content/kinetics/simpleRelays/SimpleKineticBlockEntity.java`, `BracketedKineticBlockEntity.java`.
- Renderer/visual Java class: `BracketedKineticBlockEntityRenderer.java`, `BracketedKineticBlockEntityVisual.java`, `KineticBlockEntityRenderer.java`, `KineticBlockEntityVisual.java`.
- Blockstate JSON: `src/generated/resources/assets/create/blockstates/large_cogwheel.json`.
- Model JSON files, including parent models: `models/block/large_cogwheel.json`, `models/block/large_cogwheel_shaftless.json`, `models/block/cogwheel_shaft.json`, `models/block/large_wheels.json`, `models/item/large_cogwheel.json`.
- Partial model registrations: `AllPartialModels.SHAFTLESS_LARGE_COGWHEEL`, `COGWHEEL_SHAFT`.
- Sprite-shift registrations: not found for uncased large cogwheel.
- Texture files: `textures/block/large_cogwheel.png`, `textures/block/cogwheel_axis.png`, `textures/block/axis_top.png`, plus `minecraft:block/stripped_spruce_log` particle.
- Ponder scene: no dedicated large-cogwheel scene found in the current audit; gearbox Ponder references cog/shaft usage.

## Ported Slice

- Registers `create:large_cogwheel` with an `axis` state matching the generated blockstate variants. The shared rotated-pillar default is `axis=y` from `RotatedPillarKineticBlock`.
- Kinetic propagation mirrors the relevant `RotationPropagator` gear ratios:
  - large cog to small cog: `-2`.
  - small cog to large cog: `-0.5`.
  - large cog to perpendicular large cog on a valid diagonal: `-1` or `1` from the source/target axis diff signs.
- Large cog propagation locations follow `SimpleKineticBlockEntity.addPropagationLocations`: every offset with squared distance `2`.
- Runtime visual mirrors `BracketedKineticBlockEntityRenderer`: the shaftless large cog rotates as one partial, and the central `cogwheel_shaft` rotates as a second partial. The gear offset uses `22.5` degrees on alternating positions and `11.25` otherwise; the shaft offset uses `22.5` on alternating positions and `0` otherwise.

## Deferred

- Waterlogging, encasing/brackets, survival-breaking updates, placement helper offset previews, particles, sound, stress, and advancement logic are deferred.
