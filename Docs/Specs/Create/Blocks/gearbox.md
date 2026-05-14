# Gearbox

## Source Audit

- Block/item Java class: `content/kinetics/gearbox/GearboxBlock.java`, `VerticalGearboxItem.java`.
- Block entity Java class: `content/kinetics/gearbox/GearboxBlockEntity.java`, `base/DirectionalShaftHalvesBlockEntity.java`.
- Renderer/visual Java class: `content/kinetics/gearbox/GearboxRenderer.java`, `GearboxVisual.java`, `base/KineticBlockEntityRenderer.java`.
- Blockstate JSON: `src/generated/resources/assets/create/blockstates/gearbox.json`.
- Model JSON files, including parent models: `models/block/gearbox/block.json`, `models/block/gearbox/item.json`, `models/block/gearbox/item_vertical.json`, `models/block/shaft_half.json`, `models/item/gearbox.json`.
- Partial model registrations: `AllPartialModels.SHAFT_HALF`.
- Sprite-shift registrations: not found for gearbox.
- Texture files: `textures/block/andesite_casing.png`, `textures/block/gearbox.png`, `textures/block/axis.png`, `textures/block/axis_top.png`.
- Ponder scene: `src/main/resources/assets/create/ponder/gearbox.nbt` is present; used only as player-facing context.

## Ported Slice

- Registers `create:gearbox` with an `axis` state. The shared rotated-pillar default and placement result are `axis=y`, matching `RotatedPillarKineticBlock` and `GearboxBlock.getStateForPlacement`.
- Shaft connectivity mirrors `GearboxBlock.hasShaftTowards`: a gearbox exposes shafts on every face whose axis is not the gearbox block axis.
- Axis transfer mirrors `RotationPropagator.getAxisModifier` for `GearboxBlockEntity`: same-axis opposite sides invert, perpendicular exits invert when their axis direction matches the source-facing axis direction.
- Runtime visual mirrors `GearboxRenderer`/`GearboxVisual`: the blockstate body renders statically, and `shaft_half` partials are added on every non-box-axis face with per-face signed speed.

## Deferred

- `uvlock=true` in the gearbox blockstate is recorded by the local blockstate loader but full vanilla UV-lock behavior is still deferred.
- Vertical gearbox item placement, casing connected textures, drops/clone-item differences, stress/sound, and block entity serialization are deferred.
