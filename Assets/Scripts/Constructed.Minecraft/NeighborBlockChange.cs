using System;
using Constructed.Core;

namespace Constructed.Minecraft
{
    public readonly struct NeighborBlockChange
    {
        public NeighborBlockChange(
            BlockWorld world,
            BlockPos position,
            BlockState state,
            BlockPos changedPosition,
            BlockState previousChangedState,
            BlockState newChangedState,
            Direction directionToChanged)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (previousChangedState == null)
                throw new ArgumentNullException(nameof(previousChangedState));
            if (newChangedState == null)
                throw new ArgumentNullException(nameof(newChangedState));

            World = world;
            Position = position;
            State = state;
            ChangedPosition = changedPosition;
            PreviousChangedState = previousChangedState;
            NewChangedState = newChangedState;
            DirectionToChanged = directionToChanged;
        }

        public BlockWorld World { get; }

        public BlockPos Position { get; }

        public BlockState State { get; }

        public BlockPos ChangedPosition { get; }

        public BlockState PreviousChangedState { get; }

        public BlockState NewChangedState { get; }

        public Direction DirectionToChanged { get; }
    }
}
