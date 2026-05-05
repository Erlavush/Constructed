using System;
using System.Collections.Generic;
using System.Globalization;
using Constructed.Core;
using Constructed.Minecraft;

namespace Constructed.Create
{
    public sealed class ItemVaultBlockEntity : BlockEntity
    {
        private const string RadiusKey = "vault.radius";
        private const string LengthKey = "vault.length";
        private const string SlotCountKey = "vault.inventory.slot_count";
        private const string SlotKeyPrefix = "vault.inventory.";

        private readonly Registry<ItemDefinition> itemRegistry;

        public ItemVaultBlockEntity(
            BlockEntityType type,
            BlockWorld world,
            BlockPos position,
            BlockState state,
            Registry<ItemDefinition> itemRegistry,
            int slotsPerBlock)
            : base(type, world, position, state)
        {
            if (itemRegistry == null)
                throw new ArgumentNullException(nameof(itemRegistry));
            if (slotsPerBlock <= 0)
                throw new ArgumentOutOfRangeException(nameof(slotsPerBlock), "Item Vault slots per block must be positive.");

            this.itemRegistry = itemRegistry;
            Inventory = new InventoryContainer(slotsPerBlock);
            Radius = 1;
            Length = 1;
        }

        public InventoryContainer Inventory { get; }

        public int Radius { get; private set; }

        public int Length { get; private set; }

        public bool IsController
        {
            get { return true; }
        }

        public ItemStack Insert(ItemStack stack)
        {
            return Inventory.Insert(stack);
        }

        public ItemStack Extract(int slotIndex, int amount)
        {
            return Inventory.Extract(slotIndex, amount);
        }

        protected override void OnWrite(BlockEntityDataBuilder data)
        {
            data.SetInt32(RadiusKey, Radius);
            data.SetInt32(LengthKey, Length);
            data.SetInt32(SlotCountKey, Inventory.SlotCount);

            foreach (SerializedInventorySlot slot in Inventory.SerializeSlots())
            {
                string prefix = SlotKeyPrefix + slot.SlotIndex.ToString(CultureInfo.InvariantCulture);
                data.SetString(prefix + ".item", slot.Stack.ItemId.ToString());
                data.SetInt32(prefix + ".count", slot.Stack.Count);
            }
        }

        protected override void OnRead(BlockEntityData data)
        {
            Radius = ReadPositiveInt32(data, RadiusKey, 1);
            Length = ReadPositiveInt32(data, LengthKey, 1);
            int serializedSlotCount = ReadPositiveInt32(data, SlotCountKey, Inventory.SlotCount);
            if (serializedSlotCount != Inventory.SlotCount)
                throw new InvalidOperationException($"Serialized Item Vault slot count {serializedSlotCount} does not match runtime slot count {Inventory.SlotCount}.");

            List<SerializedInventorySlot> slots = new List<SerializedInventorySlot>();
            for (int i = 0; i < Inventory.SlotCount; i++)
            {
                string prefix = SlotKeyPrefix + i.ToString(CultureInfo.InvariantCulture);
                string itemIdText;
                if (!data.TryGetString(prefix + ".item", out itemIdText))
                    continue;

                int count = data.GetInt32(prefix + ".count");
                slots.Add(new SerializedInventorySlot(
                    i,
                    new SerializedItemStack(ResourceLocation.Parse(itemIdText), count)));
            }

            Inventory.LoadSnapshot(slots, itemRegistry);
        }

        private static int ReadPositiveInt32(BlockEntityData data, string key, int defaultValue)
        {
            string valueText;
            if (!data.TryGetString(key, out valueText))
                return defaultValue;

            int value = int.Parse(valueText, CultureInfo.InvariantCulture);
            if (value <= 0)
                throw new InvalidOperationException($"{key} must be positive.");

            return value;
        }
    }
}
