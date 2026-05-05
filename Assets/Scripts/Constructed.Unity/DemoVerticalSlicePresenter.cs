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

        private readonly Dictionary<string, Material> materialsByKey = new Dictionary<string, Material>();
        private Texture2D runtimeGrassTexture;

        public int GeneratedBlockCount { get; private set; }

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
            materialsByKey.Clear();
            GeneratedBlockCount = 0;

            DemoContentCatalog catalog = DemoContentCatalog.Create();
            BlockWorld world = DemoVerticalSliceBootstrap.CreateVerticalSliceWorld(catalog);
            Transform root = CreateGeneratedRoot();

            foreach (WorldBlockEntry entry in world.GetStoredBlocks())
                CreateBlock(root, catalog, entry);

            ConfigureCamera();
            ConfigureLight();
        }

        private Transform CreateGeneratedRoot()
        {
            GameObject root = new GameObject(GeneratedRootName);
            root.transform.SetParent(transform, false);
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

                DestroyObject(texture);
            }

            runtimeGrassTexture = CreateFallbackGrassTexture();
            return runtimeGrassTexture;
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

        private static Shader FindLitShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
                return shader;

            shader = Shader.Find("Standard");
            return shader != null ? shader : Shader.Find("Diffuse");
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
            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            labelObject.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
            labelObject.transform.localScale = Vector3.one * 0.12f;

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

            camera.transform.position = new Vector3(8f, 12f, -9f);
            camera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
        }

        private void ConfigureLight()
        {
            Light light = FindObjectOfType<Light>();
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
                DestroyObject(child);
        }

        private static void DestroyObject(Object target)
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
