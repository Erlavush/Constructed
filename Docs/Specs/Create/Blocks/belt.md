# create:belt

## Purpose
Multi-block kinetic transport line for items and entities.

## Player-Facing Behavior
- Belts are not placed as single independent blocks in normal usage; they are created by `create:belt_connector` after selecting two compatible shaft endpoints.
- A created chain visually and mechanically behaves as one lane with per-block start/middle/end/pulley parts.
- Belts transport item stacks and entities; extraction/insertion and processing behavior happens through belt-aware handlers (funnels, tunnels, crushers, depot-style interactions).
- Belt visuals can be uncased animated belts or cased static casings depending on `casing`.

## Placement And State
- Runtime state keys in Create blockstates:
- `facing` (`north|south|west|east`)
- `slope` (`horizontal|upward|downward|vertical|sideways`)
- `part` (`start|middle|end|pulley`)
- `casing` (`false|true`)
- `waterlogged` (`false|true`)
- Create default blockstate (constructor registration):
- `slope=horizontal`
- `part=start`
- `casing=false`
- `waterlogged=false`
- `facing` inherits the horizontal block default.
- Connector creation sets each chain block by position:
- start/end => `part=start` or `part=end`
- middle aligned shaft => `part=pulley`
- vertical-axis pulley in chain forces slope to `sideways` for that and subsequent segments in the placement loop.

## World Interaction
- A belt line resolves to a controller segment (`BeltBlockEntity`) plus indexed segments sharing one `BeltInventory`.
- Transport is offset-based along the controller chain, not per-block isolated inventory.
- Key interaction handlers:
- `BeltMovementHandler`: entities and passenger handling
- `BeltFunnelInteractionHandler`: insertion/extraction to funnels
- `BeltTunnelInteractionHandler`: branching/splitting with tunnels
- `BeltCrusherInteractionHandler`: crusher processing pass-through
- `BeltSlicer`: vertical slicing helper used by belt logic and visuals
- Connection constraints (from connector `canConnect`):
- both endpoints loaded and valid shafts
- endpoint distance within configured max length (`maxBeltLength`, default `20`)
- endpoint shaft axes equal
- endpoint difference must satisfy Create belt geometry family check (`sames == 1`, with shaft-axis component `0`)
- for `axis=Y`, diagonal X+Z offset is rejected
- if both endpoint theoretical speeds are non-zero, their speed signs must match
- intermediate path blocks must be replaceable, except aligned shafts are allowed as pass-through pulley candidates.

## Data / Block Entity
- `BeltBlockEntity` stores controller linkage, lane state, and update flags.
- `BeltInventory` stores transported stacks with belt offsets and processes movement each tick.
- Segment-to-controller indexing is authoritative for item and entity transport position.
- Max length source:
- `AllConfigs.server().kinetics.maxBeltLength` (default `20`, from `CKinetics`).

## Ponder Notes
- Ponder belt teaching is registered through `AllItems.BELT_CONNECTOR`.
- Belt scenes explicitly teach:
- two-endpoint connector workflow
- valid orientation families: horizontal, diagonal, vertical, and horizontal runs between vertical shafts
- length range `2..20`
- transport of items and entities
- extra shafts inside a belt line (pulley behavior)
- same speed/direction propagation expectation across a belt-connected run
- dye/casing visual customization and wrench edits.

## Current Unity Status
- Unity now has a source-shaped first connector slice:
- belt block definition exposes `facing/slope/part/casing/waterlogged`
- two-click connector path creation converts shaft chains into belt states
- path validation mirrors Create geometry + speed-sign checks at current slice scope
- red/green preview particles are emitted while selecting the second endpoint
- state-driven world bridge now emits belt visual properties for blockstate model resolution.
- Still deferred in Unity:
- full controller-chain block entity behavior (`BeltBlockEntity`/`BeltInventory` equivalent)
- animated UV scroll belt rendering parity and dyed scroll routing
- belt item/entity transport logic and handler integrations
- cased/uncased interaction toggles and water behavior.

## Not Implemented Yet
- Belt controller network with shared transport inventory and authoritative index math.
- Full Create parity for replaceability/destruction semantics during connection.
- Full belt renderer behavior (`BeltRenderer`/`BeltVisual`) including scroll animation material shifting and all dyed belt sprite shifts.
- Full processing/logistics interactions on moving stacks.

## Source Anchors
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/belt/BeltBlock.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/belt/BeltBlockEntity.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/belt/BeltInventory.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/belt/transport/BeltMovementHandler.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/belt/transport/BeltFunnelInteractionHandler.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/belt/transport/BeltTunnelInteractionHandler.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/belt/transport/BeltCrusherInteractionHandler.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/belt/BeltHelper.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/belt/item/BeltConnectorItem.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/belt/item/BeltConnectorHandler.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/AllBlocks.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/foundation/data/AssetLookup.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/AllSpriteShifts.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/belt/BeltRenderer.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/belt/BeltVisual.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/config/CKinetics.java`
- `References/Create-mc1.21.1-dev/src/generated/resources/assets/create/blockstates/belt.json`

## Ponder Anchors
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/ponder/AllCreatePonderScenes.java` (belt system taught through `AllItems.BELT_CONNECTOR`)
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/ponder/scenes/BeltScenes.java` (`beltConnector`, `directions`, `transport`, `beltsCanBeEncased`)
