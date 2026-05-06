# create:andesite_alloy

## Purpose
Basic Create material item used as a common ingredient across early kinetic and logistics recipes.

## Player-Facing Behavior
- Functions as a normal inventory item, not as a placeable gameplay block in this first-slice scope.
- Appears as a standard generated item model.

## Placement And State
- No block placement behavior of its own.
- No custom item-state behavior is exposed in the current references used for this slice.

## World Interaction
- Used as a tagged Create ingot ingredient and recipe input.
- Has no special per-tick, transport, or capability behavior in the item registration used here.

## Data / Block Entity
- No block entity or custom persistent logic in the current first-slice references.

## Ponder Notes
- No dedicated `andesite_alloy` storyboard is registered in the current `AllCreatePonderScenes` reference.
- Future agents should treat recipe registrations, tags, and source usage as the current primary guidance for this item.

## Current Unity Status
- Unity currently uses it as a registered demo item and renders it in the item visual catalog.
- Unity does not yet implement crafting, mixing, or recipe consumption.

## Not Implemented Yet
- Recipe systems that consume it.
- Any broader material pipeline beyond registration and preview.

## Source Anchors
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/AllItems.java`
- `References/Create-mc1.21.1-dev/src/generated/resources/assets/create/models/item/andesite_alloy.json`
- `References/Create-mc1.21.1-dev/src/generated/resources/data/create/recipe/crafting/materials/andesite_alloy.json`
- `References/Create-mc1.21.1-dev/src/generated/resources/data/create/recipe/mixing/andesite_alloy.json`
- `References/Create-mc1.21.1-dev/src/generated/resources/data/create/tags/item/create_ingots.json`

## Ponder Anchors
- `References/Create-mc1.21.1-dev/src/main/java/com/simibubi/create/infrastructure/ponder/AllCreatePonderScenes.java` (no dedicated `andesite_alloy` registration found)
