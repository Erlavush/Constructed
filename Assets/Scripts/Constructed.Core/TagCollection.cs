using System;
using System.Collections.Generic;

namespace Constructed.Core
{
    public sealed class TagCollection<T>
    {
        private readonly Dictionary<TagKey<T>, List<ResourceLocation>> idsByTag;

        public TagCollection(Registry<T> registry)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));

            Registry = registry;
            idsByTag = new Dictionary<TagKey<T>, List<ResourceLocation>>();
        }

        public Registry<T> Registry { get; }

        public void Add(TagKey<T> tag, ResourceLocation id)
        {
            EnsureInitialized(id, nameof(id));

            List<ResourceLocation> ids = GetOrCreate(tag);
            if (!ids.Contains(id))
                ids.Add(id);
        }

        public void Add(TagKey<T> tag, T value)
        {
            Add(tag, Registry.GetId(value));
        }

        public void Replace(TagKey<T> tag, IEnumerable<ResourceLocation> ids)
        {
            EnsureTagUsesRegistry(tag);
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            List<ResourceLocation> uniqueIds = new List<ResourceLocation>();
            HashSet<ResourceLocation> seen = new HashSet<ResourceLocation>();
            foreach (ResourceLocation id in ids)
            {
                EnsureInitialized(id, nameof(ids));
                if (seen.Add(id))
                    uniqueIds.Add(id);
            }

            idsByTag[tag] = uniqueIds;
        }

        public bool Contains(TagKey<T> tag, ResourceLocation id)
        {
            EnsureTagUsesRegistry(tag);

            List<ResourceLocation> ids;
            return idsByTag.TryGetValue(tag, out ids) && ids.Contains(id);
        }

        public bool Contains(TagKey<T> tag, T value)
        {
            ResourceLocation id;
            return Registry.TryGetId(value, out id) && Contains(tag, id);
        }

        public IReadOnlyList<ResourceLocation> GetIds(TagKey<T> tag)
        {
            EnsureTagUsesRegistry(tag);

            List<ResourceLocation> ids;
            if (!idsByTag.TryGetValue(tag, out ids))
                return Array.Empty<ResourceLocation>();

            return new List<ResourceLocation>(ids);
        }

        public IReadOnlyList<T> GetValues(TagKey<T> tag)
        {
            EnsureTagUsesRegistry(tag);

            List<T> values = new List<T>();
            List<ResourceLocation> ids;
            if (!idsByTag.TryGetValue(tag, out ids))
                return values;

            foreach (ResourceLocation id in ids)
            {
                T value;
                if (Registry.TryGetValue(id, out value))
                    values.Add(value);
            }

            return values;
        }

        private List<ResourceLocation> GetOrCreate(TagKey<T> tag)
        {
            EnsureTagUsesRegistry(tag);

            List<ResourceLocation> ids;
            if (!idsByTag.TryGetValue(tag, out ids))
            {
                ids = new List<ResourceLocation>();
                idsByTag.Add(tag, ids);
            }

            return ids;
        }

        private void EnsureTagUsesRegistry(TagKey<T> tag)
        {
            if (tag.RegistryId != Registry.RegistryId)
                throw new ArgumentException($"Tag {tag} belongs to registry {tag.RegistryId}, not {Registry.RegistryId}.", nameof(tag));
        }

        private static void EnsureInitialized(ResourceLocation id, string parameterName)
        {
            if (string.IsNullOrEmpty(id.Namespace) || string.IsNullOrEmpty(id.Path))
                throw new ArgumentException("Resource id must be initialized.", parameterName);
        }
    }
}
