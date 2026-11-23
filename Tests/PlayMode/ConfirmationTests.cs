using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiradorWorks.Settings.Tests.PlayMode
{
    public sealed class ConfirmationTests
    {
        PlayModeAssets assets;
        InMemorySettingsStorage storage;
        SettingsService service;
        ConfirmationRequest lastRequest;
        float previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            assets = new PlayModeAssets();
            storage = new InMemorySettingsStorage();
            lastRequest = null;
            previousTimeScale = Time.timeScale;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = previousTimeScale;
            service?.Dispose();
            service = null;
            assets.DestroyAll();
        }

        // ---------- helpers ----------

        PlayModeToggleSetting Confirmable(string id, ApplyMode mode = ApplyMode.Immediate) =>
            assets.Setting<PlayModeToggleSetting>(id, mode, requiresConfirmation: true);

        PlayModeToggleSetting Plain(string id) => assets.Setting<PlayModeToggleSetting>(id);

        void Initialize(float timeoutSeconds, params SettingDefinition[] settings)
        {
            service = new SettingsService(assets.Profile(settings), storage)
            {
                ConfirmationTimeoutSeconds = timeoutSeconds
            };
            service.Initialize();
            service.ConfirmationRequested += request => lastRequest = request;
        }

        string Saved(string id)
        {
            if (!storage.TryLoad(out SettingsData data))
                return null;

            return data.TryGetValue(id, out string value) ? value : null;
        }

        static IEnumerator WaitUntilCompleted(ConfirmationRequest request, float maxSeconds)
        {
            double end = Time.realtimeSinceStartupAsDouble + maxSeconds;
            while (!request.IsCompleted && Time.realtimeSinceStartupAsDouble < end)
                yield return null;
        }

        static IEnumerator WaitRealSeconds(float seconds)
        {
            double end = Time.realtimeSinceStartupAsDouble + seconds;
            while (Time.realtimeSinceStartupAsDouble < end)
                yield return null;
        }

        // ---------- requesting ----------

        [Test]
        public void Set_ConfirmableSetting_RequestsConfirmation()
        {
            PlayModeToggleSetting resolution = Confirmable("display.resolution");
            Initialize(15f, resolution);

            service.Set(resolution, true);

            Assert.That(lastRequest, Is.Not.Null);
            Assert.That(lastRequest.Settings, Is.EqualTo(new[] { resolution }));
            Assert.That(lastRequest.IsCompleted, Is.False);
            Assert.That(service.ActiveConfirmation, Is.SameAs(lastRequest));
            Assert.That(service.Get(resolution), Is.True);
            Assert.That(resolution.LastApplied, Is.True);
        }

        [Test]
        public void WhileAwaitingConfirmation_FileKeepsPreviousValue()
        {
            PlayModeToggleSetting resolution = Confirmable("display.resolution");
            Initialize(15f, resolution);

            service.Set(resolution, true);

            Assert.That(Saved("display.resolution"), Is.EqualTo("false"));
        }

        [Test]
        public void OtherSettings_AreSavedNormallyDuringConfirmation()
        {
            PlayModeToggleSetting resolution = Confirmable("display.resolution");
            PlayModeToggleSetting vsync = Plain("display.vsync");
            Initialize(15f, resolution, vsync);

            service.Set(resolution, true);
            service.Set(vsync, true);

            Assert.That(Saved("display.vsync"), Is.EqualTo("true"));
            Assert.That(Saved("display.resolution"), Is.EqualTo("false"));
            Assert.That(lastRequest.IsCompleted, Is.False);
        }

        [Test]
        public void ApplyPending_DeferredConfirmableSetting_RequestsConfirmation()
        {
            PlayModeToggleSetting resolution = Confirmable("display.resolution", ApplyMode.Deferred);
            Initialize(15f, resolution);
            service.Set(resolution, true);
            Assert.That(lastRequest, Is.Null);

            service.ApplyPending();

            Assert.That(lastRequest, Is.Not.Null);
            Assert.That(service.GetApplied(resolution), Is.True);
        }

        // ---------- completing ----------

        [Test]
        public void Confirm_KeepsValueAndSavesIt()
        {
            PlayModeToggleSetting resolution = Confirmable("display.resolution");
            Initialize(15f, resolution);
            service.Set(resolution, true);
            bool? result = null;
            lastRequest.Completed += confirmed => result = confirmed;

            lastRequest.Confirm();

            Assert.That(result, Is.True);
            Assert.That(lastRequest.IsConfirmed, Is.True);
            Assert.That(service.Get(resolution), Is.True);
            Assert.That(Saved("display.resolution"), Is.EqualTo("true"));
            Assert.That(service.ActiveConfirmation, Is.Null);
        }

        [Test]
        public void Cancel_RevertsAndReappliesPreviousValue()
        {
            PlayModeToggleSetting resolution = Confirmable("display.resolution");
            Initialize(15f, resolution);
            service.Set(resolution, true);
            bool? result = null;
            lastRequest.Completed += confirmed => result = confirmed;

            lastRequest.Cancel();

            Assert.That(result, Is.False);
            Assert.That(service.Get(resolution), Is.False);
            Assert.That(resolution.LastApplied, Is.False);
            Assert.That(resolution.ApplyCount, Is.EqualTo(3));
            Assert.That(Saved("display.resolution"), Is.EqualTo("false"));
        }

        [Test]
        public void NewConfirmation_RevertsTheOneInProgress()
        {
            PlayModeToggleSetting resolution = Confirmable("display.resolution");
            PlayModeToggleSetting mode = Confirmable("display.mode");
            Initialize(15f, resolution, mode);

            service.Set(resolution, true);
            ConfirmationRequest first = lastRequest;
            service.Set(mode, true);

            Assert.That(first.IsCompleted, Is.True);
            Assert.That(first.IsConfirmed, Is.False);
            Assert.That(service.Get(resolution), Is.False);
            Assert.That(service.ActiveConfirmation, Is.SameAs(lastRequest));
            Assert.That(lastRequest, Is.Not.SameAs(first));
        }

        [Test]
        public void Dispose_CompletesActiveConfirmationWithoutConfirming()
        {
            PlayModeToggleSetting resolution = Confirmable("display.resolution");
            Initialize(15f, resolution);
            service.Set(resolution, true);
            ConfirmationRequest request = lastRequest;

            service.Dispose();
            service = null;

            Assert.That(request.IsCompleted, Is.True);
            Assert.That(request.IsConfirmed, Is.False);
        }

        // ---------- timer ----------

        [UnityTest]
        public IEnumerator Timeout_RevertsAutomatically()
        {
            PlayModeToggleSetting resolution = Confirmable("display.resolution");
            Initialize(0.2f, resolution);
            service.Set(resolution, true);

            yield return WaitUntilCompleted(lastRequest, maxSeconds: 3f);

            Assert.That(lastRequest.IsCompleted, Is.True);
            Assert.That(lastRequest.IsConfirmed, Is.False);
            Assert.That(service.Get(resolution), Is.False);
        }

        [UnityTest]
        public IEnumerator Timeout_UsesRealTimeEvenWithTimeScaleZero()
        {
            PlayModeToggleSetting resolution = Confirmable("display.resolution");
            Initialize(0.2f, resolution);
            Time.timeScale = 0f;
            service.Set(resolution, true);

            yield return WaitUntilCompleted(lastRequest, maxSeconds: 3f);

            Assert.That(lastRequest.IsCompleted, Is.True);
            Assert.That(service.Get(resolution), Is.False);
        }

        [UnityTest]
        public IEnumerator Confirm_StopsTheTimer()
        {
            PlayModeToggleSetting resolution = Confirmable("display.resolution");
            Initialize(0.2f, resolution);
            service.Set(resolution, true);

            lastRequest.Confirm();
            yield return WaitRealSeconds(0.5f);

            Assert.That(service.Get(resolution), Is.True);
        }
    }
}