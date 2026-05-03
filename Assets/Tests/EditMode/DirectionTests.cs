using System;
using Constructed.Core;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class DirectionTests
    {
        [Test]
        public void OppositesMatchMinecraftDirections()
        {
            Assert.AreEqual(Direction.Down, Direction.Up.Opposite());
            Assert.AreEqual(Direction.Up, Direction.Down.Opposite());
            Assert.AreEqual(Direction.South, Direction.North.Opposite());
            Assert.AreEqual(Direction.North, Direction.South.Opposite());
            Assert.AreEqual(Direction.East, Direction.West.Opposite());
            Assert.AreEqual(Direction.West, Direction.East.Opposite());
        }

        [Test]
        public void HorizontalClockwiseRotatesAroundYAxis()
        {
            Assert.AreEqual(Direction.East, Direction.North.ClockWise());
            Assert.AreEqual(Direction.South, Direction.East.ClockWise());
            Assert.AreEqual(Direction.West, Direction.South.ClockWise());
            Assert.AreEqual(Direction.North, Direction.West.ClockWise());
        }

        [Test]
        public void VerticalDirectionsDoNotRotateHorizontally()
        {
            Assert.Throws<InvalidOperationException>(() => Direction.Up.ClockWise());
            Assert.Throws<InvalidOperationException>(() => Direction.Down.CounterClockWise());
        }

        [Test]
        public void AxisAndStepsMatchGridNormals()
        {
            Assert.AreEqual(Axis.Y, Direction.Up.Axis());
            Assert.AreEqual(Axis.Z, Direction.North.Axis());
            Assert.AreEqual(Axis.X, Direction.West.Axis());

            Assert.AreEqual(AxisDirection.Positive, Direction.East.AxisDirection());
            Assert.AreEqual(AxisDirection.Negative, Direction.North.AxisDirection());

            Assert.AreEqual(new BlockPos(0, 0, -1), Direction.North.Normal());
            Assert.AreEqual(new BlockPos(-1, 0, 0), Direction.West.Normal());
        }
    }
}
