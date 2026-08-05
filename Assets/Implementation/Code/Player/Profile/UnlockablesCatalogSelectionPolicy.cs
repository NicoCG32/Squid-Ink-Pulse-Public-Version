public static class UnlockablesCatalogSelectionPolicy
{
    public static UnlockablesCatalogSaveData Select(
        UnlockablesCatalogSaveData runtimeCatalog,
        UnlockablesCatalogSaveData seedCatalog)
    {
        if (seedCatalog != null && (runtimeCatalog == null || seedCatalog.version > runtimeCatalog.version))
        {
            return seedCatalog;
        }

        return runtimeCatalog ?? seedCatalog;
    }
}
