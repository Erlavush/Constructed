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
        private readonly Dictionary<BlockPos, BlockEntity> blockEntities;
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
            blockEntities = new Dictionary<BlockPos, BlockEntity>();
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

        public int StoredBlockEntityCount
        {
            get { return blockEntities.Count; }
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

        public bool HasBlockEntity(BlockPos position)
        {
            return blockEntities.ContainsKey(position);
        }

        public BlockEntity GetBlockEntity(BlockPos position)
        {
            BlockEntity blockEntity;
            return blockEntities.TryGetValue(position, out blockEntity) ? blockEntity : null;
        }

        public TBlockEntity GetBlockEntity<TBlockEntity>(BlockPos position)
            where TBlockEntity : BlockEntity
        {
            return GetBlockEntity(position) as TBlockEntity;
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

            UpdateBlockEntityForStateChange(position, next);

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
            RunBlockEntityTicks();
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

        public int RunBlockEntityTicks()
        {
            if (blockEntities.Count == 0)
                return 0;

            List<BlockEntityTickEntry> tickEntries = GetSortedBlockEntityEntries();

            int executed = 0;
            foreach (BlockEntityTickEntry tickEntry in tickEntries)
            {
                BlockEntity current;
                if (!blockEntities.TryGetValue(tickEntry.Position, out current))
                    continue;
                if (!ReferenceEquals(current, tickEntry.BlockEntity))
                    continue;

                current.TickFromWorld();
                executed++;
            }

            return executed;
        }

        public SerializedBlockWorld Serialize()
        {
            List<SerializedWorldBlockEntry> blockEntries = new List<SerializedWorldBlockEntry>(states.Count);
            foreach (WorldBlockEntry entry in GetStoredBlocks())
                blockEntries.Add(new SerializedWorldBlockEntry(entry.Position, entry.State.Serialize()));

            List<SerializedBlockEntity> blockEntityEntries = new List<SerializedBlockEntity>(blockEntities.Count);
            foreach (BlockEntityTickEntry entry in GetSortedBlockEntityEntries())
            {
                blockEntityEntries.Add(new SerializedBlockEntity(
                    entry.Position,
                    entry.BlockEntity.Type.Id,
                    entry.BlockEntity.SerializeData()));
            }

            return new SerializedBlockWorld(CurrentTick, blockEntries, blockEntityEntries);
        }

        public static BlockWorld Deserialize(
            BlockState airState,
            SerializedBlockWorld snapshot,
            Registry<BlockDefinition> blockRegistry)
        {
            BlockWorld world = new BlockWorld(airState, snapshot.CurrentTick);
            world.LoadSnapshot(snapshot, blockRegistry);
            return world;
        }

        public void LoadSnapshot(SerializedBlockWorld snapshot, Registry<BlockDefinition> blockRegistry)
        {
            if (blockRegistry == null)
                throw new ArgumentNullException(nameof(blockRegistry));

            Clear();
            clock.Reset(snapshot.CurrentTick);

            foreach (SerializedWorldBlockEntry entry in snapshot.Blocks)
            {
                BlockDefinition block = blockRegistry.GetValue(entry.State.BlockId);
                BlockState state = block.CreateState(entry.State);
                if (IsAir(state))
                    continue;

                states.Add(entry.Position, state);
                CreateBlockEntityForState(entry.Position, state);
            }

            foreach (SerializedBlockEntity serializedBlockEntity in snapshot.BlockEntities)
            {
                BlockEntity blockEntity = GetBlockEntity(serializedBlockEntity.Position);
                if (blockEntity == null)
                    throw new InvalidOperationException($"Serialized block entity {serializedBlockEntity.TypeId} at {serializedBlockEntity.Position} has no matching block entity block state.");
                if (blockEntity.Type.Id != serializedBlockEntity.TypeId)
                    throw new InvalidOperationException($"Serialized block entity type {serializedBlockEntity.TypeId} at {serializedBlockEntity.Position} does not match loaded type {blockEntity.Type.Id}.");

                blockEntity.DeserializeDataFromWorld(serializedBlockEntity.Data);
            }
        }

        public void Clear()
        {
            foreach (BlockEntity blockEntity in blockEntities.Values)
                blockEntity.UnloadFromWorld();

            states.Clear();
            blockEntities.Clear();
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

        private List<BlockEntityTickEntry> GetSortedBlockEntityEntries()
        {
            List<BlockEntityTickEntry> tickEntries = new List<BlockEntityTickEntry>(blockEntities.Count);
            foreach (KeyValuePair<BlockPos, BlockEntity> pair in blockEntities)
                tickEntries.Add(new BlockEntityTickEntry(pair.Key, pair.Value));

            tickEntries.Sort(CompareBlockEntityTickEntries);
            return tickEntries;
        }

        private static int CompareBlockEntityTickEntries(BlockEntityTickEntry left, BlockEntityTickEntry right)
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

        private void UpdateBlockEntityForStateChange(BlockPos position, BlockState next)
        {
            BlockEntity existing;
            blockEntities.TryGetValue(position, out existing);

            BlockEntityType nextType = IsAir(next) ? null : next.Definition.BlockEntityType;
            if (existing != null && ReferenceEquals(existing.Type, nextType))
            {
                existing.ChangeStateFromWorld(next);
                return;
            }

            if (existing != null)
            {
                blockEntities.Remove(position);
                existing.DestroyFromWorld();
                existing.UnloadFromWorld();
            }

            if (nextType == null)
                return;

            CreateBlockEntityForState(position, next);
        }

        private void CreateBlockEntityForState(BlockPos position, BlockState state)
        {
            BlockEntityType blockEntityType = state.Definition.BlockEntityType;
            if (blockEntityType == null)
                return;

            BlockEntity blockEntity = blockEntityType.Create(this, position, state);
            blockEntities.Add(position, blockEntity);
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

                BlockEntity neighborBlockEntity = GetBlockEntity(neighborPosition);
                if (neighborBlockEntity != null)
                    neighborBlockEntity.NeighborChangedFromWorld(change.Position);
            }
        }

        private readonly struct BlockEntityTickEntry
        {
            public BlockEntityTickEntry(BlockPos position, BlockEntity blockEntity)
            {
                Position = position;
                BlockEntity = blockEntity;
            }

            public BlockPos Position { get; }

            public BlockEntity BlockEntity { get; }
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
