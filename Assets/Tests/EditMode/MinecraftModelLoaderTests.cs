using System.IO;
using Constructed.Core;
using Constructed.Unity;
using NUnit.Framework;
using UnityEngine;

namespace Constructed.Tests
{
    public sealed class MinecraftModelLoaderTests
    {
        [Test]
        public void GeneratedItemModelResolvesLayerTextureWithoutElements()
        {
            MinecraftResolvedModel model = CreateLoader().LoadItemModel(ResourceLocation.Parse("create:andesite_alloy"));

            Assert.IsTrue(model.UsesGeneratedItemLayers);
            Assert.IsFalse(model.UsesBlockLight);
            Assert.AreEqual(0, model.Elements.Count);
            CollectionAssert.AreEqual(
                new[] { ResourceLocation.Parse("create:item/andesite_alloy") },
                model.GeneratedItemTextureIds);
            AssertResolvedTexture(model, "layer0", "create:item/andesite_alloy");
        }

        [Test]
        public void ShaftItemModelInheritsParentElementsAndTextures()
        {
            MinecraftResolvedModel model = CreateLoader().LoadItemModel(ResourceLocation.Parse("create:shaft"));

            Assert.IsFalse(model.UsesGeneratedItemLayers);
            Assert.IsTrue(model.UsesBlockLight);
            Assert.AreEqual(1, model.Elements.Count);
            AssertVector2Equals(new Vector2(16f, 16f), model.TextureSize);
            AssertResolvedTexture(model, "0", "create:block/axis");
            AssertResolvedTexture(model, "1", "create:block/axis_top");
            Assert.AreEqual(
                ResourceLocation.Parse("create:block/axis_top"),
                model.Elements[0].Faces[Direction.Up].TextureId);
        }

        [Test]
        public void CogwheelItemModelsInheritSourceElementsAndMinecraftParticles()
        {
            MinecraftResolvedModel cogwheel = CreateLoader().LoadItemModel(ResourceLocation.Parse("create:cogwheel"));
            MinecraftResolvedModel largeCogwheel = CreateLoader().LoadItemModel(ResourceLocation.Parse("create:large_cogwheel"));

            Assert.IsFalse(cogwheel.UsesGeneratedItemLayers);
            Assert.IsTrue(cogwheel.UsesBlockLight);
            Assert.IsTrue(cogwheel.HasGuiDisplay);
            AssertVector2Equals(new Vector2(32f, 32f), cogwheel.TextureSize);
            Assert.AreEqual(7, cogwheel.Elements.Count);
            AssertResolvedTexture(cogwheel, "1_2", "create:block/cogwheel");
            AssertResolvedTexture(cogwheel, "particle", "minecraft:block/stripped_spruce_log_top");

            Assert.IsFalse(largeCogwheel.UsesGeneratedItemLayers);
            Assert.IsTrue(largeCogwheel.UsesBlockLight);
            Assert.IsTrue(largeCogwheel.HasGuiDisplay);
            AssertVector2Equals(new Vector2(32f, 32f), largeCogwheel.TextureSize);
            Assert.AreEqual(16, largeCogwheel.Elements.Count);
            AssertResolvedTexture(largeCogwheel, "4", "create:block/large_cogwheel");
            AssertResolvedTexture(largeCogwheel, "particle", "minecraft:block/stripped_spruce_log");
        }

        [Test]
        public void GearboxItemModelUsesSourceItemBodyAndAxisTextures()
        {
            MinecraftResolvedModel model = CreateLoader().LoadItemModel(ResourceLocation.Parse("create:gearbox"));

            Assert.IsFalse(model.UsesGeneratedItemLayers);
            Assert.IsTrue(model.UsesBlockLight);
            Assert.AreEqual(5, model.Elements.Count);
            AssertResolvedTexture(model, "0", "create:block/andesite_casing");
            AssertResolvedTexture(model, "1", "create:block/gearbox");
            AssertResolvedTexture(model, "1_0", "create:block/axis");
            AssertResolvedTexture(model, "1_1", "create:block/axis_top");
        }

        [Test]
        public void CreativeMotorItemModelParsesGuiDisplayAndElementRotation()
        {
            MinecraftResolvedModel model = CreateLoader().LoadItemModel(ResourceLocation.Parse("create:creative_motor"));

            Assert.IsTrue(model.HasGuiDisplay);
            AssertVector3Equals(new Vector3(30f, 45f, 0f), model.GuiDisplay.Rotation);
            AssertVector3Equals(new Vector3(0.625f, 0.625f, 0.625f), model.GuiDisplay.Scale);
            Assert.AreEqual(13, model.Elements.Count);
            Assert.IsNotNull(model.Elements[0].Rotation);
            Assert.AreEqual(Axis.Z, model.Elements[0].Rotation.Axis);
            Assert.AreEqual(22.5f, model.Elements[0].Rotation.Angle);
            AssertResolvedTexture(model, "6", "create:block/creative_motor");
        }

        [Test]
        public void CreativeCrateItemModelResolvesChildTextureOverridesAcrossParentChain()
        {
            MinecraftResolvedModel model = CreateLoader().LoadItemModel(ResourceLocation.Parse("create:creative_crate"));

            Assert.AreEqual(1, model.Elements.Count);
            AssertResolvedTexture(model, "crate", "create:block/crate_creative");
            AssertResolvedTexture(model, "2", "create:block/crate_creative");
            AssertResolvedTexture(model, "particle", "create:block/creative_casing");
            Assert.AreEqual(
                ResourceLocation.Parse("create:block/crate_creative"),
                model.Elements[0].Faces[Direction.North].TextureId);
        }

        [Test]
        public void BrassFunnelItemModelUsesTextureOverridesAndTextureSize()
        {
            MinecraftResolvedModel model = CreateLoader().LoadItemModel(ResourceLocation.Parse("create:brass_funnel"));

            AssertVector2Equals(new Vector2(32f, 32f), model.TextureSize);
            Assert.IsTrue(model.HasGuiDisplay);
            AssertResolvedTexture(model, "base", "create:block/funnel/brass_funnel");
            AssertResolvedTexture(model, "redstone", "create:block/funnel/brass_funnel_unpowered");
            Assert.AreEqual(
                ResourceLocation.Parse("create:block/funnel/brass_funnel"),
                model.Elements[0].Faces[Direction.North].TextureId);
        }

        [Test]
        public void ExtractedMinecraftResourceMirrorLoadsGrassBlockItemModel()
        {
            MinecraftResolvedModel model = CreateVanillaResourceLoader().LoadItemModel(ResourceLocation.Parse("minecraft:grass_block"));

            Assert.IsFalse(model.UsesGeneratedItemLayers);
            Assert.IsTrue(model.UsesBlockLight);
            Assert.IsTrue(model.HasGuiDisplay);
            AssertVector3Equals(new Vector3(30f, 225f, 0f), model.GuiDisplay.Rotation);
            AssertVector3Equals(new Vector3(0.625f, 0.625f, 0.625f), model.GuiDisplay.Scale);
            Assert.AreEqual(2, model.Elements.Count);
            AssertResolvedTexture(model, "top", "minecraft:block/grass_block_top");
            AssertResolvedTexture(model, "side", "minecraft:block/grass_block_side");
            AssertResolvedTexture(model, "overlay", "minecraft:block/grass_block_side_overlay");
            AssertResolvedTexture(model, "bottom", "minecraft:block/dirt");
        }

        private static MinecraftModelLoader CreateLoader()
        {
            return new MinecraftModelLoader(CreatePrivateAssetProjectPaths.GetReferenceRepositoryRoot(GetProjectRoot()));
        }

        private static MinecraftModelLoader CreateVanillaResourceLoader()
        {
            return new MinecraftModelLoader(Path.Combine(GetProjectRoot(), "References/Minecraft-1.21.1-resources"));
        }

        private static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static void AssertResolvedTexture(MinecraftResolvedModel model, string key, string expectedTexture)
        {
            Assert.IsTrue(model.TryGetResolvedTexture(key, out ResourceLocation textureId), "Missing resolved texture key " + key + ".");
            Assert.AreEqual(ResourceLocation.Parse(expectedTexture), textureId);
        }

        private static void AssertVector2Equals(Vector2 expected, Vector2 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.y, 0.0001f);
        }

        private static void AssertVector3Equals(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.y, 0.0001f);
            Assert.AreEqual(expected.z, actual.z, 0.0001f);
        }
    }
}
