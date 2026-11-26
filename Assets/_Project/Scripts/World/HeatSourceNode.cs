using UnityEngine;

namespace Boreatek.World
{
    /// <summary>
    /// Representa una burbuja de calor en el mundo 3D:
    /// - Forja Central de la Aldea Industrial de Santa (+15 °T/s, radio amplio).
    /// - Guirnaldas Luminosas Navideñas (+5 °T/s, radio 6m).
    /// - Baliza Térmica Portátil (+12 °T/s, radio 10m, capaz de derretir Cofres Congelados).
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class HeatSourceNode : MonoBehaviour
    {
        [Header("Heat Bubble Settings")]
        [Tooltip("Nombre descriptivo de la fuente térmica.")]
        [SerializeField] private string _sourceName = "Guirnalda Luminosa";

        [Tooltip("Calor regenerado por segundo dentro del radio.")]
        [Min(0.5f)]
        [SerializeField] private float _heatRegenPerSecond = 5.5f;

        [Tooltip("Radio en metros de la burbuja de calor.")]
        [Min(1f)]
        [SerializeField] private float _heatRadius = 6f;

        [Tooltip("Si es verdadero, esta fuente térmica derrite Cofres Congelados cercanos.")]
        [SerializeField] private bool _canMeltFrozenChests = true;

        [Tooltip("Si es verdadero, representa el perímetro seguro de la Aldea Industrial de Santa.")]
        [SerializeField] private bool _isVillageSafeHub;

        [Header("Visual Feedback")]
        [SerializeField] private Light _pointLight;

        private SphereCollider _triggerCollider;
        private readonly Collider[] _meltOverlapBuffer = new Collider[12];
        private float _baseLightIntensity = 2.5f;
        private float _flickerOffset;

        public string SourceName => _sourceName;
        public float HeatRegenPerSecond => _heatRegenPerSecond;
        public float HeatRadius => _heatRadius;
        public bool CanMeltFrozenChests => _canMeltFrozenChests;
        public bool IsVillageSafeHub => _isVillageSafeHub;

        private void Awake()
        {
            _triggerCollider = GetComponent<SphereCollider>();
            _triggerCollider.isTrigger = true;
            _triggerCollider.radius = _heatRadius;
            _flickerOffset = Random.Range(0f, 100f);

            if (_pointLight == null)
            {
                _pointLight = GetComponentInChildren<Light>();
            }

            if (_pointLight != null)
            {
                _baseLightIntensity = _pointLight.intensity;
                _pointLight.range = Mathf.Max(_pointLight.range, _heatRadius * 1.35f);
            }
        }

        public void ConfigureRuntime(
            string sourceName,
            float heatRegen,
            float radius,
            bool canMeltChests,
            bool isVillageHub = false)
        {
            _sourceName = sourceName;
            _heatRegenPerSecond = heatRegen;
            _heatRadius = radius;
            _canMeltFrozenChests = canMeltChests;
            _isVillageSafeHub = isVillageHub;

            if (_triggerCollider == null)
            {
                _triggerCollider = GetComponent<SphereCollider>();
            }

            _triggerCollider.isTrigger = true;
            _triggerCollider.radius = _heatRadius;

            if (_pointLight != null)
            {
                _pointLight.range = _heatRadius * 1.4f;
            }
        }

        private void Update()
        {
            if (_pointLight != null && !_isVillageSafeHub)
            {
                float pulse = Mathf.Sin((Time.time + _flickerOffset) * 3.2f) * 0.18f;
                _pointLight.intensity = _baseLightIntensity + pulse;
            }

            if (_canMeltFrozenChests)
            {
                NotifyNearbyFrozenNodes(Time.deltaTime);
            }
        }

        private void NotifyNearbyFrozenNodes(float dt)
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _heatRadius, _meltOverlapBuffer);
            for (int i = 0; i < count; i++)
            {
                Collider col = _meltOverlapBuffer[i];
                if (col == null)
                {
                    continue;
                }

                if (col.TryGetComponent(out HarvestableNode node) && node.IsFrozenLocked)
                {
                    node.ApplyExternalHeatTick(dt);
                }
            }
        }
    }
}
