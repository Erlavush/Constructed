using System;
using System.Collections.Generic;
using System.Globalization;
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
        private const string PlayerRootName = DemoMinecraftFirstPersonController.PlayerRootName;
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
        private const float BeltMagicScrollMultiplier = 1f / (31.5f * 16f);
        private const float BeltTicksPerSecond = 20f;
        private const float BeltScrollFactorDiagonal = 3f / 8f;
        private const float BeltScrollFactorOtherwise = 0.5f;
        private const float BeltScrollOffsetBottom = 0.5f;
        private const float BeltScrollOffsetOtherwise = 0f;

        private static readonly MinecraftModelDisplayTransform DefaultItemModelDisplay =
            new MinecraftModelDisplayTransform(new Vector3(30f, 225f, 0f), Vector3.zero, new Vector3(0.8f, 0.8f, 0.8f));
        private static readonly Color32 DefaultMinecraftGrassTint = new Color32(124, 189, 107, 255);
        private static readonly ResourceLocation CreativeMotorHalfShaftModelId = ResourceLocation.Parse("create:block/shaft_half");
        private static readonly ResourceLocation BeltTopTextureId = ResourceLocation.Parse("create:block/belt");
        private static readonly ResourceLocation BeltBottomTextureId = ResourceLocation.Parse("create:block/belt_offset");
        private static readonly ResourceLocation BeltDiagonalTextureId = ResourceLocation.Parse("create:block/belt_diagonal");
        private static readonly ResourceLocation BeltTopScrollTextureId = ResourceLocation.Parse("create:block/belt_scroll");
        private static readonly ResourceLocation BeltDiagonalScrollTextureId = ResourceLocation.Parse("create:block/belt_diagonal_scroll");
        private static readonly ResourceLocation BeltStartModelId = ResourceLocation.Parse("create:block/belt/start");
        private static readonly ResourceLocation BeltMiddleModelId = ResourceLocation.Parse("create:block/belt/middle");
        private static readonly ResourceLocation BeltEndModelId = ResourceLocation.Parse("create:block/belt/end");
        private static readonly ResourceLocation BeltStartBottomModelId = ResourceLocation.Parse("create:block/belt/start_bottom");
        private static readonly ResourceLocation BeltMiddleBottomModelId = ResourceLocation.Parse("create:block/belt/middle_bottom");
        private static readonly ResourceLocation BeltEndBottomModelId = ResourceLocation.Parse("create:block/belt/end_bottom");
        private static readonly ResourceLocation BeltDiagonalStartModelId = ResourceLocation.Parse("create:block/belt/diagonal_start");
        private static readonly ResourceLocation BeltDiagonalMiddleModelId = ResourceLocation.Parse("create:block/belt/diagonal_middle");
        private static readonly ResourceLocation BeltDiagonalEndModelId = ResourceLocation.Parse("create:block/belt/diagonal_end");

        private readonly Dictionary<string, Material> materialsByKey = new Dictionary<string, Material>();
        private readonly Dictionary<string, AnimatedBeltMaterialState> animatedBeltMaterialsByKey = new Dictionary<string, AnimatedBeltMaterialState>();
        private readonly Dictionary<string, Texture2D> createTexturesByPath = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Texture2D> minecraftTexturesByKey = new Dictionary<string, Texture2D>();
        private readonly List<Mesh> runtimeModelMeshes = new List<Mesh>();
        private readonly List<RotatingVisualState> rotatingVisuals = new List<RotatingVisualState>();
        private Mesh runtimeGrassBlockMesh;
        private Texture2D missingCreateItemTexture;
        private DemoContentCatalog runtimeCatalog;
        private BlockWorld runtimeWorld;

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

        public int GeneratedAnimatedMotorOutputCount { get; private set; }

        public int GeneratedAnimatedShaftCount { get; private set; }

        public int GeneratedAnimatedBeltSegmentCount { get; private set; }

        public DemoContentCatalog Catalog
        {
            get
            {
                EnsureDemoWorldInitialized();
                return runtimeCatalog;
            }
        }

        public BlockWorld World
        {
            get
            {
                EnsureDemoWorldInitialized();
                return runtimeWorld;
            }
        }

        private void OnEnable()
        {
            Rebuild();
        }

        private void Update()
        {
            float timeSeconds = GetAnimationTimeSeconds();
            UpdateAnimatedBeltMaterials(timeSeconds);
            UpdateRotatingVisuals(timeSeconds);

#if UNITY_EDITOR
            if (!Application.isPlaying && (rotatingVisuals.Count > 0 || animatedBeltMaterialsByKey.Count > 0))
            {
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                UnityEditor.SceneView.RepaintAll();
            }
#endif
        }

        public void Rebuild()
        {
            EnsureDemoWorldInitialized();
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
            GeneratedAnimatedMotorOutputCount = 0;
            GeneratedAnimatedShaftCount = 0;
            GeneratedAnimatedBeltSegmentCount = 0;

            DemoKineticSnapshot kineticSnapshot = DemoKineticResolver.Resolve(runtimeWorld, runtimeCatalog);
            DemoBeltRuntimeSnapshot beltSnapshot = DemoBeltRuntimeResolver.Resolve(runtimeWorld, runtimeCatalog, kineticSnapshot);
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

            foreach (WorldBlockEntry entry in runtimeWorld.GetStoredBlocks())
            {
                CreateBlock(
                    worldRoot,
                    runtimeCatalog,
                    entry,
                    runtimeWorld,
                    kineticSnapshot,
                    beltSnapshot,
                    modelLoader,
                    blockStateLoader);
            }

            ConfigureCamera();
            ConfigureLight();
        }

        public bool TryPlaceBlock(ResourceLocation blockId, BlockPos position, Direction nearestLookingDirection)
        {
            EnsureDemoWorldInitialized();
            if (!DemoKineticPlacementRules.IsPlaceableBlock(runtimeCatalog, blockId))
                return false;
            if (!runtimeWorld.IsAir(runtimeWorld.GetBlockState(position)))
                return false;

            BlockState placementState =
                DemoKineticPlacementRules.CreatePlacementState(runtimeCatalog, runtimeWorld, blockId, position, nearestLookingDirection);
            runtimeWorld.SetBlockState(position, placementState);
            Rebuild();
            return true;
        }

        public bool TryRemoveBlock(BlockPos position)
        {
            EnsureDemoWorldInitialized();
            BlockState state = runtimeWorld.GetBlockState(position);
            if (runtimeWorld.IsAir(state))
                return false;

            runtimeWorld.RemoveBlock(position);
            Rebuild();
            return true;
        }

        private void EnsureDemoWorldInitialized()
        {
            if (runtimeCatalog == null)
                runtimeCatalog = DemoContentCatalog.Create();
            if (runtimeWorld == null)
                runtimeWorld = DemoVerticalSliceBootstrap.CreateVerticalSliceWorld(runtimeCatalog);
        }

        private Transform CreateGeneratedRoot()
        {
            GameObject root = new GameObject(GeneratedRootName);
            root.hideFlags = HideFlags.DontSave;
            root.transform.SetParent(transform, false);
            return root.transform;
        }

        private static Transform CreateChildRoot(Transform parent, string name)
        {
            GameObject root = new GameObject(name);
            root.hideFlags = HideFlags.DontSave;
            root.transform.SetParent(parent, false);
            return root.transform;
        }

        private void CreateBlock(
            Transform root,
            DemoContentCatalog catalog,
            WorldBlockEntry entry,
            BlockWorld world,
            DemoKineticSnapshot kineticSnapshot,
            DemoBeltRuntimeSnapshot beltSnapshot,
            MinecraftModelLoader modelLoader,
            MinecraftBlockStateLoader blockStateLoader)
        {
            if (TryCreateStateDrivenWorldBlock(root, catalog, entry, kineticSnapshot, beltSnapshot, modelLoader, blockStateLoader))
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
            DemoKineticSnapshot kineticSnapshot,
            DemoBeltRuntimeSnapshot beltSnapshot,
            MinecraftModelLoader modelLoader,
            MinecraftBlockStateLoader blockStateLoader)
        {
            if (entry.State.Definition.Id == catalog.CreativeMotor.Id)
                return TryCreateCreativeMotorWorldBlock(root, catalog, entry, kineticSnapshot, modelLoader, blockStateLoader);
            if (entry.State.Definition.Id == catalog.Shaft.Id)
                return TryCreateShaftWorldBlock(root, catalog, entry, kineticSnapshot, modelLoader, blockStateLoader);
            if (entry.State.Definition.Id == catalog.Belt.Id)
                return TryCreateBeltWorldBlock(root, catalog, entry, beltSnapshot, modelLoader, blockStateLoader);

            if (!CreateFirstSliceWorldBlockVisualStateBridge.TryResolve(entry.State, out BlockStatePropertyValue[] visualProperties))
                return false;

            GameObject blockRoot = new GameObject(GetDisplayName(catalog, entry));
            blockRoot.hideFlags = HideFlags.DontSave;
            blockRoot.transform.SetParent(root, false);
            blockRoot.transform.localPosition = ToUnityPosition(entry.Position);

            if (!TryCreateBlockModel(blockRoot.transform, entry.State.Definition.Id, visualProperties, WorldBlockModelBaseScale, modelLoader, blockStateLoader))
            {
                FailedStateDrivenWorldBlockCount++;
                DestroyUnityObject(blockRoot);
                return false;
            }

            AddMeshColliders(blockRoot);
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
        }

        private void CreateGrassSurfaceBlock(Transform root, DemoContentCatalog catalog, WorldBlockEntry entry, BlockWorld world)
        {
            bool hasUp = ShouldCullGrassFace(catalog, world.GetBlockState(entry.Position.Relative(Direction.Up)));
            bool hasDown = ShouldCullGrassFace(catalog, world.GetBlockState(entry.Position.Relative(Direction.Down)));
            bool hasNorth = ShouldCullGrassFace(catalog, world.GetBlockState(entry.Position.Relative(Direction.North)));
            bool hasSouth = ShouldCullGrassFace(catalog, world.GetBlockState(entry.Position.Relative(Direction.South)));
            bool hasWest = ShouldCullGrassFace(catalog, world.GetBlockState(entry.Position.Relative(Direction.West)));
            bool hasEast = ShouldCullGrassFace(catalog, world.GetBlockState(entry.Position.Relative(Direction.East)));

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
            MeshCollider meshCollider = block.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }

        private static bool ShouldCullGrassTop(DemoContentCatalog catalog, BlockState stateAbove)
        {
            return ShouldCullGrassFace(catalog, stateAbove);
        }

        private static bool ShouldCullGrassFace(DemoContentCatalog catalog, BlockState stateAbove)
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
            DemoKineticSnapshot kineticSnapshot,
            MinecraftModelLoader modelLoader,
            MinecraftBlockStateLoader blockStateLoader)
        {
            Direction facing = entry.State.Get(DemoContentCatalog.FacingProperty);
            float shaftSpeed = 0f;
            float shaftAngleOffset = 0f;
            if (kineticSnapshot != null && kineticSnapshot.TryGet(entry.Position, out DemoKineticComponentState kineticState))
            {
                shaftSpeed = kineticState.Speed;
                shaftAngleOffset = DemoKineticResolver.GetRotationOffsetDegrees(entry.Position, kineticState.Axis);
            }

            if (!TryCreateCreativeMotorAssembly(
                    root,
                    GetDisplayName(catalog, entry),
                    ToUnityPosition(entry.Position),
                    facing,
                    WorldBlockModelBaseScale,
                    modelLoader,
                    blockStateLoader,
                    shaftSpeed,
                    shaftAngleOffset))
            {
                FailedStateDrivenWorldBlockCount++;
                return false;
            }

            AddMeshColliders(root.GetChild(root.childCount - 1).gameObject);
            GeneratedAnimatedMotorOutputCount++;
            return true;
        }

        private bool TryCreateShaftWorldBlock(
            Transform root,
            DemoContentCatalog catalog,
            WorldBlockEntry entry,
            DemoKineticSnapshot kineticSnapshot,
            MinecraftModelLoader modelLoader,
            MinecraftBlockStateLoader blockStateLoader)
        {
            if (!CreateFirstSliceWorldBlockVisualStateBridge.TryResolve(entry.State, out BlockStatePropertyValue[] visualProperties))
                return false;

            GameObject blockRoot = new GameObject(GetDisplayName(catalog, entry));
            blockRoot.hideFlags = HideFlags.DontSave;
            blockRoot.transform.SetParent(root, false);
            blockRoot.transform.localPosition = ToUnityPosition(entry.Position);

            DemoKineticComponentState kineticState = default;
            bool hasKineticState = kineticSnapshot != null && kineticSnapshot.TryGet(entry.Position, out kineticState);
            Transform modelRoot = hasKineticState
                ? CreateChildRoot(blockRoot.transform, "Spin")
                : blockRoot.transform;
            if (!TryCreateCombinedBlockModel(
                    modelRoot,
                    entry.State.Definition.Id,
                    visualProperties,
                    WorldBlockModelBaseScale,
                    modelLoader,
                    blockStateLoader))
            {
                FailedStateDrivenWorldBlockCount++;
                DestroyUnityObject(blockRoot);
                return false;
            }

            if (hasKineticState)
            {
                rotatingVisuals.Add(
                    new RotatingVisualState(
                        modelRoot,
                        ToUnityAxisVector(kineticState.Axis),
                        DemoKineticResolver.ConvertToDegreesPerSecond(kineticState.Speed),
                        DemoKineticResolver.GetRotationOffsetDegrees(entry.Position, kineticState.Axis)));
                GeneratedAnimatedShaftCount++;
            }

            AddMeshColliders(blockRoot);
            return true;
        }

        private bool TryCreateBeltWorldBlock(
            Transform root,
            DemoContentCatalog catalog,
            WorldBlockEntry entry,
            DemoBeltRuntimeSnapshot beltSnapshot,
            MinecraftModelLoader modelLoader,
            MinecraftBlockStateLoader blockStateLoader)
        {
            if (!CreateFirstSliceWorldBlockVisualStateBridge.TryResolve(entry.State, out BlockStatePropertyValue[] visualProperties))
                return false;

            GameObject blockRoot = new GameObject(GetDisplayName(catalog, entry));
            blockRoot.hideFlags = HideFlags.DontSave;
            blockRoot.transform.SetParent(root, false);
            blockRoot.transform.localPosition = ToUnityPosition(entry.Position);

            bool cased = entry.State.Get(DemoContentCatalog.BeltCasingProperty);
            bool created = cased
                ? TryCreateCombinedBlockModel(
                    blockRoot.transform,
                    entry.State.Definition.Id,
                    visualProperties,
                    WorldBlockModelBaseScale,
                    modelLoader,
                    blockStateLoader)
                : TryCreateUncasedBeltWorldBlock(
                    blockRoot.transform,
                    catalog,
                    entry.State,
                    entry.Position,
                    beltSnapshot,
                    modelLoader,
                    blockStateLoader);

            if (!created)
            {
                FailedStateDrivenWorldBlockCount++;
                DestroyUnityObject(blockRoot);
                return false;
            }

            if (!cased)
                GeneratedAnimatedBeltSegmentCount++;

            AddMeshColliders(blockRoot);
            return true;
        }

        private bool TryCreateUncasedBeltWorldBlock(
            Transform root,
            DemoContentCatalog catalog,
            BlockState beltState,
            BlockPos position,
            DemoBeltRuntimeSnapshot beltSnapshot,
            MinecraftModelLoader modelLoader,
            MinecraftBlockStateLoader blockStateLoader)
        {
            if (modelLoader == null)
                return false;

            DemoBeltSegmentRuntimeState runtimeState;
            if (beltSnapshot == null || !beltSnapshot.TryGet(position, out runtimeState))
            {
                runtimeState = new DemoBeltSegmentRuntimeState(
                    position,
                    position,
                    0,
                    1,
                    beltState.Get(DemoContentCatalog.BeltFacingProperty),
                    beltState.Get(DemoContentCatalog.BeltSlopeProperty),
                    beltState.Get(DemoContentCatalog.BeltPartProperty),
                    DemoBeltRuntimeResolver.GetRotationAxis(beltState, catalog),
                    0f);
            }

            Direction facing = runtimeState.Facing;
            DemoBeltSlope slope = runtimeState.Slope;
            DemoBeltPart part = runtimeState.Part;
            bool diagonal = IsDiagonalSlope(slope);
            float signedSpeed = GetBeltSignedSpeed(runtimeState.Speed, facing, slope);

            Transform beltRoot = CreateChildRoot(root, "Belt");
            SetBeltVisualTransform(beltRoot, facing, slope, WorldBlockModelBaseScale);

            ResourceLocation topModelId = ResolveUncasedBeltTopModel(part, diagonal);
            bool diagonalLayer = diagonal;
            bool topUsesBottomOffset = diagonal;
            if (!TryCreateAnimatedBeltLayer(beltRoot, topModelId, signedSpeed, diagonalLayer, topUsesBottomOffset, modelLoader))
            {
                DestroyUnityObject(beltRoot.gameObject);
                return false;
            }

            if (!diagonal)
            {
                ResourceLocation bottomModelId = ResolveUncasedBeltBottomModel(part);
                if (!TryCreateAnimatedBeltLayer(beltRoot, bottomModelId, signedSpeed, false, true, modelLoader))
                {
                    DestroyUnityObject(beltRoot.gameObject);
                    return false;
                }
            }

            if (part != DemoBeltPart.Middle)
                TryCreateBeltPulleyShaft(root, position, runtimeState, modelLoader, blockStateLoader);

            return true;
        }

        private bool TryCreateAnimatedBeltLayer(
            Transform root,
            ResourceLocation modelId,
            float signedSpeed,
            bool diagonal,
            bool usesBottomOffset,
            MinecraftModelLoader modelLoader)
        {
            MinecraftResolvedModel model = modelLoader.LoadModel(modelId);
            if (model.Elements.Count == 0)
                return false;

            Transform layerRoot = CreateChildRoot(root, modelId.Path.Replace('/', '_'));
            if (!TryCreateCombinedResolvedModel(
                    layerRoot,
                    model,
                    textureId => GetAnimatedBeltMaterial(textureId, signedSpeed, diagonal, usesBottomOffset)))
            {
                DestroyUnityObject(layerRoot.gameObject);
                return false;
            }

            return true;
        }

        private bool TryCreateBeltPulleyShaft(
            Transform root,
            BlockPos position,
            DemoBeltSegmentRuntimeState runtimeState,
            MinecraftModelLoader modelLoader,
            MinecraftBlockStateLoader blockStateLoader)
        {
            if (modelLoader == null || blockStateLoader == null)
                return false;

            ResourceLocation pulleyModelId = ResourceLocation.Parse("create:block/belt_pulley");
            Transform spinRoot = CreateChildRoot(root, "Pulley Shaft Spin");
            Transform orientationRoot = CreateChildRoot(spinRoot, "Orientation");
            
            // Align the orientation root with the rotation axis
            // The pulley model is Y-aligned by default.
            orientationRoot.localRotation = GetRotationToAxis(runtimeState.RotationAxis);

            if (!TryCreatePartialModel(orientationRoot, pulleyModelId, WorldBlockModelBaseScale, modelLoader))
            {
                // Fallback to full shaft block if partial model fails
                BlockStatePropertyValue[] shaftVisualProperties =
                {
                    new BlockStatePropertyValue("axis", DemoContentCatalog.AxisProperty.Serialize(runtimeState.RotationAxis))
                };

                if (!TryCreateCombinedBlockModel(
                        spinRoot,
                        DemoContentCatalog.ShaftBlockId,
                        shaftVisualProperties,
                        WorldBlockModelBaseScale,
                        modelLoader,
                        blockStateLoader))
                {
                    DestroyUnityObject(spinRoot.gameObject);
                    return false;
                }
            }

            if (runtimeState.Speed != 0f)
            {
                rotatingVisuals.Add(
                    new RotatingVisualState(
                        spinRoot,
                        ToUnityAxisVector(runtimeState.RotationAxis),
                        DemoKineticResolver.ConvertToDegreesPerSecond(runtimeState.Speed),
                        DemoKineticResolver.GetRotationOffsetDegrees(position, runtimeState.RotationAxis)));
            }

            return true;
        }

        private bool TryCreatePartialModel(
            Transform root,
            ResourceLocation modelId,
            float baseScale,
            MinecraftModelLoader modelLoader)
        {
            if (modelLoader == null)
                return false;

            try
            {
                MinecraftResolvedModel model = modelLoader.LoadModel(modelId);
                if (model.Elements.Count == 0)
                    return false;

                int faceIndex = 0;
                foreach (MinecraftModelElement element in model.Elements)
                {
                    foreach (MinecraftModelFace face in element.Faces.Values)
                    {
                        CreateModelFaceObject(root, model, element, face, faceIndex);
                        faceIndex++;
                    }
                }

                root.localScale = Vector3.one * baseScale;
                return faceIndex > 0;
            }
            catch (Exception)
            {
                return false;
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
            Vector3 localPosition,
            Direction facing,
            float baseScale,
            MinecraftModelLoader modelLoader,
            MinecraftBlockStateLoader blockStateLoader,
            float shaftSpeed,
            float shaftAngleOffset)
        {
            if (modelLoader == null || blockStateLoader == null)
                return false;

            GameObject motorRoot = new GameObject(name);
            motorRoot.hideFlags = HideFlags.DontSave;
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

            if (!TryCreateCreativeMotorHalfShaft(
                    motorRoot.transform,
                    facing,
                    baseScale,
                    modelLoader,
                    shaftSpeed,
                    shaftAngleOffset))
            {
                DestroyUnityObject(motorRoot);
                return false;
            }

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
            float shaftSpeed,
            float shaftAngleOffset)
        {
            try
            {
                MinecraftResolvedModel model = modelLoader.LoadModel(CreativeMotorHalfShaftModelId);
                if (model.Elements.Count == 0)
                    return false;

                Transform facingRoot = CreateChildRoot(root, "Shaft Half");
                facingRoot.localRotation = Quaternion.FromToRotation(Vector3.forward, ToUnityDirectionVector(facing));
                facingRoot.localScale = Vector3.one * baseScale;

                Transform spinRoot = CreateChildRoot(facingRoot, "Spin");
                if (!TryCreateCombinedResolvedModel(spinRoot, model))
                {
                    DestroyUnityObject(facingRoot.gameObject);
                    return false;
                }

                rotatingVisuals.Add(
                    new RotatingVisualState(
                        spinRoot,
                        Vector3.forward,
                        DemoKineticResolver.ConvertToDegreesPerSecond(shaftSpeed),
                        shaftAngleOffset));

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool TryCreateCombinedResolvedModel(Transform root, MinecraftResolvedModel model)
        {
            return TryCreateCombinedResolvedModel(root, model, GetCreateTextureMaterial);
        }

        private bool TryCreateCombinedResolvedModel(
            Transform root,
            MinecraftResolvedModel model,
            Func<ResourceLocation, Material> materialResolver)
        {
            if (materialResolver == null)
                throw new ArgumentNullException(nameof(materialResolver));

            Dictionary<ResourceLocation, CombinedMeshBuilder> builders = new Dictionary<ResourceLocation, CombinedMeshBuilder>();
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
                CreateCombinedMeshObject(root, pair.Key, pair.Value, materialResolver(pair.Key));

            return true;
        }

        private void CreateCombinedMeshObject(
            Transform root,
            ResourceLocation textureId,
            CombinedMeshBuilder builder,
            Material material)
        {
            GameObject meshObject = new GameObject("Combined " + textureId.Path);
            meshObject.hideFlags = HideFlags.DontSave;
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
            meshRenderer.sharedMaterial = material ?? GetCreateTextureMaterial(textureId);
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
            faceObject.hideFlags = HideFlags.DontSave;
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
                    uvs[1],
                    uvs[2],
                    uvs[3],
                    uvs[0]
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

        private static ResourceLocation ResolveUncasedBeltTopModel(DemoBeltPart part, bool diagonal)
        {
            bool start = part == DemoBeltPart.Start;
            bool end = part == DemoBeltPart.End;

            if (diagonal)
            {
                if (start)
                    return BeltDiagonalStartModelId;
                if (end)
                    return BeltDiagonalEndModelId;
                return BeltDiagonalMiddleModelId;
            }

            if (start)
                return BeltStartModelId;
            if (end)
                return BeltEndModelId;
            return BeltMiddleModelId;
        }

        private static ResourceLocation ResolveUncasedBeltBottomModel(DemoBeltPart part)
        {
            if (part == DemoBeltPart.Start)
                return BeltStartBottomModelId;
            if (part == DemoBeltPart.End)
                return BeltEndBottomModelId;
            return BeltMiddleBottomModelId;
        }

        private static bool IsDiagonalSlope(DemoBeltSlope slope)
        {
            return slope == DemoBeltSlope.Upward || slope == DemoBeltSlope.Downward;
        }

        private static float GetBeltSignedSpeed(float speed, Direction facing, DemoBeltSlope slope)
        {
            bool diagonal = IsDiagonalSlope(slope);
            bool sideways = slope == DemoBeltSlope.Sideways;
            bool vertical = slope == DemoBeltSlope.Vertical;
            bool upward = slope == DemoBeltSlope.Upward;
            bool downward = slope == DemoBeltSlope.Downward;
            bool alongX = facing.Axis() == Axis.X;
            bool alongZ = facing.Axis() == Axis.Z;

            bool shouldFlipPrimary =
                (facing.AxisDirection() == AxisDirection.Negative) ^ upward ^ ((alongX && !diagonal) || (alongZ && diagonal));
            if (shouldFlipPrimary)
                speed = -speed;

            bool shouldFlipSecondary = (sideways && (facing == Direction.South || facing == Direction.West)) ||
                (vertical && facing == Direction.East);
            if (shouldFlipSecondary)
                speed = -speed;

            return speed;
        }

        private static void SetBeltVisualTransform(Transform transform, Direction facing, DemoBeltSlope slope, float baseScale)
        {
            bool diagonal = IsDiagonalSlope(slope);
            bool sideways = slope == DemoBeltSlope.Sideways;
            bool vertical = slope == DemoBeltSlope.Vertical;
            bool downward = slope == DemoBeltSlope.Downward;
            bool alongX = facing.Axis() == Axis.X;
            bool alongZ = facing.Axis() == Axis.Z;

            float rotX =
                (!diagonal && slope != DemoBeltSlope.Horizontal ? 90f : 0f) +
                (downward ? 180f : 0f) +
                (sideways ? 90f : 0f) +
                (vertical && alongZ ? 180f : 0f);
            float rotY =
                ToYRot(facing) +
                (((diagonal ^ alongX) && !downward) ? 180f : 0f) +
                (sideways && alongZ ? 180f : 0f) +
                (vertical && alongX ? 90f : 0f);
            float rotZ = (sideways ? 90f : 0f) + (vertical && alongX ? 90f : 0f);

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(rotX, rotY, rotZ);
            transform.localScale = Vector3.one * baseScale;
        }

        private static float ToYRot(Direction direction)
        {
            return direction switch
            {
                Direction.South => 0f,
                Direction.West => 90f,
                Direction.North => 180f,
                Direction.East => 270f,
                _ => 0f
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

        private Material GetAnimatedBeltMaterial(ResourceLocation sourceTextureId, float signedSpeed, bool diagonal, bool usesBottomOffset)
        {
            if (!TryResolveBeltScrollTexture(sourceTextureId, out ResourceLocation scrollTextureId))
                return GetCreateTextureMaterial(sourceTextureId);

            float scrollFactor = diagonal ? BeltScrollFactorDiagonal : BeltScrollFactorOtherwise;
            float cycleOffset = usesBottomOffset ? BeltScrollOffsetBottom : BeltScrollOffsetOtherwise;
            string key = "belt_scroll_" +
                sourceTextureId +
                "_target_" +
                scrollTextureId +
                "_speed_" +
                signedSpeed.ToString("0.###", CultureInfo.InvariantCulture) +
                "_factor_" +
                scrollFactor.ToString("0.###", CultureInfo.InvariantCulture) +
                "_offset_" +
                cycleOffset.ToString("0.###", CultureInfo.InvariantCulture);

            Material material;
            if (materialsByKey.TryGetValue(key, out material))
                return material;

            material = new Material(FindLitShader());
            material.name = "Demo " + key;
            ConfigureCreateScrollingMaterial(material, LoadPrivateCreateTexture(scrollTextureId));
            SetMaterialTextureTransform(material, new Vector2(1f, 0.5f), Vector2.zero);

            materialsByKey.Add(key, material);
            animatedBeltMaterialsByKey[key] = new AnimatedBeltMaterialState(material, signedSpeed, scrollFactor, cycleOffset);
            return material;
        }

        private static bool TryResolveBeltScrollTexture(ResourceLocation sourceTextureId, out ResourceLocation scrollTextureId)
        {
            if (sourceTextureId == BeltTopTextureId || sourceTextureId == BeltBottomTextureId)
            {
                scrollTextureId = BeltTopScrollTextureId;
                return true;
            }

            if (sourceTextureId == BeltDiagonalTextureId)
            {
                scrollTextureId = BeltDiagonalScrollTextureId;
                return true;
            }

            scrollTextureId = default;
            return false;
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

        private static void ConfigureCreateScrollingMaterial(Material material, Texture2D texture)
        {
            if (material == null)
                throw new ArgumentNullException(nameof(material));

            material.color = Color.white;

            if (texture != null)
            {
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Repeat;
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

        private static void SetMaterialTextureTransform(Material material, Vector2 scale, Vector2 offset)
        {
            if (material == null)
                return;

            material.mainTextureScale = scale;
            material.mainTextureOffset = offset;

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTextureScale("_BaseMap", scale);
                material.SetTextureOffset("_BaseMap", offset);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTextureScale("_MainTex", scale);
                material.SetTextureOffset("_MainTex", offset);
            }
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
            if (textureId.Namespace == "minecraft")
            {
                return LoadPrivateCreateTexture(
                    new CreatePrivateAssetFileReference(
                        "assets/minecraft/textures/" + textureId.Path + ".png"));
            }

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
            labelObject.hideFlags = HideFlags.DontSave;
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
            DemoMinecraftFirstPersonController playerController = FindAnyObjectByType<DemoMinecraftFirstPersonController>();
            bool createdPlayer = false;
            if (playerController == null)
            {
                GameObject playerObject = new GameObject(PlayerRootName);
                playerObject.AddComponent<CharacterController>();
                playerController = playerObject.AddComponent<DemoMinecraftFirstPersonController>();
                createdPlayer = true;
            }
            else if (playerController.GetComponent<CharacterController>() == null)
            {
                playerController.gameObject.AddComponent<CharacterController>();
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            DemoFreeCameraController freeCamera = camera.GetComponent<DemoFreeCameraController>();
            if (freeCamera != null)
                freeCamera.enabled = false;

            playerController.RefreshRigConfiguration();
            playerController.SetPlayerCamera(camera);
            EnsureCreativeBuildController(camera);
            if (createdPlayer || !Application.isPlaying)
            {
                playerController.transform.position = new Vector3(1.5f, 1f, 4.5f);
                playerController.SetViewAngles(90f, 12f);
            }

            camera.fieldOfView = DemoMinecraftFirstPersonController.DefaultFieldOfView;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
        }

        private void EnsureCreativeBuildController(Camera camera)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));

            DemoCreativeBuildController controller = camera.GetComponent<DemoCreativeBuildController>();
            if (controller == null)
                controller = camera.gameObject.AddComponent<DemoCreativeBuildController>();

            controller.SetPresenter(this);
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
                projectRoot);
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

        private void UpdateAnimatedBeltMaterials(float timeSeconds)
        {
            if (animatedBeltMaterialsByKey.Count == 0)
                return;

            foreach (AnimatedBeltMaterialState state in animatedBeltMaterialsByKey.Values)
            {
                if (state.Material == null)
                    continue;

                float cycle = Mathf.Repeat((timeSeconds * BeltTicksPerSecond * state.Speed * BeltMagicScrollMultiplier) + state.CycleOffset, 1f);
                float offset = cycle * state.ScrollFactor;
                SetMaterialTextureTransform(state.Material, new Vector2(1f, 0.5f), new Vector2(0f, -offset));
            }
        }

        private static float GetAnimationTimeSeconds()
        {
            return Application.isPlaying
                ? Time.realtimeSinceStartup
                : (float)(DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond);
        }

        private void UpdateRotatingVisuals(float timeSeconds)
        {
            if (rotatingVisuals.Count == 0)
                return;

            foreach (RotatingVisualState rotatingVisual in rotatingVisuals)
            {
                if (rotatingVisual.Transform == null)
                    continue;

                float angle = Mathf.Repeat(
                    (timeSeconds * rotatingVisual.DegreesPerSecond) + rotatingVisual.BaseAngleDegrees,
                    360f);
                rotatingVisual.Transform.localRotation = Quaternion.AngleAxis(angle, rotatingVisual.Axis);
            }

        }

        private static void AddMeshColliders(GameObject root)
        {
            if (root == null)
                return;

            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (meshFilter == null || meshFilter.sharedMesh == null)
                    continue;

                MeshCollider meshCollider = meshFilter.GetComponent<MeshCollider>();
                if (meshCollider == null)
                    meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();

                meshCollider.sharedMesh = meshFilter.sharedMesh;
            }
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

        private static Quaternion GetRotationToAxis(Axis axis)
        {
            return axis switch
            {
                Axis.X => Quaternion.Euler(90f, 90f, 0f),
                Axis.Y => Quaternion.identity,
                Axis.Z => Quaternion.Euler(90f, 0f, 0f),
                _ => Quaternion.identity
            };
        }

        private static Vector3 ToUnityAxisVector(Axis axis)
        {
            switch (axis)
            {
                case Axis.X:
                    return Vector3.right;
                case Axis.Y:
                    return Vector3.up;
                case Axis.Z:
                    return Vector3.forward;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
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
            animatedBeltMaterialsByKey.Clear();

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

        private sealed class AnimatedBeltMaterialState
        {
            public AnimatedBeltMaterialState(Material material, float speed, float scrollFactor, float cycleOffset)
            {
                Material = material;
                Speed = speed;
                ScrollFactor = scrollFactor;
                CycleOffset = cycleOffset;
            }

            public Material Material { get; }

            public float Speed { get; }

            public float ScrollFactor { get; }

            public float CycleOffset { get; }
        }

        private sealed class RotatingVisualState
        {
            public RotatingVisualState(Transform transform, Vector3 axis, float degreesPerSecond, float baseAngleDegrees)
            {
                Transform = transform;
                Axis = axis;
                DegreesPerSecond = degreesPerSecond;
                BaseAngleDegrees = baseAngleDegrees;
            }

            public Transform Transform { get; }

            public Vector3 Axis { get; }

            public float DegreesPerSecond { get; }

            public float BaseAngleDegrees { get; }
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
