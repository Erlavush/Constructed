using Constructed.Core;
using Constructed.Create;
using Constructed.Minecraft;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class DemoVerticalSliceBootstrapTests
    {
        [Test]
        public void FlatSurfaceWorldCreatesFocusedEightByEightPlatform()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);

            Assert.AreEqual(16, DemoVerticalSliceBootstrap.ChunkSize);
            Assert.AreEqual(8, DemoVerticalSliceBootstrap.PlatformSize);
            Assert.AreEqual(64, world.StoredBlockCount);
            Assert.AreSame(catalog.Surface.DefaultState, world.GetBlockState(new BlockPos(0, 0, 0)));
            Assert.AreSame(catalog.Surface.DefaultState, world.GetBlockState(new BlockPos(7, 0, 7)));
            Assert.AreSame(catalog.Air.DefaultState, world.GetBlockState(new BlockPos(8, 0, 0)));
            Assert.AreSame(catalog.Air.DefaultState, world.GetBlockState(new BlockPos(0, 1, 0)));
        }

        [Test]
        public void VerticalSlicePlacementsAreDeterministicAndInsideChunkColumns()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            var placements = DemoVerticalSliceBootstrap.CreateVerticalSlicePlacements(catalog);

            Assert.AreEqual(3, placements.Count);
            Assert.AreEqual(DemoVerticalSliceBootstrap.CreativeMotorPosition, placements[0].Position);
            Assert.AreSame(catalog.CreativeMotor, placements[0].State.Definition);
            Assert.AreEqual(DemoVerticalSliceBootstrap.FirstShaftPosition, placements[1].Position);
            Assert.AreSame(catalog.Shaft, placements[1].State.Definition);
            Assert.AreEqual(DemoVerticalSliceBootstrap.SecondShaftPosition, placements[2].Position);
            Assert.AreSame(catalog.Shaft, placements[2].State.Definition);

            foreach (WorldBlockEntry placement in placements)
                Assert.IsTrue(DemoVerticalSliceBootstrap.IsInsideDemoChunk(placement.Position));
        }

        [Test]
        public void VerticalSliceWorldAddsFocusedMotorAndShaftLayoutAbovePlatform()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateVerticalSliceWorld(catalog);

            Assert.AreEqual(67, world.StoredBlockCount);
            Assert.AreSame(catalog.CreativeMotor, world.GetBlockState(DemoVerticalSliceBootstrap.CreativeMotorPosition).Definition);
            Assert.AreEqual(Direction.East, world.GetBlockState(DemoVerticalSliceBootstrap.CreativeMotorPosition).Get(DemoContentCatalog.FacingProperty));
            Assert.AreSame(catalog.Shaft, world.GetBlockState(DemoVerticalSliceBootstrap.FirstShaftPosition).Definition);
            Assert.AreEqual(Axis.X, world.GetBlockState(DemoVerticalSliceBootstrap.SecondShaftPosition).Get(DemoContentCatalog.AxisProperty));
            Assert.AreSame(catalog.Air.DefaultState, world.GetBlockState(new BlockPos(5, DemoVerticalSliceBootstrap.MachineY, 4)));
            Assert.AreEqual(0, world.StoredBlockEntityCount);
        }

        [Test]
        public void VerticalSliceWorldRoundTripsThroughExistingSnapshotFormat()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld source = DemoVerticalSliceBootstrap.CreateVerticalSliceWorld(catalog);

            SerializedBlockWorld snapshot = source.Serialize();
            BlockWorld restored = BlockWorld.Deserialize(catalog.Air.DefaultState, snapshot, catalog.Blocks);

            Assert.AreEqual(snapshot, restored.Serialize());
            Assert.AreEqual(0, restored.StoredBlockEntityCount);
            Assert.AreSame(catalog.CreativeMotor, restored.GetBlockState(DemoVerticalSliceBootstrap.CreativeMotorPosition).Definition);
            Assert.AreSame(catalog.Shaft, restored.GetBlockState(DemoVerticalSliceBootstrap.FirstShaftPosition).Definition);
        }
    }
}
