using Constructed.Core;
using Constructed.Minecraft;

namespace Constructed.Create
{
    public class WrenchItem : ItemDefinition
    {
        public static readonly ResourceLocation WrenchItemId = ResourceLocation.Parse("create:wrench");

        public WrenchItem(ResourceLocation id) : base(id)
        {
        }

        public void Rotate(Constructed.Minecraft.BlockWorld world, BlockPos pos, BlockState state, Direction clickedFace)
        {
            if (state.Definition is IWrenchable wrenchable)
            {
                BlockState rotated = wrenchable.GetRotatedBlockState(state, clickedFace);
                if (rotated != state)
                {
                    world.SetBlockState(pos, rotated);
                    UnityEngine.Debug.Log($"[Sound] create:wrench_rotate at {pos}");
                }
            }
            else
            {
                // Default rotation logic for non-IWrenchable blocks that have axis/facing
                BlockState rotated = WrenchableHelper.GetRotatedBlockState(state, clickedFace);
                if (rotated != state)
                {
                    world.SetBlockState(pos, rotated);
                    UnityEngine.Debug.Log($"[Sound] create:wrench_rotate at {pos}");
                }
            }
        }

        public bool Dismantle(Constructed.Minecraft.BlockWorld world, BlockPos pos, BlockState state, Direction clickedFace)
        {
            if (state.Definition is IWrenchable wrenchable)
            {
                if (wrenchable.OnWrenched(state, clickedFace, true))
                {
                    // Success, block was dismantled by the block itself
                    UnityEngine.Debug.Log($"[Sound] create:wrench_remove at {pos}");
                    return true;
                }
            }

            // Default dismantle logic: break the block and return to inventory
            // For now, we just remove the block. Inventory handling comes in Phase 2.
            // world.SetBlockState(pos, catalog.Air); // Removed for now as we don't have catalog.Air here
            UnityEngine.Debug.Log($"[Sound] create:wrench_remove at {pos}");
            return false; 
        }
    }
}
