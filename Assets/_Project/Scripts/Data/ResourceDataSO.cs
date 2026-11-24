using Boreatek.Core;
using UnityEngine;

namespace Boreatek.Data
{
    /// <summary>
    /// Define las propiedades estáticas y económicas de un recurso de Boreatek.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Resource_", menuName = "Boreatek/Data/Resource Data")]
    public class ResourceDataSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Tipo único del recurso dentro del ecosistema de Boreatek.")]
        [SerializeField] private ResourceType _resourceType;

        [Tooltip("Nombre legible mostrado en el HUD y en la Tolva Central.")]
        [SerializeField] private string _displayName = "Nuevo Recurso";

        [Tooltip("Descripción de lore y utilidad industrial.")]
        [TextArea(2, 4)]
        [SerializeField] private string _description = "Recurso extraído de la frontera polar.";

        [Header("Economy & Visuals")]
        [Tooltip("Valor en Monedas de Jengibre / Fichas del Taller al depositarse en la Tolva Central.")]
        [Min(1)]
        [SerializeField] private int _gingerbreadCoinValue = 10;

        [Tooltip("Color representativo para indicadores de UI y drops low-poly.")]
        [SerializeField] private Color _themeColor = Color.white;

        [Tooltip("Si es verdadero, no se pierde durante un rescate de emergencia por hipotermia.")]
        [SerializeField] private bool _isInsuredAgainstHypothermia;

        public ResourceType ResourceType => _resourceType;
        public string DisplayName => _displayName;
        public string Description => _description;
        public int GingerbreadCoinValue => _gingerbreadCoinValue;
        public Color ThemeColor => _themeColor;
        public bool IsInsuredAgainstHypothermia => _isInsuredAgainstHypothermia;

        /// <summary>
        /// Inicializa datos en memoria para generación procedural o fallback sin asset en disco.
        /// </summary>
        public void InitializeRuntime(
            ResourceType type,
            string displayName,
            string description,
            int coinValue,
            Color themeColor,
            bool insured = false)
        {
            _resourceType = type;
            _displayName = displayName;
            _description = description;
            _gingerbreadCoinValue = coinValue;
            _themeColor = themeColor;
            _isInsuredAgainstHypothermia = insured;
        }
    }
}
