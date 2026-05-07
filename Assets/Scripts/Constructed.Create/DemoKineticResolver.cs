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

        public static DemoKineticSnapshot Resolve(BlockWorld world, DemoContentCatalog catalog)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            Dictionary<BlockPos, DemoKineticComponentState> statesByPosition =
                new Dictionary<BlockPos, DemoKineticComponentState>();

            foreach (WorldBlockEntry entry in world.GetStoredBlocks())
            {
                if (entry.State.Definition.Id != catalog.CreativeMotor.Id)
                    continue;

                Direction facing = entry.State.Get(DemoContentCatalog.FacingProperty);
                Axis axis = facing.Axis();
                float directedSpeed = ConvertToDirectedSpeed(CreativeMotorDefaultSpeed, facing);
                statesByPosition[entry.Position] = new DemoKineticComponentState(entry.Position, axis, directedSpeed);

                BlockPos shaftPosition = entry.Position.Relative(facing);
                while (TryGetAlignedShaft(world, catalog, shaftPosition, axis))
                {
                    statesByPosition[shaftPosition] =
                        new DemoKineticComponentState(shaftPosition, axis, directedSpeed);
                    shaftPosition = shaftPosition.Relative(facing);
                }
            }

            return new DemoKineticSnapshot(statesByPosition);
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
