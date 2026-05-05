using Constructed.Core;
using Constructed.Create;
using Constructed.Minecraft;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class ItemVaultBlockEntityTests
    {
        [Test]
        public void VaultDefinitionUsesCreateIdsPropertiesAndBlockEntityType()
        {
            Registry<ItemDefinition> items = CreateItemRegistry(new ItemDefinition(ResourceLocation.Parse("create:andesite_alloy")));
            BlockEntityType vaultType = ItemVaultBlock.CreateBlockEntityType(items);
            BlockDefinition vault = ItemVaultBlock.CreateDefinition(vaultType);

            Assert.AreEqual(ResourceLocation.Parse("create:item_vault"), vault.Id);
            Assert.AreSame(vaultType, vault.BlockEntityType);
            Assert.AreEqual(Axis.X, vault.DefaultState.Get(ItemVaultBlock.HorizontalAxisProperty));
            Assert.AreEqual(false, vault.DefaultState.Get(ItemVaultBlock.LargeProperty));
            Assert.AreEqual(20, ItemVaultBlock.DefaultSlotsPerBlock);
        }

        [Test]
        public void PlacingVaultCreatesSingleBlockInventoryEntity()
        {
            TestCatalog catalog = TestCatalog.Create();
            BlockWorld world = new BlockWorld(catalog.Air.DefaultState);
            BlockPos position = new BlockPos(1, 2, 3);

            world.SetBlockState(position, catalog.Vault.DefaultState);

            ItemVaultBlockEntity vault = world.GetBlockEntity<ItemVaultBlockEntity>(position);
            Assert.NotNull(vault);
            Assert.IsTrue(vault.IsController);
            Assert.AreEqual(1, vault.Radius);
            Assert.AreEqual(1, vault.Length);
            Assert.AreEqual(ItemVaultBlock.DefaultSlotsPerBlock, vault.Inventory.SlotCount);
            Assert.AreEqual(0, vault.Inventory.TotalItemCount);
        }

        [Test]
        public void VaultAcceptsTransferInAndOut()
        {
            TestCatalog catalog = TestCatalog.Create();
            BlockWorld world = new BlockWorld(catalog.Air.DefaultState);
            world.SetBlockState(BlockPos.Zero, catalog.Vault.DefaultState);
            ItemVaultBlockEntity vault = world.GetBlockEntity<ItemVaultBlockEntity>(BlockPos.Zero);

            ItemStack insertRemainder = vault.Insert(new ItemStack(catalog.Alloy, 32));
            ItemStack extracted = vault.Extract(0, 12);

            Assert.AreSame(ItemStack.Empty, insertRemainder);
            Assert.AreEqual(12, extracted.Count);
            Assert.AreSame(catalog.Alloy, extracted.Item);
            Assert.AreEqual(20, vault.Inventory.GetStack(0).Count);
        }

        [Test]
        public void VaultInventoryPersistsThroughWorldSnapshot()
        {
            TestCatalog catalog = TestCatalog.Create();
            BlockWorld source = new BlockWorld(catalog.Air.DefaultState);
            BlockPos vaultPosition = new BlockPos(2, 0, 0);
            source.SetBlockState(vaultPosition, catalog.Vault.DefaultState.With(ItemVaultBlock.HorizontalAxisProperty, Axis.Z));
            ItemVaultBlockEntity sourceVault = source.GetBlockEntity<ItemVaultBlockEntity>(vaultPosition);
            sourceVault.Insert(new ItemStack(catalog.Alloy, 40));
            sourceVault.Insert(new ItemStack(catalog.Zinc, 5));

            SerializedBlockWorld snapshot = source.Serialize();
            BlockWorld restored = BlockWorld.Deserialize(catalog.Air.DefaultState, snapshot, catalog.Blocks);
            ItemVaultBlockEntity restoredVault = restored.GetBlockEntity<ItemVaultBlockEntity>(vaultPosition);

            Assert.AreEqual(Axis.Z, restored.GetBlockState(vaultPosition).Get(ItemVaultBlock.HorizontalAxisProperty));
            Assert.AreEqual(40, restoredVault.Inventory.GetStack(0).Count);
            Assert.AreSame(catalog.Alloy, restoredVault.Inventory.GetStack(0).Item);
            Assert.AreEqual(5, restoredVault.Inventory.GetStack(1).Count);
            Assert.AreSame(catalog.Zinc, restoredVault.Inventory.GetStack(1).Item);
            Assert.AreEqual(snapshot, restored.Serialize());
        }

        private sealed class TestCatalog
        {
            private TestCatalog(
                Registry<ItemDefinition> items,
                Registry<BlockDefinition> blocks,
                ItemDefinition alloy,
                ItemDefinition zinc,
                BlockDefinition air,
                BlockDefinition vault)
            {
                Items = items;
                Blocks = blocks;
                Alloy = alloy;
                Zinc = zinc;
                Air = air;
                Vault = vault;
            }

            public Registry<ItemDefinition> Items { get; }

            public Registry<BlockDefinition> Blocks { get; }

            public ItemDefinition Alloy { get; }

            public ItemDefinition Zinc { get; }

            public BlockDefinition Air { get; }

            public BlockDefinition Vault { get; }

            public static TestCatalog Create()
            {
                ItemDefinition alloy = new ItemDefinition(ResourceLocation.Parse("create:andesite_alloy"));
                ItemDefinition zinc = new ItemDefinition(ResourceLocation.Parse("create:zinc_ingot"));
                Registry<ItemDefinition> items = CreateItemRegistry(alloy, zinc);
                BlockDefinition air = new BlockDefinition(ResourceLocation.Parse("minecraft:air"));
                BlockEntityType vaultType = ItemVaultBlock.CreateBlockEntityType(items);
                BlockDefinition vault = ItemVaultBlock.CreateDefinition(vaultType);
                Registry<BlockDefinition> blocks = new Registry<BlockDefinition>(ResourceLocation.Parse("minecraft:block"));
                blocks.Register(air.Id, air);
                blocks.Register(vault.Id, vault);
                blocks.Freeze();
                return new TestCatalog(items, blocks, alloy, zinc, air, vault);
            }
        }

        private static Registry<ItemDefinition> CreateItemRegistry(params ItemDefinition[] definitions)
        {
            Registry<ItemDefinition> registry = new Registry<ItemDefinition>(ResourceLocation.Parse("minecraft:item"));
            foreach (ItemDefinition definition in definitions)
                registry.Register(definition.Id, definition);
            registry.Freeze();
            return registry;
        }
    }
}
