using System;
using System.Collections.Generic;
using Constructed.Core;
using Constructed.Minecraft;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class BlockWorldScheduledTickTests
    {
        [Test]
        public void ScheduledBlockTickRunsAfterDelayAndAdvancesWorldClock()
        {
            List<ScheduledBlockTick> ticks = new List<ScheduledBlockTick>();
            BlockState state = State("constructed:ticker", new RecordingLifecycle(ticks));
            BlockWorld world = new BlockWorld(AirState());
            BlockPos position = BlockPos.Zero;
            world.SetBlockState(position, state);

            bool scheduled = world.ScheduleBlockTick(position, state, 2);

            Assert.IsTrue(scheduled);
            Assert.IsTrue(world.HasScheduledBlockTick(position, state));
            Assert.AreEqual(1, world.PendingScheduledBlockTickCount);
            Assert.AreEqual(0, world.RunScheduledBlockTicks());

            world.Tick();

            Assert.AreEqual(1, world.CurrentTick);
            Assert.AreEqual(0, ticks.Count);

            world.Tick();

            Assert.AreEqual(2, world.CurrentTick);
            Assert.AreEqual(1, ticks.Count);
            Assert.AreSame(world, ticks[0].World);
            Assert.AreEqual(position, ticks[0].Position);
            Assert.AreSame(state, ticks[0].State);
            Assert.AreEqual(0, ticks[0].ScheduledAtTick);
            Assert.AreEqual(2, ticks[0].TriggerTick);
            Assert.AreEqual(ScheduledTickPriority.Normal, ticks[0].Priority);
            Assert.IsFalse(world.HasScheduledBlockTick(position, state));
            Assert.AreEqual(0, world.PendingScheduledBlockTickCount);
        }

        [Test]
        public void DueTicksRunByTriggerTickPriorityThenInsertionOrder()
        {
            List<string> calls = new List<string>();
            BlockWorld world = new BlockWorld(AirState());
            BlockState normalEarly = State("constructed:normal_early", new NamedLifecycle("normal_early", calls));
            BlockState normalLate = State("constructed:normal_late", new NamedLifecycle("normal_late", calls));
            BlockState high = State("constructed:high", new NamedLifecycle("high", calls));
            BlockState firstDue = State("constructed:first_due", new NamedLifecycle("first_due", calls));
            BlockPos normalEarlyPos = new BlockPos(0, 0, 0);
            BlockPos normalLatePos = new BlockPos(1, 0, 0);
            BlockPos highPos = new BlockPos(2, 0, 0);
            BlockPos firstDuePos = new BlockPos(3, 0, 0);
            world.SetBlockState(normalEarlyPos, normalEarly);
            world.SetBlockState(normalLatePos, normalLate);
            world.SetBlockState(highPos, high);
            world.SetBlockState(firstDuePos, firstDue);

            world.ScheduleBlockTick(normalEarlyPos, normalEarly, 2);
            world.ScheduleBlockTick(normalLatePos, normalLate, 2);
            world.ScheduleBlockTick(highPos, high, 2, ScheduledTickPriority.High);
            world.ScheduleBlockTick(firstDuePos, firstDue, 1);

            world.Tick(2);

            CollectionAssert.AreEqual(new[]
            {
                "first_due@1",
                "high@2",
                "normal_early@2",
                "normal_late@2"
            }, calls);
        }

        [Test]
        public void DuplicateScheduledBlockTicksAreIgnoredUntilThePendingTickRuns()
        {
            List<ScheduledBlockTick> ticks = new List<ScheduledBlockTick>();
            BlockState state = State("constructed:ticker", new RecordingLifecycle(ticks));
            BlockWorld world = new BlockWorld(AirState());
            BlockPos position = BlockPos.Zero;
            world.SetBlockState(position, state);

            bool first = world.ScheduleBlockTick(position, state, 2);
            bool duplicate = world.ScheduleBlockTick(position, state, 1, ScheduledTickPriority.High);

            Assert.IsTrue(first);
            Assert.IsFalse(duplicate);
            Assert.AreEqual(1, world.PendingScheduledBlockTickCount);

            world.Tick(2);

            Assert.AreEqual(1, ticks.Count);
            Assert.AreEqual(2, ticks[0].TriggerTick);
        }

        [Test]
        public void ScheduledBlockTickSkipsWhenCurrentBlockNoLongerMatchesScheduledBlock()
        {
            List<ScheduledBlockTick> oldTicks = new List<ScheduledBlockTick>();
            List<ScheduledBlockTick> newTicks = new List<ScheduledBlockTick>();
            BlockState oldState = State("constructed:old", new RecordingLifecycle(oldTicks));
            BlockState newState = State("constructed:new", new RecordingLifecycle(newTicks));
            BlockWorld world = new BlockWorld(AirState());
            BlockPos position = BlockPos.Zero;
            world.SetBlockState(position, oldState);
            world.ScheduleBlockTick(position, oldState, 1);

            world.SetBlockState(position, newState);
            int executed = world.RunScheduledBlockTicks();
            world.Tick();

            Assert.AreEqual(0, executed);
            Assert.AreEqual(0, oldTicks.Count);
            Assert.AreEqual(0, newTicks.Count);
            Assert.IsFalse(world.HasScheduledBlockTick(position, oldState));
        }

        [Test]
        public void ClearRemovesStoredBlocksAndPendingScheduledBlockTicks()
        {
            BlockState state = State("constructed:ticker", new RecordingLifecycle(new List<ScheduledBlockTick>()));
            BlockWorld world = new BlockWorld(AirState());
            BlockPos position = BlockPos.Zero;
            world.SetBlockState(position, state);
            world.ScheduleBlockTick(position, state, 1);

            world.Clear();

            Assert.AreEqual(0, world.StoredBlockCount);
            Assert.AreEqual(0, world.PendingScheduledBlockTickCount);
            Assert.IsFalse(world.HasScheduledBlockTick(position, state));
        }

        [Test]
        public void InvalidScheduledTickInputsAreRejected()
        {
            BlockState state = State("constructed:ticker", new RecordingLifecycle(new List<ScheduledBlockTick>()));
            BlockWorld world = new BlockWorld(AirState());

            Assert.Throws<ArgumentNullException>(() => world.ScheduleBlockTick(BlockPos.Zero, (BlockDefinition)null, 1));
            Assert.Throws<ArgumentNullException>(() => world.ScheduleBlockTick(BlockPos.Zero, (BlockState)null, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => world.ScheduleBlockTick(BlockPos.Zero, state, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BlockWorld(AirState(), -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => world.Tick(0));
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
            private readonly List<ScheduledBlockTick> scheduledTicks;

            public RecordingLifecycle(List<ScheduledBlockTick> scheduledTicks)
            {
                this.scheduledTicks = scheduledTicks;
            }

            public void OnBlockPlaced(BlockStateChange change)
            {
            }

            public void OnBlockRemoved(BlockStateChange change)
            {
            }

            public void OnNeighborChanged(NeighborBlockChange change)
            {
            }

            public void OnScheduledTick(ScheduledBlockTick tick)
            {
                scheduledTicks.Add(tick);
            }
        }

        private sealed class NamedLifecycle : IBlockLifecycle
        {
            private readonly string name;
            private readonly List<string> calls;

            public NamedLifecycle(string name, List<string> calls)
            {
                this.name = name;
                this.calls = calls;
            }

            public void OnBlockPlaced(BlockStateChange change)
            {
            }

            public void OnBlockRemoved(BlockStateChange change)
            {
            }

            public void OnNeighborChanged(NeighborBlockChange change)
            {
            }

            public void OnScheduledTick(ScheduledBlockTick tick)
            {
                calls.Add($"{name}@{tick.World.CurrentTick}");
            }
        }
    }
}
