using Constructed.Core;
using Constructed.Minecraft;
using Constructed.Unity;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class CreateFirstSliceBlockVisualCatalogTests
    {
        [Test]
        public void BlockCatalogContainsExpectedFirstSliceEntriesInPreviewOrder()
        {
            Assert.AreEqual(5, CreateFirstSliceBlockVisualCatalog.Entries.Count);

            CollectionAssert.AreEqual(
                new[]
                {
                    ResourceLocation.Parse("create:shaft"),
                    ResourceLocation.Parse("create:creative_motor"),
                    ResourceLocation.Parse("create:creative_crate"),
                    ResourceLocation.Parse("create:brass_funnel"),
                    ResourceLocation.Parse("create:item_vault")
                },
                GetBlockIds());
        }

        [Test]
        public void EveryBlockCatalogEntryDefinesExactPreviewProperties()
        {
            CollectionAssert.AreEqual(
                new[]
                {
                    new BlockStatePropertyValue("axis", "x")
                },
                CreateFirstSliceBlockVisualCatalog.Entries[0].PreviewProperties);

            CollectionAssert.AreEqual(
                new[]
                {
                    new BlockStatePropertyValue("facing", "east")
                },
                CreateFirstSliceBlockVisualCatalog.Entries[1].PreviewProperties);

            CollectionAssert.AreEqual(
                new[]
                {
                    new BlockStatePropertyValue("facing", "down")
                },
                CreateFirstSliceBlockVisualCatalog.Entries[2].PreviewProperties);

            CollectionAssert.AreEqual(
                new[]
                {
                    new BlockStatePropertyValue("extracting", "false"),
                    new BlockStatePropertyValue("facing", "east"),
                    new BlockStatePropertyValue("powered", "false"),
                    new BlockStatePropertyValue("waterlogged", "false")
                },
                CreateFirstSliceBlockVisualCatalog.Entries[3].PreviewProperties);

            CollectionAssert.AreEqual(
                new[]
                {
                    new BlockStatePropertyValue("axis", "x"),
                    new BlockStatePropertyValue("large", "false")
                },
                CreateFirstSliceBlockVisualCatalog.Entries[4].PreviewProperties);
        }

        private static ResourceLocation[] GetBlockIds()
        {
            ResourceLocation[] ids = new ResourceLocation[CreateFirstSliceBlockVisualCatalog.Entries.Count];
            for (int i = 0; i < ids.Length; i++)
                ids[i] = CreateFirstSliceBlockVisualCatalog.Entries[i].BlockId;

            return ids;
        }
    }
}
