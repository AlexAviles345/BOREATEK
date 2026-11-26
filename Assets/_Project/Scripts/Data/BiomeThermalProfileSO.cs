using Boreatek.Core;
using UnityEngine;

namespace Boreatek.Data
{
    /// <summary>
    /// Perfil térmico, visual y de penalización de movimiento de cada bioma de Boreatek.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Biome_", menuName = "Boreatek/Data/Biome Thermal Profile")]
    public class BiomeThermalProfileSO : ScriptableObject
    {
        [Header("Biome Identity")]
        [SerializeField] private BiomeType _biomeType;
        [SerializeField] private string _biomeName = "Bioma Polar";

        [Header("Thermal & Movement Rules")]
        [Tooltip("Si es verdadero (Aldea Industrial de Santa), regenera calor en lugar de drenarlo.")]
        [SerializeField] private bool _isSafeWarmHub;

        [Tooltip("Tasa base de drenaje térmico en °T/s (o regeneración si es zona segura).")]
        [Min(0f)]
        [SerializeField] private float _baseThermalRatePerSecond = 1.0f;

        [Tooltip("Multiplicador de velocidad de movimiento por nieve profunda (1.0 = sin penalización, 0.75 = Cementerio de Trineos).")]
        [Range(0.4f, 1.0f)]
        [SerializeField] private float _deepSnowSpeedMultiplier = 1.0f;

        [Header("Atmosphere & URP Color")]
        [SerializeField] private Color _ambientTint = new Color(0.11f, 0.29f, 0.40f);

        public BiomeType BiomeType => _biomeType;
        public string BiomeName => _biomeName;
        public bool IsSafeWarmHub => _isSafeWarmHub;
        public float BaseThermalRatePerSecond => _baseThermalRatePerSecond;
        public float DeepSnowSpeedMultiplier => _deepSnowSpeedMultiplier;
        public Color AmbientTint => _ambientTint;

        public void InitializeRuntime(
            BiomeType type,
            string biomeName,
            bool isSafeHub,
            float thermalRate,
            float snowSpeedMult,
            Color ambientTint)
        {
            _biomeType = type;
            _biomeName = biomeName;
            _isSafeWarmHub = isSafeHub;
            _baseThermalRatePerSecond = thermalRate;
            _deepSnowSpeedMultiplier = snowSpeedMult;
            _ambientTint = ambientTint;
        }
    }
}
