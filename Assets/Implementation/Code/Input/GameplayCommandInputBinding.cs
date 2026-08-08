using System;

public enum SquidInkPulseGameplayCommand
{
    TogglePause,
    UseGadgetSlot1,
    UseGadgetSlot2,
    BuyShopOffer
}

public sealed class GameplayCommandInputBinding : IDisposable
{
    private SquidInkPulseGameplayInputReader inputReader;
    private readonly SquidInkPulseGameplayCommand command;
    private bool requested;

    public GameplayCommandInputBinding(
        SquidInkPulseGameplayInputReader inputReader,
        SquidInkPulseGameplayCommand command)
    {
        this.inputReader = inputReader != null
            ? inputReader
            : throw new ArgumentNullException(nameof(inputReader));
        this.command = command;
        Subscribe();
    }

    public bool TryConsumeRequest()
    {
        if (!requested)
        {
            return false;
        }

        requested = false;
        return true;
    }

    public void Dispose()
    {
        if (inputReader == null)
        {
            return;
        }

        Unsubscribe();
        inputReader = null;
        requested = false;
    }

    private void Subscribe()
    {
        switch (command)
        {
            case SquidInkPulseGameplayCommand.TogglePause:
                inputReader.PauseToggleRequested += HandleRequested;
                break;
            case SquidInkPulseGameplayCommand.UseGadgetSlot1:
                inputReader.GadgetSlot1Requested += HandleRequested;
                break;
            case SquidInkPulseGameplayCommand.UseGadgetSlot2:
                inputReader.GadgetSlot2Requested += HandleRequested;
                break;
            case SquidInkPulseGameplayCommand.BuyShopOffer:
                inputReader.ShopPurchaseRequested += HandleRequested;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), command, null);
        }
    }

    private void Unsubscribe()
    {
        switch (command)
        {
            case SquidInkPulseGameplayCommand.TogglePause:
                inputReader.PauseToggleRequested -= HandleRequested;
                break;
            case SquidInkPulseGameplayCommand.UseGadgetSlot1:
                inputReader.GadgetSlot1Requested -= HandleRequested;
                break;
            case SquidInkPulseGameplayCommand.UseGadgetSlot2:
                inputReader.GadgetSlot2Requested -= HandleRequested;
                break;
            case SquidInkPulseGameplayCommand.BuyShopOffer:
                inputReader.ShopPurchaseRequested -= HandleRequested;
                break;
        }
    }

    private void HandleRequested()
    {
        requested = true;
    }
}
