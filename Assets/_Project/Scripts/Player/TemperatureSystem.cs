using System;
using System.Collections.Generic;
using Boreatek.Audio;
using Boreatek.Core;
using Boreatek.Data;
using Boreatek.World;
using UnityEngine;

namespace Boreatek.Player
{
    /// <summary>
    /// Gestiona la temperatura corporal (0 a 100 °T) y la salud del Duende Ingeniero:
    /// - Drena calor fuera de la Aldea Industrial según el bioma actual y el estado de la Ventisca Ciega.
    /// - Regenera calor dentro de las burbujas térmicas de Guirnaldas Luminosas y Balizas Térmicas.
    /// - Aplica las mejoras del Abrigo Térmico Acolchado y el buff del Termo de Chocolate Caliente (60s).
    /// - Ejecuta el rescate de emergencia por hipotermia cuando el calor llega a 0 °T.
    /// </summary>
    public class TemperatureSystem : MonoBehaviour
    {
        [Header("Thermal Vital Stats")]
        [SerializeField] private float _maxTemperature = 100f;
        [SerializeField] private float _currentTemperature = 100f;
        [SerializeField] private float _hypothermiaGraceSeconds = 5f;
        [SerializeField, Min(0f)] private float _shelterGraceSeconds = 24f;

        [Header("Structural Health Stats")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _currentHealth = 100f;

        [Header("Thermal Coat & Village Safe Point")]
        [Tooltip("Nivel actual del Abrigo Térmico Acolchado (1 = 0% reducción, 2 = 25%, 3 = 50%).")]
        [Range(1, 3)]
        [SerializeField] private int _thermalCoatTier = 1;

        [Tooltip("Reducción porcentual de pérdida de calor otorgada por el abrigo (0 a 0.65).")]
        [Range(0f, 0.65f)]
        [SerializeField] private float _coatCoolingReduction;

        [Tooltip("Punto de reaparición en la Aldea Industrial tras un rescate por hipotermia.")]
        [SerializeField] private Transform _villageRespawnPoint;

        [Header("Diegetic Brass Backpack Gauge")]
        [Tooltip("Aguja o indicador visual del manómetro térmico en el Tanque-Mochila Presurizado de Latón.")]
        [SerializeField] private Transform _brassGaugeNeedleTransform;
        private Transform _upperBrassGaugeNeedle;

        private readonly HashSet<HeatSourceNode> _activeHeatSources = new HashSet<HeatSourceNode>();
        private BiomeType _currentBiome = BiomeType.SantaIndustrialVillage;
        private float _currentBiomeBaseCoolingRate;
        private float _hotChocolateBuffRemainingSeconds;
        private float _hypothermiaCountdown;
        private float _coldExposure;
        private BoreatekSurfaceChunks _surfaceChunks;
        private string _currentZoneLabel = "Aldea Industrial de Santa";

        internal void SetSurfaceChunks(BoreatekSurfaceChunks chunks) => _surfaceChunks = chunks;

        public float MaxTemperature => _maxTemperature;
        public float CurrentTemperature => _currentTemperature;
        public float NormalizedTemperature => Mathf.Clamp01(_currentTemperature / Mathf.Max(1f, _maxTemperature));
        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public float NormalizedHealth => Mathf.Clamp01(_currentHealth / Mathf.Max(1f, _maxHealth));
        public int ThermalCoatTier => _thermalCoatTier;
        public float CoatCoolingReduction => _coatCoolingReduction;
        public bool IsInsideHeatBubble => _activeHeatSources.Count > 0 || _currentBiome == BiomeType.SantaIndustrialVillage;
        public bool IsHotChocolateActive => _hotChocolateBuffRemainingSeconds > 0f;
        public float HotChocolateRemainingSeconds => _hotChocolateBuffRemainingSeconds;
        internal float ShelterGraceRemaining => Mathf.Max(0f, _shelterGraceSeconds - _coldExposure);
        public BiomeType CurrentBiome => _currentBiome;
        public string ActiveHeatSourceName { get; private set; } = "Aldea Industrial de Santa";

        public event Action<float, float> OnTemperatureChanged;
        public event Action<float, float> OnHealthChanged;
        internal float CombatHitAt { get; private set; } = float.NegativeInfinity;
        internal float CombatHitStrength { get; private set; }
        public event Action<BiomeType, string> OnBiomeOrThermalZoneChanged;
        internal event Action<bool> OnVillageRescueTriggered;
        internal event Action<bool> OnDied;
        private bool _isDead;
        private bool _diedOfCold;

        private void Start()
        {
            _currentTemperature = _maxTemperature;
            _currentHealth = _maxHealth;
            _hypothermiaCountdown = _hypothermiaGraceSeconds;
            OnTemperatureChanged?.Invoke(_currentTemperature, _maxTemperature);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_isDead || dt <= 0f)
            {
                return;
            }

            CleanNullHeatSources();
            UpdateHotChocolateTimer(dt);
            EvaluateBiomeByPosition();
            UpdateThermalBalance(dt);
            UpdateDiegeticBrassGauge();
        }

        private void UpdateHotChocolateTimer(float dt)
        {
            if (_hotChocolateBuffRemainingSeconds > 0f)
            {
                _hotChocolateBuffRemainingSeconds = Mathf.Max(0f, _hotChocolateBuffRemainingSeconds - dt);
            }
        }

        /// <summary>
        /// Evalúa automáticamente el bioma y estrato subterráneo según la posición 3D del Duende Ingeniero.
        /// </summary>
        private void EvaluateBiomeByPosition()
        {
            Vector3 pos = transform.position;

            BiomeType detectedBiome;
            float baseCooling;
            string zoneLabel;

            if (_surfaceChunks != null && _surfaceChunks.IsInProceduralCave(pos))
            {
                detectedBiome = BiomeType.IceCavesLevel1;
                baseCooling = 1.45f;
                zoneLabel = "Cueva de cristales";
            }
            else if (pos.y < -28f)
            {
                detectedBiome = BiomeType.LuminousCoreAbyssLevel3;
                baseCooling = 3.0f;
                zoneLabel = "El Abismo del Núcleo Luminoso (Nivel 3)";
            }
            else if (pos.y < -16f)
            {
                detectedBiome = BiomeType.BlueIceGeodeLevel2;
                baseCooling = 2.5f;
                zoneLabel = "La Geoda de Hielo Azul (Nivel 2)";
            }
            else if (pos.y < -6f)
            {
                detectedBiome = BiomeType.IceCavesLevel1;
                baseCooling = 1.5f;
                zoneLabel = "Cuevas de Hielo (Nivel 1)";
            }
            else if (pos.y < -1.5f)
            {
                detectedBiome = BiomeType.VentilationTunnels;
                baseCooling = 1.0f;
                zoneLabel = "Túneles de Ventilación / Transición";
            }
            else if (BoreatekWorldBuilder.IsVillageGround(pos.x, pos.z))
            {
                detectedBiome = BiomeType.SantaIndustrialVillage;
                baseCooling = 0f;
                zoneLabel = "Aldea Industrial de Santa (Zona Segura)";
            }
            else if (_surfaceChunks != null && BoreatekSurfaceChunks.IsFrontier(pos.x, pos.z))
            {
                detectedBiome = _surfaceChunks.FrontierBiome(pos.x, pos.z);
                (baseCooling, zoneLabel) = detectedBiome switch
                {
                    BiomeType.GiantCandyForest => (1.5f, "Bosque de Caramelos de la Frontera"),
                    BiomeType.SleighGraveyard => (2.5f, "Páramo de Trineos de la Frontera"),
                    BiomeType.GlacialFlats => (1.45f, "Planicies de hielo"),
                    BiomeType.SnowDunes => (1.15f, "Dunas de nieve"),
                    BiomeType.SnowyTaiga => (1.05f, "Taiga nevada"),
                    BiomeType.GlacialMoraine => (1.55f, "Morrenas glaciares"),
                    BiomeType.FrostBirchForest => (1.12f, "Bosque de abedules escarchados"),
                    BiomeType.FrostValley => (1.23f, "Valle de escarcha"),
                    BiomeType.AlpineRidges => (1.74f, "Crestas alpinas"),
                    _ => (1.0f, "Tundra de la Frontera")
                };
            }
            else
            {
                detectedBiome = BoreatekWorldBuilder.StartingSurfaceBiome(pos.x, pos.z);
                (baseCooling, zoneLabel) = detectedBiome switch
                {
                    BiomeType.GiantCandyForest => (1.5f, "Bosque de Caramelos Gigantes"),
                    BiomeType.SleighGraveyard => (2.5f, "El Cementerio de Trineos"),
                    _ => (1.0f, "Tundra Exterior Inicial")
                };
            }

            _currentBiomeBaseCoolingRate = baseCooling;
            if (detectedBiome != _currentBiome || _currentZoneLabel != zoneLabel)
            {
                _currentBiome = detectedBiome;
                _currentZoneLabel = zoneLabel;
                OnBiomeOrThermalZoneChanged?.Invoke(_currentBiome, zoneLabel);
            }
            if (_activeHeatSources.Count == 0) ActiveHeatSourceName = _currentZoneLabel;
        }

        private void UpdateThermalBalance(float dt)
        {
            float highestRegen = 0f;
            if (_currentBiome == BiomeType.SantaIndustrialVillage)
            {
                highestRegen = 15f;
            }

            foreach (HeatSourceNode source in _activeHeatSources)
            {
                if (source != null && source.HeatRegenPerSecond > highestRegen)
                {
                    highestRegen = source.HeatRegenPerSecond;
                }
            }

            float previousTemp = _currentTemperature;

            if (highestRegen > 0f)
            {
                _currentTemperature = Mathf.Min(_maxTemperature, _currentTemperature + highestRegen * dt);
                if (_currentTemperature >= _maxTemperature - 0.01f) _coldExposure = 0f;
                _hypothermiaCountdown = _hypothermiaGraceSeconds;
            }
            else if (!IsHotChocolateActive)
            {
                float previousExposure = _coldExposure;
                _coldExposure = Mathf.Min(_shelterGraceSeconds + 5f, _coldExposure + dt);
                float coolingTime = Mathf.Min(dt, Mathf.Max(0f, previousExposure + dt - _shelterGraceSeconds));
                float onset = Mathf.SmoothStep(0f, 1f, (_coldExposure - _shelterGraceSeconds) / 5f);
                float weatherMult = DayNightCycleManager.Instance != null
                    ? DayNightCycleManager.Instance.CurrentWeatherCoolingMultiplier
                    : 1.0f;

                float effectiveDrain = _currentBiomeBaseCoolingRate * weatherMult * (1f - _coatCoolingReduction);
                _currentTemperature = Mathf.Max(0f, _currentTemperature - effectiveDrain * coolingTime * onset);
            }

            if (!Mathf.Approximately(previousTemp, _currentTemperature))
            {
                OnTemperatureChanged?.Invoke(_currentTemperature, _maxTemperature);
            }

            if (_currentTemperature <= 0.01f)
            {
                _hypothermiaCountdown -= dt;
                if (_hypothermiaCountdown <= 0f)
                {
                    Die(fromCold: true);
                }
            }
            else
            {
                _hypothermiaCountdown = _hypothermiaGraceSeconds;
            }
        }

        private void UpdateDiegeticBrassGauge()
        {
            if (_brassGaugeNeedleTransform == null && _upperBrassGaugeNeedle == null) return;

            float angle = Mathf.Lerp(-120f, 120f, NormalizedTemperature);
            Quaternion reading = Quaternion.Euler(0f, 0f, -angle);
            if (_brassGaugeNeedleTransform != null) _brassGaugeNeedleTransform.localRotation = reading;
            if (_upperBrassGaugeNeedle != null) _upperBrassGaugeNeedle.localRotation = reading;
        }

        /// <summary>
        /// Consume un Termo de Chocolate Caliente: restaura el calor al 100% y frena el congelamiento por 60 segundos.
        /// </summary>
        public void ConsumeHotChocolateThermos(float immunityDurationSeconds = 60f)
        {
            _currentTemperature = _maxTemperature;
            _hotChocolateBuffRemainingSeconds = Mathf.Max(_hotChocolateBuffRemainingSeconds, immunityDurationSeconds);
            _hypothermiaCountdown = _hypothermiaGraceSeconds;
            OnTemperatureChanged?.Invoke(_currentTemperature, _maxTemperature);
        }

        /// <summary>
        /// Consume un Bastón de Menta Curativo para recuperar salud tras ataques de Juguetes Defectuosos.
        /// </summary>
        public void ConsumePeppermintCane(float healAmount = 50f)
        {
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + healAmount);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        /// <summary>
        /// Recibe daño en combate contra Soldados de Juguete Defectuosos o Gólems de Escarcha.
        /// </summary>
        public void ApplyCombatDamage(float damage, Vector3? attackerPosition = null)
        {
            if (_isDead || damage <= 0f || Time.timeScale <= 0f)
            {
                return;
            }

            CombatHitAt = Time.time;
            CombatHitStrength = Mathf.Clamp01(damage / (_maxHealth * 0.24f));
            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (_currentHealth <= 0f)
            {
                Die(fromCold: false);
            }
            else if (TryGetComponent(out PlayerController player))
            {
                BoreatekSoundscape.Play(SoundCue.Hurt);
                player.ReactToCombatDamage(attackerPosition, CombatHitStrength);
            }
        }

        /// <summary>
        /// Mejora permanentemente el Abrigo Térmico Acolchado (Verde Oliva y Cuero con Forro de Borrego).
        /// </summary>
        public void UpgradeThermalCoat(int newTier, float newCoolingReduction)
        {
            _thermalCoatTier = Mathf.Clamp(newTier, 1, 3);
            _coatCoolingReduction = Mathf.Clamp(newCoolingReduction, 0f, 0.65f);
        }

        public void SetVillageRespawnPoint(Transform respawnTransform, Transform gaugeNeedle = null,
            Transform upperGaugeNeedle = null)
        {
            _villageRespawnPoint = respawnTransform;
            if (gaugeNeedle != null)
            {
                _brassGaugeNeedleTransform = gaugeNeedle;
            }
            _upperBrassGaugeNeedle = upperGaugeNeedle;
        }

        private void Die(bool fromCold)
        {
            _isDead = true;
            BoreatekSoundscape.Play(SoundCue.Death, strength: 0.88f);
            _diedOfCold = fromCold;
            if (OnDied == null)
            {
                RespawnAtVillage();
                return;
            }

            OnDied.Invoke(fromCold);
        }

        internal void RespawnAtVillage()
        {
            if (!_isDead)
            {
                return;
            }

            _isDead = false;
            _currentTemperature = _maxTemperature;
            _currentHealth = _maxHealth;
            CombatHitAt = float.NegativeInfinity;
            _hypothermiaCountdown = _hypothermiaGraceSeconds;
            _hotChocolateBuffRemainingSeconds = 0f;
            _coldExposure = 0f;
            _activeHeatSources.Clear();

            if (TryGetComponent(out CharacterController cc))
            {
                bool wasEnabled = cc.enabled;
                cc.enabled = false;
                transform.position = _villageRespawnPoint != null
                    ? _villageRespawnPoint.position
                    : new Vector3(0f, 1.2f, -6f);
                cc.enabled = wasEnabled;
            }
            else
            {
                transform.position = _villageRespawnPoint != null
                    ? _villageRespawnPoint.position
                    : new Vector3(0f, 1.2f, -6f);
            }

            OnTemperatureChanged?.Invoke(_currentTemperature, _maxTemperature);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            OnVillageRescueTriggered?.Invoke(_diedOfCold);
        }

        [Serializable]
        internal struct SaveState
        {
            public float temperature, health, coatReduction, chocolateSeconds, coldGrace, coldExposure;
            public int coatTier;
        }

        internal SaveState CaptureSave() => new SaveState
        {
            temperature = _currentTemperature,
            health = _currentHealth,
            coatReduction = _coatCoolingReduction,
            chocolateSeconds = _hotChocolateBuffRemainingSeconds,
            coldGrace = _hypothermiaCountdown,
            coldExposure = _coldExposure,
            coatTier = _thermalCoatTier
        };

        internal void RestoreSave(SaveState state)
        {
            _currentTemperature = Mathf.Clamp(state.temperature, 0f, _maxTemperature);
            _currentHealth = Mathf.Clamp(state.health, 1f, _maxHealth);
            CombatHitAt = float.NegativeInfinity;
            _hotChocolateBuffRemainingSeconds = state.chocolateSeconds;
            _hypothermiaCountdown = state.coldGrace;
            _coldExposure = Mathf.Clamp(state.coldExposure, 0f, _shelterGraceSeconds + 5f);
            UpgradeThermalCoat(state.coatTier, state.coatReduction);
            OnTemperatureChanged?.Invoke(_currentTemperature, _maxTemperature);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        private void CleanNullHeatSources()
        {
            _activeHeatSources.RemoveWhere(s => s == null || !s.gameObject.activeInHierarchy);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out HeatSourceNode heatSource))
            {
                _activeHeatSources.Add(heatSource);
                ActiveHeatSourceName = heatSource.SourceName;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out HeatSourceNode heatSource))
            {
                _activeHeatSources.Remove(heatSource);
            }
        }
    }
}
