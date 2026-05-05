using System;
using Constructed.Core;
using Constructed.Minecraft;

namespace Constructed.Create
{
    public static class ItemVaultBlock
    {
        public const int DefaultSlotsPerBlock = 20;

        public static readonly ResourceLocation Id = ResourceLocation.Parse("create:item_vault");
        public static readonly ResourceLocation BlockEntityTypeId = ResourceLocation.Parse("create:item_vault");
        public static readonly StateProperty<Axis> HorizontalAxisProperty =
            new StateProperty<Axis>("horizontal_axis", new[] { Axis.X, Axis.Z }, Axis.X);
        public static readonly StateProperty<bool> LargeProperty = StateProperty<bool>.Bool("large");

        public static BlockEntityType CreateBlockEntityType(Registry<ItemDefinition> itemRegistry)
        {
            return CreateBlockEntityType(itemRegistry, DefaultSlotsPerBlock);
        }

        public static BlockEntityType CreateBlockEntityType(Registry<ItemDefinition> itemRegistry, int slotsPerBlock)
        {
            if (itemRegistry == null)
                throw new ArgumentNullException(nameof(itemRegistry));
            if (slotsPerBlock <= 0)
                throw new ArgumentOutOfRangeException(nameof(slotsPerBlock), "Item Vault slots per block must be positive.");

            return new BlockEntityType(
                BlockEntityTypeId,
                (type, world, position, state) => new ItemVaultBlockEntity(type, world, position, state, itemRegistry, slotsPerBlock));
        }

        public static BlockDefinition CreateDefinition(BlockEntityType blockEntityType)
        {
            return new BlockDefinition(
                Id,
                new IStateProperty[] { HorizontalAxisProperty, LargeProperty },
                BlockLifecycle.None,
                blockEntityType);
        }
    }
}
