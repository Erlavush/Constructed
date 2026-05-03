using System;
using Constructed.Core;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class ResourceLocationTests
    {
        [Test]
        public void ParseAcceptsExplicitNamespace()
        {
            ResourceLocation id = ResourceLocation.Parse("create:shaft");

            Assert.AreEqual("create", id.Namespace);
            Assert.AreEqual("shaft", id.Path);
            Assert.AreEqual("create:shaft", id.ToString());
        }

        [Test]
        public void ParseUsesMinecraftAsDefaultNamespace()
        {
            ResourceLocation id = ResourceLocation.Parse("stone");

            Assert.AreEqual(new ResourceLocation("minecraft", "stone"), id);
        }

        [Test]
        public void ParseCanUseCustomDefaultNamespace()
        {
            ResourceLocation id = ResourceLocation.Parse("shaft", "create");

            Assert.AreEqual(new ResourceLocation("create", "shaft"), id);
        }

        [Test]
        public void InvalidCharactersAreRejected()
        {
            Assert.Throws<ArgumentException>(() => ResourceLocation.Parse("Create:shaft"));
            Assert.Throws<ArgumentException>(() => ResourceLocation.Parse("create:Mechanical Press"));
            Assert.Throws<FormatException>(() => ResourceLocation.Parse("create:bad:extra"));
        }
    }
}
