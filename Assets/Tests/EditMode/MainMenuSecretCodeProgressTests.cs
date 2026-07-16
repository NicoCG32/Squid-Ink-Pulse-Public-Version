using NUnit.Framework;

public sealed class MainMenuSecretCodeProgressTests
{
    [Test]
    public void Advance_ReturnsTrue_WhenCompleteCodeIsEntered()
    {
        MainMenuSecretCodeProgress progress = CreateProgress();

        bool completed = Advance(progress, "SONICYNOTA7");

        Assert.IsTrue(completed);
        Assert.AreEqual(0, progress.Progress);
    }

    [Test]
    public void Advance_IsCaseInsensitive()
    {
        MainMenuSecretCodeProgress progress = CreateProgress();

        bool completed = Advance(progress, "sonicynota7");

        Assert.IsTrue(completed);
    }

    [Test]
    public void Advance_RestartsFromFirstCharacter_WhenMismatchIsNewCodeStart()
    {
        MainMenuSecretCodeProgress progress = CreateProgress();

        Advance(progress, "SONIS");

        Assert.AreEqual(1, progress.Progress);
    }

    [Test]
    public void Advance_Resets_WhenMismatchIsNotCodeStart()
    {
        MainMenuSecretCodeProgress progress = CreateProgress();

        Advance(progress, "SONIX");

        Assert.AreEqual(0, progress.Progress);
    }

    [TestCase('a', true, 'A')]
    [TestCase('7', true, '7')]
    [TestCase('-', false, '-')]
    public void TryNormalizeInput_OnlyAcceptsLettersAndDigits(char input, bool expectedAccepted, char expectedNormalized)
    {
        bool accepted = MainMenuSecretCodeProgress.TryNormalizeInput(input, out char normalizedInput);

        Assert.AreEqual(expectedAccepted, accepted);
        Assert.AreEqual(expectedNormalized, normalizedInput);
    }

    private static MainMenuSecretCodeProgress CreateProgress()
    {
        return new MainMenuSecretCodeProgress("SONICYNOTA7");
    }

    private static bool Advance(MainMenuSecretCodeProgress progress, string text)
    {
        bool completed = false;
        for (int i = 0; i < text.Length; i++)
        {
            completed = progress.Advance(text[i]);
        }

        return completed;
    }
}
