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
            if (catalog == null)
                return;

            BlockState currentBelt = change.World.GetBlockState(change.Position);
            if (currentBelt.Definition.Id != catalog.Belt.Id)
                return;

            // Check if the neighbor change is along the rotation axis of the belt
            Axis rotationAxis = DemoBeltRuntimeResolver.GetRotationAxis(currentBelt, catalog);
            if (change.DirectionToChanged.Axis() != rotationAxis)
                return;

            // Check if the neighbor at this side is now a compatible shaft (or motor)
            BlockState neighborState = change.World.GetBlockState(change.ChangedPosition);
            bool hasKineticInput = false;
            
            if (neighborState.Definition.Id == catalog.Shaft.Id)
            {
                hasKineticInput = neighborState.Get(DemoContentCatalog.AxisProperty) == rotationAxis;
            }
            else if (neighborState.Definition.Id == catalog.CreativeMotor.Id)
            {
                hasKineticInput = neighborState.Get(DemoContentCatalog.FacingProperty).Axis() == rotationAxis &&
                                 neighborState.Get(DemoContentCatalog.FacingProperty).Opposite() == change.DirectionToChanged;
            }

            DemoBeltPart currentPart = currentBelt.Get(DemoContentCatalog.BeltPartProperty);

            if (currentPart == DemoBeltPart.Middle && hasKineticInput)
            {
                // Convert to Pulley to allow kinetic propagation
                change.World.SetBlockState(change.Position, currentBelt.With(DemoContentCatalog.BeltPartProperty, DemoBeltPart.Pulley));
            }
            else if (currentPart == DemoBeltPart.Pulley && !hasKineticInput)
            {
                // Check the other side along the same axis before reverting to Middle
                BlockPos otherSidePos = change.Position.Relative(change.DirectionToChanged.Opposite());
                BlockState otherSideState = change.World.GetBlockState(otherSidePos);
                
                bool hasOtherInput = false;
                if (otherSideState.Definition.Id == catalog.Shaft.Id)
                {
                    hasOtherInput = otherSideState.Get(DemoContentCatalog.AxisProperty) == rotationAxis;
                }
                else if (otherSideState.Definition.Id == catalog.CreativeMotor.Id)
                {
                    hasOtherInput = otherSideState.Get(DemoContentCatalog.FacingProperty).Axis() == rotationAxis &&
                                   otherSideState.Get(DemoContentCatalog.FacingProperty).Opposite() == change.DirectionToChanged.Opposite();
                }

                if (!hasOtherInput)
                {
                    // No kinetic input left at this axial position, revert to Middle
                    change.World.SetBlockState(change.Position, currentBelt.With(DemoContentCatalog.BeltPartProperty, DemoBeltPart.Middle));
                }
            }
        }

        public void OnScheduledTick(ScheduledBlockTick tick)
        {
        }
    }
}
