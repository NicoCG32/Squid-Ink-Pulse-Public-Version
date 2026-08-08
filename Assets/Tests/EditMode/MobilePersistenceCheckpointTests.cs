using NUnit.Framework;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class MobilePersistenceCheckpointTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerPreferencesCheckpoint.FlushIfPending(() => { });
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPreferencesCheckpoint.FlushIfPending(() => { });
        }

        [Test]
        public void FlushIfPending_DoesNothingWithoutChanges()
        {
            int saveCount = 0;

            bool flushed = PlayerPreferencesCheckpoint.FlushIfPending(() => saveCount++);

            Assert.That(flushed, Is.False);
            Assert.That(saveCount, Is.Zero);
        }

        [Test]
        public void FlushIfPending_SavesPendingChangesOnlyOnce()
        {
            int saveCount = 0;
            PlayerPreferencesCheckpoint.MarkPending();

            bool firstFlush = PlayerPreferencesCheckpoint.FlushIfPending(() => saveCount++);
            bool secondFlush = PlayerPreferencesCheckpoint.FlushIfPending(() => saveCount++);

            Assert.That(firstFlush, Is.True);
            Assert.That(secondFlush, Is.False);
            Assert.That(saveCount, Is.EqualTo(1));
            Assert.That(PlayerPreferencesCheckpoint.HasPendingChanges, Is.False);
        }

        [Test]
        public void CheckpointForPause_FlushesOnlyWhenEnteringBackground()
        {
            PlayerPreferencesCheckpoint.MarkPending();

            bool whileActive = MobilePersistenceCheckpoint.CheckpointForPause(isPaused: false);
            bool whilePaused = MobilePersistenceCheckpoint.CheckpointForPause(isPaused: true);

            Assert.That(whileActive, Is.False);
            Assert.That(whilePaused, Is.True);
            Assert.That(PlayerPreferencesCheckpoint.HasPendingChanges, Is.False);
        }

        [Test]
        public void FocusLossAfterPause_DoesNotDuplicateFlush()
        {
            PlayerPreferencesCheckpoint.MarkPending();

            bool pauseFlush = MobilePersistenceCheckpoint.CheckpointForPause(isPaused: true);
            bool focusFlush = MobilePersistenceCheckpoint.CheckpointForFocus(hasFocus: false);

            Assert.That(pauseFlush, Is.True);
            Assert.That(focusFlush, Is.False);
        }

        [Test]
        public void FocusGain_DoesNotFlushPendingPreferences()
        {
            PlayerPreferencesCheckpoint.MarkPending();

            bool flushed = MobilePersistenceCheckpoint.CheckpointForFocus(hasFocus: true);

            Assert.That(flushed, Is.False);
            Assert.That(PlayerPreferencesCheckpoint.HasPendingChanges, Is.True);
        }
    }
}
