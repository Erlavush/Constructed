using Constructed.Core;
using Constructed.Minecraft;

namespace Constructed.Create
{
    public sealed class GearboxBlock : BlockDefinition, IWrenchable
    {
        public GearboxBlock(ResourceLocation id, IStateProperty[] properties) : base(id, properties)
        {
        }

        public BlockState GetRotatedBlockState(BlockState state, Direction clickedFace)
        {
            return WrenchableHelper.GetRotatedBlockState(state, clickedFace);
        }

        public bool OnWrenched(BlockState state, Direction clickedFace, bool isSneaking)
        {
            return isSneaking;
        }
    }
}
