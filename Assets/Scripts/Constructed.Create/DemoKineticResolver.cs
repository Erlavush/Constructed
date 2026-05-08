using System;
using System.Collections.Generic;
using Constructed.Core;
using Constructed.Minecraft;

namespace Constructed.Create
{
    public readonly struct DemoKineticComponentState : IEquatable<DemoKineticComponentState>
    {
        public DemoKineticComponentState(BlockPos position, Axis axis, float speed)
        {
            Position = position;
            Axis = axis;
            Speed = speed;
        }

        public BlockPos Position { get; }

        public Axis Axis { get; }

        public float Speed { get; }

        public bool Equals(DemoKineticComponentState other)
        {
            return Position == other.Position && Axis == other.Axis && Speed.Equals(other.Speed);
        }

        public override bool Equals(object obj)
        {
            return obj is DemoKineticComponentState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Position, (int)Axis, Speed);
        }

        public static bool operator ==(DemoKineticComponentState left, DemoKineticComponentState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DemoKineticComponentState left, DemoKineticComponentState right)
        {
            return !left.Equals(right);
        }
    }

    public sealed class DemoKineticSnapshot
    {
        private readonly Dictionary<BlockPos, DemoKineticComponentState> statesByPosition;

        internal DemoKineticSnapshot(Dictionary<BlockPos, DemoKineticComponentState> statesByPosition)
        {
            this.statesByPosition = statesByPosition ?? throw new ArgumentNullException(nameof(statesByPosition));
        }

        public int Count
        {
            get { return statesByPosition.Count; }
        }

        public bool TryGet(BlockPos position, out DemoKineticComponentState state)
        {
            return statesByPosition.TryGetValue(position, out state);
        }
    }

    public static class DemoKineticResolver
    {
        public const float CreativeMotorDefaultSpeed = 16f;
        public const float DegreesPerSecondPerSpeedUnit = 6f;
        public const float AlternatingShaftRotationOffsetDegrees = 22.5f;

        private static readonly Direction[] NeighborDirections =
        {
            Direction.Down,
            Direction.Up,
            Direction.North,
            Direction.South,
            Direction.West,
            Direction.East
        };

        public static DemoKineticSnapshot Resolve(BlockWorld world, DemoContentCatalog catalog)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            Dictionary<BlockPos, DemoKineticComponentState> statesByPosition =
                new Dictionary<BlockPos, DemoKineticComponentState>();
            Queue<(BlockPos Position, Axis Axis, float Speed)> frontier =
                new Queue<(BlockPos Position, Axis Axis, float Speed)>();

            // Pass 1: Find all Sources (Creative Motors)
            foreach (WorldBlockEntry entry in world.GetStoredBlocks())
            {
                if (entry.State.Definition.Id != catalog.CreativeMotor.Id)
                    continue;

                Direction facing = entry.State.Get(DemoContentCatalog.FacingProperty);
                Axis axis = facing.Axis();
                float directedSpeed = ConvertToDirectedSpeed(CreativeMotorDefaultSpeed, facing);

                if (!statesByPosition.ContainsKey(entry.Position))
                {
                    statesByPosition[entry.Position] = new DemoKineticComponentState(entry.Position, axis, directedSpeed);
                    frontier.Enqueue((entry.Position, axis, directedSpeed));
                }
            }

            // Pass 2: Propagate via BFS
            HashSet<BlockPos> processedBelts = new HashSet<BlockPos>();

            while (frontier.Count > 0)
            {
                var (currentPos, currentAxis, currentSpeed) = frontier.Dequeue();

                foreach (Direction dir in NeighborDirections)
                {
                    BlockPos neighborPos = currentPos.Relative(dir);
                    if (statesByPosition.ContainsKey(neighborPos))
                        continue;

                    BlockState neighborState = world.GetBlockState(neighborPos);
                    if (neighborState == null || neighborState.IsAir)
                        continue;

                    // Connection logic
                    if (CanConnect(world, catalog, currentPos, currentAxis, neighborPos, neighborState, dir, out Axis neighborAxis))
                    {
                        // Special case for belts: propagate to the whole chain
                        if (neighborState.Definition.Id == catalog.Belt.Id)
                        {
                            if (processedBelts.Contains(neighborPos))
                                continue;

                            List<BlockPos> chain = GetBeltChain(world, catalog, neighborPos);
                            foreach (BlockPos segmentPos in chain)
                            {
                                if (statesByPosition.ContainsKey(segmentPos))
                                    continue;

                                statesByPosition[segmentPos] = new DemoKineticComponentState(segmentPos, neighborAxis, currentSpeed);
                                frontier.Enqueue((segmentPos, neighborAxis, currentSpeed));
                                processedBelts.Add(segmentPos);
                            }
                        }
                        else
                        {
                            statesByPosition[neighborPos] = new DemoKineticComponentState(neighborPos, neighborAxis, currentSpeed);
                            frontier.Enqueue((neighborPos, neighborAxis, currentSpeed));
                        }
                    }
                }
            }

            return new DemoKineticSnapshot(statesByPosition);
        }

        private static bool CanConnect(
            BlockWorld world,
            DemoContentCatalog catalog,
            BlockPos fromPos,
            Axis fromAxis,
            BlockPos toPos,
            BlockState toState,
            Direction directionTo,
            out Axis toAxis)
        {
            toAxis = Axis.Y; // Default
            BlockState fromState = world.GetBlockState(fromPos);

            if (!HasShaftTowards(catalog, fromState, directionTo))
                return false;
            if (!HasShaftTowards(catalog, toState, directionTo.Opposite()))
                return false;

            Axis fromRealAxis = GetBlockRotationAxis(catalog, fromState);
            Axis toRealAxis = GetBlockRotationAxis(catalog, toState);

            if (fromRealAxis != toRealAxis)
                return false;

            toAxis = toRealAxis;
            return true;
        }

        private static bool HasShaftTowards(DemoContentCatalog catalog, BlockState state, Direction side)
        {
            if (state == null) return false;

            if (state.Definition.Id == catalog.Shaft.Id)
            {
                return state.Get(DemoContentCatalog.AxisProperty) == side.Axis();
            }
            if (state.Definition.Id == catalog.Belt.Id)
            {
                Axis rotationAxis = DemoBeltRuntimeResolver.GetRotationAxis(state, catalog);
                if (side.Axis() != rotationAxis)
                    return false;

                DemoBeltPart part = state.Get(DemoContentCatalog.BeltPartProperty);
                return part == DemoBeltPart.Start || part == DemoBeltPart.End || part == DemoBeltPart.Pulley;
            }
            if (state.Definition.Id == catalog.CreativeMotor.Id)
            {
                return state.Get(DemoContentCatalog.FacingProperty) == side;
            }

            return false;
        }

        private static Axis GetBlockRotationAxis(DemoContentCatalog catalog, BlockState state)
        {
            if (state.Definition.Id == catalog.Shaft.Id)
                return state.Get(DemoContentCatalog.AxisProperty);
            if (state.Definition.Id == catalog.Belt.Id)
                return DemoBeltRuntimeResolver.GetRotationAxis(state, catalog);
            if (state.Definition.Id == catalog.CreativeMotor.Id)
                return state.Get(DemoContentCatalog.FacingProperty).Axis();
            return Axis.Y;
        }

        private static List<BlockPos> GetBeltChain(BlockWorld world, DemoContentCatalog catalog, BlockPos seed)
        {
            // Find start of chain
            BlockPos current = seed;
            while (true)
            {
                BlockState state = world.GetBlockState(current);
                BlockPos? prev = DemoBeltRuntimeResolver.NextSegmentPosition(state, catalog, current, false);
                if (!prev.HasValue || world.GetBlockState(prev.Value).Definition.Id != catalog.Belt.Id)
                    break;
                current = prev.Value;
            }

            // Collect whole chain
            List<BlockPos> chain = new List<BlockPos>();
            while (true)
            {
                chain.Add(current);
                BlockState state = world.GetBlockState(current);
                BlockPos? next = DemoBeltRuntimeResolver.NextSegmentPosition(state, catalog, current, true);
                if (!next.HasValue || world.GetBlockState(next.Value).Definition.Id != catalog.Belt.Id)
                    break;
                current = next.Value;
            }
            return chain;
        }

        public static float ConvertToDirectedSpeed(float axisSpeed, Direction direction)
        {
            return direction.AxisDirection() == AxisDirection.Positive ? axisSpeed : -axisSpeed;
        }

        public static float ConvertToDegreesPerSecond(float speed)
        {
            return speed * DegreesPerSecondPerSpeedUnit;
        }

        public static float GetRotationOffsetDegrees(BlockPos position, Axis axis)
        {
            return ShouldOffsetRotation(position, axis) ? AlternatingShaftRotationOffsetDegrees : 0f;
        }

        public static bool ShouldOffsetRotation(BlockPos position, Axis axis)
        {
            if (axis == Axis.X)
                return ((position.Y + position.Z) & 1) == 0;
            if (axis == Axis.Y)
                return ((position.X + position.Z) & 1) == 0;
            return ((position.X + position.Y) & 1) == 0;
        }

        private static bool TryGetAlignedShaft(BlockWorld world, DemoContentCatalog catalog, BlockPos position, Axis requiredAxis)
        {
            BlockState state = world.GetBlockState(position);
            return state.Definition.Id == catalog.Shaft.Id &&
                state.Get(DemoContentCatalog.AxisProperty) == requiredAxis;
        }
    }
}
