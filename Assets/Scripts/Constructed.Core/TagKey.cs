using System;

namespace Constructed.Core
{
    public readonly struct TagKey<T> : IEquatable<TagKey<T>>
    {
        public TagKey(ResourceLocation registryId, ResourceLocation id)
        {
            if (string.IsNullOrEmpty(registryId.Namespace) || string.IsNullOrEmpty(registryId.Path))
                throw new ArgumentException("Registry id must be initialized.", nameof(registryId));
            if (string.IsNullOrEmpty(id.Namespace) || string.IsNullOrEmpty(id.Path))
                throw new ArgumentException("Tag id must be initialized.", nameof(id));

            RegistryId = registryId;
            Id = id;
        }

        public ResourceLocation RegistryId { get; }

        public ResourceLocation Id { get; }

        public static TagKey<T> Create(ResourceLocation registryId, ResourceLocation id)
        {
            return new TagKey<T>(registryId, id);
        }

        public static TagKey<T> Parse(ResourceLocation registryId, string id, string defaultNamespace = ResourceLocation.DefaultNamespace)
        {
            return new TagKey<T>(registryId, ResourceLocation.Parse(id, defaultNamespace));
        }

        public bool Equals(TagKey<T> other)
        {
            return RegistryId == other.RegistryId && Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is TagKey<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RegistryId, Id);
        }

        public override string ToString()
        {
            return $"{RegistryId}#{Id}";
        }

        public static bool operator ==(TagKey<T> left, TagKey<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TagKey<T> left, TagKey<T> right)
        {
            return !left.Equals(right);
        }
    }
}
