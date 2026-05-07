using System;
using System.Collections.Generic;
using Constructed.Core;
using Constructed.Minecraft;

namespace Constructed.Create
{
    public enum DemoBeltSlope
    {
        Horizontal,
        Upward,
        Downward,
        Vertical,
        Sideways
    }

    public enum DemoBeltPart
    {
        Start,
        Middle,
        End,
        Pulley
    }

    public enum DemoBeltConnectionFailureReason
    {
        None,
        InvalidFirstShaft,
        InvalidSecondShaft,
        TooLong,
        InvalidGeometry,
        AxisMismatch,
        VerticalDiagonalNotAllowed,
        ConflictingEndpointSpeedSign,
        BlockedPath
    }

    public readonly struct DemoBeltConnectionEvaluation
    {
        public DemoBeltConnectionEvaluation(
            bool canConnect,
            DemoBeltConnectionFailureReason failureReason,
            Axis shaftAxis,
            Direction facing,
            DemoBeltSlope slope,
            IReadOnlyList<BlockPos> chain)
        {
            CanConnect = canConnect;
            FailureReason = failureReason;
            ShaftAxis = shaftAxis;
            Facing = facing;
            Slope = slope;
            Chain = chain ?? Array.Empty<BlockPos>();
        }

        public bool CanConnect { get; }

        public DemoBeltConnectionFailureReason FailureReason { get; }

        public Axis ShaftAxis { get; }

        public Direction Facing { get; }

        public DemoBeltSlope Slope { get; }

        public IReadOnlyList<BlockPos> Chain { get; }
    }

    public static class DemoBeltPlacementService
    {
        public const int DefaultMaxBeltLength = 20;
        private const int ChainIterationLimit = 1000;

        public static bool ValidateShaftEndpoint(BlockWorld world, DemoContentCatalog catalog, BlockPos position)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            return TryGetShaftAxis(world.GetBlockState(position), catalog, out _);
        }

        public static DemoBeltConnectionEvaluation EvaluateConnection(
            BlockWorld world,
            DemoContentCatalog catalog,
            BlockPos firstShaft,
            BlockPos secondShaft,
            int maxLength = DefaultMaxBeltLength)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (maxLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxLength), "Belt max length must be positive.");

            if (!TryGetShaftAxis(world.GetBlockState(firstShaft), catalog, out Axis shaftAxis))
            {
                return new DemoBeltConnectionEvaluation(
                    false,
                    DemoBeltConnectionFailureReason.InvalidFirstShaft,
                    Axis.X,
                    Direction.East,
                    DemoBeltSlope.Horizontal,
                    Array.Empty<BlockPos>());
            }

            if (!TryGetShaftAxis(world.GetBlockState(secondShaft), catalog, out Axis secondAxis))
            {
                return new DemoBeltConnectionEvaluation(
                    false,
                    DemoBeltConnectionFailureReason.InvalidSecondShaft,
                    shaftAxis,
                    Direction.East,
                    DemoBeltSlope.Horizontal,
                    Array.Empty<BlockPos>());
            }

            if (!IsWithinBeltLength(firstShaft, secondShaft, maxLength))
            {
                return new DemoBeltConnectionEvaluation(
                    false,
                    DemoBeltConnectionFailureReason.TooLong,
                    shaftAxis,
                    Direction.East,
                    DemoBeltSlope.Horizontal,
                    Array.Empty<BlockPos>());
            }

            BlockPos diff = secondShaft - firstShaft;
            int x = diff.X;
            int y = diff.Y;
            int z = diff.Z;
            int sames = (Math.Abs(x) == Math.Abs(y) ? 1 : 0) +
                (Math.Abs(y) == Math.Abs(z) ? 1 : 0) +
                (Math.Abs(z) == Math.Abs(x) ? 1 : 0);

            if (Choose(shaftAxis, x, y, z) != 0 || sames != 1)
            {
                return new DemoBeltConnectionEvaluation(
                    false,
                    DemoBeltConnectionFailureReason.InvalidGeometry,
                    shaftAxis,
                    Direction.East,
                    DemoBeltSlope.Horizontal,
                    Array.Empty<BlockPos>());
            }

            if (shaftAxis != secondAxis)
            {
                return new DemoBeltConnectionEvaluation(
                    false,
                    DemoBeltConnectionFailureReason.AxisMismatch,
                    shaftAxis,
                    Direction.East,
                    DemoBeltSlope.Horizontal,
                    Array.Empty<BlockPos>());
            }

            if (shaftAxis == Axis.Y && x != 0 && z != 0)
            {
                return new DemoBeltConnectionEvaluation(
                    false,
                    DemoBeltConnectionFailureReason.VerticalDiagonalNotAllowed,
                    shaftAxis,
                    Direction.East,
                    DemoBeltSlope.Horizontal,
                    Array.Empty<BlockPos>());
            }

            DemoKineticSnapshot kinetic = DemoKineticResolver.Resolve(world, catalog);
            float speedFirst = kinetic.TryGet(firstShaft, out DemoKineticComponentState firstState) ? firstState.Speed : 0f;
            float speedSecond = kinetic.TryGet(secondShaft, out DemoKineticComponentState secondState) ? secondState.Speed : 0f;
            if (HasConflictingSpeedSign(speedFirst, speedSecond))
            {
                return new DemoBeltConnectionEvaluation(
                    false,
                    DemoBeltConnectionFailureReason.ConflictingEndpointSpeedSign,
                    shaftAxis,
                    Direction.East,
                    DemoBeltSlope.Horizontal,
                    Array.Empty<BlockPos>());
            }

            BlockPos step = new BlockPos(Sign(x), Sign(y), Sign(z));
            for (BlockPos current = firstShaft + step; current != secondShaft; current += step)
            {
                BlockState state = world.GetBlockState(current);
                if (TryGetShaftAxis(state, catalog, out Axis axis) && axis == shaftAxis)
                    continue;
                if (world.IsAir(state))
                    continue;

                return new DemoBeltConnectionEvaluation(
                    false,
                    DemoBeltConnectionFailureReason.BlockedPath,
                    shaftAxis,
                    Direction.East,
                    DemoBeltSlope.Horizontal,
                    Array.Empty<BlockPos>());
            }

            DemoBeltSlope slope = GetSlopeBetween(firstShaft, secondShaft);
            Direction facing = GetFacingFromTo(firstShaft, secondShaft, shaftAxis);
            BlockPos[] chain = GetBeltChainBetween(firstShaft, secondShaft, slope, facing).ToArray();
            return new DemoBeltConnectionEvaluation(
                true,
                DemoBeltConnectionFailureReason.None,
                shaftAxis,
                facing,
                slope,
                chain);
        }

        public static bool TryCreateConnection(
            BlockWorld world,
            DemoContentCatalog catalog,
            BlockPos firstShaft,
            BlockPos secondShaft,
            out DemoBeltConnectionEvaluation evaluation,
            int maxLength = DefaultMaxBeltLength)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            evaluation = EvaluateConnection(world, catalog, firstShaft, secondShaft, maxLength);
            if (!evaluation.CanConnect)
                return false;

            DemoBeltSlope mutableSlope = evaluation.Slope;
            foreach (BlockPos position in evaluation.Chain)
            {
                DemoBeltPart part = position == firstShaft
                    ? DemoBeltPart.Start
                    : position == secondShaft
                        ? DemoBeltPart.End
                        : DemoBeltPart.Middle;

                bool hasAlignedShaft = TryGetShaftAxis(world.GetBlockState(position), catalog, out Axis shaftAxis) &&
                    shaftAxis == evaluation.ShaftAxis;
                if (part == DemoBeltPart.Middle && hasAlignedShaft)
                    part = DemoBeltPart.Pulley;
                if (hasAlignedShaft && shaftAxis == Axis.Y)
                    mutableSlope = DemoBeltSlope.Sideways;

                BlockState beltState = catalog.Belt.DefaultState
                    .With(DemoContentCatalog.BeltSlopeProperty, mutableSlope)
                    .With(DemoContentCatalog.BeltPartProperty, part)
                    .With(DemoContentCatalog.BeltFacingProperty, evaluation.Facing)
                    .With(DemoContentCatalog.BeltCasingProperty, false)
                    .With(DemoContentCatalog.BeltWaterloggedProperty, false);
                world.SetBlockState(position, beltState);
            }

            return true;
        }

        private static bool TryGetShaftAxis(BlockState state, DemoContentCatalog catalog, out Axis axis)
        {
            if (state != null && state.Definition.Id == catalog.Shaft.Id)
            {
                axis = state.Get(DemoContentCatalog.AxisProperty);
                return true;
            }

            axis = Axis.X;
            return false;
        }

        private static bool IsWithinBeltLength(BlockPos first, BlockPos second, int maxLength)
        {
            long dx = first.X - second.X;
            long dy = first.Y - second.Y;
            long dz = first.Z - second.Z;
            long distanceSquared = (dx * dx) + (dy * dy) + (dz * dz);
            long maxLengthSquared = (long)maxLength * maxLength;
            return distanceSquared <= maxLengthSquared;
        }

        private static bool HasConflictingSpeedSign(float firstSpeed, float secondSpeed)
        {
            if (firstSpeed == 0f || secondSpeed == 0f)
                return false;

            return Sign(firstSpeed) != Sign(secondSpeed);
        }

        private static DemoBeltSlope GetSlopeBetween(BlockPos start, BlockPos end)
        {
            BlockPos diff = end - start;
            if (diff.Y != 0)
            {
                if (diff.Z != 0 || diff.X != 0)
                    return diff.Y > 0 ? DemoBeltSlope.Upward : DemoBeltSlope.Downward;
                return DemoBeltSlope.Vertical;
            }

            return DemoBeltSlope.Horizontal;
        }

        private static Direction GetFacingFromTo(BlockPos start, BlockPos end, Axis shaftAxis)
        {
            Axis beltAxis = start.X == end.X ? Axis.Z : Axis.X;
            BlockPos diff = end - start;
            AxisDirection axisDirection;
            if (diff.X == 0 && diff.Z == 0)
                axisDirection = diff.Y > 0 ? AxisDirection.Positive : AxisDirection.Negative;
            else
                axisDirection = Choose(beltAxis, diff.X, diff.Y, diff.Z) > 0 ? AxisDirection.Positive : AxisDirection.Negative;

            Direction facing = GetDirection(axisDirection, beltAxis);
            if (diff.X == diff.Z)
            {
                Axis rotatedAxis = shaftAxis == Axis.X ? Axis.Z : Axis.X;
                facing = GetDirection(axisDirection, rotatedAxis);
            }

            return facing;
        }

        private static List<BlockPos> GetBeltChainBetween(BlockPos start, BlockPos end, DemoBeltSlope slope, Direction direction)
        {
            List<BlockPos> positions = new List<BlockPos>();
            int limit = ChainIterationLimit;
            BlockPos current = start;
            do
            {
                positions.Add(current);
                if (slope == DemoBeltSlope.Vertical)
                {
                    int verticalStep = direction.AxisDirection() == AxisDirection.Positive ? 1 : -1;
                    current = current.Offset(0, verticalStep, 0);
                    continue;
                }

                current = current.Relative(direction);
                if (slope != DemoBeltSlope.Horizontal)
                {
                    int verticalStep = slope == DemoBeltSlope.Upward ? 1 : -1;
                    current = current.Offset(0, verticalStep, 0);
                }
            }
            while (current != end && limit-- > 0);

            positions.Add(end);
            return positions;
        }

        private static Direction GetDirection(AxisDirection axisDirection, Axis axis)
        {
            if (axis == Axis.X)
                return axisDirection == AxisDirection.Positive ? Direction.East : Direction.West;
            if (axis == Axis.Y)
                return axisDirection == AxisDirection.Positive ? Direction.Up : Direction.Down;
            return axisDirection == AxisDirection.Positive ? Direction.South : Direction.North;
        }

        private static int Choose(Axis axis, int x, int y, int z)
        {
            return axis switch
            {
                Axis.X => x,
                Axis.Y => y,
                Axis.Z => z,
                _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
            };
        }

        private static int Sign(float value)
        {
            if (value > 0f)
                return 1;
            if (value < 0f)
                return -1;
            return 0;
        }
    }
}
