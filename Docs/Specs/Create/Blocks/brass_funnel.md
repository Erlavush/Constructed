# create:brass_funnel

## Purpose
Filtered Create funnel for item transfer, with the brass-specific filtering features enabled.

## Player-Facing Behavior
- Attaches to blocks in any direction and either extracts from the attached inventory or collects into it.
- Default placement is extracting; placing while sneaking flips it to collecting.
- Redstone power pauses transfer.
- Brass funnels support item filtering, and extractor modes can use counted filters.

## Placement And State
- Uses `facing=up|down|north|south|east|west`.
- Uses `extracting`, `powered`, and `waterlogged` state keys.
- Horizontal and vertical orientations use different generated models.
- Can convert to an equivalent brass belt funnel form when used over belts.

## World Interaction
- `determineCurrentMode()` resolves to `PAUSED`, `COLLECT`, `EXTRACT`, `PUSHING_TO_BELT`, or `TAKING_FROM_BELT` from the live block state.
- Power pauses the funnel and resets extraction cooldown.
- Non-belt extractor funnels run on the logistics extraction timer; default config is `8` ticks between transfers when not re-triggered by redstone.
- Upward non-extracting funnels can accept direct belt input.
- Brass funnels emit flap feedback and can award funnel-related advancements on transfer.

## Data / Block Entity
- Stores filter data through `FilteringBehaviour`.
- Uses inventory-manipulation behavior and inventory-version tracking.
- Clearing the block entity clears the filter.

## Ponder Notes
- Ponder first teaches funnels as inventory transfer components that move items to and from inventories.
- Ponder teaches directionality rules explicitly:
- normal placement means the funnel pulls from the attached inventory
- sneaking during placement flips it to inserting into the attached inventory
- wrenching flips `extracting` after placement
- Ponder teaches belt funnels depend on belt movement direction for inserting vs extracting.
- Ponder teaches redstone power pauses any funnel action.
- The dedicated brass-funnel scene teaches the brass upgrade:
- andesite funnels only extract single items
- brass funnels can extract up to a full stack
- the value panel can precisely control extracted stack size
- the filter slot can restrict transfers to matching items only
- Ponder also teaches that funnels do not directly transfer between two closed inventories; chutes are suggested for that case.

## Current Unity Status
- Unity currently renders brass funnel world visuals from `facing`, with visual defaults for `extracting=false`, `powered=false`, and `waterlogged=false`.
- Unity does not yet simulate transfer, filtering, redstone pause, or belt-funnel behavior.

## Not Implemented Yet
- Real inventory extraction and collection.
- Counted filter behavior.
- Belt funnel conversion and transport interaction.
- Redstone-driven cooldown and pause behavior.

## Source Anchors
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/logistics/funnel/AbstractFunnelBlock.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/logistics/funnel/FunnelBlock.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/logistics/funnel/FunnelBlockEntity.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/logistics/funnel/BrassFunnelBlock.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/AllBlocks.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/config/CLogistics.java`
- `References/Create-mc1.21.1-dev/src/generated/resources/assets/create/blockstates/brass_funnel.json`

## Ponder Anchors
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/ponder/AllCreatePonderScenes.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/ponder/scenes/FunnelScenes.java` (`intro`, `directionality`, `redstone`, `brass`, `transposer`)
