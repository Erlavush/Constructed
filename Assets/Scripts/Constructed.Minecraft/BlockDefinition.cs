using System;
using System.Collections.Generic;
using Constructed.Core;

namespace Constructed.Minecraft
{
    public sealed class BlockDefinition
    {
        private readonly Dictionary<string, IStateProperty> propertiesByName;
        private readonly List<IStateProperty> properties;

        public BlockDefinition(ResourceLocation id)
            : this(id, Array.Empty<IStateProperty>())
        {
        }

        public BlockDefinition(ResourceLocation id, IEnumerable<IStateProperty> properties)
        {
            if (string.IsNullOrEmpty(id.Namespace) || string.IsNullOrEmpty(id.Path))
                throw new ArgumentException("Block id must be initialized.", nameof(id));
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            Id = id;
            this.properties = new List<IStateProperty>();
            propertiesByName = new Dictionary<string, IStateProperty>(StringComparer.Ordinal);

            Dictionary<string, object> defaultValues = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (IStateProperty property in properties)
                AddProperty(property, defaultValues);

            Properties = this.properties.AsReadOnly();
            DefaultState = new BlockState(this, defaultValues);
        }

        public ResourceLocation Id { get; }

        public IReadOnlyList<IStateProperty> Properties { get; }

        public BlockState DefaultState { get; }

        public bool HasProperty(string name)
        {
            return propertiesByName.ContainsKey(name);
        }

        public bool HasProperty(IStateProperty property)
        {
            if (property == null)
                return false;

            IStateProperty declared;
            return propertiesByName.TryGetValue(property.Name, out declared) && ReferenceEquals(declared, property);
        }

        public bool TryGetProperty(string name, out IStateProperty property)
        {
            return propertiesByName.TryGetValue(name, out property);
        }

        public IStateProperty GetProperty(string name)
        {
            IStateProperty property;
            if (!TryGetProperty(name, out property))
                throw new KeyNotFoundException($"Block {Id} does not define state property {name}.");

            return property;
        }

        public BlockState CreateState(IEnumerable<BlockStatePropertyValue> serializedProperties)
        {
            if (serializedProperties == null)
                return DefaultState;

            BlockState state = DefaultState;
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (BlockStatePropertyValue propertyValue in serializedProperties)
            {
                if (!seen.Add(propertyValue.Name))
                    throw new ArgumentException($"Duplicate serialized property {propertyValue.Name} for block {Id}.");

                IStateProperty property = GetProperty(propertyValue.Name);
                object parsedValue = property.ParseValue(propertyValue.Value);
                state = state.WithValue(property, parsedValue);
            }

            return state;
        }

        public BlockState CreateState(SerializedBlockState serialized)
        {
            if (serialized.BlockId != Id)
                throw new ArgumentException($"Serialized block state id {serialized.BlockId} does not match block {Id}.", nameof(serialized));

            return CreateState(serialized.Properties);
        }

        public override string ToString()
        {
            return Id.ToString();
        }

        internal IStateProperty RequireDeclaredProperty(IStateProperty property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            IStateProperty declared;
            if (!propertiesByName.TryGetValue(property.Name, out declared) || !ReferenceEquals(declared, property))
                throw new ArgumentException($"Block {Id} does not define state property {property.Name}.", nameof(property));

            return declared;
        }

        private void AddProperty(IStateProperty property, Dictionary<string, object> defaultValues)
        {
            if (property == null)
                throw new ArgumentException("Block state property cannot be null.");
            if (propertiesByName.ContainsKey(property.Name))
                throw new ArgumentException($"Block {Id} already defines state property {property.Name}.");

            this.properties.Add(property);
            propertiesByName.Add(property.Name, property);
            defaultValues.Add(property.Name, property.DefaultValue);
        }
    }
}
