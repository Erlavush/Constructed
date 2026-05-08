using Constructed.Core;
using Constructed.Minecraft;

namespace Constructed.Create
{
    public class ShaftBlock : BlockDefinition, IWrenchable
    {
        public ShaftBlock(ResourceLocation id, IStateProperty[] properties) : base(id, properties)
        {
        }

        public BlockState GetRotatedBlockState(BlockState state, Direction clickedFace)
        {
            return WrenchableHelper.GetRotatedBlockState(state, clickedFace);
        }

        public bool OnWrenched(BlockState state, Direction clickedFace, bool isSneaking)
        {
            // Default behavior for now: dismantle returns true to trigger removal
            return isSneaking;
        }
    }
}
