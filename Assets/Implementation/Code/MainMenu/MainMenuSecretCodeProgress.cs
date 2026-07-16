using System;

public sealed class MainMenuSecretCodeProgress
{
    private readonly string secretCode;

    public MainMenuSecretCodeProgress(string secretCode)
    {
        if (string.IsNullOrWhiteSpace(secretCode))
        {
            throw new ArgumentException("Secret code must not be empty.", nameof(secretCode));
        }

        this.secretCode = secretCode.ToUpperInvariant();
    }

    public int Progress { get; private set; }
    public int Length => secretCode.Length;

    public bool Advance(char input)
    {
        char normalizedInput = char.ToUpperInvariant(input);
        if (Progress >= 0
            && Progress < secretCode.Length
            && normalizedInput == secretCode[Progress])
        {
            Progress++;
        }
        else if (normalizedInput == secretCode[0])
        {
            Progress = 1;
        }
        else
        {
            Progress = 0;
        }

        if (Progress < secretCode.Length)
        {
            return false;
        }

        Progress = 0;
        return true;
    }

    public void Reset()
    {
        Progress = 0;
    }

    public static bool TryNormalizeInput(char input, out char normalizedInput)
    {
        normalizedInput = char.ToUpperInvariant(input);
        return char.IsLetterOrDigit(normalizedInput);
    }
}
