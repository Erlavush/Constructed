using System;
using System.Collections.Generic;
using Constructed.Core;

namespace Constructed.Minecraft
{
    public readonly struct SerializedBlockWorld : IEquatable<SerializedBlockWorld>
    {
        private readonly SerializedWorldBlockEntry[] blocks;
        private readonly SerializedBlockEntity[] blockEntities;

        public SerializedBlockWorld(
            long currentTick,
            IEnumerable<SerializedWorldBlockEntry> blocks,
            IEnumerable<SerializedBlockEntity> blockEntities)
        {
            if (currentTick < 0)
                throw new ArgumentOutOfRangeException(nameof(currentTick), "Serialized world tick cannot be negative.");

            CurrentTick = currentTick;
            this.blocks = CopyBlocks(blocks);
            this.blockEntities = CopyBlockEntities(blockEntities);
        }

        public long CurrentTick { get; }

        public IReadOnlyList<SerializedWorldBlockEntry> Blocks
        {
            get { return blocks ?? Array.Empty<SerializedWorldBlockEntry>(); }
        }

        public IReadOnlyList<SerializedBlockEntity> BlockEntities
        {
            get { return blockEntities ?? Array.Empty<SerializedBlockEntity>(); }
        }

        public bool Equals(SerializedBlockWorld other)
        {
            if (CurrentTick != other.CurrentTick || Blocks.Count != other.Blocks.Count || BlockEntities.Count != other.BlockEntities.Count)
                return false;

            for (int i = 0; i < Blocks.Count; i++)
            {
                if (Blocks[i] != other.Blocks[i])
                    return false;
            }

            for (int i = 0; i < BlockEntities.Count; i++)
            {
                if (BlockEntities[i] != other.BlockEntities[i])
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is SerializedBlockWorld other && Equals(other);
        }

        public override int GetHashCode()
        {
            int hash = CurrentTick.GetHashCode();
            foreach (SerializedWorldBlockEntry block in Blocks)
                hash = HashCode.Combine(hash, block);
            foreach (SerializedBlockEntity blockEntity in BlockEntities)
                hash = HashCode.Combine(hash, blockEntity);

            return hash;
        }

        public static bool operator ==(SerializedBlockWorld left, SerializedBlockWorld right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SerializedBlockWorld left, SerializedBlockWorld right)
        {
            return !left.Equals(right);
        }

        private static SerializedWorldBlockEntry[] CopyBlocks(IEnumerable<SerializedWorldBlockEntry> blocks)
        {
            if (blocks == null)
                return Array.Empty<SerializedWorldBlockEntry>();

            List<SerializedWorldBlockEntry> copy = new List<SerializedWorldBlockEntry>();
            HashSet<BlockPos> positions = new HashSet<BlockPos>();
            foreach (SerializedWorldBlockEntry block in blocks)
            {
                if (!positions.Add(block.Position))
                    throw new ArgumentException($"Duplicate serialized block position {block.Position}.");

                copy.Add(block);
            }

            return copy.ToArray();
        }

        private static SerializedBlockEntity[] CopyBlockEntities(IEnumerable<SerializedBlockEntity> blockEntities)
        {
            if (blockEntities == null)
                return Array.Empty<SerializedBlockEntity>();

            List<SerializedBlockEntity> copy = new List<SerializedBlockEntity>();
            HashSet<BlockPos> positions = new HashSet<BlockPos>();
            foreach (SerializedBlockEntity blockEntity in blockEntities)
            {
                if (!positions.Add(blockEntity.Position))
                    throw new ArgumentException($"Duplicate serialized block entity position {blockEntity.Position}.");

                copy.Add(blockEntity);
            }

            return copy.ToArray();
        }
    }
}
