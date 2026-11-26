# Modelo de Datos y Relaciones (`ScriptableObjects` & Runtime State)

> **Equivalente al Diagrama Entidad-Relación (ER) para la Arquitectura Orientada a Datos en Unity 6**

---

## 1. Diagrama de Entidades de Datos y Estado en Runtime

```mermaid
erDiagram
    BIOME_THERMAL_PROFILE_SO ||--o{ WORLD_ZONE_TRIGGER : "configura drenaje termico"
    RESOURCE_DATA_SO ||--o{ HARVESTABLE_NODE_CONFIG_SO : "es producido por"
    HARVESTABLE_NODE_CONFIG_SO ||--o{ HARVESTABLE_NODE : "define dureza y nivel de pico"
    RESOURCE_DATA_SO ||--o{ INVENTORY_SLOT : "almacenado en Tanque-Mochila"
    SHOP_ITEM_SO ||--o{ SHOP_CATALOG_SO : "pertenece a"

    BIOME_THERMAL_PROFILE_SO {
        string biomeId
        string displayName
        float baseCoolingRatePerSec
        float deepSnowSpeedMultiplier
        Color ambientFogColor
    }

    RESOURCE_DATA_SO {
        string resourceId
        string displayName
        Sprite icon
        int gingerbreadCoinValue
        bool isInsuredAgainstHypothermia
    }

    HARVESTABLE_NODE_CONFIG_SO {
        string nodeId
        NodeCategory category
        int requiredPickaxeTier
        int maxHitPoints
        bool requiresHeatToMelt
        float meltDurationSeconds
        int minDropAmount
        int maxDropAmount
    }

    SHOP_ITEM_SO {
        string itemId
        string displayName
        ShopCategory category
        int costInGingerbreadCoins
        int targetUpgradeTier
        float effectValue
        float effectDuration
    }

    INVENTORY_SLOT {
        string resourceId
        int currentAmount
    }
```

---

## 2. Ventajas del Modelo Basado en `ScriptableObjects`

1. **Cero Duplicación en Memoria:** Si existen 200 *Pinos Cristalizados* y 150 vetas de *Cristal de Caramelo* en la escena, todos apuntan a la misma instancia en memoria de `HARVESTABLE_NODE_CONFIG_SO`.
2. **Balanceo sin Recompilar Código:** Los diseñadores pueden ajustar en el Inspector el valor en **Monedas de Jengibre** de cada mineral, la tasa de enfriamiento del *Cementerio de Trineos* o el costo del *Pico Nivel 2* incluso mientras prueban el juego en el Editor.
