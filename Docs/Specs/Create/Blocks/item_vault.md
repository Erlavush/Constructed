# create:item_vault

## Purpose
High-capacity item storage that can merge into a multiblock vault.

## Player-Facing Behavior
- Stores items as a logistics inventory.
- Adjacent vault blocks can merge into a larger controller-based multiblock.
- Placing a vault block against the end face of an existing wide vault can auto-place matching blocks to extend the full face.

## Placement And State
- Uses `axis=x|z` and `large=true|false`.
- `large` is not a separate storage mode; it is the visual state used when the connected vault radius is larger than `2`.
- Placement chooses a horizontal axis, preferring the clicked face when that face axis is horizontal.
- Wrenching can break the large visual state and then reorient the block.

## World Interaction
- Uses a block entity with item capability exposure.
- Breaking a vault drops its contents.
- Neighbor changes can trigger connectivity recalculation.
- Multiblock controller logic tracks controller position, width/radius, and length; controller length cap is `radius * 3`.

## Data / Block Entity
- Per-block capacity comes from config; default is `20` stacks per block.
- The controller combines per-block handlers into one logical inventory wrapper.
- Persistent data includes controller position, size/radius, length, and inventory contents.
- The item form strips controller/size/length tags before applying block-entity data on placement.

## Ponder Notes
- Ponder teaches Item Vaults as large automation-facing storage.
- It explicitly says contents cannot be added or taken manually.
- It explicitly shows automation components inserting into and extracting from the vault.
- Ponder teaches the multiblock sizing rules:
- vaults can combine to increase capacity
- the base square can be up to `3` blocks wide
- length can grow up to `3x` the diameter

## Current Unity Status
- Unity currently implements only a single-block Item Vault inventory with `20` slots and matching block visuals.
- Unity bridges the runtime `horizontal_axis` property to the reference blockstate `axis` key for visuals.

## Not Implemented Yet
- Real multiblock controller formation.
- Shared multiblock inventory capability.
- Auto-multi-place behavior from the item form.
- Connected-texture and large-vault visual behavior driven from real connectivity.

## Source Anchors
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/logistics/vault/ItemVaultBlock.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/logistics/vault/ItemVaultBlockEntity.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/logistics/vault/ItemVaultItem.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/AllBlocks.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/config/CLogistics.java`
- `References/Create-mc1.21.1-dev/src/generated/resources/assets/create/blockstates/item_vault.json`

## Ponder Anchors
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/ponder/AllCreatePonderScenes.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/ponder/scenes/ItemVaultScenes.java` (`storage`, `sizes`)
