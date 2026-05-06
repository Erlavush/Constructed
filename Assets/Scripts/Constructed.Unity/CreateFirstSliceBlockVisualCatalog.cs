using System;
using System.Collections.Generic;
using Constructed.Core;
using Constructed.Minecraft;

namespace Constructed.Unity
{
    public sealed class CreateBlockVisualCatalogEntry
    {
        private readonly BlockStatePropertyValue[] previewProperties;

        public CreateBlockVisualCatalogEntry(
            ResourceLocation blockId,
            string label,
            IEnumerable<BlockStatePropertyValue> previewProperties)
        {
            if (string.IsNullOrEmpty(blockId.Namespace) || string.IsNullOrEmpty(blockId.Path))
                throw new ArgumentException("Create block catalog id must be initialized.", nameof(blockId));
            if (string.IsNullOrWhiteSpace(label))
                throw new ArgumentException("Create block catalog label cannot be empty.", nameof(label));
            if (previewProperties == null)
                throw new ArgumentNullException(nameof(previewProperties));

            List<BlockStatePropertyValue> properties = new List<BlockStatePropertyValue>();
            foreach (BlockStatePropertyValue property in previewProperties)
                properties.Add(property);
            if (properties.Count == 0)
                throw new ArgumentException("Create block catalog entries must define at least one preview property.", nameof(previewProperties));

            BlockId = blockId;
            Label = label;
            this.previewProperties = properties.ToArray();
        }

        public ResourceLocation BlockId { get; }

        public string Label { get; }

        public IReadOnlyList<BlockStatePropertyValue> PreviewProperties
        {
            get { return previewProperties; }
        }
    }

    public static class CreateFirstSliceBlockVisualCatalog
    {
        private static readonly CreateBlockVisualCatalogEntry[] entries =
        {
            Entry("create:shaft", "Shaft", "axis=x"),
            Entry("create:creative_motor", "Creative Motor", "facing=east"),
            Entry("create:creative_crate", "Creative Crate", "facing=down"),
            Entry("create:brass_funnel", "Brass Funnel", "extracting=false", "facing=east", "powered=false", "waterlogged=false"),
            Entry("create:item_vault", "Item Vault", "axis=x", "large=false")
        };

        private static readonly Dictionary<ResourceLocation, CreateBlockVisualCatalogEntry> entriesById = BuildEntriesById();

        public static IReadOnlyList<CreateBlockVisualCatalogEntry> Entries
        {
            get { return entries; }
        }

        public static CreateBlockVisualCatalogEntry GetEntry(ResourceLocation blockId)
        {
            if (!TryGetEntry(blockId, out CreateBlockVisualCatalogEntry entry))
                throw new KeyNotFoundException("Create first-slice block catalog does not contain " + blockId + ".");

            return entry;
        }

        public static bool TryGetEntry(ResourceLocation blockId, out CreateBlockVisualCatalogEntry entry)
        {
            return entriesById.TryGetValue(blockId, out entry);
        }

        private static Dictionary<ResourceLocation, CreateBlockVisualCatalogEntry> BuildEntriesById()
        {
            Dictionary<ResourceLocation, CreateBlockVisualCatalogEntry> byId =
                new Dictionary<ResourceLocation, CreateBlockVisualCatalogEntry>();
            foreach (CreateBlockVisualCatalogEntry entry in entries)
                byId.Add(entry.BlockId, entry);

            return byId;
        }

        private static CreateBlockVisualCatalogEntry Entry(string blockId, string label, params string[] previewProperties)
        {
            BlockStatePropertyValue[] properties = new BlockStatePropertyValue[previewProperties.Length];
            for (int i = 0; i < previewProperties.Length; i++)
            {
                string[] parts = previewProperties[i].Split('=');
                properties[i] = new BlockStatePropertyValue(parts[0], parts[1]);
            }

            return new CreateBlockVisualCatalogEntry(ResourceLocation.Parse(blockId), label, properties);
        }
    }
}
