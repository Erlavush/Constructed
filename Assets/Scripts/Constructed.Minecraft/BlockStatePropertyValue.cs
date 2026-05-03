using System;

namespace Constructed.Minecraft
{
    public readonly struct BlockStatePropertyValue : IEquatable<BlockStatePropertyValue>
    {
        public BlockStatePropertyValue(string name, string value)
        {
            if (!StateProperty<object>.IsValidName(name))
                throw new ArgumentException($"Invalid block state property name: {name}", nameof(name));
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Block state property value cannot be empty.", nameof(value));

            Name = name;
            Value = value;
        }

        public string Name { get; }

        public string Value { get; }

        public bool Equals(BlockStatePropertyValue other)
        {
            return Name == other.Name && Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is BlockStatePropertyValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Value);
        }

        public override string ToString()
        {
            return $"{Name}={Value}";
        }

        public static bool operator ==(BlockStatePropertyValue left, BlockStatePropertyValue right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BlockStatePropertyValue left, BlockStatePropertyValue right)
        {
            return !left.Equals(right);
        }
    }
}
