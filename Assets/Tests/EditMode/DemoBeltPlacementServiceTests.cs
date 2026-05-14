using Constructed.Core;
using Constructed.Create;
using Constructed.Minecraft;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class DemoBeltPlacementServiceTests
    {
        [Test]
        public void ValidateShaftEndpointRequiresAShaft()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);
            BlockPos shaftPosition = new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 2);
            world.SetBlockState(shaftPosition, catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));

            Assert.IsTrue(DemoBeltPlacementService.ValidateShaftEndpoint(world, catalog, shaftPosition));
            Assert.IsFalse(DemoBeltPlacementService.ValidateShaftEndpoint(world, catalog, new BlockPos(2, DemoVerticalSliceBootstrap.SurfaceY, 2)));
        }

        [Test]
        public void EvaluateConnectionRejectsMismatchedShaftAxis()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);
            BlockPos first = new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 2);
            BlockPos second = new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 5);
            world.SetBlockState(first, catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));
            world.SetBlockState(second, catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.Z));

            DemoBeltConnectionEvaluation evaluation = DemoBeltPlacementService.EvaluateConnection(world, catalog, first, second);
            Assert.IsFalse(evaluation.CanConnect);
            Assert.AreEqual(DemoBeltConnectionFailureReason.AxisMismatch, evaluation.FailureReason);
        }

        [Test]
        public void TryCreateConnectionBuildsHorizontalBeltChainWithExpectedParts()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);
            BlockPos first = new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 2);
            BlockPos second = new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 5);
            world.SetBlockState(first, catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));
            world.SetBlockState(second, catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));

            Assert.IsTrue(DemoBeltPlacementService.TryCreateConnection(world, catalog, first, second, out DemoBeltConnectionEvaluation evaluation));
            Assert.IsTrue(evaluation.CanConnect);
            Assert.AreEqual(DemoBeltSlope.Horizontal, evaluation.Slope);
            Assert.AreEqual(Direction.South, evaluation.Facing);
            CollectionAssert.AreEqual(
                new[]
                {
                    first,
                    new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 3),
                    new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 4),
                    second
                },
                evaluation.Chain);

            AssertBeltState(world, catalog, first, DemoBeltPart.Start, DemoBeltSlope.Horizontal, Direction.South);
            AssertBeltState(world, catalog, new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 3), DemoBeltPart.Middle, DemoBeltSlope.Horizontal, Direction.South);
            AssertBeltState(world, catalog, new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 4), DemoBeltPart.Middle, DemoBeltSlope.Horizontal, Direction.South);
            AssertBeltState(world, catalog, second, DemoBeltPart.End, DemoBeltSlope.Horizontal, Direction.South);
        }

        [Test]
        public void TryCreateConnectionMarksAlignedMiddleShaftAsPulley()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);
            BlockPos first = new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 2);
            BlockPos middle = new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 3);
            BlockPos second = new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 4);
            world.SetBlockState(first, catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));
            world.SetBlockState(middle, catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));
            world.SetBlockState(second, catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));

            Assert.IsTrue(DemoBeltPlacementService.TryCreateConnection(world, catalog, first, second, out _));
            AssertBeltState(world, catalog, middle, DemoBeltPart.Pulley, DemoBeltSlope.Horizontal, Direction.South);
        }

        [Test]
        public void TryCreateConnectionUsesSidewaysSlopeAfterVerticalPulley()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);
            BlockPos first = new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 2);
            BlockPos middle = new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 3);
            BlockPos second = new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 4);
            world.SetBlockState(first, catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.Y));
            world.SetBlockState(middle, catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.Y));
            world.SetBlockState(second, catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.Y));

            Assert.IsTrue(DemoBeltPlacementService.TryCreateConnection(world, catalog, first, second, out _));
            AssertBeltState(world, catalog, first, DemoBeltPart.Start, DemoBeltSlope.Sideways, Direction.South);
            AssertBeltState(world, catalog, middle, DemoBeltPart.Pulley, DemoBeltSlope.Sideways, Direction.South);
            AssertBeltState(world, catalog, second, DemoBeltPart.End, DemoBeltSlope.Sideways, Direction.South);
        }

        [Test]
        public void TryCreateConnectionRejectsOpposingEndpointSpeedSigns()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);
            BlockPos first = new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 2);
            BlockPos second = new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 6);
            world.SetBlockState(first, catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));
            world.SetBlockState(second, catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));
            world.SetBlockState(
                first.Relative(Direction.West),
                catalog.CreativeMotor.DefaultState.With(DemoContentCatalog.FacingProperty, Direction.East));
            world.SetBlockState(
                second.Relative(Direction.East),
                catalog.CreativeMotor.DefaultState.With(DemoContentCatalog.FacingProperty, Direction.West));

            Assert.IsFalse(DemoBeltPlacementService.TryCreateConnection(world, catalog, first, second, out DemoBeltConnectionEvaluation evaluation));
            Assert.AreEqual(DemoBeltConnectionFailureReason.ConflictingEndpointSpeedSign, evaluation.FailureReason);
            Assert.AreSame(catalog.Shaft, world.GetBlockState(first).Definition);
            Assert.AreSame(catalog.Shaft, world.GetBlockState(second).Definition);
        }

        private static void AssertBeltState(
            BlockWorld world,
            DemoContentCatalog catalog,
            BlockPos position,
            DemoBeltPart expectedPart,
            DemoBeltSlope expectedSlope,
            Direction expectedFacing)
        {
            BlockState state = world.GetBlockState(position);
            Assert.AreSame(catalog.Belt, state.Definition);
            Assert.AreEqual(expectedPart, state.Get(DemoContentCatalog.BeltPartProperty), position.ToString());
            Assert.AreEqual(expectedSlope, state.Get(DemoContentCatalog.BeltSlopeProperty), position.ToString());
            Assert.AreEqual(expectedFacing, state.Get(DemoContentCatalog.BeltFacingProperty), position.ToString());
            Assert.IsFalse(state.Get(DemoContentCatalog.BeltCasingProperty), position.ToString());
            Assert.IsFalse(state.Get(DemoContentCatalog.BeltWaterloggedProperty), position.ToString());
        }
    }
}
