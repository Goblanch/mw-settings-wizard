using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class ApplyBatchTests
    {
        TestAssets assets;
        SettingsService service;
        RecordingBatch lastBatch;

        [SetUp]
        public void SetUp()
        {
            assets = new TestAssets();
            lastBatch = null;
        }

        [TearDown]
        public void TearDown()
        {
            service?.Dispose();
            service = null;
            assets.DestroyAll();
        }

        FakeToggleSetting BatchedToggle(string id, ApplyMode mode = ApplyMode.Immediate, bool defaultValue = false)
        {
            var setting = assets.Setting<FakeToggleSetting>(id, applyMode: mode);
            assets.Configure(setting, so => so.FindProperty("defaultValue").boolValue = defaultValue);
            setting.OnApply = (value, context) =>
            {
                lastBatch = context.GetBatch<RecordingBatch>();
                lastBatch.Items.Add(id);
            };
            return setting;
        }

        void Initialize(params SettingDefinition[] settings)
        {
            service = new SettingsService(assets.Profile(settings), new InMemorySettingsStorage());
            service.Initialize();
        }

        [Test]
        public void Initialize_SettingsShareOneBatchCommittedOnce()
        {
            Initialize(BatchedToggle("a"), BatchedToggle("b"));

            Assert.That(lastBatch.Items, Is.EqualTo(new[] { "a", "b" }));
            Assert.That(lastBatch.CommitCount, Is.EqualTo(1));
        }

        [Test]
        public void Set_Immediate_CommitsItsOwnBatch()
        {
            FakeToggleSetting a = BatchedToggle("a");
            Initialize(a);
            RecordingBatch initializeBatch = lastBatch;

            service.Set(a, true);

            Assert.That(lastBatch, Is.Not.SameAs(initializeBatch));
            Assert.That(lastBatch.Items, Is.EqualTo(new[] { "a" }));
            Assert.That(lastBatch.CommitCount, Is.EqualTo(1));
        }

        [Test]
        public void ApplyPending_CommitsOnceForAllPendingSettings()
        {
            FakeToggleSetting a = BatchedToggle("a", ApplyMode.Deferred);
            FakeToggleSetting b = BatchedToggle("b", ApplyMode.Deferred);
            Initialize(a, b);
            service.Set(a, true);
            service.Set(b, true);
            lastBatch = null;

            service.ApplyPending();

            Assert.That(lastBatch.Items, Is.EqualTo(new[] { "a", "b" }));
            Assert.That(lastBatch.CommitCount, Is.EqualTo(1));
        }

        [Test]
        public void ResetToDefaults_CommitsOnce()
        {
            FakeToggleSetting a = BatchedToggle("a", defaultValue: true);
            FakeToggleSetting b = BatchedToggle("b", defaultValue: true);
            Initialize(a, b);
            service.Set(a, false);
            service.Set(b, false);
            lastBatch = null;

            service.ResetToDefaults();

            Assert.That(lastBatch.Items, Is.EqualTo(new[] { "a", "b" }));
            Assert.That(lastBatch.CommitCount, Is.EqualTo(1));
        }

        [Test]
        public void FailingBatch_IsLoggedAndOtherBatchesStillCommit()
        {
            SecondRecordingBatch second = null;
            var a = assets.Setting<FakeToggleSetting>("a");
            a.OnApply = (value, context) => context.GetBatch<RecordingBatch>().ThrowOnCommit = true;
            var b = assets.Setting<FakeToggleSetting>("b");
            b.OnApply = (value, context) => second = context.GetBatch<SecondRecordingBatch>();
            LogAssert.Expect(LogType.Error, new Regex("Committing RecordingBatch failed"));

            Initialize(a, b);

            Assert.That(second.CommitCount, Is.EqualTo(1));
        }

        [Test]
        public void ValueApplied_IsRaisedAfterTheBatchIsCommitted()
        {
            FakeToggleSetting a = BatchedToggle("a");
            Initialize(a);
            int commitsSeenByHandler = -1;
            service.ValueApplied += _ => commitsSeenByHandler = lastBatch.CommitCount;

            service.Set(a, true);

            Assert.That(commitsSeenByHandler, Is.EqualTo(1));
        }

        [Test]
        public void GetBatch_AfterCommit_Throws()
        {
            SettingsApplyContext kept = null;
            var a = assets.Setting<FakeToggleSetting>("a");
            a.OnApply = (value, context) => kept = context;

            Initialize(a);

            Assert.Throws<InvalidOperationException>(() => kept.GetBatch<RecordingBatch>());
        }
    }
}