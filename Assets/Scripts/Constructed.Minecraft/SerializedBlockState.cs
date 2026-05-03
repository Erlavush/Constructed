using System;
using System.Collections.Generic;
using System.Text;
using Constructed.Core;

namespace Constructed.Minecraft
{
    public readonly struct SerializedBlockState : IEquatable<SerializedBlockState>
    {
        private readonly BlockStatePropertyValue[] properties;

        public SerializedBlockState(ResourceLocation blockId, IEnumerable<BlockStatePropertyValue> properties)
        {
            if (string.IsNullOrEmpty(blockId.Namespace) || string.IsNullOrEmpty(blockId.Path))
                throw new ArgumentException("Block id must be initialized.", nameof(blockId));

            BlockId = blockId;
            this.properties = CopyProperties(properties);
        }

        public ResourceLocation BlockId { get; }

        public IReadOnlyList<BlockStatePropertyValue> Properties
        {
            get { return properties ?? Array.Empty<BlockStatePropertyValue>(); }
        }

        public bool TryGetProperty(string name, out string value)
        {
            foreach (BlockStatePropertyValue property in Properties)
            {
                if (property.Name == name)
                {
                    value = property.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public string GetProperty(string name)
        {
            string value;
            if (!TryGetProperty(name, out value))
                throw new KeyNotFoundException($"Serialized block state {BlockId} does not contain property {name}.");

            return value;
        }

        public bool Equals(SerializedBlockState other)
        {
            if (BlockId != other.BlockId || Properties.Count != other.Properties.Count)
                return false;

            for (int i = 0; i < Properties.Count; i++)
            {
                if (Properties[i] != other.Properties[i])
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is SerializedBlockState other && Equals(other);
        }

        public override int GetHashCode()
        {
            int hash = BlockId.GetHashCode();
            foreach (BlockStatePropertyValue property in Properties)
                hash = HashCode.Combine(hash, property);

            return hash;
        }

        public override string ToString()
        {
            if (Properties.Count == 0)
                return BlockId.ToString();

            StringBuilder builder = new StringBuilder();
            builder.Append(BlockId);
            builder.Append('[');
            for (int i = 0; i < Properties.Count; i++)
            {
                if (i > 0)
                    builder.Append(',');
                builder.Append(Properties[i]);
            }

            builder.Append(']');
            return builder.ToString();
        }

        public static bool operator ==(SerializedBlockState left, SerializedBlockState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SerializedBlockState left, SerializedBlockState right)
        {
            return !left.Equals(right);
        }

        private static BlockStatePropertyValue[] CopyProperties(IEnumerable<BlockStatePropertyValue> values)
        {
            if (values == null)
                return Array.Empty<BlockStatePropertyValue>();

            List<BlockStatePropertyValue> copy = new List<BlockStatePropertyValue>();
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            foreach (BlockStatePropertyValue value in values)
            {
                if (!names.Add(value.Name))
                    throw new ArgumentException($"Duplicate block state property {value.Name}.");
                copy.Add(value);
            }

            return copy.ToArray();
        }
    }
}
