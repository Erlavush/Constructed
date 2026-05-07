using Constructed.Core;
using Constructed.Create;
using Constructed.Minecraft;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class DemoBeltRuntimeResolverTests
    {
        [Test]
        public void ResolveBuildsIndexedChainAndUsesAdjacentKineticSpeed()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);
            BlockPos first = new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 2);
            BlockPos second = new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 4);
            BlockPos motor = first.Relative(Direction.West);

            world.SetBlockState(first, catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));
            world.SetBlockState(second, catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));
            world.SetBlockState(motor, catalog.CreativeMotor.DefaultState.With(DemoContentCatalog.FacingProperty, Direction.East));

            Assert.IsTrue(DemoBeltPlacementService.TryCreateConnection(world, catalog, first, second, out _));

            DemoKineticSnapshot kinetic = DemoKineticResolver.Resolve(world, catalog);
            DemoBeltRuntimeSnapshot belts = DemoBeltRuntimeResolver.Resolve(world, catalog, kinetic);

            Assert.AreEqual(3, belts.Count);
            AssertRuntimeState(belts, first, first, 0, 3, Direction.South, DemoBeltPart.Start, DemoBeltSlope.Horizontal, Axis.X, 16f);
            AssertRuntimeState(belts, new BlockPos(2, DemoVerticalSliceBootstrap.MachineY, 3), first, 1, 3, Direction.South, DemoBeltPart.Middle, DemoBeltSlope.Horizontal, Axis.X, 16f);
            AssertRuntimeState(belts, second, first, 2, 3, Direction.South, DemoBeltPart.End, DemoBeltSlope.Horizontal, Axis.X, 16f);
        }

        [Test]
        public void NextSegmentPositionMatchesCreateHorizontalAndVerticalRules()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockPos origin = new BlockPos(0, 5, 0);

            BlockState horizontalStart = catalog.Belt.DefaultState
                .With(DemoContentCatalog.BeltFacingProperty, Direction.East)
                .With(DemoContentCatalog.BeltSlopeProperty, DemoBeltSlope.Horizontal)
                .With(DemoContentCatalog.BeltPartProperty, DemoBeltPart.Start);
            BlockState verticalStart = catalog.Belt.DefaultState
                .With(DemoContentCatalog.BeltFacingProperty, Direction.South)
                .With(DemoContentCatalog.BeltSlopeProperty, DemoBeltSlope.Vertical)
                .With(DemoContentCatalog.BeltPartProperty, DemoBeltPart.Start);
            BlockState verticalEnd = catalog.Belt.DefaultState
                .With(DemoContentCatalog.BeltFacingProperty, Direction.North)
                .With(DemoContentCatalog.BeltSlopeProperty, DemoBeltSlope.Vertical)
                .With(DemoContentCatalog.BeltPartProperty, DemoBeltPart.End);

            Assert.AreEqual(new BlockPos(1, 5, 0), DemoBeltRuntimeResolver.NextSegmentPosition(horizontalStart, catalog, origin, true));
            Assert.IsNull(DemoBeltRuntimeResolver.NextSegmentPosition(horizontalStart, catalog, origin, false));

            Assert.AreEqual(new BlockPos(0, 6, 0), DemoBeltRuntimeResolver.NextSegmentPosition(verticalStart, catalog, origin, true));
            Assert.IsNull(DemoBeltRuntimeResolver.NextSegmentPosition(verticalEnd, catalog, origin, true));
            Assert.AreEqual(new BlockPos(0, 6, 0), DemoBeltRuntimeResolver.NextSegmentPosition(verticalEnd, catalog, origin, false));
        }

        private static void AssertRuntimeState(
            DemoBeltRuntimeSnapshot snapshot,
            BlockPos position,
            BlockPos expectedController,
            int expectedIndex,
            int expectedLength,
            Direction expectedFacing,
            DemoBeltPart expectedPart,
            DemoBeltSlope expectedSlope,
            Axis expectedAxis,
            float expectedSpeed)
        {
            Assert.IsTrue(snapshot.TryGet(position, out DemoBeltSegmentRuntimeState state), position.ToString());
            Assert.AreEqual(expectedController, state.ControllerPosition, position.ToString());
            Assert.AreEqual(expectedIndex, state.Index, position.ToString());
            Assert.AreEqual(expectedLength, state.Length, position.ToString());
            Assert.AreEqual(expectedFacing, state.Facing, position.ToString());
            Assert.AreEqual(expectedPart, state.Part, position.ToString());
            Assert.AreEqual(expectedSlope, state.Slope, position.ToString());
            Assert.AreEqual(expectedAxis, state.RotationAxis, position.ToString());
            Assert.AreEqual(expectedSpeed, state.Speed, 0.001f, position.ToString());
        }
    }
}
