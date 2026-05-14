using Constructed.Core;
using Constructed.Create;
using Constructed.Minecraft;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class DemoKineticResolverTests
    {
        [Test]
        public void ResolverPropagatesCreativeMotorSpeedThroughAlignedShaftRun()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateVerticalSliceWorld(catalog);

            DemoKineticSnapshot snapshot = DemoKineticResolver.Resolve(world, catalog);

            Assert.AreEqual(3, snapshot.Count);
            AssertKineticState(snapshot, DemoVerticalSliceBootstrap.CreativeMotorPosition, Axis.X, 16f);
            AssertKineticState(snapshot, DemoVerticalSliceBootstrap.FirstShaftPosition, Axis.X, 16f);
            AssertKineticState(snapshot, DemoVerticalSliceBootstrap.SecondShaftPosition, Axis.X, 16f);
            Assert.AreEqual(96f, DemoKineticResolver.ConvertToDegreesPerSecond(16f), 0.001f);
            Assert.AreEqual(0f, DemoKineticResolver.GetRotationOffsetDegrees(DemoVerticalSliceBootstrap.FirstShaftPosition, Axis.X), 0.001f);
        }

        [Test]
        public void ResolverUsesFacingDirectionForMotorSpeedSign()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);
            BlockPos motorPosition = new BlockPos(5, DemoVerticalSliceBootstrap.MachineY, 4);
            BlockPos shaftPosition = motorPosition.Relative(Direction.West);

            world.SetBlockState(
                motorPosition,
                catalog.CreativeMotor.DefaultState.With(DemoContentCatalog.FacingProperty, Direction.West));
            world.SetBlockState(
                shaftPosition,
                catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));

            DemoKineticSnapshot snapshot = DemoKineticResolver.Resolve(world, catalog);

            AssertKineticState(snapshot, motorPosition, Axis.X, -16f);
            AssertKineticState(snapshot, shaftPosition, Axis.X, -16f);
            Assert.AreEqual(-96f, DemoKineticResolver.ConvertToDegreesPerSecond(-16f), 0.001f);
        }

        [Test]
        public void ResolverStopsAtMisalignedShafts()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);
            world.SetBlockState(
                DemoVerticalSliceBootstrap.CreativeMotorPosition,
                catalog.CreativeMotor.DefaultState.With(DemoContentCatalog.FacingProperty, Direction.East));
            world.SetBlockState(
                DemoVerticalSliceBootstrap.FirstShaftPosition,
                catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.Z));
            world.SetBlockState(
                DemoVerticalSliceBootstrap.SecondShaftPosition,
                catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));

            DemoKineticSnapshot snapshot = DemoKineticResolver.Resolve(world, catalog);

            Assert.AreEqual(1, snapshot.Count);
            AssertKineticState(snapshot, DemoVerticalSliceBootstrap.CreativeMotorPosition, Axis.X, 16f);
            Assert.IsFalse(snapshot.TryGet(DemoVerticalSliceBootstrap.FirstShaftPosition, out _));
            Assert.IsFalse(snapshot.TryGet(DemoVerticalSliceBootstrap.SecondShaftPosition, out _));
        }

        [Test]
        public void ResolverReversesSpeedAcrossSideMeshedSmallCogwheels()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);
            BlockPos motorPos = new BlockPos(0, DemoVerticalSliceBootstrap.MachineY, 0);
            BlockPos drivingCogPos = motorPos.Relative(Direction.East);
            BlockPos drivenCogPos = drivingCogPos.Relative(Direction.South);

            world.SetBlockState(
                motorPos,
                catalog.CreativeMotor.DefaultState.With(DemoContentCatalog.FacingProperty, Direction.East));
            world.SetBlockState(
                drivingCogPos,
                catalog.Cogwheel.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));
            world.SetBlockState(
                drivenCogPos,
                catalog.Cogwheel.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));

            DemoKineticSnapshot snapshot = DemoKineticResolver.Resolve(world, catalog);

            Assert.AreEqual(3, snapshot.Count);
            AssertKineticState(snapshot, motorPos, Axis.X, 16f);
            AssertKineticState(snapshot, drivingCogPos, Axis.X, 16f);
            AssertKineticState(snapshot, drivenCogPos, Axis.X, -16f);
        }

        [Test]
        public void ResolverAppliesLargeToSmallCogwheelRatio()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);
            BlockPos motorPos = new BlockPos(0, DemoVerticalSliceBootstrap.MachineY, 0);
            BlockPos largeCogPos = motorPos.Relative(Direction.East);
            BlockPos smallCogPos = largeCogPos.Relative(Direction.Up).Relative(Direction.South);

            world.SetBlockState(
                motorPos,
                catalog.CreativeMotor.DefaultState.With(DemoContentCatalog.FacingProperty, Direction.East));
            world.SetBlockState(
                largeCogPos,
                catalog.LargeCogwheel.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));
            world.SetBlockState(
                smallCogPos,
                catalog.Cogwheel.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));

            DemoKineticSnapshot snapshot = DemoKineticResolver.Resolve(world, catalog);

            Assert.AreEqual(3, snapshot.Count);
            AssertKineticState(snapshot, motorPos, Axis.X, 16f);
            AssertKineticState(snapshot, largeCogPos, Axis.X, 16f);
            AssertKineticState(snapshot, smallCogPos, Axis.X, -32f);
        }

        [Test]
        public void ResolverRedirectsGearboxSpeedUsingSourceFacingAxisModifier()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);
            BlockPos motorPos = new BlockPos(0, DemoVerticalSliceBootstrap.MachineY, 0);
            BlockPos gearboxPos = motorPos.Relative(Direction.East);
            BlockPos outputShaftPos = gearboxPos.Relative(Direction.North);

            world.SetBlockState(
                motorPos,
                catalog.CreativeMotor.DefaultState.With(DemoContentCatalog.FacingProperty, Direction.East));
            world.SetBlockState(
                gearboxPos,
                catalog.Gearbox.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.Y));
            world.SetBlockState(
                outputShaftPos,
                catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.Z));

            DemoKineticSnapshot snapshot = DemoKineticResolver.Resolve(world, catalog);

            Assert.AreEqual(3, snapshot.Count);
            AssertKineticState(snapshot, motorPos, Axis.X, 16f);
            AssertKineticState(snapshot, gearboxPos, Axis.Y, 16f);
            AssertKineticState(snapshot, outputShaftPos, Axis.Z, -16f);
            Assert.IsTrue(snapshot.TryGet(gearboxPos, out DemoKineticComponentState gearboxState));
            Assert.AreEqual(Direction.West, gearboxState.SourceFacing.Value);
            Assert.AreEqual(-16f, DemoKineticResolver.GetGearboxFaceSpeed(gearboxState, Direction.North), 0.001f);
            Assert.AreEqual(16f, DemoKineticResolver.GetGearboxFaceSpeed(gearboxState, Direction.South), 0.001f);
        }

        [Test]
        public void ResolverPropagatesThroughBeltsToAttachedShafts()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateFlatSurfaceWorld(catalog);
            
            // Layout: Motor(E) -> Shaft(X) -> Belt(Start, N) -> Belt(End, N) -> Shaft(X)
            // Motor at (0, 5, 0)
            BlockPos motorPos = new BlockPos(0, 5, 0);
            BlockPos shaft1Pos = motorPos.Relative(Direction.East);
            BlockPos belt1Pos = shaft1Pos.Relative(Direction.East);
            BlockPos belt2Pos = belt1Pos.Relative(Direction.North);
            BlockPos shaft2Pos = belt2Pos.Relative(Direction.East);
            
            world.SetBlockState(motorPos, catalog.CreativeMotor.DefaultState.With(DemoContentCatalog.FacingProperty, Direction.East));
            world.SetBlockState(shaft1Pos, catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));
            
            // Belt rotation axis for North-facing horizontal belt is X
            world.SetBlockState(belt1Pos, catalog.Belt.DefaultState
                .With(DemoContentCatalog.BeltFacingProperty, Direction.North)
                .With(DemoContentCatalog.BeltSlopeProperty, DemoBeltSlope.Horizontal)
                .With(DemoContentCatalog.BeltPartProperty, DemoBeltPart.Start));
            world.SetBlockState(belt2Pos, catalog.Belt.DefaultState
                .With(DemoContentCatalog.BeltFacingProperty, Direction.North)
                .With(DemoContentCatalog.BeltSlopeProperty, DemoBeltSlope.Horizontal)
                .With(DemoContentCatalog.BeltPartProperty, DemoBeltPart.End));
                
            world.SetBlockState(shaft2Pos, catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.X));

            DemoKineticSnapshot snapshot = DemoKineticResolver.Resolve(world, catalog);

            AssertKineticState(snapshot, motorPos, Axis.X, 16f);
            AssertKineticState(snapshot, shaft1Pos, Axis.X, 16f);
            AssertKineticState(snapshot, belt1Pos, Axis.X, 16f);
            AssertKineticState(snapshot, belt2Pos, Axis.X, 16f);
            AssertKineticState(snapshot, shaft2Pos, Axis.X, 16f);
        }

        private static void AssertKineticState(DemoKineticSnapshot snapshot, BlockPos position, Axis expectedAxis, float expectedSpeed)
        {
            Assert.IsTrue(snapshot.TryGet(position, out DemoKineticComponentState state), position.ToString());
            Assert.AreEqual(expectedAxis, state.Axis, position.ToString());
            Assert.AreEqual(expectedSpeed, state.Speed, 0.001f, position.ToString());
        }
    }
}
