using Constructed.Core;
using Constructed.Minecraft;

namespace Constructed.Create
{
    public class CreativeMotorBlock : BlockDefinition, IWrenchable
    {
        public CreativeMotorBlock(ResourceLocation id, IStateProperty[] properties) : base(id, properties)
        {
        }

        public BlockState GetRotatedBlockState(BlockState state, Direction clickedFace)
        {
            // Motors rotate around the clicked face axis
            return WrenchableHelper.GetRotatedBlockState(state, clickedFace);
        }

        public bool OnWrenched(BlockState state, Direction clickedFace, bool isSneaking)
        {
            return isSneaking;
        }
    }
}
