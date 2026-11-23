using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class PresetTests
    {
        TestAssets assets;
        InMemorySettingsStorage storage;
        SettingsService service;
        List<SettingDefinition> changed;

        FakeToggleSetting shadows;
        FakeToggleSetting bloom;
        FakeRangeSetting scale;
        FakeToggleSetting vsync;
        PresetSetting selector;
        SettingsPreset low;
        SettingsPreset high;

        [SetUp]
        public void SetUp()
        {
            assets = new TestAssets();
            storage = new InMemorySettingsStorage();
            changed = new List<SettingDefinition>();

            shadows = assets.Setting<FakeToggleSetting>("graphics.shadows");
            bloom = assets.Setting<FakeToggleSetting>("graphics.bloom");
            scale = Range("graphics.scale", 0.5f, 2f, 1f);
            vsync = assets.Setting<FakeToggleSetting>("display.vsync");
            selector = assets.Setting<PresetSetting>("graphics.preset");

            low = assets.Preset("low", (shadows, "false"), (bloom, "false"), (scale, "0.75"));
            high = assets.Preset("high", (shadows, "true"), (bloom, "true"), (scale, "1"));
        }

        [TearDown]
        public void TearDown()
        {
            service?.Dispose();
            service = null;
            assets.DestroyAll();
        }

        // ---------- helpers ----------

        FakeRangeSetting Range(string id, float min, float max, float defaultValue)
        {
            var setting = assets.Setting<FakeRangeSetting>(id);
            assets.Configure(setting, so =>
            {
                so.FindProperty("min").floatValue = min;
                so.FindProperty("max").floatValue = max;
                so.FindProperty("defaultValue").floatValue = defaultValue;
            });
            return setting;
        }

        void Initialize() =>
            Initialize(new SettingDefinition[] { shadows, bloom, scale, vsync, selector }, low, high);

        void Initialize(SettingDefinition[] settings, params SettingsPreset[] presets)
        {
            service = new SettingsService(assets.ProfileWithPresets(settings, presets), storage);
            service.Initialize();
            service.ValueChanged += changed.Add;
        }

        static SettingsData Data(params (string id, string value)[] entries)
        {
            var data = new SettingsData();
            foreach ((string id, string value) in entries)
                data.Set(id, value);
            return data;
        }

        string Saved(string id)
        {
            if (!storage.TryLoad(out SettingsData data))
                return null;

            return data.TryGetValue(id, out string value) ? value : null;
        }

        // ---------- active preset ----------

        [Test]
        public void Initialize_NoPresetMatches_SelectorIsCustom()
        {
            Initialize();

            Assert.That(service.Get(selector), Is.EqualTo(PresetSetting.CustomKey));
            Assert.That(service.ActivePreset, Is.Null);
        }

        [Test]
        public void Initialize_SavedValuesMatchAPreset_SelectorShowsIt()
        {
            storage = new InMemorySettingsStorage(Data(
                ("graphics.shadows", "false"), ("graphics.bloom", "false"), ("graphics.scale", "0.75")));

            Initialize();

            Assert.That(service.Get(selector), Is.EqualTo("low"));
            Assert.That(service.ActivePreset, Is.SameAs(low));
        }

        [Test]
        public void ChangingASettingOfThePreset_SwitchesToCustom()
        {
            Initialize();
            service.ApplyPreset(high);
            changed.Clear();

            service.Set(shadows, false);

            Assert.That(service.Get(selector), Is.EqualTo(PresetSetting.CustomKey));
            Assert.That(changed, Does.Contain(selector));
        }

        [Test]
        public void ChangingASettingOutsidePresets_KeepsThePreset()
        {
            Initialize();
            service.ApplyPreset(high);

            service.Set(vsync, true);

            Assert.That(service.Get(selector), Is.EqualTo("high"));
        }

        // ---------- applying ----------

        [Test]
        public void ApplyPreset_SetsEveryEntryAndSavesOnce()
        {
            Initialize();

            service.ApplyPreset(high);

            Assert.That(service.Get(shadows), Is.True);
            Assert.That(service.Get(bloom), Is.True);
            Assert.That(service.Get(scale), Is.EqualTo(1f));
            Assert.That(storage.SaveCount, Is.EqualTo(1));
            Assert.That(service.ActivePreset, Is.SameAs(high));
        }

        [Test]
        public void SelectingAPresetInTheSelector_AppliesIt()
        {
            Initialize();

            service.Set(selector, "low");

            Assert.That(service.Get(scale), Is.EqualTo(0.75f));
            Assert.That(service.Get(selector), Is.EqualTo("low"));
        }

        [Test]
        public void SelectingCustom_DoesNothing()
        {
            Initialize();
            service.ApplyPreset(high);

            service.Set(selector, PresetSetting.CustomKey);

            Assert.That(service.Get(selector), Is.EqualTo("high"));
            Assert.That(service.Get(shadows), Is.True);
        }

        [Test]
        public void DeferredSettings_PresetIsPendingUntilApplied()
        {
            var deferred = assets.Setting<FakeToggleSetting>("graphics.ao", applyMode: ApplyMode.Deferred);
            SettingsPreset ultra = assets.Preset("ultra", (deferred, "true"));
            Initialize(new SettingDefinition[] { deferred, selector }, ultra);

            service.ApplyPreset(ultra);

            Assert.That(service.HasPending(deferred), Is.True);
            Assert.That(service.Get(selector), Is.EqualTo("ultra"));

            service.RevertPending();

            Assert.That(service.Get(selector), Is.EqualTo(PresetSetting.CustomKey));
        }

        [Test]
        public void ApplyPreset_NotInProfile_Throws()
        {
            Initialize(new SettingDefinition[] { shadows, bloom, scale, vsync, selector }, low);

            Assert.Throws<ArgumentException>(() => service.ApplyPreset(high));
        }

        // ---------- reset and persistence ----------

        [Test]
        public void ResetToDefaults_SkipsSelectorAndRecomputesIt()
        {
            Initialize();
            service.ApplyPreset(high);

            service.ResetToDefaults();

            Assert.That(service.Get(shadows), Is.False);
            Assert.That(service.Get(selector), Is.EqualTo(PresetSetting.CustomKey));
        }

        [Test]
        public void SelectorKey_IsSaved()
        {
            Initialize();

            service.ApplyPreset(low);

            Assert.That(Saved("graphics.preset"), Is.EqualTo("low"));
        }

        // ---------- options ----------

        [Test]
        public void GetOptions_ListsPresetsThenCustom()
        {
            Initialize();

            IEnumerable<string> keys = selector.GetOptions(service.Context).Select(option => option.Key);

            Assert.That(keys, Is.EqualTo(new[] { "low", "high", PresetSetting.CustomKey }));
        }
    }
}