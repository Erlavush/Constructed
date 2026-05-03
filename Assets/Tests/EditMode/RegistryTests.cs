using System;
using Constructed.Core;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class RegistryTests
    {
        [Test]
        public void RegisterStoresEntriesWithStableIdsAndIndices()
        {
            Registry<TestDefinition> registry = NewRegistry();
            TestDefinition shaft = new TestDefinition("shaft");
            TestDefinition cogwheel = new TestDefinition("cogwheel");

            RegistryEntry<TestDefinition> first = registry.Register("create:shaft", shaft);
            RegistryEntry<TestDefinition> second = registry.Register("create:cogwheel", cogwheel);

            Assert.AreEqual(ResourceLocation.Parse("create:shaft"), first.Id);
            Assert.AreEqual(0, first.Index);
            Assert.AreSame(shaft, first.Value);
            Assert.AreEqual(1, second.Index);
            Assert.AreSame(cogwheel, registry.GetValue(ResourceLocation.Parse("create:cogwheel")));
            Assert.AreEqual(ResourceLocation.Parse("create:cogwheel"), registry.GetId(cogwheel));
            Assert.AreEqual(2, registry.Count);
            Assert.AreEqual(first, registry.Entries[0]);
            Assert.AreEqual(second, registry.Entries[1]);
        }

        [Test]
        public void DuplicateIdsAreRejected()
        {
            Registry<TestDefinition> registry = NewRegistry();
            registry.Register("create:shaft", new TestDefinition("shaft"));

            Assert.Throws<ArgumentException>(() => registry.Register("create:shaft", new TestDefinition("other")));
        }

        [Test]
        public void DuplicateValuesAreRejected()
        {
            Registry<TestDefinition> registry = NewRegistry();
            TestDefinition definition = new TestDefinition("shared");
            registry.Register("create:first", definition);

            Assert.Throws<ArgumentException>(() => registry.Register("create:second", definition));
        }

        [Test]
        public void UninitializedIdsAreRejected()
        {
            Registry<TestDefinition> registry = NewRegistry();

            Assert.Throws<ArgumentException>(() => registry.Register(default(ResourceLocation), new TestDefinition("bad")));
        }

        [Test]
        public void FrozenRegistryRejectsLateRegistration()
        {
            Registry<TestDefinition> registry = NewRegistry();
            registry.Register("create:shaft", new TestDefinition("shaft"));
            registry.Freeze();

            Assert.IsTrue(registry.IsFrozen);
            Assert.Throws<InvalidOperationException>(() => registry.Register("create:cogwheel", new TestDefinition("cogwheel")));
        }

        private static Registry<TestDefinition> NewRegistry()
        {
            return new Registry<TestDefinition>(ResourceLocation.Parse("constructed:test_registry"));
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
