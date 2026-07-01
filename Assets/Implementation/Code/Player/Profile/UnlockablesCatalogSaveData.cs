using System;
using System.Linq;

[Serializable]
public class UnlockablesCatalogSaveData
{
    private const float LegacyRechargeRateReferenceChargePerSecond = 150f;

    public int version = PlayerProfileRepository.UnlockablesCatalogVersion;
    public UnlockableSkinDefinition[] skins = Array.Empty<UnlockableSkinDefinition>();
    public RunGadgetUnlockDefinition[] runGadgets = Array.Empty<RunGadgetUnlockDefinition>();
    public PermanentUpgradeDefinition[] permanentUpgrades = Array.Empty<PermanentUpgradeDefinition>();

    public static UnlockablesCatalogSaveData CreateDefault()
    {
        UnlockablesCatalogSaveData data = new()
        {
            version = PlayerProfileRepository.UnlockablesCatalogVersion,
            skins = new[]
            {
                Skin(PlayerSkinIds.Default, "Default", string.Empty, 0, string.Empty, defaultUnlocked: true, playerSkinPrefabResourcePath: "PlayerSkins/Default"),
                Skin("skin.bob_marley", "Rasta Marino", "Cuando el oceano se pone pesado, responde con ritmo, calma y tinta.", 420000, "ShopMenu/Skins/BobMarley", playerSkinPrefabResourcePath: "PlayerSkins/Marley"),
                Skin("skin.rockstar", "Rockstar", "Convierte cada dash peligroso en un solo epico bajo presion marina.", 500000, "ShopMenu/Skins/Rockstar", playerSkinPrefabResourcePath: "PlayerSkins/Rock"),
                Skin("skin.formal", "Formal", "Porque escapar del caos oceanico tambien puede hacerse con elegancia.", 75000, "ShopMenu/Skins/Formal", playerSkinPrefabResourcePath: "PlayerSkins/Formal"),
                Skin("skin.sonic", "Erizo Veloz", "Cuando el oceano acelera el caos, el acelera todavia mas.", 1200000, "ShopMenu/Skins/Sonic", playerSkinPrefabResourcePath: "PlayerSkins/Sonic"),
                Skin("skin.huaso", "Huaso Submarino", "Con coraje de sobra, zapatea entre corrientes como si fueran cueca brava.", 181000, "ShopMenu/Skins/Huaso", playerSkinPrefabResourcePath: "PlayerSkins/Huaso"),
                Skin("skin.chile", "Chile Marino", "Cuando el mar se pone dificil, responde con orgullo, tinta y aguante.", 180920, "ShopMenu/Skins/Chile", playerSkinPrefabResourcePath: "PlayerSkins/Chile"),
                Skin("skin.nemo", "Pez Aventurero", "Puede dar vueltas de mas, pero siempre encuentra una salida con estilo.", 700000, "ShopMenu/Skins/Nemo", playerSkinPrefabResourcePath: "PlayerSkins/Nemo"),
                Skin("skin.travis", "Travis", "Brilla entre el caos submarino como si cada obstaculo fuera parte del show.", 10000000, "ShopMenu/Skins/Travis", playerSkinPrefabResourcePath: "PlayerSkins/Travis")
            },
            runGadgets = new[]
            {
                new RunGadgetUnlockDefinition
                {
                    id = PlayerUnlockableIds.ShellShieldGadget,
                    gameplayId = "ShellShield",
                    displayName = "Shell Shield",
                    defaultUnlocked = true,
                    basePrice = 0,
                    unlockGoal = UnlockGoalDefinition.None()
                },
                new RunGadgetUnlockDefinition
                {
                    id = PlayerUnlockableIds.InkBottleGadget,
                    gameplayId = "InkBottle",
                    displayName = "Ink-Bottle",
                    defaultUnlocked = true,
                    basePrice = 0,
                    unlockGoal = UnlockGoalDefinition.None()
                }
            },
            permanentUpgrades = new[]
            {
                Upgrade(
                    PlayerUnlockableIds.InkPulseDurationUpgrade,
                    "Tinta Persistente",
                    "Tu nube aguanta mas: entra, limpia el peligro y sal con estilo.",
                    100,
                    1.5f,
                    PermanentUpgradeEffectModes.Multiplier,
                    1f,
                    0.075f,
                    "ShopMenu/Skills/Upgrades/InkPulsePersistence",
                    "ShopMenu/Skills/Upgrades/InkPulsePersistenceInk"),
                Upgrade(
                    PlayerUnlockableIds.InkPulseRechargeRateUpgrade,
                    "Pulso Recargado",
                    "Menos espera entre pulsos; mas escapes al limite.",
                    120,
                    1.5f,
                    PermanentUpgradeEffectModes.Additive,
                    0f,
                    11.25f,
                    "ShopMenu/Skills/Upgrades/ChargeRate",
                    "ShopMenu/Skills/Upgrades/ChargeRateInk"),
                Upgrade(
                    PlayerUnlockableIds.ShrimpMultiplierUpgrade,
                    "Botin de Camarones",
                    "Cada camaron rinde mas cuando el oceano se pone pesado.",
                    200,
                    1.6f,
                    PermanentUpgradeEffectModes.Multiplier,
                    1f,
                    0.05f,
                    "ShopMenu/Skills/Upgrades/MoneyMultiplier",
                    "ShopMenu/Skills/Upgrades/MoneyMultiplierInk"),
                Upgrade(
                    PlayerUnlockableIds.ScoreMultiplierUpgrade,
                    "Gloria Marina",
                    "Cada maniobra peligrosa deja una historia mas grande.",
                    150,
                    1.6f,
                    PermanentUpgradeEffectModes.Multiplier,
                    1f,
                    0.05f,
                    "ShopMenu/Skills/Upgrades/PointMultiplier",
                    "ShopMenu/Skills/Upgrades/PointMultiplierInk")
            }
        };

        data.Normalize();
        return data;
    }

    private static UnlockableSkinDefinition Skin(
        string id,
        string displayName,
        string description,
        int basePrice,
        string shopSpriteResourcePath,
        bool defaultUnlocked = false,
        string playerSkinPrefabResourcePath = "")
    {
        return new UnlockableSkinDefinition
        {
            id = id,
            displayName = displayName,
            description = description,
            defaultUnlocked = defaultUnlocked,
            basePrice = basePrice,
            shopSpriteResourcePath = shopSpriteResourcePath,
            playerSkinPrefabResourcePath = playerSkinPrefabResourcePath,
            unlockGoal = UnlockGoalDefinition.None()
        };
    }

    private static PermanentUpgradeDefinition Upgrade(
        string id,
        string displayName,
        string description,
        int basePrice,
        float priceGrowthMultiplier,
        string effectMode,
        float baseEffectValue,
        float effectPerLevel,
        string shopSpriteResourcePath,
        string shopHighlightedSpriteResourcePath)
    {
        return new PermanentUpgradeDefinition
        {
            id = id,
            displayName = displayName,
            description = description,
            maxLevel = 10,
            basePrice = basePrice,
            priceGrowthMultiplier = priceGrowthMultiplier,
            effectMode = effectMode,
            baseEffectValue = baseEffectValue,
            effectPerLevel = effectPerLevel,
            shopSpriteResourcePath = shopSpriteResourcePath,
            shopHighlightedSpriteResourcePath = shopHighlightedSpriteResourcePath,
            unlockGoal = UnlockGoalDefinition.None()
        };
    }

    public void Normalize()
    {
        int sourceVersion = Math.Max(1, version);
        version = Math.Max(1, version);
        skins = NormalizeDefinitions(skins);
        runGadgets = NormalizeDefinitions(runGadgets);
        permanentUpgrades = NormalizeDefinitions(permanentUpgrades);

        if (sourceVersion < 2)
        {
            NormalizeLegacyPermanentUpgradeEffects();
        }
    }

    private static T[] NormalizeDefinitions<T>(T[] definitions) where T : UnlockableDefinitionBase
    {
        return definitions?
            .Where(definition => definition != null && !string.IsNullOrWhiteSpace(definition.id))
            .Select(definition =>
            {
                definition.Normalize();
                return definition;
            })
            .GroupBy(definition => definition.id)
            .Select(group => group.First())
            .ToArray() ?? Array.Empty<T>();
    }

    private void NormalizeLegacyPermanentUpgradeEffects()
    {
        PermanentUpgradeDefinition rechargeRateUpgrade = permanentUpgrades
            .FirstOrDefault(upgrade => upgrade.id == PlayerUnlockableIds.InkPulseRechargeRateUpgrade);
        if (rechargeRateUpgrade == null)
        {
            return;
        }

        if (rechargeRateUpgrade.effectPerLevel > 0f && rechargeRateUpgrade.effectPerLevel <= 1f)
        {
            rechargeRateUpgrade.effectPerLevel *= LegacyRechargeRateReferenceChargePerSecond;
        }
    }
}

[Serializable]
public abstract class UnlockableDefinitionBase
{
    public string id;
    public string displayName;
    public string description;
    public bool defaultUnlocked;
    public int basePrice;
    public string shopSpriteResourcePath;
    public string shopHighlightedSpriteResourcePath;
    public UnlockGoalDefinition unlockGoal = UnlockGoalDefinition.None();

    public virtual void Normalize()
    {
        id = id?.Trim() ?? string.Empty;
        displayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName.Trim();
        description = description?.Trim() ?? string.Empty;
        basePrice = Math.Max(0, basePrice);
        shopSpriteResourcePath = NormalizeResourcePath(shopSpriteResourcePath);
        shopHighlightedSpriteResourcePath = NormalizeResourcePath(shopHighlightedSpriteResourcePath);
        unlockGoal ??= UnlockGoalDefinition.None();
        unlockGoal.Normalize();
    }

    protected static string NormalizeResourcePath(string value)
    {
        string normalized = value?.Trim().Replace('\\', '/') ?? string.Empty;
        return normalized.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            ? normalized.Substring(0, normalized.Length - 4)
            : normalized;
    }
}

[Serializable]
public class UnlockableSkinDefinition : UnlockableDefinitionBase
{
    public string shopBuyedSpriteResourcePath;
    public string shopSelectedSpriteResourcePath;
    public string playerSkinPrefabResourcePath;

    public override void Normalize()
    {
        base.Normalize();
        shopBuyedSpriteResourcePath = NormalizeResourcePath(shopBuyedSpriteResourcePath);
        shopSelectedSpriteResourcePath = NormalizeResourcePath(shopSelectedSpriteResourcePath);
        playerSkinPrefabResourcePath = NormalizeResourcePath(playerSkinPrefabResourcePath);
    }
}

[Serializable]
public class RunGadgetUnlockDefinition : UnlockableDefinitionBase
{
    public string gameplayId;

    public override void Normalize()
    {
        base.Normalize();
        gameplayId = gameplayId?.Trim() ?? string.Empty;
    }
}

[Serializable]
public class PermanentUpgradeDefinition : UnlockableDefinitionBase
{
    public int maxLevel = 1;
    public float priceGrowthMultiplier = 1f;
    public string effectMode = PermanentUpgradeEffectModes.Multiplier;
    public float baseEffectValue = 1f;
    public float effectPerLevel = 0.1f;

    public bool IsAdditiveEffect =>
        string.Equals(effectMode, PermanentUpgradeEffectModes.Additive, StringComparison.OrdinalIgnoreCase);

    public override void Normalize()
    {
        base.Normalize();
        maxLevel = Math.Max(1, maxLevel);
        priceGrowthMultiplier = Math.Max(1f, priceGrowthMultiplier);
        effectPerLevel = Math.Max(0f, effectPerLevel);
        NormalizeEffectContract();
    }

    private void NormalizeEffectContract()
    {
        effectMode = PermanentUpgradeEffectModes.Normalize(effectMode, GetDefaultEffectMode());

        if (id == PlayerUnlockableIds.InkPulseRechargeRateUpgrade)
        {
            effectMode = PermanentUpgradeEffectModes.Additive;
            baseEffectValue = 0f;
            return;
        }

        if (IsAdditiveEffect)
        {
            baseEffectValue = Math.Max(0f, baseEffectValue);
            return;
        }

        baseEffectValue = baseEffectValue > 0f ? baseEffectValue : 1f;
    }

    private string GetDefaultEffectMode()
    {
        return id == PlayerUnlockableIds.InkPulseRechargeRateUpgrade
            ? PermanentUpgradeEffectModes.Additive
            : PermanentUpgradeEffectModes.Multiplier;
    }
}

[Serializable]
public class UnlockGoalDefinition
{
    public string goalType = "None";
    public long targetValue;

    public static UnlockGoalDefinition None()
    {
        return new UnlockGoalDefinition
        {
            goalType = UnlockGoalTypes.None,
            targetValue = 0
        };
    }

    public void Normalize()
    {
        goalType = string.IsNullOrWhiteSpace(goalType) ? UnlockGoalTypes.None : goalType.Trim();
        targetValue = Math.Max(0, targetValue);
    }
}

public static class UnlockGoalTypes
{
    public const string None = "None";
    public const string BestScore = "BestScore";
    public const string TotalShrimpsCollected = "TotalShrimpsCollected";
    public const string TotalRuns = "TotalRuns";
    public const string TotalPortalsCrossed = "TotalPortalsCrossed";
}

public static class PermanentUpgradeEffectModes
{
    public const string Multiplier = "Multiplier";
    public const string Additive = "Additive";

    public static string Normalize(string effectMode, string fallback)
    {
        string normalizedFallback = IsKnown(fallback) ? fallback : Multiplier;
        if (string.IsNullOrWhiteSpace(effectMode))
        {
            return normalizedFallback;
        }

        string trimmed = effectMode.Trim();
        if (string.Equals(trimmed, Multiplier, StringComparison.OrdinalIgnoreCase))
        {
            return Multiplier;
        }

        if (string.Equals(trimmed, Additive, StringComparison.OrdinalIgnoreCase))
        {
            return Additive;
        }

        return normalizedFallback;
    }

    public static bool IsKnown(string effectMode)
    {
        return string.Equals(effectMode, Multiplier, StringComparison.OrdinalIgnoreCase)
            || string.Equals(effectMode, Additive, StringComparison.OrdinalIgnoreCase);
    }
}
