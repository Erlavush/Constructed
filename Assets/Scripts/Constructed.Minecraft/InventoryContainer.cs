using System;
using System.Collections.Generic;
using Constructed.Core;

namespace Constructed.Minecraft
{
    public sealed class InventoryContainer
    {
        private readonly ItemStack[] slots;

        public InventoryContainer(int slotCount)
            : this(slotCount, ItemDefinition.DefaultMaxStackSize)
        {
        }

        public InventoryContainer(int slotCount, int slotLimit)
        {
            if (slotCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(slotCount), "Inventory slot count must be positive.");
            if (slotLimit <= 0)
                throw new ArgumentOutOfRangeException(nameof(slotLimit), "Inventory slot limit must be positive.");

            SlotLimit = slotLimit;
            slots = new ItemStack[slotCount];
            FillWithEmpty(slots);
        }

        public int SlotCount
        {
            get { return slots.Length; }
        }

        public int SlotLimit { get; }

        public int TotalItemCount
        {
            get
            {
                int count = 0;
                foreach (ItemStack stack in slots)
                    count += stack.Count;

                return count;
            }
        }

        public ItemStack GetStack(int slotIndex)
        {
            return slots[RequireSlotIndex(slotIndex)];
        }

        public void SetStack(int slotIndex, ItemStack stack)
        {
            if (stack == null)
                throw new ArgumentNullException(nameof(stack));

            slotIndex = RequireSlotIndex(slotIndex);
            if (!stack.IsEmpty && stack.Count > GetMaxStackSize(stack.Item))
                throw new ArgumentOutOfRangeException(nameof(stack), $"Stack count cannot exceed this inventory slot limit for {stack.Item.Id}.");

            slots[slotIndex] = stack.IsEmpty ? ItemStack.Empty : stack;
        }

        public ItemStack Insert(ItemStack incoming)
        {
            if (incoming == null)
                throw new ArgumentNullException(nameof(incoming));
            if (incoming.IsEmpty)
                return ItemStack.Empty;

            ItemStack remainder = incoming;
            for (int i = 0; i < slots.Length && !remainder.IsEmpty; i++)
            {
                if (!slots[i].IsEmpty && ReferenceEquals(slots[i].Item, remainder.Item))
                    remainder = Insert(i, remainder);
            }

            for (int i = 0; i < slots.Length && !remainder.IsEmpty; i++)
            {
                if (slots[i].IsEmpty)
                    remainder = Insert(i, remainder);
            }

            return remainder;
        }

        public ItemStack Insert(int slotIndex, ItemStack incoming)
        {
            if (incoming == null)
                throw new ArgumentNullException(nameof(incoming));
            if (incoming.IsEmpty)
                return ItemStack.Empty;

            slotIndex = RequireSlotIndex(slotIndex);
            ItemStack current = slots[slotIndex];
            if (!current.IsEmpty && !ReferenceEquals(current.Item, incoming.Item))
                return incoming;

            int maxStackSize = GetMaxStackSize(incoming.Item);
            int currentCount = current.IsEmpty ? 0 : current.Count;
            int available = maxStackSize - currentCount;
            if (available <= 0)
                return incoming;

            int moved = Math.Min(available, incoming.Count);
            slots[slotIndex] = new ItemStack(incoming.Item, currentCount + moved);

            int remaining = incoming.Count - moved;
            return remaining == 0 ? ItemStack.Empty : new ItemStack(incoming.Item, remaining);
        }

        public ItemStack Extract(int slotIndex, int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Extract amount must be positive.");

            slotIndex = RequireSlotIndex(slotIndex);
            ItemStack current = slots[slotIndex];
            if (current.IsEmpty)
                return ItemStack.Empty;

            ItemStack extracted = current.Split(amount, out ItemStack remainder);
            slots[slotIndex] = remainder;
            return extracted;
        }

        public void Clear()
        {
            FillWithEmpty(slots);
        }

        public SerializedInventorySlot[] SerializeSlots()
        {
            List<SerializedInventorySlot> serialized = new List<SerializedInventorySlot>();
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty)
                    serialized.Add(new SerializedInventorySlot(i, slots[i].Serialize()));
            }

            return serialized.ToArray();
        }

        public void LoadSnapshot(IEnumerable<SerializedInventorySlot> serializedSlots, Registry<ItemDefinition> itemRegistry)
        {
            if (itemRegistry == null)
                throw new ArgumentNullException(nameof(itemRegistry));

            ItemStack[] loaded = new ItemStack[slots.Length];
            FillWithEmpty(loaded);
            if (serializedSlots != null)
            {
                HashSet<int> seen = new HashSet<int>();
                foreach (SerializedInventorySlot serializedSlot in serializedSlots)
                {
                    int slotIndex = RequireSlotIndex(serializedSlot.SlotIndex);
                    if (!seen.Add(slotIndex))
                        throw new ArgumentException($"Duplicate serialized inventory slot {slotIndex}.");

                    ItemStack stack = ItemStack.Deserialize(serializedSlot.Stack, itemRegistry);
                    if (stack.Count > GetMaxStackSize(stack.Item))
                        throw new ArgumentOutOfRangeException(nameof(serializedSlots), $"Serialized stack count cannot exceed this inventory slot limit for {stack.Item.Id}.");

                    loaded[slotIndex] = stack;
                }
            }

            Array.Copy(loaded, slots, slots.Length);
        }

        private int GetMaxStackSize(ItemDefinition item)
        {
            return Math.Min(SlotLimit, item.MaxStackSize);
        }

        private int RequireSlotIndex(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length)
                throw new ArgumentOutOfRangeException(nameof(slotIndex), $"Inventory slot index must be between 0 and {slots.Length - 1}.");

            return slotIndex;
        }

        private static void FillWithEmpty(ItemStack[] target)
        {
            for (int i = 0; i < target.Length; i++)
                target[i] = ItemStack.Empty;
        }
    }
}
