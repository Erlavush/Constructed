using System;
using System.Collections.Generic;
using Constructed.Core;
using Constructed.Minecraft;

namespace Constructed.Create
{
    public readonly struct DemoKineticComponentState : IEquatable<DemoKineticComponentState>
    {
        public DemoKineticComponentState(BlockPos position, Axis axis, float speed)
            : this(position, axis, speed, null)
        {
        }

        public DemoKineticComponentState(BlockPos position, Axis axis, float speed, Direction? sourceFacing)
        {
            Position = position;
            Axis = axis;
            Speed = speed;
            SourceFacing = sourceFacing;
        }

        public BlockPos Position { get; }

        public Axis Axis { get; }

        public float Speed { get; }

        public Direction? SourceFacing { get; }

        public bool Equals(DemoKineticComponentState other)
        {
            return Position == other.Position &&
                Axis == other.Axis &&
                Speed.Equals(other.Speed) &&
                SourceFacing == other.SourceFacing;
        }

        public override bool Equals(object obj)
        {
            return obj is DemoKineticComponentState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Position, (int)Axis, Speed, SourceFacing);
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
            Queue<BlockPos> frontier = new Queue<BlockPos>();

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
                    frontier.Enqueue(entry.Position);
                }
            }

            // Pass 2: Propagate via BFS
            HashSet<BlockPos> processedBelts = new HashSet<BlockPos>();

            while (frontier.Count > 0)
            {
                BlockPos currentPos = frontier.Dequeue();
                BlockState currentState = world.GetBlockState(currentPos);
                DemoKineticComponentState currentKineticState = statesByPosition[currentPos];

                foreach (BlockPos neighborPos in GetPotentialNeighborPositions(catalog, currentState, currentPos))
                {
                    if (statesByPosition.ContainsKey(neighborPos))
                        continue;

                    BlockState neighborState = world.GetBlockState(neighborPos);
                    if (neighborState == null || world.IsAir(neighborState))
                        continue;

                    // Connection logic
                    if (TryGetRotationSpeedModifier(
                            catalog,
                            currentPos,
                            currentState,
                            currentKineticState,
                            neighborPos,
                            neighborState,
                            out Axis neighborAxis,
                            out float speedModifier))
                    {
                        float neighborSpeed = currentKineticState.Speed * speedModifier;
                        Direction? sourceFacing = TryGetDirectionFromDiff(currentPos - neighborPos, out Direction resolvedSourceFacing)
                            ? resolvedSourceFacing
                            : (Direction?)null;

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

                                statesByPosition[segmentPos] = new DemoKineticComponentState(segmentPos, neighborAxis, neighborSpeed, sourceFacing);
                                frontier.Enqueue(segmentPos);
                                processedBelts.Add(segmentPos);
                            }
                        }
                        else
                        {
                            statesByPosition[neighborPos] = new DemoKineticComponentState(neighborPos, neighborAxis, neighborSpeed, sourceFacing);
                            frontier.Enqueue(neighborPos);
                        }
                    }
                }
            }

            return new DemoKineticSnapshot(statesByPosition);
        }

        private static bool TryGetRotationSpeedModifier(
            DemoContentCatalog catalog,
            BlockPos fromPos,
            BlockState fromState,
            DemoKineticComponentState fromKineticState,
            BlockPos toPos,
            BlockState toState,
            out Axis toAxis,
            out float speedModifier)
        {
            toAxis = GetBlockRotationAxis(catalog, toState);
            speedModifier = 0f;

            BlockPos diff = toPos - fromPos;
            if (!TryGetNearestDirection(diff, out Direction directionTo))
                return false;

            bool alignedAxes = IsAlignedWithDirection(diff, directionTo);
            bool connectedByAxis = alignedAxes &&
                HasShaftTowards(catalog, fromState, directionTo) &&
                HasShaftTowards(catalog, toState, directionTo.Opposite());

            if (connectedByAxis)
            {
                float targetAxisModifier = GetAxisModifier(catalog, toState, null, directionTo.Opposite());
                if (targetAxisModifier != 0f)
                    targetAxisModifier = 1f / targetAxisModifier;

                speedModifier = GetAxisModifier(catalog, fromState, fromKineticState.SourceFacing, directionTo) * targetAxisModifier;
                return Math.Abs(speedModifier) > 0f;
            }

            if (IsLargeToLargeGear(catalog, fromState, toState, diff))
            {
                Axis sourceAxis = fromState.Get(DemoContentCatalog.AxisProperty);
                Axis targetAxis = toState.Get(DemoContentCatalog.AxisProperty);
                int sourceAxisDiff = diff.Get(sourceAxis);
                int targetAxisDiff = diff.Get(targetAxis);

                speedModifier = (sourceAxisDiff > 0) ^ (targetAxisDiff > 0) ? -1f : 1f;
                return true;
            }

            if (CogWheelBlock.IsLargeCog(catalog, fromState) && CogWheelBlock.IsSmallCog(catalog, toState) &&
                IsLargeToSmallCog(catalog, fromState, toState, diff))
            {
                speedModifier = -2f;
                return true;
            }

            if (CogWheelBlock.IsLargeCog(catalog, toState) && CogWheelBlock.IsSmallCog(catalog, fromState) &&
                IsLargeToSmallCog(catalog, toState, fromState, fromPos - toPos))
            {
                speedModifier = -0.5f;
                return true;
            }

            if (CogWheelBlock.IsSmallCog(catalog, fromState) && CogWheelBlock.IsSmallCog(catalog, toState))
            {
                if (diff.DistManhattan(BlockPos.Zero) != 1)
                    return false;
                if (directionTo.Axis() == GetBlockRotationAxis(catalog, fromState))
                    return false;
                if (GetBlockRotationAxis(catalog, fromState) == GetBlockRotationAxis(catalog, toState))
                {
                    speedModifier = -1f;
                    return true;
                }
            }

            return false;
        }

        private static bool HasShaftTowards(DemoContentCatalog catalog, BlockState state, Direction side)
        {
            if (state == null) return false;

            if (state.Definition.Id == catalog.Shaft.Id)
            {
                return state.Get(DemoContentCatalog.AxisProperty) == side.Axis();
            }
            if (CogWheelBlock.IsCog(catalog, state))
            {
                return state.Get(DemoContentCatalog.AxisProperty) == side.Axis();
            }
            if (state.Definition.Id == catalog.Gearbox.Id)
            {
                return side.Axis() != state.Get(DemoContentCatalog.AxisProperty);
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
            if (CogWheelBlock.IsCog(catalog, state))
                return state.Get(DemoContentCatalog.AxisProperty);
            if (state.Definition.Id == catalog.Gearbox.Id)
                return state.Get(DemoContentCatalog.AxisProperty);
            if (state.Definition.Id == catalog.Belt.Id)
                return DemoBeltRuntimeResolver.GetRotationAxis(state, catalog);
            if (state.Definition.Id == catalog.CreativeMotor.Id)
                return state.Get(DemoContentCatalog.FacingProperty).Axis();
            return Axis.Y;
        }

        private static IReadOnlyList<BlockPos> GetPotentialNeighborPositions(DemoContentCatalog catalog, BlockState state, BlockPos position)
        {
            List<BlockPos> neighbors = new List<BlockPos>();
            HashSet<BlockPos> uniqueNeighbors = new HashSet<BlockPos>();
            foreach (Direction direction in NeighborDirections)
                AddNeighbor(uniqueNeighbors, neighbors, position.Relative(direction));

            if (state == null)
                return neighbors;

            if (CogWheelBlock.IsSmallCog(catalog, state))
            {
                Axis axis = state.Get(DemoContentCatalog.AxisProperty);
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            BlockPos offset = new BlockPos(x, y, z);
                            if (offset.Get(axis) != 0)
                                continue;
                            if (SquaredDistance(offset) != 2)
                                continue;

                            AddNeighbor(uniqueNeighbors, neighbors, position + offset);
                        }
                    }
                }
            }
            else if (CogWheelBlock.IsLargeCog(catalog, state))
            {
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            BlockPos offset = new BlockPos(x, y, z);
                            if (SquaredDistance(offset) != 2)
                                continue;

                            AddNeighbor(uniqueNeighbors, neighbors, position + offset);
                        }
                    }
                }
            }

            return neighbors;
        }

        private static void AddNeighbor(HashSet<BlockPos> uniqueNeighbors, List<BlockPos> neighbors, BlockPos position)
        {
            if (uniqueNeighbors.Add(position))
                neighbors.Add(position);
        }

        private static int SquaredDistance(BlockPos position)
        {
            return (position.X * position.X) + (position.Y * position.Y) + (position.Z * position.Z);
        }

        private static bool IsAlignedWithDirection(BlockPos diff, Direction direction)
        {
            foreach (Axis axis in new[] { Axis.X, Axis.Y, Axis.Z })
            {
                if (axis == direction.Axis())
                    continue;
                if (diff.Get(axis) != 0)
                    return false;
            }

            return diff.Get(direction.Axis()) != 0;
        }

        private static bool TryGetNearestDirection(BlockPos diff, out Direction direction)
        {
            direction = Direction.North;
            int bestDot = int.MinValue;
            foreach (Direction candidate in NeighborDirections)
            {
                int dot =
                    (diff.X * candidate.StepX()) +
                    (diff.Y * candidate.StepY()) +
                    (diff.Z * candidate.StepZ());
                if (dot <= bestDot)
                    continue;

                bestDot = dot;
                direction = candidate;
            }

            return bestDot > 0;
        }

        private static bool TryGetDirectionFromDiff(BlockPos diff, out Direction direction)
        {
            if (diff.DistManhattan(BlockPos.Zero) != 1)
            {
                direction = Direction.North;
                return false;
            }

            return TryGetNearestDirection(diff, out direction);
        }

        private static float GetAxisModifier(
            DemoContentCatalog catalog,
            BlockState state,
            Direction? sourceFacing,
            Direction direction)
        {
            if (state.Definition.Id != catalog.Gearbox.Id || !sourceFacing.HasValue)
                return 1f;

            Direction source = sourceFacing.Value;
            if (direction.Axis() == source.Axis())
                return direction == source ? 1f : -1f;

            return direction.AxisDirection() == source.AxisDirection() ? -1f : 1f;
        }

        private static bool IsLargeToLargeGear(DemoContentCatalog catalog, BlockState from, BlockState to, BlockPos diff)
        {
            if (!CogWheelBlock.IsLargeCog(catalog, from) || !CogWheelBlock.IsLargeCog(catalog, to))
                return false;

            Axis fromAxis = from.Get(DemoContentCatalog.AxisProperty);
            Axis toAxis = to.Get(DemoContentCatalog.AxisProperty);
            if (fromAxis == toAxis)
                return false;

            foreach (Axis axis in new[] { Axis.X, Axis.Y, Axis.Z })
            {
                int axisDiff = diff.Get(axis);
                if (axis == fromAxis || axis == toAxis)
                {
                    if (axisDiff == 0)
                        return false;
                }
                else if (axisDiff != 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsLargeToSmallCog(DemoContentCatalog catalog, BlockState largeCog, BlockState smallCog, BlockPos diff)
        {
            Axis largeAxis = largeCog.Get(DemoContentCatalog.AxisProperty);
            if (largeAxis != GetBlockRotationAxis(catalog, smallCog))
                return false;
            if (diff.Get(largeAxis) != 0)
                return false;

            foreach (Axis axis in new[] { Axis.X, Axis.Y, Axis.Z })
            {
                if (axis == largeAxis)
                    continue;
                if (Math.Abs(diff.Get(axis)) != 1)
                    return false;
            }

            return true;
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
            // Consistent global rotation sign: positive axis directions get positive speed,
            // negative axis directions get negative speed. This ensures that all connected
            // components along the same axis rotate in the same direction in global space.
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

        public static float GetCogwheelRotationOffsetDegrees(BlockPos position, Axis axis, bool isLarge)
        {
            if (ShouldOffsetRotation(position, axis))
                return AlternatingShaftRotationOffsetDegrees;

            return isLarge ? 11.25f : 0f;
        }

        public static float GetLargeCogwheelShaftRotationOffsetDegrees(BlockPos position, Axis axis)
        {
            return ShouldOffsetRotation(position, axis) ? AlternatingShaftRotationOffsetDegrees : 0f;
        }

        public static float GetGearboxFaceSpeed(DemoKineticComponentState gearboxState, Direction direction)
        {
            float speed = gearboxState.Speed;
            if (speed == 0f || !gearboxState.SourceFacing.HasValue)
                return speed;

            Direction sourceFacing = gearboxState.SourceFacing.Value;
            if (sourceFacing.Axis() == direction.Axis())
                return sourceFacing == direction ? speed : -speed;
            if (sourceFacing.AxisDirection() == direction.AxisDirection())
                return -speed;

            return speed;
        }

        public static bool ShouldOffsetRotation(BlockPos position, Axis axis)
        {
            if (axis == Axis.X)
                return ((position.Y + position.Z) & 1) == 0;
            if (axis == Axis.Y)
                return ((position.X + position.Z) & 1) == 0;
            return ((position.X + position.Y) & 1) == 0;
        }

    }
}
