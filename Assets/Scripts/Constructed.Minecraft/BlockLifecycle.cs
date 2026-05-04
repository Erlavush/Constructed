namespace Constructed.Minecraft
{
    public static class BlockLifecycle
    {
        public static readonly IBlockLifecycle None = new NoOpBlockLifecycle();

        private sealed class NoOpBlockLifecycle : IBlockLifecycle
        {
            public void OnBlockPlaced(BlockStateChange change)
            {
            }

            public void OnBlockRemoved(BlockStateChange change)
            {
            }

            public void OnNeighborChanged(NeighborBlockChange change)
            {
            }

            public void OnScheduledTick(ScheduledBlockTick tick)
            {
            }
        }
    }
}
