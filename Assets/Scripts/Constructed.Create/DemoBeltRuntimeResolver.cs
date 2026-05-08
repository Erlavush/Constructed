using System;
using System.Collections.Generic;
using Constructed.Core;
using Constructed.Minecraft;

namespace Constructed.Create
{
    public readonly struct DemoBeltSegmentRuntimeState : IEquatable<DemoBeltSegmentRuntimeState>
    {
        public DemoBeltSegmentRuntimeState(
            BlockPos position,
            BlockPos controllerPosition,
            int index,
            int length,
            Direction facing,
            DemoBeltSlope slope,
            DemoBeltPart part,
            Axis rotationAxis,
            float speed)
        {
            Position = position;
            ControllerPosition = controllerPosition;
            Index = index;
            Length = length;
            Facing = facing;
            Slope = slope;
            Part = part;
            RotationAxis = rotationAxis;
            Speed = speed;
        }

        public BlockPos Position { get; }

        public BlockPos ControllerPosition { get; }

        public int Index { get; }

        public int Length { get; }

        public Direction Facing { get; }

        public DemoBeltSlope Slope { get; }

        public DemoBeltPart Part { get; }

        public Axis RotationAxis { get; }

        public float Speed { get; }

        public bool Equals(DemoBeltSegmentRuntimeState other)
        {
            return Position == other.Position &&
                ControllerPosition == other.ControllerPosition &&
                Index == other.Index &&
                Length == other.Length &&
                Facing == other.Facing &&
                Slope == other.Slope &&
                Part == other.Part &&
                RotationAxis == other.RotationAxis &&
                Speed.Equals(other.Speed);
        }

        public override bool Equals(object obj)
        {
            return obj is DemoBeltSegmentRuntimeState other && Equals(other);
        }

        public override int GetHashCode()
        {
            int hash1 = HashCode.Combine(
                Position,
                ControllerPosition,
                Index,
                Length);
            int hash2 = HashCode.Combine(
                (int)Facing,
                (int)Slope,
                (int)Part,
                (int)RotationAxis);
            return HashCode.Combine(hash1, hash2, Speed);
        }

        public static bool operator ==(DemoBeltSegmentRuntimeState left, DemoBeltSegmentRuntimeState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DemoBeltSegmentRuntimeState left, DemoBeltSegmentRuntimeState right)
        {
            return !left.Equals(right);
        }
    }

    public sealed class DemoBeltRuntimeSnapshot
    {
        private readonly Dictionary<BlockPos, DemoBeltSegmentRuntimeState> statesByPosition;

        internal DemoBeltRuntimeSnapshot(Dictionary<BlockPos, DemoBeltSegmentRuntimeState> statesByPosition)
        {
            this.statesByPosition = statesByPosition ?? throw new ArgumentNullException(nameof(statesByPosition));
        }

        public int Count
        {
            get { return statesByPosition.Count; }
        }

        public bool TryGet(BlockPos position, out DemoBeltSegmentRuntimeState state)
        {
            return statesByPosition.TryGetValue(position, out state);
        }
    }

    public static class DemoBeltRuntimeResolver
    {
        private static readonly Direction[] NeighborDirections =
        {
            Direction.Down,
            Direction.Up,
            Direction.North,
            Direction.South,
            Direction.West,
            Direction.East
        };

        private const int ChainIterationLimit = 1000;

        public static DemoBeltRuntimeSnapshot Resolve(
            BlockWorld world,
            DemoContentCatalog catalog,
            DemoKineticSnapshot kineticSnapshot = null)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            DemoKineticSnapshot resolvedKinetics = kineticSnapshot ?? DemoKineticResolver.Resolve(world, catalog);
            Dictionary<BlockPos, DemoBeltSegmentRuntimeState> statesByPosition =
                new Dictionary<BlockPos, DemoBeltSegmentRuntimeState>();
            HashSet<BlockPos> visited = new HashSet<BlockPos>();

            foreach (WorldBlockEntry entry in world.GetStoredBlocks())
            {
                if (!IsBeltState(entry.State, catalog))
                    continue;
                if (visited.Contains(entry.Position))
                    continue;

                BlockPos controllerPosition = FindControllerPosition(world, catalog, entry.Position);
                List<BlockPos> chain = GetBeltChain(world, catalog, controllerPosition);
                if (chain.Count == 0)
                    continue;

                Axis rotationAxis = GetRotationAxis(world.GetBlockState(controllerPosition), catalog);
                
                // Find chain speed from any segment already in the kinetic snapshot
                float chainSpeed = 0f;
                foreach (BlockPos segmentPos in chain)
                {
                    if (resolvedKinetics.TryGet(segmentPos, out DemoKineticComponentState kineticState))
                    {
                        chainSpeed = kineticState.Speed;
                        break;
                    }
                }

                for (int index = 0; index < chain.Count; index++)
                {
                    BlockPos segmentPosition = chain[index];
                    BlockState segmentState = world.GetBlockState(segmentPosition);
                    Direction facing = segmentState.Get(DemoContentCatalog.BeltFacingProperty);
                    DemoBeltSlope slope = segmentState.Get(DemoContentCatalog.BeltSlopeProperty);
                    DemoBeltPart part = segmentState.Get(DemoContentCatalog.BeltPartProperty);

                    statesByPosition[segmentPosition] = new DemoBeltSegmentRuntimeState(
                        segmentPosition,
                        controllerPosition,
                        index,
                        chain.Count,
                        facing,
                        slope,
                        part,
                        rotationAxis,
                        chainSpeed);
                    visited.Add(segmentPosition);
                }
            }

            return new DemoBeltRuntimeSnapshot(statesByPosition);
        }

        public static Axis GetRotationAxis(BlockState beltState, DemoContentCatalog catalog)
        {
            if (beltState == null)
                throw new ArgumentNullException(nameof(beltState));
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (!IsBeltState(beltState, catalog))
                throw new ArgumentException("Expected a create:belt block state.", nameof(beltState));

            DemoBeltSlope slope = beltState.Get(DemoContentCatalog.BeltSlopeProperty);
            if (slope == DemoBeltSlope.Sideways)
                return Axis.Y;

            return beltState.Get(DemoContentCatalog.BeltFacingProperty).ClockWise().Axis();
        }

        public static BlockPos? NextSegmentPosition(BlockState beltState, DemoContentCatalog catalog, BlockPos position, bool forward)
        {
            if (beltState == null)
                throw new ArgumentNullException(nameof(beltState));
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (!IsBeltState(beltState, catalog))
                return null;

            Direction facing = beltState.Get(DemoContentCatalog.BeltFacingProperty);
            DemoBeltSlope slope = beltState.Get(DemoContentCatalog.BeltSlopeProperty);
            DemoBeltPart part = beltState.Get(DemoContentCatalog.BeltPartProperty);
            int offset = forward ? 1 : -1;

            if ((part == DemoBeltPart.End && forward) || (part == DemoBeltPart.Start && !forward))
                return null;
            if (slope == DemoBeltSlope.Vertical)
            {
                int verticalStep = facing.AxisDirection() == AxisDirection.Positive ? offset : -offset;
                return position.Offset(0, verticalStep, 0);
            }

            BlockPos next = position.Relative(facing, offset);
            if (slope != DemoBeltSlope.Horizontal && slope != DemoBeltSlope.Sideways)
                next = next.Offset(0, slope == DemoBeltSlope.Upward ? offset : -offset, 0);
            return next;
        }


        private static BlockPos FindControllerPosition(BlockWorld world, DemoContentCatalog catalog, BlockPos seed)
        {
            BlockPos current = seed;
            int limit = ChainIterationLimit;
            while (limit-- > 0)
            {
                BlockState state = world.GetBlockState(current);
                if (!IsBeltState(state, catalog))
                    break;

                BlockPos? previous = NextSegmentPosition(state, catalog, current, false);
                if (!previous.HasValue)
                    break;
                if (!IsBeltState(world.GetBlockState(previous.Value), catalog))
                    break;

                current = previous.Value;
            }

            return current;
        }

        private static List<BlockPos> GetBeltChain(BlockWorld world, DemoContentCatalog catalog, BlockPos controllerPosition)
        {
            List<BlockPos> chain = new List<BlockPos>();
            BlockPos current = controllerPosition;
            int limit = ChainIterationLimit;
            while (limit-- > 0)
            {
                BlockState state = world.GetBlockState(current);
                if (!IsBeltState(state, catalog))
                    break;

                chain.Add(current);
                BlockPos? next = NextSegmentPosition(state, catalog, current, true);
                if (!next.HasValue)
                    break;
                if (!IsBeltState(world.GetBlockState(next.Value), catalog))
                    break;

                current = next.Value;
            }

            return chain;
        }

        private static bool IsBeltState(BlockState state, DemoContentCatalog catalog)
        {
            return state != null && state.Definition.Id == catalog.Belt.Id;
        }
    }
}
