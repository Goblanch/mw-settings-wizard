using System;
using NUnit.Framework;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class SettingsContextTests
    {
        TestAssets assets;
        SettingsService service;

        [SetUp]
        public void SetUp() => assets = new TestAssets();

        [TearDown]
        public void TearDown()
        {
            service?.Dispose();
            service = null;
            assets.DestroyAll();
        }

        FakeToggleSetting Toggle(string id, ApplyMode mode = ApplyMode.Immediate) =>
            assets.Setting<FakeToggleSetting>(id, applyMode: mode);

        void Initialize(params SettingDefinition[] settings)
        {
            service = new SettingsService(assets.Profile(settings), new InMemorySettingsStorage());
            service.Initialize();
        }

        [Test]
        public void StandaloneContext_KnowsNoSettings()
        {
            FakeToggleSetting vsync = Toggle("display.vsync");
            var context = new SettingsContext();

            Assert.That(context.TryGetValue(vsync, out _), Is.False);
            Assert.That(context.TryGetValue<bool>("display.vsync", out _), Is.False);
        }

        [Test]
        public void TryGetValue_ReturnsCurrentAndTryGetApplied_ReturnsApplied()
        {
            FakeToggleSetting vsync = Toggle("display.vsync", ApplyMode.Deferred);
            Initialize(vsync);
            service.Set(vsync, true);

            Assert.That(service.Context.TryGetValue(vsync, out bool current), Is.True);
            Assert.That(current, Is.True);
            Assert.That(service.Context.TryGetApplied(vsync, out bool appliedValue), Is.True);
            Assert.That(appliedValue, Is.False);
        }

        [Test]
        public void TryGetValue_ById_ChecksIdAndType()
        {
            FakeToggleSetting vsync = Toggle("display.vsync");
            Initialize(vsync);
            service.Set(vsync, true);

            Assert.That(service.Context.TryGetValue("display.vsync", out bool value), Is.True);
            Assert.That(value, Is.True);
            Assert.That(service.Context.TryGetValue<float>("display.vsync", out _), Is.False);
            Assert.That(service.Context.TryGetValue<bool>("unknown", out _), Is.False);
        }

        [Test]
        public void TryGetValue_DuringLoad_OnlySeesSettingsAlreadyLoaded()
        {
            FakeToggleSetting first = Toggle("first");
            FakeToggleSetting second = Toggle("second");
            bool firstSawSecond = true;
            bool secondSawFirst = false;
            first.DefaultProvider = context => { firstSawSecond = context.TryGetValue(second, out _); return false; };
            second.DefaultProvider = context => { secondSawFirst = context.TryGetValue(first, out _); return false; };

            Initialize(first, second);

            Assert.That(firstSawSecond, Is.False);
            Assert.That(secondSawFirst, Is.True);
        }

        [Test]
        public void GetOrAddService_CreatesOnlyOnce()
        {
            Initialize(Toggle("display.vsync"));
            int created = 0;

            FakeDisposableService a = service.Context.GetOrAddService(() => { created++; return new FakeDisposableService(); });
            FakeDisposableService b = service.Context.GetOrAddService(() => { created++; return new FakeDisposableService(); });

            Assert.That(created, Is.EqualTo(1));
            Assert.That(b, Is.SameAs(a));
        }

        [Test]
        public void GetOrAddService_FactoryReturningNull_Throws()
        {
            Initialize(Toggle("display.vsync"));

            Assert.Throws<InvalidOperationException>(
                () => service.Context.GetOrAddService<FakeDisposableService>(() => null));
        }

        [Test]
        public void Services_AreDisposedWithTheSettingsService()
        {
            Initialize(Toggle("display.vsync"));
            FakeDisposableService shared = service.Context.GetOrAddService(() => new FakeDisposableService());

            service.Dispose();

            Assert.That(shared.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void AfterDispose_TryGetValue_ReturnsFalse()
        {
            FakeToggleSetting vsync = Toggle("display.vsync");
            Initialize(vsync);
            SettingsContext context = service.Context;

            service.Dispose();

            Assert.That(context.TryGetValue(vsync, out _), Is.False);
        }
    }
}