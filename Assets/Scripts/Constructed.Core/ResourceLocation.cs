using System;
using System.Text.RegularExpressions;

namespace Constructed.Core
{
    public readonly struct ResourceLocation : IEquatable<ResourceLocation>
    {
        public const string DefaultNamespace = "minecraft";

        private static readonly Regex NamespacePattern = new Regex("^[a-z0-9_.-]+$", RegexOptions.Compiled);
        private static readonly Regex PathPattern = new Regex("^[a-z0-9_./-]+$", RegexOptions.Compiled);

        public ResourceLocation(string namespaceId, string path)
        {
            if (!IsValidNamespace(namespaceId))
                throw new ArgumentException($"Invalid resource namespace: {namespaceId}", nameof(namespaceId));
            if (!IsValidPath(path))
                throw new ArgumentException($"Invalid resource path: {path}", nameof(path));

            Namespace = namespaceId;
            Path = path;
        }

        public string Namespace { get; }

        public string Path { get; }

        public static ResourceLocation Parse(string value, string defaultNamespace = DefaultNamespace)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Resource location cannot be empty.", nameof(value));

            int separator = value.IndexOf(':');
            if (separator < 0)
                return new ResourceLocation(defaultNamespace, value);

            if (separator != value.LastIndexOf(':'))
                throw new FormatException($"Resource location can contain only one namespace separator: {value}");

            return new ResourceLocation(value.Substring(0, separator), value.Substring(separator + 1));
        }

        public static bool IsValidNamespace(string value)
        {
            return !string.IsNullOrEmpty(value) && NamespacePattern.IsMatch(value);
        }

        public static bool IsValidPath(string value)
        {
            return !string.IsNullOrEmpty(value) && PathPattern.IsMatch(value);
        }

        public bool Equals(ResourceLocation other)
        {
            return Namespace == other.Namespace && Path == other.Path;
        }

        public override bool Equals(object obj)
        {
            return obj is ResourceLocation other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Namespace, Path);
        }

        public override string ToString()
        {
            return $"{Namespace}:{Path}";
        }

        public static bool operator ==(ResourceLocation left, ResourceLocation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ResourceLocation left, ResourceLocation right)
        {
            return !left.Equals(right);
        }
    }
}
