# 05 — Especificación Técnica de Módulos C# (`05-modules`)

Esta carpeta documenta a detalle cada uno de los scripts y subsistemas en C# que conforman **BOREATEK**, definiendo sus campos serializados, propiedades públicas, eventos, dependencias y fragmentos de referencia listos para su implementación en `Assets/_Project/Scripts/`.

## 📄 Documentos de Módulos

* [**`player-controller-module.md`**](player-controller-module.md) — `PlayerController.cs` y `HeadlampController.cs` (Locomoción en nieve, cámara 3D, faro LED del casco minero, raycast de interacción y uso del pico energético).
* [**`temperature-system-module.md`**](temperature-system-module.md) — `TemperatureSystem.cs` y `HeatSourceNode.cs` (Cálculo térmico por bioma, burbujas de calor de guirnaldas/balizas, abrigo térmico y rescate por hipotermia).
* [**`harvestable-node-module.md`**](harvestable-node-module.md) — `HarvestableNode.cs` y `FrozenChest.cs` (Resistencia de vetas, nivel de herramienta requerido, deshielo por calor y sobrecarga `2x`).
* [**`inventory-and-shop-module.md`**](inventory-and-shop-module.md) — `InventoryAndShop.cs` y `CentralHopper.cs` (Capacidad del Tanque-Mochila de Latón, conversión a Monedas de Jengibre y compras en tienda).
* [**`day-night-and-weather-module.md`**](day-night-and-weather-module.md) — `DayNightCycleManager.cs` (Ciclos de 20 minutos reales, transición de luz direccional, evento de **Ventisca Ciega** y **La Gran Nochebuena**).
* [**`enemy-ai-and-automation-module.md`**](enemy-ai-and-automation-module.md) — `DefectiveToyAI.cs`, `MechanicalElfExtractor.cs` y `ConveyorBelt.cs` (IA del Soldado de Juguete Defectuoso y red de extracción automatizada).
