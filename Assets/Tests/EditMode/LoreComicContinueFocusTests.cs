using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class LoreComicContinueFocusTests
    {
        [TestCase(true,true)]
        [TestCase(true,false)]
        [TestCase(false,false)]
        public void Comic_OwnsSubmitAndRestoresOnlyItsOwnSelection(bool showContinue,bool externalSelection)
        {
            EventSystem previousSystem=EventSystem.current;
            var system=new GameObject("TestEventSystem",typeof(EventSystem));
            var root=new GameObject("Comic",typeof(RectTransform),typeof(CanvasGroup));
            var previous=new GameObject("Previous",typeof(RectTransform),typeof(Button));
            var other=new GameObject("Other",typeof(RectTransform),typeof(Button));
            try
            {
                // EditMode does not run EventSystem's play-mode registration.
                Invoke(system.GetComponent<EventSystem>(),"OnEnable");
                EventSystem.current=system.GetComponent<EventSystem>();
                var button=new GameObject("Continue",typeof(RectTransform),typeof(Button)).GetComponent<Button>();
                button.transform.SetParent(root.transform,false);
                var navigation=button.navigation; navigation.mode=Navigation.Mode.Automatic; button.navigation=navigation;
                EventSystem.current.SetSelectedGameObject(previous);
                var focus=root.AddComponent<LoreComicContinueFocus>();
                typeof(LoreComicContinueFocus).GetField("button",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(focus,button);
                focus.SetPresented(true,showContinue);
                Assert.That(EventSystem.current.currentSelectedGameObject,Is.SameAs(showContinue ? button.gameObject : null));
                Assert.That(button.navigation.mode,Is.EqualTo(Navigation.Mode.None));
                if (externalSelection) EventSystem.current.SetSelectedGameObject(other);
                focus.SetPresented(false,false);
                Assert.That(EventSystem.current.currentSelectedGameObject,Is.SameAs(externalSelection ? other : previous));
                Assert.That(button.navigation.mode,Is.EqualTo(Navigation.Mode.Automatic));
            }
            finally
            {
                Object.DestroyImmediate(root); Object.DestroyImmediate(previous); Object.DestroyImmediate(other);
                Invoke(system.GetComponent<EventSystem>(),"OnDisable");
                Object.DestroyImmediate(system);
                if (previousSystem != null) EventSystem.current=previousSystem;
            }
        }
        private static void Invoke(object target,string name) => target.GetType()
            .GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
    }
}
