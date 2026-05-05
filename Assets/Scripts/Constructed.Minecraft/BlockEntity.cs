using System;
using System.Collections.Generic;
using Constructed.Core;

namespace Constructed.Minecraft
{
    public abstract class BlockEntity
    {
        private readonly Dictionary<BlockEntityBehaviorType, BlockEntityBehavior> behaviorsByType;
        private readonly List<BlockEntityBehavior> behaviors;
        private int lazyTickCounter;
        private int lazyTickRate;

        protected BlockEntity(BlockEntityType type, BlockWorld world, BlockPos position, BlockState state)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (!ReferenceEquals(state.Definition.BlockEntityType, type))
                throw new ArgumentException($"Block state {state} does not use block entity type {type.Id}.", nameof(state));

            Type = type;
            World = world;
            Position = position;
            State = state;
            behaviorsByType = new Dictionary<BlockEntityBehaviorType, BlockEntityBehavior>();
            behaviors = new List<BlockEntityBehavior>();
            Behaviors = behaviors.AsReadOnly();
            SetLazyTickRate(10);
        }

        public BlockEntityType Type { get; }

        public BlockWorld World { get; }

        public BlockPos Position { get; }

        public BlockState State { get; private set; }

        public bool IsInitialized { get; private set; }

        public bool IsDestroyed { get; private set; }

        public bool IsUnloaded { get; private set; }

        public IReadOnlyList<BlockEntityBehavior> Behaviors { get; }

        public int LazyTickRate
        {
            get { return lazyTickRate; }
        }

        public void SetLazyTickRate(int tickRate)
        {
            if (tickRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickRate), "Lazy tick rate must be positive.");

            lazyTickRate = tickRate;
            lazyTickCounter = tickRate;
        }

        public TBehavior GetBehavior<TBehavior>(BlockEntityBehaviorType<TBehavior> behaviorType)
            where TBehavior : BlockEntityBehavior
        {
            TBehavior behavior;
            return TryGetBehavior(behaviorType, out behavior) ? behavior : null;
        }

        public bool TryGetBehavior<TBehavior>(BlockEntityBehaviorType<TBehavior> behaviorType, out TBehavior behavior)
            where TBehavior : BlockEntityBehavior
        {
            if (behaviorType == null)
                throw new ArgumentNullException(nameof(behaviorType));

            BlockEntityBehavior found;
            if (behaviorsByType.TryGetValue(behaviorType, out found))
            {
                behavior = (TBehavior)found;
                return true;
            }

            behavior = null;
            return false;
        }

        public void AttachBehaviorLate(BlockEntityBehavior behavior)
        {
            AddBehavior(behavior);

            if (IsInitialized)
                behavior.InitializeFromBlockEntity();
        }

        public BlockEntityData SerializeData()
        {
            BlockEntityDataBuilder data = new BlockEntityDataBuilder();
            OnWrite(data);
            foreach (BlockEntityBehavior behavior in behaviors)
                behavior.WriteDataFromBlockEntity(data);

            return data.Build();
        }

        protected void AddBehavior(BlockEntityBehavior behavior)
        {
            if (behavior == null)
                throw new ArgumentNullException(nameof(behavior));
            if (!ReferenceEquals(behavior.BlockEntity, this))
                throw new ArgumentException("Block entity behavior belongs to a different block entity.", nameof(behavior));
            if (behavior.BehaviorType == null)
                throw new ArgumentException("Block entity behavior type cannot be null.", nameof(behavior));
            if (!behavior.BehaviorType.BehaviorClrType.IsInstanceOfType(behavior))
                throw new ArgumentException($"Behavior {behavior.GetType()} does not match behavior type {behavior.BehaviorType}.", nameof(behavior));
            if (behaviorsByType.ContainsKey(behavior.BehaviorType))
                throw new ArgumentException($"Block entity {Type.Id} already has behavior {behavior.BehaviorType}.");

            behaviorsByType.Add(behavior.BehaviorType, behavior);
            behaviors.Add(behavior);
        }

        protected virtual void OnInitialize()
        {
        }

        protected virtual void OnTick()
        {
        }

        protected virtual void OnLazyTick()
        {
        }

        protected virtual void OnBlockStateChanged(BlockState previousState, BlockState newState)
        {
        }

        protected virtual void OnNeighborChanged(BlockPos neighborPosition)
        {
        }

        protected virtual void OnDestroyed()
        {
        }

        protected virtual void OnUnloaded()
        {
        }

        protected virtual void OnWrite(BlockEntityDataBuilder data)
        {
        }

        protected virtual void OnRead(BlockEntityData data)
        {
        }

        internal void TickFromWorld()
        {
            if (IsUnloaded)
                return;

            if (!IsInitialized)
            {
                IsInitialized = true;
                OnInitialize();
                foreach (BlockEntityBehavior behavior in behaviors)
                    behavior.InitializeFromBlockEntity();
                OnLazyTick();
            }

            if (lazyTickCounter-- <= 0)
            {
                lazyTickCounter = lazyTickRate;
                OnLazyTick();
            }

            OnTick();
            foreach (BlockEntityBehavior behavior in behaviors)
                behavior.TickFromBlockEntity();
        }

        internal void ChangeStateFromWorld(BlockState newState)
        {
            if (newState == null)
                throw new ArgumentNullException(nameof(newState));
            if (!ReferenceEquals(newState.Definition.BlockEntityType, Type))
                throw new ArgumentException($"Block state {newState} does not use block entity type {Type.Id}.", nameof(newState));

            BlockState previousState = State;
            if (ReferenceEquals(previousState, newState))
                return;

            State = newState;
            OnBlockStateChanged(previousState, newState);
            foreach (BlockEntityBehavior behavior in behaviors)
                behavior.BlockStateChangedFromBlockEntity(previousState, newState);
        }

        internal void NeighborChangedFromWorld(BlockPos neighborPosition)
        {
            if (IsUnloaded)
                return;

            OnNeighborChanged(neighborPosition);
            foreach (BlockEntityBehavior behavior in behaviors)
                behavior.NeighborChangedFromBlockEntity(neighborPosition);
        }

        internal void DestroyFromWorld()
        {
            if (IsDestroyed)
                return;

            IsDestroyed = true;
            OnDestroyed();
            foreach (BlockEntityBehavior behavior in behaviors)
                behavior.DestroyFromBlockEntity();
        }

        internal void UnloadFromWorld()
        {
            if (IsUnloaded)
                return;

            IsUnloaded = true;
            OnUnloaded();
            foreach (BlockEntityBehavior behavior in behaviors)
                behavior.UnloadFromBlockEntity();
        }

        internal void DeserializeDataFromWorld(BlockEntityData data)
        {
            OnRead(data);
            foreach (BlockEntityBehavior behavior in behaviors)
                behavior.ReadDataFromBlockEntity(data);
        }
    }
}
