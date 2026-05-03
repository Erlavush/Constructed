using System;

namespace Constructed.Core
{
    public readonly struct BlockPos : IEquatable<BlockPos>
    {
        public static readonly BlockPos Zero = new BlockPos(0, 0, 0);

        public BlockPos(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public int X { get; }

        public int Y { get; }

        public int Z { get; }

        public BlockPos Offset(int x, int y, int z)
        {
            return new BlockPos(X + x, Y + y, Z + z);
        }

        public BlockPos Relative(Direction direction, int distance = 1)
        {
            return Offset(direction.StepX() * distance, direction.StepY() * distance, direction.StepZ() * distance);
        }

        public int Get(Axis axis)
        {
            return axis switch
            {
                Axis.X => X,
                Axis.Y => Y,
                Axis.Z => Z,
                _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
            };
        }

        public int DistManhattan(BlockPos other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y) + Math.Abs(Z - other.Z);
        }

        public bool Equals(BlockPos other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is BlockPos other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public override string ToString()
        {
            return $"{X},{Y},{Z}";
        }

        public static BlockPos operator +(BlockPos left, BlockPos right)
        {
            return new BlockPos(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }

        public static BlockPos operator -(BlockPos left, BlockPos right)
        {
            return new BlockPos(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }

        public static bool operator ==(BlockPos left, BlockPos right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BlockPos left, BlockPos right)
        {
            return !left.Equals(right);
        }
    }
}
