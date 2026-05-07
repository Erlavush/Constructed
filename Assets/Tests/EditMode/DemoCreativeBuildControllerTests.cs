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
        public void BuildControllerStartsWithMotorAndShaftHotbarDefaults()
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
                Assert.AreEqual(7, controller.InventoryEntryCount);
                Assert.AreEqual(DemoContentCatalog.CreativeMotorBlockId, controller.GetHotbarSlotId(0).Value);
                Assert.AreEqual(DemoContentCatalog.ShaftBlockId, controller.GetHotbarSlotId(1).Value);
                Assert.IsFalse(controller.GetHotbarSlotId(2).HasValue);
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
    }
}
