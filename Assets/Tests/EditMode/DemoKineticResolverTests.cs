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

        private static void AssertKineticState(DemoKineticSnapshot snapshot, BlockPos position, Axis expectedAxis, float expectedSpeed)
        {
            Assert.IsTrue(snapshot.TryGet(position, out DemoKineticComponentState state), position.ToString());
            Assert.AreEqual(expectedAxis, state.Axis, position.ToString());
            Assert.AreEqual(expectedSpeed, state.Speed, 0.001f, position.ToString());
        }
    }
}
