using System;
using Constructed.Core;
using Constructed.Minecraft;

namespace Constructed.Create
{
    public sealed class CogWheelBlock : BlockDefinition, IWrenchable
    {
        private static readonly Direction[] SurvivalCheckDirections =
        {
            Direction.Down,
            Direction.Up,
            Direction.North,
            Direction.South,
            Direction.West,
            Direction.East
        };

        public CogWheelBlock(ResourceLocation id, IStateProperty[] properties, bool isLarge) : base(id, properties)
        {
            IsLarge = isLarge;
        }

        public bool IsLarge { get; }

        public bool IsSmall
        {
            get { return !IsLarge; }
        }

        public BlockState GetRotatedBlockState(BlockState state, Direction clickedFace)
        {
            return WrenchableHelper.GetRotatedBlockState(state, clickedFace);
        }

        public bool OnWrenched(BlockState state, Direction clickedFace, bool isSneaking)
        {
            return isSneaking;
        }

        public static bool IsValidCogwheelPosition(bool large, DemoContentCatalog catalog, BlockWorld world, BlockPos position, Axis cogAxis)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (world == null)
                throw new ArgumentNullException(nameof(world));

            foreach (Direction facing in SurvivalCheckDirections)
            {
                if (facing.Axis() == cogAxis)
                    continue;

                BlockState blockState = world.GetBlockState(position.Relative(facing));
                if (blockState.Definition.HasProperty(DemoContentCatalog.AxisProperty) &&
                    facing.Axis() == blockState.Get(DemoContentCatalog.AxisProperty))
                {
                    continue;
                }

                if (IsLargeCog(catalog, blockState) || (large && IsSmallCog(catalog, blockState)))
                    return false;
            }

            return true;
        }

        public static bool IsSmallCog(DemoContentCatalog catalog, BlockState state)
        {
            return state != null && state.Definition.Id == catalog.Cogwheel.Id;
        }

        public static bool IsLargeCog(DemoContentCatalog catalog, BlockState state)
        {
            return state != null && state.Definition.Id == catalog.LargeCogwheel.Id;
        }

        public static bool IsCog(DemoContentCatalog catalog, BlockState state)
        {
            return IsSmallCog(catalog, state) || IsLargeCog(catalog, state);
        }
    }
}
