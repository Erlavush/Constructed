using System;
using System.Collections.Generic;

namespace Constructed.Core
{
    public readonly struct RegistryEntry<T> : IEquatable<RegistryEntry<T>>
    {
        public RegistryEntry(ResourceLocation id, int index, T value)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Registry index cannot be negative.");
            if (ReferenceEquals(value, null))
                throw new ArgumentNullException(nameof(value));

            Id = id;
            Index = index;
            Value = value;
        }

        public ResourceLocation Id { get; }

        public int Index { get; }

        public T Value { get; }

        public bool Equals(RegistryEntry<T> other)
        {
            return Id == other.Id && Index == other.Index && EqualityComparer<T>.Default.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is RegistryEntry<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Index, Value);
        }

        public override string ToString()
        {
            return $"{Index}:{Id}";
        }

        public static bool operator ==(RegistryEntry<T> left, RegistryEntry<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RegistryEntry<T> left, RegistryEntry<T> right)
        {
            return !left.Equals(right);
        }
    }
}
