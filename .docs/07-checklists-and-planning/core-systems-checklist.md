# Checklist de Programación de Sistemas Nucleares (C# Unity 6)

---

## 1. Arquitectura y Configuración Inicial
- [ ] Crear carpeta `Assets/_Project/` con todas las subcarpetas definidas en `04-architecture/unity-folder-structure.md`.
- [ ] Configurar `Boreatek.Runtime.asmdef` referenciando `Unity.InputSystem` y `Unity.RenderPipelines.Universal.Runtime`.
- [ ] Crear capas de física en `TagManager.asset`: `Player`, `Interactable`, `HeatZone`, `Enemy`.
- [ ] Implementar interfaz base `IInteractable.cs` en `Boreatek.Core`.

## 2. M-01: `PlayerController.cs`
- [ ] Lectura de `Move`, `Look`, `Jump`, `Sprint`, `Attack`, `Interact` desde `InputSystem_Actions`.
- [ ] Multiplicador de velocidad en nieve profunda (`0.75x`) y anulación al desbloquear *Botas de Nieve Nivel 2*.
- [ ] Control de encendido/apagado del **Faro Frontal LED del Casco Minero**.
- [ ] Golpe con el **Pico Industrial de Cristal Energético Rojo** y detección por raycast.

## 3. M-02: `TemperatureSystem.cs`
- [ ] Drenaje térmico dinámico según bioma ($D_{\text{bioma}}$) y clima ($M_{\text{clima}}$).
- [ ] Detección de burbujas térmicas (`HeatSourceNode`: Aldea Industrial, Guirnaldas Luminosas y Balizas Térmicas).
- [ ] Aplicación de resistencia del **Abrigo Térmico Acolchado** (`0 %`, `25 %`, `50 %`).
- [ ] Efecto del **Termo de Chocolate Caliente** (`100 °T` + `60s` sin enfriamiento).
- [ ] Rescate por hipotermia a `0 °T` con pérdida del `30 %` de recursos no asegurados en el tanque-mochila.

## 4. M-03: `HarvestableNode.cs`
- [ ] Configuración mediante `HarvestableNodeConfigSO` para los 8 tipos de nodos.
- [ ] Bloqueo por nivel de pico insuficiente (`RequiredPickaxeTier` 1, 2 o 3).
- [ ] Lógica de deshielo de `15 segundos` para **Cofres Congelados** al detectar una Baliza Térmica cercana.
- [ ] Multiplicador `2x` de recolección durante **La Gran Nochebuena**.

## 5. M-04: `InventoryAndShop.cs`
- [ ] Límite de ranuras del **Tanque-Mochila Presurizado de Latón** (`10` ➔ `20` ➔ `35`).
- [ ] Descarga en la **Tolva Central** y conversión a **Monedas de Jengibre / Fichas del Taller**.
- [ ] Compra y aplicación de las 4 categorías de la tienda (Consumibles, Herramientas Tácticas, Mejoras Permanentes y Automatización).

## 6. M-05, M-06 y M-07: Ciclo 20 Minutos, IA y Automatización
- [ ] `DayNightCycleManager.cs`: Ciclo de 20 minutos reales (`10m` día / `10m` noche), transición de **Ventisca Ciega** y disparo de **La Gran Nochebuena**.
- [ ] `DefectiveToyAI.cs`: Patrulla en `NavMesh`, detección de luz del faro frontal / calor de balizas y persecución.
- [ ] `MechanicalElfExtractor.cs` y `ConveyorBelt.cs`: Extracción automática cada `10s` y entrega continua a la Tolva Central.
