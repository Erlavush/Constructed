using System.Reflection;
using Constructed.Unity;
using NUnit.Framework;

namespace Constructed.Tests
{
    public sealed class DemoMinecraftFirstPersonControllerTests
    {
        [Test]
        public void ControllerPreservesAttachedCameraOnlyForLiveRebuildCase()
        {
            MethodInfo method = typeof(DemoMinecraftFirstPersonController).GetMethod(
                "ShouldPreserveAttachedCamera",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method);
            Assert.AreEqual(true, method.Invoke(null, new object[] { true, true, true }));
            Assert.AreEqual(false, method.Invoke(null, new object[] { false, true, true }));
            Assert.AreEqual(false, method.Invoke(null, new object[] { true, false, true }));
            Assert.AreEqual(false, method.Invoke(null, new object[] { true, true, false }));
        }
    }
}
