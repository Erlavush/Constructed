using System;

namespace Constructed.Minecraft
{
    public readonly struct SerializedInventorySlot : IEquatable<SerializedInventorySlot>
    {
        public SerializedInventorySlot(int slotIndex, SerializedItemStack stack)
        {
            if (slotIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(slotIndex), "Inventory slot index cannot be negative.");
            if (stack.IsEmpty)
                throw new ArgumentException("Serialized inventory slot stack cannot be empty.", nameof(stack));

            SlotIndex = slotIndex;
            Stack = stack;
        }

        public int SlotIndex { get; }

        public SerializedItemStack Stack { get; }

        public bool Equals(SerializedInventorySlot other)
        {
            return SlotIndex == other.SlotIndex && Stack == other.Stack;
        }

        public override bool Equals(object obj)
        {
            return obj is SerializedInventorySlot other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SlotIndex, Stack);
        }

        public override string ToString()
        {
            return $"{SlotIndex}:{Stack}";
        }

        public static bool operator ==(SerializedInventorySlot left, SerializedInventorySlot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SerializedInventorySlot left, SerializedInventorySlot right)
        {
            return !left.Equals(right);
        }
    }
}
