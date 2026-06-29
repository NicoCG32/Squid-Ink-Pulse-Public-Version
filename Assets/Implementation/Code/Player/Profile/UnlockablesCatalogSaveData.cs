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
                Skin(PlayerSkinIds.Default, "Default", string.Empty, 0, string.Empty, defaultUnlocked: true),
                Skin("skin.king", "Rey Abisal", "Avanza entre corrientes y peligros como si el oceano le debiera respeto.", 250000, "ShopMenu/Skins/King"),
                Skin("skin.bob_marley", "Rasta Marino", "Cuando el oceano se pone pesado, responde con ritmo, calma y tinta.", 420000, "ShopMenu/Skins/BobMarley"),
                Skin("skin.german", "Calamar Aleman", "Esquiva con precision quirurgica, incluso cuando el mar decide improvisar.", 180000, "ShopMenu/Skins/German"),
                Skin("skin.vikingo", "Vikingo", "Las olas rugen, los obstaculos tiemblan y el sigue conquistando profundidad.", 350000, "ShopMenu/Skins/Vikingo"),
                Skin("skin.rockstar", "Rockstar", "Convierte cada dash peligroso en un solo epico bajo presion marina.", 500000, "ShopMenu/Skins/Rockstar"),
                Skin("skin.formal", "Formal", "Porque escapar del caos oceanico tambien puede hacerse con elegancia.", 75000, "ShopMenu/Skins/Formal"),
                Skin("skin.china", "Tradicion Marina", "Enfrenta las corrientes con serenidad, tecnica y una paciencia legendaria.", 275000, "ShopMenu/Skins/China"),
                Skin("skin.fantasma", "Fantasma", "Atraviesa el peligro tan suave que hasta las amenazas dudan si lo vieron.", 666666, "ShopMenu/Skins/Fantasma"),
                Skin("skin.sonic", "Erizo Veloz", "Cuando el oceano acelera el caos, el acelera todavia mas.", 1200000, "ShopMenu/Skins/Sonic"),
                Skin("skin.raya", "Raya Marina", "Se desliza entre obstaculos como sombra elegante en plena tormenta submarina.", 95000, "ShopMenu/Skins/Raya"),
                Skin("skin.huaso", "Huaso Submarino", "Con coraje de sobra, zapatea entre corrientes como si fueran cueca brava.", 181000, "ShopMenu/Skins/Huaso"),
                Skin("skin.zombie", "Zombie", "El oceano insiste en detenerlo, pero su determinacion sigue flotando.", 333333, "ShopMenu/Skins/Zombie"),
                Skin("skin.chile", "Chile Marino", "Cuando el mar se pone dificil, responde con orgullo, tinta y aguante.", 180920, "ShopMenu/Skins/Chile"),
                Skin("skin.nemo", "Pez Aventurero", "Puede dar vueltas de mas, pero siempre encuentra una salida con estilo.", 700000, "ShopMenu/Skins/Nemo"),
                Skin("skin.clown", "Payaso Abisal", "Hace reir al peligro justo antes de esquivarlo con un dash impecable.", 222222, "ShopMenu/Skins/Clown"),
                Skin("skin.black_metal", "Black Metal", "Oscuro, intenso y perfecto para sobrevivir cuando el oceano se pone brutal.", 1500000, "ShopMenu/Skins/BlackMetal"),
                Skin("skin.travis", "Travis", "Brilla entre el caos submarino como si cada obstaculo fuera parte del show.", 10000000, "ShopMenu/Skins/Travis")
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
                    "La tinta permanece activa por mas tiempo, extendiendo la duracion del impulso.",
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
                    "El pulso de tinta se regenera mas rapido, permitiendo impulsarte con mayor frecuencia.",
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
                    "Aumenta la cantidad de camarones recolectados, mejorando las recompensas de cada partida.",
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
                    "Cada maniobra arriesgada vale mas, aumentando el puntaje obtenido durante la partida.",
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
        bool defaultUnlocked = false)
    {
        return new UnlockableSkinDefinition
        {
            id = id,
            displayName = displayName,
            description = description,
            defaultUnlocked = defaultUnlocked,
            basePrice = basePrice,
            shopSpriteResourcePath = shopSpriteResourcePath,
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
