using System;

public sealed class InkPulseInputBinding : IDisposable
{
    private SquidInkPulseGameplayInputReader inputReader;
    private bool activationRequested;

    public InkPulseInputBinding(SquidInkPulseGameplayInputReader inputReader)
    {
        this.inputReader = inputReader != null
            ? inputReader
            : throw new ArgumentNullException(nameof(inputReader));

        inputReader.InkPulseRequested += HandleInkPulseRequested;
    }

    public bool TryConsumeActivationRequest()
    {
        if (!activationRequested)
        {
            return false;
        }

        activationRequested = false;
        return true;
    }

    public void Dispose()
    {
        if (inputReader == null)
        {
            return;
        }

        inputReader.InkPulseRequested -= HandleInkPulseRequested;
        inputReader = null;
        activationRequested = false;
    }

    private void HandleInkPulseRequested()
    {
        activationRequested = true;
    }
}
