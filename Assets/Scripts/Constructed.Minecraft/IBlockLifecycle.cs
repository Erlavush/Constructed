namespace Constructed.Minecraft
{
    public interface IBlockLifecycle
    {
        void OnBlockPlaced(BlockStateChange change);

        void OnBlockRemoved(BlockStateChange change);

        void OnNeighborChanged(NeighborBlockChange change);

        void OnScheduledTick(ScheduledBlockTick tick);
    }
}
