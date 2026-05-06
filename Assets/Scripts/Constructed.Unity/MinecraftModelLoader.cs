using System;
using System.Collections.Generic;
using System.IO;
using Constructed.Core;
using UnityEngine;

namespace Constructed.Unity
{
    public sealed class MinecraftModelFace
    {
        public MinecraftModelFace(Direction direction, Vector4 uv, int rotationDegrees, ResourceLocation textureId)
        {
            if (rotationDegrees != 0 && rotationDegrees != 90 && rotationDegrees != 180 && rotationDegrees != 270)
                throw new ArgumentOutOfRangeException(nameof(rotationDegrees), "Face rotation must be 0, 90, 180, or 270 degrees.");
            if (string.IsNullOrEmpty(textureId.Namespace) || string.IsNullOrEmpty(textureId.Path))
                throw new ArgumentException("Resolved face texture id must be initialized.", nameof(textureId));

            Direction = direction;
            Uv = uv;
            RotationDegrees = rotationDegrees;
            TextureId = textureId;
        }

        public Direction Direction { get; }

        public Vector4 Uv { get; }

        public int RotationDegrees { get; }

        public ResourceLocation TextureId { get; }
    }

    public sealed class MinecraftModelElementRotation
    {
        public MinecraftModelElementRotation(Vector3 origin, Axis axis, float angle, bool rescale)
        {
            Origin = origin;
            Axis = axis;
            Angle = angle;
            Rescale = rescale;
        }

        public Vector3 Origin { get; }

        public Axis Axis { get; }

        public float Angle { get; }

        public bool Rescale { get; }
    }

    public sealed class MinecraftModelElement
    {
        private readonly Dictionary<Direction, MinecraftModelFace> faces;

        public MinecraftModelElement(
            string name,
            Vector3 from,
            Vector3 to,
            MinecraftModelElementRotation rotation,
            IDictionary<Direction, MinecraftModelFace> faces)
        {
            if (faces == null)
                throw new ArgumentNullException(nameof(faces));

            Vector3 min = Vector3.Min(from, to);
            Vector3 max = Vector3.Max(from, to);
            if (max.x <= min.x || max.y <= min.y || max.z <= min.z)
                throw new ArgumentException("Minecraft model element bounds must have positive size.", nameof(to));

            Name = name ?? string.Empty;
            From = min;
            To = max;
            Rotation = rotation;
            this.faces = new Dictionary<Direction, MinecraftModelFace>(faces);
            if (this.faces.Count == 0)
                throw new ArgumentException("Minecraft model elements must have at least one face.", nameof(faces));
        }

        public string Name { get; }

        public Vector3 From { get; }

        public Vector3 To { get; }

        public MinecraftModelElementRotation Rotation { get; }

        public IReadOnlyDictionary<Direction, MinecraftModelFace> Faces
        {
            get { return faces; }
        }
    }

    public sealed class MinecraftModelDisplayTransform
    {
        public MinecraftModelDisplayTransform(Vector3 rotation, Vector3 translation, Vector3 scale)
        {
            Rotation = rotation;
            Translation = translation;
            Scale = scale;
        }

        public Vector3 Rotation { get; }

        public Vector3 Translation { get; }

        public Vector3 Scale { get; }
    }

    public sealed class MinecraftResolvedModel
    {
        private readonly Dictionary<string, ResourceLocation> resolvedTextures;
        private readonly List<ResourceLocation> generatedItemTextureIds;
        private readonly List<MinecraftModelElement> elements;

        public MinecraftResolvedModel(
            ResourceLocation modelId,
            IDictionary<string, ResourceLocation> resolvedTextures,
            IEnumerable<ResourceLocation> generatedItemTextureIds,
            IEnumerable<MinecraftModelElement> elements,
            Vector2 textureSize,
            MinecraftModelDisplayTransform guiDisplay,
            bool usesGeneratedItemLayers)
        {
            if (string.IsNullOrEmpty(modelId.Namespace) || string.IsNullOrEmpty(modelId.Path))
                throw new ArgumentException("Resolved model id must be initialized.", nameof(modelId));
            if (resolvedTextures == null)
                throw new ArgumentNullException(nameof(resolvedTextures));
            if (generatedItemTextureIds == null)
                throw new ArgumentNullException(nameof(generatedItemTextureIds));
            if (elements == null)
                throw new ArgumentNullException(nameof(elements));
            if (textureSize.x <= 0f || textureSize.y <= 0f)
                throw new ArgumentOutOfRangeException(nameof(textureSize), "Model texture size must be positive.");

            ModelId = modelId;
            this.resolvedTextures = new Dictionary<string, ResourceLocation>(resolvedTextures, StringComparer.Ordinal);
            this.generatedItemTextureIds = new List<ResourceLocation>(generatedItemTextureIds);
            this.elements = new List<MinecraftModelElement>(elements);
            TextureSize = textureSize;
            GuiDisplay = guiDisplay;
            UsesGeneratedItemLayers = usesGeneratedItemLayers;
        }

        public ResourceLocation ModelId { get; }

        public IReadOnlyList<MinecraftModelElement> Elements
        {
            get { return elements; }
        }

        public IReadOnlyList<ResourceLocation> GeneratedItemTextureIds
        {
            get { return generatedItemTextureIds; }
        }

        public Vector2 TextureSize { get; }

        public MinecraftModelDisplayTransform GuiDisplay { get; }

        public bool UsesGeneratedItemLayers { get; }

        public bool HasGuiDisplay
        {
            get { return !ReferenceEquals(GuiDisplay, null); }
        }

        public bool TryGetResolvedTexture(string key, out ResourceLocation textureId)
        {
            return resolvedTextures.TryGetValue(key, out textureId);
        }
    }

    public sealed class MinecraftModelLoader
    {
        private static readonly ResourceLocation BlockBlockModelId = ResourceLocation.Parse("block/block");
        private static readonly ResourceLocation GeneratedItemModelId = ResourceLocation.Parse("item/generated");

        private readonly string assetRoot;
        private readonly Dictionary<ResourceLocation, MinecraftResolvedModel> resolvedModelsById =
            new Dictionary<ResourceLocation, MinecraftResolvedModel>();
        private readonly Dictionary<ResourceLocation, MergedModelState> mergedStatesById =
            new Dictionary<ResourceLocation, MergedModelState>();

        public MinecraftModelLoader(string assetRoot)
        {
            if (string.IsNullOrWhiteSpace(assetRoot))
                throw new ArgumentException("Minecraft model asset root cannot be empty.", nameof(assetRoot));

            this.assetRoot = Path.GetFullPath(assetRoot);
        }

        public MinecraftResolvedModel LoadItemModel(ResourceLocation itemId)
        {
            if (string.IsNullOrEmpty(itemId.Namespace) || string.IsNullOrEmpty(itemId.Path))
                throw new ArgumentException("Item id must be initialized.", nameof(itemId));

            return LoadModel(new ResourceLocation(itemId.Namespace, "item/" + itemId.Path));
        }

        public MinecraftResolvedModel LoadModel(ResourceLocation modelId)
        {
            if (resolvedModelsById.TryGetValue(modelId, out MinecraftResolvedModel resolvedModel))
                return resolvedModel;

            MergedModelState merged = LoadMergedState(modelId, new HashSet<ResourceLocation>());
            resolvedModel = ResolveModel(merged);
            resolvedModelsById.Add(modelId, resolvedModel);
            return resolvedModel;
        }

        private MergedModelState LoadMergedState(ResourceLocation modelId, HashSet<ResourceLocation> loadingStack)
        {
            if (mergedStatesById.TryGetValue(modelId, out MergedModelState merged))
                return merged;

            if (!loadingStack.Add(modelId))
                throw new InvalidDataException("Detected a circular Minecraft model parent chain at " + modelId + ".");

            try
            {
                if (modelId == BlockBlockModelId)
                {
                    merged = MergedModelState.CreateBuiltinBlock(modelId);
                }
                else if (modelId == GeneratedItemModelId)
                {
                    merged = MergedModelState.CreateBuiltinGeneratedItem(modelId);
                }
                else
                {
                    RawModelDocument rawModel = ParseModelDocument(modelId, ResolveModelAbsolutePath(modelId));
                    MergedModelState parentState = null;
                    if (rawModel.ParentModelId.HasValue)
                        parentState = LoadMergedState(rawModel.ParentModelId.Value, loadingStack);

                    merged = MergedModelState.Merge(modelId, parentState, rawModel);
                }

                mergedStatesById.Add(modelId, merged);
                return merged;
            }
            finally
            {
                loadingStack.Remove(modelId);
            }
        }

        private MinecraftResolvedModel ResolveModel(MergedModelState merged)
        {
            Dictionary<string, ResourceLocation> resolvedTextures =
                ResolveTextureMap(merged.ModelId, merged.TextureReferences);
            List<MinecraftModelElement> elements = ResolveElements(merged.ElementTemplates, resolvedTextures);
            List<ResourceLocation> generatedItemTextures =
                ResolveGeneratedItemLayers(merged.ModelId, merged.UsesGeneratedItemLayers, merged.TextureReferences, resolvedTextures);
            MinecraftModelDisplayTransform guiDisplay = null;
            merged.DisplayTransforms.TryGetValue("gui", out guiDisplay);

            return new MinecraftResolvedModel(
                merged.ModelId,
                resolvedTextures,
                generatedItemTextures,
                elements,
                merged.TextureSize,
                guiDisplay,
                merged.UsesGeneratedItemLayers);
        }

        private string ResolveModelAbsolutePath(ResourceLocation modelId)
        {
            string relativePath = modelId.Path.Replace('/', Path.DirectorySeparatorChar) + ".json";
            string generatedPath = Path.Combine(
                assetRoot,
                "src",
                "generated",
                "resources",
                "assets",
                modelId.Namespace,
                "models",
                relativePath);
            if (File.Exists(generatedPath))
                return generatedPath;

            string mainPath = Path.Combine(
                assetRoot,
                "src",
                "main",
                "resources",
                "assets",
                modelId.Namespace,
                "models",
                relativePath);
            if (File.Exists(mainPath))
                return mainPath;

            throw new FileNotFoundException("Could not find Minecraft model JSON for " + modelId + " under " + assetRoot + ".", mainPath);
        }

        private static RawModelDocument ParseModelDocument(ResourceLocation modelId, string absolutePath)
        {
            Dictionary<string, object> root = MinecraftJsonParser.ParseObject(File.ReadAllText(absolutePath), absolutePath);
            ResourceLocation? parentId = null;
            if (MinecraftJsonParser.TryGetString(root, "parent", out string parentValue))
                parentId = ResourceLocation.Parse(parentValue);

            Dictionary<string, string> textures = ParseTextureReferences(
                MinecraftJsonParser.TryGetObject(root, "textures", out Dictionary<string, object> texturesObject) ? texturesObject : null);
            List<ModelElementTemplate> elements = ParseElements(
                MinecraftJsonParser.TryGetArray(root, "elements", out List<object> elementsArray) ? elementsArray : null,
                absolutePath);
            Dictionary<string, MinecraftModelDisplayTransform> displayTransforms =
                ParseDisplayTransforms(
                    MinecraftJsonParser.TryGetObject(root, "display", out Dictionary<string, object> displayObject) ? displayObject : null);
            Vector2? textureSize = ParseTextureSize(
                MinecraftJsonParser.TryGetArray(root, "texture_size", out List<object> textureSizeArray) ? textureSizeArray : null,
                absolutePath);

            return new RawModelDocument(parentId, textures, elements, displayTransforms, textureSize);
        }

        private static Dictionary<string, string> ParseTextureReferences(Dictionary<string, object> texturesObject)
        {
            Dictionary<string, string> textures = new Dictionary<string, string>(StringComparer.Ordinal);
            if (texturesObject == null)
                return textures;

            foreach (KeyValuePair<string, object> property in texturesObject)
            {
                string value = MinecraftJsonParser.AsString(property.Value, "textures." + property.Key);
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                textures[property.Key] = value;
            }

            return textures;
        }

        private static Vector2? ParseTextureSize(List<object> textureSizeArray, string sourcePath)
        {
            if (textureSizeArray == null)
                return null;
            if (textureSizeArray.Count != 2)
                throw new InvalidDataException("Model texture_size must contain exactly two numbers in " + sourcePath + ".");

            return new Vector2(ReadFloat(textureSizeArray[0]), ReadFloat(textureSizeArray[1]));
        }

        private static List<ModelElementTemplate> ParseElements(List<object> elementsArray, string sourcePath)
        {
            if (elementsArray == null)
                return null;

            List<ModelElementTemplate> elements = new List<ModelElementTemplate>(elementsArray.Count);
            foreach (object elementToken in elementsArray)
            {
                Dictionary<string, object> elementObject = MinecraftJsonParser.AsObject(elementToken, sourcePath + " element");

                string name = MinecraftJsonParser.TryGetString(elementObject, "name", out string elementName) ? elementName : string.Empty;
                Vector3 from = ReadVector3(elementObject["from"], sourcePath, "from");
                Vector3 to = ReadVector3(elementObject["to"], sourcePath, "to");
                ModelElementRotationTemplate rotation = ParseElementRotation(
                    MinecraftJsonParser.TryGetObject(elementObject, "rotation", out Dictionary<string, object> rotationObject) ? rotationObject : null,
                    sourcePath);
                Dictionary<Direction, ModelFaceTemplate> faces = ParseFaces(
                    MinecraftJsonParser.TryGetObject(elementObject, "faces", out Dictionary<string, object> facesObject) ? facesObject : null,
                    sourcePath);
                elements.Add(new ModelElementTemplate(name, from, to, rotation, faces));
            }

            return elements;
        }

        private static ModelElementRotationTemplate ParseElementRotation(Dictionary<string, object> rotationObject, string sourcePath)
        {
            if (rotationObject == null)
                return null;

            string axisValue = MinecraftJsonParser.TryGetString(rotationObject, "axis", out string parsedAxis) ? parsedAxis : null;
            if (string.IsNullOrWhiteSpace(axisValue))
                throw new InvalidDataException("Model element rotation axis is required in " + sourcePath + ".");

            double angle;
            bool rescale;
            return new ModelElementRotationTemplate(
                ReadVector3(rotationObject["origin"], sourcePath, "rotation.origin"),
                ParseAxis(axisValue, sourcePath),
                MinecraftJsonParser.TryGetNumber(rotationObject, "angle", out angle) ? (float)angle : 0f,
                MinecraftJsonParser.TryGetBoolean(rotationObject, "rescale", out rescale) && rescale);
        }

        private static Dictionary<Direction, ModelFaceTemplate> ParseFaces(Dictionary<string, object> facesObject, string sourcePath)
        {
            if (facesObject == null)
                throw new InvalidDataException("Model elements must define a faces object in " + sourcePath + ".");

            Dictionary<Direction, ModelFaceTemplate> faces = new Dictionary<Direction, ModelFaceTemplate>();
            foreach (KeyValuePair<string, object> property in facesObject)
            {
                Dictionary<string, object> faceObject = MinecraftJsonParser.AsObject(property.Value, sourcePath + " face " + property.Key);

                Direction direction = ParseDirection(property.Key, sourcePath);
                string textureReference = MinecraftJsonParser.TryGetString(faceObject, "texture", out string parsedTextureReference)
                    ? parsedTextureReference
                    : null;
                if (string.IsNullOrWhiteSpace(textureReference))
                    throw new InvalidDataException("Model face texture reference is required for " + direction + " in " + sourcePath + ".");

                int rotationDegrees = MinecraftJsonParser.TryGetNumber(faceObject, "rotation", out double rotationNumber)
                    ? Convert.ToInt32(rotationNumber)
                    : 0;
                faces.Add(
                    direction,
                    new ModelFaceTemplate(
                        direction,
                        MinecraftJsonParser.TryGetArray(faceObject, "uv", out List<object> uvArray)
                            ? ReadVector4(uvArray, sourcePath, "faces." + property.Key + ".uv")
                            : Vector4.zero,
                        rotationDegrees,
                        textureReference));
            }

            if (faces.Count == 0)
                throw new InvalidDataException("Model element faces cannot be empty in " + sourcePath + ".");

            return faces;
        }

        private static Dictionary<string, MinecraftModelDisplayTransform> ParseDisplayTransforms(Dictionary<string, object> displayObject)
        {
            Dictionary<string, MinecraftModelDisplayTransform> transforms = new Dictionary<string, MinecraftModelDisplayTransform>(StringComparer.Ordinal);
            if (displayObject == null)
                return transforms;

            foreach (KeyValuePair<string, object> property in displayObject)
            {
                Dictionary<string, object> transformObject = MinecraftJsonParser.AsObject(property.Value, "display." + property.Key);

                Vector3 rotation = MinecraftJsonParser.TryGetArray(transformObject, "rotation", out List<object> rotationArray)
                    ? ReadVector3(rotationArray, string.Empty, "display." + property.Key + ".rotation")
                    : Vector3.zero;
                Vector3 translation = MinecraftJsonParser.TryGetArray(transformObject, "translation", out List<object> translationArray)
                    ? ReadVector3(translationArray, string.Empty, "display." + property.Key + ".translation")
                    : Vector3.zero;
                Vector3 scale = MinecraftJsonParser.TryGetArray(transformObject, "scale", out List<object> scaleArray)
                    ? ReadVector3(scaleArray, string.Empty, "display." + property.Key + ".scale")
                    : Vector3.one;

                transforms[property.Key] = new MinecraftModelDisplayTransform(rotation, translation, scale);
            }

            return transforms;
        }

        private static Dictionary<string, ResourceLocation> ResolveTextureMap(
            ResourceLocation modelId,
            Dictionary<string, string> textureReferences)
        {
            Dictionary<string, ResourceLocation> resolved = new Dictionary<string, ResourceLocation>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> pair in textureReferences)
            {
                resolved[pair.Key] = ResolveTextureReference(modelId, textureReferences, pair.Value, new HashSet<string>());
            }

            return resolved;
        }

        private static ResourceLocation ResolveTextureReference(
            ResourceLocation modelId,
            Dictionary<string, string> textureReferences,
            string reference,
            HashSet<string> keyStack)
        {
            if (!reference.StartsWith("#", StringComparison.Ordinal))
                return ResourceLocation.Parse(reference);

            string key = reference.Substring(1);
            if (!textureReferences.TryGetValue(key, out string nextReference))
                throw new InvalidDataException("Model " + modelId + " references missing texture variable #" + key + ".");
            if (!keyStack.Add(key))
                throw new InvalidDataException("Model " + modelId + " contains a circular texture variable reference at #" + key + ".");

            try
            {
                return ResolveTextureReference(modelId, textureReferences, nextReference, keyStack);
            }
            finally
            {
                keyStack.Remove(key);
            }
        }

        private static List<MinecraftModelElement> ResolveElements(
            List<ModelElementTemplate> templates,
            Dictionary<string, ResourceLocation> resolvedTextures)
        {
            List<MinecraftModelElement> elements = new List<MinecraftModelElement>();
            if (templates == null)
                return elements;

            foreach (ModelElementTemplate template in templates)
            {
                Dictionary<Direction, MinecraftModelFace> faces = new Dictionary<Direction, MinecraftModelFace>();
                foreach (KeyValuePair<Direction, ModelFaceTemplate> pair in template.Faces)
                {
                    ModelFaceTemplate faceTemplate = pair.Value;
                    faces.Add(
                        pair.Key,
                        new MinecraftModelFace(
                            faceTemplate.Direction,
                            faceTemplate.Uv,
                            faceTemplate.RotationDegrees,
                            ResolveFaceTexture(faceTemplate.TextureReference, resolvedTextures)));
                }

                MinecraftModelElementRotation rotation = null;
                if (template.Rotation != null)
                {
                    rotation = new MinecraftModelElementRotation(
                        template.Rotation.Origin,
                        template.Rotation.Axis,
                        template.Rotation.Angle,
                        template.Rotation.Rescale);
                }

                elements.Add(new MinecraftModelElement(template.Name, template.From, template.To, rotation, faces));
            }

            return elements;
        }

        private static List<ResourceLocation> ResolveGeneratedItemLayers(
            ResourceLocation modelId,
            bool usesGeneratedItemLayers,
            Dictionary<string, string> textureReferences,
            Dictionary<string, ResourceLocation> resolvedTextures)
        {
            List<ResourceLocation> layers = new List<ResourceLocation>();
            if (!usesGeneratedItemLayers)
                return layers;

            SortedDictionary<int, ResourceLocation> layersByIndex = new SortedDictionary<int, ResourceLocation>();
            foreach (KeyValuePair<string, string> pair in textureReferences)
            {
                if (!pair.Key.StartsWith("layer", StringComparison.Ordinal))
                    continue;
                if (!int.TryParse(pair.Key.Substring("layer".Length), out int layerIndex))
                    continue;
                if (!resolvedTextures.TryGetValue(pair.Key, out ResourceLocation textureId))
                    throw new InvalidDataException("Generated item model " + modelId + " is missing resolved texture layer " + pair.Key + ".");

                layersByIndex[layerIndex] = textureId;
            }

            foreach (KeyValuePair<int, ResourceLocation> pair in layersByIndex)
                layers.Add(pair.Value);

            return layers;
        }

        private static ResourceLocation ResolveFaceTexture(string textureReference, Dictionary<string, ResourceLocation> resolvedTextures)
        {
            if (!textureReference.StartsWith("#", StringComparison.Ordinal))
                return ResourceLocation.Parse(textureReference);

            string key = textureReference.Substring(1);
            if (!resolvedTextures.TryGetValue(key, out ResourceLocation textureId))
                throw new InvalidDataException("Resolved model is missing texture variable #" + key + ".");

            return textureId;
        }

        private static Vector3 ReadVector3(object token, string sourcePath, string fieldName)
        {
            List<object> array = MinecraftJsonParser.AsArray(token, fieldName);
            if (array.Count != 3)
                throw new InvalidDataException("Expected a three-number array for " + fieldName + " in " + sourcePath + ".");

            return new Vector3(ReadFloat(array[0]), ReadFloat(array[1]), ReadFloat(array[2]));
        }

        private static Vector4 ReadVector4(object token, string sourcePath, string fieldName)
        {
            List<object> array = MinecraftJsonParser.AsArray(token, fieldName);
            if (array.Count != 4)
                throw new InvalidDataException("Expected a four-number array for " + fieldName + " in " + sourcePath + ".");

            return new Vector4(ReadFloat(array[0]), ReadFloat(array[1]), ReadFloat(array[2]), ReadFloat(array[3]));
        }

        private static float ReadFloat(object token)
        {
            return MinecraftJsonParser.ToFloat(token, "number");
        }

        private static Axis ParseAxis(string value, string sourcePath)
        {
            switch (value)
            {
                case "x":
                    return Axis.X;
                case "y":
                    return Axis.Y;
                case "z":
                    return Axis.Z;
                default:
                    throw new InvalidDataException("Unsupported model rotation axis '" + value + "' in " + sourcePath + ".");
            }
        }

        private static Direction ParseDirection(string value, string sourcePath)
        {
            switch (value)
            {
                case "down":
                    return Direction.Down;
                case "up":
                    return Direction.Up;
                case "north":
                    return Direction.North;
                case "south":
                    return Direction.South;
                case "west":
                    return Direction.West;
                case "east":
                    return Direction.East;
                default:
                    throw new InvalidDataException("Unsupported model face direction '" + value + "' in " + sourcePath + ".");
            }
        }

        private sealed class RawModelDocument
        {
            public RawModelDocument(
                ResourceLocation? parentModelId,
                Dictionary<string, string> textureReferences,
                List<ModelElementTemplate> elements,
                Dictionary<string, MinecraftModelDisplayTransform> displayTransforms,
                Vector2? textureSize)
            {
                ParentModelId = parentModelId;
                TextureReferences = textureReferences ?? new Dictionary<string, string>(StringComparer.Ordinal);
                Elements = elements;
                DisplayTransforms = displayTransforms ?? new Dictionary<string, MinecraftModelDisplayTransform>(StringComparer.Ordinal);
                TextureSize = textureSize;
            }

            public ResourceLocation? ParentModelId { get; }

            public Dictionary<string, string> TextureReferences { get; }

            public List<ModelElementTemplate> Elements { get; }

            public Dictionary<string, MinecraftModelDisplayTransform> DisplayTransforms { get; }

            public Vector2? TextureSize { get; }
        }

        private sealed class MergedModelState
        {
            public MergedModelState(
                ResourceLocation modelId,
                Dictionary<string, string> textureReferences,
                List<ModelElementTemplate> elementTemplates,
                Dictionary<string, MinecraftModelDisplayTransform> displayTransforms,
                Vector2 textureSize,
                bool usesGeneratedItemLayers)
            {
                ModelId = modelId;
                TextureReferences = textureReferences;
                ElementTemplates = elementTemplates;
                DisplayTransforms = displayTransforms;
                TextureSize = textureSize;
                UsesGeneratedItemLayers = usesGeneratedItemLayers;
            }

            public ResourceLocation ModelId { get; }

            public Dictionary<string, string> TextureReferences { get; }

            public List<ModelElementTemplate> ElementTemplates { get; }

            public Dictionary<string, MinecraftModelDisplayTransform> DisplayTransforms { get; }

            public Vector2 TextureSize { get; }

            public bool UsesGeneratedItemLayers { get; }

            public static MergedModelState CreateBuiltinBlock(ResourceLocation modelId)
            {
                return new MergedModelState(
                    modelId,
                    new Dictionary<string, string>(StringComparer.Ordinal),
                    null,
                    new Dictionary<string, MinecraftModelDisplayTransform>(StringComparer.Ordinal),
                    new Vector2(16f, 16f),
                    false);
            }

            public static MergedModelState CreateBuiltinGeneratedItem(ResourceLocation modelId)
            {
                return new MergedModelState(
                    modelId,
                    new Dictionary<string, string>(StringComparer.Ordinal),
                    null,
                    new Dictionary<string, MinecraftModelDisplayTransform>(StringComparer.Ordinal),
                    new Vector2(16f, 16f),
                    true);
            }

            public static MergedModelState Merge(
                ResourceLocation modelId,
                MergedModelState parentState,
                RawModelDocument rawModel)
            {
                Dictionary<string, string> mergedTextureReferences = parentState != null
                    ? new Dictionary<string, string>(parentState.TextureReferences, StringComparer.Ordinal)
                    : new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (KeyValuePair<string, string> pair in rawModel.TextureReferences)
                    mergedTextureReferences[pair.Key] = pair.Value;

                Dictionary<string, MinecraftModelDisplayTransform> mergedDisplayTransforms = parentState != null
                    ? new Dictionary<string, MinecraftModelDisplayTransform>(parentState.DisplayTransforms, StringComparer.Ordinal)
                    : new Dictionary<string, MinecraftModelDisplayTransform>(StringComparer.Ordinal);
                foreach (KeyValuePair<string, MinecraftModelDisplayTransform> pair in rawModel.DisplayTransforms)
                    mergedDisplayTransforms[pair.Key] = pair.Value;

                List<ModelElementTemplate> mergedElements = rawModel.Elements ?? (parentState != null ? parentState.ElementTemplates : null);
                Vector2 textureSize = rawModel.TextureSize ?? (parentState != null ? parentState.TextureSize : new Vector2(16f, 16f));
                bool usesGeneratedItemLayers = rawModel.ParentModelId.HasValue && rawModel.ParentModelId.Value == GeneratedItemModelId;
                if (parentState != null && parentState.UsesGeneratedItemLayers)
                    usesGeneratedItemLayers = true;

                return new MergedModelState(
                    modelId,
                    mergedTextureReferences,
                    mergedElements,
                    mergedDisplayTransforms,
                    textureSize,
                    usesGeneratedItemLayers);
            }
        }

        private sealed class ModelElementTemplate
        {
            public ModelElementTemplate(
                string name,
                Vector3 from,
                Vector3 to,
                ModelElementRotationTemplate rotation,
                Dictionary<Direction, ModelFaceTemplate> faces)
            {
                Name = name ?? string.Empty;
                From = from;
                To = to;
                Rotation = rotation;
                Faces = faces ?? new Dictionary<Direction, ModelFaceTemplate>();
            }

            public string Name { get; }

            public Vector3 From { get; }

            public Vector3 To { get; }

            public ModelElementRotationTemplate Rotation { get; }

            public Dictionary<Direction, ModelFaceTemplate> Faces { get; }
        }

        private sealed class ModelElementRotationTemplate
        {
            public ModelElementRotationTemplate(Vector3 origin, Axis axis, float angle, bool rescale)
            {
                Origin = origin;
                Axis = axis;
                Angle = angle;
                Rescale = rescale;
            }

            public Vector3 Origin { get; }

            public Axis Axis { get; }

            public float Angle { get; }

            public bool Rescale { get; }
        }

        private sealed class ModelFaceTemplate
        {
            public ModelFaceTemplate(Direction direction, Vector4 uv, int rotationDegrees, string textureReference)
            {
                Direction = direction;
                Uv = uv;
                RotationDegrees = rotationDegrees;
                TextureReference = textureReference;
            }

            public Direction Direction { get; }

            public Vector4 Uv { get; }

            public int RotationDegrees { get; }

            public string TextureReference { get; }
        }
    }
}
