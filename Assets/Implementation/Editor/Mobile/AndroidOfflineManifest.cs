using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class AndroidOfflineManifest : IPreprocessBuildWithReport, IPostGenerateGradleAndroidProject
{
    private static bool? removeInternetPermission;
    public int callbackOrder => 1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        removeInternetPermission = ShouldRemoveInternet(report.summary.platform, report.summary.options);
    }

    public static bool ShouldRemoveInternet(BuildTarget target, BuildOptions options) =>
        target == BuildTarget.Android && (options & BuildOptions.Development) == 0;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        if (!removeInternetPermission.HasValue)
            throw new BuildFailedException("Falta el contexto de build para aplicar el contrato Android sin red.");
        if (!removeInternetPermission.Value) return;

        string gradleRoot = new DirectoryInfo(path).Parent.FullName;
        string manifestPath = Path.Combine(gradleRoot, "launcher", "src", "main", "AndroidManifest.xml");
        if (!File.Exists(manifestPath))
            throw new BuildFailedException("No se encontro el manifiesto launcher para el contrato Android sin red.");
        XDocument document = XDocument.Load(manifestPath);
        if (Apply(document, development: false)) document.Save(manifestPath);
        Debug.Log("[AndroidOfflineManifest] Non-Development launcher removes INTERNET during manifest merge.");
    }

    public static bool Apply(XDocument document, bool development)
    {
        if (development) return false;
        XElement root = document?.Root;
        if (root == null || root.Name != "manifest")
            throw new ArgumentException("Se requiere un AndroidManifest con raiz manifest.", nameof(document));
        XNamespace android = "http://schemas.android.com/apk/res/android";
        XNamespace tools = "http://schemas.android.com/tools";
        XElement[] entries = root.Elements("uses-permission")
            .Where(item => (string)item.Attribute(android + "name") == "android.permission.INTERNET").ToArray();
        if (entries.Length == 1 && (string)entries[0].Attribute(tools + "node") == "remove") return false;
        root.SetAttributeValue(XNamespace.Xmlns + "tools", tools.NamespaceName);
        foreach (XElement entry in entries) entry.Remove();
        root.Add(new XElement("uses-permission",
            new XAttribute(android + "name", "android.permission.INTERNET"),
            new XAttribute(tools + "node", "remove")));
        return true;
    }
}
