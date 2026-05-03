using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Constructed.Minecraft
{
    public sealed class StateProperty<T> : IStateProperty
    {
        private static readonly Regex NamePattern = new Regex("^[a-z0-9_]+$", RegexOptions.Compiled);

        private readonly List<T> values;
        private readonly List<object> objectValues;
        private readonly Dictionary<string, T> valuesBySerializedName;
        private readonly Func<T, string> valueSerializer;

        public StateProperty(string name, IEnumerable<T> validValues, T defaultValue, Func<T, string> valueSerializer = null)
        {
            if (!IsValidName(name))
                throw new ArgumentException($"Invalid block state property name: {name}", nameof(name));
            if (validValues == null)
                throw new ArgumentNullException(nameof(validValues));

            Name = name;
            this.valueSerializer = valueSerializer ?? DefaultSerializeValue;
            values = new List<T>();
            objectValues = new List<object>();
            valuesBySerializedName = new Dictionary<string, T>(StringComparer.Ordinal);

            HashSet<T> seenValues = new HashSet<T>();
            foreach (T value in validValues)
                AddValidValue(value, seenValues);

            if (values.Count == 0)
                throw new ArgumentException("Block state property must have at least one valid value.", nameof(validValues));
            if (!Contains(defaultValue))
                throw new ArgumentException($"Default value is not valid for block state property {name}.", nameof(defaultValue));

            Default = defaultValue;
        }

        public string Name { get; }

        public T Default { get; }

        public IReadOnlyList<T> Values
        {
            get { return values; }
        }

        public Type ValueType
        {
            get { return typeof(T); }
        }

        public object DefaultValue
        {
            get { return Default; }
        }

        public IReadOnlyList<object> ValidValues
        {
            get { return objectValues; }
        }

        public static StateProperty<bool> Bool(string name, bool defaultValue = false)
        {
            return new StateProperty<bool>(name, new[] { false, true }, defaultValue);
        }

        public static bool IsValidName(string name)
        {
            return !string.IsNullOrEmpty(name) && NamePattern.IsMatch(name);
        }

        public bool Contains(T value)
        {
            return values.Contains(value);
        }

        public bool IsValidValue(object value)
        {
            return value is T typed && Contains(typed);
        }

        public string Serialize(T value)
        {
            if (!Contains(value))
                throw new ArgumentException($"Value {value} is not valid for block state property {Name}.", nameof(value));

            return valueSerializer(value);
        }

        public string SerializeValue(object value)
        {
            if (!(value is T typed))
                throw new ArgumentException($"Value is not a {typeof(T).Name} for block state property {Name}.", nameof(value));

            return Serialize(typed);
        }

        public T Parse(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException($"Serialized value for block state property {Name} cannot be empty.", nameof(value));

            T parsed;
            if (!valuesBySerializedName.TryGetValue(value, out parsed))
                throw new ArgumentException($"Value {value} is not valid for block state property {Name}.", nameof(value));

            return parsed;
        }

        public object ParseValue(string value)
        {
            return Parse(value);
        }

        private void AddValidValue(T value, HashSet<T> seenValues)
        {
            if (ReferenceEquals(value, null))
                throw new ArgumentException($"Null is not a valid value for block state property {Name}.");
            if (!seenValues.Add(value))
                throw new ArgumentException($"Duplicate value {value} for block state property {Name}.");

            string serialized = valueSerializer(value);
            if (string.IsNullOrEmpty(serialized))
                throw new ArgumentException($"Serialized value for block state property {Name} cannot be empty.");
            if (valuesBySerializedName.ContainsKey(serialized))
                throw new ArgumentException($"Duplicate serialized value {serialized} for block state property {Name}.");

            values.Add(value);
            objectValues.Add(value);
            valuesBySerializedName.Add(serialized, value);
        }

        private static string DefaultSerializeValue(T value)
        {
            string text = value.ToString();
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Block state property values must serialize to non-empty strings.");

            return text.ToLowerInvariant();
        }
    }
}
