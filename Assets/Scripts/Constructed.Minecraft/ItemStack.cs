using System;
using Constructed.Core;

namespace Constructed.Minecraft
{
    public sealed class ItemStack : IEquatable<ItemStack>
    {
        public static readonly ItemStack Empty = new ItemStack();

        private ItemStack()
        {
            Count = 0;
        }

        public ItemStack(ItemDefinition item, int count)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Item stack count must be positive.");
            if (count > item.MaxStackSize)
                throw new ArgumentOutOfRangeException(nameof(count), $"Item stack count cannot exceed {item.MaxStackSize} for {item.Id}.");

            Item = item;
            Count = count;
        }

        public ItemDefinition Item { get; }

        public int Count { get; }

        public bool IsEmpty
        {
            get { return Item == null; }
        }

        public bool CanMerge(ItemStack other)
        {
            if (other == null)
                return false;
            if (IsEmpty || other.IsEmpty)
                return true;

            return ReferenceEquals(Item, other.Item);
        }

        public ItemStack WithCount(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Item stack count cannot be negative.");
            if (count == 0)
                return Empty;
            if (IsEmpty)
                throw new InvalidOperationException("Cannot assign a positive count to an empty stack without an item.");

            return new ItemStack(Item, count);
        }

        public ItemStack Split(int amount, out ItemStack remainder)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Split amount must be positive.");
            if (IsEmpty)
            {
                remainder = Empty;
                return Empty;
            }

            int takenCount = Math.Min(amount, Count);
            int remainingCount = Count - takenCount;
            remainder = remainingCount == 0 ? Empty : new ItemStack(Item, remainingCount);
            return new ItemStack(Item, takenCount);
        }

        public ItemStack Merge(ItemStack incoming, out ItemStack remainder)
        {
            if (incoming == null)
                throw new ArgumentNullException(nameof(incoming));
            if (incoming.IsEmpty)
            {
                remainder = Empty;
                return this;
            }
            if (IsEmpty)
            {
                remainder = Empty;
                return incoming;
            }
            if (!ReferenceEquals(Item, incoming.Item))
                throw new InvalidOperationException($"Cannot merge {incoming.Item.Id} into {Item.Id}.");

            int capacity = Item.MaxStackSize - Count;
            if (capacity <= 0)
            {
                remainder = incoming;
                return this;
            }

            int movedCount = Math.Min(capacity, incoming.Count);
            int remainderCount = incoming.Count - movedCount;
            remainder = remainderCount == 0 ? Empty : new ItemStack(incoming.Item, remainderCount);
            return new ItemStack(Item, Count + movedCount);
        }

        public SerializedItemStack Serialize()
        {
            if (IsEmpty)
                return SerializedItemStack.Empty;

            return new SerializedItemStack(Item.Id, Count);
        }

        public static ItemStack Deserialize(SerializedItemStack serialized, Registry<ItemDefinition> itemRegistry)
        {
            if (itemRegistry == null)
                throw new ArgumentNullException(nameof(itemRegistry));
            if (serialized.IsEmpty)
                return Empty;

            return new ItemStack(itemRegistry.GetValue(serialized.ItemId), serialized.Count);
        }

        public bool Equals(ItemStack other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (IsEmpty || other.IsEmpty)
                return IsEmpty && other.IsEmpty;

            return ReferenceEquals(Item, other.Item) && Count == other.Count;
        }

        public override bool Equals(object obj)
        {
            return obj is ItemStack other && Equals(other);
        }

        public override int GetHashCode()
        {
            if (IsEmpty)
                return 0;

            return HashCode.Combine(Item.Id, Count);
        }

        public override string ToString()
        {
            return IsEmpty ? "empty" : $"{Item.Id} x{Count}";
        }
    }
}
