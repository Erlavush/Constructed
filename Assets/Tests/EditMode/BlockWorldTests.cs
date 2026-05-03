using System;
using System.Collections.Generic;
using Constructed.Core;
using Constructed.Minecraft;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class BlockWorldTests
    {
        [Test]
        public void MissingPositionsReturnAirState()
        {
            BlockState air = AirState();
            BlockWorld world = new BlockWorld(air);
            BlockPos position = new BlockPos(3, 4, 5);

            Assert.AreSame(air, world.GetBlockState(position));
            Assert.IsFalse(world.HasStoredBlock(position));
            Assert.AreEqual(0, world.StoredBlockCount);
        }

        [Test]
        public void SetStoresNonAirStateAndReturnsPreviousState()
        {
            BlockState air = AirState();
            BlockState stone = BlockState("minecraft:stone");
            BlockWorld world = new BlockWorld(air);
            BlockPos position = new BlockPos(1, 2, 3);

            BlockState previous = world.SetBlockState(position, stone);

            Assert.AreSame(air, previous);
            Assert.AreSame(stone, world.GetBlockState(position));
            Assert.IsTrue(world.HasStoredBlock(position));
            Assert.AreEqual(1, world.StoredBlockCount);
        }

        [Test]
        public void SettingAirRemovesStoredState()
        {
            BlockState air = AirState();
            BlockState stone = BlockState("minecraft:stone");
            BlockWorld world = new BlockWorld(air);
            BlockPos position = new BlockPos(1, 2, 3);
            world.SetBlockState(position, stone);

            BlockState previous = world.SetBlockState(position, air);

            Assert.AreSame(stone, previous);
            Assert.AreSame(air, world.GetBlockState(position));
            Assert.IsFalse(world.HasStoredBlock(position));
            Assert.AreEqual(0, world.StoredBlockCount);
        }

        [Test]
        public void AirIsRecognizedByBlockId()
        {
            BlockWorld world = new BlockWorld(AirState());
            BlockState equivalentAir = AirState();
            BlockState stone = BlockState("minecraft:stone");
            BlockPos position = new BlockPos(1, 2, 3);
            world.SetBlockState(position, stone);

            world.SetBlockState(position, equivalentAir);

            Assert.IsTrue(world.IsAir(equivalentAir));
            Assert.AreEqual(0, world.StoredBlockCount);
            Assert.AreSame(world.AirState, world.GetBlockState(position));
        }

        [Test]
        public void RemoveReturnsPreviousStateAndClearsPosition()
        {
            BlockState air = AirState();
            BlockState stone = BlockState("minecraft:stone");
            BlockWorld world = new BlockWorld(air);
            BlockPos position = new BlockPos(1, 2, 3);

            Assert.AreSame(air, world.RemoveBlock(position));

            world.SetBlockState(position, stone);

            Assert.AreSame(stone, world.RemoveBlock(position));
            Assert.AreSame(air, world.GetBlockState(position));
            Assert.AreEqual(0, world.StoredBlockCount);
        }

        [Test]
        public void StoredBlocksAreReturnedInStablePositionOrder()
        {
            BlockWorld world = new BlockWorld(AirState());
            BlockState stone = BlockState("minecraft:stone");
            BlockState dirt = BlockState("minecraft:dirt");

            world.SetBlockState(new BlockPos(2, 0, 0), stone);
            world.SetBlockState(new BlockPos(1, 5, 0), dirt);
            world.SetBlockState(new BlockPos(1, 2, 9), stone);

            IReadOnlyList<WorldBlockEntry> entries = world.GetStoredBlocks();

            Assert.AreEqual(3, entries.Count);
            Assert.AreEqual(new BlockPos(1, 2, 9), entries[0].Position);
            Assert.AreSame(stone, entries[0].State);
            Assert.AreEqual(new BlockPos(1, 5, 0), entries[1].Position);
            Assert.AreSame(dirt, entries[1].State);
            Assert.AreEqual(new BlockPos(2, 0, 0), entries[2].Position);
            Assert.AreSame(stone, entries[2].State);
        }

        [Test]
        public void NullStatesAreRejected()
        {
            BlockWorld world = new BlockWorld(AirState());

            Assert.Throws<ArgumentNullException>(() => new BlockWorld(null));
            Assert.Throws<ArgumentNullException>(() => world.SetBlockState(BlockPos.Zero, null));
            Assert.Throws<ArgumentNullException>(() => world.IsAir(null));
        }

        private static BlockState AirState()
        {
            return BlockState("minecraft:air");
        }

        private static BlockState BlockState(string id)
        {
            return new BlockDefinition(ResourceLocation.Parse(id)).DefaultState;
        }
    }
}
