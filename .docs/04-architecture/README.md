# 04 — Arquitectura de Software en Unity 6 (`04-architecture`)

Esta carpeta define la arquitectura técnica del proyecto en **Unity 6 (`6000.6.0f1` URP)** siguiendo la estructura de ingeniería de software: diagramas de arquitectura (**C4**), registro de decisiones de arquitectura (**ADRs**), estándares de codificación (**Standards**), modelo de datos (**ScriptableObjects**) y jerarquía de carpetas en `Assets/`.

## 📂 Estructura de `04-architecture`

* [**`unity-folder-structure.md`**](unity-folder-structure.md) — Organización obligatoria de carpetas dentro de `Assets/_Project/` y separación de `Assembly Definitions` (`.asmdef`).
* [**`data-model-scriptable-objects.md`**](data-model-scriptable-objects.md) — Diagrama Entidad-Relación del modelo de datos en Unity (`ScriptableObjects` de recursos, biomas, mejoras e inventario).
* [**`c4/`**](c4/) — Diagramas de arquitectura de sistemas:
  * [`system-context-and-containers.md`](c4/system-context-and-containers.md) — Niveles C1 y C2: Subsistemas del motor (Input System, URP, Core Gameplay, AI Navigation, UI).
  * [`event-bus-and-communication.md`](c4/event-bus-and-communication.md) — Nivel C3: Desacoplamiento mediante eventos C# (`Action<T>`) e interfaces (`IInteractable`).
* [**`decisions/`**](decisions/) — Registros de Decisiones de Arquitectura (ADRs):
  * [`adr-001-unity6-urp-stylized-pipeline.md`](decisions/adr-001-unity6-urp-stylized-pipeline.md)
  * [`adr-002-scriptableobjects-and-events.md`](decisions/adr-002-scriptableobjects-and-events.md)
  * [`adr-003-new-input-system-actions.md`](decisions/adr-003-new-input-system-actions.md)
* [**`standards/`**](standards/) — Reglas y estándares de programación:
  * [`csharp-coding-conventions.md`](standards/csharp-coding-conventions.md) — Nomenclatura, principios SOLID, serialización segura y plantilla oficial de script.
  * [`unity-performance-and-physics-rules.md`](standards/unity-performance-and-physics-rules.md) — Optimización de memoria, física `NonAlloc`, caché de componentes y presupuesto de luces URP.
