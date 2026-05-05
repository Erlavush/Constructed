using System;
using Constructed.Core;
using Constructed.Minecraft;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class ItemDefinitionTests
    {
        [Test]
        public void DefinitionOwnsIdAndMaxStackSize()
        {
            ItemDefinition andesite = new ItemDefinition(ResourceLocation.Parse("create:andesite_alloy"));
            ItemDefinition goggles = new ItemDefinition(ResourceLocation.Parse("create:goggles"), 1);

            Assert.AreEqual(ResourceLocation.Parse("create:andesite_alloy"), andesite.Id);
            Assert.AreEqual(64, andesite.MaxStackSize);
            Assert.AreEqual(ResourceLocation.Parse("create:goggles"), goggles.Id);
            Assert.AreEqual(1, goggles.MaxStackSize);
            Assert.AreEqual("create:goggles", goggles.ToString());
        }

        [Test]
        public void InvalidDefinitionInputsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new ItemDefinition(default(ResourceLocation)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ItemDefinition(ResourceLocation.Parse("create:bad"), 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ItemDefinition(ResourceLocation.Parse("create:bad"), -1));
        }
    }
}
