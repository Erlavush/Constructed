using System;
using Constructed.Core;

namespace Constructed.Minecraft
{
    public readonly struct BlockStateChange
    {
        public BlockStateChange(BlockWorld world, BlockPos position, BlockState previousState, BlockState newState)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (previousState == null)
                throw new ArgumentNullException(nameof(previousState));
            if (newState == null)
                throw new ArgumentNullException(nameof(newState));

            World = world;
            Position = position;
            PreviousState = previousState;
            NewState = newState;
        }

        public BlockWorld World { get; }

        public BlockPos Position { get; }

        public BlockState PreviousState { get; }

        public BlockState NewState { get; }

        public bool WasAir
        {
            get { return World.IsAir(PreviousState); }
        }

        public bool IsAir
        {
            get { return World.IsAir(NewState); }
        }
    }
}
