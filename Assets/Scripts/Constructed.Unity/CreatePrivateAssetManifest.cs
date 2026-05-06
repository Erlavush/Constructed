using System;
using System.Collections.Generic;
using Constructed.Core;

namespace Constructed.Unity
{
    public enum CreateVisualAssetKind
    {
        Item,
        Block
    }

    public sealed class CreatePrivateAssetFileReference : IEquatable<CreatePrivateAssetFileReference>
    {
        public const string MainResourcesPrefix = "src/main/resources/assets/create/";
        public const string GeneratedResourcesPrefix = "src/generated/resources/assets/create/";

        public CreatePrivateAssetFileReference(string repositoryRelativePath)
        {
            if (string.IsNullOrWhiteSpace(repositoryRelativePath))
                throw new ArgumentException("Repository-relative asset path cannot be empty.", nameof(repositoryRelativePath));
            if (System.IO.Path.IsPathRooted(repositoryRelativePath))
                throw new ArgumentException("Repository-relative asset path cannot be absolute.", nameof(repositoryRelativePath));

            string normalizedPath = repositoryRelativePath.Replace('\\', '/');
            if (normalizedPath.StartsWith("/", StringComparison.Ordinal))
                throw new ArgumentException("Repository-relative asset path cannot start from the root.", nameof(repositoryRelativePath));
            if (normalizedPath.Contains("/../", StringComparison.Ordinal) ||
                normalizedPath.Contains("../", StringComparison.Ordinal) ||
                normalizedPath.EndsWith("/..", StringComparison.Ordinal))
                throw new ArgumentException("Repository-relative asset path cannot escape the Create reference root.", nameof(repositoryRelativePath));
            if (!normalizedPath.StartsWith(MainResourcesPrefix, StringComparison.Ordinal) &&
                !normalizedPath.StartsWith(GeneratedResourcesPrefix, StringComparison.Ordinal))
                throw new ArgumentException("Create asset paths must stay inside src/main/resources or src/generated/resources assets/create.", nameof(repositoryRelativePath));

            RepositoryRelativePath = normalizedPath;
        }

        public string RepositoryRelativePath { get; }

        public string PrivateRelativePath
        {
            get { return RepositoryRelativePath; }
        }

        public override string ToString()
        {
            return RepositoryRelativePath;
        }

        public bool Equals(CreatePrivateAssetFileReference other)
        {
            return !ReferenceEquals(other, null) &&
                string.Equals(RepositoryRelativePath, other.RepositoryRelativePath, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is CreatePrivateAssetFileReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(RepositoryRelativePath);
        }
    }

    public sealed class CreateVisualAssetTarget
    {
        private readonly List<CreatePrivateAssetFileReference> files;

        public CreateVisualAssetTarget(
            ResourceLocation resourceId,
            CreateVisualAssetKind visualKind,
            string catalogLabel,
            IEnumerable<CreatePrivateAssetFileReference> files)
        {
            if (string.IsNullOrEmpty(resourceId.Namespace) || string.IsNullOrEmpty(resourceId.Path))
                throw new ArgumentException("Create visual asset target id must be initialized.", nameof(resourceId));
            if (string.IsNullOrWhiteSpace(catalogLabel))
                throw new ArgumentException("Create visual asset target label cannot be empty.", nameof(catalogLabel));
            if (files == null)
                throw new ArgumentNullException(nameof(files));

            ResourceId = resourceId;
            VisualKind = visualKind;
            CatalogLabel = catalogLabel;
            this.files = new List<CreatePrivateAssetFileReference>();

            HashSet<string> uniquePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (CreatePrivateAssetFileReference file in files)
            {
                if (file == null)
                    throw new ArgumentException("Create visual asset target files cannot contain null entries.", nameof(files));
                if (!uniquePaths.Add(file.RepositoryRelativePath))
                    continue;

                this.files.Add(file);
            }

            if (this.files.Count == 0)
                throw new ArgumentException("Create visual asset target must contain at least one file.", nameof(files));
        }

        public ResourceLocation ResourceId { get; }

        public CreateVisualAssetKind VisualKind { get; }

        public string CatalogLabel { get; }

        public IReadOnlyList<CreatePrivateAssetFileReference> Files
        {
            get { return files; }
        }
    }

    public sealed class CreatePrivateAssetManifest
    {
        private readonly List<CreateVisualAssetTarget> targets;
        private readonly Dictionary<string, CreateVisualAssetTarget> targetsByKey;
        private readonly List<CreatePrivateAssetFileReference> uniqueFiles;

        public CreatePrivateAssetManifest(IEnumerable<CreateVisualAssetTarget> targets)
        {
            if (targets == null)
                throw new ArgumentNullException(nameof(targets));

            this.targets = new List<CreateVisualAssetTarget>();
            targetsByKey = new Dictionary<string, CreateVisualAssetTarget>(StringComparer.Ordinal);
            uniqueFiles = new List<CreatePrivateAssetFileReference>();
            Dictionary<string, CreatePrivateAssetFileReference> uniqueFilesByPath =
                new Dictionary<string, CreatePrivateAssetFileReference>(StringComparer.OrdinalIgnoreCase);

            foreach (CreateVisualAssetTarget target in targets)
            {
                if (target == null)
                    throw new ArgumentException("Create private asset manifest targets cannot contain null entries.", nameof(targets));

                string key = CreateKey(target.ResourceId, target.VisualKind);
                if (targetsByKey.ContainsKey(key))
                    throw new ArgumentException($"Create private asset manifest already contains {target.VisualKind} target {target.ResourceId}.", nameof(targets));

                targetsByKey.Add(key, target);
                this.targets.Add(target);

                foreach (CreatePrivateAssetFileReference file in target.Files)
                {
                    if (uniqueFilesByPath.ContainsKey(file.RepositoryRelativePath))
                        continue;

                    uniqueFilesByPath.Add(file.RepositoryRelativePath, file);
                    uniqueFiles.Add(file);
                }
            }

            if (this.targets.Count == 0)
                throw new ArgumentException("Create private asset manifest must contain at least one target.", nameof(targets));
        }

        public IReadOnlyList<CreateVisualAssetTarget> Targets
        {
            get { return targets; }
        }

        public IReadOnlyList<CreatePrivateAssetFileReference> UniqueFiles
        {
            get { return uniqueFiles; }
        }

        public CreateVisualAssetTarget GetTarget(ResourceLocation resourceId, CreateVisualAssetKind visualKind)
        {
            if (!TryGetTarget(resourceId, visualKind, out CreateVisualAssetTarget target))
                throw new KeyNotFoundException($"Create private asset manifest does not contain {visualKind} target {resourceId}.");

            return target;
        }

        public bool TryGetTarget(ResourceLocation resourceId, CreateVisualAssetKind visualKind, out CreateVisualAssetTarget target)
        {
            return targetsByKey.TryGetValue(CreateKey(resourceId, visualKind), out target);
        }

        private static string CreateKey(ResourceLocation resourceId, CreateVisualAssetKind visualKind)
        {
            return visualKind + ":" + resourceId;
        }
    }

    public static class CreateFirstSlicePrivateAssetManifest
    {
        public static readonly ResourceLocation[] ItemCatalogIds =
        {
            ResourceLocation.Parse("create:andesite_alloy"),
            ResourceLocation.Parse("create:belt_connector"),
            ResourceLocation.Parse("create:shaft"),
            ResourceLocation.Parse("create:creative_motor"),
            ResourceLocation.Parse("create:creative_crate"),
            ResourceLocation.Parse("create:brass_funnel"),
            ResourceLocation.Parse("create:item_vault")
        };

        public static readonly ResourceLocation[] BlockCatalogIds =
        {
            ResourceLocation.Parse("create:shaft"),
            ResourceLocation.Parse("create:belt"),
            ResourceLocation.Parse("create:creative_motor"),
            ResourceLocation.Parse("create:creative_crate"),
            ResourceLocation.Parse("create:brass_funnel"),
            ResourceLocation.Parse("create:item_vault")
        };

        public static readonly CreatePrivateAssetManifest Manifest = Build();

        private static CreatePrivateAssetManifest Build()
        {
            return new CreatePrivateAssetManifest(new[]
            {
                Item("create:andesite_alloy", "Andesite Alloy",
                    Generated("models/item/andesite_alloy.json"),
                    Main("textures/item/andesite_alloy.png")),

                Item("create:belt_connector", "Belt Connector",
                    Generated("models/item/belt_connector.json"),
                    Main("textures/item/belt_connector.png")),

                Item("create:shaft", "Shaft",
                    Generated("models/item/shaft.json"),
                    Main("models/block/shaft.json"),
                    Main("textures/block/axis.png"),
                    Main("textures/block/axis_top.png")),

                Item("create:creative_motor", "Creative Motor",
                    Generated("models/item/creative_motor.json"),
                    Main("models/block/creative_motor/item.json"),
                    Main("textures/block/axis.png"),
                    Main("textures/block/creative_casing.png"),
                    Main("textures/block/creative_motor.png"),
                    Main("textures/block/flap_display_front.png")),

                Item("create:creative_crate", "Creative Crate",
                    Generated("models/item/creative_crate.json"),
                    Generated("models/block/crate/creative/single.json"),
                    Main("models/block/crate/single.json"),
                    Main("textures/block/creative_casing.png"),
                    Main("textures/block/crate_creative.png"),
                    Main("textures/block/crate_creative_side.png")),

                Item("create:brass_funnel", "Brass Funnel",
                    Generated("models/item/brass_funnel.json"),
                    Main("models/block/funnel/item.json"),
                    Main("textures/block/brass_block.png"),
                    Main("textures/block/funnel/brass_funnel.png"),
                    Main("textures/block/funnel/brass_funnel_neutral.png"),
                    Main("textures/block/funnel/brass_funnel_unpowered.png"),
                    Main("textures/block/funnel/funnel_back.png")),

                Item("create:item_vault", "Item Vault",
                    Generated("models/item/item_vault.json"),
                    Main("models/block/item_vault.json"),
                    Main("textures/block/vault/vault_bottom_small.png"),
                    Main("textures/block/vault/vault_front_small.png"),
                    Main("textures/block/vault/vault_side_small.png"),
                    Main("textures/block/vault/vault_top_small.png")),

                Block("create:shaft", "Shaft Block",
                    Generated("blockstates/shaft.json"),
                    Main("models/block/shaft.json"),
                    Main("textures/block/axis.png"),
                    Main("textures/block/axis_top.png")),

                Block("create:belt", "Belt Block",
                    BeltBlockFiles()),

                Block("create:creative_motor", "Creative Motor Block",
                    Generated("blockstates/creative_motor.json"),
                    Main("models/block/creative_motor/block.json"),
                    Main("models/block/creative_motor/block_vertical.json"),
                    Main("textures/block/axis.png"),
                    Main("textures/block/axis_top.png"),
                    Main("textures/block/creative_casing.png"),
                    Main("textures/block/creative_motor.png"),
                    Main("textures/block/flap_display_front.png")),

                Block("create:creative_crate", "Creative Crate Block",
                    Generated("blockstates/creative_crate.json"),
                    Generated("models/block/crate/creative/single.json"),
                    Main("models/block/crate/single.json"),
                    Main("textures/block/creative_casing.png"),
                    Main("textures/block/crate_creative.png"),
                    Main("textures/block/crate_creative_side.png")),

                Block("create:brass_funnel", "Brass Funnel Block",
                    BrassFunnelBlockFiles()),

                Block("create:item_vault", "Item Vault Block",
                    Generated("blockstates/item_vault.json"),
                    Main("models/block/item_vault.json"),
                    Main("textures/block/vault/vault_bottom_small.png"),
                    Main("textures/block/vault/vault_front_small.png"),
                    Main("textures/block/vault/vault_side_small.png"),
                    Main("textures/block/vault/vault_top_small.png"))
            });
        }

        private static IEnumerable<CreatePrivateAssetFileReference> BeltBlockFiles()
        {
            yield return Generated("blockstates/belt.json");
            yield return Main("models/block/belt/particle.json");
            yield return Main("models/block/belt/start.json");
            yield return Main("models/block/belt/start_bottom.json");
            yield return Main("models/block/belt/middle.json");
            yield return Main("models/block/belt/middle_bottom.json");
            yield return Main("models/block/belt/end.json");
            yield return Main("models/block/belt/end_bottom.json");
            yield return Main("models/block/belt/diagonal_start.json");
            yield return Main("models/block/belt/diagonal_middle.json");
            yield return Main("models/block/belt/diagonal_end.json");
            yield return Main("models/block/belt_casing/horizontal_start.json");
            yield return Main("models/block/belt_casing/horizontal_middle.json");
            yield return Main("models/block/belt_casing/horizontal_end.json");
            yield return Main("models/block/belt_casing/horizontal_pulley.json");
            yield return Main("models/block/belt_casing/diagonal_start.json");
            yield return Main("models/block/belt_casing/diagonal_middle.json");
            yield return Main("models/block/belt_casing/diagonal_end.json");
            yield return Main("models/block/belt_casing/diagonal_pulley.json");
            yield return Main("models/block/belt_casing/sideways_start.json");
            yield return Main("models/block/belt_casing/sideways_middle.json");
            yield return Main("models/block/belt_casing/sideways_end.json");
            yield return Main("models/block/belt_casing/sideways_pulley.json");
            yield return Main("textures/block/belt.png");
            yield return Main("textures/block/belt_offset.png");
            yield return Main("textures/block/belt_diagonal.png");
            yield return Main("textures/block/belt_scroll.png");
            yield return Main("textures/block/belt_diagonal_scroll.png");
            yield return Main("textures/block/andesite_belt_cover.png");
            yield return Main("textures/block/brass_belt_cover.png");
            yield return Main("textures/block/belt/andesite_belt_casing.png");
            yield return Main("textures/block/belt/brass_belt_casing.png");
        }

        private static IEnumerable<CreatePrivateAssetFileReference> BrassFunnelBlockFiles()
        {
            yield return Generated("blockstates/brass_funnel.json");
            yield return Generated("models/block/brass_funnel_horizontal_pull_unpowered.json");
            yield return Generated("models/block/brass_funnel_horizontal_pull_powered.json");
            yield return Generated("models/block/brass_funnel_horizontal_push_unpowered.json");
            yield return Generated("models/block/brass_funnel_horizontal_push_powered.json");
            yield return Generated("models/block/brass_funnel_vertical_pull_unpowered.json");
            yield return Generated("models/block/brass_funnel_vertical_pull_powered.json");
            yield return Generated("models/block/brass_funnel_vertical_push_unpowered.json");
            yield return Generated("models/block/brass_funnel_vertical_push_powered.json");
            yield return Main("models/block/funnel/block_horizontal.json");
            yield return Main("models/block/funnel/block_vertical.json");
            yield return Main("textures/block/brass_block.png");
            yield return Main("textures/block/funnel/brass_funnel.png");
            yield return Main("textures/block/funnel/brass_funnel_frame.png");
            yield return Main("textures/block/funnel/brass_funnel_pull.png");
            yield return Main("textures/block/funnel/brass_funnel_push.png");
            yield return Main("textures/block/funnel/brass_funnel_unpowered.png");
            yield return Main("textures/block/funnel/brass_funnel_powered.png");
            yield return Main("textures/block/funnel/funnel_back.png");
            yield return Main("textures/block/funnel/funnel_open.png");
            yield return Main("textures/block/funnel/funnel_closed.png");
        }

        private static CreateVisualAssetTarget Item(
            string resourceId,
            string catalogLabel,
            params CreatePrivateAssetFileReference[] files)
        {
            return new CreateVisualAssetTarget(ResourceLocation.Parse(resourceId), CreateVisualAssetKind.Item, catalogLabel, files);
        }

        private static CreateVisualAssetTarget Block(
            string resourceId,
            string catalogLabel,
            params CreatePrivateAssetFileReference[] files)
        {
            return new CreateVisualAssetTarget(ResourceLocation.Parse(resourceId), CreateVisualAssetKind.Block, catalogLabel, files);
        }

        private static CreateVisualAssetTarget Block(
            string resourceId,
            string catalogLabel,
            IEnumerable<CreatePrivateAssetFileReference> files)
        {
            return new CreateVisualAssetTarget(ResourceLocation.Parse(resourceId), CreateVisualAssetKind.Block, catalogLabel, files);
        }

        private static CreatePrivateAssetFileReference Main(string relativePath)
        {
            return new CreatePrivateAssetFileReference(CreatePrivateAssetFileReference.MainResourcesPrefix + relativePath);
        }

        private static CreatePrivateAssetFileReference Generated(string relativePath)
        {
            return new CreatePrivateAssetFileReference(CreatePrivateAssetFileReference.GeneratedResourcesPrefix + relativePath);
        }
    }
}
