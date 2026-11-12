using System.Linq;
using NUnit.Framework;
#if UNITY_6000_4_OR_NEWER
using UnityEngine.Assemblies;
#endif

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class EditModeSmokeTests
    {
        [Test]
        public void TestAssembly_Runs()
        {
            Assert.Pass();
        }

        [Test]
        public void CoreAssembly_IsCompiled()
        {
            AssertAssemblyLoaded("MiradorWorks.Settings");
        }

        [Test]
        public void UrpAssembly_IsCompiled_WhenUrpIsInstalled()
        {
            AssertAssemblyLoaded(
                "MiradorWorks.Settings.URP",
                "MiradorWorks.Settings.URP was not compiled. Check that URP 17+ is installed and the versionDefines in its asmdef.");
        }

        static void AssertAssemblyLoaded(string name, string message = null)
        {
#if UNITY_6000_4_OR_NEWER
            bool loaded = CurrentAssemblies.GetLoadedAssemblies().Any(a => a.GetName().Name == name);
            Assert.That(loaded, Is.True, message ?? $"{name} is not loaded.");
#else
            Assert.Ignore("Requires Unity 6000.4+ (CurrentAssemblies API).");
#endif
        }
    }
}