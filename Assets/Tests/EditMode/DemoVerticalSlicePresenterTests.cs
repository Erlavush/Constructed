using Constructed.Unity;
using Constructed.Create;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Constructed.Tests
{
    public sealed class DemoVerticalSlicePresenterTests
    {
        [Test]
        public void PresenterBuildsExpectedDemoBlockCount()
        {
            GameObject presenterObject = new GameObject("Presenter Test");
            try
            {
                DemoVerticalSlicePresenter presenter = presenterObject.AddComponent<DemoVerticalSlicePresenter>();
                presenter.Rebuild();

                Assert.AreEqual(67, presenter.GeneratedBlockCount);
                Assert.AreEqual(0, presenter.GeneratedItemPreviewCount);
                Assert.AreEqual(0, presenter.GeneratedBlockCatalogPreviewCount);
                Assert.AreEqual(3, presenter.GeneratedStateDrivenWorldBlockCount);
                Assert.AreEqual(0, presenter.GeneratedModelItemPreviewCount);
                Assert.AreEqual(0, presenter.GeneratedFlatItemPreviewCount);
                Assert.AreEqual(0, presenter.FailedItemModelPreviewCount);
                Assert.AreEqual(0, presenter.FailedBlockCatalogPreviewCount);
                Assert.AreEqual(0, presenter.FailedStateDrivenWorldBlockCount);
                Assert.AreEqual(1, presenter.GeneratedAnimatedMotorOutputCount);
                Assert.AreEqual(2, presenter.GeneratedAnimatedShaftCount);
                Assert.AreEqual(CreateFirstSlicePrivateAssetManifest.Manifest.UniqueFiles.Count, presenter.SyncedCreateAssetFileCount + presenter.MissingCreateAssetFileCount);
                Assert.AreEqual(1, presenterObject.transform.childCount);
                Assert.AreEqual("Generated Demo Layout", presenterObject.transform.GetChild(0).name);
                Assert.AreEqual(1, presenterObject.transform.GetChild(0).childCount);
                Assert.AreEqual("World", presenterObject.transform.GetChild(0).GetChild(0).name);
                GameObject playerObject = GameObject.Find(DemoMinecraftFirstPersonController.PlayerRootName);
                Assert.NotNull(playerObject);
                DemoMinecraftFirstPersonController playerController = playerObject.GetComponent<DemoMinecraftFirstPersonController>();
                Assert.NotNull(playerController);
                CharacterController characterController = playerObject.GetComponent<CharacterController>();
                Assert.NotNull(characterController);
                Assert.AreEqual(DemoMinecraftFirstPersonController.StandingWidth * 0.5f, characterController.radius, 0.001f);
                Assert.AreEqual(DemoMinecraftFirstPersonController.StandingHeight, characterController.height, 0.001f);
                Assert.AreEqual(DemoMinecraftFirstPersonController.DefaultStepOffset, characterController.stepOffset, 0.001f);
                GameObject cameraObject = GameObject.Find("Main Camera");
                Assert.NotNull(cameraObject);
                Assert.NotNull(cameraObject.GetComponent<DemoCreativeBuildController>());
                Assert.AreSame(playerObject.transform, cameraObject.transform.parent);
                Assert.AreEqual(DemoMinecraftFirstPersonController.StandingEyeHeight, cameraObject.transform.localPosition.y, 0.001f);
                Assert.AreEqual(DemoMinecraftFirstPersonController.DefaultFieldOfView, cameraObject.GetComponent<Camera>().fieldOfView, 0.001f);
                Assert.Less(Quaternion.Angle(cameraObject.transform.localRotation, Quaternion.Euler(12f, 0f, 0f)), 0.001f);
                MeshCollider[] colliders = presenterObject.GetComponentsInChildren<MeshCollider>(true);
                Assert.Greater(colliders.Length, 0);
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
                DestroyIfFound("Main Camera");
                DestroyIfFound(DemoMinecraftFirstPersonController.PlayerRootName);
                DestroyIfFound("Directional Light");
            }
        }

        [Test]
        public void PresenterUsesPointFilteredCreateTexturesForModelFaces()
        {
            GameObject presenterObject = new GameObject("Presenter Texture Test");
            try
            {
                DemoVerticalSlicePresenter presenter = presenterObject.AddComponent<DemoVerticalSlicePresenter>();
                presenter.Rebuild();

                MeshRenderer[] renderers = presenterObject.GetComponentsInChildren<MeshRenderer>(true);
                List<Texture2D> createTextures = new List<Texture2D>();
                foreach (MeshRenderer renderer in renderers)
                {
                    Material material = renderer.sharedMaterial;
                    if (material == null || material.name == null || !material.name.StartsWith("Demo create_texture_"))
                        continue;
                    Assert.Less(material.renderQueue, 3000);
                    if (material.mainTexture is Texture2D texture)
                        createTextures.Add(texture);
                }

                Assert.Greater(createTextures.Count, 0);
                foreach (Texture2D texture in createTextures)
                {
                    Assert.AreEqual(FilterMode.Point, texture.filterMode);
                    Assert.AreEqual(TextureWrapMode.Clamp, texture.wrapMode);
                    Assert.AreEqual(0, texture.anisoLevel);
                }
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
                DestroyIfFound("Main Camera");
                DestroyIfFound(DemoMinecraftFirstPersonController.PlayerRootName);
                DestroyIfFound("Directional Light");
            }
        }

        [Test]
        public void PresenterUsesVanillaGrassTexturesForSurfaceBlocks()
        {
            GameObject presenterObject = new GameObject("Presenter Grass Test");
            try
            {
                DemoVerticalSlicePresenter presenter = presenterObject.AddComponent<DemoVerticalSlicePresenter>();
                presenter.Rebuild();

                MeshRenderer[] renderers = presenterObject.GetComponentsInChildren<MeshRenderer>(true);
                HashSet<string> surfaceMaterialKeys = new HashSet<string>();
                bool foundSurfaceRenderer = false;
                foreach (MeshRenderer renderer in renderers)
                {
                    Material[] materials = renderer.sharedMaterials;
                    if (materials == null || materials.Length == 0)
                        continue;

                    bool rendererUsesSurfaceMaterials = false;
                    foreach (Material material in materials)
                    {
                        if (material == null || string.IsNullOrEmpty(material.name))
                            continue;

                        if (material.name.StartsWith("Demo surface_grass_top_tinted"))
                        {
                            surfaceMaterialKeys.Add("top");
                            Texture2D texture = material.mainTexture as Texture2D;
                            AssertSurfaceTexture(texture);
                            AssertTextureLooksGrassTinted(texture);
                            rendererUsesSurfaceMaterials = true;
                        }
                        else if (material.name.StartsWith("Demo surface_dirt_bottom"))
                        {
                            surfaceMaterialKeys.Add("bottom");
                            AssertSurfaceTexture(material.mainTexture as Texture2D);
                            rendererUsesSurfaceMaterials = true;
                        }
                        else if (material.name.StartsWith("Demo surface_grass_side_tinted"))
                        {
                            surfaceMaterialKeys.Add("side");
                            Texture2D texture = material.mainTexture as Texture2D;
                            AssertSurfaceTexture(texture);
                            StringAssert.Contains("surface_grass_side_tinted", texture.name);
                            rendererUsesSurfaceMaterials = true;
                        }
                    }

                    if (!rendererUsesSurfaceMaterials)
                        continue;

                    foundSurfaceRenderer = true;
                    MeshFilter filter = renderer.GetComponent<MeshFilter>();
                    Assert.NotNull(filter);
                    Assert.NotNull(filter.sharedMesh);
                    Assert.AreEqual(3, filter.sharedMesh.subMeshCount);
                    Assert.AreEqual(3, materials.Length);
                }

                Assert.IsTrue(foundSurfaceRenderer);
                CollectionAssert.AreEquivalent(new[] { "top", "bottom", "side" }, surfaceMaterialKeys);
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
                DestroyIfFound("Main Camera");
                DestroyIfFound(DemoMinecraftFirstPersonController.PlayerRootName);
                DestroyIfFound("Directional Light");
            }
        }

        [Test]
        public void PresenterModelFaceVerticesUseOutwardWindingForAllDirections()
        {
            MinecraftModelElement element = new MinecraftModelElement(
                "Test",
                new Vector3(0f, 0f, 0f),
                new Vector3(16f, 16f, 16f),
                null,
                new Dictionary<Constructed.Core.Direction, MinecraftModelFace>
                {
                    { Constructed.Core.Direction.Up, new MinecraftModelFace(Constructed.Core.Direction.Up, Vector4.zero, 0, Constructed.Core.ResourceLocation.Parse("create:block/test")) }
                });

            MethodInfo method = typeof(DemoVerticalSlicePresenter).GetMethod(
                "CreateModelFaceVertices",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method);

            AssertFaceWinding(method, element, Constructed.Core.Direction.Down, Vector3.down);
            AssertFaceWinding(method, element, Constructed.Core.Direction.Up, Vector3.up);
            AssertFaceWinding(method, element, Constructed.Core.Direction.North, Vector3.back);
            AssertFaceWinding(method, element, Constructed.Core.Direction.South, Vector3.forward);
            AssertFaceWinding(method, element, Constructed.Core.Direction.West, Vector3.left);
            AssertFaceWinding(method, element, Constructed.Core.Direction.East, Vector3.right);
        }

        [Test]
        public void PresenterOnlyCullsGrassTopForCurrentFullCubeBlocks()
        {
            DemoContentCatalog catalog = DemoContentCatalog.Create();
            MethodInfo method = typeof(DemoVerticalSlicePresenter).GetMethod(
                "ShouldCullGrassTop",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method);
            AssertCullRule(method, catalog, catalog.Surface.DefaultState, true);
            AssertCullRule(method, catalog, catalog.CreativeCrate.DefaultState, true);
            AssertCullRule(method, catalog, catalog.ItemVault.DefaultState, true);
            AssertCullRule(method, catalog, catalog.CreativeMotor.DefaultState, false);
            AssertCullRule(method, catalog, catalog.Shaft.DefaultState, false);
            AssertCullRule(method, catalog, catalog.Belt.DefaultState, false);
            AssertCullRule(method, catalog, catalog.BrassFunnel.DefaultState, false);
            AssertCullRule(method, catalog, catalog.Air.DefaultState, false);
        }

        [Test]
        public void PresenterKeepsStandaloneShaftOnStateDrivenPathWhenMotorIsRemoved()
        {
            GameObject presenterObject = new GameObject("Presenter Standalone Shaft Test");
            try
            {
                DemoVerticalSlicePresenter presenter = presenterObject.AddComponent<DemoVerticalSlicePresenter>();
                presenter.Rebuild();

                Assert.IsTrue(presenter.TryRemoveBlock(DemoVerticalSliceBootstrap.CreativeMotorPosition));
                Assert.IsTrue(presenter.TryRemoveBlock(DemoVerticalSliceBootstrap.SecondShaftPosition));

                Assert.AreEqual(65, presenter.GeneratedBlockCount);
                Assert.AreEqual(1, presenter.GeneratedStateDrivenWorldBlockCount);
                Assert.AreEqual(0, presenter.GeneratedAnimatedMotorOutputCount);
                Assert.AreEqual(0, presenter.GeneratedAnimatedShaftCount);
                Assert.AreEqual(0, presenter.FailedStateDrivenWorldBlockCount);
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
                DestroyIfFound("Main Camera");
                DestroyIfFound(DemoMinecraftFirstPersonController.PlayerRootName);
                DestroyIfFound("Directional Light");
            }
        }

        [Test]
        public void PresenterPlacesGrassBlockThroughBuildPlacementPath()
        {
            GameObject presenterObject = new GameObject("Presenter Grass Placement Test");
            try
            {
                DemoVerticalSlicePresenter presenter = presenterObject.AddComponent<DemoVerticalSlicePresenter>();
                presenter.Rebuild();

                Constructed.Core.BlockPos placementPosition =
                    new Constructed.Core.BlockPos(0, DemoVerticalSliceBootstrap.MachineY, 0);

                Assert.IsTrue(presenter.TryPlaceBlock(DemoContentCatalog.SurfaceBlockId, placementPosition, Constructed.Core.Direction.North));
                Assert.AreSame(presenter.Catalog.Surface, presenter.World.GetBlockState(placementPosition).Definition);
                Assert.AreEqual(68, presenter.GeneratedBlockCount);
                Assert.AreEqual(3, presenter.GeneratedStateDrivenWorldBlockCount);
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
                DestroyIfFound("Main Camera");
                DestroyIfFound(DemoMinecraftFirstPersonController.PlayerRootName);
                DestroyIfFound("Directional Light");
            }
        }

        private static void AssertSurfaceTexture(Texture2D texture)
        {
            Assert.NotNull(texture);
            Assert.AreEqual(FilterMode.Point, texture.filterMode);
            Assert.AreEqual(TextureWrapMode.Repeat, texture.wrapMode);
            Assert.AreEqual(0, texture.anisoLevel);
        }

        private static void AssertTextureLooksGrassTinted(Texture2D texture)
        {
            Assert.NotNull(texture);
            Color32 sample = texture.GetPixel(8, 8);
            Assert.Greater(sample.g, sample.r);
            Assert.Greater(sample.g, sample.b);
        }

        private static void DestroyIfFound(string name)
        {
            GameObject found = GameObject.Find(name);
            if (found != null)
                Object.DestroyImmediate(found);
        }

        private static void AssertFaceWinding(
            MethodInfo method,
            MinecraftModelElement element,
            Constructed.Core.Direction direction,
            Vector3 expectedNormal)
        {
            object[] arguments = { element, direction };
            Vector3[] vertices = (Vector3[])method.Invoke(null, arguments);
            Assert.NotNull(vertices);
            Assert.AreEqual(4, vertices.Length);

            Vector3 normal = Vector3.Cross(vertices[1] - vertices[0], vertices[2] - vertices[0]).normalized;
            Assert.Greater(Vector3.Dot(normal, expectedNormal), 0.99f, direction.ToString());
        }

        private static void AssertCullRule(MethodInfo method, DemoContentCatalog catalog, Constructed.Minecraft.BlockState stateAbove, bool expected)
        {
            object result = method.Invoke(null, new object[] { catalog, stateAbove });
            Assert.AreEqual(expected, (bool)result, stateAbove.Definition.Id.ToString());
        }
    }
}
