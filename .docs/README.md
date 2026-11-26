# 🎄⚙️ BOREATEK — Centro de Documentación Técnica y de Diseño (`.docs`)

> **Mini Game Jam — Una navidad distinta a la ordinaria (3D)**  
> **Motor:** Unity 6 (`6000.6.0f1` URP) | **Género:** Exploración, Supervivencia Térmica, Minería y Automatización 3D

Bienvenido a la documentación oficial de **BOREATEK**. El repositorio documental está organizado en **9 módulos numerados (`00` a `08`)**, cada uno desglosado en múltiples documentos especializados (`.md`) y subcarpetas temáticas para cubrir desde el lore y la biblia de arte (con sus 11 artes conceptuales integrados en `02-art-bible/concept-art/`) hasta la arquitectura C4, decisiones ADR, contratos de código C# y guías operativas en Unity 6.

---

## 🗂️ Árbol Completo de la Documentación (`.docs/`)

```text
.docs/
├── README.md                                       # Índice Maestro del Centro Documental
│
├── 00-overview/                                    # Visión ejecutiva, pilares y alcance
│   ├── README.md
│   ├── game-pitch-and-pillars.md                   # Sinopsis, ficha técnica y 4 pilares de diseño
│   ├── core-gameplay-loop.md                       # Bucle micro, medio (20 min) y macro (20 horas)
│   └── game-jam-scope.md                           # Alcance MVP Mini Game Jam vs. Visión Completa
│
├── 01-lore-and-world/                              # Universo, cronología, biomas y entidades
│   ├── README.md
│   ├── worldbuilding-and-geothermal-lore.md        # La Falla Geotérmica Polar, minerales y rebelión oxidada
│   ├── campaign-20h-timeline.md                    # Progresión continua de 20 horas (60 ciclos de 20 min)
│   ├── surface-biomes-and-weather.md               # Aldea Industrial, Tundra, Caramelos, Cementerio y Ventisca
│   ├── underground-excavation-levels.md            # Túneles de Ventilación, Cuevas N1, Geoda N2 y Abismo N3
│   └── characters-and-bestiary.md                  # Duende Ingeniero (IP 100% original) y Juguetes Defectuosos
│
├── 02-art-bible/                                   # Biblia de arte, estilo 3D low-poly y galería conceptual
│   ├── README.md
│   ├── visual-direction-lowpoly-vs-concept.md      # REGLA CRÍTICA: Juego 3D Estilizado/Low-Poly vs. Concept Art
│   ├── color-palette-and-urp-lighting.md           # Paleta Frío vs. Calor Industrial, luces URP, niebla y Bloom
│   ├── 3d-modeling-and-character-standards.md      # Proporciones Chibi, presupuesto poligonal, escala y pivotes
│   ├── shaders-and-vfx-specs.md                    # Shaders (Cristal, Hielo Azul, Magma) y catálogo de partículas
│   └── concept-art/                                # Las 11 piezas oficiales de Concept Art
│       ├── paisajes/                               # 9 escenarios (Superficie, Ventisca y Subsuelo N1-N3)
│       └── personajes/                             # 2 personajes (Duende Ingeniero 3D y Soldado de Plomo)
│
├── 03-game-design/                                 # Mecánicas detalladas, fórmulas y balance (GDD)
│   ├── README.md
│   ├── thermal-survival-mechanics.md               # Ecuación térmica, burbujas de calor e hipotermia
│   ├── mining-and-harvestable-resources.md         # Matriz de los 8 recursos, dureza y Cofres Congelados
│   ├── economy-hopper-and-shop.md                  # Tolva Central, Monedas de Jengibre y catálogo de tienda
│   ├── automation-and-conveyors.md                 # Duendes Mecánicos Extractores y Cintas Transportadoras
│   └── christmas-eve-climax-event.md               # Evento final (Hito 20h): Trineo Blindado y sobrecarga 2x
│
├── 04-architecture/                                # Ingeniería de software en Unity 6 y estándares C#
│   ├── README.md
│   ├── unity-folder-structure.md                   # Jerarquía obligatoria de Assets/_Project/
│   ├── data-model-scriptable-objects.md            # Diagrama ER de ScriptableObjects e inventario
│   ├── c4/
│   │   ├── system-context-and-containers.md        # Diagramas C1 y C2 de subsistemas en Unity 6
│   │   └── event-bus-and-communication.md          # Nivel C3: Bus de eventos Action<T> e IInteractable
│   ├── decisions/
│   │   ├── adr-001-unity6-urp-stylized-pipeline.md # ADR-001: URP 17.6 y estilo 3D Low-Poly Estilizado
│   │   ├── adr-002-scriptableobjects-and-events.md # ADR-002: Arquitectura orientada a datos y eventos
│   │   └── adr-003-new-input-system-actions.md     # ADR-003: Uso de InputSystem_Actions.inputactions
│   └── standards/
│       ├── csharp-coding-conventions.md            # Nomenclatura (_camelCase, PascalCase) y encapsulamiento
│       └── unity-performance-and-physics-rules.md  # Reglas Zero-GC, caché en Awake, Layers y luces URP
│
├── 05-modules/                                     # Especificación técnica de cada script/módulo C#
│   ├── README.md
│   ├── player-controller-module.md                 # M-01: PlayerController.cs y Faro Frontal LED
│   ├── temperature-system-module.md                # M-02: TemperatureSystem.cs y HeatSourceNode.cs
│   ├── harvestable-node-module.md                  # M-03: HarvestableNode.cs y Cofres Congelados
│   ├── inventory-and-shop-module.md                # M-04: InventoryAndShop.cs y Tolva Central
│   ├── day-night-and-weather-module.md             # M-05: DayNightCycleManager.cs y Ventisca Ciega
│   └── enemy-ai-and-automation-module.md           # M-06 y M-07: DefectiveToyAI.cs y Automatización
│
├── 06-runbooks/                                    # Procedimientos operativos paso a paso
│   ├── README.md
│   ├── git-workflow-and-unity-merge.md             # Ramas Git, commits y prevención de conflictos .unity/.meta
│   ├── create-new-biome-and-node.md                # Guía para integrar un FBX, Material URP, SO y Prefab
│   ├── urp-lighting-and-fog-setup.md               # Configuración de Global Volume, Bloom y niebla exponencial
│   └── build-and-release-pipeline.md               # Proceso de compilación y empaquetado Windows 64-bit
│
├── 07-checklists-and-planning/                     # Planificación por hitos y listas de control
│   ├── README.md
│   ├── roadmap-and-milestones.md                   # Hoja de ruta en 5 fases de desarrollo
│   ├── core-systems-checklist.md                   # Checklist de implementación de scripts C#
│   ├── art-3d-and-audio-checklist.md               # Checklist de modelos 3D estilizados, shaders y audio
│   └── qa-performance-and-release-checklist.md     # Checklist de pruebas funcionales y 60+ FPS
│
└── 08-prompts-and-templates/                       # Prompts de IA y plantillas documentales
    ├── README.md
    ├── ai-art-and-3d-prompts.md                    # Prompts para turnarounds 3D estilizados y props
    ├── unity-csharp-coding-prompts.md              # Prompts de contexto para código C# y shaders en Unity 6
    └── templates/
        ├── biome-spec-template.md                  # Plantilla para nuevos biomas
        ├── csharp-module-spec-template.md          # Plantilla para nuevos módulos C#
        └── adr-template.md                         # Plantilla para nuevas decisiones de arquitectura
```
