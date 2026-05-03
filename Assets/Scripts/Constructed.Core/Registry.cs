using System;
using System.Collections.Generic;

namespace Constructed.Core
{
    public sealed class Registry<T>
    {
        private readonly Dictionary<ResourceLocation, RegistryEntry<T>> entriesById;
        private readonly Dictionary<T, ResourceLocation> idsByValue;
        private readonly List<RegistryEntry<T>> entries;

        public Registry(ResourceLocation registryId)
        {
            if (string.IsNullOrEmpty(registryId.Namespace) || string.IsNullOrEmpty(registryId.Path))
                throw new ArgumentException("Registry id must be initialized.", nameof(registryId));

            RegistryId = registryId;
            entriesById = new Dictionary<ResourceLocation, RegistryEntry<T>>();
            idsByValue = new Dictionary<T, ResourceLocation>();
            entries = new List<RegistryEntry<T>>();
        }

        public ResourceLocation RegistryId { get; }

        public bool IsFrozen { get; private set; }

        public int Count
        {
            get { return entries.Count; }
        }

        public IReadOnlyList<RegistryEntry<T>> Entries
        {
            get { return entries; }
        }

        public RegistryEntry<T> Register(string id, T value)
        {
            return Register(ResourceLocation.Parse(id), value);
        }

        public RegistryEntry<T> Register(ResourceLocation id, T value)
        {
            if (IsFrozen)
                throw new InvalidOperationException($"Registry {RegistryId} is frozen.");
            if (ReferenceEquals(value, null))
                throw new ArgumentNullException(nameof(value));
            EnsureInitialized(id, nameof(id));
            if (entriesById.ContainsKey(id))
                throw new ArgumentException($"Registry {RegistryId} already contains id {id}.", nameof(id));
            if (idsByValue.ContainsKey(value))
                throw new ArgumentException($"Registry {RegistryId} already contains the supplied value.", nameof(value));

            RegistryEntry<T> entry = new RegistryEntry<T>(id, entries.Count, value);
            entriesById.Add(id, entry);
            idsByValue.Add(value, id);
            entries.Add(entry);
            return entry;
        }

        public void Freeze()
        {
            IsFrozen = true;
        }

        public bool ContainsId(ResourceLocation id)
        {
            return entriesById.ContainsKey(id);
        }

        public bool TryGetEntry(ResourceLocation id, out RegistryEntry<T> entry)
        {
            return entriesById.TryGetValue(id, out entry);
        }

        public RegistryEntry<T> GetEntry(ResourceLocation id)
        {
            RegistryEntry<T> entry;
            if (!TryGetEntry(id, out entry))
                throw new KeyNotFoundException($"Registry {RegistryId} does not contain id {id}.");

            return entry;
        }

        public bool TryGetValue(ResourceLocation id, out T value)
        {
            RegistryEntry<T> entry;
            if (entriesById.TryGetValue(id, out entry))
            {
                value = entry.Value;
                return true;
            }

            value = default(T);
            return false;
        }

        public T GetValue(ResourceLocation id)
        {
            T value;
            if (!TryGetValue(id, out value))
                throw new KeyNotFoundException($"Registry {RegistryId} does not contain id {id}.");

            return value;
        }

        public bool TryGetId(T value, out ResourceLocation id)
        {
            if (ReferenceEquals(value, null))
            {
                id = default(ResourceLocation);
                return false;
            }

            return idsByValue.TryGetValue(value, out id);
        }

        public ResourceLocation GetId(T value)
        {
            ResourceLocation id;
            if (!TryGetId(value, out id))
                throw new KeyNotFoundException($"Registry {RegistryId} does not contain the supplied value.");

            return id;
        }

        private static void EnsureInitialized(ResourceLocation id, string parameterName)
        {
            if (string.IsNullOrEmpty(id.Namespace) || string.IsNullOrEmpty(id.Path))
                throw new ArgumentException("Resource id must be initialized.", parameterName);
        }
    }
}
