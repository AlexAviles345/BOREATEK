using System;
using Boreatek.Audio;
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
    public partial class InventoryAndShop : MonoBehaviour
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
        private int _aviationCanisters;
        [SerializeField] private bool _hasGiftRadarUnlocked;

        private readonly Dictionary<ResourceType, int> _backpackContents = new Dictionary<ResourceType, int>();
        private readonly List<HeatPlacement> _heatPlacements = new List<HeatPlacement>();
        private BoreatekSurfaceChunks _surfaceChunks;
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

        internal int GetEquipmentStock(EquipmentSlot equipment) => equipment switch
        {
            EquipmentSlot.Pickaxe or EquipmentSlot.Sword => 1,
            EquipmentSlot.Chocolate => _hotChocolateThermosCount,
            EquipmentSlot.Cookie => _gingerbreadCookieCount,
            EquipmentSlot.Peppermint => _peppermintCaneCount,
            EquipmentSlot.Garland => _stringLightsStock,
            EquipmentSlot.Beacon => _thermalBeaconsStock,
            EquipmentSlot.Firecracker => _festiveFirecrackersStock,
            EquipmentSlot.Extractor => _mechanicalElfExtractorsStock,
            _ => 0
        };

        public event Action OnInventoryOrCoinsChanged;

        internal void SetSurfaceChunks(BoreatekSurfaceChunks surface) => _surfaceChunks = surface;

        private void Awake()
        {
            _temperatureSystem = GetComponent<TemperatureSystem>();
            BuildDefaultShopCatalogIfEmpty();
        }

        private void Start()
        {
            // El generador agrega el controlador del jugador después del inventario.
            _playerController = GetComponent<PlayerController>();
        }

        private void OnEnable()
        {
            if (_temperatureSystem != null)
            {
                _temperatureSystem.OnVillageRescueTriggered += HandleRescueResourceLoss;
            }
        }

        private void OnDisable()
        {
            if (_temperatureSystem != null)
            {
                _temperatureSystem.OnVillageRescueTriggered -= HandleRescueResourceLoss;
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
        internal enum PurchaseAvailability { Available, InsufficientFunds, AlreadyOwned }

        internal PurchaseAvailability GetPurchaseAvailability(ShopItemSO item)
        {
            bool owned = item.ActionType switch
            {
                ShopActionType.UpgradePickaxe => _playerController != null && _playerController.CurrentPickaxeTier >= item.AmountOrTargetTier,
                ShopActionType.UpgradeBrassBackpack => _backpackTier >= item.AmountOrTargetTier,
                ShopActionType.UpgradeThermalCoat => _temperatureSystem != null && _temperatureSystem.ThermalCoatTier >= item.AmountOrTargetTier,
                ShopActionType.GiftRadar => _hasGiftRadarUnlocked,
                _ => false
            };
            if (owned)
            {
                return PurchaseAvailability.AlreadyOwned;
            }
            return _gingerbreadCoins >= item.CostInGingerbreadCoins
                ? PurchaseAvailability.Available
                : PurchaseAvailability.InsufficientFunds;
        }

        public bool TryPurchaseShopItem(ShopItemSO item)
        {
            if (item == null)
            {
                return false;
            }

            var availability = GetPurchaseAvailability(item);
            if (availability == PurchaseAvailability.InsufficientFunds)
            {
                _playerController?.PublishStatus($"❌ Monedas de Jengibre insuficientes para {item.DisplayName} (Requiere {item.CostInGingerbreadCoins}).");
                return false;
            }
            if (availability == PurchaseAvailability.AlreadyOwned)
            {
                _playerController?.PublishStatus("Ya tienes esta mejora o una superior.");
                return false;
            }

            switch (item.ActionType)
            {
                case ShopActionType.UpgradePickaxe:
                    _playerController?.UpgradePickaxeTier(item.AmountOrTargetTier);
                    break;

                case ShopActionType.UpgradeBrassBackpack:
                    _backpackTier = item.AmountOrTargetTier;
                    _maxBackpackSlots = Mathf.RoundToInt(item.EffectMagnitude);
                    if (_backpackTier >= 3)
                    {
                        _hypothermiaLossPercentage = 0.10f;
                    }
                    _playerController?.PublishStatus($"🎒 ¡Tanque-Mochila de Latón mejorado a Nivel {_backpackTier} ({_maxBackpackSlots} ranuras)!");
                    break;

                case ShopActionType.UpgradeThermalCoat:
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
                    _hasGiftRadarUnlocked = true;
                    break;

                case ShopActionType.MechanicalElfExtractor:
                case ShopActionType.ConveyorBeltSegment:
                    _mechanicalElfExtractorsStock += item.AmountOrTargetTier;
                    break;
            }

            _gingerbreadCoins -= item.CostInGingerbreadCoins;
            BoreatekSoundscape.Play(SoundCue.Confirm);
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
            BoreatekSoundscape.Play(SoundCue.Drink);
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
            BoreatekSoundscape.Play(SoundCue.Eat);
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
            BoreatekSoundscape.Play(SoundCue.Eat);
            _temperatureSystem?.ConsumePeppermintCane(50f);
            OnInventoryOrCoinsChanged?.Invoke();
            _playerController?.PublishStatus("🍬 Bastón de Menta Curativo consumido: +50 HP restaurados.");
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
            BoreatekSoundscape.Play(SoundCue.Blast, blastCenter, 0.91f);
            Collider[] hits = Physics.OverlapSphere(blastCenter, 5.5f);
            int affectedNodes = 0;
            var enemiesHit = new HashSet<AI.IEnemyDamageable>();

            for (int i = 0; i < hits.Length; i++)
            {
                HarvestableNode node = hits[i].GetComponentInParent<HarvestableNode>();
                if (node != null && !node.IsDepleted)
                {
                    node.BlastWithFestiveFirecracker(this, _playerController);
                    affectedNodes++;
                }

                AI.IEnemyDamageable enemy = hits[i].GetComponentInParent<AI.IEnemyDamageable>();
                if (enemy != null && enemiesHit.Add(enemy))
                {
                    enemy.TakeBlastDamage(120f, blastCenter, _playerController);
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
            BoreatekSoundscape.Play(SoundCue.Reboot, targetNode.transform.position);
            if (_surfaceChunks == null || !_surfaceChunks.TryPlaceFrontierExtractor(targetNode))
            {
                MechanicalElfExtractor.CreateAutomatedExtractorOnNode(targetNode, this);
            }
            OnInventoryOrCoinsChanged?.Invoke();
            _playerController?.PublishStatus($"🤖 ¡Duende Mecánico Extractor y Cinta Transportadora instalados en {targetNode.NodeConfig.NodeDisplayName}!");
        }

        internal void RecoverExtractor(MechanicalElfExtractor extractor)
        {
            if (extractor == null || !extractor.isActiveAndEnabled || extractor.Owner != this) return;
            _surfaceChunks?.UnregisterFrontierExtractor(extractor);
            extractor.gameObject.SetActive(false);
            Destroy(extractor.gameObject);
            _mechanicalElfExtractorsStock++;
            BoreatekSoundscape.Play(SoundCue.Confirm, strength: 0.55f);
            OnInventoryOrCoinsChanged?.Invoke();
            _playerController?.PublishStatus("Autómata extractor guardado en la mochila.");
        }

        internal int AviationCanisters => _aviationCanisters;

        internal void BuyAviationCanister()
        {
            if (_aviationCanisters >= 3)
            {
                _playerController?.PublishStatus("Puedes llevar hasta tres bidones de gasolina.");
                return;
            }
            if (_gingerbreadCoins < BoreatekBushPlane.CanPrice)
            {
                _playerController?.PublishStatus($"El bidón cuesta {BoreatekBushPlane.CanPrice} monedas. Vende recursos en la Tolva Central.");
                return;
            }
            _gingerbreadCoins -= BoreatekBushPlane.CanPrice;
            _aviationCanisters++;
            BoreatekSoundscape.Play(SoundCue.Confirm);
            _playerController?.PublishStatus($"Bidón de {BoreatekBushPlane.CanLiters} L comprado. Pulsa R en la avioneta estacionada para repostar.");
            OnInventoryOrCoinsChanged?.Invoke();
        }

        internal bool TryUseAviationCanister()
        {
            if (_aviationCanisters <= 0) return false;
            _aviationCanisters--;
            OnInventoryOrCoinsChanged?.Invoke();
            return true;
        }

        [Serializable]
        internal struct ResourceStack
        {
            public ResourceType type;
            public int count;
        }

        [Serializable]
        internal struct SaveState
        {
            public int backpackTier, backpackSlots, coins, cores;
            public int chocolate, cookies, peppermint, lights, beacons, firecrackers, extractors, aviationCanisters;
            public float resourceLoss;
            public bool radar;
            public ResourceStack[] resources;
            public HeatPlacement[] heatSources;
        }

        internal SaveState CaptureSave()
        {
            var resources = new List<ResourceStack>();
            foreach (var item in _backpackContents)
            {
                resources.Add(new ResourceStack { type = item.Key, count = item.Value });
            }

            return new SaveState
            {
                backpackTier = _backpackTier, backpackSlots = _maxBackpackSlots,
                coins = _gingerbreadCoins, cores = _luminousCoresDeliveredToForge,
                chocolate = _hotChocolateThermosCount, cookies = _gingerbreadCookieCount,
                peppermint = _peppermintCaneCount, lights = _stringLightsStock,
                beacons = _thermalBeaconsStock, firecrackers = _festiveFirecrackersStock,
                extractors = _mechanicalElfExtractorsStock, aviationCanisters = _aviationCanisters, resourceLoss = _hypothermiaLossPercentage,
                radar = _hasGiftRadarUnlocked, resources = resources.ToArray(),
                heatSources = _heatPlacements.ToArray()
            };
        }

        internal void RestoreSave(SaveState state)
        {
            _backpackTier = state.backpackTier;
            _maxBackpackSlots = state.backpackSlots;
            _gingerbreadCoins = state.coins;
            _luminousCoresDeliveredToForge = state.cores;
            _hotChocolateThermosCount = state.chocolate;
            _gingerbreadCookieCount = state.cookies;
            _peppermintCaneCount = state.peppermint;
            _stringLightsStock = state.lights;
            _thermalBeaconsStock = state.beacons;
            _festiveFirecrackersStock = state.firecrackers;
            _mechanicalElfExtractorsStock = state.extractors;
            _aviationCanisters = Mathf.Clamp(state.aviationCanisters, 0, 3);
            _hypothermiaLossPercentage = state.resourceLoss;
            _hasGiftRadarUnlocked = state.radar;
            _backpackContents.Clear();
            foreach (ResourceStack item in state.resources)
            {
                _backpackContents[item.type] = item.count;
            }
            foreach (HeatPlacement item in state.heatSources)
            {
                RegisterHeatPlacement(item);
            }
            RecalculateBackpackCount();
            OnInventoryOrCoinsChanged?.Invoke();
        }

        private void HandleRescueResourceLoss(bool fromCold)
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
            string cause = fromCold ? "HIPOTERMIA" : "DERROTA EN COMBATE";
            _playerController?.PublishStatus($"¡{cause}! Has vuelto a la Aldea Industrial (-{totalLost} recursos no asegurados).");
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
                ShopActionType.HotChocolateThermos, "Restaura el calor al 100% y frena el congelamiento por 60s.", 30, 1, 60f);
            AddRuntimeShopItem("cookie_ginger", "Galleta de Jengibre Energética", ShopCategory.Consumable,
                ShopActionType.GingerbreadCookie, "Aumenta la velocidad de carrera y de picado por 45s.", 25, 1, 45f);
            AddRuntimeShopItem("cane_mint", "Bastón de Menta Curativo", ShopCategory.Consumable,
                ShopActionType.PeppermintCane, "Recupera hasta 50 HP.", 35, 1, 50f);

            // 2. Herramientas Tácticas
            AddRuntimeShopItem("string_lights_5", "Guirnaldas Luminosas (x5)", ShopCategory.TacticalTool,
                ShopActionType.FestiveStringLights, "Puntos de luz y calor portátil en nieve y túneles.", 40, 5, 6.5f);
            AddRuntimeShopItem("thermal_beacon", "Baliza Térmica Portátil", ShopCategory.TacticalTool,
                ShopActionType.ThermalBeacon, "Calefactor recuperable que derrite cofres congelados.", 90, 1, 10.5f);
            AddRuntimeShopItem("firecrackers_3", "Petardos Festivos (x3)", ShopCategory.TacticalTool,
                ShopActionType.FestiveFirecrackers, "Petardos térmicos para fracturar hielo azul y dañar gólems.", 75, 3, 5.5f);
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
                ShopActionType.MechanicalElfExtractor, "Extrae y envía recursos automáticamente desde una veta a la tolva.", 400, 1, 10f);
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
