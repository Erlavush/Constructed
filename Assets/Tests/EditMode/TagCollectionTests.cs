using System;
using Constructed.Core;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class TagCollectionTests
    {
        [Test]
        public void ContainsIdsAndRegisteredValues()
        {
            Registry<TestDefinition> registry = NewRegistry();
            TestDefinition shaft = new TestDefinition("shaft");
            TestDefinition cogwheel = new TestDefinition("cogwheel");
            registry.Register("create:shaft", shaft);
            registry.Register("create:cogwheel", cogwheel);

            TagCollection<TestDefinition> tags = new TagCollection<TestDefinition>(registry);
            TagKey<TestDefinition> relays = TagKey<TestDefinition>.Parse(registry.RegistryId, "create:kinetic_relays");
            tags.Add(relays, ResourceLocation.Parse("create:shaft"));
            tags.Add(relays, cogwheel);

            Assert.IsTrue(tags.Contains(relays, ResourceLocation.Parse("create:shaft")));
            Assert.IsTrue(tags.Contains(relays, cogwheel));
            Assert.AreEqual(2, tags.GetIds(relays).Count);
            Assert.AreSame(shaft, tags.GetValues(relays)[0]);
            Assert.AreSame(cogwheel, tags.GetValues(relays)[1]);
        }

        [Test]
        public void ReplaceOverwritesAndDeduplicatesMembership()
        {
            Registry<TestDefinition> registry = NewRegistry();
            TestDefinition shaft = new TestDefinition("shaft");
            TestDefinition cogwheel = new TestDefinition("cogwheel");
            registry.Register("create:shaft", shaft);
            registry.Register("create:cogwheel", cogwheel);

            TagCollection<TestDefinition> tags = new TagCollection<TestDefinition>(registry);
            TagKey<TestDefinition> relays = TagKey<TestDefinition>.Parse(registry.RegistryId, "create:kinetic_relays");
            tags.Replace(relays, new[]
            {
                ResourceLocation.Parse("create:shaft"),
                ResourceLocation.Parse("create:cogwheel"),
                ResourceLocation.Parse("create:shaft")
            });

            Assert.AreEqual(2, tags.GetIds(relays).Count);

            tags.Replace(relays, new[] { ResourceLocation.Parse("create:cogwheel") });

            Assert.IsFalse(tags.Contains(relays, ResourceLocation.Parse("create:shaft")));
            Assert.IsTrue(tags.Contains(relays, ResourceLocation.Parse("create:cogwheel")));
            Assert.AreEqual(1, tags.GetValues(relays).Count);
        }

        [Test]
        public void WrongRegistryTagIsRejected()
        {
            Registry<TestDefinition> registry = NewRegistry();
            TagCollection<TestDefinition> tags = new TagCollection<TestDefinition>(registry);
            TagKey<TestDefinition> itemTag = TagKey<TestDefinition>.Parse(ResourceLocation.Parse("minecraft:item"), "create:kinetic_relays");

            Assert.Throws<ArgumentException>(() => tags.Add(itemTag, ResourceLocation.Parse("create:shaft")));
        }

        [Test]
        public void UninitializedMemberIdsAreRejected()
        {
            Registry<TestDefinition> registry = NewRegistry();
            TagCollection<TestDefinition> tags = new TagCollection<TestDefinition>(registry);
            TagKey<TestDefinition> relays = TagKey<TestDefinition>.Parse(registry.RegistryId, "create:kinetic_relays");

            Assert.Throws<ArgumentException>(() => tags.Add(relays, default(ResourceLocation)));
            Assert.Throws<ArgumentException>(() => tags.Replace(relays, new[] { default(ResourceLocation) }));
        }

        private static Registry<TestDefinition> NewRegistry()
        {
            return new Registry<TestDefinition>(ResourceLocation.Parse("minecraft:block"));
        }

        private sealed class TestDefinition
        {
            public TestDefinition(string name)
            {
                Name = name;
            }

            public string Name { get; }
        }
    }
}
