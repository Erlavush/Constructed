using Constructed.Core;
using Constructed.Create;
using Constructed.Minecraft;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class DemoContentCatalogTests
    {
        [Test]
        public void CatalogRegistersFixedVerticalSliceIds()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();

            Assert.IsTrue(catalog.Items.IsFrozen);
            Assert.IsTrue(catalog.Blocks.IsFrozen);
            Assert.AreEqual(1, catalog.Items.Count);
            Assert.AreEqual(8, catalog.Blocks.Count);
            Assert.AreEqual(ResourceLocation.Parse("create:andesite_alloy"), catalog.DemoTransferItem.Id);
            Assert.AreEqual(ResourceLocation.Parse("minecraft:air"), catalog.Air.Id);
            Assert.AreEqual(ResourceLocation.Parse("minecraft:grass_block"), catalog.Surface.Id);
            Assert.AreEqual(ResourceLocation.Parse("create:creative_motor"), catalog.CreativeMotor.Id);
            Assert.AreEqual(ResourceLocation.Parse("create:shaft"), catalog.Shaft.Id);
            Assert.AreEqual(ResourceLocation.Parse("create:belt"), catalog.Belt.Id);
            Assert.AreEqual(ResourceLocation.Parse("create:creative_crate"), catalog.CreativeCrate.Id);
            Assert.AreEqual(ResourceLocation.Parse("create:brass_funnel"), catalog.BrassFunnel.Id);
            Assert.AreEqual(ResourceLocation.Parse("create:item_vault"), catalog.ItemVault.Id);
        }

        [Test]
        public void CatalogDefinitionsExposeOrientationPropertiesNeededByDemoLayout()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();

            Assert.AreEqual(Direction.East, catalog.CreativeMotor.DefaultState.Get(DemoContentCatalog.FacingProperty));
            Assert.AreEqual(Axis.X, catalog.Shaft.DefaultState.Get(DemoContentCatalog.AxisProperty));
            Assert.AreEqual(Direction.North, catalog.Belt.DefaultState.Get(DemoContentCatalog.BeltFacingProperty));
            Assert.AreEqual(DemoBeltSlope.Horizontal, catalog.Belt.DefaultState.Get(DemoContentCatalog.BeltSlopeProperty));
            Assert.AreEqual(DemoBeltPart.Start, catalog.Belt.DefaultState.Get(DemoContentCatalog.BeltPartProperty));
            Assert.AreEqual(false, catalog.Belt.DefaultState.Get(DemoContentCatalog.BeltCasingProperty));
            Assert.AreEqual(false, catalog.Belt.DefaultState.Get(DemoContentCatalog.BeltWaterloggedProperty));
            Assert.AreEqual(Direction.East, catalog.CreativeCrate.DefaultState.Get(DemoContentCatalog.FacingProperty));
            Assert.AreEqual(Direction.East, catalog.BrassFunnel.DefaultState.Get(DemoContentCatalog.FacingProperty));
            Assert.AreSame(catalog.ItemVaultBlockEntityType, catalog.ItemVault.BlockEntityType);
            Assert.AreEqual(Axis.X, catalog.ItemVault.DefaultState.Get(ItemVaultBlock.HorizontalAxisProperty));
            Assert.AreEqual(false, catalog.ItemVault.DefaultState.Get(ItemVaultBlock.LargeProperty));
        }
    }
}
