using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class SettingsServiceTests
    {
        TestAssets assets;
        InMemorySettingsStorage storage;
        SettingsService service;
        List<SettingDefinition> changed;
        List<SettingDefinition> applied;

        [SetUp]
        public void SetUp()
        {
            assets = new TestAssets();
            storage = new InMemorySettingsStorage();
            changed = new List<SettingDefinition>();
            applied = new List<SettingDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            service?.Dispose();
            service = null;
            assets.DestroyAll();
        }

        // ---------- helpers ----------

        FakeToggleSetting Toggle(string id, bool defaultValue = false,
            ApplyMode mode = ApplyMode.Immediate, SettingCategory category = null)
        {
            var setting = assets.Setting<FakeToggleSetting>(id, applyMode: mode, category: category);
            assets.Configure(setting, so => so.FindProperty("defaultValue").boolValue = defaultValue);
            return setting;
        }

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

        void Create(params SettingDefinition[] settings)
        {
            service = new SettingsService(assets.Profile(settings), storage);
        }

        void Initialize(params SettingDefinition[] settings)
        {
            Create(settings);
            service.Initialize();
            service.ValueChanged += changed.Add;
            service.ValueApplied += applied.Add;
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

        // ---------- construction and initialization ----------

        [Test]
        public void Constructor_NullArguments_Throw()
        {
            SettingsProfile profile = assets.Profile();
            Assert.Throws<ArgumentNullException>(() => new SettingsService(null, storage));
            Assert.Throws<ArgumentNullException>(() => new SettingsService(profile, null));
        }

        [Test]
        public void Get_BeforeInitialize_Throws()
        {
            FakeToggleSetting vsync = Toggle("display.vsync");
            Create(vsync);

            Assert.Throws<InvalidOperationException>(() => service.Get(vsync));
        }

        [Test]
        public void Initialize_InvalidProfile_ThrowsWithErrors()
        {
            Create(Toggle("a"), Toggle("a"));

            var exception = Assert.Throws<InvalidOperationException>(() => service.Initialize());
            Assert.That(exception.Message, Does.Contain("Duplicate id 'a'"));
        }

        [Test]
        public void Initialize_Twice_Throws()
        {
            Initialize(Toggle("display.vsync"));
            Assert.Throws<InvalidOperationException>(() => service.Initialize());
        }

        [Test]
        public void Initialize_WithoutSavedData_AppliesDefaultsWithoutSaving()
        {
            FakeToggleSetting vsync = Toggle("display.vsync", defaultValue: true);
            Initialize(vsync);

            Assert.That(service.Get(vsync), Is.True);
            Assert.That(vsync.ApplyCount, Is.EqualTo(1));
            Assert.That(vsync.LastApplied, Is.True);
            Assert.That(storage.SaveCount, Is.Zero);
        }

        [Test]
        public void Initialize_SavedValue_IsLoadedAndSanitized()
        {
            storage = new InMemorySettingsStorage(Data(("graphics.scale", "7")));
            FakeRangeSetting scale = Range("graphics.scale", 0.5f, 2f, 1f);

            Initialize(scale);

            Assert.That(service.Get(scale), Is.EqualTo(2f));
            Assert.That(scale.LastApplied, Is.EqualTo(2f));
        }

        [Test]
        public void Initialize_InvalidSavedValue_UsesDefaultAndWarns()
        {
            storage = new InMemorySettingsStorage(Data(("display.vsync", "maybe")));
            FakeToggleSetting vsync = Toggle("display.vsync", defaultValue: true);
            LogAssert.Expect(LogType.Warning, new Regex("invalid"));

            Initialize(vsync);

            Assert.That(service.Get(vsync), Is.True);
        }

        [Test]
        public void Initialize_UnsupportedSetting_IsNotApplied()
        {
            FakeToggleSetting vsync = Toggle("display.vsync", defaultValue: true);
            vsync.Supported = false;

            Initialize(vsync);

            Assert.That(service.IsSupported(vsync), Is.False);
            Assert.That(vsync.ApplyCount, Is.Zero);
            Assert.That(service.Get(vsync), Is.True);
        }

        [Test]
        public void Initialize_ApplyFailure_IsLoggedAndOthersStillApply()
        {
            FakeToggleSetting broken = Toggle("broken");
            broken.ThrowOnApply = true;
            FakeToggleSetting other = Toggle("other");
            LogAssert.Expect(LogType.Error, new Regex("Applying 'broken' failed"));

            Initialize(broken, other);

            Assert.That(other.ApplyCount, Is.EqualTo(1));
        }

        // ---------- immediate settings ----------

        [Test]
        public void Set_Immediate_AppliesSavesAndNotifies()
        {
            FakeToggleSetting vsync = Toggle("display.vsync");
            Initialize(vsync);

            service.Set(vsync, true);

            Assert.That(service.Get(vsync), Is.True);
            Assert.That(vsync.ApplyCount, Is.EqualTo(2));
            Assert.That(vsync.LastApplied, Is.True);
            Assert.That(storage.SaveCount, Is.EqualTo(1));
            Assert.That(Saved("display.vsync"), Is.EqualTo("true"));
            Assert.That(changed, Is.EqualTo(new[] { vsync }));
            Assert.That(applied, Is.EqualTo(new[] { vsync }));
        }

        [Test]
        public void Set_SameValue_DoesNothing()
        {
            FakeToggleSetting vsync = Toggle("display.vsync");
            Initialize(vsync);

            service.Set(vsync, false);

            Assert.That(vsync.ApplyCount, Is.EqualTo(1));
            Assert.That(storage.SaveCount, Is.Zero);
            Assert.That(changed, Is.Empty);
        }

        [Test]
        public void Set_SanitizesValue()
        {
            FakeRangeSetting scale = Range("graphics.scale", 0.5f, 2f, 1f);
            Initialize(scale);

            service.Set(scale, 5f);

            Assert.That(service.Get(scale), Is.EqualTo(2f));
        }

        [Test]
        public void Set_SettingNotInProfile_Throws()
        {
            FakeToggleSetting outside = Toggle("outside");
            Initialize(Toggle("display.vsync"));

            Assert.Throws<ArgumentException>(() => service.Set(outside, true));
        }

        // ---------- deferred settings ----------

        [Test]
        public void Set_Deferred_KeepsValuePendingUntilApplied()
        {
            FakeToggleSetting vsync = Toggle("display.vsync", mode: ApplyMode.Deferred);
            Initialize(vsync);

            service.Set(vsync, true);

            Assert.That(service.Get(vsync), Is.True);
            Assert.That(service.GetApplied(vsync), Is.False);
            Assert.That(service.HasPendingChanges, Is.True);
            Assert.That(service.HasPending(vsync), Is.True);
            Assert.That(vsync.ApplyCount, Is.EqualTo(1));
            Assert.That(storage.SaveCount, Is.Zero);
            Assert.That(changed, Is.EqualTo(new[] { vsync }));
            Assert.That(applied, Is.Empty);
        }

        [Test]
        public void Set_DeferredBackToAppliedValue_ClearsPending()
        {
            FakeToggleSetting vsync = Toggle("display.vsync", mode: ApplyMode.Deferred);
            Initialize(vsync);

            service.Set(vsync, true);
            service.Set(vsync, false);

            Assert.That(service.HasPendingChanges, Is.False);
        }

        [Test]
        public void ApplyPending_AppliesEverythingAndSavesOnce()
        {
            FakeToggleSetting a = Toggle("a", mode: ApplyMode.Deferred);
            FakeToggleSetting b = Toggle("b", mode: ApplyMode.Deferred);
            Initialize(a, b);
            service.Set(a, true);
            service.Set(b, true);

            service.ApplyPending();

            Assert.That(a.ApplyCount, Is.EqualTo(2));
            Assert.That(b.ApplyCount, Is.EqualTo(2));
            Assert.That(storage.SaveCount, Is.EqualTo(1));
            Assert.That(service.HasPendingChanges, Is.False);
            Assert.That(applied, Is.EqualTo(new[] { a, b }));
            Assert.That(Saved("a"), Is.EqualTo("true"));
        }

        [Test]
        public void ApplyPending_NothingPending_DoesNotSave()
        {
            Initialize(Toggle("a", mode: ApplyMode.Deferred));

            service.ApplyPending();

            Assert.That(storage.SaveCount, Is.Zero);
        }

        [Test]
        public void RevertPending_RestoresAppliedValue()
        {
            FakeToggleSetting vsync = Toggle("display.vsync", mode: ApplyMode.Deferred);
            Initialize(vsync);
            service.Set(vsync, true);
            changed.Clear();

            service.RevertPending();

            Assert.That(service.Get(vsync), Is.False);
            Assert.That(service.HasPendingChanges, Is.False);
            Assert.That(vsync.ApplyCount, Is.EqualTo(1));
            Assert.That(storage.SaveCount, Is.Zero);
            Assert.That(changed, Is.EqualTo(new[] { vsync }));
        }

        // ---------- reset ----------

        [Test]
        public void ResetToDefaults_RestoresDefaultsAndSavesOnce()
        {
            FakeToggleSetting a = Toggle("a", defaultValue: true);
            FakeToggleSetting b = Toggle("b", defaultValue: true);
            Initialize(a, b);
            service.Set(a, false);
            service.Set(b, false);

            service.ResetToDefaults();

            Assert.That(service.Get(a), Is.True);
            Assert.That(service.Get(b), Is.True);
            Assert.That(storage.SaveCount, Is.EqualTo(3));
        }

        [Test]
        public void ResetToDefaults_WithCategory_OnlyResetsThatCategory()
        {
            SettingCategory display = assets.Category("display");
            SettingCategory graphics = assets.Category("graphics");
            FakeToggleSetting a = Toggle("a", defaultValue: true, category: display);
            FakeToggleSetting b = Toggle("b", defaultValue: true, category: graphics);
            Initialize(a, b);
            service.Set(a, false);
            service.Set(b, false);

            service.ResetToDefaults(display);

            Assert.That(service.Get(a), Is.True);
            Assert.That(service.Get(b), Is.False);
        }

        // ---------- persistence ----------

        [Test]
        public void Save_KeepsUnknownSavedIds()
        {
            storage = new InMemorySettingsStorage(Data(("removed.setting", "x")));
            FakeToggleSetting vsync = Toggle("display.vsync");
            Initialize(vsync);

            service.Set(vsync, true);

            Assert.That(Saved("removed.setting"), Is.EqualTo("x"));
        }

        [Test]
        public void SaveFailure_IsLoggedAndValueKept()
        {
            FakeToggleSetting vsync = Toggle("display.vsync");
            service = new SettingsService(assets.Profile(vsync), new ThrowingSaveStorage());
            service.Initialize();
            LogAssert.Expect(LogType.Error, new Regex("Could not save settings"));

            service.Set(vsync, true);

            Assert.That(service.Get(vsync), Is.True);
        }

        // ---------- events and lifetime ----------

        [Test]
        public void FailingHandler_DoesNotStopOtherHandlers()
        {
            FakeToggleSetting vsync = Toggle("display.vsync");
            Create(vsync);
            service.Initialize();
            service.ValueChanged += _ => throw new InvalidOperationException("boom");
            service.ValueChanged += changed.Add;
            LogAssert.Expect(LogType.Error, new Regex("boom"));

            service.Set(vsync, true);

            Assert.That(changed, Is.EqualTo(new[] { vsync }));
            Assert.That(storage.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_ThenGet_Throws()
        {
            FakeToggleSetting vsync = Toggle("display.vsync");
            Initialize(vsync);

            service.Dispose();

            Assert.Throws<ObjectDisposedException>(() => service.Get(vsync));
        }
    }
}