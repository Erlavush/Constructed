using Constructed.Core;
using Constructed.Minecraft;
using System;

namespace Constructed.Create
{
    public sealed class BeltBlockLifecycle : IBlockLifecycle
    {
        private DemoContentCatalog catalog;

        public void Initialize(DemoContentCatalog catalog)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public void OnBlockPlaced(BlockStateChange change)
        {
        }

        public void OnBlockRemoved(BlockStateChange change)
        {
            if (catalog == null)
                return;

            // Only trigger if we are removing the belt or changing it to a non-belt block
            if (change.NewState.Definition.Id == catalog.Belt.Id)
                return;

            // Handle the current position: if it was a pulley, it should leave a shaft behind
            // unless it's being replaced by something else that isn't air (e.g. another block)
            // Actually, Create leaves a shaft even if you replace it with something else? 
            // Usually, if you place a block on a belt, it breaks the belt.
            if (change.IsAir)
            {
                DemoBeltPart previousPart = change.PreviousState.Get(DemoContentCatalog.BeltPartProperty);
                bool previouslyHadPulley = previousPart == DemoBeltPart.Start || previousPart == DemoBeltPart.End || previousPart == DemoBeltPart.Pulley;
                if (previouslyHadPulley)
                {
                    Axis axis = DemoBeltRuntimeResolver.GetRotationAxis(change.PreviousState, catalog);
                    BlockState shaftState = catalog.Shaft.DefaultState.WithValue(DemoContentCatalog.AxisProperty, axis);
                    // Use a flag or check to avoid infinite recursion if we were called from ourselves
                    // But here we are changing from Belt to Shaft, so it won't trigger Belt.OnBlockRemoved again.
                    change.World.SetBlockState(change.Position, shaftState);
                }
            }

            // Recursive destruction of the chain
            // We use the previous state to determine where the next segments were
            foreach (bool forward in new[] { true, false })
            {
                BlockPos? neighborPos = DemoBeltRuntimeResolver.NextSegmentPosition(change.PreviousState, catalog, change.Position, forward);
                if (!neighborPos.HasValue)
                    continue;

                BlockState neighborState = change.World.GetBlockState(neighborPos.Value);
                if (neighborState.Definition.Id != catalog.Belt.Id)
                    continue;

                // Check if the neighbor had a pulley (Start, End, or Pulley part)
                DemoBeltPart neighborPart = neighborState.Get(DemoContentCatalog.BeltPartProperty);
                bool hadPulley = neighborPart == DemoBeltPart.Start || neighborPart == DemoBeltPart.End || neighborPart == DemoBeltPart.Pulley;

                if (hadPulley)
                {
                    // Replace with shaft oriented correctly
                    Axis axis = DemoBeltRuntimeResolver.GetRotationAxis(neighborState, catalog);
                    BlockState shaftState = catalog.Shaft.DefaultState.WithValue(DemoContentCatalog.AxisProperty, axis);
                    change.World.SetBlockState(neighborPos.Value, shaftState);
                }
                else
                {
                    // Remove block (replace with air)
                    change.World.RemoveBlock(neighborPos.Value);
                }
            }
        }

        public void OnNeighborChanged(NeighborBlockChange change)
        {
        }

        public void OnScheduledTick(ScheduledBlockTick tick)
        {
        }
    }
}
