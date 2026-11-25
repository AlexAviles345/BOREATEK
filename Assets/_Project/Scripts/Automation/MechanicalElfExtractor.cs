using Boreatek.Core;
using Boreatek.Data;
using Boreatek.Economy;
using Boreatek.World;
using UnityEngine;

namespace Boreatek.Automation
{
    /// <summary>
    /// Representa un Duende Mecánico Extractor conectado por una Cinta Transportadora
    /// hasta los hornos y la Tolva Central de la Aldea Industrial de Santa.
    /// Extrae recursos periódicamente y los envía automáticamente por la cinta.
    /// </summary>
    public class MechanicalElfExtractor : MonoBehaviour
    {
        [Header("Automation Settings")]
        [SerializeField] private float _extractionIntervalSeconds = 8.0f;
        [SerializeField] private HarvestableNode _attachedNode;
        [SerializeField] private InventoryAndShop _targetVillageEconomy;
        [SerializeField] private Transform _extractorArmVisual;
        [SerializeField] private Transform _orePacketVisual;

        private float _extractionTimer;
        private Vector3 _conveyorStartPos;
        private Vector3 _conveyorEndPos;

        internal HarvestableNode AttachedNode => _attachedNode;
        internal float SecondsUntilExtraction
        {
            get => _extractionTimer;
            set => _extractionTimer = value;
        }

        public void InitializeExtractor(
            HarvestableNode node,
            InventoryAndShop economy,
            Transform armVisual,
            Transform orePacket,
            Vector3 startPos,
            Vector3 endPos)
        {
            _attachedNode = node;
            _targetVillageEconomy = economy;
            _extractorArmVisual = armVisual;
            _orePacketVisual = orePacket;
            _conveyorStartPos = startPos;
            _conveyorEndPos = endPos;
            _extractionTimer = _extractionIntervalSeconds;
        }

        private void Update()
        {
            if (_attachedNode == null || _attachedNode.NodeConfig == null || _targetVillageEconomy == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            _extractionTimer -= dt;

            if (_extractorArmVisual != null)
            {
                float angle = Mathf.Sin(Time.time * 5.0f) * 25f;
                _extractorArmVisual.localRotation = Quaternion.Euler(angle, 0f, 0f);
            }

            if (_orePacketVisual != null)
            {
                float progress = 1f - Mathf.Clamp01(_extractionTimer / Mathf.Max(0.1f, _extractionIntervalSeconds));
                _orePacketVisual.position = Vector3.Lerp(_conveyorStartPos, _conveyorEndPos, progress) + Vector3.up * 0.35f;
            }

            if (_extractionTimer <= 0f)
            {
                _extractionTimer = _extractionIntervalSeconds;
                ResourceDataSO resource = _attachedNode.NodeConfig.OutputResource;
                int mult = DayNightCycleManager.Instance != null
                    ? DayNightCycleManager.Instance.ResourceYieldMultiplier
                    : 1;

                _targetVillageEconomy.AddAutomatedHopperDelivery(resource, 1 * mult);
            }
        }

        /// <summary>
        /// Construye visual y funcionalmente un Duende Mecánico Extractor + Cinta Transportadora en 3D low-poly.
        /// </summary>
        public static MechanicalElfExtractor CreateAutomatedExtractorOnNode(
            HarvestableNode node,
            InventoryAndShop economy)
        {
            Vector3 nodePos = node.transform.position;
            Vector3 villageHopperPos = new Vector3(0f, 0.4f, 4f);

            GameObject root = new GameObject($"AutomatedExtractor_{node.NodeConfig.NodeDisplayName}");
            root.transform.position = nodePos + new Vector3(1.2f, 0f, 0f);

            // Chasis de latón del Duende Mecánico
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            body.transform.localScale = new Vector3(0.65f, 0.55f, 0.65f);
            Destroy(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().material.color = new Color(0.75f, 0.55f, 0.28f);

            // Casco verde oliva del autómata
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            head.transform.localScale = Vector3.one * 0.52f;
            Destroy(head.GetComponent<Collider>());
            head.GetComponent<Renderer>().material.color = new Color(0.29f, 0.36f, 0.31f);

            // Brazo picador con cristal rojo
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.transform.SetParent(root.transform, false);
            arm.transform.localPosition = new Vector3(-0.45f, 0.85f, 0f);
            arm.transform.localScale = new Vector3(0.7f, 0.16f, 0.16f);
            Destroy(arm.GetComponent<Collider>());
            arm.GetComponent<Renderer>().material.color = new Color(0.90f, 0.22f, 0.27f);

            // Cinta Transportadora direccionada hacia la Tolva Central
            Vector3 startConveyor = root.transform.position;
            Vector3 dirToHopper = (villageHopperPos - startConveyor);
            float beltLength = Mathf.Min(12f, dirToHopper.magnitude);
            Vector3 endConveyor = startConveyor + dirToHopper.normalized * beltLength;

            GameObject belt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            belt.transform.SetParent(root.transform, true);
            belt.transform.position = (startConveyor + endConveyor) * 0.5f + Vector3.up * 0.15f;
            belt.transform.rotation = Quaternion.LookRotation(dirToHopper.normalized);
            belt.transform.localScale = new Vector3(0.6f, 0.12f, beltLength);
            Destroy(belt.GetComponent<Collider>());
            belt.GetComponent<Renderer>().material.color = new Color(0.22f, 0.24f, 0.27f);

            // Paquete de mineral deslizándose por la cinta
            GameObject packet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            packet.transform.SetParent(root.transform, true);
            packet.transform.localScale = Vector3.one * 0.35f;
            Destroy(packet.GetComponent<Collider>());
            if (node.NodeConfig.OutputResource != null)
            {
                packet.GetComponent<Renderer>().material.color = node.NodeConfig.OutputResource.ThemeColor;
            }

            MechanicalElfExtractor extractor = root.AddComponent<MechanicalElfExtractor>();
            extractor.InitializeExtractor(node, economy, arm.transform, packet.transform, startConveyor, endConveyor);
            return extractor;
        }
    }
}
