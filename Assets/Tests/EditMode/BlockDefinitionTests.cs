using System;
using System.Collections.Generic;
using Constructed.Core;
using Constructed.Minecraft;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class BlockDefinitionTests
    {
        [Test]
        public void DefinitionOwnsIdPropertiesAndDefaultState()
        {
            StateProperty<Axis> axis = AxisProperty();
            StateProperty<bool> waterlogged = StateProperty<bool>.Bool("waterlogged");
            BlockDefinition shaft = new BlockDefinition(ResourceLocation.Parse("create:shaft"), new IStateProperty[] { axis, waterlogged });

            Assert.AreEqual(ResourceLocation.Parse("create:shaft"), shaft.Id);
            Assert.AreEqual(2, shaft.Properties.Count);
            Assert.IsTrue(shaft.HasProperty(axis));
            Assert.IsTrue(shaft.HasProperty("waterlogged"));
            Assert.AreSame(axis, shaft.GetProperty("axis"));
            Assert.AreEqual(Axis.Y, shaft.DefaultState.Get(axis));
            Assert.AreEqual(false, shaft.DefaultState.Get(waterlogged));
            Assert.AreEqual("create:shaft[axis=y,waterlogged=false]", shaft.DefaultState.ToString());
        }

        [Test]
        public void DuplicatePropertyNamesAreRejected()
        {
            StateProperty<Axis> axis = AxisProperty();
            StateProperty<Direction> duplicateAxis = new StateProperty<Direction>("axis", new[] { Direction.North, Direction.South }, Direction.North);

            Assert.Throws<ArgumentException>(() => new BlockDefinition(ResourceLocation.Parse("create:bad"), new IStateProperty[] { axis, duplicateAxis }));
        }

        [Test]
        public void CreateStateParsesSerializedValuesFromDefaultState()
        {
            StateProperty<Axis> axis = AxisProperty();
            StateProperty<bool> waterlogged = StateProperty<bool>.Bool("waterlogged");
            BlockDefinition shaft = new BlockDefinition(ResourceLocation.Parse("create:shaft"), new IStateProperty[] { axis, waterlogged });

            BlockState state = shaft.CreateState(new[]
            {
                new BlockStatePropertyValue("axis", "x"),
                new BlockStatePropertyValue("waterlogged", "true")
            });

            Assert.AreEqual(Axis.X, state.Get(axis));
            Assert.AreEqual(true, state.Get(waterlogged));
            Assert.AreEqual("create:shaft[axis=x,waterlogged=true]", state.Serialize().ToString());
        }

        [Test]
        public void CreateStateRejectsWrongBlockIdUnknownPropertiesAndDuplicateProperties()
        {
            StateProperty<Axis> axis = AxisProperty();
            BlockDefinition shaft = new BlockDefinition(ResourceLocation.Parse("create:shaft"), new IStateProperty[] { axis });

            Assert.Throws<ArgumentException>(() => shaft.CreateState(new SerializedBlockState(ResourceLocation.Parse("create:cogwheel"), null)));
            Assert.Throws<KeyNotFoundException>(() => shaft.CreateState(new[] { new BlockStatePropertyValue("powered", "true") }));
            Assert.Throws<ArgumentException>(() => shaft.CreateState(new[]
            {
                new BlockStatePropertyValue("axis", "x"),
                new BlockStatePropertyValue("axis", "y")
            }));
        }

        private static StateProperty<Axis> AxisProperty()
        {
            return new StateProperty<Axis>("axis", new[] { Axis.X, Axis.Y, Axis.Z }, Axis.Y);
        }
    }
}
