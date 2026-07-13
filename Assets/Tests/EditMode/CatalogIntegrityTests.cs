using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SquidInkPulse.Tests.EditMode
{
    public sealed class CatalogIntegrityTests
    {
        private static readonly string[] RequiredPermanentUpgradeIds =
        {
            PlayerUnlockableIds.InkPulseDurationUpgrade,
            PlayerUnlockableIds.InkPulseRechargeRateUpgrade,
            PlayerUnlockableIds.ShrimpMultiplierUpgrade,
            PlayerUnlockableIds.ScoreMultiplierUpgrade
        };

        [Test]
        public void RuntimeCatalog_HasUniqueIdsPerCategory()
        {
            UnlockablesCatalogSaveData catalog = LoadRuntimeCatalog();

            AssertUniqueIds(catalog.skins.Select(skin => skin.id), "skins");
            AssertUniqueIds(catalog.runGadgets.Select(gadget => gadget.id), "runGadgets");
            AssertUniqueIds(catalog.permanentUpgrades.Select(upgrade => upgrade.id), "permanentUpgrades");
        }

        [Test]
        public void RuntimeCatalog_DefaultProfileReferencesExist()
        {
            UnlockablesCatalogSaveData catalog = LoadRuntimeCatalog();
            PlayerProfileSaveData defaultProfile = PlayerProfileSaveData.CreateDefault();

            Assert.That(catalog.skins.Select(skin => skin.id), Does.Contain(defaultProfile.skins.equippedSkinId));
            foreach (string skinId in defaultProfile.skins.unlockedSkinIds)
            {
                Assert.That(catalog.skins.Any(skin => skin.id == skinId), Is.True, $"Skin default no existe en catalogo: {skinId}");
            }

            foreach (string gadgetId in defaultProfile.runGadgetUnlocks.unlockedRunGadgetIds)
            {
                Assert.That(catalog.runGadgets.Any(gadget => gadget.id == gadgetId), Is.True, $"Gadget default no existe en catalogo: {gadgetId}");
            }
        }

        [Test]
        public void RuntimeCatalog_PlayerSkinPrefabResourcesResolveToCompleteVisualSets()
        {
            UnlockablesCatalogSaveData catalog = LoadRuntimeCatalog();
            foreach (UnlockableSkinDefinition skin in catalog.skins)
            {
                if (string.IsNullOrWhiteSpace(skin.playerSkinPrefabResourcePath))
                {
                    continue;
                }

                GameObject prefab = Resources.Load<GameObject>(skin.playerSkinPrefabResourcePath);
                Assert.That(prefab, Is.Not.Null, $"Skin '{skin.id}' no resuelve Resources/{skin.playerSkinPrefabResourcePath}.");

                GameObject instance = Object.Instantiate(prefab);
                try
                {
                    PlayerSkinVisualSet visualSet = instance.GetComponentInChildren<PlayerSkinVisualSet>(includeInactive: true);
                    Assert.That(visualSet, Is.Not.Null, $"Skin '{skin.id}' no contiene PlayerSkinVisualSet.");

                    visualSet.ResolveReferences();
                    Assert.That(visualSet.IsConfigured, Is.True, $"Skin '{skin.id}' no tiene raices visuales completas.");
                    Assert.That(visualSet.MovementAnimator, Is.Not.Null, $"Skin '{skin.id}' no tiene animador de movimiento.");
                    Assert.That(visualSet.InkPulseAnimator, Is.Not.Null, $"Skin '{skin.id}' no tiene animador de Ink-Pulse.");
                    Assert.That(visualSet.PortalAnimator, Is.Not.Null, $"Skin '{skin.id}' no tiene animador de portal.");
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }
        }

        [Test]
        public void RuntimeCatalog_PermanentUpgradesHaveValidContracts()
        {
            UnlockablesCatalogSaveData catalog = LoadRuntimeCatalog();

            foreach (string requiredId in RequiredPermanentUpgradeIds)
            {
                Assert.That(catalog.permanentUpgrades.Any(upgrade => upgrade.id == requiredId), Is.True, $"Falta mejora requerida: {requiredId}");
            }

            foreach (PermanentUpgradeDefinition upgrade in catalog.permanentUpgrades)
            {
                Assert.That(upgrade.maxLevel, Is.GreaterThan(0), $"maxLevel invalido en {upgrade.id}");
                Assert.That(upgrade.priceGrowthMultiplier, Is.GreaterThanOrEqualTo(1f), $"priceGrowthMultiplier invalido en {upgrade.id}");
                Assert.That(PermanentUpgradeEffectModes.IsKnown(upgrade.effectMode), Is.True, $"effectMode invalido en {upgrade.id}");
                Assert.That(upgrade.effectPerLevel, Is.GreaterThanOrEqualTo(0f), $"effectPerLevel invalido en {upgrade.id}");
            }
        }

        [Test]
        public void RuntimeCatalog_RunGadgetsMapToSupportedGameplayIds()
        {
            UnlockablesCatalogSaveData catalog = LoadRuntimeCatalog();

            foreach (RunGadgetUnlockDefinition gadget in catalog.runGadgets)
            {
                Assert.That(GadgetCatalog.TryGetGadgetId(gadget.id, out GadgetId mappedGadget), Is.True, $"Gadget sin mapeo soportado: {gadget.id}");
                Assert.That(Enum.TryParse(gadget.gameplayId, ignoreCase: false, out GadgetId gameplayGadget), Is.True, $"gameplayId invalido: {gadget.gameplayId}");
                Assert.That(gameplayGadget, Is.EqualTo(mappedGadget), $"gameplayId no coincide con id de desbloqueo para {gadget.id}");
            }
        }

        private static UnlockablesCatalogSaveData LoadRuntimeCatalog()
        {
            string catalogPath = Path.Combine(Application.dataPath, "StreamingAssets", "db", "unlockables-catalog.json");
            Assert.That(File.Exists(catalogPath), Is.True, $"No existe catalogo runtime: {catalogPath}");

            UnlockablesCatalogSaveData catalog = JsonUtility.FromJson<UnlockablesCatalogSaveData>(File.ReadAllText(catalogPath));
            Assert.That(catalog, Is.Not.Null);
            catalog.Normalize();
            return catalog;
        }

        private static void AssertUniqueIds(IEnumerable<string> ids, string category)
        {
            string[] duplicateIds = ids
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .GroupBy(id => id)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();

            Assert.That(duplicateIds, Is.Empty, $"IDs duplicados en {category}: {string.Join(", ", duplicateIds)}");
        }
    }
}
