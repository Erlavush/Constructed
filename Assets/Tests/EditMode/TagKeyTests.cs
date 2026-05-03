using Constructed.Core;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class TagKeyTests
    {
        [Test]
        public void EqualityIncludesRegistryAndTagId()
        {
            ResourceLocation blockRegistry = ResourceLocation.Parse("minecraft:block");
            ResourceLocation itemRegistry = ResourceLocation.Parse("minecraft:item");

            TagKey<object> blockTag = TagKey<object>.Parse(blockRegistry, "create:kinetic_relays");
            TagKey<object> sameBlockTag = TagKey<object>.Parse(blockRegistry, "create:kinetic_relays");
            TagKey<object> itemTag = TagKey<object>.Parse(itemRegistry, "create:kinetic_relays");

            Assert.AreEqual(blockTag, sameBlockTag);
            Assert.AreNotEqual(blockTag, itemTag);
            Assert.AreEqual("minecraft:block#create:kinetic_relays", blockTag.ToString());
        }
    }
}
