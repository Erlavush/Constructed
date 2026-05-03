using Constructed.Core;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class BlockPosTests
    {
        [Test]
        public void RelativeUsesDirectionNormals()
        {
            BlockPos origin = new BlockPos(10, 20, 30);

            Assert.AreEqual(new BlockPos(10, 21, 30), origin.Relative(Direction.Up));
            Assert.AreEqual(new BlockPos(10, 18, 30), origin.Relative(Direction.Down, 2));
            Assert.AreEqual(new BlockPos(10, 20, 27), origin.Relative(Direction.North, 3));
            Assert.AreEqual(new BlockPos(14, 20, 30), origin.Relative(Direction.East, 4));
        }

        [Test]
        public void SubtractAndManhattanDistanceAreDeterministic()
        {
            BlockPos a = new BlockPos(7, -2, 4);
            BlockPos b = new BlockPos(2, 3, -1);

            Assert.AreEqual(new BlockPos(5, -5, 5), a - b);
            Assert.AreEqual(15, a.DistManhattan(b));
        }
    }
}
