using System;
using System.Collections.Generic;
using Constructed.Core;
using Constructed.Minecraft;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class InventoryContainerTests
    {
        [Test]
        public void ContainerStartsEmptyAndRejectsInvalidInputs()
        {
            InventoryContainer inventory = new InventoryContainer(2, 16);

            Assert.AreEqual(2, inventory.SlotCount);
            Assert.AreEqual(16, inventory.SlotLimit);
            Assert.AreSame(ItemStack.Empty, inventory.GetStack(0));
            Assert.AreEqual(0, inventory.TotalItemCount);
            Assert.Throws<ArgumentOutOfRangeException>(() => new InventoryContainer(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new InventoryContainer(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => inventory.GetStack(-1));
            Assert.Throws<ArgumentNullException>(() => inventory.SetStack(0, null));
        }

        [Test]
        public void InsertFillsExistingStacksThenEmptySlotsAndReturnsOverflow()
        {
            ItemDefinition alloy = new ItemDefinition(ResourceLocation.Parse("create:andesite_alloy"));
            InventoryContainer inventory = new InventoryContainer(2, 16);

            ItemStack firstOverflow = inventory.Insert(new ItemStack(alloy, 12));
            ItemStack secondOverflow = inventory.Insert(new ItemStack(alloy, 20));
            ItemStack thirdOverflow = inventory.Insert(new ItemStack(alloy, 12));

            Assert.AreSame(ItemStack.Empty, firstOverflow);
            Assert.AreSame(ItemStack.Empty, secondOverflow);
            Assert.AreEqual(16, inventory.GetStack(0).Count);
            Assert.AreEqual(16, inventory.GetStack(1).Count);
            Assert.AreEqual(12, thirdOverflow.Count);
            Assert.AreSame(alloy, thirdOverflow.Item);
            Assert.AreEqual(32, inventory.TotalItemCount);
        }

        [Test]
        public void InsertKeepsDifferentItemsSeparate()
        {
            ItemDefinition alloy = new ItemDefinition(ResourceLocation.Parse("create:andesite_alloy"));
            ItemDefinition zinc = new ItemDefinition(ResourceLocation.Parse("create:zinc_ingot"));
            InventoryContainer inventory = new InventoryContainer(1);

            inventory.Insert(new ItemStack(alloy, 8));
            ItemStack overflow = inventory.Insert(new ItemStack(zinc, 4));

            Assert.AreEqual(8, inventory.GetStack(0).Count);
            Assert.AreSame(alloy, inventory.GetStack(0).Item);
            Assert.AreEqual(4, overflow.Count);
            Assert.AreSame(zinc, overflow.Item);
        }

        [Test]
        public void ExtractReturnsRequestedItemsAndUpdatesSlot()
        {
            ItemDefinition alloy = new ItemDefinition(ResourceLocation.Parse("create:andesite_alloy"));
            InventoryContainer inventory = new InventoryContainer(1);
            inventory.SetStack(0, new ItemStack(alloy, 10));

            ItemStack extracted = inventory.Extract(0, 4);
            ItemStack rest = inventory.Extract(0, 20);

            Assert.AreEqual(4, extracted.Count);
            Assert.AreEqual(6, rest.Count);
            Assert.AreSame(ItemStack.Empty, inventory.GetStack(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => inventory.Extract(0, 0));
        }

        [Test]
        public void SerializeAndLoadSnapshotUseStableSlotIndexes()
        {
            ItemDefinition alloy = new ItemDefinition(ResourceLocation.Parse("create:andesite_alloy"));
            ItemDefinition zinc = new ItemDefinition(ResourceLocation.Parse("create:zinc_ingot"));
            Registry<ItemDefinition> items = CreateItemRegistry(alloy, zinc);
            InventoryContainer source = new InventoryContainer(3);
            source.SetStack(2, new ItemStack(zinc, 5));
            source.SetStack(0, new ItemStack(alloy, 7));

            SerializedInventorySlot[] serialized = source.SerializeSlots();
            InventoryContainer restored = new InventoryContainer(3);
            restored.LoadSnapshot(serialized, items);

            Assert.AreEqual(2, serialized.Length);
            Assert.AreEqual(0, serialized[0].SlotIndex);
            Assert.AreEqual(ResourceLocation.Parse("create:andesite_alloy"), serialized[0].Stack.ItemId);
            Assert.AreEqual(2, serialized[1].SlotIndex);
            Assert.AreEqual(ResourceLocation.Parse("create:zinc_ingot"), serialized[1].Stack.ItemId);
            Assert.AreEqual(7, restored.GetStack(0).Count);
            Assert.AreSame(ItemStack.Empty, restored.GetStack(1));
            Assert.AreEqual(5, restored.GetStack(2).Count);
        }

        [Test]
        public void InvalidSerializedSlotsAreRejectedWithoutPartialLoad()
        {
            ItemDefinition alloy = new ItemDefinition(ResourceLocation.Parse("create:andesite_alloy"));
            Registry<ItemDefinition> items = CreateItemRegistry(alloy);
            InventoryContainer inventory = new InventoryContainer(1);
            inventory.SetStack(0, new ItemStack(alloy, 3));

            Assert.Throws<ArgumentOutOfRangeException>(() => new SerializedInventorySlot(-1, new SerializedItemStack(alloy.Id, 1)));
            Assert.Throws<ArgumentException>(() => new SerializedInventorySlot(0, SerializedItemStack.Empty));
            Assert.Throws<ArgumentException>(() => inventory.LoadSnapshot(new[]
            {
                new SerializedInventorySlot(0, new SerializedItemStack(alloy.Id, 1)),
                new SerializedInventorySlot(0, new SerializedItemStack(alloy.Id, 2))
            }, items));
            Assert.Throws<ArgumentOutOfRangeException>(() => inventory.LoadSnapshot(new[]
            {
                new SerializedInventorySlot(4, new SerializedItemStack(alloy.Id, 1))
            }, items));
            Assert.AreEqual(3, inventory.GetStack(0).Count);
        }

        private static Registry<ItemDefinition> CreateItemRegistry(params ItemDefinition[] definitions)
        {
            Registry<ItemDefinition> registry = new Registry<ItemDefinition>(ResourceLocation.Parse("minecraft:item"));
            foreach (ItemDefinition definition in definitions)
                registry.Register(definition.Id, definition);
            registry.Freeze();
            return registry;
        }
    }
}
