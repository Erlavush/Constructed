# create:belt_connector

## Purpose
Tool item used to define two shaft endpoints and create a mechanical belt chain between them.

## Player-Facing Behavior
- First use on a valid shaft stores the first endpoint in the held item.
- Second use on a compatible second shaft creates the belt chain between the two endpoints.
- Sneak-use clears the stored first endpoint.
- On successful belt creation, one connector item is consumed unless the player is in creative mode.

## Placement And State
- Acts as a `BlockItem` for `create:belt`.
- Stores the first selected shaft position in the `BELT_FIRST_SHAFT` item data component.
- The created belt segments receive `slope`, `part`, and `horizontal_facing` based on the endpoint geometry.

## World Interaction
- Valid endpoints must be shafts, loaded, within the configured max belt length, and share a compatible shaft axis.
- The path between endpoints must be replaceable, except for same-axis shafts already in the path.
- If both endpoints are already rotating, their theoretical speed signs must not conflict.
- Creation logic marks start, middle, end, and pulley belt segments while converting the path to belt blocks.

## Data / Block Entity
- Uses the held item's custom data to remember the first selected shaft position.
- Does not itself own a block entity.

## Ponder Notes
- Ponder explicitly teaches the two-click workflow:
- right-click the first shaft to mark it
- right-click the second shaft to create the belt
- It explicitly teaches that accidental selections can be canceled by right-clicking while sneaking.
- It shows that additional shafts can be added throughout the belt.
- It shows that shafts connected via belts rotate with identical speed and direction.
- It shows that added shafts can be removed with the wrench.
- It also shows that belts created this way can be dyed for aesthetics.
- The same belt tutorial set also teaches valid belt orientations and item/entity transport behavior.

## Current Unity Status
- Unity currently previews the item visually only.
- Unity does not yet implement endpoint selection, path validation, or belt creation.

## Not Implemented Yet
- Interactive two-click belt placement in Unity.
- Validation against a future kinetic network.
- Automatic conversion of the selected shaft path into controller-linked belt segments.

## Source Anchors
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/belt/item/BeltConnectorItem.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/belt/item/BeltConnectorHandler.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/AllItems.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/config/CKinetics.java`
- `References/Create-mc1.21.1-dev/src/generated/resources/assets/create/models/item/belt_connector.json`

## Ponder Anchors
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/ponder/AllCreatePonderScenes.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/ponder/scenes/BeltScenes.java` (`beltConnector`, `directions`, `transport`, `beltsCanBeEncased`)
