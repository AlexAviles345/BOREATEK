using Boreatek.Core;
using UnityEngine;

namespace Boreatek.World
{
    public sealed class BoreatekPolarAtmosphere : MonoBehaviour
    {
        private static readonly int Night = Shader.PropertyToID("_Night");
        private static readonly int Storm = Shader.PropertyToID("_Storm");

        private Transform _traveler;
        private BoreatekSurfaceChunks _surface;
        private DayNightCycleManager _clock;
        private Material _previousSky;
        private Material _sky;
        private Material _snowMaterial;
        private ParticleSystem _snow;
        private bool _snowActive;
        private float _stormWeight;
        private float _nextSnowRefresh;
        private float _snowBudget;
        private readonly System.Random _snowRandom = new System.Random(20261224);

        internal void Initialize(Transform traveler, DayNightCycleManager clock, BoreatekSurfaceChunks surface)
        {
            _traveler = traveler;
            _clock = clock;
            _surface = surface;
            Shader skyShader = Resources.Load<Shader>("Boreatek/PolarSky");
            if (skyShader != null)
            {
                _previousSky = RenderSettings.skybox;
                _sky = new Material(skyShader) { name = "M_PolarSky_Runtime" };
                RenderSettings.skybox = _sky;
            }

            Shader snowShader = Resources.Load<Shader>("Boreatek/WindborneSnow");
            if (snowShader != null)
            {
                _snowMaterial = new Material(snowShader) { name = "M_WindborneSnow_Runtime" };
                BuildSnowEmitter();
            }
            UpdateWeather(immediate: true);
        }

        private void LateUpdate() => UpdateWeather(immediate: false);

        private void UpdateWeather(bool immediate)
        {
            float targetStorm = _clock != null && _clock.IsBlindBlizzardActive ?
                (_clock.IsChristmasEveClimax ? 0.79f : 1f) : 0f;
            _stormWeight = immediate ? targetStorm : Mathf.MoveTowards(_stormWeight, targetStorm, Time.deltaTime * 0.42f);
            if (_sky != null)
            {
                _sky.SetFloat(Night, _clock != null ? _clock.NightSkyWeight : 0f);
                _sky.SetFloat(Storm, _stormWeight);
            }
            if (_snow == null || _traveler == null) return;

            Vector3 traveler = _traveler.position;
            bool outdoors = traveler.y > -1.8f && !_surface.IsInProceduralCave(traveler);
            if (outdoors != _snowActive)
            {
                _snowActive = outdoors;
                if (outdoors)
                {
                    _snow.transform.position = traveler + Vector3.up * 6.8f - Vector3.right * (3f + 6f * _stormWeight);
                    _snow.Play();
                }
                else
                {
                    _snowBudget = 0f;
                    _snow.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
            if (!outdoors) return;
            _snow.transform.position = traveler + Vector3.up * 6.8f - Vector3.right * (3f + 6f * _stormWeight);

            if (immediate || Time.time >= _nextSnowRefresh)
            {
                _nextSnowRefresh = Time.time + 0.15f;
                var wind = _snow.velocityOverLifetime;
                wind.x = Mathf.Lerp(0.85f, 9.3f, _stormWeight);
                wind.y = Mathf.Lerp(-2.1f, -3.6f, _stormWeight);
                wind.z = Mathf.Lerp(0.35f, 1.85f, _stormWeight);
            }
            EmitSnow(Time.deltaTime);
        }

        private void EmitSnow(float dt)
        {
            _snowBudget += Mathf.Lerp(26f, 278f, _stormWeight) * dt;
            int count = Mathf.FloorToInt(_snowBudget);
            _snowBudget -= count;
            Vector3 origin = _snow.transform.position;
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = new Vector3(
                    ((float)_snowRandom.NextDouble() - 0.5f) * 29f,
                    ((float)_snowRandom.NextDouble() - 0.5f) * 4f,
                    ((float)_snowRandom.NextDouble() - 0.5f) * 29f);
                TryEmitOutdoorFlake(origin + offset);
            }
        }

        private bool TryEmitOutdoorFlake(Vector3 point)
        {
            // Trace from above so even a single-sided roof covers the area beneath it.
            const float overheadDistance = 90f;
            if (Physics.Raycast(point + Vector3.up * overheadDistance, Vector3.down, overheadDistance + 0.065f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;

            _snow.Emit(new ParticleSystem.EmitParams { position = point, applyShapeToPosition = false }, 1);
            return true;
        }

        private void BuildSnowEmitter()
        {
            var emitter = new GameObject("VFX_PolarSnowAndBlizzard");
            emitter.transform.SetParent(transform, false);
            _snow = emitter.AddComponent<ParticleSystem>();
            _snow.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            _snow.useAutoRandomSeed = false;
            _snow.randomSeed = 20261224;

            var settings = _snow.main;
            settings.playOnAwake = false;
            settings.loop = true;
            settings.simulationSpace = ParticleSystemSimulationSpace.World;
            settings.startLifetime = new ParticleSystem.MinMaxCurve(3.3f, 4.2f);
            settings.startSpeed = 0f;
            settings.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.14f);
            settings.startColor = new ParticleSystem.MinMaxGradient(new Color(0.73f, 0.88f, 1f, 0.5f), Color.white);
            settings.maxParticles = 1250;
            var emission = _snow.emission;
            emission.enabled = false;
            var shape = _snow.shape;
            shape.enabled = false;
            var wind = _snow.velocityOverLifetime;
            wind.enabled = true;
            wind.space = ParticleSystemSimulationSpace.World;
            var collision = _snow.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.quality = ParticleSystemCollisionQuality.High;
            collision.collidesWith = Physics.DefaultRaycastLayers;
            collision.enableDynamicColliders = true;
            collision.radiusScale = 0.7f;
            collision.bounce = 0f;
            collision.lifetimeLoss = 1f;
            var renderer = emitter.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _snowMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void OnDestroy()
        {
            if (_sky != null && RenderSettings.skybox == _sky)
            {
                RenderSettings.skybox = _previousSky;
            }
            if (_sky != null) Destroy(_sky);
            if (_snowMaterial != null) Destroy(_snowMaterial);
        }
    }
}
