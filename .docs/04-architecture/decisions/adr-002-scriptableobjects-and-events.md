# ADR-002: Arquitectura Orientada a Datos (`ScriptableObjects`) y Eventos C#

* **Fecha:** 2026-11-24
* **Estado:** Aceptado

---

## 1. Contexto
Los 5 sistemas principales de **BOREATEK** (`PlayerController`, `TemperatureSystem`, `HarvestableNode`, `InventoryAndShop`, `DayNightCycleManager`) comparten constantes de balance (tasas de enfriamiento, dureza de minerales, precios en Monedas de Jengibre) y necesitan reaccionar a cambios globales (como el inicio de una *Ventisca Ciega* o *La Gran Nochebuena*).

## 2. Decisión
1. Externalizar todos los datos de configuración y balance en activos **`ScriptableObject`** dentro de `Assets/_Project/Data/`.
2. Prohibir las referencias circulares directas entre `MonoBehaviours` en `Update()`, utilizando **eventos nativos de C# (`event Action<T>`)** para notificar cambios de estado.

## 3. Consecuencias
* **Positivas:**
  * Ajuste de balance inmediato en el Inspector de Unity sin modificar código.
  * Pruebas unitarias y escenas de sandbox (`Sandbox_Test.unity`) fáciles de montar sin requerir que todos los managers existan simultáneamente.
* **Restricciones:**
  * Todo suscriptor de un evento en `OnEnable()` **debe** desuscribirse obligatoriamente en `OnDisable()` para prevenir fugas de memoria (*memory leaks*).
