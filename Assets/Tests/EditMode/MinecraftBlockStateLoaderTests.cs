using System.IO;
using Constructed.Core;
using Constructed.Minecraft;
using Constructed.Unity;
using NUnit.Framework;
using UnityEngine;

namespace Constructed.Tests
{
    public sealed class MinecraftBlockStateLoaderTests
    {
        [Test]
        public void ShaftVariantsResolveToSharedModelWithAxisRotations()
        {
            MinecraftBlockStateDefinition definition = CreateLoader().LoadBlockState(ResourceLocation.Parse("create:shaft"));

            AssertVariant(definition, "axis=x", "create:block/shaft", 90, 90);
            AssertVariant(definition, "axis=y", "create:block/shaft", 0, 0);
            AssertVariant(definition, "axis=z", "create:block/shaft", 90, 180);
        }

        [Test]
        public void CogwheelVariantsResolveToAxisRotatedModels()
        {
            MinecraftBlockStateDefinition cogwheel = CreateLoader().LoadBlockState(ResourceLocation.Parse("create:cogwheel"));
            MinecraftBlockStateDefinition largeCogwheel = CreateLoader().LoadBlockState(ResourceLocation.Parse("create:large_cogwheel"));

            AssertVariant(cogwheel, "axis=x", "create:block/cogwheel", 90, 90);
            AssertVariant(cogwheel, "axis=y", "create:block/cogwheel", 0, 0);
            AssertVariant(cogwheel, "axis=z", "create:block/cogwheel", 90, 180);
            AssertVariant(largeCogwheel, "axis=x", "create:block/large_cogwheel", 90, 90);
            AssertVariant(largeCogwheel, "axis=y", "create:block/large_cogwheel", 0, 0);
            AssertVariant(largeCogwheel, "axis=z", "create:block/large_cogwheel", 90, 180);
        }

        [Test]
        public void GearboxVariantsResolveWithSourceUvLockFlag()
        {
            MinecraftBlockStateDefinition definition = CreateLoader().LoadBlockState(ResourceLocation.Parse("create:gearbox"));

            AssertVariant(definition, "axis=x", "create:block/gearbox/block", 90, 90, true);
            AssertVariant(definition, "axis=y", "create:block/gearbox/block", 0, 0, true);
            AssertVariant(definition, "axis=z", "create:block/gearbox/block", 90, 180, true);
        }

        [Test]
        public void CreativeMotorVariantsResolveHorizontalAndVerticalModels()
        {
            MinecraftBlockStateDefinition definition = CreateLoader().LoadBlockState(ResourceLocation.Parse("create:creative_motor"));

            AssertVariant(definition, "facing=east", "create:block/creative_motor/block", 0, 270);
            AssertVariant(definition, "facing=up", "create:block/creative_motor/block_vertical", 0, 0);
            AssertVariant(definition, "facing=down", "create:block/creative_motor/block_vertical", 180, 0);
        }

        [Test]
        public void CreativeCrateVariantsResolveToSharedSingleModel()
        {
            MinecraftBlockStateDefinition definition = CreateLoader().LoadBlockState(ResourceLocation.Parse("create:creative_crate"));

            AssertVariant(definition, "facing=down", "create:block/crate/creative/single", 0, 0);
            AssertVariant(definition, "facing=west", "create:block/crate/creative/single", 0, 0);
        }

        [Test]
        public void BrassFunnelVariantsResolvePullAndPushModels()
        {
            MinecraftBlockStateDefinition definition = CreateLoader().LoadBlockState(ResourceLocation.Parse("create:brass_funnel"));

            AssertVariant(
                definition,
                "extracting=false,facing=east,powered=false,waterlogged=false",
                "create:block/brass_funnel_horizontal_pull_unpowered",
                0,
                90);
            AssertVariant(
                definition,
                "extracting=true,facing=east,powered=true,waterlogged=false",
                "create:block/brass_funnel_horizontal_push_powered",
                0,
                90);
            AssertVariant(
                definition,
                "extracting=false,facing=up,powered=false,waterlogged=false",
                "create:block/brass_funnel_vertical_pull_unpowered",
                0,
                180);
        }

        [Test]
        public void ItemVaultVariantsResolveAxisRotation()
        {
            MinecraftBlockStateDefinition definition = CreateLoader().LoadBlockState(ResourceLocation.Parse("create:item_vault"));

            AssertVariant(definition, "axis=x,large=false", "create:block/item_vault", 0, 90);
            AssertVariant(definition, "axis=z,large=true", "create:block/item_vault", 0, 0);
        }

        [Test]
        public void BlockCatalogEntriesResolveAgainstFirstSliceBlockstates()
        {
            MinecraftBlockStateLoader loader = CreateLoader();
            foreach (CreateBlockVisualCatalogEntry entry in CreateFirstSliceBlockVisualCatalog.Entries)
            {
                MinecraftBlockStateDefinition definition = loader.LoadBlockState(entry.BlockId);
                Assert.DoesNotThrow(() => definition.ResolveVariant(entry.PreviewProperties));
            }
        }

        private static MinecraftBlockStateLoader CreateLoader()
        {
            return new MinecraftBlockStateLoader(CreatePrivateAssetProjectPaths.GetReferenceRepositoryRoot(GetProjectRoot()));
        }

        private static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static void AssertVariant(
            MinecraftBlockStateDefinition definition,
            string variantProperties,
            string expectedModel,
            int expectedXRotation,
            int expectedYRotation,
            bool expectedUvLock = false)
        {
            MinecraftBlockStateVariant variant = definition.ResolveVariant(ParseProperties(variantProperties));

            Assert.AreEqual(ResourceLocation.Parse(expectedModel), variant.ModelId);
            Assert.AreEqual(expectedXRotation, variant.XRotationDegrees);
            Assert.AreEqual(expectedYRotation, variant.YRotationDegrees);
            Assert.AreEqual(expectedUvLock, variant.UvLock);
        }

        private static BlockStatePropertyValue[] ParseProperties(string properties)
        {
            string[] parts = properties.Split(',');
            BlockStatePropertyValue[] values = new BlockStatePropertyValue[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                string[] pair = parts[i].Split('=');
                values[i] = new BlockStatePropertyValue(pair[0], pair[1]);
            }

            return values;
        }
    }
}
