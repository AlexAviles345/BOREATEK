using System;
using Boreatek.Audio;
using Boreatek.Core;
using Boreatek.World;
using UnityEngine;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;

namespace Boreatek.Economy
{
    public partial class InventoryAndShop
    {
        private Transform _heatToolRoot;
        private static readonly Vector3[] BeaconFeet = { new(0.32f, 0f, 0f), new(-0.16f, 0f, 0.277f), new(-0.16f, 0f, -0.277f) };
        private static readonly Vector3[] GarlandFeet = { new(-0.49f, 0f, 0f), new(0.49f, 0f, 0f) };

        [Serializable]
        internal struct HeatPlacement
        {
            public string id;
            public Vector3 position, normal;
            public float yaw;
            public bool isBeacon;
        }

        internal static bool CanAnchorHeatTool(Vector3 point, Vector3 normal, bool beacon, float yaw)
        {
            if (normal.y < 0.82f) return false;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0f, yaw, 0f);
            Vector3 halfSize = beacon ? new Vector3(0.35f, 0.39f, 0.35f) : new Vector3(0.50f, 0.24f, 0.12f);
            if (Physics.CheckBox(point + normal * (halfSize.y + 0.07f), halfSize, rotation,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
            foreach (Vector3 baseFoot in beacon ? BeaconFeet : GarlandFeet)
            {
                Vector3 foot = point + rotation * baseFoot;
                if (!Physics.Raycast(foot + Vector3.up * 0.17f, Vector3.down, out RaycastHit ground, 0.33f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) || ground.normal.y < 0.63f) return false;
            }
            return true;
        }

        internal bool TryPlaceHeatTool(EquipmentSlot slot, Vector3 point, Vector3 normal, float yaw)
        {
            bool beacon = slot == EquipmentSlot.Beacon;
            if (!beacon && slot != EquipmentSlot.Garland) return false;
            if (GetEquipmentStock(slot) <= 0 || !CanAnchorHeatTool(point, normal, beacon, yaw)) return false;
            RegisterHeatPlacement(new HeatPlacement { position = point, normal = normal, yaw = yaw, isBeacon = beacon });
            if (beacon) _thermalBeaconsStock--;
            else _stringLightsStock--;
            BoreatekSoundscape.Play(SoundCue.Deploy, point, beacon ? 0.90f : 0.76f);
            OnInventoryOrCoinsChanged?.Invoke();
            _playerController?.PublishStatus(beacon ? "Baliza activada. Apunta a la carcasa y pulsa E para recogerla."
                : "Guirnalda encendida. Calienta el terreno de alrededor.");
            return true;
        }

        private void RegisterHeatPlacement(HeatPlacement placement)
        {
            if (string.IsNullOrEmpty(placement.id)) placement.id = Guid.NewGuid().ToString("N");
            _heatPlacements.Add(placement);
            if (_surfaceChunks == null || !_surfaceChunks.RegisterFrontierHeat(placement))
                SpawnHeatSource(placement);
        }

        internal bool RecoverBeacon(string id, GameObject beacon)
        {
            int index = _heatPlacements.FindIndex(item => item.isBeacon && item.id == id);
            if (index < 0 || beacon == null || !beacon.activeInHierarchy) return false;
            HeatPlacement item = _heatPlacements[index];
            _heatPlacements.RemoveAt(index);
            _surfaceChunks?.UnregisterFrontierHeat(item);
            beacon.SetActive(false);
            Destroy(beacon);
            _thermalBeaconsStock++;
            BoreatekSoundscape.Play(SoundCue.Confirm, strength: 0.55f);
            OnInventoryOrCoinsChanged?.Invoke();
            _playerController?.PublishStatus("Baliza recogida y guardada en la mochila.");
            return true;
        }

        internal void SpawnHeatSource(HeatPlacement placement, Transform sector = null, BoreatekLandscapeGeometry sharedMaterials = null)
        {
            if (placement.normal.sqrMagnitude < 0.5f)
            {
                placement = AnchorLegacyPlacement(placement);
                int index = _heatPlacements.FindIndex(item => item.id == placement.id);
                if (index >= 0) _heatPlacements[index] = placement;
            }

            if (_heatToolRoot == null)
            {
                var installations = new GameObject("Portable_Heat_Installations");
                _heatToolRoot = installations.transform;
                if (_surfaceChunks != null) _heatToolRoot.SetParent(_surfaceChunks.transform.parent, false);
            }
            BoreatekLandscapeGeometry materials = sharedMaterials ?? _heatToolRoot.GetComponentInParent<BoreatekLandscapeGeometry>();
            if (materials == null)
            {
                materials = _heatToolRoot.gameObject.AddComponent<BoreatekLandscapeGeometry>();
                materials.Initialize(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            }

            bool beacon = placement.isBeacon;
            string label = beacon ? "Baliza Térmica Portátil" : "Guirnalda Luminosa Navideña";
            float radius = beacon ? 10.5f : 6.5f;
            var root = new GameObject(beacon ? "Baliza_Termica_Portatil" : "Guirnalda_Luminosa");
            root.transform.SetParent(sector != null ? sector : _heatToolRoot, false);
            root.transform.SetPositionAndRotation(placement.position,
                Quaternion.FromToRotation(Vector3.up, placement.normal.normalized) * Quaternion.Euler(0f, placement.yaw, 0f));
            materials.PlaceShared(root.transform, beacon ? "Calefactor_Con_Base_Y_Asa" : "Guirnalda_Con_Estacas",
                () => beacon ? ModelBeacon(materials) : ModelGarland(materials));

            var body = root.AddComponent<BoxCollider>();
            body.center = Vector3.up * (beacon ? 0.42f : 0.25f);
            body.size = beacon ? new Vector3(0.65f, 0.84f, 0.65f) : new Vector3(1.01f, 0.50f, 0.16f);
            if (beacon) root.AddComponent<PortableThermalBeacon>().Initialize(this, placement.id);
            var lamp = new GameObject("ThermalPointLight");
            lamp.transform.SetParent(root.transform, false);
            lamp.transform.localPosition = Vector3.up * (beacon ? 0.78f : 0.46f);
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = beacon ? new Color(1f, 0.51f, 0.22f) : new Color(1f, 0.72f, 0.34f);
            light.intensity = beacon ? 3.6f : 2.0f;
            light.shadows = LightShadows.None;
            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            root.AddComponent<HeatSourceNode>().ConfigureRuntime(label, beacon ? 13f : 5.5f, radius, canMeltChests: true);
        }

        private static HeatPlacement AnchorLegacyPlacement(HeatPlacement placement)
        {
            Physics.SyncTransforms();
            Vector3 origin = placement.position + Vector3.up * 0.65f;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.down, 25f, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
            {
                if (hit.normal.y < 0.62f || hit.collider.GetComponentInParent<HeatSourceNode>() != null ||
                    hit.collider.GetComponentInParent<HarvestableNode>() != null || hit.collider is CharacterController) continue;
                if (placement.normal.sqrMagnitude >= 0.5f && hit.point.y <= placement.position.y) continue;
                placement.position = hit.point;
                placement.normal = hit.normal;
            }
            if (placement.normal.sqrMagnitude < 0.5f) placement.normal = Vector3.up;
            return placement;
        }

        private static Batch ModelBeacon(BoreatekLandscapeGeometry materials)
        {
            var mesh = new Batch();
            Material iron = materials.Finish(new Color32(42, 53, 68, 255));
            Material brass = materials.Finish(new Color32(169, 102, 56, 255), smoothness: 0.43f);
            Material glass = materials.Finish(new Color32(255, 153, 70, 255), 1.25f);
            Material vents = materials.Finish(new Color32(70, 83, 90, 255));
            for (int leg = 0; leg < 3; leg++)
            {
                float angle = leg * Mathf.PI * 2f / 3f;
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 foot = dir * 0.32f + Vector3.up * 0.035f;
                mesh.Box(foot, new Vector3(0.19f, 0.07f, 0.18f), iron, Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f));
                mesh.Frustum(foot + Vector3.up * 0.015f, dir * 0.18f + Vector3.up * 0.25f, 0.072f, 0.062f, 6, brass);
            }
            mesh.Frustum(Vector3.up * 0.095f, Vector3.up * 0.27f, 0.235f, 0.27f, 10, iron);
            mesh.Frustum(Vector3.up * 0.245f, Vector3.up * 0.67f, 0.231f, 0.207f, 10, glass);
            for (int side = 0; side < 6; side++)
            {
                float angle = side * 60f;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                mesh.Box(dir * 0.22f + Vector3.up * 0.44f, new Vector3(0.042f, 0.39f, 0.047f), iron,
                    Quaternion.Euler(2f, angle, 0f));
            }
            mesh.Frustum(Vector3.up * 0.64f, Vector3.up * 0.748f, 0.251f, 0.19f, 10, brass);
            mesh.Frustum(Vector3.up * 0.748f, Vector3.up * 0.785f, 0.185f, 0.185f, 10, vents);
            foreach (float x in new[] { -0.16f, 0.16f })
                mesh.Box(new Vector3(x, 0.827f, 0f), new Vector3(0.045f, 0.125f, 0.047f), brass);
            mesh.Box(Vector3.up * 0.887f, new Vector3(0.36f, 0.053f, 0.056f), iron);
            return mesh;
        }

        private static Batch ModelGarland(BoreatekLandscapeGeometry materials)
        {
            var mesh = new Batch();
            Material iron = materials.Finish(new Color32(57, 68, 74, 255));
            Material amber = materials.Finish(new Color32(246, 172, 84, 255), 1.2f);
            foreach (float x in new[] { -0.49f, 0.49f })
                mesh.Frustum(new Vector3(x, 0f, 0f), new Vector3(x, 0.52f, 0f), 0.055f, 0.031f, 6, iron);
            Vector3 start = new Vector3(-0.49f, 0.45f, 0f);
            for (int i = 1; i <= 7; i++)
            {
                float x = -0.49f + i * 0.14f;
                Vector3 end = new Vector3(x, 0.34f + 0.46f * x * x, 0f);
                mesh.Frustum(start, end, 0.019f, 0.019f, 5, iron);
                if (i < 7)
                    mesh.Frustum(end + Vector3.down * 0.11f, end, 0.014f, 0.040f, 6, amber);
                start = end;
            }
            return mesh;
        }
    }
}
