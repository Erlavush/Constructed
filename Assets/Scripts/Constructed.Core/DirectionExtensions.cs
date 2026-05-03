using System;

namespace Constructed.Core
{
    public static class DirectionExtensions
    {
        public static Axis Axis(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Down:
                case Direction.Up:
                    return global::Constructed.Core.Axis.Y;
                case Direction.North:
                case Direction.South:
                    return global::Constructed.Core.Axis.Z;
                case Direction.West:
                case Direction.East:
                    return global::Constructed.Core.Axis.X;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        public static AxisDirection AxisDirection(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Down:
                case Direction.North:
                case Direction.West:
                    return global::Constructed.Core.AxisDirection.Negative;
                case Direction.Up:
                case Direction.South:
                case Direction.East:
                    return global::Constructed.Core.AxisDirection.Positive;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        public static Direction Opposite(this Direction direction)
        {
            return direction switch
            {
                Direction.Down => Direction.Up,
                Direction.Up => Direction.Down,
                Direction.North => Direction.South,
                Direction.South => Direction.North,
                Direction.West => Direction.East,
                Direction.East => Direction.West,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
        }

        public static bool IsHorizontal(this Direction direction)
        {
            return direction.Axis() != global::Constructed.Core.Axis.Y;
        }

        public static bool IsVertical(this Direction direction)
        {
            return direction.Axis() == global::Constructed.Core.Axis.Y;
        }

        public static int StepX(this Direction direction)
        {
            return direction switch
            {
                Direction.West => -1,
                Direction.East => 1,
                _ => 0
            };
        }

        public static int StepY(this Direction direction)
        {
            return direction switch
            {
                Direction.Down => -1,
                Direction.Up => 1,
                _ => 0
            };
        }

        public static int StepZ(this Direction direction)
        {
            return direction switch
            {
                Direction.North => -1,
                Direction.South => 1,
                _ => 0
            };
        }

        public static BlockPos Normal(this Direction direction)
        {
            return new BlockPos(direction.StepX(), direction.StepY(), direction.StepZ());
        }

        public static Direction ClockWise(this Direction direction)
        {
            return direction switch
            {
                Direction.North => Direction.East,
                Direction.East => Direction.South,
                Direction.South => Direction.West,
                Direction.West => Direction.North,
                _ => throw new InvalidOperationException("Only horizontal directions can rotate clockwise.")
            };
        }

        public static Direction CounterClockWise(this Direction direction)
        {
            return direction switch
            {
                Direction.North => Direction.West,
                Direction.West => Direction.South,
                Direction.South => Direction.East,
                Direction.East => Direction.North,
                _ => throw new InvalidOperationException("Only horizontal directions can rotate counterclockwise.")
            };
        }
    }
}
