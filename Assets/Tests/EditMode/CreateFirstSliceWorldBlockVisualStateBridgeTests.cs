using System.IO;
using Constructed.Core;
using Constructed.Create;
using Constructed.Minecraft;
using Constructed.Unity;
using NUnit.Framework;
using UnityEngine;

namespace Constructed.Tests
{
    public sealed class CreateFirstSliceWorldBlockVisualStateBridgeTests
    {
        [Test]
        public void BridgeSerializesShaftAxisFromRuntimeState()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockState state = catalog.Shaft.DefaultState.With(DemoContentCatalog.AxisProperty, Axis.Z);

            AssertBridgeProperties(
                state,
                new[]
                {
                    new BlockStatePropertyValue("axis", "z")
                });
        }

        [Test]
        public void BridgeSerializesFacingBasedBlocksFromRuntimeState()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();

            AssertBridgeProperties(
                catalog.CreativeMotor.DefaultState.With(DemoContentCatalog.FacingProperty, Direction.Up),
                new[]
                {
                    new BlockStatePropertyValue("facing", "up")
                });

            AssertBridgeProperties(
                catalog.CreativeCrate.DefaultState.With(DemoContentCatalog.FacingProperty, Direction.Down),
                new[]
                {
                    new BlockStatePropertyValue("facing", "down")
                });
        }

        [Test]
        public void BridgeAddsCurrentFunnelVisualDefaultsAroundRuntimeFacing()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockState state = catalog.BrassFunnel.DefaultState.With(DemoContentCatalog.FacingProperty, Direction.West);

            AssertBridgeProperties(
                state,
                new[]
                {
                    new BlockStatePropertyValue("extracting", "false"),
                    new BlockStatePropertyValue("facing", "west"),
                    new BlockStatePropertyValue("powered", "false"),
                    new BlockStatePropertyValue("waterlogged", "false")
                });
        }

        [Test]
        public void BridgeAliasesItemVaultHorizontalAxisToVisualAxis()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockState state = catalog.ItemVault.DefaultState
                .With(ItemVaultBlock.HorizontalAxisProperty, Axis.Z)
                .With(ItemVaultBlock.LargeProperty, true);

            AssertBridgeProperties(
                state,
                new[]
                {
                    new BlockStatePropertyValue("axis", "z"),
                    new BlockStatePropertyValue("large", "true")
                });
        }

        [Test]
        public void BridgeSkipsUnsupportedWorldBlocks()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();

            Assert.IsFalse(CreateFirstSliceWorldBlockVisualStateBridge.TryResolve(catalog.Surface.DefaultState, out _));
            Assert.IsFalse(CreateFirstSliceWorldBlockVisualStateBridge.TryResolve(catalog.Belt.DefaultState, out _));
        }

        [Test]
        public void DemoWorldSupportedPlacementsResolveAgainstCreateBlockstates()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            MinecraftBlockStateLoader loader = CreateLoader();
            int supportedPlacements = 0;

            foreach (WorldBlockEntry entry in DemoVerticalSliceBootstrap.CreateVerticalSlicePlacements(catalog))
            {
                if (!CreateFirstSliceWorldBlockVisualStateBridge.TryResolve(entry.State, out BlockStatePropertyValue[] visualProperties))
                    continue;

                MinecraftBlockStateDefinition definition = loader.LoadBlockState(entry.State.Definition.Id);
                Assert.DoesNotThrow(() => definition.ResolveVariant(visualProperties), entry.Position.ToString());
                supportedPlacements++;
            }

            Assert.AreEqual(3, supportedPlacements);
        }

        private static void AssertBridgeProperties(BlockState state, BlockStatePropertyValue[] expectedProperties)
        {
            Assert.IsTrue(CreateFirstSliceWorldBlockVisualStateBridge.TryResolve(state, out BlockStatePropertyValue[] actualProperties));
            CollectionAssert.AreEqual(expectedProperties, actualProperties);
        }

        private static MinecraftBlockStateLoader CreateLoader()
        {
            return new MinecraftBlockStateLoader(CreatePrivateAssetProjectPaths.GetReferenceRepositoryRoot(GetProjectRoot()));
        }

        private static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }
    }
}
