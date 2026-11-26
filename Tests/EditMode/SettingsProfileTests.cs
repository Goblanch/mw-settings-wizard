using NUnit.Framework;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class SettingsProfileTests
    {
        TestAssets assets;

        [SetUp]
        public void SetUp() => assets = new TestAssets();

        [TearDown]
        public void TearDown() => assets.DestroyAll();

        [Test]
        public void Validate_EmptyProfile_HasNoErrors()
        {
            SettingsProfile profile = assets.Profile();
            Assert.That(profile.Validate(), Is.Empty);
        }

        [Test]
        public void Validate_ValidProfile_HasNoErrors()
        {
            SettingsProfile profile = assets.Profile(
                assets.Setting<FakeToggleSetting>("display.vsync"),
                assets.Setting<FakeRangeSetting>("graphics.renderscale"));

            Assert.That(profile.Validate(), Is.Empty);
        }

        [Test]
        public void Validate_NullEntry_ReportsIndex()
        {
            SettingsProfile profile = assets.Profile(
                assets.Setting<FakeToggleSetting>("display.vsync"),
                null);

            Assert.That(profile.Validate(), Has.Count.EqualTo(1));
            Assert.That(profile.Validate()[0], Does.Contain("index 1"));
        }

        [Test]
        public void Validate_EmptyId_ReportsAssetName()
        {
            SettingsProfile profile = assets.Profile(
                assets.Setting<FakeToggleSetting>("", name: "VSync"));

            Assert.That(profile.Validate(), Has.Count.EqualTo(1));
            Assert.That(profile.Validate()[0], Does.Contain("'VSync' has no id"));
        }

        [Test]
        public void Validate_InvalidId_ReportsIt()
        {
            SettingsProfile profile = assets.Profile(
                assets.Setting<FakeToggleSetting>("has space", name: "VSync"));

            Assert.That(profile.Validate(), Has.Count.EqualTo(1));
            Assert.That(profile.Validate()[0], Does.Contain("invalid id 'has space'"));
        }

        [Test]
        public void Validate_DuplicateIds_ReportsBothAssets()
        {
            SettingsProfile profile = assets.Profile(
                assets.Setting<FakeToggleSetting>("display.vsync", name: "VSync"),
                assets.Setting<FakeToggleSetting>("display.vsync", name: "VSyncCopy"));

            Assert.That(profile.Validate(), Has.Count.EqualTo(1));
            Assert.That(profile.Validate()[0], Does.Contain("'VSync'").And.Contain("'VSyncCopy'"));
        }

        [Test]
        public void Validate_SameAssetTwice_ReportsOnce()
        {
            FakeToggleSetting vsync = assets.Setting<FakeToggleSetting>("display.vsync", name: "VSync");
            SettingsProfile profile = assets.Profile(vsync, vsync);

            Assert.That(profile.Validate(), Has.Count.EqualTo(1));
            Assert.That(profile.Validate()[0], Does.Contain("listed more than once"));
        }

        [Test]
        public void Validate_ReportsEveryError()
        {
            SettingsProfile profile = assets.Profile(
                null,
                assets.Setting<FakeToggleSetting>("", name: "NoId"),
                assets.Setting<FakeToggleSetting>("a", name: "A1"),
                assets.Setting<FakeToggleSetting>("a", name: "A2"));

            Assert.That(profile.Validate(), Has.Count.EqualTo(3));
        }

        [Test]
        public void TryGet_ExistingId_ReturnsSetting()
        {
            FakeToggleSetting vsync = assets.Setting<FakeToggleSetting>("display.vsync");
            SettingsProfile profile = assets.Profile(null, vsync);

            Assert.That(profile.TryGet("display.vsync", out SettingDefinition found), Is.True);
            Assert.That(found, Is.SameAs(vsync));
        }

        [Test]
        public void TryGet_MissingId_ReturnsFalse()
        {
            SettingsProfile profile = assets.Profile(assets.Setting<FakeToggleSetting>("display.vsync"));

            Assert.That(profile.TryGet("missing", out SettingDefinition found), Is.False);
            Assert.That(found, Is.Null);
            Assert.That(profile.TryGet(null, out _), Is.False);
        }
    }
}