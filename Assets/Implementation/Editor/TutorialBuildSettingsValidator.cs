using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class TutorialBuildSettingsValidator : IPreprocessBuildWithReport
{
    private const string TutorialScenePath = "Assets/Scenes/Game/ZonaTutorial.unity";
    private const string MenuPath = "Tools/Squid Ink Pulse/Validate Tutorial Isolation";

    public int callbackOrder => -100;

    public void OnPreprocessBuild(BuildReport report)
    {
        ValidateEditorBuildSettings();
    }

    [MenuItem(MenuPath)]
    public static void ValidateEditorBuildSettings()
    {
        string offendingScene = FindEnabledTutorialScene(EditorBuildSettings.scenes);
        if (string.IsNullOrEmpty(offendingScene))
        {
            Debug.Log("[TutorialBuildSettingsValidator] Tutorial jugable aislado: ZonaTutorial no esta habilitada en Build Settings.");
            return;
        }

        throw new BuildFailedException(
            $"El tutorial jugable pendiente no debe entrar al build activo: '{offendingScene}'. " +
            "El tutorial disponible para jugadores es el comic de Como Jugar en MainMenu.");
    }

    private static string FindEnabledTutorialScene(EditorBuildSettingsScene[] scenes)
    {
        if (scenes == null)
        {
            return null;
        }

        EditorBuildSettingsScene offendingScene = scenes.FirstOrDefault(IsEnabledTutorialScene);
        return offendingScene?.path;
    }

    private static bool IsEnabledTutorialScene(EditorBuildSettingsScene scene)
    {
        return scene != null
            && scene.enabled
            && string.Equals(scene.path, TutorialScenePath, StringComparison.OrdinalIgnoreCase);
    }
}
