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
        private const string PrivateMinecraftTextureDirectory = "PrivateTemp/Minecraft";
        private const string PrivateGrassTopTexturePath = PrivateMinecraftTextureDirectory + "/grass_block_top.png";
        private const string PrivateGrassSideTexturePath = PrivateMinecraftTextureDirectory + "/grass_block_side.png";
        private const string PrivateGrassSideOverlayTexturePath = PrivateMinecraftTextureDirectory + "/grass_block_side_overlay.png";
        private const string PrivateDirtTexturePath = PrivateMinecraftTextureDirectory + "/dirt.png";
        private const string ReferenceMinecraftTextureRoot = "References/Minecraft-1.21.1-resources/assets/minecraft/textures/block";
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
        private const float CreativeMotorDefaultGeneratedSpeedRpm = 16f;

        private static readonly MinecraftModelDisplayTransform DefaultItemModelDisplay =
            new MinecraftModelDisplayTransform(new Vector3(30f, 225f, 0f), Vector3.zero, new Vector3(0.8f, 0.8f, 0.8f));
        private static readonly Color32 DefaultMinecraftGrassTint = new Color32(124, 189, 107, 255);
        private static readonly BlockPos CreativeMotorShowcaseCenterPosition = new BlockPos(24, 1, 8);
        private static readonly ResourceLocation CreativeMotorHalfShaftModelId = ResourceLocation.Parse("create:block/shaft_half");

        private readonly Dictionary<string, Material> materialsByKey = new Dictionary<string, Material>();
        private readonly Dictionary<string, Texture2D> createTexturesByPath = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Texture2D> minecraftTexturesByKey = new Dictionary<string, Texture2D>();
        private readonly List<Mesh> runtimeModelMeshes = new List<Mesh>();
        private readonly List<RotatingVisualState> rotatingVisuals = new List<RotatingVisualState>();
        private Mesh runtimeGrassBlockMesh;
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

        public int GeneratedCreativeMotorShowcasePlatformBlockCount { get; private set; }

        public int GeneratedCreativeMotorShowcaseMotorCount { get; private set; }

        public int GeneratedCreativeMotorShowcaseAnimatedShaftCount { get; private set; }

        private void OnEnable()
        {
            Rebuild();
        }

        private void Update()
        {
            UpdateRotatingVisuals();
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
            GeneratedCreativeMotorShowcasePlatformBlockCount = 0;
            GeneratedCreativeMotorShowcaseMotorCount = 0;
            GeneratedCreativeMotorShowcaseAnimatedShaftCount = 0;

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
            Transform showcaseRoot = CreateChildRoot(root, "Focused Block Showcases");

            foreach (WorldBlockEntry entry in world.GetStoredBlocks())
                CreateBlock(worldRoot, catalog, entry, world, modelLoader, blockStateLoader);

            CreateItemCatalog(itemCatalogRoot, modelLoader);
            CreateBlockCatalog(blockCatalogRoot, modelLoader, blockStateLoader);
            CreateCreativeMotorShowcase(showcaseRoot, catalog, modelLoader, blockStateLoader);

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
            BlockWorld world,
            MinecraftModelLoader modelLoader,
            MinecraftBlockStateLoader blockStateLoader)
        {
            if (TryCreateStateDrivenWorldBlock(root, catalog, entry, modelLoader, blockStateLoader))
            {
                GeneratedStateDrivenWorldBlockCount++;
                GeneratedBlockCount++;
                return;
            }

            CreatePlaceholderWorldBlock(root, catalog, entry, world);
            GeneratedBlockCount++;
        }

        private bool TryCreateStateDrivenWorldBlock(
            Transform root,
            DemoContentCatalog catalog,
            WorldBlockEntry entry,
            MinecraftModelLoader modelLoader,
            MinecraftBlockStateLoader blockStateLoader)
        {
            if (entry.State.Definition.Id == catalog.CreativeMotor.Id)
                return TryCreateCreativeMotorWorldBlock(root, catalog, entry, modelLoader, blockStateLoader);

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

        private void CreatePlaceholderWorldBlock(Transform root, DemoContentCatalog catalog, WorldBlockEntry entry, BlockWorld world)
        {
            if (entry.State.Definition.Id == catalog.Surface.Id)
            {
                CreateGrassSurfaceBlock(root, catalog, entry, world);
                return;
            }

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

        private void CreateGrassSurfaceBlock(Transform root, DemoContentCatalog catalog, WorldBlockEntry entry, BlockWorld world)
        {
            bool hasUp = ShouldCullGrassTop(catalog, world.GetBlockState(entry.Position.Relative(Direction.Up)));
            bool hasDown = !world.IsAir(world.GetBlockState(entry.Position.Relative(Direction.Down)));
            bool hasNorth = !world.IsAir(world.GetBlockState(entry.Position.Relative(Direction.North)));
            bool hasSouth = !world.IsAir(world.GetBlockState(entry.Position.Relative(Direction.South)));
            bool hasWest = !world.IsAir(world.GetBlockState(entry.Position.Relative(Direction.West)));
            bool hasEast = !world.IsAir(world.GetBlockState(entry.Position.Relative(Direction.East)));

            Mesh mesh = CreateGrassBlockMeshCulled(hasUp, hasDown, hasNorth, hasSouth, hasWest, hasEast);
            if (mesh == null)
                return;

            GameObject block = new GameObject(GetDisplayName(catalog, entry));
            block.transform.SetParent(root, false);
            block.transform.localPosition = ToUnityPosition(entry.Position);

            MeshFilter meshFilter = block.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = block.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterials = new[]
            {
                GetMaterial("surface_grass_top_tinted", Color.white, LoadTintedGrassTopTexture()),
                GetMaterial("surface_dirt_bottom", Color.white, LoadMinecraftTexture("surface_dirt_bottom", PrivateDirtTexturePath, "dirt.png")),
                GetMaterial("surface_grass_side_tinted", Color.white, LoadTintedGrassSideTexture())
            };
        }

        private static bool ShouldCullGrassTop(DemoContentCatalog catalog, BlockState stateAbove)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (stateAbove == null)
                return false;

            ResourceLocation id = stateAbove.Definition.Id;
            return id == catalog.Surface.Id ||
                id == catalog.CreativeCrate.Id ||
                id == catalog.ItemVault.Id;
        }

        private bool TryCreateCreativeMotorWorldBlock(
            Transform root,
            DemoContentCatalog catalog,
            WorldBlockEntry entry,
            MinecraftModelLoader modelLoader,
            MinecraftBlockStateLoader blockStateLoader)
        {
            Direction facing = entry.State.Get(DemoContentCatalog.FacingProperty);
            bool createdShaft;
            if (!TryCreateCreativeMotorAssembly(
                    root,
                    GetDisplayName(catalog, entry),
                    GetLabel(catalog, entry.State),
                    ToUnityPosition(entry.Position),
                    facing,
                    WorldBlockModelBaseScale,
                    modelLoader,
                    blockStateLoader,
                    true,
                    out createdShaft))
            {
                FailedStateDrivenWorldBlockCount++;
                return false;
            }

            return true;
        }

        private void CreateCreativeMotorShowcase(
            Transform root,
            DemoContentCatalog catalog,
            MinecraftModelLoader modelLoader,
            MinecraftBlockStateLoader blockStateLoader)
        {
            Transform showcaseRoot = CreateChildRoot(root, "Creative Motor Showcase");
            AddFloatingLabel(
                showcaseRoot,
                "Creative Motor Focus",
                new Vector3(
                    CreativeMotorShowcaseCenterPosition.X + 0.5f,
                    CreativeMotorShowcaseCenterPosition.Y + 2.05f,
                    CreativeMotorShowcaseCenterPosition.Z + 0.5f),
                0.16f);

            BlockWorld showcaseWorld = new BlockWorld(catalog.Air.DefaultState);
            for (int x = CreativeMotorShowcaseCenterPosition.X - 1; x <= CreativeMotorShowcaseCenterPosition.X + 1; x++)
            {
                for (int z = CreativeMotorShowcaseCenterPosition.Z - 1; z <= CreativeMotorShowcaseCenterPosition.Z + 1; z++)
                    showcaseWorld.SetBlockState(new BlockPos(x, 0, z), catalog.Surface.DefaultState);
            }

            BlockState showcaseMotorState =
                catalog.CreativeMotor.DefaultState.With(DemoContentCatalog.FacingProperty, Direction.North);
            showcaseWorld.SetBlockState(CreativeMotorShowcaseCenterPosition, showcaseMotorState);

            for (int x = CreativeMotorShowcaseCenterPosition.X - 1; x <= CreativeMotorShowcaseCenterPosition.X + 1; x++)
            {
                for (int z = CreativeMotorShowcaseCenterPosition.Z - 1; z <= CreativeMotorShowcaseCenterPosition.Z + 1; z++)
                {
                    WorldBlockEntry grassEntry = new WorldBlockEntry(new BlockPos(x, 0, z), catalog.Surface.DefaultState);
                    CreateGrassSurfaceBlock(showcaseRoot, catalog, grassEntry, showcaseWorld);
                    GeneratedCreativeMotorShowcasePlatformBlockCount++;
                }
            }

            bool createdShaft;
            if (TryCreateCreativeMotorAssembly(
                    showcaseRoot,
                    "Creative Motor Showcase",
                    "Creative Motor",
                    ToUnityPosition(CreativeMotorShowcaseCenterPosition),
                    Direction.North,
                    WorldBlockModelBaseScale,
                    modelLoader,
                    blockStateLoader,
                    true,
                    out createdShaft))
            {
                GeneratedCreativeMotorShowcaseMotorCount++;
                if (createdShaft)
                    GeneratedCreativeMotorShowcaseAnimatedShaftCount++;
            }
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

        private bool TryCreateCreativeMotorAssembly(
            Transform root,
            string name,
            string label,
            Vector3 localPosition,
            Direction facing,
            float baseScale,
            MinecraftModelLoader modelLoader,
            MinecraftBlockStateLoader blockStateLoader,
            bool animateShaft,
            out bool createdShaft)
        {
            createdShaft = false;
            if (modelLoader == null || blockStateLoader == null)
                return false;

            GameObject motorRoot = new GameObject(name);
            motorRoot.transform.SetParent(root, false);
            motorRoot.transform.localPosition = localPosition;

            BlockStatePropertyValue[] visualProperties =
            {
                new BlockStatePropertyValue("facing", DemoContentCatalog.FacingProperty.SerializeValue(facing))
            };

            if (!TryCreateCombinedBlockModel(
                    motorRoot.transform,
                    ResourceLocation.Parse("create:creative_motor"),
                    visualProperties,
                    baseScale,
                    modelLoader,
                    blockStateLoader))
            {
                DestroyUnityObject(motorRoot);
                return false;
            }

            createdShaft = TryCreateCreativeMotorHalfShaft(motorRoot.transform, facing, baseScale, modelLoader, animateShaft);
            AddLabel(motorRoot.transform, label);
            return true;
        }

        private bool TryCreateCombinedBlockModel(
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
                if (!TryCreateCombinedResolvedModel(modelRoot, model))
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

        private bool TryCreateCreativeMotorHalfShaft(
            Transform root,
            Direction facing,
            float baseScale,
            MinecraftModelLoader modelLoader,
            bool animate)
        {
            try
            {
                MinecraftResolvedModel model = modelLoader.LoadModel(CreativeMotorHalfShaftModelId);
                if (model.Elements.Count == 0)
                    return false;

                Transform facingRoot = CreateChildRoot(root, "Shaft Half");
                facingRoot.localRotation = Quaternion.FromToRotation(Vector3.forward, ToUnityDirectionVector(facing));
                facingRoot.localScale = Vector3.one * baseScale;

                Transform spinRoot = CreateChildRoot(facingRoot, animate ? "Spin" : "Model");
                if (!TryCreateCombinedResolvedModel(spinRoot, model))
                {
                    DestroyUnityObject(facingRoot.gameObject);
                    return false;
                }

                if (animate)
                    rotatingVisuals.Add(new RotatingVisualState(spinRoot, GetCreativeMotorAnimationDegreesPerSecond(facing)));

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool TryCreateCombinedResolvedModel(Transform root, MinecraftResolvedModel model)
        {
            Dictionary<ResourceLocation, CombinedMeshBuilder> builders =
                new Dictionary<ResourceLocation, CombinedMeshBuilder>();
            int faceCount = 0;
            foreach (MinecraftModelElement element in model.Elements)
            {
                foreach (MinecraftModelFace face in element.Faces.Values)
                {
                    CombinedMeshBuilder builder;
                    if (!builders.TryGetValue(face.TextureId, out builder))
                    {
                        builder = new CombinedMeshBuilder();
                        builders.Add(face.TextureId, builder);
                    }

                    builder.AddQuad(CreateModelFaceVertices(element, face.Direction), CreateModelFaceUvs(face, model.TextureSize));
                    faceCount++;
                }
            }

            if (faceCount == 0)
                return false;

            foreach (KeyValuePair<ResourceLocation, CombinedMeshBuilder> pair in builders)
                CreateCombinedMeshObject(root, pair.Key, pair.Value);

            return true;
        }

        private void CreateCombinedMeshObject(Transform root, ResourceLocation textureId, CombinedMeshBuilder builder)
        {
            GameObject meshObject = new GameObject("Combined " + textureId.Path);
            meshObject.transform.SetParent(root, false);

            MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
            Mesh mesh = new Mesh();
            mesh.name = "Combined " + textureId.Path;
            mesh.vertices = builder.Vertices.ToArray();
            mesh.uv = builder.Uvs.ToArray();
            mesh.triangles = builder.Triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            runtimeModelMeshes.Add(mesh);
            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = GetCreateTextureMaterial(textureId);
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
            mesh.triangles = CreateSingleSidedQuadTriangles();
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
                        new Vector3(to.x, from.y, from.z),
                        new Vector3(from.x, from.y, from.z),
                        new Vector3(from.x, to.y, from.z),
                        new Vector3(to.x, to.y, from.z)
                    };
                    break;
                case Direction.South:
                    vertices = new[]
                    {
                        new Vector3(from.x, from.y, to.z),
                        new Vector3(to.x, from.y, to.z),
                        new Vector3(to.x, to.y, to.z),
                        new Vector3(from.x, to.y, to.z)
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

        private static int[] CreateSingleSidedQuadTriangles()
        {
            return new[]
            {
                0, 1, 2,
                0, 2, 3
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
                return GetMaterial("surface_grass_side_tinted", Color.white, LoadTintedGrassSideTexture());
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

            material = new Material(FindLitShader());
            material.name = "Demo " + key;
            ConfigureCreateModelMaterial(material, LoadPrivateCreateTexture(textureId));
            materialsByKey.Add(key, material);
            return material;
        }

        private static void ConfigureCreateModelMaterial(Material material, Texture2D texture)
        {
            if (material == null)
                throw new ArgumentNullException(nameof(material));

            material.color = Color.white;

            if (texture != null)
            {
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.anisoLevel = 0;

                material.mainTexture = texture;
                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_MainTex"))
                    material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", Color.white);
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 0f);
            if (material.HasProperty("_AlphaClip"))
                material.SetFloat("_AlphaClip", 1f);
            if (material.HasProperty("_Cutoff"))
                material.SetFloat("_Cutoff", 0.1f);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 1f);

            material.EnableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 2450;
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

        private Texture2D LoadMinecraftTexture(string cacheKey, string privateRelativePath, string referenceFileName)
        {
            Texture2D existingTexture;
            if (minecraftTexturesByKey.TryGetValue(cacheKey, out existingTexture))
                return existingTexture;

            string texturePath = ResolveMinecraftTexturePath(privateRelativePath, referenceFileName);
            if (texturePath != null)
            {
                byte[] data = File.ReadAllBytes(texturePath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (texture.LoadImage(data))
                {
                    ConfigureLoadedTexture(texture, TextureWrapMode.Repeat);
                    texture.name = "Minecraft " + cacheKey;
                    minecraftTexturesByKey.Add(cacheKey, texture);
                    return texture;
                }

                DestroyUnityObject(texture);
            }

            Texture2D fallbackTexture;
            if (cacheKey == "surface_dirt_bottom")
                fallbackTexture = CreateFallbackDirtTexture();
            else if (cacheKey == "surface_grass_side_overlay_source")
                fallbackTexture = CreateFallbackTransparentTexture();
            else
                fallbackTexture = CreateFallbackGrassTexture();

            fallbackTexture.name = "Fallback " + cacheKey;
            minecraftTexturesByKey.Add(cacheKey, fallbackTexture);
            return fallbackTexture;
        }

        private Texture2D LoadTintedGrassTopTexture()
        {
            const string outputKey = "surface_grass_top_tinted";
            Texture2D existingTexture;
            if (minecraftTexturesByKey.TryGetValue(outputKey, out existingTexture))
                return existingTexture;

            Texture2D sourceTexture = LoadMinecraftTexture("surface_grass_top_source", PrivateGrassTopTexturePath, "grass_block_top.png");
            Texture2D tintedTexture = CreateTintedTexture(sourceTexture, DefaultMinecraftGrassTint, outputKey);
            minecraftTexturesByKey.Add(outputKey, tintedTexture);
            return tintedTexture;
        }

        private Texture2D LoadTintedGrassSideTexture()
        {
            const string outputKey = "surface_grass_side_tinted";
            Texture2D existingTexture;
            if (minecraftTexturesByKey.TryGetValue(outputKey, out existingTexture))
                return existingTexture;

            Texture2D baseTexture = LoadMinecraftTexture("surface_grass_side_source", PrivateGrassSideTexturePath, "grass_block_side.png");
            Texture2D overlayTexture = LoadMinecraftTexture("surface_grass_side_overlay_source", PrivateGrassSideOverlayTexturePath, "grass_block_side_overlay.png");
            Texture2D tintedTexture = CreateTintedOverlayTexture(baseTexture, overlayTexture, DefaultMinecraftGrassTint, outputKey);
            minecraftTexturesByKey.Add(outputKey, tintedTexture);
            return tintedTexture;
        }

        private static string ResolveMinecraftTexturePath(string privateRelativePath, string referenceFileName)
        {
            string privatePath = Path.Combine(Application.dataPath, privateRelativePath);
            if (File.Exists(privatePath))
                return privatePath;

            string referencePath = Path.Combine(GetProjectRoot(), ReferenceMinecraftTextureRoot, referenceFileName);
            return File.Exists(referencePath) ? referencePath : null;
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

            ConfigureLoadedTexture(texture, TextureWrapMode.Repeat);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateFallbackTransparentTexture()
        {
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                    texture.SetPixel(x, y, clear);
            }

            ConfigureLoadedTexture(texture, TextureWrapMode.Repeat);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateFallbackDirtTexture()
        {
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color dark = new Color(0.33f, 0.23f, 0.12f, 1f);
            Color light = new Color(0.46f, 0.31f, 0.18f, 1f);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                    texture.SetPixel(x, y, ((x + (y * 2)) % 5 == 0) ? light : dark);
            }

            ConfigureLoadedTexture(texture, TextureWrapMode.Repeat);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateTintedTexture(Texture2D sourceTexture, Color32 tint, string outputKey)
        {
            Texture2D texture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
            Color32[] sourcePixels = sourceTexture.GetPixels32();
            Color32[] tintedPixels = new Color32[sourcePixels.Length];
            for (int i = 0; i < sourcePixels.Length; i++)
            {
                Color32 sourcePixel = sourcePixels[i];
                tintedPixels[i] = new Color32(
                    MultiplyByte(sourcePixel.r, tint.r),
                    MultiplyByte(sourcePixel.g, tint.g),
                    MultiplyByte(sourcePixel.b, tint.b),
                    sourcePixel.a);
            }

            texture.SetPixels32(tintedPixels);
            ConfigureLoadedTexture(texture, TextureWrapMode.Repeat);
            texture.name = "Minecraft " + outputKey;
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateTintedOverlayTexture(Texture2D baseTexture, Texture2D overlayTexture, Color32 tint, string outputKey)
        {
            Texture2D texture = new Texture2D(baseTexture.width, baseTexture.height, TextureFormat.RGBA32, false);
            Color32[] basePixels = baseTexture.GetPixels32();
            Color32[] overlayPixels = overlayTexture.GetPixels32();
            Color32[] compositePixels = new Color32[basePixels.Length];
            for (int i = 0; i < basePixels.Length; i++)
            {
                Color32 basePixel = basePixels[i];
                Color32 overlayPixel = overlayPixels[i];
                byte overlayR = MultiplyByte(overlayPixel.r, tint.r);
                byte overlayG = MultiplyByte(overlayPixel.g, tint.g);
                byte overlayB = MultiplyByte(overlayPixel.b, tint.b);
                compositePixels[i] = AlphaBlend(basePixel, new Color32(overlayR, overlayG, overlayB, overlayPixel.a));
            }

            texture.SetPixels32(compositePixels);
            ConfigureLoadedTexture(texture, TextureWrapMode.Repeat);
            texture.name = "Minecraft " + outputKey;
            texture.Apply();
            return texture;
        }

        private static Color32 AlphaBlend(Color32 basePixel, Color32 overlayPixel)
        {
            int overlayAlpha = overlayPixel.a;
            if (overlayAlpha == 0)
                return basePixel;

            int inverseAlpha = 255 - overlayAlpha;
            byte red = (byte)(((basePixel.r * inverseAlpha) + (overlayPixel.r * overlayAlpha)) / 255);
            byte green = (byte)(((basePixel.g * inverseAlpha) + (overlayPixel.g * overlayAlpha)) / 255);
            byte blue = (byte)(((basePixel.b * inverseAlpha) + (overlayPixel.b * overlayAlpha)) / 255);
            return new Color32(red, green, blue, basePixel.a);
        }

        private static byte MultiplyByte(byte left, byte right)
        {
            return (byte)((left * right) / 255);
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

            EnsureFreeCameraController(camera);
            camera.transform.position = new Vector3(8f, 12.5f, -11.5f);
            camera.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
        }

        private static void EnsureFreeCameraController(Camera camera)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));

            if (camera.GetComponent<DemoFreeCameraController>() == null)
                camera.gameObject.AddComponent<DemoFreeCameraController>();
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

        private void UpdateRotatingVisuals()
        {
            if (rotatingVisuals.Count == 0)
                return;

            float timeSeconds = Application.isPlaying
                ? Time.realtimeSinceStartup
                : (float)(DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond);

            foreach (RotatingVisualState rotatingVisual in rotatingVisuals)
            {
                if (rotatingVisual.Transform == null)
                    continue;

                float angle = Mathf.Repeat(timeSeconds * rotatingVisual.DegreesPerSecond, 360f);
                rotatingVisual.Transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                UnityEditor.SceneView.RepaintAll();
            }
#endif
        }

        private static Vector3 ToUnityDirectionVector(Direction direction)
        {
            switch (direction)
            {
                case Direction.Down:
                    return Vector3.down;
                case Direction.Up:
                    return Vector3.up;
                case Direction.North:
                    return Vector3.back;
                case Direction.South:
                    return Vector3.forward;
                case Direction.West:
                    return Vector3.left;
                case Direction.East:
                    return Vector3.right;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        private static float GetCreativeMotorAnimationDegreesPerSecond(Direction facing)
        {
            float degreesPerSecond = ConvertCreateSpeedToDegreesPerSecond(CreativeMotorDefaultGeneratedSpeedRpm);
            return IsPositiveAxisDirection(facing) ? degreesPerSecond : -degreesPerSecond;
        }

        private static float ConvertCreateSpeedToDegreesPerSecond(float speedRpm)
        {
            return speedRpm * 360f / 60f;
        }

        private static bool IsPositiveAxisDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                case Direction.South:
                case Direction.East:
                    return true;
                case Direction.Down:
                case Direction.North:
                case Direction.West:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        private Mesh CreateGrassBlockMeshCulled(
            bool neighborUp,
            bool neighborDown,
            bool neighborNorth,
            bool neighborSouth,
            bool neighborWest,
            bool neighborEast)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> topTriangles = new List<int>();
            List<int> bottomTriangles = new List<int>();
            List<int> sideTriangles = new List<int>();

            // Unity is left-handed: front face = clockwise winding from outside.
            // Each quad uses {base, base+1, base+2, base, base+2, base+3}.

            // Top face (submesh 0 — grass_top). CW from above.
            if (!neighborUp)
            {
                int b = vertices.Count;
                vertices.Add(new Vector3(-0.5f, 0.5f, 0.5f));
                vertices.Add(new Vector3(0.5f, 0.5f, 0.5f));
                vertices.Add(new Vector3(0.5f, 0.5f, -0.5f));
                vertices.Add(new Vector3(-0.5f, 0.5f, -0.5f));
                normals.Add(Vector3.up); normals.Add(Vector3.up); normals.Add(Vector3.up); normals.Add(Vector3.up);
                uvs.Add(new Vector2(0f, 1f)); uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(1f, 0f)); uvs.Add(new Vector2(0f, 0f));
                topTriangles.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }

            // Bottom face (submesh 1 — dirt). CW from below.
            if (!neighborDown)
            {
                int b = vertices.Count;
                vertices.Add(new Vector3(-0.5f, -0.5f, -0.5f));
                vertices.Add(new Vector3(0.5f, -0.5f, -0.5f));
                vertices.Add(new Vector3(0.5f, -0.5f, 0.5f));
                vertices.Add(new Vector3(-0.5f, -0.5f, 0.5f));
                normals.Add(Vector3.down); normals.Add(Vector3.down); normals.Add(Vector3.down); normals.Add(Vector3.down);
                uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(1f, 0f)); uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(0f, 1f));
                bottomTriangles.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }

            // North face -Z (submesh 2 — grass_side). CW from -Z.
            if (!neighborNorth)
            {
                int b = vertices.Count;
                vertices.Add(new Vector3(0.5f, -0.5f, -0.5f));
                vertices.Add(new Vector3(-0.5f, -0.5f, -0.5f));
                vertices.Add(new Vector3(-0.5f, 0.5f, -0.5f));
                vertices.Add(new Vector3(0.5f, 0.5f, -0.5f));
                normals.Add(Vector3.back); normals.Add(Vector3.back); normals.Add(Vector3.back); normals.Add(Vector3.back);
                uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(1f, 0f)); uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(0f, 1f));
                sideTriangles.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }

            // South face +Z (submesh 2 — grass_side). CW from +Z.
            if (!neighborSouth)
            {
                int b = vertices.Count;
                vertices.Add(new Vector3(-0.5f, -0.5f, 0.5f));
                vertices.Add(new Vector3(0.5f, -0.5f, 0.5f));
                vertices.Add(new Vector3(0.5f, 0.5f, 0.5f));
                vertices.Add(new Vector3(-0.5f, 0.5f, 0.5f));
                normals.Add(Vector3.forward); normals.Add(Vector3.forward); normals.Add(Vector3.forward); normals.Add(Vector3.forward);
                uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(1f, 0f)); uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(0f, 1f));
                sideTriangles.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }

            // West face -X (submesh 2 — grass_side). CW from -X.
            if (!neighborWest)
            {
                int b = vertices.Count;
                vertices.Add(new Vector3(-0.5f, -0.5f, -0.5f));
                vertices.Add(new Vector3(-0.5f, -0.5f, 0.5f));
                vertices.Add(new Vector3(-0.5f, 0.5f, 0.5f));
                vertices.Add(new Vector3(-0.5f, 0.5f, -0.5f));
                normals.Add(Vector3.left); normals.Add(Vector3.left); normals.Add(Vector3.left); normals.Add(Vector3.left);
                uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(1f, 0f)); uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(0f, 1f));
                sideTriangles.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }

            // East face +X (submesh 2 — grass_side). CW from +X.
            if (!neighborEast)
            {
                int b = vertices.Count;
                vertices.Add(new Vector3(0.5f, -0.5f, 0.5f));
                vertices.Add(new Vector3(0.5f, -0.5f, -0.5f));
                vertices.Add(new Vector3(0.5f, 0.5f, -0.5f));
                vertices.Add(new Vector3(0.5f, 0.5f, 0.5f));
                normals.Add(Vector3.right); normals.Add(Vector3.right); normals.Add(Vector3.right); normals.Add(Vector3.right);
                uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(1f, 0f)); uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(0f, 1f));
                sideTriangles.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }

            if (vertices.Count == 0)
                return null;

            Mesh mesh = new Mesh();
            mesh.name = "Grass Block Culled";
            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.subMeshCount = 3;
            mesh.SetTriangles(topTriangles.ToArray(), 0);
            mesh.SetTriangles(bottomTriangles.ToArray(), 1);
            mesh.SetTriangles(sideTriangles.ToArray(), 2);
            mesh.RecalculateBounds();
            runtimeModelMeshes.Add(mesh);
            return mesh;
        }

        private Mesh GetRuntimeGrassBlockMesh()
        {
            if (runtimeGrassBlockMesh != null)
                return runtimeGrassBlockMesh;

            Mesh mesh = new Mesh();
            mesh.name = "Runtime Grass Block";
            mesh.vertices = new[]
            {
                // Top (CW from above)
                new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),

                // Bottom (CW from below)
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),

                // North -Z (CW from -Z)
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),

                // South +Z (CW from +Z)
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),

                // West -X (CW from -X)
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),

                // East +X (CW from +X)
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, 0.5f)
            };
            mesh.normals = new[]
            {
                Vector3.up, Vector3.up, Vector3.up, Vector3.up,
                Vector3.down, Vector3.down, Vector3.down, Vector3.down,
                Vector3.back, Vector3.back, Vector3.back, Vector3.back,
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
                Vector3.left, Vector3.left, Vector3.left, Vector3.left,
                Vector3.right, Vector3.right, Vector3.right, Vector3.right
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
            };
            mesh.subMeshCount = 3;
            mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            mesh.SetTriangles(new[] { 4, 5, 6, 4, 6, 7 }, 1);
            mesh.SetTriangles(
                new[]
                {
                    8, 9, 10, 8, 10, 11,
                    12, 13, 14, 12, 14, 15,
                    16, 17, 18, 16, 18, 19,
                    20, 21, 22, 20, 22, 23
                },
                2);
            mesh.RecalculateBounds();
            runtimeGrassBlockMesh = mesh;
            return runtimeGrassBlockMesh;
        }

        private void DestroyRuntimeAssets()
        {
            rotatingVisuals.Clear();

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

            foreach (Texture2D texture in minecraftTexturesByKey.Values)
                DestroyUnityObject(texture);
            minecraftTexturesByKey.Clear();

            if (runtimeGrassBlockMesh != null)
            {
                DestroyUnityObject(runtimeGrassBlockMesh);
                runtimeGrassBlockMesh = null;
            }

            if (missingCreateItemTexture != null)
            {
                DestroyUnityObject(missingCreateItemTexture);
                missingCreateItemTexture = null;
            }
        }

        private sealed class CombinedMeshBuilder
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector2> uvs = new List<Vector2>();
            private readonly List<int> triangles = new List<int>();

            public List<Vector3> Vertices
            {
                get { return vertices; }
            }

            public List<Vector2> Uvs
            {
                get { return uvs; }
            }

            public List<int> Triangles
            {
                get { return triangles; }
            }

            public void AddQuad(IReadOnlyList<Vector3> quadVertices, IReadOnlyList<Vector2> quadUvs)
            {
                int baseIndex = vertices.Count;
                for (int i = 0; i < quadVertices.Count; i++)
                {
                    vertices.Add(quadVertices[i]);
                    uvs.Add(quadUvs[i]);
                }

                triangles.Add(baseIndex + 0);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 0);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 3);
            }
        }

        private sealed class RotatingVisualState
        {
            public RotatingVisualState(Transform transform, float degreesPerSecond)
            {
                Transform = transform;
                DegreesPerSecond = degreesPerSecond;
            }

            public Transform Transform { get; }

            public float DegreesPerSecond { get; }
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
