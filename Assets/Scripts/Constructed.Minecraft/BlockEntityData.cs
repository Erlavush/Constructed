using System;
using System.Collections.Generic;
using System.Globalization;

namespace Constructed.Minecraft
{
    public readonly struct BlockEntityData : IEquatable<BlockEntityData>
    {
        private readonly BlockEntityDataValue[] values;

        public BlockEntityData(IEnumerable<BlockEntityDataValue> values)
        {
            this.values = CopyValues(values);
        }

        public static BlockEntityData Empty
        {
            get { return new BlockEntityData(Array.Empty<BlockEntityDataValue>()); }
        }

        public IReadOnlyList<BlockEntityDataValue> Values
        {
            get { return values ?? Array.Empty<BlockEntityDataValue>(); }
        }

        public bool TryGetString(string key, out string value)
        {
            foreach (BlockEntityDataValue dataValue in Values)
            {
                if (dataValue.Key == key)
                {
                    value = dataValue.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public string GetString(string key)
        {
            string value;
            if (!TryGetString(key, out value))
                throw new KeyNotFoundException($"Block entity data does not contain key {key}.");

            return value;
        }

        public int GetInt32(string key)
        {
            return int.Parse(GetString(key), CultureInfo.InvariantCulture);
        }

        public long GetInt64(string key)
        {
            return long.Parse(GetString(key), CultureInfo.InvariantCulture);
        }

        public bool GetBoolean(string key)
        {
            return bool.Parse(GetString(key));
        }

        public bool Equals(BlockEntityData other)
        {
            if (Values.Count != other.Values.Count)
                return false;

            for (int i = 0; i < Values.Count; i++)
            {
                if (Values[i] != other.Values[i])
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is BlockEntityData other && Equals(other);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            foreach (BlockEntityDataValue value in Values)
                hash = HashCode.Combine(hash, value);

            return hash;
        }

        public static bool operator ==(BlockEntityData left, BlockEntityData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BlockEntityData left, BlockEntityData right)
        {
            return !left.Equals(right);
        }

        private static BlockEntityDataValue[] CopyValues(IEnumerable<BlockEntityDataValue> values)
        {
            if (values == null)
                return Array.Empty<BlockEntityDataValue>();

            List<BlockEntityDataValue> copy = new List<BlockEntityDataValue>();
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (BlockEntityDataValue value in values)
            {
                if (!keys.Add(value.Key))
                    throw new ArgumentException($"Duplicate block entity data key {value.Key}.");

                copy.Add(value);
            }

            return copy.ToArray();
        }
    }
}
