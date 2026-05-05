using System;
using Constructed.Core;

namespace Constructed.Minecraft
{
    public abstract class BlockEntityBehavior
    {
        private int lazyTickCounter;
        private int lazyTickRate;

        protected BlockEntityBehavior(BlockEntity blockEntity)
        {
            if (blockEntity == null)
                throw new ArgumentNullException(nameof(blockEntity));

            BlockEntity = blockEntity;
            SetLazyTickRate(10);
        }

        public abstract BlockEntityBehaviorType BehaviorType { get; }

        public BlockEntity BlockEntity { get; internal set; }

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

        internal void InitializeFromBlockEntity()
        {
            OnInitialize();
        }

        internal void TickFromBlockEntity()
        {
            if (lazyTickCounter-- <= 0)
            {
                lazyTickCounter = lazyTickRate;
                OnLazyTick();
            }

            OnTick();
        }

        internal void BlockStateChangedFromBlockEntity(BlockState previousState, BlockState newState)
        {
            OnBlockStateChanged(previousState, newState);
        }

        internal void NeighborChangedFromBlockEntity(BlockPos neighborPosition)
        {
            OnNeighborChanged(neighborPosition);
        }

        internal void DestroyFromBlockEntity()
        {
            OnDestroyed();
        }

        internal void UnloadFromBlockEntity()
        {
            OnUnloaded();
        }
    }
}
