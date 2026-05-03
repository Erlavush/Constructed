using System.Collections.Generic;
using Constructed.Core;
using Constructed.Minecraft;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class BlockWorldLifecycleTests
    {
        [Test]
        public void PlacingBlockDispatchesPlacedCallbackAndNeighborUpdate()
        {
            RecordingLifecycle centerLifecycle = new RecordingLifecycle();
            RecordingLifecycle neighborLifecycle = new RecordingLifecycle();
            BlockState center = State("constructed:center", centerLifecycle);
            BlockState neighbor = State("constructed:neighbor", neighborLifecycle);
            BlockWorld world = new BlockWorld(AirState());
            BlockPos centerPos = BlockPos.Zero;
            BlockPos eastPos = centerPos.Relative(Direction.East);
            world.SetBlockState(eastPos, neighbor);
            centerLifecycle.Clear();
            neighborLifecycle.Clear();

            BlockState previous = world.SetBlockState(centerPos, center);

            Assert.AreSame(world.AirState, previous);
            Assert.AreEqual(1, centerLifecycle.Placed.Count);
            Assert.AreEqual(0, centerLifecycle.Removed.Count);
            Assert.AreSame(center, centerLifecycle.Placed[0].NewState);
            Assert.AreEqual(centerPos, centerLifecycle.Placed[0].Position);
            Assert.AreEqual(1, neighborLifecycle.NeighborChanges.Count);
            Assert.AreEqual(eastPos, neighborLifecycle.NeighborChanges[0].Position);
            Assert.AreEqual(centerPos, neighborLifecycle.NeighborChanges[0].ChangedPosition);
            Assert.AreEqual(Direction.West, neighborLifecycle.NeighborChanges[0].DirectionToChanged);
            Assert.AreSame(world.AirState, neighborLifecycle.NeighborChanges[0].PreviousChangedState);
            Assert.AreSame(center, neighborLifecycle.NeighborChanges[0].NewChangedState);
        }

        [Test]
        public void RemovingBlockDispatchesRemovedCallbackAndNeighborUpdate()
        {
            RecordingLifecycle centerLifecycle = new RecordingLifecycle();
            RecordingLifecycle neighborLifecycle = new RecordingLifecycle();
            BlockState center = State("constructed:center", centerLifecycle);
            BlockState neighbor = State("constructed:neighbor", neighborLifecycle);
            BlockWorld world = new BlockWorld(AirState());
            BlockPos centerPos = BlockPos.Zero;
            BlockPos westPos = centerPos.Relative(Direction.West);
            world.SetBlockState(centerPos, center);
            world.SetBlockState(westPos, neighbor);
            centerLifecycle.Clear();
            neighborLifecycle.Clear();

            BlockState previous = world.RemoveBlock(centerPos);

            Assert.AreSame(center, previous);
            Assert.AreEqual(0, centerLifecycle.Placed.Count);
            Assert.AreEqual(1, centerLifecycle.Removed.Count);
            Assert.AreSame(center, centerLifecycle.Removed[0].PreviousState);
            Assert.AreSame(world.AirState, centerLifecycle.Removed[0].NewState);
            Assert.AreEqual(1, neighborLifecycle.NeighborChanges.Count);
            Assert.AreEqual(Direction.East, neighborLifecycle.NeighborChanges[0].DirectionToChanged);
            Assert.AreSame(center, neighborLifecycle.NeighborChanges[0].PreviousChangedState);
            Assert.AreSame(world.AirState, neighborLifecycle.NeighborChanges[0].NewChangedState);
        }

        [Test]
        public void ReplacingBlockDispatchesRemovedThenPlacedAndSingleNeighborUpdate()
        {
            RecordingLifecycle oldLifecycle = new RecordingLifecycle();
            RecordingLifecycle newLifecycle = new RecordingLifecycle();
            RecordingLifecycle neighborLifecycle = new RecordingLifecycle();
            BlockState oldState = State("constructed:old", oldLifecycle);
            BlockState newState = State("constructed:new", newLifecycle);
            BlockState neighbor = State("constructed:neighbor", neighborLifecycle);
            BlockWorld world = new BlockWorld(AirState());
            BlockPos centerPos = BlockPos.Zero;
            world.SetBlockState(centerPos, oldState);
            world.SetBlockState(centerPos.Relative(Direction.North), neighbor);
            oldLifecycle.Clear();
            newLifecycle.Clear();
            neighborLifecycle.Clear();

            BlockState previous = world.SetBlockState(centerPos, newState);

            Assert.AreSame(oldState, previous);
            Assert.AreEqual(1, oldLifecycle.Removed.Count);
            Assert.AreEqual(1, newLifecycle.Placed.Count);
            Assert.AreEqual(1, neighborLifecycle.NeighborChanges.Count);
            Assert.AreSame(oldState, neighborLifecycle.NeighborChanges[0].PreviousChangedState);
            Assert.AreSame(newState, neighborLifecycle.NeighborChanges[0].NewChangedState);
        }

        [Test]
        public void SettingSameStateDoesNotDispatchCallbacks()
        {
            RecordingLifecycle lifecycle = new RecordingLifecycle();
            RecordingLifecycle neighborLifecycle = new RecordingLifecycle();
            BlockState state = State("constructed:block", lifecycle);
            BlockState neighbor = State("constructed:neighbor", neighborLifecycle);
            BlockWorld world = new BlockWorld(AirState());
            BlockPos position = BlockPos.Zero;
            world.SetBlockState(position, state);
            world.SetBlockState(position.Relative(Direction.Up), neighbor);
            lifecycle.Clear();
            neighborLifecycle.Clear();

            BlockState previous = world.SetBlockState(position, state);

            Assert.AreSame(state, previous);
            Assert.AreEqual(0, lifecycle.Placed.Count);
            Assert.AreEqual(0, lifecycle.Removed.Count);
            Assert.AreEqual(0, neighborLifecycle.NeighborChanges.Count);
        }

        [Test]
        public void NeighborUpdatesAreOnlySentToStoredNonAirBlocks()
        {
            RecordingLifecycle centerLifecycle = new RecordingLifecycle();
            RecordingLifecycle northLifecycle = new RecordingLifecycle();
            RecordingLifecycle southLifecycle = new RecordingLifecycle();
            BlockState center = State("constructed:center", centerLifecycle);
            BlockState north = State("constructed:north", northLifecycle);
            BlockState south = State("constructed:south", southLifecycle);
            BlockWorld world = new BlockWorld(AirState());
            BlockPos centerPos = BlockPos.Zero;
            world.SetBlockState(centerPos.Relative(Direction.North), north);
            world.SetBlockState(centerPos.Relative(Direction.South), south);
            northLifecycle.Clear();
            southLifecycle.Clear();

            world.SetBlockState(centerPos, center);

            Assert.AreEqual(1, northLifecycle.NeighborChanges.Count);
            Assert.AreEqual(Direction.South, northLifecycle.NeighborChanges[0].DirectionToChanged);
            Assert.AreEqual(1, southLifecycle.NeighborChanges.Count);
            Assert.AreEqual(Direction.North, southLifecycle.NeighborChanges[0].DirectionToChanged);
        }

        private static BlockState AirState()
        {
            return State("minecraft:air", BlockLifecycle.None);
        }

        private static BlockState State(string id, IBlockLifecycle lifecycle)
        {
            return new BlockDefinition(ResourceLocation.Parse(id), new IStateProperty[0], lifecycle).DefaultState;
        }

        private sealed class RecordingLifecycle : IBlockLifecycle
        {
            public readonly List<BlockStateChange> Placed = new List<BlockStateChange>();
            public readonly List<BlockStateChange> Removed = new List<BlockStateChange>();
            public readonly List<NeighborBlockChange> NeighborChanges = new List<NeighborBlockChange>();

            public void OnBlockPlaced(BlockStateChange change)
            {
                Placed.Add(change);
            }

            public void OnBlockRemoved(BlockStateChange change)
            {
                Removed.Add(change);
            }

            public void OnNeighborChanged(NeighborBlockChange change)
            {
                NeighborChanges.Add(change);
            }

            public void Clear()
            {
                Placed.Clear();
                Removed.Clear();
                NeighborChanges.Clear();
            }
        }
    }
}
