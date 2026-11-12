using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace MiradorWorks.Settings.Tests.PlayMode
{
    public sealed class PlayModeSmokeTests
    {
        [UnityTest]
        public IEnumerator PlayMode_AdvancesOneFrame()
        {
            yield return null;
            Assert.Pass();
        }
    }
}