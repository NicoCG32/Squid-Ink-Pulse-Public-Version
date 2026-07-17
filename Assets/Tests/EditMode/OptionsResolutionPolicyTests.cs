using NUnit.Framework;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class OptionsResolutionPolicyTests
    {
        [Test]
        public void BuildUniqueResolutionList_RemovesDuplicatesAndSortsByWidthThenHeight()
        {
            DisplayResolutionOption[] source =
            {
                new(1920, 1080),
                new(1280, 720),
                new(1920, 1080),
                new(1024, 768)
            };

            DisplayResolutionOption[] result = OptionsResolutionPolicy.BuildUniqueResolutionList(
                source,
                new DisplayResolutionOption(800, 600));

            Assert.That(result, Has.Length.EqualTo(3));
            Assert.That(result[0].Width, Is.EqualTo(1024));
            Assert.That(result[0].Height, Is.EqualTo(768));
            Assert.That(result[1].Width, Is.EqualTo(1280));
            Assert.That(result[1].Height, Is.EqualTo(720));
            Assert.That(result[2].Width, Is.EqualTo(1920));
            Assert.That(result[2].Height, Is.EqualTo(1080));
        }

        [Test]
        public void BuildUniqueResolutionList_UsesClampedFallback_WhenSourceIsEmpty()
        {
            DisplayResolutionOption[] result = OptionsResolutionPolicy.BuildUniqueResolutionList(
                new DisplayResolutionOption[0],
                new DisplayResolutionOption(0, -10));

            Assert.That(result, Has.Length.EqualTo(1));
            Assert.That(result[0].Width, Is.EqualTo(1));
            Assert.That(result[0].Height, Is.EqualTo(1));
        }

        [Test]
        public void ResolvePreferredIndex_UsesLegacyIndex_WhenNoSavedSizeExists()
        {
            DisplayResolutionOption[] resolutions =
            {
                new(1024, 768),
                new(1280, 720),
                new(1920, 1080)
            };

            int index = OptionsResolutionPolicy.ResolvePreferredIndex(
                resolutions,
                hasSavedSize: false,
                savedWidth: 0,
                savedHeight: 0,
                hasLegacyIndex: true,
                legacyIndex: 2,
                fallback: new DisplayResolutionOption(1280, 720));

            Assert.That(index, Is.EqualTo(2));
        }

        [Test]
        public void ResolvePreferredIndex_UsesClosestSavedSize_WhenSavedSizeExists()
        {
            DisplayResolutionOption[] resolutions =
            {
                new(1024, 768),
                new(1280, 720),
                new(1920, 1080)
            };

            int index = OptionsResolutionPolicy.ResolvePreferredIndex(
                resolutions,
                hasSavedSize: true,
                savedWidth: 1300,
                savedHeight: 730,
                hasLegacyIndex: true,
                legacyIndex: 2,
                fallback: new DisplayResolutionOption(1920, 1080));

            Assert.That(index, Is.EqualTo(1));
        }

        [TestCase(-1, false)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        [TestCase(2, false)]
        public void IsValidResolutionIndex_ChecksBounds(int index, bool expected)
        {
            DisplayResolutionOption[] resolutions =
            {
                new(1024, 768),
                new(1280, 720)
            };

            Assert.That(OptionsResolutionPolicy.IsValidResolutionIndex(resolutions, index), Is.EqualTo(expected));
        }
    }
}
