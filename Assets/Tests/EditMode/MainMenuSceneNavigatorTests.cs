using System.Collections.Generic;
using NUnit.Framework;

public sealed class MainMenuSceneNavigatorTests
{
    [Test]
    public void ResolveLoadableSceneName_ReturnsConfiguredValue_WhenItCanLoad()
    {
        string resolved = MainMenuSceneNavigator.ResolveLoadableSceneName(
            "Assets/Scenes/ShopMenu/ShopMenu.unity",
            CanLoad("Assets/Scenes/ShopMenu/ShopMenu.unity"));

        Assert.AreEqual("Assets/Scenes/ShopMenu/ShopMenu.unity", resolved);
    }

    [Test]
    public void ResolveLoadableSceneName_TriesPathWithoutUnityExtension()
    {
        string resolved = MainMenuSceneNavigator.ResolveLoadableSceneName(
            "Assets/Scenes/Game/ZonaEpipelagica.unity",
            CanLoad("Assets/Scenes/Game/ZonaEpipelagica"));

        Assert.AreEqual("Assets/Scenes/Game/ZonaEpipelagica", resolved);
    }

    [Test]
    public void ResolveLoadableSceneName_TriesShortSceneNameLast()
    {
        string resolved = MainMenuSceneNavigator.ResolveLoadableSceneName(
            "Assets/Scenes/Game/ZonaEpipelagica.unity",
            CanLoad("ZonaEpipelagica"));

        Assert.AreEqual("ZonaEpipelagica", resolved);
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("MissingScene")]
    public void ResolveLoadableSceneName_ReturnsNull_WhenSceneCannotBeResolved(string sceneName)
    {
        string resolved = MainMenuSceneNavigator.ResolveLoadableSceneName(sceneName, _ => false);

        Assert.IsNull(resolved);
    }

    private static System.Func<string, bool> CanLoad(params string[] loadableScenes)
    {
        HashSet<string> scenes = new(loadableScenes);
        return scenes.Contains;
    }
}
