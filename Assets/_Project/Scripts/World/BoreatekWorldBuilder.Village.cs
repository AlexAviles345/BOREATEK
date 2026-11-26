using UnityEngine;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;

namespace Boreatek.World
{
    public partial class BoreatekWorldBuilder
    {
        private void BuildVillageNeighborhood(Transform campus, FoundryMaterials m)
        {
            Transform neighborhood = CharacterPart(campus, "Barrio_De_La_Forja", Vector3.zero);
            Transform dining = BuildFoundryWing(neighborhood, "Comedor", new Vector3(-19.6f, 0f, -4.4f), 6.8f, 6.4f,
                -1, m, "COMEDOR");
            Transform dispatch = BuildFoundryWing(neighborhood, "Mensajeria", new Vector3(19.0f, 0f, -4.6f), 6.2f, 6.2f,
                1, m, "MENSAJERÍA");
            Transform lodge = BuildFoundryWing(neighborhood, "Viviendas", new Vector3(-18.4f, 0f, 9.4f), 7.0f, 6.4f,
                -1, m, "VIVIENDAS");

            var paving = new Batch();
            Pave(paving, new Vector2(-23.1f, -9.3f), new Vector2(22.4f, -7.7f), 2.5f, m);
            Pave(paving, new Vector2(-15.1f, -8.5f), new Vector2(-14.9f, 10.2f), 2.2f, m);
            Pave(paving, new Vector2(14.4f, -8.1f), new Vector2(15.2f, 8.0f), 2.4f, m);
            Pave(paving, new Vector2(-18.4f, 4.9f), new Vector2(-15.0f, 4.9f), 2.0f, m);
            _landscape.Place(neighborhood, "Village_Paved_Lanes", paving, collision: true);

            var yard = new Batch();
            foreach (Vector3 point in new[] { new Vector3(-14f, 0f, -9.4f), new Vector3(14.6f, 0f, -9.2f),
                new Vector3(-16.2f, 0f, 5.25f), new Vector3(16.4f, 0f, 3.1f) })
            {
                Beam(yard, point, point + Vector3.up * 2.55f, 0.17f, m.Timber);
                Beam(yard, point + Vector3.up * 2.46f, point + new Vector3(0.48f, 2.46f, 0f), 0.12f, m.Iron);
                AddFoundryLantern(yard, point + new Vector3(0.43f, 2.20f, 0f), 0.88f, m);
            }
            AddCrate(yard, new Vector3(20.8f, 0.08f, -8.3f), 0.62f, m);
            AddCrate(yard, new Vector3(21.46f, 0.08f, -8.0f), 0.51f, m);
            _landscape.Place(neighborhood, "Street_Lanterns_And_Delivery_Crates", yard);

            var kitchen = new Batch();
            AddWorkbench(kitchen, new Vector3(0.0f, FoundryFloor, 1.02f), 2.4f, m);
            kitchen.Frustum(new Vector3(0.15f, 1.12f, 1.01f), new Vector3(0.15f, 1.42f, 1.01f), 0.26f, 0.32f, 12, m.Iron);
            kitchen.Frustum(new Vector3(0.15f, 1.415f, 1.01f), new Vector3(0.15f, 1.435f, 1.01f), 0.32f, 0.29f, 12, m.Hearth);
            AddWorkbench(kitchen, new Vector3(-2.0f, FoundryFloor, -0.16f), 1.32f, m);
            AddCrate(kitchen, new Vector3(2.55f, FoundryFloor, 1.95f), 0.59f, m);
            _landscape.Place(dining, "Stove_And_Prep_Table", kitchen, collision: true);

            var parcels = new Batch();
            AddWorkbench(parcels, new Vector3(0f, FoundryFloor, 1.05f), 2.4f, m);
            for (int i = 0; i < 3; i++)
            {
                AddCrate(parcels, new Vector3(-0.66f + i * 0.56f, 1.15f, 1.10f), 0.30f + i * 0.046f, m);
                AddCrate(parcels, new Vector3(-2.30f, FoundryFloor, 0.35f + i * 0.70f), 0.54f, m);
            }
            _landscape.Place(dispatch, "Postal_Bench_And_Parcels", parcels, collision: true);

            var beds = new Batch();
            for (int i = -1; i <= 1; i++)
            {
                float x = i * 1.94f;
                beds.Box(new Vector3(x, FoundryFloor + 0.32f, 1.60f), new Vector3(1.25f, 0.49f, 2.09f), m.Timber);
                beds.Box(new Vector3(x, FoundryFloor + 0.61f, 1.44f), new Vector3(1.19f, 0.19f, 1.58f), i == 0 ? m.Roof : m.Banner);
                beds.Box(new Vector3(x, FoundryFloor + 0.68f, 2.28f), new Vector3(0.84f, 0.18f, 0.39f), m.Snow);
            }
            AddCrate(beds, new Vector3(-2.55f, FoundryFloor, -1.5f), 0.59f, m);
            _landscape.Place(lodge, "Beds_And_Storage_Trunks", beds, collision: true);

            AddVillageHearth(dining, "Estufa del comedor");
            AddVillageHearth(dispatch, "Estufa de la mensajería");
            AddVillageHearth(lodge, "Estufa de las viviendas");

            SpawnFoundryWorker(dining, "Duende_Cocinera", FoundryWorker.Job.Cook, new Color32(148, 85, 60, 255),
                new Vector3(0.18f, FoundryFloor, 0.06f), 0f, 0.13f, m, rest: new Vector3(-17.35f, 0f, -8.8f));
            SpawnFoundryWorker(dispatch, "Duende_Despachadora", FoundryWorker.Job.Packer, new Color32(106, 82, 126, 255),
                new Vector3(0.21f, FoundryFloor, 0.04f), 0f, 0.72f, m, rest: new Vector3(16.0f, 0f, -7.65f));
            SpawnFoundryWorker(lodge, "Duende_Cuidador", FoundryWorker.Job.Sweeper, new Color32(86, 111, 72, 255),
                new Vector3(1.50f, FoundryFloor, -0.63f), -90f, 0.41f, m, rest: new Vector3(-18.1f, 0f, 4.85f));
            SpawnFoundryWorker(neighborhood, "Duende_Cartero", FoundryWorker.Job.Courier, new Color32(112, 70, 71, 255),
                new Vector3(19.05f, 0.12f, -8.65f), 70f, 0.27f, m, rest: new Vector3(-16.8f, 0f, -7.25f));
            SpawnFoundryWorker(neighborhood, "Duende_De_Mantenimiento", FoundryWorker.Job.Sweeper, new Color32(58, 108, 130, 255),
                new Vector3(-15.05f, 0.15f, 0.4f), 170f, 0.88f, m, rest: new Vector3(-17.2f, 0f, 5.7f));
        }

        private static void Pave(Batch batch, Vector2 start, Vector2 end, float width, FoundryMaterials m)
        {
            Vector2 direction = end - start;
            Vector2 middle = (start + end) * 0.5f;
            float yaw = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            batch.Box(new Vector3(middle.x, -0.142f, middle.y), new Vector3(width, 0.50f, direction.magnitude),
                m.Stone, Quaternion.Euler(0f, yaw, 0f));
        }

        private static void AddVillageHearth(Transform room, string label)
        {
            Transform point = CharacterPart(room, "Domestic_Heat", new Vector3(0f, 0.63f, 1.55f));
            var trigger = point.gameObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 3.4f;
            point.gameObject.AddComponent<HeatSourceNode>().ConfigureRuntime(label, 8.5f, 3.4f,
                canMeltChests: false, isVillageHub: false);
        }
    }
}
