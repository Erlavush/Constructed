using System;

namespace Constructed.Minecraft
{
    public readonly struct BlockEntityDataValue : IEquatable<BlockEntityDataValue>
    {
        public BlockEntityDataValue(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Block entity data key cannot be empty.", nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Key = key;
            Value = value;
        }

        public string Key { get; }

        public string Value { get; }

        public bool Equals(BlockEntityDataValue other)
        {
            return Key == other.Key && Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is BlockEntityDataValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Key, Value);
        }

        public override string ToString()
        {
            return $"{Key}={Value}";
        }

        public static bool operator ==(BlockEntityDataValue left, BlockEntityDataValue right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BlockEntityDataValue left, BlockEntityDataValue right)
        {
            return !left.Equals(right);
        }
    }
}
