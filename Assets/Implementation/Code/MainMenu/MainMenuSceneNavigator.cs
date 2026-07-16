using System;

public static class MainMenuSceneNavigator
{
    public static string ResolveLoadableSceneName(string sceneName, Func<string, bool> canLoadScene)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || canLoadScene == null)
        {
            return null;
        }

        if (canLoadScene(sceneName))
        {
            return sceneName;
        }

        string sceneWithoutExtension = sceneName.EndsWith(".unity", StringComparison.Ordinal)
            ? sceneName.Substring(0, sceneName.Length - ".unity".Length)
            : sceneName;

        if (canLoadScene(sceneWithoutExtension))
        {
            return sceneWithoutExtension;
        }

        int lastSlashIndex = sceneWithoutExtension.LastIndexOf('/');
        if (lastSlashIndex < 0 || lastSlashIndex >= sceneWithoutExtension.Length - 1)
        {
            return null;
        }

        string shortSceneName = sceneWithoutExtension.Substring(lastSlashIndex + 1);
        return canLoadScene(shortSceneName) ? shortSceneName : null;
    }
}
