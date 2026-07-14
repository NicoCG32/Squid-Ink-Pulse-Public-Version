using NUnit.Framework;
using UnityEngine;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class InGameShopPurchaseServiceTests
    {
        [Test]
        public void TryPurchase_Success_SpendsAndAcquires()
        {
            int spent = 0;
            GadgetId acquiredGadget = GadgetId.None;

            InGameShopPurchaseResult result = InGameShopPurchaseService.TryPurchase(
                GadgetId.InkBottle,
                icon: null,
                Color.white,
                price: 7,
                hasGadget: _ => false,
                trySpend: amount =>
                {
                    spent += amount;
                    return true;
                },
                refund: _ => Assert.Fail("No debe reembolsar una compra exitosa."),
                acquire: (gadget, _, _) =>
                {
                    acquiredGadget = gadget;
                    return true;
                });

            Assert.That(result, Is.EqualTo(InGameShopPurchaseResult.Success));
            Assert.That(spent, Is.EqualTo(7));
            Assert.That(acquiredGadget, Is.EqualTo(GadgetId.InkBottle));
        }

        [Test]
        public void TryPurchase_AlreadyOwned_DoesNotSpend()
        {
            bool attemptedSpend = false;

            InGameShopPurchaseResult result = InGameShopPurchaseService.TryPurchase(
                GadgetId.ShellShield,
                icon: null,
                Color.white,
                price: 8,
                hasGadget: _ => true,
                trySpend: _ =>
                {
                    attemptedSpend = true;
                    return true;
                },
                refund: _ => Assert.Fail("No debe reembolsar si nunca gasto."),
                acquire: (_, _, _) => true);

            Assert.That(result, Is.EqualTo(InGameShopPurchaseResult.AlreadyOwned));
            Assert.That(attemptedSpend, Is.False);
        }

        [Test]
        public void TryPurchase_InsufficientFunds_DoesNotAcquire()
        {
            bool attemptedAcquire = false;

            InGameShopPurchaseResult result = InGameShopPurchaseService.TryPurchase(
                GadgetId.InkBottle,
                icon: null,
                Color.white,
                price: 7,
                hasGadget: _ => false,
                trySpend: _ => false,
                refund: _ => Assert.Fail("No debe reembolsar si no gasto."),
                acquire: (_, _, _) =>
                {
                    attemptedAcquire = true;
                    return true;
                });

            Assert.That(result, Is.EqualTo(InGameShopPurchaseResult.InsufficientFunds));
            Assert.That(attemptedAcquire, Is.False);
        }

        [Test]
        public void TryPurchase_InventoryRejects_RefundsSpentAmount()
        {
            int refunded = 0;

            InGameShopPurchaseResult result = InGameShopPurchaseService.TryPurchase(
                GadgetId.ShellShield,
                icon: null,
                Color.white,
                price: 8,
                hasGadget: _ => false,
                trySpend: _ => true,
                refund: amount => refunded += amount,
                acquire: (_, _, _) => false);

            Assert.That(result, Is.EqualTo(InGameShopPurchaseResult.InventoryRejected));
            Assert.That(refunded, Is.EqualTo(8));
        }
    }
}
