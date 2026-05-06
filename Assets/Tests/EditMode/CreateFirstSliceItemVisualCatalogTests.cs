using Constructed.Core;
using Constructed.Unity;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class CreateFirstSliceItemVisualCatalogTests
    {
        [Test]
        public void ItemCatalogContainsExpectedFirstSliceEntriesInPreviewOrder()
        {
            Assert.AreEqual(7, CreateFirstSliceItemVisualCatalog.Entries.Count);

            CollectionAssert.AreEqual(
                CreateFirstSlicePrivateAssetManifest.ItemCatalogIds,
                GetItemIds());
        }

        [Test]
        public void EveryItemCatalogPreviewTextureIsCoveredByManifestTarget()
        {
            foreach (CreateItemVisualCatalogEntry entry in CreateFirstSliceItemVisualCatalog.Entries)
            {
                CreateVisualAssetTarget target =
                    CreateFirstSlicePrivateAssetManifest.Manifest.GetTarget(entry.ItemId, CreateVisualAssetKind.Item);

                bool found = false;
                foreach (CreatePrivateAssetFileReference file in target.Files)
                {
                    if (!file.Equals(entry.PreviewTextureFile))
                        continue;

                    found = true;
                    break;
                }

                Assert.IsTrue(found, $"Missing preview texture mapping for {entry.ItemId}.");
            }
        }

        private static ResourceLocation[] GetItemIds()
        {
            ResourceLocation[] ids = new ResourceLocation[CreateFirstSliceItemVisualCatalog.Entries.Count];
            for (int i = 0; i < ids.Length; i++)
                ids[i] = CreateFirstSliceItemVisualCatalog.Entries[i].ItemId;

            return ids;
        }
    }
}
