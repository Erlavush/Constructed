using System;
using Constructed.Core;

namespace Constructed.Minecraft
{
    public readonly struct SerializedBlockEntity : IEquatable<SerializedBlockEntity>
    {
        public SerializedBlockEntity(BlockPos position, ResourceLocation typeId, BlockEntityData data)
        {
            if (string.IsNullOrEmpty(typeId.Namespace) || string.IsNullOrEmpty(typeId.Path))
                throw new ArgumentException("Serialized block entity type id must be initialized.", nameof(typeId));

            Position = position;
            TypeId = typeId;
            Data = data;
        }

        public BlockPos Position { get; }

        public ResourceLocation TypeId { get; }

        public BlockEntityData Data { get; }

        public bool Equals(SerializedBlockEntity other)
        {
            return Position == other.Position && TypeId == other.TypeId && Data == other.Data;
        }

        public override bool Equals(object obj)
        {
            return obj is SerializedBlockEntity other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Position, TypeId, Data);
        }

        public static bool operator ==(SerializedBlockEntity left, SerializedBlockEntity right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SerializedBlockEntity left, SerializedBlockEntity right)
        {
            return !left.Equals(right);
        }
    }
}
