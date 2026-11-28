using NUnit.Framework;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class AudioSettingsTests
    {
        TestAssets assets;
        SettingsService service;
        FakeAudioChannel channel;

        [SetUp]
        public void SetUp()
        {
            assets = new TestAssets();
            channel = assets.Asset<FakeAudioChannel>("Master");
        }

        [TearDown]
        public void TearDown()
        {
            service?.Dispose();
            service = null;
            assets.DestroyAll();
        }

        VolumeSetting Volume(AudioChannel target)
        {
            var volume = assets.Setting<VolumeSetting>("audio.master");
            assets.Configure(volume, so => so.FindProperty("channel").objectReferenceValue = target);
            return volume;
        }

        MuteSetting Mute(AudioChannel target)
        {
            var mute = assets.Setting<MuteSetting>("audio.mute");
            assets.Configure(mute, so => so.FindProperty("channel").objectReferenceValue = target);
            return mute;
        }

        void Initialize(params SettingDefinition[] settings)
        {
            service = new SettingsService(assets.Profile(settings), new InMemorySettingsStorage());
            service.Initialize();
        }

        [Test]
        public void Volume_DefaultsToFullVolumeInRangeZeroToOne()
        {
            VolumeSetting volume = Volume(channel);

            Assert.That(volume.Min, Is.EqualTo(0f));
            Assert.That(volume.Max, Is.EqualTo(1f));
            Assert.That(volume.GetDefault(new SettingsContext()), Is.EqualTo(1f));
        }

        [Test]
        public void Volume_IsAppliedToItsChannelOnInitialize()
        {
            Initialize(Volume(channel));

            Assert.That(channel.Applied, Is.EqualTo(new[] { 1f }));
        }

        [Test]
        public void Volume_Set_AppliesTheNewValue()
        {
            VolumeSetting volume = Volume(channel);
            Initialize(volume);

            service.Set(volume, 0.5f);

            Assert.That(channel.LastVolume, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void Mute_SilencesAndUnmuteBringsTheVolumeBack()
        {
            VolumeSetting volume = Volume(channel);
            MuteSetting mute = Mute(channel);
            Initialize(volume, mute);
            service.Set(volume, 0.4f);

            service.Set(mute, true);
            Assert.That(channel.LastVolume, Is.EqualTo(0f));

            service.Set(mute, false);
            Assert.That(channel.LastVolume, Is.EqualTo(0.4f).Within(0.0001f));
        }

        [Test]
        public void Volume_ChangedWhileMuted_StaysSilentUntilUnmuted()
        {
            VolumeSetting volume = Volume(channel);
            MuteSetting mute = Mute(channel);
            Initialize(volume, mute);
            service.Set(mute, true);

            service.Set(volume, 0.7f);
            Assert.That(channel.LastVolume, Is.EqualTo(0f));

            service.Set(mute, false);
            Assert.That(channel.LastVolume, Is.EqualTo(0.7f).Within(0.0001f));
        }

        [Test]
        public void Dispose_RestoresTheChannelOnce()
        {
            Initialize(Volume(channel), Mute(channel));

            service.Dispose();
            service = null;

            Assert.That(channel.RestoreCount, Is.EqualTo(1));
        }

        [Test]
        public void WithoutChannel_IsNotSupported()
        {
            VolumeSetting volume = Volume(null);
            MuteSetting mute = Mute(null);
            Initialize(volume, mute);

            Assert.That(service.IsSupported(volume), Is.False);
            Assert.That(service.IsSupported(mute), Is.False);
        }

        [Test]
        public void WithUnavailableChannel_IsNotSupportedAndNothingIsApplied()
        {
            channel.Available = false;
            VolumeSetting volume = Volume(channel);
            Initialize(volume);

            Assert.That(service.IsSupported(volume), Is.False);
            Assert.That(channel.Applied, Is.Empty);
        }
    }
}