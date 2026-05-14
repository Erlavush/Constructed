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

            return blockId == catalog.CreativeMotor.Id ||
                blockId == catalog.Shaft.Id ||
                blockId == catalog.Cogwheel.Id ||
                blockId == catalog.LargeCogwheel.Id ||
                blockId == catalog.Gearbox.Id ||
                blockId == catalog.Surface.Id;
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

            if (blockId == catalog.Cogwheel.Id)
            {
                Axis axis = ResolveCogwheelAxis(catalog, world, position, nearestLookingDirection, false);
                return catalog.Cogwheel.DefaultState.With(DemoContentCatalog.AxisProperty, axis);
            }

            if (blockId == catalog.LargeCogwheel.Id)
            {
                Axis axis = ResolveCogwheelAxis(catalog, world, position, nearestLookingDirection, true);
                return catalog.LargeCogwheel.DefaultState.With(DemoContentCatalog.AxisProperty, axis);
            }

            if (blockId == catalog.Gearbox.Id)
                return catalog.Gearbox.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.Y);

            if (blockId == catalog.Surface.Id)
                return catalog.Surface.DefaultState;

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

        public static Axis ResolveCogwheelAxis(
            DemoContentCatalog catalog,
            BlockWorld world,
            BlockPos position,
            Direction nearestLookingDirection,
            bool large)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (world == null)
                throw new ArgumentNullException(nameof(world));

            Axis? preferredAxis = GetPreferredAxis(catalog, world, position);
            if (preferredAxis.HasValue && CogWheelBlock.IsValidCogwheelPosition(large, catalog, world, position, preferredAxis.Value))
                return preferredAxis.Value;

            Axis lookAxis = nearestLookingDirection.Axis();
            if (CogWheelBlock.IsValidCogwheelPosition(large, catalog, world, position, lookAxis))
                return lookAxis;

            foreach (Axis axis in new[] { Axis.X, Axis.Y, Axis.Z })
            {
                if (CogWheelBlock.IsValidCogwheelPosition(large, catalog, world, position, axis))
                    return axis;
            }

            throw new InvalidOperationException($"No valid {(large ? "large " : string.Empty)}cogwheel axis exists at {position}.");
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
            if (id == catalog.Cogwheel.Id || id == catalog.LargeCogwheel.Id)
                return state.Get(DemoContentCatalog.AxisProperty) == face.Axis();
            if (id == catalog.Gearbox.Id)
                return state.Get(DemoContentCatalog.AxisProperty) != face.Axis();
            if (id == catalog.CreativeMotor.Id)
                return state.Get(DemoContentCatalog.FacingProperty) == face;

            if (id == catalog.Belt.Id)
            {
                DemoBeltPart part = state.Get(DemoContentCatalog.BeltPartProperty);
                if (part == DemoBeltPart.Middle)
                    return false;
                
                Axis rotationAxis = DemoBeltRuntimeResolver.GetRotationAxis(state, catalog);
                return rotationAxis == face.Axis();
            }

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
