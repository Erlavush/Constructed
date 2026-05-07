using Constructed.Core;
using Constructed.Create;
using Constructed.Unity;
using NUnit.Framework;
using UnityEngine;

namespace Constructed.Tests
{
    public sealed class DemoCreativeBuildControllerTests
    {
        [Test]
        public void BuildControllerStartsWithMotorShaftAndGrassHotbarDefaults()
        {
            GameObject presenterObject = new GameObject("Presenter Build Controller Test");
            try
            {
                DemoVerticalSlicePresenter presenter = presenterObject.AddComponent<DemoVerticalSlicePresenter>();
                presenter.Rebuild();

                GameObject cameraObject = GameObject.Find("Main Camera");
                Assert.NotNull(cameraObject);
                DemoCreativeBuildController controller = cameraObject.GetComponent<DemoCreativeBuildController>();
                Assert.NotNull(controller);
                Assert.AreEqual(0, controller.SelectedHotbarSlotIndex);
                Assert.IsFalse(controller.InventoryOpen);
                Assert.AreEqual(8, controller.InventoryEntryCount);
                Assert.AreEqual(DemoContentCatalog.CreativeMotorBlockId, controller.GetHotbarSlotId(0).Value);
                Assert.AreEqual(DemoContentCatalog.ShaftBlockId, controller.GetHotbarSlotId(1).Value);
                Assert.AreEqual(DemoContentCatalog.SurfaceBlockId, controller.GetHotbarSlotId(2).Value);
                Assert.IsFalse(controller.GetHotbarSlotId(3).HasValue);
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
        public void BuildControllerProvidesVisibleHotbarIconsForMotorShaftAndGrass()
        {
            GameObject presenterObject = new GameObject("Presenter Hotbar Icon Test");
            try
            {
                DemoVerticalSlicePresenter presenter = presenterObject.AddComponent<DemoVerticalSlicePresenter>();
                presenter.Rebuild();

                GameObject cameraObject = GameObject.Find("Main Camera");
                Assert.NotNull(cameraObject);
                DemoCreativeBuildController controller = cameraObject.GetComponent<DemoCreativeBuildController>();
                Assert.NotNull(controller);

                Texture2D motorIcon = controller.GetHotbarSlotIconTexture(0);
                Texture2D shaftIcon = controller.GetHotbarSlotIconTexture(1);
                Texture2D grassIcon = controller.GetHotbarSlotIconTexture(2);
                Assert.NotNull(motorIcon);
                Assert.NotNull(shaftIcon);
                Assert.NotNull(grassIcon);
                Assert.AreEqual(64, motorIcon.width);
                Assert.AreEqual(64, motorIcon.height);
                Assert.AreEqual(64, shaftIcon.width);
                Assert.AreEqual(64, shaftIcon.height);
                Assert.AreEqual(64, grassIcon.width);
                Assert.AreEqual(64, grassIcon.height);
                StringAssert.StartsWith("Rendered Icon ", motorIcon.name);
                StringAssert.StartsWith("Rendered Icon ", shaftIcon.name);
                StringAssert.StartsWith("Rendered Icon ", grassIcon.name);
                Assert.AreEqual(FilterMode.Point, motorIcon.filterMode);
                Assert.AreEqual(FilterMode.Point, shaftIcon.filterMode);
                Assert.AreEqual(FilterMode.Point, grassIcon.filterMode);
                Assert.Greater(CountOpaquePixels(motorIcon), 0);
                Assert.Greater(CountOpaquePixels(shaftIcon), 0);
                Assert.Greater(CountOpaquePixels(grassIcon), 0);
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
                DestroyIfFound("Main Camera");
                DestroyIfFound(DemoMinecraftFirstPersonController.PlayerRootName);
                DestroyIfFound("Directional Light");
            }
        }

        private static void DestroyIfFound(string name)
        {
            GameObject found = GameObject.Find(name);
            if (found != null)
                Object.DestroyImmediate(found);
        }

        private static int CountOpaquePixels(Texture2D texture)
        {
            int count = 0;
            Color32[] pixels = texture.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a > 0)
                    count++;
            }

            return count;
        }
    }
}
