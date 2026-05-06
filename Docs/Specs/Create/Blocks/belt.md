# create:belt

## Purpose
Multi-block kinetic transport line for items and entities.

## Player-Facing Behavior
- Forms a belt chain between compatible shaft endpoints.
- Moves items along the chain and can also carry entities.
- Belt segments can be straight, sloped, vertical, or pulley-linked depending on endpoint geometry.

## Placement And State
- Uses `facing`, `slope`, `part`, `casing`, and `waterlogged`.
- `slope` includes `horizontal`, `upward`, `downward`, `vertical`, and `sideways`.
- `part` includes `start`, `middle`, `end`, and `pulley`.
- The connector item places belts by converting the path between two compatible shafts into belt segments.

## World Interaction
- Belts are controller-based: segments point at one controller block entity and share transport state.
- Live belt behavior depends on slope, part, and facing.
- Belt blocks can transport entities through passenger tracking and transported items through `BeltInventory`.
- Belt segments can gain or lose casing state.
- Connector validation requires compatible shaft endpoints, a clear/replaceable path, matching shaft axis, and non-conflicting speed signs when both shafts are already moving.

## Data / Block Entity
- Stores controller position and shared transport inventory state.
- Segment block entities reference one `BeltInventory` through the controller.
- Item transport uses offset-based positions along the controller belt chain rather than per-segment standalone inventories.
- Default max belt length comes from config and is `20` blocks.

## Ponder Notes
- The current Ponder reference teaches belts through the `belt_connector` component rather than a separate `belt` registration.
- Ponder teaches four valid orientation families:
- horizontal
- diagonal
- vertical
- vertical shafts connected horizontally
- It explicitly says belts cannot connect in arbitrary directions.
- It explicitly says belts can span lengths from `2` to `20` blocks.
- Ponder teaches that moving belts transport items and other entities.
- It also teaches that right-clicking with an empty hand can take items off a belt.
- The belt tutorial scenes also show mid-belt shafts, same-speed/same-direction relay, wrench removal of added shafts, and aesthetic dyeing.

## Current Unity Status
- Unity currently has the belt block id, placeholder placements in the demo world, and reference asset coverage for belt visuals.
- Unity does not yet render the world belt chain from real belt state fan-out, and it does not simulate controller chains or transport.

## Not Implemented Yet
- State-driven world belt visuals for start/middle/end/pulley and slope variants.
- Belt controller logic, shared transport inventory, and item/entity transport.
- Real belt placement behavior from the belt connector item.

## Source Anchors
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/belt/BeltBlock.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/belt/BeltBlockEntity.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/belt/BeltHelper.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/belt/item/BeltConnectorItem.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/belt/item/BeltConnectorHandler.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/AllBlocks.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/config/CKinetics.java`
- `References/Create-mc1.21.1-dev/src/generated/resources/assets/create/blockstates/belt.json`

## Ponder Anchors
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/ponder/AllCreatePonderScenes.java` (belt system taught through `AllItems.BELT_CONNECTOR`)
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/ponder/scenes/BeltScenes.java` (`beltConnector`, `directions`, `transport`, `beltsCanBeEncased`)
