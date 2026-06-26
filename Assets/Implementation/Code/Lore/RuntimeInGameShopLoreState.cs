public static class RuntimeInGameShopLoreState
{
    private static bool firstDealerShopAccessPresented;
    private static bool firstDealerShopExitPresented;

    public static bool TryMarkFirstDealerShopAccess()
    {
        if (firstDealerShopAccessPresented)
        {
            return false;
        }

        firstDealerShopAccessPresented = true;
        return true;
    }

    public static bool TryMarkFirstDealerShopExit()
    {
        if (firstDealerShopExitPresented)
        {
            return false;
        }

        firstDealerShopExitPresented = true;
        return true;
    }

    public static void ResetForRuntime()
    {
        firstDealerShopAccessPresented = false;
        firstDealerShopExitPresented = false;
    }
}
