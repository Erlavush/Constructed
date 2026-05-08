using Constructed.Core;
using Constructed.Minecraft;

namespace Constructed.Create
{
    /// <summary>
    /// Interface for blocks that can be interacted with using a Wrench.
    /// Matches Create mod's IWrenchable.
    /// </summary>
    public interface IWrenchable
    {
        /// <summary>
        /// Called when the block is right-clicked with a wrench.
        /// </summary>
        BlockState GetRotatedBlockState(BlockState state, Direction clickedFace);

        /// <summary>
        /// Called when the block is sneak-right-clicked with a wrench.
        /// Returns true if the block was successfully dismantled.
        /// </summary>
        bool OnWrenched(BlockState state, Direction clickedFace, bool isSneaking);
    }
}
