using System;
using System.Collections.Generic;
using Boreatek.Automation;
using Boreatek.Core;
using Boreatek.Data;
using Boreatek.Player;
using Boreatek.World;
using UnityEngine;

namespace Boreatek.Economy
{
    /// <summary>
    /// Administra el Tanque-Mochila Presurizado de Latón del Duende Ingeniero,
    /// la conversión de recursos en la Tolva Central a Monedas de Jengibre / Fichas del Taller,
    /// el inventario de consumibles/herramientas tácticas y el catálogo de mejoras y autómatas.
    /// </summary>
    public class InventoryAndShop : MonoBehaviour
    {
        [Header("Brass Tank-Backpack Capacity")]
        [SerializeField] private int _backpackTier = 1;
        [SerializeField] private int _maxBackpackSlots = 12;
        [SerializeField] private float _hypothermiaLossPercentage = 0.30f;

        [Header("Workshop Economy")]
        [SerializeField] private int _gingerbreadCoins = 60;
        [SerializeField] private int _luminousCoresDeliveredToForge;
        [SerializeField] private int _luminousCoresRequiredForChristmasEve = 3;

        [Header("Tactical & Consumable Stock")]
        [SerializeField] private int _hotChocolateThermosCount = 1;
        [SerializeField] private int _gingerbreadCookieCount = 1;
        [SerializeField] private int _peppermintCaneCount = 1;
        [SerializeField] private int _stringLightsStock = 5;
        [SerializeField] private int _thermalBeaconsStock = 1;
        [SerializeField] private int _festiveFirecrackersStock = 2;
        [SerializeField] private int _mechanicalElfExtractorsStock;
        [SerializeField] private bool _hasGiftRadarUnlocked;

        private readonly Dictionary<ResourceType, int> _backpackContents = new Dictionary<ResourceType, int>();
        private readonly Dictionary<ResourceType, ResourceDataSO> _resourceCatalog = new Dictionary<ResourceType, ResourceDataSO>();
        private readonly List<ShopItemSO> _shopCatalog = new List<ShopItemSO>();

        private PlayerController _playerController;
        private TemperatureSystem _temperatureSystem;

        public int BackpackTier => _backpackTier;
        public int MaxBackpackSlots => _maxBackpackSlots;
        public int CurrentBackpackCount { get; private set; }
        public int GingerbreadCoins => _gingerbreadCoins;
        public int LuminousCoresDeliveredToForge => _luminousCoresDeliveredToForge;
        public int LuminousCoresRequiredForChristmasEve => _luminousCoresRequiredForChristmasEve;

        public int HotChocolateThermosCount => _hotChocolateThermosCount;
        public int GingerbreadCookieCount => _gingerbreadCookieCount;
        public int PeppermintCaneCount => _peppermintCaneCount;
        public int StringLightsStock => _stringLightsStock;
        public int ThermalBeaconsStock => _thermalBeaconsStock;
        public int FestiveFirecrackersStock => _festiveFirecrackersStock;
        public int MechanicalElfExtractorsStock => _mechanicalElfExtractorsStock;
        public bool HasGiftRadarUnlocked => _hasGiftRadarUnlocked;

        public IReadOnlyDictionary<ResourceType, int> BackpackContents => _backpackContents;
        public IReadOnlyList<ShopItemSO> ShopCatalog => _shopCatalog;

        public event Action OnInventoryOrCoinsChanged;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _temperatureSystem = GetComponent<TemperatureSystem>();
            BuildDefaultShopCatalogIfEmpty();
        }

        private void OnEnable()
        {
            if (_temperatureSystem != null)
            {
                _temperatureSystem.OnHypothermiaRescueTriggered += HandleHypothermiaResourceLoss;
            }
        }

        private void OnDisable()
        {
            if (_temperatureSystem != null)
            {
                _temperatureSystem.OnHypothermiaRescueTriggered -= HandleHypothermiaResourceLoss;
            }
        }

        public void RegisterResourceCatalog(IEnumerable<ResourceDataSO> resources)
        {
            foreach (ResourceDataSO res in resources)
            {
                if (res != null)
                {
                    _resourceCatalog[res.ResourceType] = res;
                }
            }
        }

        public ResourceDataSO GetResourceDefinition(ResourceType type)
        {
            _resourceCatalog.TryGetValue(type, out ResourceDataSO data);
            return data;
        }

        /// <summary>
        /// Añade recursos al Tanque-Mochila Presurizado de Latón respetando su capacidad actual.
        /// </summary>
        public int AddResourceToBackpack(ResourceDataSO resource, int amount)
        {
            if (resource == null || amount <= 0)
            {
                return 0;
            }

            _resourceCatalog[resource.ResourceType] = resource;

            int freeSlots = Mathf.Max(0, _maxBackpackSlots - CurrentBackpackCount);
            int added = Mathf.Min(freeSlots, amount);
            if (added <= 0)
            {
                return 0;
            }

            if (!_backpackContents.ContainsKey(resource.ResourceType))
            {
                _backpackContents[resource.ResourceType] = 0;
            }

            _backpackContents[resource.ResourceType] += added;
            RecalculateBackpackCount();
            OnInventoryOrCoinsChanged?.Invoke();
            return added;
        }

        /// <summary>
        /// Deposita todos los materiales del Tanque-Mochila en la Tolva Central de la Aldea Industrial,
        /// convirtiéndolos en Monedas de Jengibre / Fichas del Taller y contabilizando los Núcleos Luminosos.
        /// </summary>
        public int DepositAllResourcesIntoCentralHopper()
        {
            if (CurrentBackpackCount <= 0)
            {
                _playerController?.PublishStatus("📥 El Tanque-Mochila está vacío. Explora la tundra o las cuevas para extraer minerales.");
                return 0;
            }

            int totalCoinsEarned = 0;
            int luminousCoresInBatch = 0;

            foreach (KeyValuePair<ResourceType, int> kvp in _backpackContents)
            {
                if (kvp.Value <= 0)
                {
                    continue;
                }

                int unitValue = 10;
                if (_resourceCatalog.TryGetValue(kvp.Key, out ResourceDataSO data) && data != null)
                {
                    unitValue = data.GingerbreadCoinValue;
                }

                totalCoinsEarned += kvp.Value * unitValue;

                if (kvp.Key == ResourceType.LuminousCore)
                {
                    luminousCoresInBatch += kvp.Value;
                }
            }

            _backpackContents.Clear();
            CurrentBackpackCount = 0;
            _gingerbreadCoins += totalCoinsEarned;

            if (luminousCoresInBatch > 0)
            {
                _luminousCoresDeliveredToForge += luminousCoresInBatch;
                if (_luminousCoresDeliveredToForge >= _luminousCoresRequiredForChristmasEve &&
                    DayNightCycleManager.Instance != null &&
                    !DayNightCycleManager.Instance.IsChristmasEveClimax)
                {
                    DayNightCycleManager.Instance.StartChristmasEveClimax();
                    _playerController?.PublishStatus($"🎄⚙️ ¡LA GRAN NOCHEBUENA HA COMENZADO! Trineo Blindado en aproximación y sobrecarga 2x activa.");
                }
            }

            OnInventoryOrCoinsChanged?.Invoke();
            _playerController?.PublishStatus($"📥 Tolva Central procesó la carga: +{totalCoinsEarned} Monedas de Jengibre acreditadas.");
            return totalCoinsEarned;
        }

        /// <summary>
        /// Acredita Monedas de Jengibre directamente desde la red de Duendes Mecánicos y Cintas Transportadoras.
        /// </summary>
        public void AddAutomatedHopperDelivery(ResourceDataSO resource, int amount)
        {
            if (resource == null || amount <= 0)
            {
                return;
            }

            int coins = resource.GingerbreadCoinValue * amount;
            _gingerbreadCoins += coins;

            if (resource.ResourceType == ResourceType.LuminousCore)
            {
                _luminousCoresDeliveredToForge += amount;
            }

            OnInventoryOrCoinsChanged?.Invoke();
        }

        /// <summary>
        /// Compra un artículo o mejora en la Tienda del Taller usando Monedas de Jengibre.
        /// </summary>
        public bool TryPurchaseShopItem(ShopItemSO item)
        {
            if (item == null)
            {
                return false;
            }

            if (_gingerbreadCoins < item.CostInGingerbreadCoins)
            {
                _playerController?.PublishStatus($"❌ Monedas de Jengibre insuficientes para {item.DisplayName} (Requiere {item.CostInGingerbreadCoins}).");
                return false;
            }

            switch (item.ActionType)
            {
                case ShopActionType.UpgradePickaxe:
                    if (_playerController != null && _playerController.CurrentPickaxeTier >= item.AmountOrTargetTier)
                    {
                        _playerController.PublishStatus("✔️ Ya posees este nivel o superior del Pico Industrial con Cristal Energético.");
                        return false;
                    }
                    _playerController?.UpgradePickaxeTier(item.AmountOrTargetTier);
                    break;

                case ShopActionType.UpgradeBrassBackpack:
                    if (_backpackTier >= item.AmountOrTargetTier)
                    {
                        _playerController?.PublishStatus("✔️ Ya posees este nivel o superior del Tanque-Mochila Presurizado de Latón.");
                        return false;
                    }
                    _backpackTier = item.AmountOrTargetTier;
                    _maxBackpackSlots = Mathf.RoundToInt(item.EffectMagnitude);
                    if (_backpackTier >= 3)
                    {
                        _hypothermiaLossPercentage = 0.10f;
                    }
                    _playerController?.PublishStatus($"🎒 ¡Tanque-Mochila de Latón mejorado a Nivel {_backpackTier} ({_maxBackpackSlots} ranuras)!");
                    break;

                case ShopActionType.UpgradeThermalCoat:
                    if (_temperatureSystem != null && _temperatureSystem.ThermalCoatTier >= item.AmountOrTargetTier)
                    {
                        _playerController?.PublishStatus("✔️ Ya posees este nivel o superior del Abrigo Térmico Acolchado.");
                        return false;
                    }
                    _temperatureSystem?.UpgradeThermalCoat(item.AmountOrTargetTier, item.EffectMagnitude);
                    _playerController?.PublishStatus($"🧥 ¡Abrigo Térmico + Botas de Nieve mejorados a Nivel {item.AmountOrTargetTier} (-{Mathf.RoundToInt(item.EffectMagnitude * 100f)}% frío)!");
                    break;

                case ShopActionType.HotChocolateThermos:
                    _hotChocolateThermosCount += item.AmountOrTargetTier;
                    break;

                case ShopActionType.GingerbreadCookie:
                    _gingerbreadCookieCount += item.AmountOrTargetTier;
                    break;

                case ShopActionType.PeppermintCane:
                    _peppermintCaneCount += item.AmountOrTargetTier;
                    break;

                case ShopActionType.FestiveStringLights:
                    _stringLightsStock += item.AmountOrTargetTier;
                    break;

                case ShopActionType.ThermalBeacon:
                    _thermalBeaconsStock += item.AmountOrTargetTier;
                    break;

                case ShopActionType.FestiveFirecrackers:
                    _festiveFirecrackersStock += item.AmountOrTargetTier;
                    break;

                case ShopActionType.GiftRadar:
                    if (_hasGiftRadarUnlocked)
                    {
                        _playerController?.PublishStatus("✔️ El Radar de Regalos ya está instalado en tu casco.");
                        return false;
                    }
                    _hasGiftRadarUnlocked = true;
                    break;

                case ShopActionType.MechanicalElfExtractor:
                case ShopActionType.ConveyorBeltSegment:
                    _mechanicalElfExtractorsStock += item.AmountOrTargetTier;
                    break;
            }

            _gingerbreadCoins -= item.CostInGingerbreadCoins;
            OnInventoryOrCoinsChanged?.Invoke();
            _playerController?.PublishStatus($"🛒 Comprado: {item.DisplayName} (-{item.CostInGingerbreadCoins} Monedas de Jengibre).");
            return true;
        }

        public void TryUseHotChocolateThermos()
        {
            if (_hotChocolateThermosCount <= 0)
            {
                _playerController?.PublishStatus("❌ No tienes Termos de Chocolate Caliente. Cómpralos en la Tienda de la Aldea [Tab].");
                return;
            }

            _hotChocolateThermosCount--;
            _temperatureSystem?.ConsumeHotChocolateThermos(60f);
            OnInventoryOrCoinsChanged?.Invoke();
            _playerController?.PublishStatus("☕ Termo de Chocolate Caliente consumido: Calor al 100% y +60s de protección contra Ventisca Ciega.");
        }

        public void TryUseGingerbreadCookie()
        {
            if (_gingerbreadCookieCount <= 0)
            {
                _playerController?.PublishStatus("❌ No tienes Galletas de Jengibre Energéticas.");
                return;
            }

            _gingerbreadCookieCount--;
            _playerController?.ApplyGingerbreadCookieBuff(45f);
            OnInventoryOrCoinsChanged?.Invoke();
        }

        public void TryUsePeppermintCane()
        {
            if (_peppermintCaneCount <= 0)
            {
                _playerController?.PublishStatus("❌ No tienes Bastones de Menta Curativos.");
                return;
            }

            _peppermintCaneCount--;
            _temperatureSystem?.ConsumePeppermintCane(50f);
            OnInventoryOrCoinsChanged?.Invoke();
            _playerController?.PublishStatus("🍬 Bastón de Menta Curativo consumido: +50 HP restaurados.");
        }

        /// <summary>
        /// Despliega una Guirnalda Luminosa Navideña en el mundo 3D (Tecla G).
        /// </summary>
        public void TryDeployStringLight(Vector3 worldPos)
        {
            if (_stringLightsStock <= 0)
            {
                _playerController?.PublishStatus("❌ Sin Guirnaldas Luminosas disponibles. Adquiérelas en la tienda [Tab].");
                return;
            }

            _stringLightsStock--;
            SpawnStylizedHeatNode(
                worldPos,
                "Guirnalda Luminosa Navideña",
                heatRegen: 5.5f,
                radius: 6.5f,
                canMeltChests: true,
                lightColor: new Color(1.0f, 0.72f, 0.25f),
                isBeacon: false);

            OnInventoryOrCoinsChanged?.Invoke();
            _playerController?.PublishStatus("💡 Guirnalda Luminosa desplegada: Burbuja de calor (+5.5 °T/s, radio 6.5m) activa.");
        }

        /// <summary>
        /// Despliega una Baliza Térmica Portátil de alta potencia capaz de derretir Cofres Congelados (Tecla H).
        /// </summary>
        public void TryDeployThermalBeacon(Vector3 worldPos)
        {
            if (_thermalBeaconsStock <= 0)
            {
                _playerController?.PublishStatus("❌ Sin Balizas Térmicas Portátiles. Adquiérelas en la tienda [Tab].");
                return;
            }

            _thermalBeaconsStock--;
            SpawnStylizedHeatNode(
                worldPos,
                "Baliza Térmica Portátil",
                heatRegen: 13.0f,
                radius: 10.5f,
                canMeltChests: true,
                lightColor: new Color(1.0f, 0.45f, 0.10f),
                isBeacon: true);

            OnInventoryOrCoinsChanged?.Invoke();
            _playerController?.PublishStatus("🔥 Baliza Térmica Portátil activada (+13 °T/s, radio 10.5m). Derritiendo Cofres Congelados cercanos...");
        }

        /// <summary>
        /// Detona un Petardo Festivo para fracturar vetas duras, bloqueos de hielo y Gólems (Tecla Q).
        /// </summary>
        public void TryDetonateFestiveFirecracker(Vector3 blastCenter)
        {
            if (_festiveFirecrackersStock <= 0)
            {
                _playerController?.PublishStatus("❌ Sin Petardos Festivos. Cómpralos en la Tienda del Taller [Tab].");
                return;
            }

            _festiveFirecrackersStock--;
            Collider[] hits = Physics.OverlapSphere(blastCenter, 5.5f);
            int affectedNodes = 0;

            for (int i = 0; i < hits.Length; i++)
            {
                HarvestableNode node = hits[i].GetComponentInParent<HarvestableNode>();
                if (node != null && !node.IsDepleted)
                {
                    node.BlastWithFestiveFirecracker(this, _playerController);
                    affectedNodes++;
                }

                AI.DefectiveToyAI enemy = hits[i].GetComponentInParent<AI.DefectiveToyAI>();
                if (enemy != null)
                {
                    enemy.TakePickaxeDamage(120f, 3, _playerController);
                }
            }

            OnInventoryOrCoinsChanged?.Invoke();
            _playerController?.PublishStatus($"🧨 ¡Petardo Festivo detonado! ({affectedNodes} nodos fracturados en el radio).");
        }

        /// <summary>
        /// Instala un Duende Mecánico Extractor + Cinta Transportadora sobre la veta mineral más cercana (Tecla R).
        /// </summary>
        public void TryDeployMechanicalElfExtractor(Vector3 playerPos)
        {
            if (_mechanicalElfExtractorsStock <= 0)
            {
                _playerController?.PublishStatus("❌ No tienes Duendes Mecánicos Extractores en inventario. Cómpralos en la tienda [Tab].");
                return;
            }

            Collider[] hits = Physics.OverlapSphere(playerPos, 6.0f);
            HarvestableNode targetNode = null;
            for (int i = 0; i < hits.Length; i++)
            {
                HarvestableNode node = hits[i].GetComponentInParent<HarvestableNode>();
                if (node != null && node.NodeConfig != null && node.NodeConfig.OutputResource != null)
                {
                    targetNode = node;
                    break;
                }
            }

            if (targetNode == null)
            {
                _playerController?.PublishStatus("⚠️ Acércate a una veta mineral (< 6m) para instalar el Duende Mecánico Extractor.");
                return;
            }

            _mechanicalElfExtractorsStock--;
            MechanicalElfExtractor.CreateAutomatedExtractorOnNode(targetNode, this);
            OnInventoryOrCoinsChanged?.Invoke();
            _playerController?.PublishStatus($"🤖 ¡Duende Mecánico Extractor y Cinta Transportadora instalados en {targetNode.NodeConfig.NodeDisplayName}!");
        }

        private void SpawnStylizedHeatNode(
            Vector3 pos,
            string label,
            float heatRegen,
            float radius,
            bool canMeltChests,
            Color lightColor,
            bool isBeacon)
        {
            if (Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 6f))
            {
                pos = hit.point;
            }

            GameObject root = new GameObject($"Deployed_{label}");
            root.transform.position = pos;

            // Poste low-poly de latón/cobre
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.transform.SetParent(root.transform, false);
            post.transform.localPosition = new Vector3(0f, isBeacon ? 0.65f : 0.45f, 0f);
            post.transform.localScale = isBeacon ? new Vector3(0.35f, 0.65f, 0.35f) : new Vector3(0.14f, 0.45f, 0.14f);
            Destroy(post.GetComponent<Collider>());
            Renderer postRenderer = post.GetComponent<Renderer>();
            postRenderer.material.color = new Color(0.69f, 0.49f, 0.31f);

            // Foco / Núcleo térmico emisivo
            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.transform.SetParent(root.transform, false);
            bulb.transform.localPosition = new Vector3(0f, isBeacon ? 1.4f : 1.0f, 0f);
            bulb.transform.localScale = isBeacon ? Vector3.one * 0.48f : Vector3.one * 0.28f;
            Destroy(bulb.GetComponent<Collider>());
            Renderer bulbRenderer = bulb.GetComponent<Renderer>();
            bulbRenderer.material.color = lightColor;
            if (bulbRenderer.material.HasProperty("_EmissionColor"))
            {
                bulbRenderer.material.EnableKeyword("_EMISSION");
                bulbRenderer.material.SetColor("_EmissionColor", lightColor * 2.5f);
            }

            // Luz puntual URP sin sombras para alto rendimiento
            GameObject lightObj = new GameObject("ThermalPointLight");
            lightObj.transform.SetParent(root.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, isBeacon ? 1.55f : 1.15f, 0f);
            Light pointLight = lightObj.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = lightColor;
            pointLight.intensity = isBeacon ? 4.2f : 2.6f;
            pointLight.range = radius * 1.4f;
            pointLight.shadows = LightShadows.None;

            SphereCollider trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = radius;

            HeatSourceNode heatNode = root.AddComponent<HeatSourceNode>();
            heatNode.ConfigureRuntime(label, heatRegen, radius, canMeltChests);
        }

        private void HandleHypothermiaResourceLoss()
        {
            List<ResourceType> keys = new List<ResourceType>(_backpackContents.Keys);
            int totalLost = 0;

            for (int i = 0; i < keys.Count; i++)
            {
                ResourceType type = keys[i];
                if (_resourceCatalog.TryGetValue(type, out ResourceDataSO data) &&
                    data != null &&
                    data.IsInsuredAgainstHypothermia)
                {
                    continue;
                }

                int current = _backpackContents[type];
                int lost = Mathf.CeilToInt(current * _hypothermiaLossPercentage);
                _backpackContents[type] = Mathf.Max(0, current - lost);
                totalLost += lost;
            }

            RecalculateBackpackCount();
            OnInventoryOrCoinsChanged?.Invoke();
            _playerController?.PublishStatus($"🥶 ¡HIPOTERMIA! Brigada de rescate te devolvió a la Aldea Industrial (-{totalLost} recursos no asegurados).");
        }

        private void RecalculateBackpackCount()
        {
            int sum = 0;
            foreach (KeyValuePair<ResourceType, int> kvp in _backpackContents)
            {
                sum += kvp.Value;
            }

            CurrentBackpackCount = sum;
        }

        private void BuildDefaultShopCatalogIfEmpty()
        {
            if (_shopCatalog.Count > 0)
            {
                return;
            }

            // 1. Comida y Bebidas (Consumibles)
            AddRuntimeShopItem("thermos_choco", "Termo de Chocolate Caliente", ShopCategory.Consumable,
                ShopActionType.HotChocolateThermos, "Restaura calor al 100% y frena el congelamiento por 60s [Tecla 1].", 30, 1, 60f);
            AddRuntimeShopItem("cookie_ginger", "Galleta de Jengibre Energética", ShopCategory.Consumable,
                ShopActionType.GingerbreadCookie, "+30% velocidad en nieve y +40% velocidad de picado por 45s [Tecla 2].", 25, 1, 45f);
            AddRuntimeShopItem("cane_mint", "Bastón de Menta Curativo", ShopCategory.Consumable,
                ShopActionType.PeppermintCane, "Recupera +50 HP tras combates contra Juguetes Defectuosos [Tecla 3].", 35, 1, 50f);

            // 2. Herramientas Tácticas
            AddRuntimeShopItem("string_lights_5", "Guirnaldas Luminosas (x5)", ShopCategory.TacticalTool,
                ShopActionType.FestiveStringLights, "Puntos de luz y burbuja de calor portátil en nieve y túneles [Tecla G].", 40, 5, 6.5f);
            AddRuntimeShopItem("thermal_beacon", "Baliza Térmica Portátil", ShopCategory.TacticalTool,
                ShopActionType.ThermalBeacon, "Calefactor de alta potencia que derrite Cofres Congelados [Tecla H].", 90, 1, 10.5f);
            AddRuntimeShopItem("firecrackers_3", "Petardos Festivos (x3)", ShopCategory.TacticalTool,
                ShopActionType.FestiveFirecrackers, "Explosivos térmicos para fracturar Hielo Azul y Gólems [Tecla Q].", 75, 3, 5.5f);
            AddRuntimeShopItem("gift_radar", "Radar de Regalos", ShopCategory.TacticalTool,
                ShopActionType.GiftRadar, "Detecta la distancia al Cofre Congelado o Trineo más cercano en el HUD.", 150, 1, 1f);

            // 3. Mejoras Permanentes del Duende Ingeniero
            AddRuntimeShopItem("pickaxe_t2", "Pico Cristal Energético Nivel 2", ShopCategory.PermanentUpgrade,
                ShopActionType.UpgradePickaxe, "Desbloquea extracción de Hierro Polar y La Geoda de Hielo Azul (Nivel 2).", 250, 2, 2f);
            AddRuntimeShopItem("pickaxe_t3", "Pico Cristal Energético Nivel 3", ShopCategory.PermanentUpgrade,
                ShopActionType.UpgradePickaxe, "Desbloquea extracción de Núcleos Luminosos en El Abismo (Nivel 3).", 750, 3, 4f);
            AddRuntimeShopItem("backpack_t2", "Tanque-Mochila de Latón Nivel 2 (22 Slots)", ShopCategory.PermanentUpgrade,
                ShopActionType.UpgradeBrassBackpack, "Amplía la capacidad de carga del tanque presurizado a 22 minerales.", 200, 2, 22f);
            AddRuntimeShopItem("backpack_t3", "Tanque-Mochila de Latón Nivel 3 (35 Slots)", ShopCategory.PermanentUpgrade,
                ShopActionType.UpgradeBrassBackpack, "Amplía a 35 minerales y asegura el 90% contra hipotermia.", 600, 3, 35f);
            AddRuntimeShopItem("coat_t2", "Abrigo Térmico + Botas Nieve Nivel 2", ShopCategory.PermanentUpgrade,
                ShopActionType.UpgradeThermalCoat, "Reduce pérdida de calor en 25% y anula lentitud en nieve profunda.", 300, 2, 0.25f);
            AddRuntimeShopItem("coat_t3", "Abrigo Térmico Geotérmico Nivel 3", ShopCategory.PermanentUpgrade,
                ShopActionType.UpgradeThermalCoat, "Reduce pérdida de calor en 50% en Ventisca Ciega y Abismo.", 800, 3, 0.50f);

            // 4. Automatización
            AddRuntimeShopItem("auto_elf_conveyor", "Duende Mecánico + Cinta Transportadora", ShopCategory.Automation,
                ShopActionType.MechanicalElfExtractor, "Extrae y envía recursos automáticamente desde una veta a la Tolva [Tecla R].", 400, 1, 10f);
        }

        private void AddRuntimeShopItem(
            string id,
            string name,
            ShopCategory category,
            ShopActionType actionType,
            string desc,
            int cost,
            int amountOrTier,
            float magnitude)
        {
            ShopItemSO item = ScriptableObject.CreateInstance<ShopItemSO>();
            item.InitializeRuntime(id, name, category, actionType, desc, cost, amountOrTier, magnitude);
            _shopCatalog.Add(item);
        }
    }
}
