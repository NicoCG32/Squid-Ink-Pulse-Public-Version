using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class LoreComicViewTests
{
    [Test]
    public void Show_MakesRootVisibleAndInteractive()
    {
        Fixture fixture = new();
        try
        {
            fixture.Root.SetActive(false);

            fixture.View.Show(sprite: null, showContinue: true);

            Assert.IsTrue(fixture.Root.activeSelf);
            Assert.AreEqual(1f, fixture.CanvasGroup.alpha);
            Assert.IsTrue(fixture.CanvasGroup.interactable);
            Assert.IsTrue(fixture.CanvasGroup.blocksRaycasts);
            Assert.IsTrue(fixture.ContinueRoot.activeSelf);
            Assert.IsTrue(fixture.ContinueButton.interactable);
        }
        finally
        {
            fixture.Destroy();
        }
    }

    [Test]
    public void HideImmediate_HidesRootAndDisablesInteraction()
    {
        Fixture fixture = new();
        try
        {
            fixture.View.Show(sprite: null, showContinue: true);

            fixture.View.HideImmediate();

            Assert.IsFalse(fixture.Root.activeSelf);
            Assert.AreEqual(0f, fixture.CanvasGroup.alpha);
            Assert.IsFalse(fixture.CanvasGroup.interactable);
            Assert.IsFalse(fixture.CanvasGroup.blocksRaycasts);
            Assert.IsFalse(fixture.ContinueRoot.activeSelf);
            Assert.IsFalse(fixture.ContinueButton.interactable);
        }
        finally
        {
            fixture.Destroy();
        }
    }

    [Test]
    public void NormalizeRenderableScale_RestoresCollapsedScalesAndSorting()
    {
        Fixture fixture = new();
        try
        {
            fixture.Owner.transform.localScale = Vector3.zero;
            fixture.Root.transform.localScale = Vector3.zero;
            fixture.Canvas.transform.localScale = Vector3.zero;

            fixture.View.NormalizeRenderableScale();

            Assert.AreEqual(Vector3.one, fixture.Owner.transform.localScale);
            Assert.AreEqual(Vector3.one, fixture.Root.transform.localScale);
            Assert.AreEqual(Vector3.one, fixture.Canvas.transform.localScale);
            Assert.AreEqual(5000, fixture.Canvas.sortingOrder);
        }
        finally
        {
            fixture.Destroy();
        }
    }

    private sealed class Fixture
    {
        public readonly GameObject Owner;
        public readonly GameObject Root;
        public readonly Canvas Canvas;
        public readonly CanvasGroup CanvasGroup;
        public readonly Button ContinueButton;
        public readonly GameObject ContinueRoot;
        public readonly LoreComicView View;

        public Fixture()
        {
            Owner = new GameObject("Owner");
            Canvas = Owner.AddComponent<Canvas>();
            Root = new GameObject("ComicRoot");
            Root.transform.SetParent(Owner.transform);
            CanvasGroup = Root.AddComponent<CanvasGroup>();
            Root.AddComponent<Image>();

            GameObject imageObject = new("Vineta");
            imageObject.transform.SetParent(Root.transform);
            Image comicImage = imageObject.AddComponent<Image>();

            ContinueRoot = new GameObject("ContinuarBoton");
            ContinueRoot.transform.SetParent(Root.transform);
            ContinueButton = ContinueRoot.AddComponent<Button>();

            View = new LoreComicView(
                Root,
                CanvasGroup,
                comicImage,
                ContinueButton,
                ContinueRoot,
                Owner.transform,
                minimumSortingOrder: 5000);
        }

        public void Destroy()
        {
            Object.DestroyImmediate(Owner);
        }
    }
}
