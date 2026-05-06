using Constructed.Unity;
using NUnit.Framework;
using System.Collections.Generic;
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

                Assert.AreEqual(268, presenter.GeneratedBlockCount);
                Assert.AreEqual(7, presenter.GeneratedItemPreviewCount);
                Assert.AreEqual(5, presenter.GeneratedBlockCatalogPreviewCount);
                Assert.AreEqual(6, presenter.GeneratedStateDrivenWorldBlockCount);
                Assert.AreEqual(5, presenter.GeneratedModelItemPreviewCount);
                Assert.AreEqual(2, presenter.GeneratedFlatItemPreviewCount);
                Assert.AreEqual(0, presenter.FailedItemModelPreviewCount);
                Assert.AreEqual(0, presenter.FailedBlockCatalogPreviewCount);
                Assert.AreEqual(0, presenter.FailedStateDrivenWorldBlockCount);
                Assert.AreEqual(CreateFirstSlicePrivateAssetManifest.Manifest.UniqueFiles.Count, presenter.SyncedCreateAssetFileCount + presenter.MissingCreateAssetFileCount);
                Assert.AreEqual(1, presenterObject.transform.childCount);
                Assert.AreEqual("Generated Demo Layout", presenterObject.transform.GetChild(0).name);
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
                DestroyIfFound("Main Camera");
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
                DestroyIfFound("Directional Light");
            }
        }

        private static void DestroyIfFound(string name)
        {
            GameObject found = GameObject.Find(name);
            if (found != null)
                Object.DestroyImmediate(found);
        }
    }
}
