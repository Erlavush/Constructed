# create:creative_motor

## Purpose
Configurable kinetic power source.

## Player-Facing Behavior
- Outputs rotational power from the face it points toward.
- Lets the player configure the generated rotation speed.

## Placement And State
- Uses `facing=up|down|north|south|east|west`.
- The shaft output side is the block's `facing`.
- Vertical and horizontal facings use different generated models.

## World Interaction
- `hasShaftTowards()` is true only on the facing side.
- `getRotationAxis()` is the axis of the facing direction.
- Registered as a kinetic generator with stress capacity `16384.0`.
- Registration also sets generator speed handling for the creative motor block.

## Data / Block Entity
- Stores a configurable generated speed.
- Default generated speed is `16`; max absolute speed is `256`.
- The stored speed is converted into signed output based on facing direction.
- Optionally exposes a ComputerCraft peripheral capability when that mod is present.

## Ponder Notes
- Ponder teaches the creative motor as a compact, configurable source of rotational force.
- It explicitly shows the input panels as the place where generated speed is configured.
- The scene demonstrates the configured speed increasing the connected kinetic speed after adjustment.

## Current Unity Status
- Unity currently represents the motor's facing and visuals in catalogs and world placement.
- Unity does not yet simulate configurable speed, generated rotation, or kinetic network output.

## Not Implemented Yet
- Scroll-value or UI configuration of motor speed.
- Real source behavior inside the future kinetic network.
- Any ComputerCraft integration.

## Source Anchors
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/motor/CreativeMotorBlock.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/motor/CreativeMotorBlockEntity.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/content/kinetics/motor/CreativeMotorGenerator.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/AllBlocks.java`
- `References/Create-mc1.21.1-dev/src/generated/resources/assets/create/blockstates/creative_motor.json`

## Ponder Anchors
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/ponder/AllCreatePonderScenes.java`
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/ponder/scenes/KineticsScenes.java` (`creativeMotor`)
