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

        private readonly SimulationClock clock;
        private readonly Dictionary<BlockPos, BlockState> states;
        private readonly List<ScheduledBlockTickEntry> scheduledBlockTicks;
        private readonly HashSet<ScheduledBlockTickKey> scheduledBlockTickKeys;
        private long nextScheduledBlockTickOrder;

        public BlockWorld(BlockState airState)
            : this(airState, 0)
        {
        }

        public BlockWorld(BlockState airState, long initialTick)
        {
            if (airState == null)
                throw new ArgumentNullException(nameof(airState));

            AirState = airState;
            clock = new SimulationClock(initialTick);
            states = new Dictionary<BlockPos, BlockState>();
            scheduledBlockTicks = new List<ScheduledBlockTickEntry>();
            scheduledBlockTickKeys = new HashSet<ScheduledBlockTickKey>();
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

        public long CurrentTick
        {
            get { return clock.CurrentTick; }
        }

        public int PendingScheduledBlockTickCount
        {
            get { return scheduledBlockTicks.Count; }
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

        public bool ScheduleBlockTick(
            BlockPos position,
            BlockDefinition block,
            long delay,
            ScheduledTickPriority priority = ScheduledTickPriority.Normal)
        {
            if (block == null)
                throw new ArgumentNullException(nameof(block));
            if (delay < 0)
                throw new ArgumentOutOfRangeException(nameof(delay), "Scheduled block tick delay cannot be negative.");

            ScheduledBlockTickKey key = new ScheduledBlockTickKey(position, block.Id);
            if (!scheduledBlockTickKeys.Add(key))
                return false;

            long triggerTick;
            checked
            {
                triggerTick = CurrentTick + delay;
            }

            ScheduledBlockTickEntry entry = new ScheduledBlockTickEntry(
                position,
                block,
                CurrentTick,
                triggerTick,
                priority,
                nextScheduledBlockTickOrder++);
            scheduledBlockTicks.Add(entry);
            return true;
        }

        public bool ScheduleBlockTick(
            BlockPos position,
            BlockState state,
            long delay,
            ScheduledTickPriority priority = ScheduledTickPriority.Normal)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            return ScheduleBlockTick(position, state.Definition, delay, priority);
        }

        public bool HasScheduledBlockTick(BlockPos position, BlockDefinition block)
        {
            if (block == null)
                throw new ArgumentNullException(nameof(block));

            return scheduledBlockTickKeys.Contains(new ScheduledBlockTickKey(position, block.Id));
        }

        public bool HasScheduledBlockTick(BlockPos position, BlockState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            return HasScheduledBlockTick(position, state.Definition);
        }

        public void Tick()
        {
            clock.Advance();
            RunScheduledBlockTicks();
        }

        public void Tick(long ticks)
        {
            if (ticks <= 0)
                throw new ArgumentOutOfRangeException(nameof(ticks), "Tick count must be positive.");

            for (long i = 0; i < ticks; i++)
                Tick();
        }

        public int RunScheduledBlockTicks()
        {
            if (scheduledBlockTicks.Count == 0)
                return 0;

            List<ScheduledBlockTickEntry> due = new List<ScheduledBlockTickEntry>();
            for (int i = scheduledBlockTicks.Count - 1; i >= 0; i--)
            {
                ScheduledBlockTickEntry entry = scheduledBlockTicks[i];
                if (entry.TriggerTick > CurrentTick)
                    continue;

                scheduledBlockTicks.RemoveAt(i);
                scheduledBlockTickKeys.Remove(new ScheduledBlockTickKey(entry.Position, entry.Block.Id));
                due.Add(entry);
            }

            if (due.Count == 0)
                return 0;

            due.Sort(CompareScheduledBlockTicks);

            int executed = 0;
            foreach (ScheduledBlockTickEntry entry in due)
            {
                BlockState currentState = GetBlockState(entry.Position);
                if (currentState.Definition.Id != entry.Block.Id)
                    continue;

                ScheduledBlockTick tick = new ScheduledBlockTick(
                    this,
                    entry.Position,
                    entry.Block,
                    currentState,
                    entry.ScheduledAtTick,
                    entry.TriggerTick,
                    entry.Priority,
                    entry.Order);

                currentState.Definition.Lifecycle.OnScheduledTick(tick);
                executed++;
            }

            return executed;
        }

        public void Clear()
        {
            states.Clear();
            scheduledBlockTicks.Clear();
            scheduledBlockTickKeys.Clear();
            nextScheduledBlockTickOrder = 0;
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

        private static int CompareScheduledBlockTicks(ScheduledBlockTickEntry left, ScheduledBlockTickEntry right)
        {
            int trigger = left.TriggerTick.CompareTo(right.TriggerTick);
            if (trigger != 0)
                return trigger;

            int priority = left.Priority.CompareTo(right.Priority);
            if (priority != 0)
                return priority;

            return left.Order.CompareTo(right.Order);
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

        private readonly struct ScheduledBlockTickKey : IEquatable<ScheduledBlockTickKey>
        {
            public ScheduledBlockTickKey(BlockPos position, ResourceLocation blockId)
            {
                Position = position;
                BlockId = blockId;
            }

            public BlockPos Position { get; }

            public ResourceLocation BlockId { get; }

            public bool Equals(ScheduledBlockTickKey other)
            {
                return Position == other.Position && BlockId == other.BlockId;
            }

            public override bool Equals(object obj)
            {
                return obj is ScheduledBlockTickKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Position, BlockId);
            }
        }

        private readonly struct ScheduledBlockTickEntry
        {
            public ScheduledBlockTickEntry(
                BlockPos position,
                BlockDefinition block,
                long scheduledAtTick,
                long triggerTick,
                ScheduledTickPriority priority,
                long order)
            {
                Position = position;
                Block = block;
                ScheduledAtTick = scheduledAtTick;
                TriggerTick = triggerTick;
                Priority = priority;
                Order = order;
            }

            public BlockPos Position { get; }

            public BlockDefinition Block { get; }

            public long ScheduledAtTick { get; }

            public long TriggerTick { get; }

            public ScheduledTickPriority Priority { get; }

            public long Order { get; }
        }
    }
}
