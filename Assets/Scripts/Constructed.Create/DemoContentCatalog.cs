using Constructed.Core;
using Constructed.Minecraft;

namespace Constructed.Create
{
    public sealed class DemoContentCatalog
    {
        public static readonly ResourceLocation AirId = ResourceLocation.Parse("minecraft:air");
        public static readonly ResourceLocation SurfaceBlockId = ResourceLocation.Parse("minecraft:grass_block");
        public static readonly ResourceLocation CreativeMotorBlockId = ResourceLocation.Parse("create:creative_motor");
        public static readonly ResourceLocation ShaftBlockId = ResourceLocation.Parse("create:shaft");
        public static readonly ResourceLocation BeltBlockId = ResourceLocation.Parse("create:belt");
        public static readonly ResourceLocation BeltConnectorItemId = ResourceLocation.Parse("create:belt_connector");
        public static readonly ResourceLocation CreativeCrateBlockId = ResourceLocation.Parse("create:creative_crate");
        public static readonly ResourceLocation BrassFunnelBlockId = ResourceLocation.Parse("create:brass_funnel");
        public static readonly ResourceLocation DemoTransferItemId = ResourceLocation.Parse("create:andesite_alloy");

        public static readonly StateProperty<Axis> AxisProperty =
            new StateProperty<Axis>("axis", new[] { Axis.X, Axis.Y, Axis.Z }, Axis.X);
        public static readonly StateProperty<Direction> FacingProperty =
            new StateProperty<Direction>(
                "facing",
                new[] { Direction.Down, Direction.Up, Direction.North, Direction.South, Direction.West, Direction.East },
                Direction.East);
        public static readonly StateProperty<Direction> BeltFacingProperty =
            new StateProperty<Direction>(
                "facing",
                new[] { Direction.North, Direction.South, Direction.West, Direction.East },
                Direction.North);
        public static readonly StateProperty<DemoBeltSlope> BeltSlopeProperty =
            new StateProperty<DemoBeltSlope>(
                "slope",
                new[]
                {
                    DemoBeltSlope.Horizontal,
                    DemoBeltSlope.Upward,
                    DemoBeltSlope.Downward,
                    DemoBeltSlope.Vertical,
                    DemoBeltSlope.Sideways
                },
                DemoBeltSlope.Horizontal);
        public static readonly StateProperty<DemoBeltPart> BeltPartProperty =
            new StateProperty<DemoBeltPart>(
                "part",
                new[]
                {
                    DemoBeltPart.Start,
                    DemoBeltPart.Middle,
                    DemoBeltPart.End,
                    DemoBeltPart.Pulley
                },
                DemoBeltPart.Start);
        public static readonly StateProperty<bool> BeltCasingProperty = StateProperty<bool>.Bool("casing", false);
        public static readonly StateProperty<bool> BeltWaterloggedProperty = StateProperty<bool>.Bool("waterlogged", false);

        private DemoContentCatalog(
            Registry<ItemDefinition> items,
            Registry<BlockDefinition> blocks,
            BlockEntityType itemVaultBlockEntityType,
            ItemDefinition demoTransferItem,
            BlockDefinition air,
            BlockDefinition surface,
            BlockDefinition creativeMotor,
            BlockDefinition shaft,
            BlockDefinition belt,
            BlockDefinition creativeCrate,
            BlockDefinition brassFunnel,
            BlockDefinition itemVault)
        {
            Items = items;
            Blocks = blocks;
            ItemVaultBlockEntityType = itemVaultBlockEntityType;
            DemoTransferItem = demoTransferItem;
            Air = air;
            Surface = surface;
            CreativeMotor = creativeMotor;
            Shaft = shaft;
            Belt = belt;
            CreativeCrate = creativeCrate;
            BrassFunnel = brassFunnel;
            ItemVault = itemVault;
        }

        public Registry<ItemDefinition> Items { get; }

        public Registry<BlockDefinition> Blocks { get; }

        public BlockEntityType ItemVaultBlockEntityType { get; }

        public ItemDefinition DemoTransferItem { get; }

        public BlockDefinition Air { get; }

        public BlockDefinition Surface { get; }

        public BlockDefinition CreativeMotor { get; }

        public BlockDefinition Shaft { get; }

        public BlockDefinition Belt { get; }

        public BlockDefinition CreativeCrate { get; }

        public BlockDefinition BrassFunnel { get; }

        public BlockDefinition ItemVault { get; }

        public static DemoContentCatalog Create()
        {
            ItemDefinition demoTransferItem = new ItemDefinition(DemoTransferItemId);
            Registry<ItemDefinition> items = new Registry<ItemDefinition>(ResourceLocation.Parse("minecraft:item"));
            items.Register(demoTransferItem.Id, demoTransferItem);
            items.Freeze();

            BlockEntityType itemVaultBlockEntityType = ItemVaultBlock.CreateBlockEntityType(items);
            BlockDefinition air = new BlockDefinition(AirId);
            BlockDefinition surface = new BlockDefinition(SurfaceBlockId);
            BlockDefinition creativeMotor = new BlockDefinition(CreativeMotorBlockId, new IStateProperty[] { FacingProperty });
            BlockDefinition shaft = new BlockDefinition(ShaftBlockId, new IStateProperty[] { AxisProperty });
            BlockDefinition belt = new BlockDefinition(
                BeltBlockId,
                new IStateProperty[]
                {
                    BeltFacingProperty,
                    BeltSlopeProperty,
                    BeltPartProperty,
                    BeltCasingProperty,
                    BeltWaterloggedProperty
                });
            BlockDefinition creativeCrate = new BlockDefinition(CreativeCrateBlockId, new IStateProperty[] { FacingProperty });
            BlockDefinition brassFunnel = new BlockDefinition(BrassFunnelBlockId, new IStateProperty[] { FacingProperty });
            BlockDefinition itemVault = ItemVaultBlock.CreateDefinition(itemVaultBlockEntityType);

            Registry<BlockDefinition> blocks = new Registry<BlockDefinition>(ResourceLocation.Parse("minecraft:block"));
            blocks.Register(air.Id, air);
            blocks.Register(surface.Id, surface);
            blocks.Register(creativeMotor.Id, creativeMotor);
            blocks.Register(shaft.Id, shaft);
            blocks.Register(belt.Id, belt);
            blocks.Register(creativeCrate.Id, creativeCrate);
            blocks.Register(brassFunnel.Id, brassFunnel);
            blocks.Register(itemVault.Id, itemVault);
            blocks.Freeze();

            return new DemoContentCatalog(
                items,
                blocks,
                itemVaultBlockEntityType,
                demoTransferItem,
                air,
                surface,
                creativeMotor,
                shaft,
                belt,
                creativeCrate,
                brassFunnel,
                itemVault);
        }
    }
}
