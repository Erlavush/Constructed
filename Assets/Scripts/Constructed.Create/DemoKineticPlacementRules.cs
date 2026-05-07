using System;
using Constructed.Core;
using Constructed.Minecraft;

namespace Constructed.Create
{
    public static class DemoKineticPlacementRules
    {
        private static readonly Direction[] PlacementDirections =
        {
            Direction.Down,
            Direction.Up,
            Direction.North,
            Direction.South,
            Direction.West,
            Direction.East
        };

        public static bool IsPlaceableBlock(DemoContentCatalog catalog, ResourceLocation blockId)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            return blockId == catalog.CreativeMotor.Id || blockId == catalog.Shaft.Id;
        }

        public static BlockState CreatePlacementState(
            DemoContentCatalog catalog,
            BlockWorld world,
            ResourceLocation blockId,
            BlockPos position,
            Direction nearestLookingDirection)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (world == null)
                throw new ArgumentNullException(nameof(world));

            if (blockId == catalog.CreativeMotor.Id)
            {
                Direction facing = ResolveCreativeMotorFacing(catalog, world, position, nearestLookingDirection);
                return catalog.CreativeMotor.DefaultState.With(DemoContentCatalog.FacingProperty, facing);
            }

            if (blockId == catalog.Shaft.Id)
            {
                Axis axis = ResolveShaftAxis(catalog, world, position, nearestLookingDirection);
                return catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, axis);
            }

            throw new InvalidOperationException($"No placement rule exists for {blockId} in the current demo slice.");
        }

        public static Direction ResolveCreativeMotorFacing(
            DemoContentCatalog catalog,
            BlockWorld world,
            BlockPos position,
            Direction nearestLookingDirection)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (world == null)
                throw new ArgumentNullException(nameof(world));

            Direction? preferredFacing = GetPreferredFacing(catalog, world, position);
            return preferredFacing ?? nearestLookingDirection.Opposite();
        }

        public static Axis ResolveShaftAxis(
            DemoContentCatalog catalog,
            BlockWorld world,
            BlockPos position,
            Direction nearestLookingDirection)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (world == null)
                throw new ArgumentNullException(nameof(world));

            Axis? preferredAxis = GetPreferredAxis(catalog, world, position);
            return preferredAxis ?? nearestLookingDirection.Axis();
        }

        public static bool HasShaftTowards(DemoContentCatalog catalog, BlockWorld world, BlockPos position, Direction face)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (world == null)
                throw new ArgumentNullException(nameof(world));

            return HasShaftTowards(catalog, world.GetBlockState(position), face);
        }

        public static bool HasShaftTowards(DemoContentCatalog catalog, BlockState state, Direction face)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            ResourceLocation id = state.Definition.Id;
            if (id == catalog.Shaft.Id)
                return state.Get(DemoContentCatalog.AxisProperty) == face.Axis();
            if (id == catalog.CreativeMotor.Id)
                return state.Get(DemoContentCatalog.FacingProperty) == face;

            return false;
        }

        private static Direction? GetPreferredFacing(DemoContentCatalog catalog, BlockWorld world, BlockPos position)
        {
            Direction? preferredSide = null;
            foreach (Direction side in PlacementDirections)
            {
                BlockPos neighborPosition = position.Relative(side);
                if (!HasShaftTowards(catalog, world, neighborPosition, side.Opposite()))
                    continue;

                if (preferredSide.HasValue && preferredSide.Value.Axis() != side.Axis())
                    return null;

                preferredSide = side;
            }

            return preferredSide;
        }

        private static Axis? GetPreferredAxis(DemoContentCatalog catalog, BlockWorld world, BlockPos position)
        {
            Axis? preferredAxis = null;
            foreach (Direction side in PlacementDirections)
            {
                BlockPos neighborPosition = position.Relative(side);
                if (!HasShaftTowards(catalog, world, neighborPosition, side.Opposite()))
                    continue;

                Axis axis = side.Axis();
                if (preferredAxis.HasValue && preferredAxis.Value != axis)
                    return null;

                preferredAxis = axis;
            }

            return preferredAxis;
        }
    }
}
