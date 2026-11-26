using System;
using Boreatek.Audio;
using Boreatek.Core;
using Boreatek.Data;
using Boreatek.Economy;
using Boreatek.Player;
using UnityEngine;

namespace Boreatek.World
{
    /// <summary>
    /// Gestiona la resistencia, nivel de herramienta requerido, deshielo térmico y drops de:
    /// - Pinos Cristalizados (Tundra)
    /// - Árboles de Caramelo Gigantes (Bosque de Caramelos)
    /// - Cofres Congelados (Cementerio de Trineos - requieren Baliza Térmica cercana por 15s)
    /// - Vetas de Carbón Negro, Cristal de Caramelo, Hierro Polar (Cuevas Nivel 1)
    /// - Bloques de Hielo Azul Profundo (La Geoda Nivel 2)
    /// - Fisuras de Núcleos Luminosos (El Abismo Nivel 3)
    /// </summary>
    public class HarvestableNode : MonoBehaviour, IInteractable
    {
        [Header("Node Configuration")]
        [SerializeField] private HarvestableNodeConfigSO _nodeConfig;
        [SerializeField] private Transform _visualMeshContainer;
        [SerializeField] private GameObject _iceOverlayVisual;

        private int _currentHitPoints;
        private float _currentMeltTimer;
        private bool _isDepleted;
        private float _respawnTimer;
        private float _hitShakeTimer;
        private Vector3 _initialLocalScale = Vector3.one;

        public HarvestableNodeConfigSO NodeConfig => _nodeConfig;
        public int CurrentHitPoints => _currentHitPoints;
        public float DestructionProgress => _nodeConfig == null ? 0f :
            1f - Mathf.Clamp01((float)_currentHitPoints / Mathf.Max(1, _nodeConfig.MaxHitPoints));
        public bool IsDepleted => _isDepleted;
        public bool IsFrozenLocked => _nodeConfig != null && _nodeConfig.RequiresHeatToMelt && _currentMeltTimer < _nodeConfig.MeltDurationSeconds;
        public float NormalizedMeltProgress => (_nodeConfig == null || !_nodeConfig.RequiresHeatToMelt)
            ? 1f
            : Mathf.Clamp01(_currentMeltTimer / Mathf.Max(1f, _nodeConfig.MeltDurationSeconds));

        public event Action<HarvestableNode> OnNodeHarvested;

        [Serializable]
        internal struct SaveState
        {
            public int hitPoints;
            public float meltSeconds, respawnSeconds;
            public bool depleted;
        }

        internal SaveState CaptureSave() => new SaveState
        {
            hitPoints = _currentHitPoints, meltSeconds = _currentMeltTimer,
            respawnSeconds = _respawnTimer, depleted = _isDepleted
        };

        internal void RestoreSave(SaveState state)
        {
            _currentHitPoints = state.hitPoints;
            _currentMeltTimer = state.meltSeconds;
            _respawnTimer = state.respawnSeconds;
            _isDepleted = state.depleted;
            SetVisualsActive(!_isDepleted);
            RefreshIceVisual();
        }

        private void Awake()
        {
            if (_visualMeshContainer == null)
            {
                _visualMeshContainer = transform;
            }

            _initialLocalScale = _visualMeshContainer.localScale;
            ResetNodeState();
        }

        public void InitializeRuntime(HarvestableNodeConfigSO config, GameObject iceOverlay = null)
        {
            _nodeConfig = config;
            _iceOverlayVisual = iceOverlay;
            if (_visualMeshContainer == null)
            {
                _visualMeshContainer = transform;
            }

            _initialLocalScale = _visualMeshContainer.localScale;
            ResetNodeState();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (_isDepleted)
            {
                if (_nodeConfig != null && _nodeConfig.RespawnTimeSeconds > 0f)
                {
                    _respawnTimer -= dt;
                    if (_respawnTimer <= 0f)
                    {
                        ResetNodeState();
                    }
                }
                return;
            }

            if (_hitShakeTimer > 0f && _visualMeshContainer != null)
            {
                _hitShakeTimer -= dt;
                float pulse = 1f + Mathf.Sin(_hitShakeTimer * 45f) * 0.12f;
                _visualMeshContainer.localScale = _initialLocalScale * pulse;
            }
            else if (_visualMeshContainer != null)
            {
                _visualMeshContainer.localScale = Vector3.Lerp(_visualMeshContainer.localScale, _initialLocalScale, dt * 10f);
            }
        }

        /// <summary>
        /// Recibe calor continuo desde una Baliza Térmica o Guirnalda cercana para derretir un Cofre Congelado.
        /// </summary>
        public void ApplyExternalHeatTick(float deltaTime)
        {
            if (_isDepleted || !IsFrozenLocked)
            {
                return;
            }

            _currentMeltTimer += deltaTime;
            RefreshIceVisual();
        }

        private void RefreshIceVisual()
        {
            if (_iceOverlayVisual != null)
            {
                float remainingIce = 1f - NormalizedMeltProgress;
                _iceOverlayVisual.transform.localScale = Vector3.one * Mathf.Max(0.05f, remainingIce);
                _iceOverlayVisual.SetActive(!_isDepleted && IsFrozenLocked);
            }
        }

        /// <summary>
        /// Procesa un golpe del Pico Industrial con Cristal Energético Rojo.
        /// </summary>
        public void HitWithPickaxe(
            int playerPickaxeTier,
            int damage,
            InventoryAndShop inventory,
            PlayerController player)
        {
            if (_isDepleted || _nodeConfig == null)
            {
                return;
            }

            if (IsFrozenLocked)
            {
                BoreatekSoundscape.Play(SoundCue.Blocked, transform.position);
                int pct = Mathf.RoundToInt(NormalizedMeltProgress * 100f);
                player?.PublishStatus($"❄️ [{_nodeConfig.NodeDisplayName}] Congelado ({pct}%). Coloca una Baliza Térmica [H] o Guirnalda [G] junto a él.");
                return;
            }

            if (playerPickaxeTier < _nodeConfig.RequiredPickaxeTier)
            {
                BoreatekSoundscape.Play(SoundCue.Blocked, transform.position);
                player?.PublishStatus($"⛏️ Nivel de Pico insuficiente para {_nodeConfig.NodeDisplayName}. Requiere Pico Energético Nivel {_nodeConfig.RequiredPickaxeTier}.");
                return;
            }

            _hitShakeTimer = 0.18f;
            SoundCue strike = _nodeConfig.OutputResource?.ResourceType switch
            {
                ResourceType.CrystallizedWood => SoundCue.Wood,
                ResourceType.PolarIron or ResourceType.PolarScrap => SoundCue.Metal,
                ResourceType.CandyCrystal or ResourceType.LuminousCore => SoundCue.Crystal,
                ResourceType.DeepBlueIce or ResourceType.FrozenChestSupply => SoundCue.Ice,
                _ => SoundCue.Stone
            };
            BoreatekSoundscape.Play(strike, transform.position);
            _currentHitPoints = Mathf.Max(0, _currentHitPoints - Mathf.Max(1, damage));

            if (_currentHitPoints <= 0)
            {
                CompleteExtraction(inventory, player);
            }
            else
            {
                player?.PublishStatus($"⛏️ Extrayendo {_nodeConfig.NodeDisplayName} ({_currentHitPoints}/{_nodeConfig.MaxHitPoints} resistencia)...");
            }
        }

        /// <summary>
        /// Destrucción instantánea mediante Petardos Festivos (explosivos térmicos).
        /// </summary>
        public void BlastWithFestiveFirecracker(InventoryAndShop inventory, PlayerController player)
        {
            if (_isDepleted || _nodeConfig == null)
            {
                return;
            }

            _currentMeltTimer = _nodeConfig.MeltDurationSeconds;
            if (_iceOverlayVisual != null)
            {
                _iceOverlayVisual.SetActive(false);
            }

            CompleteExtraction(inventory, player);
        }

        private void CompleteExtraction(InventoryAndShop inventory, PlayerController player)
        {
            if (_nodeConfig == null || _nodeConfig.OutputResource == null)
            {
                return;
            }

            int baseAmount = UnityEngine.Random.Range(_nodeConfig.MinYield, _nodeConfig.MaxYield + 1);
            int overchargeMult = DayNightCycleManager.Instance != null
                ? DayNightCycleManager.Instance.ResourceYieldMultiplier
                : 1;

            int finalAmount = baseAmount * overchargeMult;

            if (inventory != null)
            {
                int added = inventory.AddResourceToBackpack(_nodeConfig.OutputResource, finalAmount);
                if (added > 0)
                {
                    string bonusTag = overchargeMult > 1 ? " [¡SOBRECARGA NOCHEBUENA 2x!]" : "";
                    player?.PublishStatus($"✅ +{added} {_nodeConfig.OutputResource.DisplayName}{bonusTag} cargado en el Tanque-Mochila.");
                }
                else
                {
                    player?.PublishStatus("⚠️ ¡Tanque-Mochila Presurizado lleno! Descarga en la Tolva Central de la Aldea.");
                    return;
                }
            }

            _isDepleted = true;
            BoreatekSoundscape.Play(SoundCue.Break, transform.position, 0.79f);
            _respawnTimer = _nodeConfig.RespawnTimeSeconds;
            SetVisualsActive(false);
            OnNodeHarvested?.Invoke(this);
        }

        private void ResetNodeState()
        {
            _isDepleted = false;
            _currentMeltTimer = 0f;
            _currentHitPoints = _nodeConfig != null ? _nodeConfig.MaxHitPoints : 3;
            SetVisualsActive(true);

            if (_iceOverlayVisual != null && _nodeConfig != null && _nodeConfig.RequiresHeatToMelt)
            {
                _iceOverlayVisual.SetActive(true);
                _iceOverlayVisual.transform.localScale = Vector3.one;
            }
        }

        private void SetVisualsActive(bool active)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = active;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = active;
            }
        }

        public string GetInteractionPrompt()
        {
            if (_nodeConfig == null || _isDepleted)
            {
                return string.Empty;
            }

            if (IsFrozenLocked)
            {
                int pct = Mathf.RoundToInt(NormalizedMeltProgress * 100f);
                return $"❄️ {_nodeConfig.NodeDisplayName} [Bloqueado por Hielo: {pct}% deshielo - Usa Baliza Térmica (H)]";
            }

            return $"⛏️ {_nodeConfig.NodeDisplayName} -> {_nodeConfig.OutputResource.DisplayName} (Pico Nv.{_nodeConfig.RequiredPickaxeTier} | Clic Izq o [E])";
        }

        public bool CanInteract(GameObject interactor)
        {
            return !_isDepleted && _nodeConfig != null;
        }

        public void Interact(GameObject interactor)
        {
            if (interactor == null)
            {
                return;
            }

            PlayerController pc = interactor.GetComponent<PlayerController>();
            InventoryAndShop inv = interactor.GetComponent<InventoryAndShop>();
            int tier = pc != null ? pc.CurrentPickaxeTier : 1;
            int dmg = pc != null ? pc.PickaxeDamage : 1;

            HitWithPickaxe(tier, dmg, inv, pc);
        }
    }
}
