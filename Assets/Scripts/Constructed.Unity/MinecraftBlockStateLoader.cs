using System;
using System.Collections.Generic;
using System.IO;
using Constructed.Core;
using Constructed.Minecraft;

namespace Constructed.Unity
{
    public sealed class MinecraftBlockStateVariant
    {
        public MinecraftBlockStateVariant(ResourceLocation modelId, int xRotationDegrees, int yRotationDegrees, bool uvLock)
        {
            if (string.IsNullOrEmpty(modelId.Namespace) || string.IsNullOrEmpty(modelId.Path))
                throw new ArgumentException("Blockstate variant model id must be initialized.", nameof(modelId));

            ModelId = modelId;
            XRotationDegrees = xRotationDegrees;
            YRotationDegrees = yRotationDegrees;
            UvLock = uvLock;
        }

        public ResourceLocation ModelId { get; }

        public int XRotationDegrees { get; }

        public int YRotationDegrees { get; }

        public bool UvLock { get; }
    }

    public sealed class MinecraftBlockStateVariantCase
    {
        private readonly BlockStatePropertyValue[] properties;

        public MinecraftBlockStateVariantCase(
            IEnumerable<BlockStatePropertyValue> properties,
            MinecraftBlockStateVariant variant)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));
            if (variant == null)
                throw new ArgumentNullException(nameof(variant));

            List<BlockStatePropertyValue> values = new List<BlockStatePropertyValue>();
            foreach (BlockStatePropertyValue property in properties)
                values.Add(property);
            if (values.Count == 0)
                throw new ArgumentException("Blockstate variant cases must contain at least one property.", nameof(properties));

            this.properties = values.ToArray();
            Variant = variant;
            CanonicalKey = MinecraftBlockStateDefinition.CreateCanonicalKey(this.properties);
        }

        public string CanonicalKey { get; }

        public IReadOnlyList<BlockStatePropertyValue> Properties
        {
            get { return properties; }
        }

        public MinecraftBlockStateVariant Variant { get; }
    }

    public sealed class MinecraftBlockStateDefinition
    {
        private readonly MinecraftBlockStateVariantCase[] variantCases;
        private readonly Dictionary<string, MinecraftBlockStateVariantCase> variantsByCanonicalKey;

        public MinecraftBlockStateDefinition(
            ResourceLocation blockId,
            IEnumerable<MinecraftBlockStateVariantCase> variantCases)
        {
            if (string.IsNullOrEmpty(blockId.Namespace) || string.IsNullOrEmpty(blockId.Path))
                throw new ArgumentException("Blockstate definition id must be initialized.", nameof(blockId));
            if (variantCases == null)
                throw new ArgumentNullException(nameof(variantCases));

            BlockId = blockId;
            List<MinecraftBlockStateVariantCase> variants = new List<MinecraftBlockStateVariantCase>();
            variantsByCanonicalKey = new Dictionary<string, MinecraftBlockStateVariantCase>(StringComparer.Ordinal);
            foreach (MinecraftBlockStateVariantCase variantCase in variantCases)
            {
                if (variantCase == null)
                    throw new ArgumentException("Blockstate definitions cannot contain null variant cases.", nameof(variantCases));
                if (variantsByCanonicalKey.ContainsKey(variantCase.CanonicalKey))
                    throw new ArgumentException("Duplicate blockstate variant key " + variantCase.CanonicalKey + " for " + blockId + ".", nameof(variantCases));

                variants.Add(variantCase);
                variantsByCanonicalKey.Add(variantCase.CanonicalKey, variantCase);
            }

            if (variants.Count == 0)
                throw new ArgumentException("Blockstate definitions must contain at least one variant case.", nameof(variantCases));

            this.variantCases = variants.ToArray();
        }

        public ResourceLocation BlockId { get; }

        public IReadOnlyList<MinecraftBlockStateVariantCase> VariantCases
        {
            get { return variantCases; }
        }

        public MinecraftBlockStateVariant ResolveVariant(IEnumerable<BlockStatePropertyValue> properties)
        {
            if (!TryResolveVariant(properties, out MinecraftBlockStateVariant variant))
                throw new KeyNotFoundException("Blockstate " + BlockId + " does not contain variant " + CreateCanonicalKey(properties) + ".");

            return variant;
        }

        public bool TryResolveVariant(IEnumerable<BlockStatePropertyValue> properties, out MinecraftBlockStateVariant variant)
        {
            string canonicalKey = CreateCanonicalKey(properties);
            if (variantsByCanonicalKey.TryGetValue(canonicalKey, out MinecraftBlockStateVariantCase variantCase))
            {
                variant = variantCase.Variant;
                return true;
            }

            variant = null;
            return false;
        }

        public static string CreateCanonicalKey(IEnumerable<BlockStatePropertyValue> properties)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            SortedDictionary<string, string> valuesByName = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (BlockStatePropertyValue property in properties)
            {
                if (valuesByName.ContainsKey(property.Name))
                    throw new ArgumentException("Duplicate blockstate property " + property.Name + ".");

                valuesByName.Add(property.Name, property.Value);
            }

            if (valuesByName.Count == 0)
                throw new ArgumentException("Blockstate variant keys must contain at least one property.", nameof(properties));

            string canonicalKey = string.Empty;
            foreach (KeyValuePair<string, string> pair in valuesByName)
            {
                if (canonicalKey.Length > 0)
                    canonicalKey += ",";
                canonicalKey += pair.Key + "=" + pair.Value;
            }

            return canonicalKey;
        }
    }

    public sealed class MinecraftBlockStateLoader
    {
        private readonly string assetRoot;
        private readonly Dictionary<ResourceLocation, MinecraftBlockStateDefinition> definitionsById =
            new Dictionary<ResourceLocation, MinecraftBlockStateDefinition>();

        public MinecraftBlockStateLoader(string assetRoot)
        {
            if (string.IsNullOrWhiteSpace(assetRoot))
                throw new ArgumentException("Minecraft blockstate asset root cannot be empty.", nameof(assetRoot));

            this.assetRoot = Path.GetFullPath(assetRoot);
        }

        public MinecraftBlockStateDefinition LoadBlockState(ResourceLocation blockId)
        {
            if (definitionsById.TryGetValue(blockId, out MinecraftBlockStateDefinition definition))
                return definition;

            string absolutePath = ResolveBlockStateAbsolutePath(blockId);
            Dictionary<string, object> root = MinecraftJsonParser.ParseObject(File.ReadAllText(absolutePath), absolutePath);
            if (!MinecraftJsonParser.TryGetObject(root, "variants", out Dictionary<string, object> variantsObject))
                throw new InvalidDataException("Blockstate " + blockId + " must define a variants object in " + absolutePath + ".");

            List<MinecraftBlockStateVariantCase> variantCases = new List<MinecraftBlockStateVariantCase>();
            foreach (KeyValuePair<string, object> property in variantsObject)
                variantCases.Add(new MinecraftBlockStateVariantCase(ParseVariantProperties(property.Key), ParseVariant(property.Value, absolutePath)));

            definition = new MinecraftBlockStateDefinition(blockId, variantCases);
            definitionsById.Add(blockId, definition);
            return definition;
        }

        private string ResolveBlockStateAbsolutePath(ResourceLocation blockId)
        {
            string relativePath = blockId.Path.Replace('/', Path.DirectorySeparatorChar) + ".json";
            string generatedPath = Path.Combine(
                assetRoot,
                "src",
                "generated",
                "resources",
                "assets",
                blockId.Namespace,
                "blockstates",
                relativePath);
            if (File.Exists(generatedPath))
                return generatedPath;

            string mainPath = Path.Combine(
                assetRoot,
                "src",
                "main",
                "resources",
                "assets",
                blockId.Namespace,
                "blockstates",
                relativePath);
            if (File.Exists(mainPath))
                return mainPath;

            throw new FileNotFoundException("Could not find Minecraft blockstate JSON for " + blockId + " under " + assetRoot + ".", mainPath);
        }

        private static BlockStatePropertyValue[] ParseVariantProperties(string variantKey)
        {
            if (string.IsNullOrWhiteSpace(variantKey))
                throw new InvalidDataException("Blockstate variant keys cannot be empty.");

            string[] parts = variantKey.Split(',');
            BlockStatePropertyValue[] properties = new BlockStatePropertyValue[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                string[] keyValue = parts[i].Split('=');
                if (keyValue.Length != 2)
                    throw new InvalidDataException("Blockstate variant key '" + variantKey + "' must use name=value pairs.");

                properties[i] = new BlockStatePropertyValue(keyValue[0], keyValue[1]);
            }

            return properties;
        }

        private static MinecraftBlockStateVariant ParseVariant(object token, string sourcePath)
        {
            if (token is List<object> variantArray)
            {
                if (variantArray.Count == 0)
                    throw new InvalidDataException("Blockstate variant arrays must contain at least one entry in " + sourcePath + ".");

                return ParseVariant(variantArray[0], sourcePath);
            }

            Dictionary<string, object> variantObject = MinecraftJsonParser.AsObject(token, sourcePath + " variant");
            string modelValue;
            MinecraftJsonParser.TryGetString(variantObject, "model", out modelValue);
            if (string.IsNullOrWhiteSpace(modelValue))
                throw new InvalidDataException("Blockstate variants must define a model in " + sourcePath + ".");

            double xRotation;
            double yRotation;
            bool uvLock;
            return new MinecraftBlockStateVariant(
                ResourceLocation.Parse(modelValue),
                MinecraftJsonParser.TryGetNumber(variantObject, "x", out xRotation) ? Convert.ToInt32(xRotation) : 0,
                MinecraftJsonParser.TryGetNumber(variantObject, "y", out yRotation) ? Convert.ToInt32(yRotation) : 0,
                MinecraftJsonParser.TryGetBoolean(variantObject, "uvlock", out uvLock) && uvLock);
        }
    }
}
