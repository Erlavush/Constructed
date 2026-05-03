using System;
using Constructed.Core;
using Constructed.Minecraft;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class StatePropertyTests
    {
        [Test]
        public void PropertyStoresValidValuesDefaultAndSerializedNames()
        {
            StateProperty<Axis> axis = new StateProperty<Axis>("axis", new[] { Axis.X, Axis.Y, Axis.Z }, Axis.Y);

            Assert.AreEqual("axis", axis.Name);
            Assert.AreEqual(Axis.Y, axis.Default);
            Assert.AreEqual(3, axis.Values.Count);
            Assert.IsTrue(axis.Contains(Axis.X));
            Assert.AreEqual("x", axis.Serialize(Axis.X));
            Assert.AreEqual(Axis.Z, axis.Parse("z"));
        }

        [Test]
        public void InvalidNamesDefaultsAndDuplicateSerializedValuesAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new StateProperty<Axis>("Axis", new[] { Axis.X }, Axis.X));
            Assert.Throws<ArgumentException>(() => new StateProperty<Axis>("axis", new[] { Axis.X }, Axis.Y));
            Assert.Throws<ArgumentException>(() => new StateProperty<int>("number", new[] { 1, 2 }, 1, value => "same"));
        }

        [Test]
        public void BoolPropertyUsesMinecraftStyleLowercaseValues()
        {
            StateProperty<bool> powered = StateProperty<bool>.Bool("powered", true);

            Assert.AreEqual(true, powered.Default);
            Assert.AreEqual("false", powered.Serialize(false));
            Assert.AreEqual(true, powered.Parse("true"));
        }
    }
}
