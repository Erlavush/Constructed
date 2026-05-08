using Constructed.Core;
using Constructed.Create;
using Constructed.Minecraft;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class BeltBlockLifecycleTests
    {
        [Test]
        public void RemovingBeltSegmentDestroysWholeChainAndLeavesShaftsAtPulleys()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);
            
            // Setup a 3-segment belt: (2,MachineY,2) to (2,MachineY,4)
            BlockPos start = new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 2);
            BlockPos middle = new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 3);
            BlockPos end = new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 4);

            world.SetBlockState(start, catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));
            world.SetBlockState(end, catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));
            
            Assert.IsTrue(DemoBeltPlacementService.TryCreateConnection(world, catalog, start, end, out _));
            
            // Verify initial state
            Assert.AreEqual(catalog.Belt.Id, world.GetBlockState(start).Definition.Id);
            Assert.AreEqual(catalog.Belt.Id, world.GetBlockState(middle).Definition.Id);
            Assert.AreEqual(catalog.Belt.Id, world.GetBlockState(end).Definition.Id);

            // Remove the MIDDLE segment
            world.RemoveBlock(middle);

            // Verify the whole chain is gone
            Assert.IsTrue(world.IsAir(world.GetBlockState(middle)));
            
            // Start and End should have become SHAFTS because they had pulleys
            BlockState startState = world.GetBlockState(start);
            Assert.AreEqual(catalog.Shaft.Id, startState.Definition.Id);
            Assert.AreEqual(Axis.X, startState.Get(DemoContentCatalog.AxisProperty));
            
            BlockState endState = world.GetBlockState(end);
            Assert.AreEqual(catalog.Shaft.Id, endState.Definition.Id);
            Assert.AreEqual(Axis.X, endState.Get(DemoContentCatalog.AxisProperty));
        }

        [Test]
        public void RemovingStartSegmentLeavesShaftAndDestroysChain()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);
            
            BlockPos start = new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 2);
            BlockPos middle = new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 3);
            BlockPos end = new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 4);

            world.SetBlockState(start, catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));
            world.SetBlockState(end, catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));
            
            DemoBeltPlacementService.TryCreateConnection(world, catalog, start, end, out _);

            // Remove the START segment
            world.RemoveBlock(start);

            // Start should be a shaft
            Assert.AreEqual(catalog.Shaft.Id, world.GetBlockState(start).Definition.Id);
            Assert.AreEqual(Axis.X, world.GetBlockState(start).Get(DemoContentCatalog.AxisProperty));
            
            // Middle and End should be gone
            Assert.IsTrue(world.IsAir(world.GetBlockState(middle)));
            Assert.AreEqual(catalog.Shaft.Id, world.GetBlockState(end).Definition.Id);
        }
    }
}
