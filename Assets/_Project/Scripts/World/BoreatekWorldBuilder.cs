using System.Collections.Generic;
using Boreatek.AI;
using Boreatek.Core;
using Boreatek.Data;
using Boreatek.Economy;
using Boreatek.Player;
using Boreatek.UI;
using Boreatek.World;
using UnityEngine;

namespace Boreatek.World
{
    /// <summary>
    /// Ensambla y genera proceduralmente el mundo 3D estilizado / low-poly de BOREATEK:
    /// - Aldea Industrial de Santa (Torres Gemelas, Forja Central Circular, Tolva Central y Terminal de Tienda).
    /// - Superficie Procedural por Semilla: Tundra Exterior (Pinos Cristalizados y Trineos),
    ///   Bosque de Caramelos Gigantes y El Cementerio de Trineos (con Cofres Congelados).
    /// - Red Subterránea en 3 Niveles: Túneles de Ventilación con Guirnaldas, Cuevas de Hielo Nivel 1,
    ///   La Geoda de Hielo Azul Nivel 2 y El Abismo del Núcleo Luminoso Nivel 3.
    /// - Protagonista Duende Ingeniero 3D estilizado y patrullas de Soldados de Juguete Defectuosos y Gólems.
    /// </summary>
    public class BoreatekWorldBuilder : MonoBehaviour
    {
        [Header("Procedural Generation Settings")]
        [Tooltip("Semilla procedural para la distribución de pinos, árboles de caramelo, vetas y cofres congelados.")]
        [SerializeField] private int _worldSeed = 20261224;

        [SerializeField] private int _crystallizedPineCount = 45;
        [SerializeField] private int _giantCandyTreeCount = 26;
        [SerializeField] private int _sleighWreckAndFrozenChestCount = 12;

        private Shader _urpLitShader;
        private readonly Dictionary<ResourceType, ResourceDataSO> _resources = new Dictionary<ResourceType, ResourceDataSO>();
        private readonly Dictionary<string, HarvestableNodeConfigSO> _nodeConfigs = new Dictionary<string, HarvestableNodeConfigSO>();
        private BoreatekGameSession _session;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapOnSceneLoad()
        {
            if (FindAnyObjectByType<BoreatekWorldBuilder>() == null)
            {
                GameObject bootstrap = new GameObject("Boreatek_WorldManager");
                bootstrap.AddComponent<BoreatekWorldBuilder>();
            }
        }

        private void Awake()
        {
            BuildOrRefreshWorld();
        }

        /// <summary>
        /// Construye o refresca el mundo 3D completo (compatible tanto con Edit Mode para vista previa en escena como con Play Mode).
        /// </summary>
        [ContextMenu("Build / Refresh 3D World in Scene")]
        public void BuildOrRefreshWorld()
        {
            _urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (_urpLitShader == null)
            {
                _urpLitShader = Shader.Find("Standard");
            }

            CleanupExistingGeneratedObjects();
            InitializeResourceAndNodeDefinitions();
            EnsureDayNightCycleManager();
            BuildEntireBoreatekWorld();
            PlayerController player = EnsureStylizedElfEngineerPlayer();
            BoreatekHUDAndShopUI ui = EnsureHUDAndShopUI(player);
            if (Application.isPlaying)
            {
                if (_session == null)
                {
                    _session = gameObject.AddComponent<BoreatekGameSession>();
                }
                _session.Initialize(player, ui);
            }
        }

        private void CleanupExistingGeneratedObjects()
        {
            GameObject oldPlayer = GameObject.Find("Player_ElfEngineer");
            if (oldPlayer != null)
            {
                // Desemparentar cualquier cámara antes de destruir el jugador previo para nunca perder la Main Camera
                Camera[] attachedCameras = oldPlayer.GetComponentsInChildren<Camera>(true);
                for (int i = 0; i < attachedCameras.Length; i++)
                {
                    if (attachedCameras[i] != null)
                    {
                        attachedCameras[i].transform.SetParent(null, true);
                        attachedCameras[i].gameObject.SetActive(true);
                        attachedCameras[i].enabled = true;
                    }
                }

                oldPlayer.name = "Player_ElfEngineer_Disposing";
                oldPlayer.SetActive(false);

                if (Application.isPlaying) Destroy(oldPlayer);
                else DestroyImmediate(oldPlayer);
            }

            GameObject oldWorld = GameObject.Find("Boreatek_StylizedWorld_3D");
            if (oldWorld != null)
            {
                oldWorld.name = "Boreatek_StylizedWorld_3D_Disposing";
                oldWorld.SetActive(false);

                if (Application.isPlaying) Destroy(oldWorld);
                else DestroyImmediate(oldWorld);
            }

            GameObject oldRespawn = GameObject.Find("Village_SafeRespawnPoint");
            if (oldRespawn != null)
            {
                oldRespawn.name = "Village_SafeRespawnPoint_Disposing";
                oldRespawn.SetActive(false);

                if (Application.isPlaying) Destroy(oldRespawn);
                else DestroyImmediate(oldRespawn);
            }
        }

        private void InitializeResourceAndNodeDefinitions()
        {
            CreateResource(ResourceType.CrystallizedWood, "Madera Cristalizada",
                "Madera densa de los Pinos Cristalizados de la tundra exterior.", 6, new Color(0.79f, 0.91f, 1.0f));
            CreateResource(ResourceType.PolarScrap, "Chatarra Polar",
                "Engranajes y placas rescatadas de trineos estrellados.", 12, new Color(0.69f, 0.49f, 0.31f));
            CreateResource(ResourceType.BlackCoal, "Carbón Negro",
                "Combustible básico para los hornos y calderas de la aldea.", 15, new Color(0.20f, 0.22f, 0.25f));
            CreateResource(ResourceType.CandyCrystal, "Cristal de Caramelo",
                "Mineral energético rojo brillante que alimenta herramientas y autómatas.", 28, new Color(0.90f, 0.22f, 0.27f));
            CreateResource(ResourceType.PolarIron, "Hierro Polar",
                "Metal azulado templado bajo cero para herramientas y maquinaria avanzada.", 45, new Color(0.45f, 0.58f, 0.72f));
            CreateResource(ResourceType.DeepBlueIce, "Hielo Azul Profundo",
                "Cristal glacial hipercomprimido de Nivel 2 para enfriar reactores.", 80, new Color(0.37f, 0.66f, 0.83f));
            CreateResource(ResourceType.FrozenChestSupply, "Reliquia de Cofre Congelado",
                "Suministros de alto valor rescatados tras derretir un Cofre Congelado.", 135, new Color(1.0f, 0.78f, 0.22f));
            CreateResource(ResourceType.LuminousCore, "Núcleo Luminoso Abismal",
                "Esfera geotérmica de magma festivo del Nivel 3 para La Gran Nochebuena.", 220, new Color(1.0f, 0.38f, 0.05f), insured: true);

            CreateNodeConfig("pine", "Pino Cristalizado", BiomeType.OuterTundra,
                ResourceType.CrystallizedWood, tier: 1, hits: 3, minYield: 2, maxYield: 4);
            CreateNodeConfig("scrap_sleigh", "Trineo Estrellado", BiomeType.OuterTundra,
                ResourceType.PolarScrap, tier: 1, hits: 3, minYield: 2, maxYield: 3);
            CreateNodeConfig("candy_tree", "Árbol de Caramelo Gigante", BiomeType.GiantCandyForest,
                ResourceType.CandyCrystal, tier: 1, hits: 5, minYield: 2, maxYield: 4);
            CreateNodeConfig("frozen_chest", "Cofre Congelado Polar", BiomeType.SleighGraveyard,
                ResourceType.FrozenChestSupply, tier: 1, hits: 2, minYield: 1, maxYield: 2, requiresHeat: true, meltSeconds: 12f);
            CreateNodeConfig("coal_vein", "Veta de Carbón Negro", BiomeType.IceCavesLevel1,
                ResourceType.BlackCoal, tier: 1, hits: 4, minYield: 2, maxYield: 4);
            CreateNodeConfig("cave_candy_vein", "Veta de Cristal de Caramelo", BiomeType.IceCavesLevel1,
                ResourceType.CandyCrystal, tier: 1, hits: 4, minYield: 2, maxYield: 3);
            CreateNodeConfig("polar_iron_vein", "Veta de Hierro Polar", BiomeType.IceCavesLevel1,
                ResourceType.PolarIron, tier: 2, hits: 6, minYield: 2, maxYield: 4);
            CreateNodeConfig("blue_ice_geode", "Cristal de Hielo Azul Profundo", BiomeType.BlueIceGeodeLevel2,
                ResourceType.DeepBlueIce, tier: 2, hits: 7, minYield: 2, maxYield: 3);
            CreateNodeConfig("luminous_core_vein", "Núcleo Luminoso Geotérmico", BiomeType.LuminousCoreAbyssLevel3,
                ResourceType.LuminousCore, tier: 3, hits: 10, minYield: 1, maxYield: 1);
        }

        private void CreateResource(ResourceType type, string name, string desc, int coins, Color color, bool insured = false)
        {
            ResourceDataSO so = ScriptableObject.CreateInstance<ResourceDataSO>();
            so.InitializeRuntime(type, name, desc, coins, color, insured);
            _resources[type] = so;
        }

        private void CreateNodeConfig(
            string key,
            string name,
            BiomeType biome,
            ResourceType outputType,
            int tier,
            int hits,
            int minYield,
            int maxYield,
            bool requiresHeat = false,
            float meltSeconds = 12f)
        {
            HarvestableNodeConfigSO cfg = ScriptableObject.CreateInstance<HarvestableNodeConfigSO>();
            cfg.InitializeRuntime(name, biome, _resources[outputType], tier, hits, minYield, maxYield, requiresHeat, meltSeconds);
            _nodeConfigs[key] = cfg;
        }

        private void EnsureDayNightCycleManager()
        {
            if (FindAnyObjectByType<DayNightCycleManager>() == null)
            {
                gameObject.AddComponent<DayNightCycleManager>();
            }
        }

        private void BuildEntireBoreatekWorld()
        {
            Random.InitState(_worldSeed);

            Transform worldRoot = new GameObject("Boreatek_StylizedWorld_3D").transform;

            BuildArcticSurfaceTerrain(worldRoot);
            BuildSantaIndustrialVillageHub(worldRoot);
            BuildProceduralOuterTundra(worldRoot);
            BuildProceduralGiantCandyForest(worldRoot);
            BuildProceduralSleighGraveyard(worldRoot);
            BuildUndergroundExcavationNetwork(worldRoot);
        }

        private void BuildArcticSurfaceTerrain(Transform parent)
        {
            // Suelo ártico con apertura central norte para el túnel subterráneo (Z = 16 a 24)
            CreateBox(parent, "SnowTerrain_South", new Vector3(0f, -0.5f, -55f), new Vector3(240f, 1f, 138f),
                new Color(0.82f, 0.92f, 0.99f), roughness: 0.25f);
            CreateBox(parent, "SnowTerrain_North", new Vector3(0f, -0.5f, 75f), new Vector3(240f, 1f, 100f),
                new Color(0.82f, 0.92f, 0.99f), roughness: 0.25f);
            CreateBox(parent, "SnowTerrain_West", new Vector3(-62f, -0.5f, 19.5f), new Vector3(116f, 1f, 11f),
                new Color(0.82f, 0.92f, 0.99f), roughness: 0.25f);
            CreateBox(parent, "SnowTerrain_East", new Vector3(62f, -0.5f, 19.5f), new Vector3(116f, 1f, 11f),
                new Color(0.82f, 0.92f, 0.99f), roughness: 0.25f);
        }

        /// <summary>
        /// Construye la Aldea Industrial de Santa (paisaje_nevado_futurista_industrial..jpg):
        /// Dos torres gemelas con anillos luminosos, la gran Forja Central circular, la Tolva Central y la Tienda.
        /// </summary>
        private void BuildSantaIndustrialVillageHub(Transform parent)
        {
            Transform village = new GameObject("Biome_01_SantaIndustrialVillage").transform;
            village.SetParent(parent, false);

            // Plataforma circular de piedra y acero de la Aldea
            CreateCylinder(village, "Village_ThermalPlatform", new Vector3(0f, 0.08f, 0f), new Vector3(28f, 0.08f, 28f),
                new Color(0.24f, 0.27f, 0.32f));

            // Torres Gemelas Industriales con anillos superiores luminosos ámbar/rojo
            BuildIndustrialTower(village, "TwinTower_West", new Vector3(-8.5f, 0f, 8.5f));
            BuildIndustrialTower(village, "TwinTower_East", new Vector3(8.5f, 0f, 8.5f));

            // La Gran Forja Central Circular entre las dos torres
            Transform forgeRoot = new GameObject("Central_Circular_Forge").transform;
            forgeRoot.SetParent(village, false);
            forgeRoot.localPosition = new Vector3(0f, 0f, 8.5f);

            CreateCylinder(forgeRoot, "Forge_Pedestal", new Vector3(0f, 0.7f, 0f), new Vector3(7.5f, 0.7f, 7.5f),
                new Color(0.18f, 0.20f, 0.24f));

            GameObject outerWheel = CreateCylinder(forgeRoot, "Forge_OuterRing", new Vector3(0f, 4.8f, 0f),
                new Vector3(8.2f, 0.65f, 8.2f), new Color(0.32f, 0.25f, 0.22f));
            outerWheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            GameObject innerCore = CreateCylinder(forgeRoot, "Forge_EmissiveCore", new Vector3(0f, 4.8f, 0f),
                new Vector3(6.2f, 0.72f, 6.2f), new Color(1.0f, 0.52f, 0.10f), emissionIntensity: 2.8f);
            innerCore.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // Luz cálida principal y Burbuja Térmica de la Aldea Industrial
            GameObject forgeLightObj = new GameObject("Forge_WarmLightAndSafeZone");
            forgeLightObj.transform.SetParent(forgeRoot, false);
            forgeLightObj.transform.localPosition = new Vector3(0f, 3.2f, -3.0f);
            Light forgeLight = forgeLightObj.AddComponent<Light>();
            forgeLight.type = LightType.Point;
            forgeLight.color = new Color(1.0f, 0.58f, 0.16f);
            forgeLight.intensity = 6.5f;
            forgeLight.range = 32f;

            SphereCollider safeZoneTrigger = forgeLightObj.AddComponent<SphereCollider>();
            safeZoneTrigger.isTrigger = true;
            safeZoneTrigger.radius = 20f;
            HeatSourceNode villageHeat = forgeLightObj.AddComponent<HeatSourceNode>();
            villageHeat.ConfigureRuntime("Aldea Industrial de Santa (Forja Central)", 15f, 20f, canMeltChests: true, isVillageHub: true);

            // Tolva Central de Recepción de Minerales (Interactuable con 'E')
            GameObject hopperRoot = CreateBox(village, "Central_Hopper_Reception", new Vector3(-3.8f, 0.9f, 3.5f),
                new Vector3(2.4f, 1.8f, 2.2f), new Color(0.69f, 0.49f, 0.31f));
            CreateBox(hopperRoot.transform, "Hopper_GlowChute", new Vector3(0f, 0.55f, 0f),
                new Vector3(0.85f, 0.2f, 0.85f), new Color(1.0f, 0.62f, 0.11f), emissionIntensity: 2.2f);
            CentralHopper hopper = hopperRoot.AddComponent<CentralHopper>();
            hopper.ConfigureRuntime(isShopTerminal: false);

            // Terminal de la Tienda del Taller (Interactuable con 'E' o 'Tab')
            GameObject shopRoot = CreateBox(village, "Workshop_Shop_Terminal", new Vector3(3.8f, 1.0f, 3.5f),
                new Vector3(2.2f, 2.0f, 1.8f), new Color(0.29f, 0.36f, 0.31f));
            CreateBox(shopRoot.transform, "Shop_Screen_Neon", new Vector3(0f, 0.25f, -0.52f),
                new Vector3(0.8f, 0.45f, 0.1f), new Color(0.30f, 0.85f, 0.65f), emissionIntensity: 2.2f);
            CentralHopper shopTerminal = shopRoot.AddComponent<CentralHopper>();
            shopTerminal.ConfigureRuntime(isShopTerminal: true);
        }

        private void BuildIndustrialTower(Transform parent, string name, Vector3 position)
        {
            Transform tower = new GameObject(name).transform;
            tower.SetParent(parent, false);
            tower.localPosition = position;

            CreateCylinder(tower, "Tower_Shaft", new Vector3(0f, 7.5f, 0f), new Vector3(3.6f, 7.5f, 3.6f),
                new Color(0.25f, 0.28f, 0.33f));
            CreateCylinder(tower, "Tower_CrownRing", new Vector3(0f, 15.2f, 0f), new Vector3(5.2f, 0.9f, 5.2f),
                new Color(1.0f, 0.45f, 0.12f), emissionIntensity: 2.6f);
            CreateCylinder(tower, "Tower_RoofCone", new Vector3(0f, 16.6f, 0f), new Vector3(3.8f, 0.6f, 3.8f),
                new Color(0.20f, 0.22f, 0.26f));
        }

        /// <summary>
        /// Genera proceduralmente la Tundra Exterior Inicial (tundra_helada_bajo_aurora_boreal.jpg)
        /// con Pinos Cristalizados y Trineos Estrellados.
        /// </summary>
        private void BuildProceduralOuterTundra(Transform parent)
        {
            Transform tundra = new GameObject("Biome_02_OuterTundra").transform;
            tundra.SetParent(parent, false);

            for (int i = 0; i < _crystallizedPineCount; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float radius = Random.Range(22f, 65f);
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                // Evitar obstruir la rampa del túnel norte o el bosque de caramelos al este
                if (Mathf.Abs(pos.x) < 7f && pos.z > 10f && pos.z < 30f) continue;
                if (pos.x > 34f) continue;

                SpawnCrystallizedPineNode(tundra, pos);
            }

            // Trineos estrellados cercanos en la tundra
            for (int i = 0; i < 6; i++)
            {
                Vector3 pos = new Vector3(Random.Range(-45f, 25f), 0f, Random.Range(-30f, 45f));
                if (pos.magnitude < 22f) continue;
                SpawnCrashedSleighScrapNode(tundra, pos);
            }
        }

        /// <summary>
        /// Genera proceduralmente el Bosque de Caramelos Gigantes (bosque_surrealista_y_brillante.jpg)
        /// al Este de la aldea (X > 34m).
        /// </summary>
        private void BuildProceduralGiantCandyForest(Transform parent)
        {
            Transform candyForest = new GameObject("Biome_03_GiantCandyForest").transform;
            candyForest.SetParent(parent, false);

            for (int i = 0; i < _giantCandyTreeCount; i++)
            {
                Vector3 pos = new Vector3(Random.Range(35f, 88f), 0f, Random.Range(-45f, 45f));
                SpawnGiantCandyCrystalTreeNode(candyForest, pos);
            }
        }

        /// <summary>
        /// Genera proceduralmente El Cementerio de Trineos (paramo_desolado_de_nieve_profunda.jpg)
        /// al Sur (Z < -36m), con Cofres Congelados y patrullas de Soldados de Juguete Defectuosos.
        /// </summary>
        private void BuildProceduralSleighGraveyard(Transform parent)
        {
            Transform graveyard = new GameObject("Biome_04_SleighGraveyard").transform;
            graveyard.SetParent(parent, false);

            for (int i = 0; i < _sleighWreckAndFrozenChestCount; i++)
            {
                Vector3 pos = new Vector3(Random.Range(-55f, 45f), 0f, Random.Range(-88f, -38f));
                SpawnCrashedSleighScrapNode(graveyard, pos + new Vector3(2.2f, 0f, 0f));
                SpawnFrozenChestNode(graveyard, pos);

                if (i % 2 == 0)
                {
                    SpawnDefectiveLeadSoldierEnemy(graveyard, pos + new Vector3(-3.5f, 0f, 2.5f));
                }
            }
        }

        /// <summary>
        /// Construye la red subterránea continua conectada desde la entrada norte de la Aldea (Z = 16m):
        /// - Túneles de Ventilación (tunel_minero.jpg)
        /// - Cuevas de Nivel 1 (cueva_de_hielo.jpg)
        /// - La Geoda de Hielo Azul Nivel 2 (caverna_subterranea_profunda.jpg)
        /// - El Abismo del Núcleo Luminoso Nivel 3 (caverna_abismal.jpg)
        /// </summary>
        private void BuildUndergroundExcavationNetwork(Transform parent)
        {
            Transform underground = new GameObject("Underground_Excavation_Levels").transform;
            underground.SetParent(parent, false);

            // 1. Entrada y Rampa por los Túneles de Ventilación (Z = 15m a 45m, bajando a Y = -10m)
            BuildVentilationTunnelRamp(underground, new Vector3(0f, 0f, 15f), new Vector3(0f, -10f, 45f),
                "Tunnel_01_VentilationTransition", new Color(0.28f, 0.25f, 0.23f));

            // 2. Sala de Cuevas de Nivel 1 (Centro en 0, -10, 58)
            Vector3 cave1Center = new Vector3(0f, -10f, 58f);
            BuildCavernChamber(underground, "Cavern_Level1_CobaltIceCaves", cave1Center, new Vector3(26f, 8f, 26f),
                new Color(0.11f, 0.29f, 0.40f), new Color(0.18f, 0.45f, 0.65f));

            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI * 0.33f;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * 8.5f, 0f, Mathf.Sin(angle) * 8.5f);
                SpawnMineralVeinNode(underground, cave1Center + offset, "coal_vein", new Color(0.18f, 0.20f, 0.23f), 0f);
                SpawnMineralVeinNode(underground, cave1Center + offset * 0.68f, "cave_candy_vein", new Color(0.90f, 0.22f, 0.27f), 2.2f);
                SpawnMineralVeinNode(underground, cave1Center + offset * 1.12f, "polar_iron_vein", new Color(0.45f, 0.58f, 0.72f), 0.5f);
            }
            SpawnDefectiveLeadSoldierEnemy(underground, cave1Center + new Vector3(4f, 0f, 4f));

            // 3. Rampa de Nivel 1 a Nivel 2 (Z = 71m a 101m, bajando a Y = -20m)
            BuildVentilationTunnelRamp(underground, new Vector3(0f, -10f, 71f), new Vector3(0f, -20f, 101f),
                "Tunnel_02_ToBlueIceGeode", new Color(0.16f, 0.38f, 0.55f));

            // 4. Sala de La Geoda de Hielo Azul - Nivel 2 (Centro en 0, -20, 114)
            Vector3 geode2Center = new Vector3(0f, -20f, 114f);
            BuildCavernChamber(underground, "Cavern_Level2_BlueIceGeode", geode2Center, new Vector3(28f, 9f, 26f),
                new Color(0.22f, 0.52f, 0.72f), new Color(0.37f, 0.66f, 0.83f));

            for (int i = 0; i < 7; i++)
            {
                float angle = i * (Mathf.PI * 2f / 7f);
                Vector3 offset = new Vector3(Mathf.Cos(angle) * 9.0f, 0f, Mathf.Sin(angle) * 9.0f);
                SpawnMineralVeinNode(underground, geode2Center + offset, "blue_ice_geode", new Color(0.37f, 0.66f, 0.83f), 2.0f);
                SpawnMineralVeinNode(underground, geode2Center + offset * 0.6f, "polar_iron_vein", new Color(0.48f, 0.62f, 0.78f), 0.8f);
            }
            SpawnFrostGolemEnemy(underground, geode2Center + new Vector3(0f, 0f, 3f));

            // 5. Rampa de Nivel 2 a Nivel 3 (Z = 127m a 157m, bajando a Y = -32m)
            BuildVentilationTunnelRamp(underground, new Vector3(0f, -20f, 127f), new Vector3(0f, -32f, 157f),
                "Tunnel_03_ToAbyssCore", new Color(0.18f, 0.16f, 0.18f));

            // 6. Sala de El Abismo del Núcleo Luminoso - Nivel 3 (Centro en 0, -32, 172)
            Vector3 abyss3Center = new Vector3(0f, -32f, 172f);
            BuildCavernChamber(underground, "Cavern_Level3_LuminousCoreAbyss", abyss3Center, new Vector3(30f, 11f, 30f),
                new Color(0.12f, 0.11f, 0.13f), new Color(1.0f, 0.33f, 0.0f));

            // Fisura central de Magma Festivo incandescente
            CreateBox(underground, "Abyss_FestiveMagmaFissure", abyss3Center + new Vector3(0f, 4.5f, 14.2f),
                new Vector3(2.2f, 10f, 0.6f), new Color(1.0f, 0.38f, 0.02f), emissionIntensity: 3.5f);
            CreateCylinder(underground, "Abyss_MagmaPool", abyss3Center + new Vector3(0f, 0.08f, 6f),
                new Vector3(6.5f, 0.08f, 6.5f), new Color(1.0f, 0.42f, 0.05f), emissionIntensity: 3.2f);

            // Los 4 Núcleos Luminosos de Nivel 3
            Vector3[] coreOffsets =
            {
                new Vector3(-5.5f, 0f, 4f),
                new Vector3(5.5f, 0f, 4f),
                new Vector3(-4.5f, 0f, -4f),
                new Vector3(4.5f, 0f, -4f)
            };
            for (int i = 0; i < coreOffsets.Length; i++)
            {
                SpawnMineralVeinNode(underground, abyss3Center + coreOffsets[i], "luminous_core_vein",
                    new Color(1.0f, 0.42f, 0.05f), 3.4f);
            }
            SpawnFrostGolemEnemy(underground, abyss3Center + new Vector3(0f, 0f, -2f));
        }

        private void BuildVentilationTunnelRamp(
            Transform parent,
            Vector3 start,
            Vector3 end,
            string name,
            Color wallColor)
        {
            Transform tunnel = new GameObject(name).transform;
            tunnel.SetParent(parent, false);

            Vector3 delta = end - start;
            float length = delta.magnitude;
            Vector3 mid = (start + end) * 0.5f;
            Quaternion rot = Quaternion.LookRotation(delta.normalized);

            // Rampa caminable
            GameObject ramp = CreateBox(tunnel, "Tunnel_FloorRamp", mid, new Vector3(7.5f, 0.5f, length + 1.5f), wallColor);
            ramp.transform.rotation = rot;

            // Paredes laterales y techo con tuberías de cobre
            GameObject leftWall = CreateBox(tunnel, "Tunnel_WallLeft", mid + Vector3.left * 3.8f + Vector3.up * 2.5f,
                new Vector3(0.6f, 5.5f, length), wallColor * 0.85f);
            leftWall.transform.rotation = rot;

            GameObject rightWall = CreateBox(tunnel, "Tunnel_WallRight", mid + Vector3.right * 3.8f + Vector3.up * 2.5f,
                new Vector3(0.6f, 5.5f, length), wallColor * 0.85f);
            rightWall.transform.rotation = rot;

            GameObject ceiling = CreateBox(tunnel, "Tunnel_Ceiling", mid + Vector3.up * 5.2f,
                new Vector3(8.2f, 0.6f, length), wallColor * 0.75f);
            ceiling.transform.rotation = rot;

            // Guirnalda luminosa térmica a mitad del túnel para guiar al jugador
            GameObject stringLight = new GameObject("Tunnel_FestiveStringLight");
            stringLight.transform.SetParent(tunnel, false);
            stringLight.transform.position = mid + Vector3.up * 2.2f;
            Light pt = stringLight.AddComponent<Light>();
            pt.type = LightType.Point;
            pt.color = new Color(1.0f, 0.58f, 0.20f);
            pt.intensity = 2.8f;
            pt.range = 12f;

            SphereCollider heatCol = stringLight.AddComponent<SphereCollider>();
            heatCol.isTrigger = true;
            heatCol.radius = 6.5f;
            HeatSourceNode heat = stringLight.AddComponent<HeatSourceNode>();
            heat.ConfigureRuntime("Guirnalda de Túnel de Ventilación", 5.5f, 6.5f, canMeltChests: false);
        }

        private void BuildCavernChamber(
            Transform parent,
            string name,
            Vector3 center,
            Vector3 size,
            Color rockColor,
            Color crystalLightColor)
        {
            Transform chamber = new GameObject(name).transform;
            chamber.SetParent(parent, false);
            chamber.position = center;

            // Suelo y techo
            CreateBox(chamber, "Chamber_Floor", new Vector3(0f, -0.4f, 0f), new Vector3(size.x, 0.8f, size.z), rockColor);
            CreateBox(chamber, "Chamber_Ceiling", new Vector3(0f, size.y, 0f), new Vector3(size.x, 0.8f, size.z), rockColor * 0.8f);

            // Muros laterales Este y Oeste
            CreateBox(chamber, "Chamber_WallWest", new Vector3(-size.x * 0.5f, size.y * 0.5f, 0f),
                new Vector3(1f, size.y, size.z), rockColor * 0.9f);
            CreateBox(chamber, "Chamber_WallEast", new Vector3(size.x * 0.5f, size.y * 0.5f, 0f),
                new Vector3(1f, size.y, size.z), rockColor * 0.9f);

            // Luz ambiental de la cámara subterránea
            GameObject lightObj = new GameObject("Chamber_AmbientCrystalLight");
            lightObj.transform.SetParent(chamber, false);
            lightObj.transform.localPosition = new Vector3(0f, size.y - 1.5f, 0f);
            Light lt = lightObj.AddComponent<Light>();
            lt.type = LightType.Point;
            lt.color = crystalLightColor;
            lt.intensity = 4.2f;
            lt.range = size.x * 1.15f;
        }

        private void SpawnCrystallizedPineNode(Transform parent, Vector3 pos)
        {
            GameObject pine = new GameObject("Node_CrystallizedPine");
            pine.transform.SetParent(parent, false);
            pine.transform.position = pos;

            // Tronco y 3 capas low-poly escarchadas
            CreateCylinder(pine.transform, "Trunk", new Vector3(0f, 0.9f, 0f), new Vector3(0.55f, 0.9f, 0.55f),
                new Color(0.35f, 0.25f, 0.20f));
            CreateBox(pine.transform, "Foliage_Lower", new Vector3(0f, 2.1f, 0f), new Vector3(2.2f, 1.2f, 2.2f),
                new Color(0.62f, 0.84f, 0.96f));
            CreateBox(pine.transform, "Foliage_Mid", new Vector3(0f, 3.2f, 0f), new Vector3(1.6f, 1.1f, 1.6f),
                new Color(0.74f, 0.91f, 1.0f));
            CreateBox(pine.transform, "Foliage_Top", new Vector3(0f, 4.2f, 0f), new Vector3(0.95f, 1.0f, 0.95f),
                new Color(0.88f, 0.96f, 1.0f));

            BoxCollider col = pine.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 2.2f, 0f);
            col.size = new Vector3(2.0f, 4.4f, 2.0f);

            HarvestableNode node = pine.AddComponent<HarvestableNode>();
            node.InitializeRuntime(_nodeConfigs["pine"]);
        }

        private void SpawnGiantCandyCrystalTreeNode(Transform parent, Vector3 pos)
        {
            GameObject tree = new GameObject("Node_GiantCandyCrystalTree");
            tree.transform.SetParent(parent, false);
            tree.transform.position = pos;

            // Tronco rojo carmesí brillante y ramificaciones blancas/rojas (bosque_surrealista_y_brillante.jpg)
            CreateCylinder(tree.transform, "CandyTrunk_Red", new Vector3(0f, 1.3f, 0f), new Vector3(1.1f, 1.3f, 1.1f),
                new Color(0.86f, 0.14f, 0.22f), emissionIntensity: 1.4f);
            CreateCylinder(tree.transform, "CandyUpper_Ivory", new Vector3(0f, 3.1f, 0f), new Vector3(0.85f, 0.9f, 0.85f),
                new Color(0.98f, 0.94f, 0.90f));
            CreateBox(tree.transform, "CandyCanopy_GlowingRed", new Vector3(0f, 4.5f, 0f), new Vector3(3.2f, 1.4f, 3.2f),
                new Color(0.94f, 0.18f, 0.26f), emissionIntensity: 2.0f);

            BoxCollider col = tree.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 2.4f, 0f);
            col.size = new Vector3(2.4f, 4.8f, 2.4f);

            HarvestableNode node = tree.AddComponent<HarvestableNode>();
            node.InitializeRuntime(_nodeConfigs["candy_tree"]);
        }

        private void SpawnCrashedSleighScrapNode(Transform parent, Vector3 pos)
        {
            GameObject sleigh = new GameObject("Node_CrashedCargoSleigh");
            sleigh.transform.SetParent(parent, false);
            sleigh.transform.position = pos;
            sleigh.transform.rotation = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(0f, 360f), Random.Range(-8f, 8f));

            CreateBox(sleigh.transform, "Sleigh_HullRed", new Vector3(0f, 0.65f, 0f), new Vector3(1.8f, 1.1f, 2.8f),
                new Color(0.58f, 0.14f, 0.18f));
            CreateBox(sleigh.transform, "Sleigh_BrassTrim", new Vector3(0f, 1.25f, -0.5f), new Vector3(1.9f, 0.35f, 1.4f),
                new Color(0.75f, 0.56f, 0.28f));

            BoxCollider col = sleigh.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.7f, 0f);
            col.size = new Vector3(2.0f, 1.5f, 3.0f);

            HarvestableNode node = sleigh.AddComponent<HarvestableNode>();
            node.InitializeRuntime(_nodeConfigs["scrap_sleigh"]);
        }

        private void SpawnFrozenChestNode(Transform parent, Vector3 pos)
        {
            GameObject chestRoot = new GameObject("Node_FrozenChest");
            chestRoot.transform.SetParent(parent, false);
            chestRoot.transform.position = pos;

            // Cofre industrial dorado interior
            CreateBox(chestRoot.transform, "Chest_CoreBox", new Vector3(0f, 0.55f, 0f), new Vector3(1.3f, 1.0f, 1.0f),
                new Color(0.85f, 0.62f, 0.18f), emissionIntensity: 1.5f);

            // Bloque de hielo sólido que lo envuelve y se reduce al calentarse
            GameObject iceShell = CreateBox(chestRoot.transform, "Frozen_IceBlockShell", new Vector3(0f, 0.75f, 0f),
                new Vector3(1.9f, 1.6f, 1.6f), new Color(0.38f, 0.72f, 0.94f), emissionIntensity: 0.7f);

            BoxCollider col = chestRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.75f, 0f);
            col.size = new Vector3(1.9f, 1.6f, 1.6f);

            HarvestableNode node = chestRoot.AddComponent<HarvestableNode>();
            node.InitializeRuntime(_nodeConfigs["frozen_chest"], iceShell);
        }

        private void SpawnMineralVeinNode(
            Transform parent,
            Vector3 pos,
            string configKey,
            Color crystalColor,
            float emissionIntensity)
        {
            GameObject vein = new GameObject($"Node_{configKey}");
            vein.transform.SetParent(parent, false);
            vein.transform.position = pos;

            CreateBox(vein.transform, "RockBase", new Vector3(0f, 0.45f, 0f), new Vector3(1.6f, 0.9f, 1.6f),
                new Color(0.22f, 0.25f, 0.30f));

            GameObject shard1 = CreateBox(vein.transform, "CrystalShard_A", new Vector3(0f, 1.15f, 0f),
                new Vector3(0.75f, 1.4f, 0.75f), crystalColor, emissionIntensity);
            shard1.transform.localRotation = Quaternion.Euler(14f, 25f, -10f);

            GameObject shard2 = CreateBox(vein.transform, "CrystalShard_B", new Vector3(0.35f, 0.95f, -0.25f),
                new Vector3(0.55f, 1.1f, 0.55f), crystalColor, emissionIntensity);
            shard2.transform.localRotation = Quaternion.Euler(-18f, -40f, 15f);

            BoxCollider col = vein.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.size = new Vector3(1.8f, 1.9f, 1.8f);

            HarvestableNode node = vein.AddComponent<HarvestableNode>();
            node.InitializeRuntime(_nodeConfigs[configKey]);
        }

        /// <summary>
        /// Construye el modelo 3D low-poly del Soldado de Juguete Defectuoso (concepto_juguete_de_plomo.jpg).
        /// </summary>
        private void SpawnDefectiveLeadSoldierEnemy(Transform parent, Vector3 pos)
        {
            GameObject enemy = new GameObject("Enemy_DefectiveLeadSoldier");
            enemy.transform.SetParent(parent, false);
            enemy.transform.position = pos;

            // Torso oxidado verde/rojo y cabeza esférica con ojos ámbar brillantes
            CreateBox(enemy.transform, "RustedTorso", new Vector3(0f, 0.95f, 0f), new Vector3(0.95f, 1.1f, 0.65f),
                new Color(0.28f, 0.33f, 0.27f));
            CreateSphere(enemy.transform, "SphericalLeadHead", new Vector3(0f, 1.8f, 0f), Vector3.one * 0.72f,
                new Color(0.58f, 0.46f, 0.32f));
            CreateSphere(enemy.transform, "AmberEye_L", new Vector3(-0.16f, 1.84f, 0.32f), Vector3.one * 0.14f,
                new Color(1.0f, 0.65f, 0.10f), emissionIntensity: 3.0f);
            CreateSphere(enemy.transform, "AmberEye_R", new Vector3(0.16f, 1.84f, 0.32f), Vector3.one * 0.14f,
                new Color(1.0f, 0.65f, 0.10f), emissionIntensity: 3.0f);

            CapsuleCollider col = enemy.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1.1f, 0f);
            col.radius = 0.55f;
            col.height = 2.2f;

            DefectiveToyAI ai = enemy.AddComponent<DefectiveToyAI>();
            ai.InitializeRuntime("Soldado de Juguete Defectuoso", isFrostGolem: false, maxHp: 75f, speed: 4.4f);
        }

        private void SpawnFrostGolemEnemy(Transform parent, Vector3 pos)
        {
            GameObject golem = new GameObject("Enemy_FrostGolemGuardian");
            golem.transform.SetParent(parent, false);
            golem.transform.position = pos;

            CreateBox(golem.transform, "GolemIceTorso", new Vector3(0f, 1.5f, 0f), new Vector3(1.8f, 2.0f, 1.3f),
                new Color(0.32f, 0.64f, 0.86f), emissionIntensity: 1.2f);
            CreateBox(golem.transform, "GolemHead", new Vector3(0f, 2.8f, 0f), new Vector3(1.0f, 0.9f, 1.0f),
                new Color(0.55f, 0.82f, 0.98f), emissionIntensity: 1.8f);

            CapsuleCollider col = golem.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1.6f, 0f);
            col.radius = 0.95f;
            col.height = 3.2f;

            DefectiveToyAI ai = golem.AddComponent<DefectiveToyAI>();
            ai.InitializeRuntime("Gólem de Escarcha Guardián", isFrostGolem: true, maxHp: 180f, speed: 3.2f);
        }

        /// <summary>
        /// Construye al protagonista Duende Ingeniero / Minero (concepto_duende_minero.png)
        /// en estética 3D estilizada / chibi con todas sus piezas de equipamiento jugables.
        /// </summary>
        private PlayerController EnsureStylizedElfEngineerPlayer()
        {
            PlayerController existing = FindAnyObjectByType<PlayerController>();
            if (existing != null)
            {
                return existing;
            }

            GameObject playerObj = new GameObject("Player_ElfEngineer");
            playerObj.transform.position = new Vector3(0f, 1.15f, -5.5f);

            CharacterController cc = playerObj.AddComponent<CharacterController>();
            cc.height = 1.35f;
            cc.radius = 0.42f;
            cc.center = new Vector3(0f, 0.68f, 0f);

            TemperatureSystem tempSys = playerObj.AddComponent<TemperatureSystem>();
            InventoryAndShop invShop = playerObj.AddComponent<InventoryAndShop>();
            invShop.RegisterResourceCatalog(_resources.Values);
            PlayerController playerCtrl = playerObj.AddComponent<PlayerController>();

            // Modelo 3D Estilizado / Chibi del Duende Ingeniero (IP 100% Original)
            Transform visualRoot = new GameObject("ElfEngineer_ChibiModel3D").transform;
            visualRoot.SetParent(playerObj.transform, false);

            // 1. Botas de Nieve y Abrigo Térmico Acolchado (Verde Oliva #4A5D4E + Cuero + Forro de Borrego)
            CreateBox(visualRoot, "SnowBoot_L", new Vector3(-0.16f, 0.14f, 0.04f), new Vector3(0.22f, 0.26f, 0.34f),
                new Color(0.28f, 0.22f, 0.18f));
            CreateBox(visualRoot, "SnowBoot_R", new Vector3(0.16f, 0.14f, 0.04f), new Vector3(0.22f, 0.26f, 0.34f),
                new Color(0.28f, 0.22f, 0.18f));
            CreateCylinder(visualRoot, "QuiltedThermalCoat_Olive", new Vector3(0f, 0.56f, 0f),
                new Vector3(0.62f, 0.34f, 0.54f), new Color(0.29f, 0.36f, 0.31f));
            CreateCylinder(visualRoot, "SheepskinCollar", new Vector3(0f, 0.88f, 0f),
                new Vector3(0.66f, 0.08f, 0.60f), new Color(0.86f, 0.83f, 0.76f));

            // 2. Cabeza Chibi Expresiva con Nariz Rojiza y Orejas Puntiagudas Prominentes
            CreateSphere(visualRoot, "ChibiHead", new Vector3(0f, 1.12f, 0f), Vector3.one * 0.50f,
                new Color(0.95f, 0.76f, 0.66f));
            CreateSphere(visualRoot, "RosyColdNose", new Vector3(0f, 1.10f, 0.25f), Vector3.one * 0.11f,
                new Color(0.92f, 0.38f, 0.36f));
            GameObject earL = CreateBox(visualRoot, "PointedEar_L", new Vector3(-0.34f, 1.12f, 0f),
                new Vector3(0.28f, 0.10f, 0.10f), new Color(0.95f, 0.72f, 0.62f));
            earL.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            GameObject earR = CreateBox(visualRoot, "PointedEar_R", new Vector3(0.34f, 1.12f, 0f),
                new Vector3(0.28f, 0.10f, 0.10f), new Color(0.95f, 0.72f, 0.62f));
            earR.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);

            // 3. Casco Minero Metálico con Faro Frontal LED
            CreateSphere(visualRoot, "MiningHelmet_Metal", new Vector3(0f, 1.26f, 0f), new Vector3(0.56f, 0.38f, 0.58f),
                new Color(0.27f, 0.35f, 0.29f));
            CreateCylinder(visualRoot, "BrassHeadlampBezel", new Vector3(0f, 1.32f, 0.27f),
                new Vector3(0.18f, 0.06f, 0.18f), new Color(0.78f, 0.58f, 0.26f));

            GameObject headlampLightObj = new GameObject("Helmet_HeadlampSpotLight");
            headlampLightObj.transform.SetParent(visualRoot, false);
            headlampLightObj.transform.localPosition = new Vector3(0f, 1.32f, 0.32f);
            Light headlamp = headlampLightObj.AddComponent<Light>();
            headlamp.type = LightType.Spot;
            headlamp.color = new Color(1.0f, 0.90f, 0.70f);
            headlamp.intensity = 5.5f;
            headlamp.range = 24f;
            headlamp.spotAngle = 62f;

            // 4. Tanque-Mochila Presurizado de Latón con Manómetros
            CreateCylinder(visualRoot, "PressurizedBrassTankBackpack", new Vector3(0f, 0.62f, -0.34f),
                new Vector3(0.32f, 0.32f, 0.32f), new Color(0.78f, 0.58f, 0.28f));
            GameObject gaugeNeedle = CreateBox(visualRoot, "BrassPressureGaugeNeedle", new Vector3(0.17f, 0.66f, -0.34f),
                new Vector3(0.04f, 0.12f, 0.04f), new Color(0.90f, 0.18f, 0.18f));

            // 5. Pico Industrial con Cristal Energético Rojo
            Transform pickaxeRoot = new GameObject("IndustrialPickaxe_Pivot").transform;
            pickaxeRoot.SetParent(visualRoot, false);
            pickaxeRoot.localPosition = new Vector3(0.38f, 0.58f, 0.25f);

            CreateCylinder(pickaxeRoot, "Pickaxe_Handle", new Vector3(0f, 0.25f, 0f),
                new Vector3(0.06f, 0.32f, 0.06f), new Color(0.42f, 0.28f, 0.18f));
            GameObject crystalTip = CreateBox(pickaxeRoot, "Pickaxe_RedCandyCrystalTip", new Vector3(0f, 0.56f, 0.16f),
                new Vector3(0.12f, 0.14f, 0.46f), new Color(0.92f, 0.18f, 0.24f), emissionIntensity: 2.8f);

            // Pivote de cámara conectado a Main Camera
            Transform camPivot = new GameObject("Player_CameraPivot").transform;
            camPivot.SetParent(playerObj.transform, false);
            camPivot.localPosition = new Vector3(0f, 1.25f, 0f);

            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                Camera anyCam = FindAnyObjectByType<Camera>();
                if (anyCam != null)
                {
                    mainCam = anyCam;
                    mainCam.tag = "MainCamera";
                }
                else
                {
                    GameObject camObj = new GameObject("Main Camera");
                    camObj.tag = "MainCamera";
                    mainCam = camObj.AddComponent<Camera>();
                    camObj.AddComponent<AudioListener>();
                }
            }

            mainCam.gameObject.SetActive(true);
            mainCam.enabled = true;
            mainCam.clearFlags = CameraClearFlags.Skybox;
            mainCam.nearClipPlane = 0.2f;
            mainCam.farClipPlane = 400f;

            Transform respawnPoint = new GameObject("Village_SafeRespawnPoint").transform;
            respawnPoint.position = new Vector3(0f, 1.2f, -5.5f);
            tempSys.SetVillageRespawnPoint(respawnPoint, gaugeNeedle.transform);

            playerCtrl.ConfigureRigReferences(
                camPivot,
                mainCam,
                headlamp,
                pickaxeRoot,
                crystalTip.GetComponent<Renderer>(),
                visualRoot);

            return playerCtrl;
        }

        private BoreatekHUDAndShopUI EnsureHUDAndShopUI(PlayerController player)
        {
            BoreatekHUDAndShopUI ui = FindAnyObjectByType<BoreatekHUDAndShopUI>();
            if (ui == null)
            {
                ui = gameObject.AddComponent<BoreatekHUDAndShopUI>();
            }

            ui.BindPlayer(player);
            return ui;
        }

        private GameObject CreateBox(
            Transform parent,
            string name,
            Vector3 localPos,
            Vector3 scale,
            Color color,
            float emissionIntensity = 0f,
            float roughness = 0.4f)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = scale;
            ApplyStylizedMaterial(obj.GetComponent<Renderer>(), color, emissionIntensity, roughness);
            return obj;
        }

        private GameObject CreateCylinder(
            Transform parent,
            string name,
            Vector3 localPos,
            Vector3 scale,
            Color color,
            float emissionIntensity = 0f)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = scale;
            ApplyStylizedMaterial(obj.GetComponent<Renderer>(), color, emissionIntensity, 0.35f);
            return obj;
        }

        private GameObject CreateSphere(
            Transform parent,
            string name,
            Vector3 localPos,
            Vector3 scale,
            Color color,
            float emissionIntensity = 0f)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = scale;
            ApplyStylizedMaterial(obj.GetComponent<Renderer>(), color, emissionIntensity, 0.3f);
            return obj;
        }

        private void ApplyStylizedMaterial(Renderer rend, Color baseColor, float emissionIntensity, float smoothness)
        {
            if (rend == null)
            {
                return;
            }

            Material mat = new Material(_urpLitShader != null ? _urpLitShader : rend.sharedMaterial.shader);
            mat.color = baseColor;
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", baseColor);
            }

            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", 1f - smoothness);
            }

            if (emissionIntensity > 0.01f)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", baseColor * emissionIntensity);
                }
            }

            rend.sharedMaterial = mat;
        }
    }
}
