using System;
using System.Collections.Generic;

namespace Constructed.Minecraft
{
    public sealed class BlockState : IEquatable<BlockState>
    {
        private readonly Dictionary<string, object> values;

        internal BlockState(BlockDefinition definition, IDictionary<string, object> values)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            Definition = definition;
            this.values = new Dictionary<string, object>(values, StringComparer.Ordinal);
        }

        public BlockDefinition Definition { get; }

        public T Get<T>(StateProperty<T> property)
        {
            Definition.RequireDeclaredProperty(property);

            object value;
            if (!values.TryGetValue(property.Name, out value))
                throw new KeyNotFoundException($"Block state {Definition.Id} does not contain property {property.Name}.");

            return (T)value;
        }

        public object GetValue(IStateProperty property)
        {
            Definition.RequireDeclaredProperty(property);

            object value;
            if (!values.TryGetValue(property.Name, out value))
                throw new KeyNotFoundException($"Block state {Definition.Id} does not contain property {property.Name}.");

            return value;
        }

        public BlockState With<T>(StateProperty<T> property, T value)
        {
            Definition.RequireDeclaredProperty(property);
            if (!property.Contains(value))
                throw new ArgumentException($"Value {value} is not valid for block state property {property.Name}.", nameof(value));

            T current = Get(property);
            if (EqualityComparer<T>.Default.Equals(current, value))
                return this;

            Dictionary<string, object> nextValues = new Dictionary<string, object>(values, StringComparer.Ordinal);
            nextValues[property.Name] = value;
            return new BlockState(Definition, nextValues);
        }

        public BlockState WithValue(IStateProperty property, object value)
        {
            Definition.RequireDeclaredProperty(property);
            if (!property.IsValidValue(value))
                throw new ArgumentException($"Value {value} is not valid for block state property {property.Name}.", nameof(value));

            object current = GetValue(property);
            if (Equals(current, value))
                return this;

            Dictionary<string, object> nextValues = new Dictionary<string, object>(values, StringComparer.Ordinal);
            nextValues[property.Name] = value;
            return new BlockState(Definition, nextValues);
        }

        public SerializedBlockState Serialize()
        {
            List<BlockStatePropertyValue> serializedProperties = new List<BlockStatePropertyValue>();
            foreach (IStateProperty property in Definition.Properties)
            {
                object value = GetValue(property);
                serializedProperties.Add(new BlockStatePropertyValue(property.Name, property.SerializeValue(value)));
            }

            return new SerializedBlockState(Definition.Id, serializedProperties);
        }

        public bool Equals(BlockState other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (!ReferenceEquals(Definition, other.Definition))
                return false;

            foreach (IStateProperty property in Definition.Properties)
            {
                object value;
                object otherValue;
                if (!values.TryGetValue(property.Name, out value) || !other.values.TryGetValue(property.Name, out otherValue))
                    return false;
                if (!Equals(value, otherValue))
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is BlockState other && Equals(other);
        }

        public override int GetHashCode()
        {
            int hash = Definition.Id.GetHashCode();
            foreach (IStateProperty property in Definition.Properties)
                hash = HashCode.Combine(hash, GetValue(property));

            return hash;
        }

        public override string ToString()
        {
            return Serialize().ToString();
        }
    }
}
