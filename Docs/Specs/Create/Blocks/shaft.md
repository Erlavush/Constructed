# create:shaft

## Purpose
Basic straight kinetic relay shaft.

## Player-Facing Behavior
- Connects rotational power straight through its axis.
- Can be placed as a simple standalone relay or extended in straight lines.

## Placement And State
- Uses `axis=x|y|z`.
- Supports waterlogging through the shared shaft base.
- Placement helper logic extends straight shaft runs and also recognizes powered shafts as compatible extension targets.

## World Interaction
- `hasShaftTowards()` is true only when the queried face axis matches the shaft axis.
- `getRotationAxis()` is the block state's `axis`.
- Registered with zero stress impact in Create's block registration.
- Can be encased or combined with compatible shaft-adjacent variants such as the metal girder encased shaft path.

## Data / Block Entity
- Uses the shared kinetic block-entity type from the shaft base class.
- No per-block custom data beyond axis and the shared kinetic network data.

## Ponder Notes
- Ponder teaches the shaft as the basic straight-line rotational relay.
- The explicit lesson is that shafts relay rotation in a straight line.
- A second shaft-related scene teaches encasing with andesite or brass casing as a decorative transformation, not a separate kinetic rule.

## Current Unity Status
- Unity currently represents the shaft axis correctly in content registration, visual catalogs, and world visuals.
- Unity does not yet simulate kinetic propagation or network behavior.

## Not Implemented Yet
- Real kinetic speed propagation, source/sink behavior, and stress accounting.
- Extension helper behavior for live placement.

## Source Anchors
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/simpleRelays/AbstractShaftBlock.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/simpleRelays/ShaftBlock.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/AllBlocks.java`
- `References/Create-mc1.21.1-dev/src/generated/resources/assets/create/blockstates/shaft.json`

## Ponder Anchors
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/ponder/AllCreatePonderScenes.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/ponder/scenes/KineticsScenes.java` (`shaftAsRelay`, `shaftsCanBeEncased`)
