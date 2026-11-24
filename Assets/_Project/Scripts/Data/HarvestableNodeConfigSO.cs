using Boreatek.Core;
using UnityEngine;

namespace Boreatek.Data
{
    /// <summary>
    /// Configuración de dureza, requisitos térmicos y recompensas de un nodo recolectable.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_NodeConfig_", menuName = "Boreatek/Data/Harvestable Node Config")]
    public class HarvestableNodeConfigSO : ScriptableObject
    {
        [Header("Node Identity")]
        [Tooltip("Nombre visible del nodo (ej. Pino Cristalizado, Veta de Cristal de Caramelo).")]
        [SerializeField] private string _nodeDisplayName = "Nodo Recolectable";

        [Tooltip("Bioma principal donde aparece este nodo.")]
        [SerializeField] private BiomeType _nativeBiome = BiomeType.OuterTundra;

        [Tooltip("Recurso que entrega al ser extraído.")]
        [SerializeField] private ResourceDataSO _outputResource;

        [Header("Extraction Requirements")]
        [Tooltip("Nivel mínimo del Pico Industrial con Cristal Energético Rojo (1, 2 o 3).")]
        [Range(1, 3)]
        [SerializeField] private int _requiredPickaxeTier = 1;

        [Tooltip("Golpes requeridos para completar la extracción.")]
        [Min(1)]
        [SerializeField] private int _maxHitPoints = 4;

        [Tooltip("Si es verdadero (ej. Cofre Congelado), requiere una fuente de calor cercana antes de poder abrirse.")]
        [SerializeField] private bool _requiresHeatToMelt;

        [Tooltip("Segundos de exposición continua al calor para derretir el bloqueo de hielo.")]
        [Min(1f)]
        [SerializeField] private float _meltDurationSeconds = 15f;

        [Tooltip("Si es verdadero, también puede ser destruido instantáneamente con Petardos Festivos.")]
        [SerializeField] private bool _canBeBlastedWithFirecrackers = true;

        [Header("Yield & Respawn")]
        [Tooltip("Cantidad mínima de unidades entregadas al agotar el nodo.")]
        [Min(1)]
        [SerializeField] private int _minYield = 2;

        [Tooltip("Cantidad máxima de unidades entregadas al agotar el nodo.")]
        [Min(1)]
        [SerializeField] private int _maxYield = 4;

        [Tooltip("Tiempo en segundos para que la veta se regenere geológicamente (0 = no regenera).")]
        [Min(0f)]
        [SerializeField] private float _respawnTimeSeconds = 90f;

        public string NodeDisplayName => _nodeDisplayName;
        public BiomeType NativeBiome => _nativeBiome;
        public ResourceDataSO OutputResource => _outputResource;
        public int RequiredPickaxeTier => _requiredPickaxeTier;
        public int MaxHitPoints => _maxHitPoints;
        public bool RequiresHeatToMelt => _requiresHeatToMelt;
        public float MeltDurationSeconds => _meltDurationSeconds;
        public bool CanBeBlastedWithFirecrackers => _canBeBlastedWithFirecrackers;
        public int MinYield => _minYield;
        public int MaxYield => _maxYield;
        public float RespawnTimeSeconds => _respawnTimeSeconds;

        public void InitializeRuntime(
            string nodeName,
            BiomeType biome,
            ResourceDataSO outputResource,
            int requiredTier,
            int maxHits,
            int minYield,
            int maxYield,
            bool requiresHeat = false,
            float meltSeconds = 15f,
            float respawnSeconds = 90f)
        {
            _nodeDisplayName = nodeName;
            _nativeBiome = biome;
            _outputResource = outputResource;
            _requiredPickaxeTier = requiredTier;
            _maxHitPoints = maxHits;
            _minYield = minYield;
            _maxYield = maxYield;
            _requiresHeatToMelt = requiresHeat;
            _meltDurationSeconds = meltSeconds;
            _respawnTimeSeconds = respawnSeconds;
        }
    }
}
