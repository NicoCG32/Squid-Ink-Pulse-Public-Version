namespace SquidInkPulse.Tests.EditMode
{
    internal static class MobileGameRootTestCatalog
    {
        public const string TutorialGameRootPath =
            MobileGameRootCatalog.TutorialGameRootPath;

        public static readonly string[] RequiredProductGameRootPaths =
        {
            MobileGameRootCatalog.CanonicalGameRootPath,
            MobileGameRootCatalog.AbyssopelagicGameRootPath
        };

        public static string[] ProductGameRootPaths => MobileGameRootCatalog.ProductGameRootPaths;
    }
}
