# create:belt_connector

## Purpose
Tool item used to define two shaft endpoints and create a mechanical belt chain between them.

## Player-Facing Behavior
- Acts as a two-click placement tool:
- click a first shaft to lock endpoint A
- click a second compatible shaft to attempt creation
- Sneak-use clears endpoint A without placing.
- While endpoint A is stored, client preview particles are emitted:
- green when the current candidate can connect
- red when current candidate cannot connect.
- On success in Create:
- the chain is converted to belt blocks with computed `part/slope/facing`
- one connector item is consumed (unless player is in creative mode)
- endpoint A data is cleared and a short item cooldown is applied.

## Placement And State
- Acts as a `BlockItem` for `create:belt`.
- Stores first endpoint in `AllDataComponents.BELT_FIRST_SHAFT`.
- Create clears invalid stored endpoints automatically when they no longer validate or exceed distance checks before second click handling.
- On successful chain creation, each placed belt receives:
- `facing` from endpoint geometry
- `slope` from endpoint vertical relation (`horizontal|upward|downward|vertical`, potentially switched to `sideways` by vertical pulley logic)
- `part` per segment (`start|middle|end|pulley`)
- `casing=false`, `waterlogged` resolved through world water helper.

## World Interaction
- Endpoint validation rules (`validateAxis` + `canConnect`):
- both endpoints must be loaded shafts
- endpoint distance must be within `maxBeltLength` (default `20`)
- shaft axes must match
- the endpoint difference must pass Create’s geometry family rule (`sames == 1`) and keep the shaft-axis component at `0`
- for `axis=Y`, diagonal X+Z offsets are explicitly rejected
- if both endpoint theoretical speeds are non-zero, speed signs must match
- intermediate positions may be aligned shafts on the same axis; other non-replaceable blocks reject connection.
- Placement loop behavior:
- creates chain with start/middle/end
- upgrades middle aligned shafts to pulley parts
- if a pulley is a vertical-axis shaft, slope mutates to `sideways` for subsequent placed segments in that loop.

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
- Unity now implements a first source-backed connector slice:
- selectable/usable `create:belt_connector` in the build inventory
- first-click shaft lock state in controller
- sneak + click cancellation of stored first endpoint
- second-click connection attempt with Create-shaped geometry/speed-sign validation
- on success, world shafts/path are converted to belt block states
- red/green world-space preview particles while selecting the second endpoint.
- Deferred in Unity:
- full item-stack data component persistence (`BELT_FIRST_SHAFT` equivalent at item-stack level)
- full Create replaceability/destruction parity and sound/cooldown/advancement hooks
- controller-based belt runtime transport behavior.

## Not Implemented Yet
- Full server/client parity for connector interactions (cooldowns, sounds, advancements, waterlogging details, replaceability edge cases).
- Integration with future generalized kinetic network model beyond current focused resolver.
- Full belt controller-chain and transport simulation after placement.

## Source Anchors
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/belt/item/BeltConnectorItem.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/belt/item/BeltConnectorHandler.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/belt/BeltBlock.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/simpleRelays/ShaftBlock.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/AllItems.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/AllDataComponents.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/config/CKinetics.java`
- `References/Create-mc1.21.1-dev/src/generated/resources/assets/create/models/item/belt_connector.json`

## Ponder Anchors
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/ponder/AllCreatePonderScenes.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/ponder/scenes/BeltScenes.java` (`beltConnector`, `directions`, `transport`, `beltsCanBeEncased`)
