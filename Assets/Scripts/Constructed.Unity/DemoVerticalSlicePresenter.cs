using System;
using System.Collections.Generic;
using System.IO;
using Constructed.Core;
using Constructed.Create;
using Constructed.Minecraft;
using UnityEngine;

namespace Constructed.Unity
{
    [ExecuteAlways]
    public sealed class DemoVerticalSlicePresenter : MonoBehaviour
    {
        private const string GeneratedRootName = "Generated Demo Layout";
        private const string PrivateGrassTexturePath = "PrivateTemp/Minecraft/grass_block_top.png";
        private const float ItemCatalogZ = -2.5f;
        private const float ItemCatalogStartX = 1.5f;
        private const float ItemCatalogSpacing = 2.15f;
        private const float ItemCatalogCardY = 1.65f;
        private const float BlockCatalogZ = -6.0f;
        private const float BlockCatalogStartX = 3.0f;
        private const float BlockCatalogSpacing = 2.6f;
        private const float BlockCatalogPreviewY = 1.7f;
        private const float ItemModelPreviewBaseScale = 1.6f;
        private const float BlockModelPreviewBaseScale = 1.15f;
        private const float WorldBlockModelBaseScale = 1.0f;

        private static readonly MinecraftModelDisplayTransform DefaultItemModelDisplay =
            new MinecraftModelDisplayTransform(new Vector3(30f, 225f, 0f), Vector3.zero, new Vector3(0.8f, 0.8f, 0.8f));

        private readonly Dictionary<string, Material> materialsByKey = new Dictionary<string, Material>();
        private readonly Dictionary<string, Texture2D> createTexturesByPath = new Dictionary<string, Texture2D>();
        private readonly List<Mesh> runtimeModelMeshes = new List<Mesh>();
        private Texture2D runtimeGrassTexture;
        private Texture2D missingCreateItemTexture;

        public int GeneratedBlockCount { get; private set; }

        public int GeneratedItemPreviewCount { get; private set; }

        public int GeneratedBlockCatalogPreviewCount { get; private set; }

        public int GeneratedStateDrivenWorldBlockCount { get; private set; }

        public int GeneratedModelItemPreviewCount { get; private set; }

        public int GeneratedFlatItemPreviewCount { get; private set; }

        public int FailedItemModelPreviewCount { get; private set; }

        public int FailedBlockCatalogPreviewCount { get; private set; }

        public int FailedStateDrivenWorldBlockCount { get; private set; }

        public int SyncedCreateAssetFileCount { get; private set; }

        public int MissingCreateAssetFileCount { get; private set; }

        public int CopiedCreateAssetFileCount { get; private set; }

        private void OnEnable()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            ClearGeneratedObjects();
            DestroyRuntimeAssets();
            GeneratedBlockCount = 0;
            GeneratedItemPreviewCount = 0;
            GeneratedBlockCatalogPreviewCount = 0;
            GeneratedStateDrivenWorldBlockCount = 0;
            GeneratedModelItemPreviewCount = 0;
            GeneratedFlatItemPreviewCount = 0;
            FailedItemModelPreviewCount = 0;
            FailedBlockCatalogPreviewCount = 0;
            FailedStateDrivenWorldBlockCount = 0;
            SyncedCreateAssetFileCount = 0;
            MissingCreateAssetFileCount = 0;
            CopiedCreateAssetFileCount = 0;

            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateVerticalSliceWorld(catalog);
            CreatePrivateAssetSyncResult createAssetSync = SyncPrivateCreateAssets();
            if (createAssetSync != null)
            {
                SyncedCreateAssetFileCount = createAssetSync.AvailableFileCount;
                MissingCreateAssetFileCount = createAssetSync.MissingPaths.Count;
                CopiedCreateAssetFileCount = createAssetSync.CopiedPaths.Count;
            }
            MinecraftModelLoader modelLoader = CreatePrivateModelLoader();
            MinecraftBlockStateLoader blockStateLoader = CreatePrivateBlockStateLoader();

            Transform root = CreateGeneratedRoot();
            Transform worldRoot = CreateChildRoot(root, "World");
            Transform itemCatalogRoot = CreateChildRoot(root, "Item Catalog");
            Transform blockCatalogRoot = CreateChildRoot(root, "Block Catalog");

            foreach (WorldBlockEntry entry in world.GetStoredBlocks())
                CreateBlock(worldRoot, catalog, entry, modelLoader, blockStateLoader);

            CreateItemCatalog(itemCatalogRoot, modelLoader);
            CreateBlockCatalog(blockCatalogRoot, modelLoader, blockStateLoader);

            ConfigureCamera();
            ConfigureLight();
        }

        private Transform CreateGeneratedRoot()
        {
            GameObject root = new GameObject(GeneratedRootName);
            root.transform.SetParent(transform, false);
            return root.transform;
        }

        private static Transform CreateChildRoot(Transform parent, string name)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            return root.transform;
        }

        private void CreateBlock(
            Transform root,
            DemoContentCatalog catalog,
            WorldBlockEntry entry,
            MinecraftModelLoader modelLoader,
            MinecraftBlockStateLoader blockStateLoader)
        {
            if (TryCreateStateDrivenWorldBlock(root, catalog, entry, modelLoader, blockStateLoader))
            {
                GeneratedStateDrivenWorldBlockCount++;
                GeneratedBlockCount++;
                return;
            }

            CreatePlaceholderWorldBlock(root, catalog, entry);
            GeneratedBlockCount++;
        }

        private bool TryCreateStateDrivenWorldBlock(
            Transform root,
            DemoContentCatalog catalog,
            WorldBlockEntry entry,
            MinecraftModelLoader modelLoader,
            MinecraftBlockStateLoader blockStateLoader)
        {
            if (!CreateFirstSliceWorldBlockVisualStateBridge.TryResolve(entry.State, out BlockStatePropertyValue[] visualProperties))
                return false;

            GameObject blockRoot = new GameObject(GetDisplayName(catalog, entry));
            blockRoot.transform.SetParent(root, false);
            blockRoot.transform.localPosition = ToUnityPosition(entry.Position);

            if (!TryCreateBlockModel(blockRoot.transform, entry.State.Definition.Id, visualProperties, WorldBlockModelBaseScale, modelLoader, blockStateLoader))
            {
                FailedStateDrivenWorldBlockCount++;
                DestroyUnityObject(blockRoot);
                return false;
            }

            AddLabel(blockRoot.transform, GetLabel(catalog, entry.State));
            return true;
        }

        private void CreatePlaceholderWorldBlock(Transform root, DemoContentCatalog catalog, WorldBlockEntry entry)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = GetDisplayName(catalog, entry);
            block.transform.SetParent(root, false);
            block.transform.localPosition = ToUnityPosition(entry.Position);
            block.transform.localScale = GetScale(catalog, entry.State);

            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = GetMaterial(catalog, entry.State);

            if (entry.State.Definition.Id != catalog.Surface.Id)
                AddLabel(block.transform, GetLabel(catalog, entry.State));
        }

        private void CreateItemCatalog(Transform root, MinecraftModelLoader itemModelLoader)
        {
            AddFloatingLabel(root, "Create Item Catalog", new Vector3(8f, 3.65f, ItemCatalogZ), 0.16f);
            AddFloatingLabel(
                root,
                $"Private assets: {SyncedCreateAssetFileCount}/{CreateFirstSlicePrivateAssetManifest.Manifest.UniqueFiles.Count} ready, {MissingCreateAssetFileCount} missing",
                new Vector3(8f, 3.1f, ItemCatalogZ),
                0.11f);

            for (int i = 0; i < CreateFirstSliceItemVisualCatalog.Entries.Count; i++)
            {
                CreateItemPreview(root, CreateFirstSliceItemVisualCatalog.Entries[i], i, itemModelLoader);
                GeneratedItemPreviewCount++;
            }

            AddFloatingLabel(
                root,
                $"Previews: {GeneratedModelItemPreviewCount} model, {GeneratedFlatItemPreviewCount} flat, {FailedItemModelPreviewCount} fallback errors",
                new Vector3(8f, 2.55f, ItemCatalogZ),
                0.11f);
        }

        private void CreateBlockCatalog(
            Transform root,
            MinecraftModelLoader modelLoader,
            MinecraftBlockStateLoader blockStateLoader)
        {
            AddFloatingLabel(root, "Create Block Catalog", new Vector3(8f, 3.65f, BlockCatalogZ), 0.16f);
            AddFloatingLabel(
                root,
                "Blockstate variants: shaft, creative motor, creative crate, brass funnel, item vault",
                new Vector3(8f, 3.1f, BlockCatalogZ),
                0.11f);

            for (int i = 0; i < CreateFirstSliceBlockVisualCatalog.Entries.Count; i++)
            {
                CreateBlockCatalogPreview(root, CreateFirstSliceBlockVisualCatalog.Entries[i], i, modelLoader, blockStateLoader);
                GeneratedBlockCatalogPreviewCount++;
            }

            AddFloatingLabel(
                root,
                $"Previews: {GeneratedBlockCatalogPreviewCount} total, {FailedBlockCatalogPreviewCount} fallback errors",
                new Vector3(8f, 2.55f, BlockCatalogZ),
                0.11f);
        }

        private void CreateItemPreview(Transform root, CreateItemVisualCatalogEntry entry, int index, MinecraftModelLoader itemModelLoader)
        {
            float x = ItemCatalogStartX + (index * ItemCatalogSpacing);
            Vector3 pedestalPosition = new Vector3(x, 0.2f, ItemCatalogZ);
            Vector3 cardPosition = new Vector3(x, ItemCatalogCardY, ItemCatalogZ);

            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedestal.name = entry.Label + " Pedestal";
            pedestal.transform.SetParent(root, false);
            pedestal.transform.localPosition = pedestalPosition;
            pedestal.transform.localScale = new Vector3(0.9f, 0.4f, 0.9f);
            Renderer pedestalRenderer = pedestal.GetComponent<Renderer>();
            if (pedestalRenderer != null)
                pedestalRenderer.sharedMaterial = GetMaterial("item_pedestal", new Color(0.18f, 0.19f, 0.22f));

            GameObject previewRoot = new GameObject(entry.Label);
            previewRoot.transform.SetParent(root, false);
            previewRoot.transform.localPosition = cardPosition;

            if (TryCreateModelPreview(previewRoot.transform, entry, itemModelLoader))
                GeneratedModelItemPreviewCount++;
            else
            {
                CreateFlatItemPreview(previewRoot.transform, entry);
                GeneratedFlatItemPreviewCount++;
            }

            AddFloatingLabel(previewRoot.transform, entry.Label, new Vector3(0f, 0.9f, 0f), 0.1f);
        }

        private void CreateBlockCatalogPreview(
            Transform root,
            CreateBlockVisualCatalogEntry entry,
            int index,
            MinecraftModelLoader modelLoader,
            MinecraftBlockStateLoader blockStateLoader)
        {
            float x = BlockCatalogStartX + (index * BlockCatalogSpacing);
            Vector3 pedestalPosition = new Vector3(x, 0.2f, BlockCatalogZ);
            Vector3 previewPosition = new Vector3(x, BlockCatalogPreviewY, BlockCatalogZ);

            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedestal.name = entry.Label + " Pedestal";
            pedestal.transform.SetParent(root, false);
            pedestal.transform.localPosition = pedestalPosition;
            pedestal.transform.localScale = new Vector3(1.0f, 0.4f, 1.0f);
            Renderer pedestalRenderer = pedestal.GetComponent<Renderer>();
            if (pedestalRenderer != null)
                pedestalRenderer.sharedMaterial = GetMaterial("block_pedestal", new Color(0.21f, 0.18f, 0.16f));

            GameObject previewRoot = new GameObject(entry.Label);
            previewRoot.transform.SetParent(root, false);
            previewRoot.transform.localPosition = previewPosition;

            if (!TryCreateBlockModel(previewRoot.transform, entry.BlockId, entry.PreviewProperties, BlockModelPreviewBaseScale, modelLoader, blockStateLoader))
            {
                FailedBlockCatalogPreviewCount++;
                CreateFallbackBlockPreview(previewRoot.transform, entry);
            }

            AddFloatingLabel(previewRoot.transform, entry.Label, new Vector3(0f, 0.95f, 0f), 0.1f);
        }

        private void CreateFlatItemPreview(Transform root, CreateItemVisualCatalogEntry entry)
        {
            GameObject card = GameObject.CreatePrimitive(PrimitiveType.Cube);
            card.name = "Card";
            card.transform.SetParent(root, false);
            card.transform.localScale = new Vector3(1.1f, 1.1f, 0.05f);
            card.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);

            Renderer cardRenderer = card.GetComponent<Renderer>();
            if (cardRenderer != null)
                cardRenderer.sharedMaterial = GetItemPreviewMaterial(entry);
        }

        private bool TryCreateModelPreview(Transform root, CreateItemVisualCatalogEntry entry, MinecraftModelLoader itemModelLoader)
        {
            if (itemModelLoader == null)
                return false;

            Transform modelRoot = CreateChildRoot(root, "Model");
            try
            {
                MinecraftResolvedModel model = itemModelLoader.LoadModel(entry.PreviewModelId);
                if (model.Elements.Count == 0)
                {
                    DestroyUnityObject(modelRoot.gameObject);
                    return false;
                }

                ApplyItemModelDisplay(modelRoot, model);

                int faceIndex = 0;
                foreach (MinecraftModelElement element in model.Elements)
                {
                    foreach (MinecraftModelFace face in element.Faces.Values)
                    {
                        CreateModelFaceObject(modelRoot, model, element, face, faceIndex);
                        faceIndex++;
                    }
                }

                if (faceIndex == 0)
                {
                    DestroyUnityObject(modelRoot.gameObject);
                    return false;
                }

                return true;
            }
            catch (Exception)
            {
                FailedItemModelPreviewCount++;
                DestroyUnityObject(modelRoot.gameObject);
                return false;
            }
        }

        private bool TryCreateBlockModel(
            Transform root,
            ResourceLocation blockId,
            IReadOnlyList<BlockStatePropertyValue> visualProperties,
            float baseScale,
            MinecraftModelLoader modelLoader,
            MinecraftBlockStateLoader blockStateLoader)
        {
            if (modelLoader == null || blockStateLoader == null)
                return false;

            Transform modelRoot = CreateChildRoot(root, "Model");
            try
            {
                MinecraftBlockStateDefinition blockStateDefinition = blockStateLoader.LoadBlockState(blockId);
                MinecraftBlockStateVariant variant = blockStateDefinition.ResolveVariant(visualProperties);
                MinecraftResolvedModel model = modelLoader.LoadModel(variant.ModelId);
                if (model.Elements.Count == 0)
                {
                    DestroyUnityObject(modelRoot.gameObject);
                    return false;
                }

                ApplyBlockModelDisplay(modelRoot, variant, baseScale);

                int faceIndex = 0;
                foreach (MinecraftModelElement element in model.Elements)
                {
                    foreach (MinecraftModelFace face in element.Faces.Values)
                    {
                        CreateModelFaceObject(modelRoot, model, element, face, faceIndex);
                        faceIndex++;
                    }
                }

                if (faceIndex == 0)
                {
                    DestroyUnityObject(modelRoot.gameObject);
                    return false;
                }

                return true;
            }
            catch (Exception)
            {
                DestroyUnityObject(modelRoot.gameObject);
                return false;
            }
        }

        private void CreateFallbackBlockPreview(Transform root, CreateBlockVisualCatalogEntry entry)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Fallback";
            cube.transform.SetParent(root, false);
            cube.transform.localScale = new Vector3(0.95f, 0.95f, 0.95f);

            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = GetFallbackBlockMaterial(entry.BlockId);
        }

        private static void ApplyItemModelDisplay(Transform modelRoot, MinecraftResolvedModel model)
        {
            MinecraftModelDisplayTransform display = model.HasGuiDisplay ? model.GuiDisplay : DefaultItemModelDisplay;
            modelRoot.localPosition = display.Translation / 16f;
            modelRoot.localRotation = Quaternion.Euler(display.Rotation);
            modelRoot.localScale = Vector3.Scale(Vector3.one * ItemModelPreviewBaseScale, display.Scale);
        }

        private static void ApplyBlockModelDisplay(Transform modelRoot, MinecraftBlockStateVariant variant, float baseScale)
        {
            modelRoot.localPosition = Vector3.zero;
            modelRoot.localRotation = Quaternion.Euler(variant.XRotationDegrees, variant.YRotationDegrees, 0f);
            modelRoot.localScale = Vector3.one * baseScale;
        }

        private void CreateModelFaceObject(
            Transform root,
            MinecraftResolvedModel model,
            MinecraftModelElement element,
            MinecraftModelFace face,
            int faceIndex)
        {
            GameObject faceObject = new GameObject(GetModelFaceName(element, face, faceIndex));
            faceObject.transform.SetParent(root, false);

            MeshFilter meshFilter = faceObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = faceObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = CreateModelFaceMesh(model, element, face, faceIndex);
            meshRenderer.sharedMaterial = GetCreateTextureMaterial(face.TextureId);
        }

        private Mesh CreateModelFaceMesh(
            MinecraftResolvedModel model,
            MinecraftModelElement element,
            MinecraftModelFace face,
            int faceIndex)
        {
            Mesh mesh = new Mesh();
            mesh.name = "Item Face " + faceIndex;
            mesh.vertices = CreateModelFaceVertices(element, face.Direction);
            mesh.uv = CreateModelFaceUvs(face, model.TextureSize);
            mesh.triangles = CreateDoubleSidedQuadTriangles();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            runtimeModelMeshes.Add(mesh);
            return mesh;
        }

        private static Vector3[] CreateModelFaceVertices(MinecraftModelElement element, Direction direction)
        {
            Vector3 from = element.From;
            Vector3 to = element.To;
            Vector3[] vertices;
            switch (direction)
            {
                case Direction.Down:
                    vertices = new[]
                    {
                        new Vector3(from.x, from.y, from.z),
                        new Vector3(to.x, from.y, from.z),
                        new Vector3(to.x, from.y, to.z),
                        new Vector3(from.x, from.y, to.z)
                    };
                    break;
                case Direction.Up:
                    vertices = new[]
                    {
                        new Vector3(from.x, to.y, to.z),
                        new Vector3(to.x, to.y, to.z),
                        new Vector3(to.x, to.y, from.z),
                        new Vector3(from.x, to.y, from.z)
                    };
                    break;
                case Direction.North:
                    vertices = new[]
                    {
                        new Vector3(from.x, from.y, from.z),
                        new Vector3(to.x, from.y, from.z),
                        new Vector3(to.x, to.y, from.z),
                        new Vector3(from.x, to.y, from.z)
                    };
                    break;
                case Direction.South:
                    vertices = new[]
                    {
                        new Vector3(to.x, from.y, to.z),
                        new Vector3(from.x, from.y, to.z),
                        new Vector3(from.x, to.y, to.z),
                        new Vector3(to.x, to.y, to.z)
                    };
                    break;
                case Direction.West:
                    vertices = new[]
                    {
                        new Vector3(from.x, from.y, from.z),
                        new Vector3(from.x, from.y, to.z),
                        new Vector3(from.x, to.y, to.z),
                        new Vector3(from.x, to.y, from.z)
                    };
                    break;
                case Direction.East:
                    vertices = new[]
                    {
                        new Vector3(to.x, from.y, to.z),
                        new Vector3(to.x, from.y, from.z),
                        new Vector3(to.x, to.y, from.z),
                        new Vector3(to.x, to.y, to.z)
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                if (element.Rotation != null)
                    vertices[i] = ApplyElementRotation(vertices[i], element.Rotation);

                vertices[i] = ToCatalogModelLocalPoint(vertices[i]);
            }

            return vertices;
        }

        private static Vector3 ApplyElementRotation(Vector3 point, MinecraftModelElementRotation rotation)
        {
            Vector3 axis;
            switch (rotation.Axis)
            {
                case Axis.X:
                    axis = Vector3.right;
                    break;
                case Axis.Y:
                    axis = Vector3.up;
                    break;
                case Axis.Z:
                    axis = Vector3.forward;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return rotation.Origin + (Quaternion.AngleAxis(rotation.Angle, axis) * (point - rotation.Origin));
        }

        private static Vector3 ToCatalogModelLocalPoint(Vector3 modelPoint)
        {
            return (modelPoint / 16f) - (Vector3.one * 0.5f);
        }

        private static Vector2[] CreateModelFaceUvs(MinecraftModelFace face, Vector2 textureSize)
        {
            Vector2[] uvs =
            {
                new Vector2(face.Uv.x / textureSize.x, 1f - (face.Uv.w / textureSize.y)),
                new Vector2(face.Uv.z / textureSize.x, 1f - (face.Uv.w / textureSize.y)),
                new Vector2(face.Uv.z / textureSize.x, 1f - (face.Uv.y / textureSize.y)),
                new Vector2(face.Uv.x / textureSize.x, 1f - (face.Uv.y / textureSize.y))
            };

            for (int step = 0; step < (face.RotationDegrees / 90); step++)
            {
                uvs = new[]
                {
                    uvs[3],
                    uvs[0],
                    uvs[1],
                    uvs[2]
                };
            }

            return uvs;
        }

        private static int[] CreateDoubleSidedQuadTriangles()
        {
            return new[]
            {
                0, 1, 2,
                0, 2, 3,
                2, 1, 0,
                3, 2, 0
            };
        }

        private static string GetModelFaceName(MinecraftModelElement element, MinecraftModelFace face, int faceIndex)
        {
            string elementName = string.IsNullOrEmpty(element.Name) ? "Element" : element.Name;
            return elementName + " " + face.Direction + " " + faceIndex;
        }

        private static Vector3 ToUnityPosition(BlockPos position)
        {
            return new Vector3(position.X + 0.5f, position.Y + 0.5f, position.Z + 0.5f);
        }

        private static Vector3 GetScale(DemoContentCatalog catalog, BlockState state)
        {
            ResourceLocation id = state.Definition.Id;
            if (id == catalog.Shaft.Id)
                return new Vector3(1.02f, 0.24f, 0.24f);
            if (id == catalog.Belt.Id)
                return new Vector3(1.0f, 0.18f, 0.9f);
            if (id == catalog.BrassFunnel.Id)
                return new Vector3(0.64f, 0.64f, 0.64f);
            if (id == catalog.CreativeCrate.Id)
                return new Vector3(0.9f, 0.9f, 0.9f);
            if (id == catalog.ItemVault.Id)
                return new Vector3(1.05f, 1.05f, 1.05f);

            return Vector3.one;
        }

        private Material GetMaterial(DemoContentCatalog catalog, BlockState state)
        {
            ResourceLocation id = state.Definition.Id;
            if (id == catalog.Surface.Id)
                return GetMaterial("surface", new Color(0.34f, 0.66f, 0.25f), LoadPrivateGrassTexture());
            if (id == catalog.CreativeMotor.Id)
                return GetMaterial("creative_motor", new Color(0.15f, 0.55f, 0.95f));
            if (id == catalog.Shaft.Id)
                return GetMaterial("shaft", new Color(0.62f, 0.63f, 0.62f));
            if (id == catalog.Belt.Id)
                return GetMaterial("belt", new Color(0.07f, 0.07f, 0.07f));
            if (id == catalog.CreativeCrate.Id)
                return GetMaterial("creative_crate", new Color(0.46f, 0.28f, 0.86f));
            if (id == catalog.BrassFunnel.Id)
                return GetMaterial("brass_funnel", new Color(0.82f, 0.58f, 0.22f));
            if (id == catalog.ItemVault.Id)
                return GetMaterial("item_vault", new Color(0.58f, 0.45f, 0.36f));

            return GetMaterial("unknown", Color.magenta);
        }

        private Material GetMaterial(string key, Color color, Texture2D texture = null)
        {
            Material material;
            if (materialsByKey.TryGetValue(key, out material))
                return material;

            material = new Material(FindLitShader());
            material.name = "Demo " + key;
            material.color = color;
            if (texture != null)
            {
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Repeat;
                material.mainTexture = texture;
            }

            materialsByKey.Add(key, material);
            return material;
        }

        private Material GetItemPreviewMaterial(CreateItemVisualCatalogEntry entry)
        {
            string key = "item_preview_" + entry.ItemId;
            Material material;
            if (materialsByKey.TryGetValue(key, out material))
                return material;

            Texture2D texture = LoadPrivateCreateTexture(entry.PreviewTextureFile);
            material = new Material(FindItemPreviewShader());
            material.name = "Demo " + key;
            material.color = Color.white;
            if (texture != null)
            {
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                material.mainTexture = texture;
            }

            materialsByKey.Add(key, material);
            return material;
        }

        private Material GetFallbackBlockMaterial(ResourceLocation blockId)
        {
            if (blockId == ResourceLocation.Parse("create:shaft"))
                return GetMaterial("block_catalog_shaft", new Color(0.62f, 0.63f, 0.62f));
            if (blockId == ResourceLocation.Parse("create:creative_motor"))
                return GetMaterial("block_catalog_creative_motor", new Color(0.15f, 0.55f, 0.95f));
            if (blockId == ResourceLocation.Parse("create:creative_crate"))
                return GetMaterial("block_catalog_creative_crate", new Color(0.46f, 0.28f, 0.86f));
            if (blockId == ResourceLocation.Parse("create:brass_funnel"))
                return GetMaterial("block_catalog_brass_funnel", new Color(0.82f, 0.58f, 0.22f));
            if (blockId == ResourceLocation.Parse("create:item_vault"))
                return GetMaterial("block_catalog_item_vault", new Color(0.58f, 0.45f, 0.36f));

            return GetMaterial("block_catalog_unknown", Color.magenta);
        }

        private Material GetCreateTextureMaterial(ResourceLocation textureId)
        {
            string key = "create_texture_" + textureId;
            Material material;
            if (materialsByKey.TryGetValue(key, out material))
                return material;

            material = new Material(FindItemPreviewShader());
            material.name = "Demo " + key;
            material.color = Color.white;
            material.mainTexture = LoadPrivateCreateTexture(textureId);
            materialsByKey.Add(key, material);
            return material;
        }

        private static void ConfigureLoadedTexture(Texture2D texture, TextureWrapMode wrapMode)
        {
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = wrapMode;
            texture.anisoLevel = 0;
            texture.mipMapBias = 0f;
        }

        private Texture2D LoadPrivateGrassTexture()
        {
            if (runtimeGrassTexture != null)
                return runtimeGrassTexture;

            string absolutePath = Path.Combine(Application.dataPath, PrivateGrassTexturePath);
            if (File.Exists(absolutePath))
            {
                byte[] data = File.ReadAllBytes(absolutePath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (texture.LoadImage(data))
                {
                    ConfigureLoadedTexture(texture, TextureWrapMode.Repeat);
                    runtimeGrassTexture = texture;
                    return runtimeGrassTexture;
                }

                DestroyUnityObject(texture);
            }

            runtimeGrassTexture = CreateFallbackGrassTexture();
            return runtimeGrassTexture;
        }

        private Texture2D LoadPrivateCreateTexture(CreatePrivateAssetFileReference previewTextureFile)
        {
            Texture2D existingTexture;
            if (createTexturesByPath.TryGetValue(previewTextureFile.RepositoryRelativePath, out existingTexture))
                return existingTexture;

            string projectRoot = GetProjectRoot();
            string privateAssetRoot = CreatePrivateAssetProjectPaths.GetPrivateCreateAssetRoot(projectRoot);
            string privateTexturePath = CreatePrivateAssetPathResolver.ResolvePrivateAssetPath(privateAssetRoot, previewTextureFile);
            if (File.Exists(privateTexturePath))
            {
                byte[] data = File.ReadAllBytes(privateTexturePath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (texture.LoadImage(data))
                {
                    ConfigureLoadedTexture(texture, TextureWrapMode.Clamp);
                    createTexturesByPath.Add(previewTextureFile.RepositoryRelativePath, texture);
                    return texture;
                }

                DestroyUnityObject(texture);
            }

            Texture2D missingTexture = GetMissingCreateItemTexture();
            createTexturesByPath.Add(previewTextureFile.RepositoryRelativePath, missingTexture);
            return missingTexture;
        }

        private Texture2D LoadPrivateCreateTexture(ResourceLocation textureId)
        {
            if (textureId.Namespace != "create")
                return GetMissingCreateItemTexture();

            return LoadPrivateCreateTexture(
                new CreatePrivateAssetFileReference(
                    CreatePrivateAssetFileReference.MainResourcesPrefix + "textures/" + textureId.Path + ".png"));
        }

        private Texture2D GetMissingCreateItemTexture()
        {
            if (missingCreateItemTexture != null)
                return missingCreateItemTexture;

            missingCreateItemTexture = CreateFallbackCreateItemTexture();
            return missingCreateItemTexture;
        }

        private static Texture2D CreateFallbackGrassTexture()
        {
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color dark = new Color(0.22f, 0.52f, 0.18f, 1f);
            Color light = new Color(0.42f, 0.76f, 0.28f, 1f);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                    texture.SetPixel(x, y, ((x + y) % 3 == 0) ? light : dark);
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateFallbackCreateItemTexture()
        {
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color border = new Color(0.88f, 0.2f, 0.16f, 1f);
            Color fillA = new Color(0.22f, 0.22f, 0.22f, 0f);
            Color fillB = new Color(1f, 0.0f, 1f, 0.9f);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    bool isBorder = x == 0 || y == 0 || x == 15 || y == 15;
                    Color pixel = isBorder ? border : (((x + y) % 2 == 0) ? fillA : fillB);
                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();
            return texture;
        }

        private static Shader FindLitShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
                return shader;

            shader = Shader.Find("Standard");
            return shader != null ? shader : Shader.Find("Diffuse");
        }

        private static Shader FindItemPreviewShader()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                return shader;

            shader = Shader.Find("Unlit/Transparent");
            if (shader != null)
                return shader;

            return Shader.Find("Unlit/Texture");
        }

        private static string GetDisplayName(DemoContentCatalog catalog, WorldBlockEntry entry)
        {
            return GetLabel(catalog, entry.State) + " " + entry.Position;
        }

        private static string GetLabel(DemoContentCatalog catalog, BlockState state)
        {
            ResourceLocation id = state.Definition.Id;
            if (id == catalog.Surface.Id)
                return "Grass";
            if (id == catalog.CreativeMotor.Id)
                return "Creative Motor";
            if (id == catalog.Shaft.Id)
                return "Shaft";
            if (id == catalog.Belt.Id)
                return "Belt";
            if (id == catalog.CreativeCrate.Id)
                return "Creative Crate";
            if (id == catalog.BrassFunnel.Id)
                return "Brass Funnel";
            if (id == catalog.ItemVault.Id)
                return "Item Vault";

            return id.ToString();
        }

        private static void AddLabel(Transform parent, string label)
        {
            AddFloatingLabel(parent, label, new Vector3(0f, 0.72f, 0f), 0.12f);
        }

        private static void AddFloatingLabel(Transform parent, string label, Vector3 localPosition, float scale)
        {
            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
            labelObject.transform.localScale = Vector3.one * scale;

            TextMesh text = labelObject.AddComponent<TextMesh>();
            text.text = label;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.45f;
            text.fontSize = 32;
            text.color = Color.white;
        }

        private void ConfigureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.position = new Vector3(8f, 12.5f, -11.5f);
            camera.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
        }

        private void ConfigureLight()
        {
            Light light = FindAnyObjectByType<Light>();
            if (light == null)
            {
                GameObject lightObject = new GameObject("Directional Light");
                light = lightObject.AddComponent<Light>();
            }

            light.type = LightType.Directional;
            light.intensity = 2.4f;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        }

        private void ClearGeneratedObjects()
        {
            List<GameObject> toDestroy = new List<GameObject>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name == GeneratedRootName)
                    toDestroy.Add(child.gameObject);
            }

            foreach (GameObject child in toDestroy)
                DestroyUnityObject(child);
        }

        private CreatePrivateAssetSyncResult SyncPrivateCreateAssets()
        {
            string projectRoot = GetProjectRoot();
            string referenceRepositoryRoot = CreatePrivateAssetProjectPaths.GetReferenceRepositoryRoot(projectRoot);
            string privateCreateAssetRoot = CreatePrivateAssetProjectPaths.GetPrivateCreateAssetRoot(projectRoot);
            if (!Directory.Exists(referenceRepositoryRoot))
            {
                return new CreatePrivateAssetSyncResult(
                    CreateFirstSlicePrivateAssetManifest.Manifest.UniqueFiles.Count,
                    new string[0],
                    new string[0],
                    GetMissingManifestPaths());
            }

            return CreatePrivateAssetSyncService.Sync(
                CreateFirstSlicePrivateAssetManifest.Manifest,
                referenceRepositoryRoot,
                privateCreateAssetRoot);
        }

        private MinecraftModelLoader CreatePrivateModelLoader()
        {
            string privateCreateAssetRoot = CreatePrivateAssetProjectPaths.GetPrivateCreateAssetRoot(GetProjectRoot());
            if (!Directory.Exists(privateCreateAssetRoot))
                return null;

            return new MinecraftModelLoader(privateCreateAssetRoot);
        }

        private MinecraftBlockStateLoader CreatePrivateBlockStateLoader()
        {
            string privateCreateAssetRoot = CreatePrivateAssetProjectPaths.GetPrivateCreateAssetRoot(GetProjectRoot());
            if (!Directory.Exists(privateCreateAssetRoot))
                return null;

            return new MinecraftBlockStateLoader(privateCreateAssetRoot);
        }

        private static string[] GetMissingManifestPaths()
        {
            string[] paths = new string[CreateFirstSlicePrivateAssetManifest.Manifest.UniqueFiles.Count];
            for (int i = 0; i < paths.Length; i++)
                paths[i] = CreateFirstSlicePrivateAssetManifest.Manifest.UniqueFiles[i].RepositoryRelativePath;

            return paths;
        }

        private static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private void DestroyRuntimeAssets()
        {
            foreach (Material material in materialsByKey.Values)
                DestroyUnityObject(material);
            materialsByKey.Clear();

            foreach (Mesh mesh in runtimeModelMeshes)
                DestroyUnityObject(mesh);
            runtimeModelMeshes.Clear();

            foreach (Texture2D texture in createTexturesByPath.Values)
            {
                if (!ReferenceEquals(texture, missingCreateItemTexture))
                    DestroyUnityObject(texture);
            }

            createTexturesByPath.Clear();

            if (runtimeGrassTexture != null)
            {
                DestroyUnityObject(runtimeGrassTexture);
                runtimeGrassTexture = null;
            }

            if (missingCreateItemTexture != null)
            {
                DestroyUnityObject(missingCreateItemTexture);
                missingCreateItemTexture = null;
            }
        }

        private static void DestroyUnityObject(UnityEngine.Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }
    }
}
