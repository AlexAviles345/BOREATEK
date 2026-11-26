using UnityEngine;

namespace Boreatek.Core
{
    internal enum EquipmentSlot { Pickaxe, Chocolate, Cookie, Peppermint, Garland, Beacon, Firecracker, Extractor, Sword }

    /// <summary>
    /// Identificadores de los 8 recursos recolectables de Boreatek.
    /// </summary>
    public enum ResourceType
    {
        CrystallizedWood = 0,
        PolarScrap = 1,
        BlackCoal = 2,
        CandyCrystal = 3,
        PolarIron = 4,
        DeepBlueIce = 5,
        FrozenChestSupply = 6,
        LuminousCore = 7
    }

    /// <summary>
    /// Categoría de bioma de superficie o estrato subterráneo.
    /// </summary>
    public enum BiomeType
    {
        SantaIndustrialVillage = 0,
        OuterTundra = 1,
        GiantCandyForest = 2,
        SleighGraveyard = 3,
        VentilationTunnels = 4,
        IceCavesLevel1 = 5,
        BlueIceGeodeLevel2 = 6,
        LuminousCoreAbyssLevel3 = 7,
        GlacialFlats = 8,
        SnowDunes = 9,
        SnowyTaiga = 10,
        GlacialMoraine = 11,
        FrostBirchForest = 12,
        FrostValley = 13,
        AlpineRidges = 14
    }

    internal static class BiomeRules
    {
        internal static bool IsUnderground(BiomeType biome) =>
            biome is BiomeType.VentilationTunnels or BiomeType.IceCavesLevel1 or
                BiomeType.BlueIceGeodeLevel2 or BiomeType.LuminousCoreAbyssLevel3;
    }

    /// <summary>
    /// Estados climáticos dinámicos gobernados por DayNightCycleManager.
    /// </summary>
    public enum WeatherState
    {
        ClearArcticDay = 0,
        PolarNight = 1,
        BlindBlizzard = 2,
        ChristmasEveOvercharge = 3
    }

    /// <summary>
    /// Categorías de la tienda de la Aldea Industrial.
    /// </summary>
    public enum ShopCategory
    {
        Consumable = 0,
        TacticalTool = 1,
        PermanentUpgrade = 2,
        Automation = 3
    }

    /// <summary>
    /// Tipo de acción al comprar un artículo en la tienda.
    /// </summary>
    public enum ShopActionType
    {
        HotChocolateThermos = 0,
        GingerbreadCookie = 1,
        PeppermintCane = 2,
        FestiveStringLights = 3,
        ThermalBeacon = 4,
        FestiveFirecrackers = 5,
        GiftRadar = 6,
        UpgradePickaxe = 7,
        UpgradeBrassBackpack = 8,
        UpgradeThermalCoat = 9,
        MechanicalElfExtractor = 10,
        ConveyorBeltSegment = 11
    }
}
