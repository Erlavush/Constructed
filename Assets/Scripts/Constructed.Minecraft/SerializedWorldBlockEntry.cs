using System;
using Constructed.Core;

namespace Constructed.Minecraft
{
    public readonly struct SerializedWorldBlockEntry : IEquatable<SerializedWorldBlockEntry>
    {
        public SerializedWorldBlockEntry(BlockPos position, SerializedBlockState state)
        {
            if (string.IsNullOrEmpty(state.BlockId.Namespace) || string.IsNullOrEmpty(state.BlockId.Path))
                throw new ArgumentException("Serialized world block state id must be initialized.", nameof(state));

            Position = position;
            State = state;
        }

        public BlockPos Position { get; }

        public SerializedBlockState State { get; }

        public bool Equals(SerializedWorldBlockEntry other)
        {
            return Position == other.Position && State == other.State;
        }

        public override bool Equals(object obj)
        {
            return obj is SerializedWorldBlockEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Position, State);
        }

        public static bool operator ==(SerializedWorldBlockEntry left, SerializedWorldBlockEntry right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SerializedWorldBlockEntry left, SerializedWorldBlockEntry right)
        {
            return !left.Equals(right);
        }
    }
}
