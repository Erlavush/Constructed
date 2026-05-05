using System;
using System.Collections.Generic;
using Constructed.Core;
using Constructed.Minecraft;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class ItemStackTests
    {
        [Test]
        public void StackRequiresDefinitionAndCountWithinMax()
        {
            ItemDefinition goggles = new ItemDefinition(ResourceLocation.Parse("create:goggles"), 1);
            ItemDefinition alloy = new ItemDefinition(ResourceLocation.Parse("create:andesite_alloy"));

            Assert.Throws<ArgumentNullException>(() => new ItemStack(null, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ItemStack(alloy, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ItemStack(goggles, 2));

            ItemStack stack = new ItemStack(goggles, 1);
            Assert.AreSame(goggles, stack.Item);
            Assert.AreEqual(1, stack.Count);
            Assert.IsFalse(stack.IsEmpty);
        }

        [Test]
        public void EmptyStackSerializesAsEmpty()
        {
            Assert.IsTrue(ItemStack.Empty.IsEmpty);
            Assert.AreEqual(0, ItemStack.Empty.Count);
            Assert.AreEqual(SerializedItemStack.Empty, ItemStack.Empty.Serialize());
            Assert.AreEqual("empty", ItemStack.Empty.ToString());
        }

        [Test]
        public void WithCountCreatesNewImmutableStackOrEmpty()
        {
            ItemDefinition alloy = new ItemDefinition(ResourceLocation.Parse("create:andesite_alloy"));
            ItemStack stack = new ItemStack(alloy, 4);

            ItemStack changed = stack.WithCount(12);
            ItemStack emptied = stack.WithCount(0);

            Assert.AreEqual(4, stack.Count);
            Assert.AreEqual(12, changed.Count);
            Assert.AreSame(alloy, changed.Item);
            Assert.AreSame(ItemStack.Empty, emptied);
            Assert.Throws<ArgumentOutOfRangeException>(() => stack.WithCount(-1));
            Assert.Throws<InvalidOperationException>(() => ItemStack.Empty.WithCount(1));
        }

        [Test]
        public void SplitTakesRequestedCountWithoutMutatingSource()
        {
            ItemDefinition alloy = new ItemDefinition(ResourceLocation.Parse("create:andesite_alloy"));
            ItemStack source = new ItemStack(alloy, 8);

            ItemStack taken = source.Split(3, out ItemStack remainder);
            ItemStack allTaken = source.Split(20, out ItemStack emptyRemainder);

            Assert.AreEqual(8, source.Count);
            Assert.AreEqual(3, taken.Count);
            Assert.AreEqual(5, remainder.Count);
            Assert.AreSame(alloy, taken.Item);
            Assert.AreSame(alloy, remainder.Item);
            Assert.AreEqual(8, allTaken.Count);
            Assert.AreSame(ItemStack.Empty, emptyRemainder);
            Assert.Throws<ArgumentOutOfRangeException>(() => source.Split(0, out _));
        }

        [Test]
        public void MergeFillsCapacityAndReturnsOverflow()
        {
            ItemDefinition tea = new ItemDefinition(ResourceLocation.Parse("create:builders_tea"), 16);
            ItemStack partial = new ItemStack(tea, 12);
            ItemStack incoming = new ItemStack(tea, 8);

            ItemStack merged = partial.Merge(incoming, out ItemStack overflow);

            Assert.AreEqual(16, merged.Count);
            Assert.AreEqual(4, overflow.Count);
            Assert.AreSame(tea, merged.Item);
            Assert.AreSame(tea, overflow.Item);
            Assert.AreEqual(12, partial.Count);
            Assert.AreEqual(8, incoming.Count);
        }

        [Test]
        public void MergeHandlesEmptyFullAndDifferentItems()
        {
            ItemDefinition alloy = new ItemDefinition(ResourceLocation.Parse("create:andesite_alloy"));
            ItemDefinition zinc = new ItemDefinition(ResourceLocation.Parse("create:zinc_ingot"));
            ItemStack full = new ItemStack(alloy, alloy.MaxStackSize);
            ItemStack incoming = new ItemStack(alloy, 4);

            ItemStack fromEmpty = ItemStack.Empty.Merge(incoming, out ItemStack emptyOverflow);
            ItemStack stillFull = full.Merge(incoming, out ItemStack fullOverflow);

            Assert.AreSame(incoming, fromEmpty);
            Assert.AreSame(ItemStack.Empty, emptyOverflow);
            Assert.AreSame(full, stillFull);
            Assert.AreSame(incoming, fullOverflow);
            Assert.IsTrue(ItemStack.Empty.CanMerge(incoming));
            Assert.IsFalse(incoming.CanMerge(new ItemStack(zinc, 1)));
            Assert.Throws<InvalidOperationException>(() => incoming.Merge(new ItemStack(zinc, 1), out _));
        }

        [Test]
        public void SerializedStackRoundTripsThroughRegistry()
        {
            ItemDefinition alloy = new ItemDefinition(ResourceLocation.Parse("create:andesite_alloy"));
            Registry<ItemDefinition> items = new Registry<ItemDefinition>(ResourceLocation.Parse("minecraft:item"));
            items.Register(alloy.Id, alloy);
            items.Freeze();

            ItemStack stack = new ItemStack(alloy, 32);
            SerializedItemStack serialized = stack.Serialize();
            ItemStack restored = ItemStack.Deserialize(serialized, items);

            Assert.AreEqual(ResourceLocation.Parse("create:andesite_alloy"), serialized.ItemId);
            Assert.AreEqual(32, serialized.Count);
            Assert.AreEqual("create:andesite_alloy x32", serialized.ToString());
            Assert.AreEqual(stack, restored);
            Assert.AreSame(ItemStack.Empty, ItemStack.Deserialize(SerializedItemStack.Empty, items));
            Assert.Throws<KeyNotFoundException>(() => ItemStack.Deserialize(new SerializedItemStack(ResourceLocation.Parse("create:missing"), 1), items));
            Assert.Throws<ArgumentNullException>(() => ItemStack.Deserialize(serialized, null));
        }

        [Test]
        public void InvalidSerializedStackInputsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new SerializedItemStack(default(ResourceLocation), 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SerializedItemStack(ResourceLocation.Parse("create:bad"), 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SerializedItemStack(ResourceLocation.Parse("create:bad"), -1));
        }
    }
}
