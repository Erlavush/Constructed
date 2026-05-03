using System;
using System.Collections.Generic;
using Constructed.Core;

namespace Constructed.Minecraft
{
    public sealed class BlockWorld
    {
        private static readonly Direction[] NeighborDirections =
        {
            Direction.Down,
            Direction.Up,
            Direction.North,
            Direction.South,
            Direction.West,
            Direction.East
        };

        private readonly Dictionary<BlockPos, BlockState> states;

        public BlockWorld(BlockState airState)
        {
            if (airState == null)
                throw new ArgumentNullException(nameof(airState));

            AirState = airState;
            states = new Dictionary<BlockPos, BlockState>();
        }

        public BlockState AirState { get; }

        public BlockDefinition AirBlock
        {
            get { return AirState.Definition; }
        }

        public int StoredBlockCount
        {
            get { return states.Count; }
        }

        public BlockState GetBlockState(BlockPos position)
        {
            BlockState state;
            return states.TryGetValue(position, out state) ? state : AirState;
        }

        public bool HasStoredBlock(BlockPos position)
        {
            return states.ContainsKey(position);
        }

        public bool IsAir(BlockState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            return state.Definition.Id == AirBlock.Id;
        }

        public BlockState SetBlockState(BlockPos position, BlockState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            BlockState previous = GetBlockState(position);
            BlockState next = IsAir(state) ? AirState : state;
            if (previous.Equals(next))
                return previous;

            if (IsAir(next))
                states.Remove(position);
            else
                states[position] = next;

            BlockStateChange change = new BlockStateChange(this, position, previous, next);
            DispatchBlockLifecycle(change);
            DispatchNeighborUpdates(change);

            return previous;
        }

        public BlockState RemoveBlock(BlockPos position)
        {
            return SetBlockState(position, AirState);
        }

        public void Clear()
        {
            states.Clear();
        }

        public IReadOnlyList<WorldBlockEntry> GetStoredBlocks()
        {
            List<WorldBlockEntry> entries = new List<WorldBlockEntry>(states.Count);
            foreach (KeyValuePair<BlockPos, BlockState> pair in states)
                entries.Add(new WorldBlockEntry(pair.Key, pair.Value));

            entries.Sort(CompareEntries);
            return entries;
        }

        private static int CompareEntries(WorldBlockEntry left, WorldBlockEntry right)
        {
            int x = left.Position.X.CompareTo(right.Position.X);
            if (x != 0)
                return x;

            int y = left.Position.Y.CompareTo(right.Position.Y);
            if (y != 0)
                return y;

            return left.Position.Z.CompareTo(right.Position.Z);
        }

        private void DispatchBlockLifecycle(BlockStateChange change)
        {
            if (!change.WasAir)
                change.PreviousState.Definition.Lifecycle.OnBlockRemoved(change);
            if (!change.IsAir)
                change.NewState.Definition.Lifecycle.OnBlockPlaced(change);
        }

        private void DispatchNeighborUpdates(BlockStateChange change)
        {
            foreach (Direction directionFromChanged in NeighborDirections)
            {
                BlockPos neighborPosition = change.Position.Relative(directionFromChanged);
                BlockState neighborState = GetBlockState(neighborPosition);
                if (IsAir(neighborState))
                    continue;

                NeighborBlockChange neighborChange = new NeighborBlockChange(
                    this,
                    neighborPosition,
                    neighborState,
                    change.Position,
                    change.PreviousState,
                    change.NewState,
                    directionFromChanged.Opposite());

                neighborState.Definition.Lifecycle.OnNeighborChanged(neighborChange);
            }
        }
    }
}
