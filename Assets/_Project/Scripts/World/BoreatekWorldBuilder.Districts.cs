using System.Collections.Generic;
using UnityEngine;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;

namespace Boreatek.World
{
    public partial class BoreatekWorldBuilder
    {
        private static readonly Rect[] Homes = MakeHomePlots();
        private static readonly Rect[] FactoryWings =
        {
            CenteredPlot(-17.9f, 23f, 12.4f, 12.8f),
            CenteredPlot(19.3f, 23f, 12.2f, 13.6f),
            CenteredPlot(25.8f, 2.2f, 8.2f, 10.4f)
        };
        private static readonly Color32[] WorkerCoats =
        {
            new(86, 109, 65, 255), new(136, 71, 62, 255), new(80, 105, 129, 255),
            new(122, 83, 116, 255), new(151, 108, 63, 255), new(61, 109, 110, 255)
        };

        internal static IEnumerable<Rect> ExpandedVillageFootprints
        {
            get
            {
                foreach (Rect wing in FactoryWings) yield return wing;
                foreach (Rect home in Homes) yield return home;
            }
        }

        private static Rect[] MakeHomePlots()
        {
            var plots = new Rect[30];
            var layout = new (float x, float z)[]
            {
                (-34f, -23f), (36f, -23f), (-28f, -35f)
            };
            for (int district = 0; district < layout.Length; district++)
            {
                for (int i = 0; i < 10; i++)
                {
                    float x = layout[district].x + (district == 2 ? i % 5 * 14f : i % 2 * (district == 0 ? -14f : 14f));
                    float z = layout[district].z + (district == 2 ? i / 5 * -14f : i / 2 * 13f);
                    float width = 6.4f + (i + district) % 3 * 0.55f;
                    float depth = 6.15f + (i * 2 + district) % 3 * 0.45f;
                    plots[district * 10 + i] = CenteredPlot(x, z, width, depth);
                }
            }
            return plots;
        }

        private static Rect CenteredPlot(float x, float z, float width, float depth) =>
            new Rect(x - width * 0.5f, z - depth * 0.5f, width, depth);

        internal static bool IsVillageGround(float x, float z)
        {
            if (x * x + z * z <= 400f) return true;
            Vector2 point = new Vector2(x, z);
            foreach (Rect home in Homes)
                if (InsidePlot(home, point, 3.2f)) return true;
            foreach (Rect wing in FactoryWings)
                if (InsidePlot(wing, point, 5.2f)) return true;
            return false;
        }

        private static bool InsidePlot(Rect plot, Vector2 point, float margin) =>
            point.x >= plot.xMin - margin && point.x <= plot.xMax + margin &&
            point.y >= plot.yMin - margin && point.y <= plot.yMax + margin;

        private static Vector3 KeepClearOfVillageBuildings(Vector3 position)
        {
            Vector2 point = new Vector2(position.x, position.z);
            foreach (Rect plot in ExpandedVillageFootprints)
            {
                const float clearance = 2.0f;
                float doorOffset = point.x - plot.center.x;
                if (Mathf.Abs(doorOffset) <= 3.1f && point.y >= plot.yMin - 6.3f && point.y <= plot.yMin + 1f)
                {
                    point.x = doorOffset <= 0f ? plot.xMin - clearance - 0.25f : plot.xMax + clearance + 0.25f;
                    continue;
                }
                if (!InsidePlot(plot, point, clearance)) continue;
                float west = point.x - plot.xMin, east = plot.xMax - point.x;
                float south = point.y - plot.yMin, north = plot.yMax - point.y;
                float nearest = Mathf.Min(Mathf.Min(west, east), Mathf.Min(south, north));
                if (nearest == west) point.x = plot.xMin - clearance - 0.25f;
                else if (nearest == east) point.x = plot.xMax + clearance + 0.25f;
                else if (nearest == south) point.y = plot.yMin - clearance - 0.25f;
                else point.y = plot.yMax + clearance + 0.25f;
            }
            return point.x == position.x && point.y == position.z ? position : SurfacePoint(point.x, point.y);
        }

        private void BuildVillageDistricts(Transform campus, FoundryMaterials m)
        {
            string[] districts = { "Barrio_De_Los_Abedules", "Barrio_De_Los_Talleres", "Barrio_Del_Puente" };
            string[] signs = { "ABEDUL", "TALLERES", "PUENTE" };
            var roads = new Batch();
            for (int district = 0; district < districts.Length; district++)
            {
                Transform block = CharacterPart(campus, districts[district], Vector3.zero);
                var homeMaterials = new FoundryMaterials(_landscape, district + 1);
                for (int number = 0; number < 10; number++)
                {
                    int index = district * 10 + number;
                    Rect plot = Homes[index];
                    Vector2 center = plot.center;
                    Transform home = BuildFoundryWing(block, $"Casa_{index + 1:D2}", new Vector3(center.x, 0f, center.y),
                        plot.width, plot.height, center.x < 0f ? -1 : 1, homeMaterials, $"{signs[district]} {number + 1:D2}",
                        lightInterior: number % 5 == 1);
                    FurnishDistrictHome(home, plot, index, homeMaterials);
                    Vector2 porch = new Vector2(center.x, plot.yMin - 0.95f);
                    Pave(roads, porch + Vector2.up * 0.20f, porch + Vector2.down * 1.8f, 2.2f, m);
                    if (number % 3 == 2 || number == 9) continue;
                    var job = (FoundryWorker.Job)((index + district) % WorkerCoats.Length);
                    SpawnFoundryWorker(home, $"Duende_{signs[district]}_{number + 1:D2}", job,
                        WorkerCoats[(index * 5 + district) % WorkerCoats.Length],
                        new Vector3(0.20f, FoundryFloor, 0.06f), 0f, (index * 37 % 97) / 97f, m,
                        rest: new Vector3(porch.x + (number % 2 == 0 ? 2.55f : -2.55f), 0.10f, porch.y - 1.4f));
                }
                PaveDistrictRoads(roads, district, m);
            }
            _landscape.Place(campus, "Village_Paved_Lanes", roads, collision: true);
            BuildExpandedFactory(campus, m);
        }

        private void FurnishDistrictHome(Transform home, Rect plot, int index, FoundryMaterials m)
        {
            var fixtures = new Batch();
            AddWorkbench(fixtures, new Vector3(0f, FoundryFloor, 1.03f), 2.2f, m);
            AddCrate(fixtures, new Vector3(plot.width * 0.32f, FoundryFloor, 1.27f), 0.64f, m);
            Vector3 bed = new Vector3(-plot.width * 0.32f, FoundryFloor + 0.28f, 1.64f);
            fixtures.Box(bed, new Vector3(1.13f, 0.48f, 1.96f), m.Timber);
            fixtures.Box(bed + Vector3.up * 0.28f, new Vector3(1.06f, 0.16f, 1.83f), index % 2 == 0 ? m.Banner : m.Roof);
            fixtures.Box(bed + new Vector3(0f, 0.38f, 0.66f), new Vector3(0.89f, 0.16f, 0.39f), m.Snow);
            float chimney = plot.width * 0.28f;
            fixtures.Box(new Vector3(chimney, 4.15f, 1.85f), new Vector3(0.69f, 2.55f, 0.70f), m.Stone);
            fixtures.Box(new Vector3(chimney, 5.45f, 1.85f), new Vector3(0.85f, 0.17f, 0.86f), m.Copper);
            if (index % 3 == 1)
                AddBanner(fixtures, new Vector3(2.03f, 2.64f, -plot.height * 0.5f - 0.32f), 0.63f, 1.11f, m);
            _landscape.Place(home, "Household_Bench_Bed_And_Chimney", fixtures, collision: true);
        }

        private static void PaveDistrictRoads(Batch roads, int district, FoundryMaterials m)
        {
            if (district == 2)
            {
                foreach (float z in new[] { -39.8f, -53.8f })
                    Pave(roads, new Vector2(-30.8f, z), new Vector2(30.8f, z), 2.42f, m);
                foreach (float x in new[] { -7f, 7f })
                    Pave(roads, new Vector2(x, -17.9f), new Vector2(x, -53.8f), 2.38f, m);
                return;
            }
            float side = district == 0 ? -1f : 1f;
            for (int row = 0; row < 5; row++)
            {
                float z = -28f + row * 13f;
                Pave(roads, new Vector2(side * 20f, z), new Vector2(side * 52f, z), 2.54f, m);
            }
            Pave(roads, new Vector2(side * 41f, -28f), new Vector2(side * 41f, 26.2f), 2.6f, m);
        }

        private void BuildExpandedFactory(Transform campus, FoundryMaterials m)
        {
            string[] names = { "Fundicion", "Nave_De_Montaje", "Muelle_De_Carga" };
            string[] sign = { "FUNDICIÓN", "MONTAJE", "MUELLE DE CARGA" };
            var routes = new Batch();
            for (int i = 0; i < FactoryWings.Length; i++)
            {
                Rect plot = FactoryWings[i];
                Vector2 center = plot.center;
                Transform hall = BuildFoundryWing(campus, names[i], new Vector3(center.x, 0f, center.y),
                    plot.width, plot.height, center.x < 0f ? -1 : 1, m, sign[i], industrial: true);
                float entrance = plot.yMin - 0.85f;
                Pave(routes, new Vector2(center.x, entrance), new Vector2(center.x, i == 2 ? -7.8f : 10.5f), 3.45f, m);
                var machines = new Batch();
                for (int station = -1; station <= 1; station++)
                {
                    float x = station * plot.width * 0.27f;
                    var basePoint = new Vector3(x, FoundryFloor, 1.52f + station % 2 * 1.7f);
                    AddWorkbench(machines, basePoint, 1.8f, m);
                    if (i == 0)
                    {
                        Vector3 kiln = basePoint + new Vector3(0f, 1.08f, 2.12f);
                        machines.Box(kiln, new Vector3(1.75f, 2.14f, 1.42f), m.Iron);
                        AddKilnFace(machines, kiln + Vector3.back * 0.73f, 0.67f, m);
                        machines.Frustum(kiln + Vector3.up * 0.9f, kiln + Vector3.up * 6.7f, 0.34f, 0.29f, 10, m.Copper);
                    }
                    else
                    {
                        AddCrate(machines, basePoint + new Vector3(-0.4f, 0.91f, 0.18f), 0.57f, m);
                        AddCrate(machines, basePoint + Vector3.forward * 2.01f, 0.86f, m);
                    }
                }
                _landscape.Place(hall, "Factory_Production_Stations", machines, collision: true);
                SpawnFoundryWorker(hall, $"Duende_Fabrica_{i + 1}", i == 0 ? FoundryWorker.Job.Smith : FoundryWorker.Job.Packer,
                    WorkerCoats[i], new Vector3(0f, FoundryFloor, 0.57f), 0f, 0.29f + i * 0.17f, m,
                    rest: new Vector3(center.x - 2.0f, 0.10f, entrance - 1.5f));
                if (i == 1)
                    SpawnFoundryWorker(hall, "Duende_Inspector", FoundryWorker.Job.Courier, WorkerCoats[4],
                        new Vector3(3.22f, FoundryFloor, -1.12f), 0f, 0.83f, m,
                        rest: new Vector3(center.x + 2.15f, 0.10f, entrance - 2f));
            }
            Pave(routes, new Vector2(-20f, 11f), new Vector2(-11.4f, 11f), 3.55f, m);
            Pave(routes, new Vector2(12.9f, 11f), new Vector2(20.4f, 11f), 3.55f, m);
            _landscape.Place(campus, "Factory_Service_Routes", routes, collision: true);
        }
    }
}
