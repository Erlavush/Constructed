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

        private readonly Dictionary<string, Material> materialsByKey = new Dictionary<string, Material>();
        private readonly Dictionary<string, Texture2D> createTexturesByPath = new Dictionary<string, Texture2D>();
        private Texture2D runtimeGrassTexture;
        private Texture2D missingCreateItemTexture;

        public int GeneratedBlockCount { get; private set; }

        public int GeneratedItemPreviewCount { get; private set; }

        public int SyncedCreateAssetFileCount { get; private set; }

        public int MissingCreateAssetFileCount { get; private set; }

        public int CopiedCreateAssetFileCount { get; private set; }

        private void OnEnable()
        {
            Rebuild();
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled)
                return;

            Rebuild();
        }

        public void Rebuild()
        {
            ClearGeneratedObjects();
            DestroyRuntimeTextures();
            materialsByKey.Clear();
            GeneratedBlockCount = 0;
            GeneratedItemPreviewCount = 0;
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

            Transform root = CreateGeneratedRoot();
            Transform worldRoot = CreateChildRoot(root, "World");
            Transform itemCatalogRoot = CreateChildRoot(root, "Item Catalog");

            foreach (WorldBlockEntry entry in world.GetStoredBlocks())
                CreateBlock(worldRoot, catalog, entry);

            CreateItemCatalog(itemCatalogRoot);

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

        private void CreateBlock(Transform root, DemoContentCatalog catalog, WorldBlockEntry entry)
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

            GeneratedBlockCount++;
        }

        private void CreateItemCatalog(Transform root)
        {
            AddFloatingLabel(root, "Create Item Catalog", new Vector3(8f, 3.65f, ItemCatalogZ), 0.16f);
            AddFloatingLabel(
                root,
                $"Private assets: {SyncedCreateAssetFileCount}/{CreateFirstSlicePrivateAssetManifest.Manifest.UniqueFiles.Count} ready, {MissingCreateAssetFileCount} missing",
                new Vector3(8f, 3.1f, ItemCatalogZ),
                0.11f);

            for (int i = 0; i < CreateFirstSliceItemVisualCatalog.Entries.Count; i++)
            {
                CreateItemPreview(root, CreateFirstSliceItemVisualCatalog.Entries[i], i);
                GeneratedItemPreviewCount++;
            }
        }

        private void CreateItemPreview(Transform root, CreateItemVisualCatalogEntry entry, int index)
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

            GameObject card = GameObject.CreatePrimitive(PrimitiveType.Cube);
            card.name = entry.Label;
            card.transform.SetParent(root, false);
            card.transform.localPosition = cardPosition;
            card.transform.localScale = new Vector3(1.1f, 1.1f, 0.05f);
            card.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);

            Renderer cardRenderer = card.GetComponent<Renderer>();
            if (cardRenderer != null)
                cardRenderer.sharedMaterial = GetItemPreviewMaterial(entry);

            AddFloatingLabel(card.transform, entry.Label, new Vector3(0f, 0.9f, 0f), 0.1f);
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
                    createTexturesByPath.Add(previewTextureFile.RepositoryRelativePath, texture);
                    return texture;
                }

                DestroyUnityObject(texture);
            }

            Texture2D missingTexture = GetMissingCreateItemTexture();
            createTexturesByPath.Add(previewTextureFile.RepositoryRelativePath, missingTexture);
            return missingTexture;
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

        private void DestroyRuntimeTextures()
        {
            foreach (Texture2D texture in createTexturesByPath.Values)
            {
                if (!ReferenceEquals(texture, missingCreateItemTexture))
                    DestroyUnityObject(texture);
            }

            createTexturesByPath.Clear();
        }

        private static void DestroyUnityObject(Object target)
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
