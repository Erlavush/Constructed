using Constructed.Unity;
using NUnit.Framework;
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

        private static void DestroyIfFound(string name)
        {
            GameObject found = GameObject.Find(name);
            if (found != null)
                Object.DestroyImmediate(found);
        }
    }
}
