using Constructed.Core;
using Constructed.Minecraft;

namespace Constructed.Create
{
    public class BeltBlock : BlockDefinition, IWrenchable
    {
        public BeltBlock(ResourceLocation id, IStateProperty[] properties, IBlockLifecycle lifecycle) 
            : base(id, properties, lifecycle)
        {
        }

        public BlockState GetRotatedBlockState(BlockState state, Direction clickedFace)
        {
            // Belts are generally not rotated by the wrench in Create (they are chains)
            // However, we can cycle casing or other properties if we want.
            // For now, follow Create: belts don't rotate with wrench.
            return state;
        }

        public bool OnWrenched(BlockState state, Direction clickedFace, bool isSneaking)
        {
            // Dismantle returns true to trigger removal of the whole chain (handled by lifecycle)
            return isSneaking;
        }
    }
}
