using Constructed.Core;
using Constructed.Create;
using Constructed.Minecraft;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class DemoVerticalSliceBootstrapTests
    {
        [Test]
        public void FlatSurfaceWorldCreatesOneChunkOfSurfaceBlocks()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);

            Assert.AreEqual(16, DemoVerticalSliceBootstrap.ChunkSize);
            Assert.AreEqual(256, world.StoredBlockCount);
            Assert.AreSame(catalog.Surface.DefaultState, world.GetBlockState(new BlockPos(0, 0, 0)));
            Assert.AreSame(catalog.Surface.DefaultState, world.GetBlockState(new BlockPos(15, 0, 15)));
            Assert.AreSame(catalog.Air.DefaultState, world.GetBlockState(new BlockPos(16, 0, 0)));
            Assert.AreSame(catalog.Air.DefaultState, world.GetBlockState(new BlockPos(0, 1, 0)));
        }

        [Test]
        public void VerticalSlicePlacementsAreDeterministicAndInsideChunkColumns()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            var placements = DemoVerticalSliceBootstrap.CreateVerticalSlicePlacements(catalog);

            Assert.AreEqual(12, placements.Count);
            Assert.AreEqual(DemoVerticalSliceBootstrap.CreativeMotorPosition, placements[0].Position);
            Assert.AreSame(catalog.CreativeMotor, placements[0].State.Definition);
            Assert.AreEqual(DemoVerticalSliceBootstrap.FirstShaftPosition, placements[1].Position);
            Assert.AreSame(catalog.Shaft, placements[1].State.Definition);
            Assert.AreEqual(DemoVerticalSliceBootstrap.SecondShaftPosition, placements[2].Position);
            Assert.AreSame(catalog.Shaft, placements[2].State.Definition);
            Assert.AreEqual(DemoVerticalSliceBootstrap.BeltStartPosition, placements[3].Position);
            Assert.AreEqual(DemoVerticalSliceBootstrap.BeltEndPosition, placements[8].Position);
            Assert.AreEqual(DemoVerticalSliceBootstrap.CreativeCratePosition, placements[9].Position);
            Assert.AreEqual(DemoVerticalSliceBootstrap.FunnelPosition, placements[10].Position);
            Assert.AreEqual(DemoVerticalSliceBootstrap.ItemVaultPosition, placements[11].Position);

            foreach (WorldBlockEntry placement in placements)
                Assert.IsTrue(DemoVerticalSliceBootstrap.IsInsideDemoChunk(placement.Position));
        }

        [Test]
        public void VerticalSliceWorldAddsPlaceholderMachineLayoutAboveSurface()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateVerticalSliceWorld(catalog);

            Assert.AreEqual(268, world.StoredBlockCount);
            Assert.AreSame(catalog.CreativeMotor, world.GetBlockState(DemoVerticalSliceBootstrap.CreativeMotorPosition).Definition);
            Assert.AreEqual(Direction.East, world.GetBlockState(DemoVerticalSliceBootstrap.CreativeMotorPosition).Get(DemoContentCatalog.FacingProperty));
            Assert.AreSame(catalog.Shaft, world.GetBlockState(DemoVerticalSliceBootstrap.FirstShaftPosition).Definition);
            Assert.AreEqual(Axis.X, world.GetBlockState(DemoVerticalSliceBootstrap.SecondShaftPosition).Get(DemoContentCatalog.AxisProperty));
            Assert.AreSame(catalog.Belt, world.GetBlockState(DemoVerticalSliceBootstrap.BeltStartPosition).Definition);
            Assert.AreSame(catalog.Belt, world.GetBlockState(DemoVerticalSliceBootstrap.BeltEndPosition).Definition);
            Assert.AreEqual(Direction.Down, world.GetBlockState(DemoVerticalSliceBootstrap.CreativeCratePosition).Get(DemoContentCatalog.FacingProperty));
            Assert.AreEqual(Direction.East, world.GetBlockState(DemoVerticalSliceBootstrap.FunnelPosition).Get(DemoContentCatalog.FacingProperty));
            Assert.AreSame(catalog.ItemVault, world.GetBlockState(DemoVerticalSliceBootstrap.ItemVaultPosition).Definition);
            Assert.IsNotNull(world.GetBlockEntity<ItemVaultBlockEntity>(DemoVerticalSliceBootstrap.ItemVaultPosition));
            Assert.AreEqual(1, world.StoredBlockEntityCount);
        }

        [Test]
        public void VerticalSliceWorldRoundTripsThroughExistingSnapshotFormat()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld source = DemoVerticalSliceBootstrap.CreateVerticalSliceWorld(catalog);
            ItemVaultBlockEntity vault = source.GetBlockEntity<ItemVaultBlockEntity>(DemoVerticalSliceBootstrap.ItemVaultPosition);
            vault.Insert(new ItemStack(catalog.DemoTransferItem, 9));

            SerializedBlockWorld snapshot = source.Serialize();
            BlockWorld restored = BlockWorld.Deserialize(catalog.Air.DefaultState, snapshot, catalog.Blocks);
            ItemVaultBlockEntity restoredVault = restored.GetBlockEntity<ItemVaultBlockEntity>(DemoVerticalSliceBootstrap.ItemVaultPosition);

            Assert.AreEqual(snapshot, restored.Serialize());
            Assert.AreEqual(9, restoredVault.Inventory.GetStack(0).Count);
            Assert.AreSame(catalog.DemoTransferItem, restoredVault.Inventory.GetStack(0).Item);
        }
    }
}
