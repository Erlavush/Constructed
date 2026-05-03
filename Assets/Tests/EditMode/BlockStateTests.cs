using System;
using Constructed.Core;
using Constructed.Minecraft;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class BlockStateTests
    {
        [Test]
        public void WithReturnsNewStateAndLeavesOriginalUnchanged()
        {
            StateProperty<Axis> axis = AxisProperty();
            BlockDefinition shaft = new BlockDefinition(ResourceLocation.Parse("create:shaft"), new IStateProperty[] { axis });

            BlockState original = shaft.DefaultState;
            BlockState changed = original.With(axis, Axis.X);

            Assert.AreNotSame(original, changed);
            Assert.AreEqual(Axis.Y, original.Get(axis));
            Assert.AreEqual(Axis.X, changed.Get(axis));
        }

        [Test]
        public void WithSameValueReturnsSameState()
        {
            StateProperty<Axis> axis = AxisProperty();
            BlockDefinition shaft = new BlockDefinition(ResourceLocation.Parse("create:shaft"), new IStateProperty[] { axis });

            BlockState state = shaft.DefaultState;

            Assert.AreSame(state, state.With(axis, Axis.Y));
        }

        [Test]
        public void WrongPropertyOrInvalidValueIsRejected()
        {
            StateProperty<Axis> axis = AxisProperty();
            StateProperty<Direction> facing = new StateProperty<Direction>("facing", new[] { Direction.North, Direction.South }, Direction.North);
            BlockDefinition shaft = new BlockDefinition(ResourceLocation.Parse("create:shaft"), new IStateProperty[] { axis });

            Assert.Throws<ArgumentException>(() => shaft.DefaultState.With(facing, Direction.South));
            Assert.Throws<ArgumentException>(() => shaft.DefaultState.WithValue(axis, AxisDirection.Positive));
        }

        [Test]
        public void SerializeRoundTripsThroughDefinition()
        {
            StateProperty<Axis> axis = AxisProperty();
            StateProperty<bool> waterlogged = StateProperty<bool>.Bool("waterlogged");
            BlockDefinition shaft = new BlockDefinition(ResourceLocation.Parse("create:shaft"), new IStateProperty[] { axis, waterlogged });

            BlockState state = shaft.DefaultState.With(axis, Axis.Z).With(waterlogged, true);
            SerializedBlockState serialized = state.Serialize();
            BlockState restored = shaft.CreateState(serialized);

            Assert.AreEqual(ResourceLocation.Parse("create:shaft"), serialized.BlockId);
            Assert.AreEqual("z", serialized.GetProperty("axis"));
            Assert.AreEqual("true", serialized.GetProperty("waterlogged"));
            Assert.AreEqual(state, restored);
        }

        private static StateProperty<Axis> AxisProperty()
        {
            return new StateProperty<Axis>("axis", new[] { Axis.X, Axis.Y, Axis.Z }, Axis.Y);
        }
    }
}
