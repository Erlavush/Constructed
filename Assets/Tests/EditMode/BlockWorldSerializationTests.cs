using System;
using System.Collections.Generic;
using Constructed.Core;
using Constructed.Minecraft;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class BlockWorldSerializationTests
    {
        private static readonly StateProperty<bool> PoweredProperty = StateProperty<bool>.Bool("powered");

        private static readonly BlockEntityBehaviorType<PersistedBehavior> PersistedBehaviorType =
            new BlockEntityBehaviorType<PersistedBehavior>(ResourceLocation.Parse("constructed:persisted_behavior"));

        [Test]
        public void SerializeCapturesCurrentTickBlocksAndBlockEntityDataInStableOrder()
        {
            TestCatalog catalog = TestCatalog.Create();
            BlockWorld world = new BlockWorld(catalog.Air.DefaultState, 4);
            BlockState poweredMachine = catalog.Machine.DefaultState.With(PoweredProperty, true);
            BlockPos machinePosition = new BlockPos(3, 1, 0);
            world.SetBlockState(new BlockPos(2, 0, 0), catalog.Stone.DefaultState);
            world.SetBlockState(machinePosition, poweredMachine);
            world.SetBlockState(new BlockPos(-1, 0, 0), catalog.Dirt.DefaultState);
            PersistedBlockEntity machine = world.GetBlockEntity<PersistedBlockEntity>(machinePosition);
            machine.Energy = 42;
            machine.Behavior.Mode = "running";

            world.Tick(2);
            SerializedBlockWorld snapshot = world.Serialize();

            Assert.AreEqual(6, snapshot.CurrentTick);
            Assert.AreEqual(3, snapshot.Blocks.Count);
            Assert.AreEqual(new BlockPos(-1, 0, 0), snapshot.Blocks[0].Position);
            Assert.AreEqual(catalog.Dirt.Id, snapshot.Blocks[0].State.BlockId);
            Assert.AreEqual(new BlockPos(2, 0, 0), snapshot.Blocks[1].Position);
            Assert.AreEqual(catalog.Stone.Id, snapshot.Blocks[1].State.BlockId);
            Assert.AreEqual(machinePosition, snapshot.Blocks[2].Position);
            Assert.AreEqual(catalog.Machine.Id, snapshot.Blocks[2].State.BlockId);
            Assert.AreEqual("true", snapshot.Blocks[2].State.GetProperty("powered"));
            Assert.AreEqual(1, snapshot.BlockEntities.Count);
            Assert.AreEqual(machinePosition, snapshot.BlockEntities[0].Position);
            Assert.AreEqual(catalog.MachineType.Id, snapshot.BlockEntities[0].TypeId);
            Assert.AreEqual(42, snapshot.BlockEntities[0].Data.GetInt32("energy"));
            Assert.AreEqual("running", snapshot.BlockEntities[0].Data.GetString("behavior.mode"));
        }

        [Test]
        public void DeserializeRestoresStatesBlockEntitiesAndPayloadsWithoutTicking()
        {
            TestCatalog catalog = TestCatalog.Create();
            BlockWorld source = new BlockWorld(catalog.Air.DefaultState, 10);
            BlockPos machinePosition = new BlockPos(1, 2, 3);
            BlockState poweredMachine = catalog.Machine.DefaultState.With(PoweredProperty, true);
            source.SetBlockState(machinePosition, poweredMachine);
            source.SetBlockState(new BlockPos(-2, 0, 0), catalog.Stone.DefaultState);
            PersistedBlockEntity sourceMachine = source.GetBlockEntity<PersistedBlockEntity>(machinePosition);
            sourceMachine.Energy = 77;
            sourceMachine.Behavior.Mode = "loaded";
            source.Tick();
            SerializedBlockWorld snapshot = source.Serialize();

            BlockWorld restored = BlockWorld.Deserialize(catalog.Air.DefaultState, snapshot, catalog.Blocks);
            PersistedBlockEntity restoredMachine = restored.GetBlockEntity<PersistedBlockEntity>(machinePosition);

            Assert.AreEqual(source.CurrentTick, restored.CurrentTick);
            Assert.AreEqual(source.StoredBlockCount, restored.StoredBlockCount);
            Assert.AreEqual(source.StoredBlockEntityCount, restored.StoredBlockEntityCount);
            Assert.AreEqual(true, restored.GetBlockState(machinePosition).Get(PoweredProperty));
            Assert.NotNull(restoredMachine);
            Assert.AreEqual(77, restoredMachine.Energy);
            Assert.AreEqual("loaded", restoredMachine.Behavior.Mode);
            Assert.AreEqual(1, restoredMachine.ReadCount);
            Assert.AreEqual(1, restoredMachine.Behavior.ReadCount);
            Assert.IsFalse(restoredMachine.IsInitialized);
            Assert.AreEqual(0, restoredMachine.TickCount);
            Assert.AreEqual(snapshot, restored.Serialize());
        }

        [Test]
        public void LoadingSnapshotClearsExistingStateAndUnloadsPreviousBlockEntities()
        {
            TestCatalog catalog = TestCatalog.Create();
            BlockWorld world = new BlockWorld(catalog.Air.DefaultState);
            BlockPos oldPosition = new BlockPos(0, 0, 0);
            world.SetBlockState(oldPosition, catalog.Machine.DefaultState);
            PersistedBlockEntity oldEntity = world.GetBlockEntity<PersistedBlockEntity>(oldPosition);
            SerializedBlockWorld snapshot = new SerializedBlockWorld(
                12,
                new[]
                {
                    new SerializedWorldBlockEntry(new BlockPos(5, 0, 0), catalog.Stone.DefaultState.Serialize())
                },
                null);

            world.LoadSnapshot(snapshot, catalog.Blocks);

            Assert.AreEqual(12, world.CurrentTick);
            Assert.AreEqual(1, world.StoredBlockCount);
            Assert.AreEqual(0, world.StoredBlockEntityCount);
            Assert.IsFalse(world.HasStoredBlock(oldPosition));
            Assert.AreSame(catalog.Stone.DefaultState, world.GetBlockState(new BlockPos(5, 0, 0)));
            Assert.IsTrue(oldEntity.IsUnloaded);
            Assert.IsFalse(oldEntity.IsDestroyed);
        }

        [Test]
        public void InvalidSerializedWorldInputsAreRejected()
        {
            TestCatalog catalog = TestCatalog.Create();
            SerializedWorldBlockEntry stoneEntry = new SerializedWorldBlockEntry(BlockPos.Zero, catalog.Stone.DefaultState.Serialize());
            SerializedBlockEntity machineEntity = new SerializedBlockEntity(
                BlockPos.Zero,
                catalog.MachineType.Id,
                BlockEntityData.Empty);
            BlockEntityType otherType = new BlockEntityType(
                ResourceLocation.Parse("constructed:other_entity"),
                (type, world, position, state) => new PersistedBlockEntity(type, world, position, state));

            Assert.Throws<ArgumentException>(() => new BlockEntityData(new[]
            {
                new BlockEntityDataValue("same", "a"),
                new BlockEntityDataValue("same", "b")
            }));
            Assert.Throws<ArgumentException>(() => new SerializedBlockWorld(0, new[] { stoneEntry, stoneEntry }, null));
            Assert.Throws<ArgumentException>(() => new SerializedBlockWorld(0, new[] { stoneEntry }, new[] { machineEntity, machineEntity }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SerializedBlockWorld(-1, null, null));

            SerializedBlockWorld unknownBlock = new SerializedBlockWorld(
                0,
                new[]
                {
                    new SerializedWorldBlockEntry(BlockPos.Zero, new SerializedBlockState(ResourceLocation.Parse("constructed:missing"), null))
                },
                null);
            Assert.Throws<KeyNotFoundException>(() => BlockWorld.Deserialize(catalog.Air.DefaultState, unknownBlock, catalog.Blocks));

            SerializedBlockWorld missingBlockEntityBlock = new SerializedBlockWorld(0, new[] { stoneEntry }, new[] { machineEntity });
            Assert.Throws<InvalidOperationException>(() => BlockWorld.Deserialize(catalog.Air.DefaultState, missingBlockEntityBlock, catalog.Blocks));

            SerializedBlockWorld mismatchedBlockEntityType = new SerializedBlockWorld(
                0,
                new[]
                {
                    new SerializedWorldBlockEntry(BlockPos.Zero, catalog.Machine.DefaultState.Serialize())
                },
                new[]
                {
                    new SerializedBlockEntity(BlockPos.Zero, otherType.Id, BlockEntityData.Empty)
                });
            Assert.Throws<InvalidOperationException>(() => BlockWorld.Deserialize(catalog.Air.DefaultState, mismatchedBlockEntityType, catalog.Blocks));
        }

        private sealed class TestCatalog
        {
            private TestCatalog(
                Registry<BlockDefinition> blocks,
                BlockDefinition air,
                BlockDefinition stone,
                BlockDefinition dirt,
                BlockDefinition machine,
                BlockEntityType machineType)
            {
                Blocks = blocks;
                Air = air;
                Stone = stone;
                Dirt = dirt;
                Machine = machine;
                MachineType = machineType;
            }

            public Registry<BlockDefinition> Blocks { get; }

            public BlockDefinition Air { get; }

            public BlockDefinition Stone { get; }

            public BlockDefinition Dirt { get; }

            public BlockDefinition Machine { get; }

            public BlockEntityType MachineType { get; }

            public static TestCatalog Create()
            {
                BlockEntityType machineType = new BlockEntityType(
                    ResourceLocation.Parse("constructed:persisted_entity"),
                    (type, world, position, state) => new PersistedBlockEntity(type, world, position, state));
                BlockDefinition air = new BlockDefinition(ResourceLocation.Parse("minecraft:air"));
                BlockDefinition stone = new BlockDefinition(ResourceLocation.Parse("minecraft:stone"));
                BlockDefinition dirt = new BlockDefinition(ResourceLocation.Parse("minecraft:dirt"));
                BlockDefinition machine = new BlockDefinition(
                    ResourceLocation.Parse("constructed:persisted_machine"),
                    new IStateProperty[] { PoweredProperty },
                    BlockLifecycle.None,
                    machineType);
                Registry<BlockDefinition> blocks = new Registry<BlockDefinition>(ResourceLocation.Parse("minecraft:block"));
                blocks.Register(air.Id, air);
                blocks.Register(stone.Id, stone);
                blocks.Register(dirt.Id, dirt);
                blocks.Register(machine.Id, machine);
                blocks.Freeze();
                return new TestCatalog(blocks, air, stone, dirt, machine, machineType);
            }
        }

        private sealed class PersistedBlockEntity : BlockEntity
        {
            public PersistedBlockEntity(BlockEntityType type, BlockWorld world, BlockPos position, BlockState state)
                : base(type, world, position, state)
            {
                Behavior = new PersistedBehavior(this);
                AddBehavior(Behavior);
            }

            public int Energy { get; set; }

            public int ReadCount { get; private set; }

            public int TickCount { get; private set; }

            public PersistedBehavior Behavior { get; }

            protected override void OnTick()
            {
                TickCount++;
            }

            protected override void OnWrite(BlockEntityDataBuilder data)
            {
                data.SetInt32("energy", Energy);
            }

            protected override void OnRead(BlockEntityData data)
            {
                Energy = data.GetInt32("energy");
                ReadCount++;
            }
        }

        private sealed class PersistedBehavior : BlockEntityBehavior
        {
            public PersistedBehavior(BlockEntity blockEntity)
                : base(blockEntity)
            {
                Mode = "idle";
            }

            public override BlockEntityBehaviorType BehaviorType
            {
                get { return PersistedBehaviorType; }
            }

            public string Mode { get; set; }

            public int ReadCount { get; private set; }

            protected override void OnWrite(BlockEntityDataBuilder data)
            {
                data.SetString("behavior.mode", Mode);
            }

            protected override void OnRead(BlockEntityData data)
            {
                Mode = data.GetString("behavior.mode");
                ReadCount++;
            }
        }
    }
}
