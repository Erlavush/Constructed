using Constructed.Core;
using Constructed.Create;
using Constructed.Minecraft;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class DemoKineticPlacementRulesTests
    {
        [Test]
        public void CreativeMotorPlacementFacesTowardCompatibleNeighborShaft()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);
            BlockPos placementPosition = new BlockPos(4, DemoVerticalSliceBootstrap.MachineY, 4);
            world.SetBlockState(
                placementPosition.Relative(Direction.East),
                catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));

            Direction facing = DemoKineticPlacementRules.ResolveCreativeMotorFacing(
                catalog,
                world,
                placementPosition,
                Direction.North);

            Assert.AreEqual(Direction.East, facing);
        }

        [Test]
        public void CreativeMotorPlacementFallsBackToOppositeNearestLookingDirection()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);

            Direction facing = DemoKineticPlacementRules.ResolveCreativeMotorFacing(
                catalog,
                world,
                new BlockPos(4, DemoVerticalSliceBootstrap.MachineY, 4),
                Direction.North);

            Assert.AreEqual(Direction.South, facing);
        }

        [Test]
        public void ShaftPlacementPrefersCompatibleNeighborAxis()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);
            BlockPos placementPosition = new BlockPos(4, DemoVerticalSliceBootstrap.MachineY, 4);
            world.SetBlockState(
                placementPosition.Relative(Direction.West),
                catalog.CreativeMotor.DefaultState.With(DemoContentCatalog.FacingProperty, Direction.East));

            Axis axis = DemoKineticPlacementRules.ResolveShaftAxis(
                catalog,
                world,
                placementPosition,
                Direction.Up);

            Assert.AreEqual(Axis.X, axis);
        }

        [Test]
        public void CreatePlacementStateBuildsDefaultShaftFromLookAxisWhenNoNeighborMatches()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);

            BlockState placementState = DemoKineticPlacementRules.CreatePlacementState(
                catalog,
                world,
                catalog.Shaft.Id,
                new BlockPos(4, DemoVerticalSliceBootstrap.MachineY, 4),
                Direction.Up);

            Assert.AreEqual(catalog.Shaft.Id, placementState.Definition.Id);
            Assert.AreEqual(Axis.Y, placementState.Get(DemoContentCatalog.AxisProperty));
        }

        [Test]
        public void CreatePlacementStateBuildsCogwheelFromCompatibleNeighborAxis()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);
            BlockPos shaftPosition = new BlockPos(4, DemoVerticalSliceBootstrap.MachineY, 4);
            BlockPos cogPosition = shaftPosition.Relative(Direction.East);
            world.SetBlockState(
                shaftPosition,
                catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));

            Assert.IsTrue(DemoKineticPlacementRules.IsPlaceableBlock(catalog, catalog.Cogwheel.Id));
            Assert.IsTrue(DemoKineticPlacementRules.IsPlaceableBlock(catalog, catalog.LargeCogwheel.Id));
            Assert.IsTrue(DemoKineticPlacementRules.IsPlaceableBlock(catalog, catalog.Gearbox.Id));

            BlockState placementState = DemoKineticPlacementRules.CreatePlacementState(
                catalog,
                world,
                catalog.Cogwheel.Id,
                cogPosition,
                Direction.Up);

            Assert.AreEqual(catalog.Cogwheel.Id, placementState.Definition.Id);
            Assert.AreEqual(Axis.X, placementState.Get(DemoContentCatalog.AxisProperty));
        }

        [Test]
        public void CreatePlacementStateBuildsGearboxWithSourcePlacementAxisDefault()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);

            BlockState placementState = DemoKineticPlacementRules.CreatePlacementState(
                catalog,
                world,
                catalog.Gearbox.Id,
                new BlockPos(4, DemoVerticalSliceBootstrap.MachineY, 4),
                Direction.North);

            Assert.AreEqual(catalog.Gearbox.Id, placementState.Definition.Id);
            Assert.AreEqual(Axis.Y, placementState.Get(DemoContentCatalog.AxisProperty));
        }

        [Test]
        public void CreatePlacementStateBuildsDefaultGrassBlock()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);

            Assert.IsTrue(DemoKineticPlacementRules.IsPlaceableBlock(catalog, catalog.Surface.Id));

            BlockState placementState = DemoKineticPlacementRules.CreatePlacementState(
                catalog,
                world,
                catalog.Surface.Id,
                new BlockPos(0, DemoVerticalSliceBootstrap.MachineY, 0),
                Direction.North);

            Assert.AreSame(catalog.Surface, placementState.Definition);
        }
    }
}
