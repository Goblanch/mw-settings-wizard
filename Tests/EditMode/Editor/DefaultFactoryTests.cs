using System.Linq;
using MiradorWorks.Settings.Editor;
using NUnit.Framework;
using UnityEditor;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class DefaultProfileFactoryTests
    {
        const string Root = "Assets/__MwSettingsTests";

        SettingsProfile profile;

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(Root);
            AssetDatabase.CreateFolder("Assets", "__MwSettingsTests");
            profile = DefaultProfileFactory.Create(Root);
        }

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(Root);

        [Test]
        public void CreatesAValidProfile()
        {
            Assert.That(profile.Validate(), Is.Empty);
        }

        [Test]
        public void SavesEveryAssetInsideTheFolder()
        {
            Assert.That(AssetDatabase.GetAssetPath(profile), Does.StartWith(Root + "/"));
            foreach (SettingDefinition setting in profile.Settings)
                Assert.That(AssetDatabase.GetAssetPath(setting), Does.StartWith(Root + "/"));
        }

        [TestCase("display.mode")]
        [TestCase("display.resolution")]
        [TestCase("display.vsync")]
        [TestCase("display.fps")]
        [TestCase("graphics.textures")]
        [TestCase("graphics.preset")]
        // [T4.1] NEW
        [TestCase("audio.master")]
        [TestCase("audio.mute")]
        // [T4.1] END
        public void IncludesTheCoreSettingsWithSuggestedIds(string id)
        {
            Assert.That(profile.TryGet(id, out _), Is.True);
        }

        [TestCase("display.mode")]
        [TestCase("display.resolution")]
        public void DisplaySettings_AreDeferredAndRequireConfirmation(string id)
        {
            profile.TryGet(id, out SettingDefinition setting);

            Assert.That(setting.ApplyMode, Is.EqualTo(ApplyMode.Deferred));
            Assert.That(setting.RequiresConfirmation, Is.True);
        }

        [Test]
        public void VSync_IsOnByDefault()
        {
            profile.TryGet("display.vsync", out SettingDefinition setting);

            Assert.That(((VSyncSetting)setting).GetDefault(new SettingsContext()), Is.True);
        }

        // [T4.1] NEW
        [Test]
        public void AudioSettings_ShareAListenerChannelSavedInTheFolder()
        {
            profile.TryGet("audio.master", out SettingDefinition master);
            profile.TryGet("audio.mute", out SettingDefinition mute);

            AudioChannel channel = ((VolumeSetting)master).Channel;

            Assert.That(channel, Is.InstanceOf<ListenerChannel>());
            Assert.That(((MuteSetting)mute).Channel, Is.SameAs(channel));
            Assert.That(AssetDatabase.GetAssetPath(channel), Does.StartWith(Root + "/"));
        }

        [Test]
        public void AudioSettings_AreNotInPresets()
        {
            bool inPresets = profile.Presets
                .SelectMany(preset => preset.Entries)
                .Any(entry => entry.Setting is IAudioChannelSetting);

            Assert.That(inPresets, Is.False);
        }
        // [T4.1] END

        [Test]
        public void CreatesLowMediumHighPresetsAndOneSelector()
        {
            Assert.That(profile.Presets.Select(preset => preset.Key), Is.EqualTo(new[] { "low", "medium", "high" }));
            Assert.That(profile.Settings.OfType<PresetSetting>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void PresetSelector_ComesBeforeTheGraphicsSettings()
        {
            int selector = profile.Settings.ToList().FindIndex(s => s is PresetSetting);
            int textures = profile.Settings.ToList().FindIndex(s => s is TextureQualitySetting);

            Assert.That(selector, Is.LessThan(textures));
        }
    }
}