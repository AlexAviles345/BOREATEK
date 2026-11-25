using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Boreatek.Core
{
    /// <summary>
    /// Controla el ciclo continuo de día y noche de 20 minutos en tiempo real (10m día / 10m noche)
    /// a lo largo de la campaña de 20 horas (60 ciclos), gestionando la Directional Light,
    /// la niebla de la Ventisca Ciega en URP y el evento final La Gran Nochebuena.
    /// </summary>
    public class DayNightCycleManager : MonoBehaviour
    {
        public const float CYCLE_DURATION_SECONDS = 1200f; // 20 minutos reales
        public const int CAMPAIGN_TOTAL_CYCLES = 60;       // 20 horas reales

        public static DayNightCycleManager Instance { get; private set; }

        [Header("Time & Campaign Settings")]
        [Tooltip("Duración de un ciclo completo Día/Noche en segundos reales (1200s = 20 minutos).")]
        [SerializeField] private float _cycleDurationSeconds = CYCLE_DURATION_SECONDS;

        [Tooltip("Multiplicador de velocidad del tiempo (1 = tiempo real, aumentar para pruebas rápidas).")]
        [Range(1f, 60f)]
        [SerializeField] private float _timeScaleMultiplier = 1f;

        [Header("URP Directional Light & Sky")]
        [Tooltip("Luz direccional principal del sol/luna polar.")]
        [SerializeField] private Light _directionalLight;

        [SerializeField] private Color _dayLightColor = new Color(0.85f, 0.91f, 0.96f);
        [SerializeField] private Color _nightLightColor = new Color(0.11f, 0.29f, 0.40f);
        [SerializeField] private Color _christmasEveAuroraColor = new Color(0.28f, 0.85f, 0.78f);
        [SerializeField] private float _dayLightIntensity = 1.35f;
        [SerializeField] private float _nightLightIntensity = 0.22f;

        [Header("URP Dynamic Fog & Blind Blizzard")]
        [SerializeField] private Color _clearDayFogColor = new Color(0.56f, 0.72f, 0.84f);
        [SerializeField] private Color _polarNightFogColor = new Color(0.05f, 0.11f, 0.16f);
        [SerializeField] private Color _blizzardFogColor = new Color(0.32f, 0.44f, 0.54f);
        [SerializeField] private float _clearFogDensity = 0.006f;
        [SerializeField] private float _nightFogDensity = 0.014f;
        [SerializeField] private float _blindBlizzardFogDensity = 0.055f;

        [Header("Blind Blizzard Schedule")]
        [Tooltip("Duración promedio del evento climático Ventisca Ciega en segundos.")]
        [SerializeField] private float _blizzardDurationSeconds = 95f;

        private float _elapsedCycleSeconds;
        private int _currentCycleNumber = 1;
        private bool _isNightPhase;
        private bool _isBlindBlizzardActive;
        private bool _isChristmasEveClimax;
        private float _blizzardTimer;
        private float _nextBlizzardCheckTimer = 140f;
        private WeatherState _currentWeather = WeatherState.ClearArcticDay;

        public int CurrentCycleNumber => _currentCycleNumber;
        public float NormalizedCycleTime => Mathf.Clamp01(_elapsedCycleSeconds / Mathf.Max(1f, _cycleDurationSeconds));
        public bool IsNightPhase => _isNightPhase;
        public bool IsBlindBlizzardActive => _isBlindBlizzardActive;
        public bool IsChristmasEveClimax => _isChristmasEveClimax;
        public float NightSkyWeight
        {
            get
            {
                if (_isChristmasEveClimax) return 0.92f;
                float evening = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.58f, NormalizedCycleTime));
                float dawn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.85f, 1f, NormalizedCycleTime));
                return evening * (1f - dawn);
            }
        }
        public WeatherState CurrentWeather => _currentWeather;
        public float TotalCampaignHoursElapsed => ((_currentCycleNumber - 1) * _cycleDurationSeconds + _elapsedCycleSeconds) / 3600f;

        /// <summary>
        /// Multiplicador climático aplicado al drenaje térmico (1.0x día, 1.5x noche, 2.5x Ventisca Ciega).
        /// </summary>
        public float CurrentWeatherCoolingMultiplier
        {
            get
            {
                if (_isChristmasEveClimax)
                {
                    return 2.75f;
                }

                if (_isBlindBlizzardActive)
                {
                    return 2.5f;
                }

                return _isNightPhase ? 1.5f : 1.0f;
            }
        }

        /// <summary>
        /// Multiplicador global de producción de nodos (1x normal, 2x durante La Gran Nochebuena).
        /// </summary>
        public int ResourceYieldMultiplier => _isChristmasEveClimax ? 2 : 1;

        public event Action<int, bool> OnCyclePhaseChanged;
        public event Action<WeatherState> OnWeatherStateChanged;
        public event Action OnChristmasEveClimaxStarted;

        [Serializable]
        internal struct SaveState
        {
            public int cycle;
            public float elapsed, blizzardSeconds, nextBlizzard;
            public bool blizzard, christmasEve;
        }

        internal SaveState CaptureSave() => new SaveState
        {
            cycle = _currentCycleNumber, elapsed = _elapsedCycleSeconds,
            blizzard = _isBlindBlizzardActive, christmasEve = _isChristmasEveClimax,
            blizzardSeconds = _blizzardTimer, nextBlizzard = _nextBlizzardCheckTimer
        };

        internal void RestoreSave(SaveState state)
        {
            _currentCycleNumber = state.cycle;
            _elapsedCycleSeconds = state.elapsed;
            _isNightPhase = NormalizedCycleTime >= 0.5f;
            _isBlindBlizzardActive = state.blizzard;
            _isChristmasEveClimax = state.christmasEve;
            _blizzardTimer = state.blizzardSeconds;
            _nextBlizzardCheckTimer = state.nextBlizzard;
            RefreshWeatherState();
            UpdateLightingAndFog(1f);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_directionalLight == null)
            {
                Light[] lights = FindObjectsByType<Light>();
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].type == LightType.Directional)
                    {
                        _directionalLight = lights[i];
                        break;
                    }
                }
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = _clearFogDensity;
            RenderSettings.fogColor = _clearDayFogColor;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            UpdateLightingAndFog(1f);
        }

        private void Update()
        {
            float dt = Time.deltaTime * _timeScaleMultiplier;
            UpdateClockAndPhases(dt);
            UpdateDynamicBlizzard(dt);
            UpdateLightingAndFog(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void UpdateClockAndPhases(float dt)
        {
            _elapsedCycleSeconds += dt;

            if (_elapsedCycleSeconds >= _cycleDurationSeconds)
            {
                _elapsedCycleSeconds -= _cycleDurationSeconds;
                _currentCycleNumber++;

                if (_currentCycleNumber >= CAMPAIGN_TOTAL_CYCLES && !_isChristmasEveClimax)
                {
                    StartChristmasEveClimax();
                }
            }

            bool shouldBeNight = NormalizedCycleTime >= 0.5f;
            if (shouldBeNight != _isNightPhase)
            {
                _isNightPhase = shouldBeNight;
                OnCyclePhaseChanged?.Invoke(_currentCycleNumber, _isNightPhase);
                RefreshWeatherState();
            }
        }

        private void UpdateDynamicBlizzard(float dt)
        {
            if (_isChristmasEveClimax)
            {
                return;
            }

            if (_isBlindBlizzardActive)
            {
                _blizzardTimer -= dt;
                if (_blizzardTimer <= 0f)
                {
                    SetBlindBlizzard(false);
                }
            }
            else
            {
                _nextBlizzardCheckTimer -= dt;
                if (_nextBlizzardCheckTimer <= 0f)
                {
                    // Mayor probabilidad durante los 10 minutos de noche polar
                    float chance = _isNightPhase ? 0.65f : 0.25f;
                    if (UnityEngine.Random.value <= chance)
                    {
                        SetBlindBlizzard(true);
                    }

                    _nextBlizzardCheckTimer = UnityEngine.Random.Range(110f, 200f);
                }
            }
        }

        private void UpdateLightingAndFog(float unscaledDt)
        {
            float cycleProgress = NormalizedCycleTime;
            float night = NightSkyWeight;

            if (_directionalLight != null)
            {
                float afternoon = Mathf.Sin(Mathf.Clamp01(cycleProgress * 2f) * Mathf.PI);
                float sunPitch = Mathf.Lerp(20f + afternoon * 27f, 14f, night);
                Quaternion targetRotation = Quaternion.Euler(sunPitch, Mathf.Lerp(-42f, 54f, cycleProgress), 0f);
                _directionalLight.transform.rotation = Quaternion.Slerp(
                    _directionalLight.transform.rotation,
                    targetRotation,
                    unscaledDt * 4f);

                Color targetLightColor = _isChristmasEveClimax
                    ? Color.Lerp(_nightLightColor, _christmasEveAuroraColor, 0.55f)
                    : Color.Lerp(_dayLightColor, _nightLightColor, night);

                float targetIntensity = Mathf.Lerp(_dayLightIntensity, _nightLightIntensity, night);
                if (_isBlindBlizzardActive)
                {
                    targetIntensity *= 0.6f;
                }

                _directionalLight.color = Color.Lerp(_directionalLight.color, targetLightColor, unscaledDt * 2f);
                _directionalLight.intensity = Mathf.Lerp(_directionalLight.intensity, targetIntensity, unscaledDt * 2f);
            }

            float targetFogDensity = Mathf.Lerp(_clearFogDensity, _nightFogDensity, night);
            Color targetFogColor = Color.Lerp(_clearDayFogColor, _polarNightFogColor, night);

            if (_isChristmasEveClimax || _isBlindBlizzardActive)
            {
                targetFogDensity = _blindBlizzardFogDensity;
                targetFogColor = _blizzardFogColor;
            }
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, targetFogDensity, unscaledDt * 1.5f);
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, targetFogColor, unscaledDt * 1.5f);
            Color sky = Color.Lerp(new Color(0.52f, 0.67f, 0.78f), new Color(0.12f, 0.23f, 0.34f), night);
            if (_isBlindBlizzardActive) sky = Color.Lerp(sky, _blizzardFogColor, 0.48f);
            float ambientBlend = unscaledDt * 2f;
            RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, sky, ambientBlend);
            RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor, sky * 0.68f, ambientBlend);
            RenderSettings.ambientGroundColor = Color.Lerp(RenderSettings.ambientGroundColor, sky * 0.42f, ambientBlend);
        }

        /// <summary>
        /// Activa o desactiva manualmente el evento climático Ventisca Ciega.
        /// </summary>
        public void SetBlindBlizzard(bool active)
        {
            _isBlindBlizzardActive = active;
            _blizzardTimer = active ? _blizzardDurationSeconds : 0f;
            RefreshWeatherState();
        }

        /// <summary>
        /// Activa el evento final La Gran Nochebuena (hito de las 20 horas o entrega de Núcleos Luminosos):
        /// sobrecarga todos los nodos al doble (2x) y activa la tormenta máxima.
        /// </summary>
        [ContextMenu("Trigger Christmas Eve Climax")]
        public void StartChristmasEveClimax()
        {
            if (_isChristmasEveClimax)
            {
                return;
            }

            _isChristmasEveClimax = true;
            _isBlindBlizzardActive = true;
            RefreshWeatherState();
            OnChristmasEveClimaxStarted?.Invoke();
        }

        [ContextMenu("Toggle Blind Blizzard")]
        public void ToggleBlindBlizzardDebug()
        {
            SetBlindBlizzard(!_isBlindBlizzardActive);
        }

        private void RefreshWeatherState()
        {
            WeatherState nextState;
            if (_isChristmasEveClimax)
            {
                nextState = WeatherState.ChristmasEveOvercharge;
            }
            else if (_isBlindBlizzardActive)
            {
                nextState = WeatherState.BlindBlizzard;
            }
            else if (_isNightPhase)
            {
                nextState = WeatherState.PolarNight;
            }
            else
            {
                nextState = WeatherState.ClearArcticDay;
            }

            if (nextState != _currentWeather)
            {
                _currentWeather = nextState;
                OnWeatherStateChanged?.Invoke(_currentWeather);
            }
        }
    }
}
