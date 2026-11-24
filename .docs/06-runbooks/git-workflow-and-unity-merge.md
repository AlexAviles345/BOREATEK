# Runbook: Flujo de Trabajo en Git y Prevención de Conflictos en Unity

---

## 1. Modelo de Ramas (Git Flow Ligero)

* `main`: Rama de entregas estables (Builds verificadas de la Mini Game Jam).
* `develop`: Rama principal de integración diaria.
* `feature/<modulo>`: Para programación de sistemas (ej. `feature/temperature-system`, `feature/hopper-economy`).
* `art/<activo>`: Para integración de modelos 3D low-poly, shaders e iluminación (ej. `art/elf-engineer-rig`, `art/blue-ice-geode`).
* `docs/<tema>`: Para expansiones documentales dentro de `.docs/`.

---

## 2. Convención de Mensajes de Commit

Se mantiene el estándar utilizado en el historial del repositorio:
* `[ADD] Descripción clara` — Nuevos archivos, scripts, artes conceptuales o sistemas.
* `[FIX] Descripción clara` — Corrección de errores de código, colisiones o referencias rotas.
* `[UPDATE] Descripción clara` — Ajustes de balance en `ScriptableObjects`, mejoras visuales o refactorización.
* `[DOCS] Descripción clara` — Cambios exclusivos en la documentación `.docs/`.

---

## 3. Reglas de Oro para Evitar Conflictos en Archivos `.unity` y `.meta`

1. **Regla del `.meta` Indivisible:**
   * Cada vez que creas, mueves o renombras un archivo o carpeta en Unity, debes hacerlo **desde dentro del panel Project del Editor de Unity** (o asegurarte de mover su `.meta` junto con él).
   * Nunca hagas commit de un script o modelo sin su archivo `.meta` correspondiente.
2. **Escena Principal Basada en Prefabs:**
   * Dos personas no deben editar `BoreatekWorld.unity` al mismo tiempo.
   * Cada bioma (*Aldea Industrial*, *Bosque de Caramelos*, *Cueva Nivel 1*) y cada sistema (*HUD*, *Duende Ingeniero*, *Tolva Central*) debe ser un **Prefab** independiente en `Assets/_Project/Prefabs/`. Así cada desarrollador modifica su propio `.prefab` sin tocar el archivo `.unity`.
