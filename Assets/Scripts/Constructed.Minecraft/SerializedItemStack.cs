using System;
using Constructed.Core;

namespace Constructed.Minecraft
{
    public readonly struct SerializedItemStack : IEquatable<SerializedItemStack>
    {
        public static readonly SerializedItemStack Empty = default(SerializedItemStack);

        public SerializedItemStack(ResourceLocation itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId.Namespace) || string.IsNullOrEmpty(itemId.Path))
                throw new ArgumentException("Item id must be initialized.", nameof(itemId));
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Serialized item stack count must be positive.");

            ItemId = itemId;
            Count = count;
        }

        public ResourceLocation ItemId { get; }

        public int Count { get; }

        public bool IsEmpty
        {
            get { return Count == 0; }
        }

        public bool Equals(SerializedItemStack other)
        {
            return ItemId == other.ItemId && Count == other.Count;
        }

        public override bool Equals(object obj)
        {
            return obj is SerializedItemStack other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ItemId, Count);
        }

        public override string ToString()
        {
            return IsEmpty ? "empty" : $"{ItemId} x{Count}";
        }

        public static bool operator ==(SerializedItemStack left, SerializedItemStack right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SerializedItemStack left, SerializedItemStack right)
        {
            return !left.Equals(right);
        }
    }
}
