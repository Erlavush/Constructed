using System;
using Constructed.Core;

namespace Constructed.Minecraft
{
    public readonly struct WorldBlockEntry : IEquatable<WorldBlockEntry>
    {
        public WorldBlockEntry(BlockPos position, BlockState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            Position = position;
            State = state;
        }

        public BlockPos Position { get; }

        public BlockState State { get; }

        public bool Equals(WorldBlockEntry other)
        {
            return Position == other.Position && Equals(State, other.State);
        }

        public override bool Equals(object obj)
        {
            return obj is WorldBlockEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Position, State);
        }

        public override string ToString()
        {
            return $"{Position}:{State}";
        }

        public static bool operator ==(WorldBlockEntry left, WorldBlockEntry right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(WorldBlockEntry left, WorldBlockEntry right)
        {
            return !left.Equals(right);
        }
    }
}
