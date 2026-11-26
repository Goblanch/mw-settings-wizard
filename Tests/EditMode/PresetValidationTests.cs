using NUnit.Framework;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class PresetValidationTests
    {
        TestAssets assets;
        FakeToggleSetting shadows;
        FakeToggleSetting bloom;

        [SetUp]
        public void SetUp()
        {
            assets = new TestAssets();
            shadows = assets.Setting<FakeToggleSetting>("graphics.shadows", name: "Shadows");
            bloom = assets.Setting<FakeToggleSetting>("graphics.bloom", name: "Bloom");
        }

        [TearDown]
        public void TearDown() => assets.DestroyAll();

        SettingsProfile ProfileWith(params SettingsPreset[] presets) =>
            assets.ProfileWithPresets(new SettingDefinition[] { shadows, bloom }, presets);

        [Test]
        public void ValidPresets_HaveNoErrors()
        {
            SettingsProfile profile = ProfileWith(
                assets.Preset("low", (shadows, "false"), (bloom, "false")),
                assets.Preset("high", (shadows, "true"), (bloom, "true")));

            Assert.That(profile.Validate(), Is.Empty);
        }

        [Test]
        public void NullPreset_IsReported()
        {
            SettingsProfile profile = ProfileWith(new SettingsPreset[] { null });

            Assert.That(profile.Validate(), Has.Count.EqualTo(1));
            Assert.That(profile.Validate()[0], Does.Contain("Preset at index 0"));
        }

        [Test]
        public void InvalidKey_IsReported()
        {
            SettingsProfile profile = ProfileWith(assets.Preset("has space", (shadows, "true")));

            Assert.That(profile.Validate()[0], Does.Contain("invalid key 'has space'"));
        }

        [Test]
        public void ReservedCustomKey_IsReported()
        {
            SettingsProfile profile = ProfileWith(assets.Preset(PresetSetting.CustomKey, (shadows, "true")));

            Assert.That(profile.Validate()[0], Does.Contain("reserved key"));
        }

        [Test]
        public void DuplicateKeys_AreReported()
        {
            SettingsProfile profile = ProfileWith(
                assets.Preset("high", (shadows, "true")),
                assets.Preset("high", (bloom, "true")));

            Assert.That(profile.Validate(), Has.Count.EqualTo(1));
            Assert.That(profile.Validate()[0], Does.Contain("Duplicate preset key 'high'"));
        }

        [Test]
        public void SettingNotInProfile_IsReported()
        {
            FakeToggleSetting outside = assets.Setting<FakeToggleSetting>("outside", name: "Outside");
            SettingsProfile profile = ProfileWith(assets.Preset("high", (outside, "true")));

            Assert.That(profile.Validate()[0], Does.Contain("'Outside', which is not in the profile"));
        }

        [Test]
        public void InvalidValue_IsReported()
        {
            SettingsProfile profile = ProfileWith(assets.Preset("high", (shadows, "maybe")));

            Assert.That(profile.Validate()[0], Does.Contain("invalid value 'maybe' for 'Shadows'"));
        }

        [Test]
        public void SameSettingTwiceInAPreset_IsReported()
        {
            SettingsProfile profile = ProfileWith(assets.Preset("high", (shadows, "true"), (shadows, "false")));

            Assert.That(profile.Validate()[0], Does.Contain("sets 'Shadows' more than once"));
        }

        [Test]
        public void PresetSettingTheSelector_IsReported()
        {
            PresetSetting selector = assets.Setting<PresetSetting>("graphics.preset", name: "Preset");
            SettingsProfile profile = assets.ProfileWithPresets(
                new SettingDefinition[] { shadows, selector },
                assets.Preset("high", (selector, "high")));

            Assert.That(profile.Validate()[0], Does.Contain("cannot set the preset selector"));
        }

        [Test]
        public void TwoSelectors_AreReported()
        {
            SettingsProfile profile = assets.Profile(
                assets.Setting<PresetSetting>("preset.a"),
                assets.Setting<PresetSetting>("preset.b"));

            Assert.That(profile.Validate(), Has.Count.EqualTo(1));
            Assert.That(profile.Validate()[0], Does.Contain("Only one PresetSetting"));
        }
    }
}