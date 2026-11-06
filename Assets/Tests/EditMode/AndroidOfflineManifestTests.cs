using System;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using UnityEditor;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class AndroidOfflineManifestTests
    {
        private const string Manifest = "<manifest xmlns:android='http://schemas.android.com/apk/res/android' package='test.game'>"
            + "<uses-permission android:name='android.permission.INTERNET'/>"
            + "<uses-permission android:name='test.game.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION'/>"
            + "<permission android:name='test.game.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION' android:protectionLevel='signature'/>"
            + "<application android:icon='@mipmap/icon'><activity android:name='GameActivity'/></application></manifest>";

        [TestCase(BuildTarget.Android, BuildOptions.None, true)]
        [TestCase(BuildTarget.Android, BuildOptions.CompressWithLz4HC, true)]
        [TestCase(BuildTarget.Android, BuildOptions.Development, false)]
        [TestCase(BuildTarget.Android, BuildOptions.Development | BuildOptions.AllowDebugging, false)]
        [TestCase(BuildTarget.StandaloneWindows64, BuildOptions.None, false)]
        public void ManifestPolicy_OnlyAppliesToNonDevelopmentAndroid(BuildTarget target, BuildOptions options, bool expected)
        {
            Assert.That(AndroidOfflineManifest.ShouldRemoveInternet(target, options), Is.EqualTo(expected));
        }

        [Test]
        public void OfflineManifest_RemovesInheritedInternetAndPreservesOtherDeclarations()
        {
            XDocument document = XDocument.Parse(Manifest);
            XElement application = new XElement(document.Root.Element("application"));
            XElement signature = new XElement(document.Root.Element("permission"));
            Assert.That(AndroidOfflineManifest.Apply(document, false), Is.True);
            XNamespace android = "http://schemas.android.com/apk/res/android";
            XNamespace tools = "http://schemas.android.com/tools";
            XElement internet = document.Root.Elements("uses-permission")
                .Single(item => (string)item.Attribute(android + "name") == "android.permission.INTERNET");
            Assert.That((string)internet.Attribute(tools + "node"), Is.EqualTo("remove"));
            Assert.That(document.Root.Elements("uses-permission").Count(), Is.EqualTo(2));
            Assert.That(XNode.DeepEquals(application, document.Root.Element("application")), Is.True);
            Assert.That(XNode.DeepEquals(signature, document.Root.Element("permission")), Is.True);
            Assert.That(AndroidOfflineManifest.Apply(document, false), Is.False, "Repeated callbacks must not duplicate the merge directive.");
        }

        [Test]
        public void DevelopmentManifest_KeepsNetworkAndDebugDeclarationsUnchanged()
        {
            XDocument document = XDocument.Parse(Manifest);
            string before = document.ToString();
            Assert.That(AndroidOfflineManifest.Apply(document, true), Is.False);
            Assert.That(document.ToString(), Is.EqualTo(before));
        }

        [Test]
        public void MissingInternet_StillBlocksThePermissionFromDependencies()
        {
            XDocument document = XDocument.Parse("<manifest><application/></manifest>");
            Assert.That(AndroidOfflineManifest.Apply(document, false), Is.True);
            XNamespace tools = "http://schemas.android.com/tools";
            Assert.That((string)document.Root.Element("uses-permission").Attribute(tools + "node"), Is.EqualTo("remove"));
        }

        [Test]
        public void InvalidManifest_FailsInsteadOfSilentlySkippingTheContract()
        {
            Assert.Throws<ArgumentException>(() => AndroidOfflineManifest.Apply(XDocument.Parse("<application/>"), false));
        }
    }
}
