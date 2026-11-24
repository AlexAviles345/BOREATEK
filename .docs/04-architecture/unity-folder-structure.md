# Estructura Oficial de Carpetas en Unity (`Assets/`)

Para evitar el desorden típico en proyectos de Unity y facilitar el trabajo colaborativo en Git, todo activo propio de **BOREATEK** se almacena bajo `Assets/_Project/` (el guion bajo garantiza que aparezca siempre en la parte superior del panel *Project*).

---

## 1. Árbol de Directorios en `Assets/`

```text
Assets/
├── _Project/                           # Raíz exclusiva de activos y código de BOREATEK
│   ├── Art/
│   │   ├── Animations/                 # Animator Controllers y clips (.anim)
│   │   ├── Materials/                  # Materiales URP Lit y Custom Shader Materials (prefijo M_)
│   │   ├── Meshes/                     # Modelos 3D estilizados/low-poly (.fbx)
│   │   │   ├── Characters/             # SK_ElfEngineer, SK_DefectiveLeadSoldier, SK_FrostGolem
│   │   │   ├── Environment/            # Biomas, pinos cristalizados, árboles de caramelo, geodas
│   │   │   ├── Industrial/             # Aldea Industrial, torres, forja, tolva, túneles, cintas
│   │   │   └── Props/                  # Casco, tanque-mochila, pico energético, guirnaldas, cofres
│   │   ├── Shaders/                    # Shader Graphs (SG_StylizedCrystal, SG_TranslucentBlueIce)
│   │   ├── Textures/                   # Texturas PBR estilizadas y Atlas (prefijo T_)
│   │   └── VFX/                        # Prefabs de Particle Systems (Ventisca, Chispas, Burbuja Calor)
│   ├── Audio/
│   │   ├── Music/                      # Música ambiental polar e industrial
│   │   └── SFX/                        # Efectos de sonido (pico, viento, engranajes, tolva, tienda)
│   ├── Data/                           # Instancias serializadas de ScriptableObjects (.asset)
│   │   ├── Biomes/                     # Perfiles térmicos por bioma (SO_Biome_Tundra, etc.)
│   │   ├── Resources/                  # Datos de los 8 recursos y nodos recolectables
│   │   └── Shop/                       # Catálogo de consumibles, tácticos, mejoras y autómatas
│   ├── Prefabs/
│   │   ├── Characters/                 # Prefab_Player_ElfEngineer, Prefab_Enemy_LeadSoldier
│   │   ├── Environment/                # Módulos de biomas y túneles listos para ensamblar
│   │   ├── Gameplay/                   # Nodos recolectables, cofres congelados, guirnaldas, balizas
│   │   └── UI/                         # Canvas de HUD térmico, Tienda de la Aldea y Avisos
│   ├── Scenes/
│   │   ├── Boot.unity                  # Inicialización de sistemas globales
│   │   ├── BoreatekWorld.unity         # Escena principal del mundo 3D (Superficie + Subsuelo)
│   │   └── Sandbox_Test.unity          # Escena aislada para pruebas rápidas de mecánicas
│   └── Scripts/
│       ├── Boreatek.Runtime.asmdef     # Definición de ensamblado principal
│       ├── Core/                       # DayNightCycleManager, Interfaces (IInteractable), Enums
│       ├── Data/                       # Clases C# de definición de ScriptableObjects
│       ├── Player/                     # PlayerController, TemperatureSystem, HeadlampController
│       ├── World/                      # HarvestableNode, HeatSourceNode, FrozenChest
│       ├── Economy/                    # InventoryAndShop, CentralHopper
│       ├── Automation/                 # MechanicalElfExtractor, ConveyorBelt
│       ├── AI/                         # DefectiveToyAI, FrostGolemAI
│       └── UI/                         # ThermalHUDView, ShopView
├── Settings/                           # Perfiles URP (PC_RPAsset, Mobile_RPAsset, Volume Profiles)
└── InputSystem_Actions.inputactions    # Mapa de acciones del Unity Input System
```

---

## 2. Reglas de Organización de Archivos

1. **Separación Estricta entre Código (`Scripts/Data/`) e Instancias (`Data/`):**
   * El archivo `.cs` que define la clase `ResourceNodeDataSO : ScriptableObject` vive en `Assets/_Project/Scripts/Data/`.
   * Los archivos `.asset` creados en el editor (ej. `SO_Resource_CandyCrystal.asset`) viven en `Assets/_Project/Data/Resources/`.
2. **Modularidad por Prefabs:**
   * Ningún objeto interactivo (veta mineral, pino cristalizado, guirnalda, tolva o enemigo) debe crearse únicamente dentro de la jerarquía de la escena. Todo debe existir como un `.prefab` dentro de `Assets/_Project/Prefabs/` para que los cambios se propaguen automáticamente a toda la escena.
