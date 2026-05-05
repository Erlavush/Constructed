using System;
using System.Collections.Generic;
using Constructed.Core;
using Constructed.Minecraft;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class BlockWorldBlockEntityTests
    {
        private static readonly BlockEntityBehaviorType<RecordingBehavior> PrimaryBehaviorType =
            new BlockEntityBehaviorType<RecordingBehavior>(ResourceLocation.Parse("constructed:primary_behavior"));

        private static readonly BlockEntityBehaviorType<RecordingBehavior> SecondaryBehaviorType =
            new BlockEntityBehaviorType<RecordingBehavior>(ResourceLocation.Parse("constructed:secondary_behavior"));

        [Test]
        public void PlacingBlockWithEntityTypeCreatesRuntimeObjectAtPosition()
        {
            RecordingBlockEntity created = null;
            BlockEntityType type = RecordingType(entity => created = entity);
            BlockState state = State("constructed:entity_block", type);
            BlockWorld world = new BlockWorld(AirState());
            BlockPos position = new BlockPos(4, 5, 6);

            world.SetBlockState(position, state);

            RecordingBlockEntity blockEntity = world.GetBlockEntity<RecordingBlockEntity>(position);
            Assert.AreSame(created, blockEntity);
            Assert.AreSame(type, blockEntity.Type);
            Assert.AreSame(world, blockEntity.World);
            Assert.AreEqual(position, blockEntity.Position);
            Assert.AreSame(state, blockEntity.State);
            Assert.IsFalse(blockEntity.IsInitialized);
            Assert.AreEqual(1, world.StoredBlockEntityCount);
            Assert.IsTrue(world.HasBlockEntity(position));
        }

        [Test]
        public void WorldTicksBlockEntitiesInStablePositionOrder()
        {
            List<string> calls = new List<string>();
            BlockEntityType type = RecordingType(null, calls);
            BlockState state = State("constructed:entity_block", type);
            BlockWorld world = new BlockWorld(AirState());

            world.SetBlockState(new BlockPos(2, 0, 0), state);
            world.SetBlockState(new BlockPos(-1, 0, 0), state);
            world.SetBlockState(new BlockPos(1, 0, 0), state);

            world.Tick();

            Assert.AreEqual(1, world.CurrentTick);
            CollectionAssert.AreEqual(new[]
            {
                "-1,0,0:initialize",
                "-1,0,0:lazy",
                "-1,0,0:tick",
                "1,0,0:initialize",
                "1,0,0:lazy",
                "1,0,0:tick",
                "2,0,0:initialize",
                "2,0,0:lazy",
                "2,0,0:tick"
            }, calls);
        }

        [Test]
        public void LazyTickRunsOnInitializeAndThenByConfiguredRate()
        {
            RecordingBlockEntity created = null;
            BlockEntityType type = RecordingType(entity =>
            {
                entity.SetLazyTickRate(2);
                created = entity;
            });
            BlockState state = State("constructed:entity_block", type);
            BlockWorld world = new BlockWorld(AirState());
            world.SetBlockState(BlockPos.Zero, state);

            world.Tick(3);

            Assert.AreEqual(1, created.InitializeCount);
            Assert.AreEqual(3, created.TickCount);
            Assert.AreEqual(2, created.LazyTickCount);
        }

        [Test]
        public void ChangingStateWithSameBlockEntityTypeKeepsRuntimeObjectAndNotifiesBehaviors()
        {
            StateProperty<bool> powered = StateProperty<bool>.Bool("powered");
            RecordingBlockEntity created = null;
            BlockEntityType type = RecordingType(entity =>
            {
                entity.AddRecordingBehavior(PrimaryBehaviorType);
                created = entity;
            });
            BlockDefinition definition = new BlockDefinition(
                ResourceLocation.Parse("constructed:entity_block"),
                new IStateProperty[] { powered },
                BlockLifecycle.None,
                type);
            BlockState off = definition.DefaultState;
            BlockState on = off.With(powered, true);
            BlockWorld world = new BlockWorld(AirState());
            world.SetBlockState(BlockPos.Zero, off);
            RecordingBehavior behavior = created.GetBehavior(PrimaryBehaviorType);

            world.SetBlockState(BlockPos.Zero, on);

            Assert.AreSame(created, world.GetBlockEntity(BlockPos.Zero));
            Assert.AreSame(on, created.State);
            Assert.AreEqual(0, created.DestroyCount);
            Assert.AreEqual(0, created.UnloadCount);
            Assert.AreEqual(1, created.StateChanges.Count);
            Assert.AreSame(off, created.StateChanges[0].PreviousState);
            Assert.AreSame(on, created.StateChanges[0].NewState);
            Assert.AreEqual(1, behavior.StateChanges.Count);
            Assert.AreSame(off, behavior.StateChanges[0].PreviousState);
            Assert.AreSame(on, behavior.StateChanges[0].NewState);
        }

        [Test]
        public void RemovingBlockDestroysAndUnloadsRuntimeObject()
        {
            RecordingBlockEntity created = null;
            BlockEntityType type = RecordingType(entity => created = entity);
            BlockState state = State("constructed:entity_block", type);
            BlockWorld world = new BlockWorld(AirState());
            world.SetBlockState(BlockPos.Zero, state);

            world.RemoveBlock(BlockPos.Zero);

            Assert.IsFalse(world.HasBlockEntity(BlockPos.Zero));
            Assert.AreEqual(0, world.StoredBlockEntityCount);
            Assert.IsTrue(created.IsDestroyed);
            Assert.IsTrue(created.IsUnloaded);
            Assert.AreEqual(1, created.DestroyCount);
            Assert.AreEqual(1, created.UnloadCount);
        }

        [Test]
        public void BehaviorLookupAndLateAttachmentUseTypedBehaviorKeys()
        {
            RecordingBlockEntity created = null;
            BlockEntityType type = RecordingType(entity =>
            {
                entity.AddRecordingBehavior(PrimaryBehaviorType);
                created = entity;
            });
            BlockState state = State("constructed:entity_block", type);
            BlockWorld world = new BlockWorld(AirState());
            world.SetBlockState(BlockPos.Zero, state);
            RecordingBehavior primary = created.GetBehavior(PrimaryBehaviorType);

            int ticked = world.RunBlockEntityTicks();
            RecordingBehavior secondary = created.AttachRecordingBehaviorLate(SecondaryBehaviorType);

            Assert.AreEqual(1, ticked);
            Assert.AreSame(primary, created.GetBehavior(PrimaryBehaviorType));
            Assert.AreSame(secondary, created.GetBehavior(SecondaryBehaviorType));
            Assert.AreEqual(1, primary.InitializeCount);
            Assert.AreEqual(1, primary.TickCount);
            Assert.AreEqual(1, secondary.InitializeCount);
            Assert.AreEqual(0, secondary.TickCount);
        }

        [Test]
        public void NeighborUpdatesReachAdjacentBlockEntitiesAndBehaviors()
        {
            RecordingBlockEntity neighborEntity = null;
            BlockEntityType type = RecordingType(entity =>
            {
                entity.AddRecordingBehavior(PrimaryBehaviorType);
                neighborEntity = entity;
            });
            BlockState neighbor = State("constructed:neighbor_entity_block", type);
            BlockState changed = State("constructed:changed");
            BlockWorld world = new BlockWorld(AirState());
            BlockPos changedPosition = BlockPos.Zero;
            BlockPos neighborPosition = changedPosition.Relative(Direction.East);
            world.SetBlockState(neighborPosition, neighbor);
            RecordingBehavior behavior = neighborEntity.GetBehavior(PrimaryBehaviorType);

            world.SetBlockState(changedPosition, changed);

            Assert.AreEqual(1, neighborEntity.NeighborPositions.Count);
            Assert.AreEqual(changedPosition, neighborEntity.NeighborPositions[0]);
            Assert.AreEqual(1, behavior.NeighborPositions.Count);
            Assert.AreEqual(changedPosition, behavior.NeighborPositions[0]);
        }

        [Test]
        public void InvalidBlockEntityInputsAreRejected()
        {
            BlockEntityType type = RecordingType();
            BlockState state = State("constructed:entity_block", type);
            BlockWorld world = new BlockWorld(AirState());

            Assert.Throws<ArgumentNullException>(() => new BlockEntityType(ResourceLocation.Parse("constructed:type"), null));
            Assert.Throws<ArgumentException>(() => new BlockEntityType(default(ResourceLocation), (entityType, blockWorld, position, blockState) => null));
            Assert.Throws<ArgumentNullException>(() => type.Create(null, BlockPos.Zero, state));
            Assert.Throws<ArgumentNullException>(() => type.Create(world, BlockPos.Zero, null));
            Assert.Throws<ArgumentException>(() => type.Create(world, BlockPos.Zero, State("constructed:no_entity")));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                world.SetBlockState(BlockPos.Zero, state);
                world.GetBlockEntity<RecordingBlockEntity>(BlockPos.Zero).SetLazyTickRate(0);
            });
        }

        private static BlockState AirState()
        {
            return State("minecraft:air");
        }

        private static BlockState State(string id)
        {
            return new BlockDefinition(ResourceLocation.Parse(id)).DefaultState;
        }

        private static BlockState State(string id, BlockEntityType blockEntityType)
        {
            return new BlockDefinition(ResourceLocation.Parse(id), blockEntityType).DefaultState;
        }

        private static BlockEntityType RecordingType(Action<RecordingBlockEntity> configure = null, List<string> calls = null)
        {
            return new BlockEntityType(
                ResourceLocation.Parse("constructed:recording_entity"),
                (type, world, position, state) =>
                {
                    RecordingBlockEntity entity = new RecordingBlockEntity(type, world, position, state, calls);
                    if (configure != null)
                        configure(entity);
                    return entity;
                });
        }

        private readonly struct StateChangeRecord
        {
            public StateChangeRecord(BlockState previousState, BlockState newState)
            {
                PreviousState = previousState;
                NewState = newState;
            }

            public BlockState PreviousState { get; }

            public BlockState NewState { get; }
        }

        private sealed class RecordingBlockEntity : BlockEntity
        {
            private readonly List<string> calls;

            public RecordingBlockEntity(
                BlockEntityType type,
                BlockWorld world,
                BlockPos position,
                BlockState state,
                List<string> calls)
                : base(type, world, position, state)
            {
                this.calls = calls;
                StateChanges = new List<StateChangeRecord>();
                NeighborPositions = new List<BlockPos>();
            }

            public int InitializeCount { get; private set; }

            public int TickCount { get; private set; }

            public int LazyTickCount { get; private set; }

            public int DestroyCount { get; private set; }

            public int UnloadCount { get; private set; }

            public List<StateChangeRecord> StateChanges { get; }

            public List<BlockPos> NeighborPositions { get; }

            public RecordingBehavior AddRecordingBehavior(BlockEntityBehaviorType<RecordingBehavior> behaviorType)
            {
                RecordingBehavior behavior = new RecordingBehavior(this, behaviorType);
                AddBehavior(behavior);
                return behavior;
            }

            public RecordingBehavior AttachRecordingBehaviorLate(BlockEntityBehaviorType<RecordingBehavior> behaviorType)
            {
                RecordingBehavior behavior = new RecordingBehavior(this, behaviorType);
                AttachBehaviorLate(behavior);
                return behavior;
            }

            protected override void OnInitialize()
            {
                InitializeCount++;
                Record("initialize");
            }

            protected override void OnTick()
            {
                TickCount++;
                Record("tick");
            }

            protected override void OnLazyTick()
            {
                LazyTickCount++;
                Record("lazy");
            }

            protected override void OnBlockStateChanged(BlockState previousState, BlockState newState)
            {
                StateChanges.Add(new StateChangeRecord(previousState, newState));
            }

            protected override void OnNeighborChanged(BlockPos neighborPosition)
            {
                NeighborPositions.Add(neighborPosition);
            }

            protected override void OnDestroyed()
            {
                DestroyCount++;
            }

            protected override void OnUnloaded()
            {
                UnloadCount++;
            }

            private void Record(string call)
            {
                if (calls == null)
                    return;

                calls.Add($"{Position.X},{Position.Y},{Position.Z}:{call}");
            }
        }

        private sealed class RecordingBehavior : BlockEntityBehavior
        {
            private readonly BlockEntityBehaviorType<RecordingBehavior> behaviorType;

            public RecordingBehavior(BlockEntity blockEntity, BlockEntityBehaviorType<RecordingBehavior> behaviorType)
                : base(blockEntity)
            {
                this.behaviorType = behaviorType;
                StateChanges = new List<StateChangeRecord>();
                NeighborPositions = new List<BlockPos>();
            }

            public override BlockEntityBehaviorType BehaviorType
            {
                get { return behaviorType; }
            }

            public int InitializeCount { get; private set; }

            public int TickCount { get; private set; }

            public List<StateChangeRecord> StateChanges { get; }

            public List<BlockPos> NeighborPositions { get; }

            protected override void OnInitialize()
            {
                InitializeCount++;
            }

            protected override void OnTick()
            {
                TickCount++;
            }

            protected override void OnBlockStateChanged(BlockState previousState, BlockState newState)
            {
                StateChanges.Add(new StateChangeRecord(previousState, newState));
            }

            protected override void OnNeighborChanged(BlockPos neighborPosition)
            {
                NeighborPositions.Add(neighborPosition);
            }
        }
    }
}
