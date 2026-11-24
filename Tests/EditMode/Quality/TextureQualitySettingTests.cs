using System.Linq;
using NUnit.Framework;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class TextureQualitySettingTests
    {
        [TestCase(TextureQualitySetting.Full, 0)]
        [TestCase(TextureQualitySetting.Half, 1)]
        [TestCase(TextureQualitySetting.Quarter, 2)]
        [TestCase(TextureQualitySetting.Eighth, 3)]
        [TestCase("unknown", 0)]
        public void MipmapLimitFor_MapsKeys(string key, int expected)
        {
            Assert.That(TextureQualitySetting.MipmapLimitFor(key), Is.EqualTo(expected));
        }

        [Test]
        public void Options_GoFromFullToEighth()
        {
            var assets = new TestAssets();
            try
            {
                var setting = assets.Setting<TextureQualitySetting>("graphics.textures");

                string[] keys = setting.GetOptions(new SettingsContext()).Select(option => option.Key).ToArray();

                Assert.That(keys, Is.EqualTo(new[]
                {
                    TextureQualitySetting.Full, TextureQualitySetting.Half,
                    TextureQualitySetting.Quarter, TextureQualitySetting.Eighth
                }));
            }
            finally
            {
                assets.DestroyAll();
            }
        }
    }
}