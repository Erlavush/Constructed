using System;
using System.Collections.Generic;
using Constructed.Core;

namespace Constructed.Unity
{
    public sealed class CreateItemVisualCatalogEntry
    {
        public CreateItemVisualCatalogEntry(
            ResourceLocation itemId,
            string label,
            CreatePrivateAssetFileReference previewTextureFile)
        {
            if (string.IsNullOrEmpty(itemId.Namespace) || string.IsNullOrEmpty(itemId.Path))
                throw new ArgumentException("Create item catalog id must be initialized.", nameof(itemId));
            if (string.IsNullOrWhiteSpace(label))
                throw new ArgumentException("Create item catalog label cannot be empty.", nameof(label));
            if (previewTextureFile == null)
                throw new ArgumentNullException(nameof(previewTextureFile));

            ItemId = itemId;
            Label = label;
            PreviewTextureFile = previewTextureFile;
        }

        public ResourceLocation ItemId { get; }

        public string Label { get; }

        public CreatePrivateAssetFileReference PreviewTextureFile { get; }
    }

    public static class CreateFirstSliceItemVisualCatalog
    {
        private static readonly CreateItemVisualCatalogEntry[] entries =
        {
            Entry("create:andesite_alloy", "Andesite Alloy", "textures/item/andesite_alloy.png"),
            Entry("create:belt_connector", "Belt Connector", "textures/item/belt_connector.png"),
            Entry("create:shaft", "Shaft", "textures/block/axis_top.png"),
            Entry("create:creative_motor", "Creative Motor", "textures/block/creative_motor.png"),
            Entry("create:creative_crate", "Creative Crate", "textures/block/crate_creative.png"),
            Entry("create:brass_funnel", "Brass Funnel", "textures/block/funnel/brass_funnel.png"),
            Entry("create:item_vault", "Item Vault", "textures/block/vault/vault_front_small.png")
        };

        private static readonly Dictionary<ResourceLocation, CreateItemVisualCatalogEntry> entriesById = BuildEntriesById();

        public static IReadOnlyList<CreateItemVisualCatalogEntry> Entries
        {
            get { return entries; }
        }

        public static CreateItemVisualCatalogEntry GetEntry(ResourceLocation itemId)
        {
            if (!TryGetEntry(itemId, out CreateItemVisualCatalogEntry entry))
                throw new KeyNotFoundException($"Create first-slice item catalog does not contain {itemId}.");

            return entry;
        }

        public static bool TryGetEntry(ResourceLocation itemId, out CreateItemVisualCatalogEntry entry)
        {
            return entriesById.TryGetValue(itemId, out entry);
        }

        private static Dictionary<ResourceLocation, CreateItemVisualCatalogEntry> BuildEntriesById()
        {
            Dictionary<ResourceLocation, CreateItemVisualCatalogEntry> byId =
                new Dictionary<ResourceLocation, CreateItemVisualCatalogEntry>();
            foreach (CreateItemVisualCatalogEntry entry in entries)
                byId.Add(entry.ItemId, entry);

            return byId;
        }

        private static CreateItemVisualCatalogEntry Entry(string itemId, string label, string mainResourceRelativePath)
        {
            return new CreateItemVisualCatalogEntry(
                ResourceLocation.Parse(itemId),
                label,
                new CreatePrivateAssetFileReference(CreatePrivateAssetFileReference.MainResourcesPrefix + mainResourceRelativePath));
        }
    }
}
