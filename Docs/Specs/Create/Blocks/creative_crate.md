# create:creative_crate

## Purpose
Infinite logistics source crate.

## Player-Facing Behavior
- Supplies unlimited copies of one configured item to attached Create logistics systems.
- Uses a crate-shaped directional block; the model is the same for all six facings.

## Placement And State
- Directional block with `facing=up|down|north|south|east|west`.
- Wrenchable through the shared crate base block.

## World Interaction
- Exposes a block item-handler capability through its block entity.
- The creative handler is bottomless: slot `0` returns the configured item at max stack size, and extraction returns up to the requested amount.
- The handler does not store inserts; `insertItem()` returns `ItemStack.EMPTY`, so callers see the insert as accepted rather than accumulated.
- Registered as mounted item storage for contraption storage behavior.

## Data / Block Entity
- Stores a filter item through `FilteringBehaviour`; that filter defines the supplied item.
- Uses no finite inventory count.
- Clearing the block entity clears the filter.

## Ponder Notes
- No dedicated `creative_crate` storyboard is registered in the current `AllCreatePonderScenes` reference.
- Future agents should treat this spec and the source anchors as the current primary guidance for Creative Crate behavior.

## Current Unity Status
- Unity currently represents it as a facing block with state-driven visuals in `SampleScene`.
- Unity does not yet simulate the configured infinite supply behavior.

## Not Implemented Yet
- Real extraction into belts, funnels, and other logistics targets.
- Any contraption-mounted storage behavior.

## Source Anchors
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/logistics/crate/CreativeCrateBlock.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/logistics/crate/CreativeCrateBlockEntity.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/logistics/crate/CrateBlock.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/logistics/crate/BottomlessItemHandler.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/AllBlocks.java`
- `References/Create-mc1.21.1-dev/src/generated/resources/assets/create/blockstates/creative_crate.json`

## Ponder Anchors
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/ponder/AllCreatePonderScenes.java` (no dedicated `creative_crate` registration found)
