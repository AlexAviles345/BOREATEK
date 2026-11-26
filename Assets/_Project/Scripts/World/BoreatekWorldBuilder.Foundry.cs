using UnityEngine;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;

namespace Boreatek.World
{
    public partial class BoreatekWorldBuilder
    {
        private const float FoundryFloor = 0.24f;

        private sealed class FoundryMaterials
        {
            internal readonly Material Stone, Timber, Roof, Snow, Iron, Copper, Hearth, Window, Ice, Banner, Stitch;

            internal FoundryMaterials(BoreatekLandscapeGeometry geometry)
            {
                Stone = geometry.Finish(new Color32(71, 87, 105, 255));
                Timber = geometry.Finish(new Color32(113, 66, 47, 255));
                Roof = geometry.Finish(new Color32(39, 57, 79, 255));
                Snow = geometry.Finish(Frost);
                Iron = geometry.Finish(new Color32(51, 62, 76, 255), smoothness: 0.26f, metallic: 0.15f);
                Copper = geometry.Finish(Brass, smoothness: 0.34f, metallic: 0.22f);
                Hearth = geometry.Finish(new Color32(255, 155, 48, 255), 1.25f);
                Window = geometry.Finish(new Color32(116, 194, 224, 255), 0.35f);
                Ice = geometry.Finish(new Color32(52, 133, 171, 255), smoothness: 0.43f);
                Banner = geometry.Finish(new Color32(129, 39, 46, 255));
                Stitch = geometry.Finish(new Color32(214, 159, 81, 255));
            }
        }

        private void BuildFoundryCampus(Transform village)
        {
            var m = new FoundryMaterials(_landscape);
            Transform campus = CharacterPart(village, "Foundry_Campus", Vector3.zero);
            BuildFoundryApproach(campus, m);
            BuildFoundryTower(campus, "TwinTower_West", -8.7f, 16.2f, m);
            BuildFoundryTower(campus, "TwinTower_East", 8.7f, 18.1f, m);

            var pipes = new Batch();
            Vector3 west = new Vector3(-7.3f, 10.25f, 13.50f);
            Vector3 east = new Vector3(7.2f, 13.25f, 13.78f);
            Vector3 elbow = new Vector3(-5f, east.y, east.z);
            pipes.Frustum(west, new Vector3(elbow.x, west.y + 0.48f, elbow.z), 0.42f, 0.42f, 12, m.Copper);
            pipes.Frustum(new Vector3(elbow.x, west.y, elbow.z), elbow, 0.44f, 0.44f, 12, m.Copper);
            pipes.Frustum(elbow, east, 0.44f, 0.44f, 12, m.Copper);
            for (int i = 0; i < 8; i++)
            {
                Vector3 collar = Vector3.Lerp(elbow, east, i / 7f);
                pipes.Frustum(collar - Vector3.right * 0.072f, collar + Vector3.right * 0.072f, 0.51f, 0.51f, 12, m.Iron);
            }
            _landscape.Place(campus, "Overhead_Copper_Steam_Main", pipes);

            BuildForgeHall(campus, m);
            BuildFoundryWing(campus, "Herreria", new Vector3(-9.15f, 0f, 3.25f), 6.4f, 7.7f, -1, m);
            BuildFoundryWing(campus, "Almacen", new Vector3(9.15f, 0f, 3.25f), 6.4f, 7.7f, 1, m);
            BuildFoundryWing(campus, "Sala_De_Vapor", new Vector3(10.05f, 0f, 10.0f), 5.2f, 5.4f, 1, m);
            var gallery = new Batch();
            gallery.Box(new Vector3(6.68f, FoundryFloor - 0.11f, 9.56f), new Vector3(1.85f, 0.22f, 2.79f), m.Iron);
            foreach (float z in new[] { 8.20f, 10.93f })
            {
                Wall(gallery, true, z, 5.87f, 7.49f, FoundryFloor, 2.75f, m.Timber);
                AddFoundryWindow(gallery, new Vector3(6.68f, 1.82f, z), 0.76f, 0.72f, Quaternion.identity, m);
            }
            gallery.Box(new Vector3(6.68f, 2.81f, 9.56f), new Vector3(1.94f, 0.19f, 3.04f), m.Roof);
            _landscape.Place(campus, "Walkway_To_Steam_Room", gallery, collision: true);
            BuildFoundryIceOutcrops(campus, m);
            AddFoundryStations(campus, m);
            BuildVillageNeighborhood(campus, m);
        }

        private void BuildFoundryApproach(Transform campus, FoundryMaterials m)
        {
            var ice = new Batch();
            var bridge = new Batch();
            var details = new Batch();
            for (int i = 0; i < 8; i++)
            {
                float x = -14f + i * 4f;
                ice.Box(new Vector3(x, -0.055f, -16.05f), new Vector3(4.03f, 0.14f, 3.65f + i % 3 * 0.42f),
                    i % 3 == 0 ? m.Window : m.Ice, Quaternion.Euler(0f, (i % 3 - 1) * 6f, 0f));
                float bank = -18.52f - i % 3 * 0.24f;
                if (Mathf.Abs(x) > 2f)
                {
                    ice.Frustum(new Vector3(x, -0.06f, bank), new Vector3(x + 0.4f, 0.23f, bank - 0.3f),
                        2.4f, 1.37f, 6, m.Snow);
                    AddCrystal(ice, new Vector3(x, 0.03f, -14.14f), new Vector3(x + 0.24f, 0.7f + i % 3 * 0.16f, -13.93f),
                        0.27f, m.Ice, m.Window);
                }
            }
            for (int i = 0; i < 17; i++)
            {
                float z = -20.1f + i * 0.4f;
                float y = 0.14f + Mathf.Min(3, Mathf.Min(i, 16 - i)) * 0.1f;
                bridge.Box(new Vector3(0f, y - 0.09f, z), new Vector3(3.9f, 0.18f, 0.396f), i % 5 == 0 ? m.Stone : m.Timber);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 1.83f;
                for (int i = 0; i < 4; i++)
                {
                    float z = -19.75f + i * 1.91f;
                    bridge.Box(new Vector3(x, 0.54f, z), new Vector3(0.25f, 1.10f, 0.25f), m.Timber);
                    details.Box(new Vector3(x, 1.08f, z), new Vector3(0.35f, 0.11f, 0.35f), m.Iron);
                    if (i == 0 || i == 3) AddFoundryLantern(details, new Vector3(x, 1.34f, z), 0.75f, m);
                }
                bridge.Box(new Vector3(x, 0.94f, -16.89f), new Vector3(0.13f, 0.16f, 5.85f), m.Timber);
                Beam(details, new Vector3(x, 0.02f, -18.7f), new Vector3(x, 0.33f, -16.7f), 0.20f, m.Timber);
                Beam(details, new Vector3(x, 0.33f, -16.7f), new Vector3(x, 0.02f, -14.7f), 0.20f, m.Timber);
                AddBanner(details, new Vector3(side * 3.42f, 4.24f, 0.24f), 0.74f, 1.65f, m);
            }
            _landscape.Place(campus, "Frozen_Channel_And_Ice_Banks", ice);
            _landscape.Place(campus, "Entrance_Bridge", bridge, collision: true);
            _landscape.Place(campus, "Bridge_Lanterns_And_Banners", details);
        }

        private void BuildFoundryIceOutcrops(Transform campus, FoundryMaterials m)
        {
            var cliff = new Batch();
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 5; i++)
                {
                    float x = side * (25.6f + i * 0.65f);
                    float z = 5.9f + i * 2.17f;
                    Vector3 foot = SurfacePoint(x, z) - Vector3.up * 0.35f;
                    Vector3 ridge = foot + new Vector3(side * 0.82f, 1.9f + i * 0.55f, 0.21f);
                    cliff.Frustum(foot, ridge, 1.54f + i * 0.12f, 0.72f, 6, i % 2 == 0 ? m.Ice : m.Stone);
                    cliff.Frustum(ridge, ridge + new Vector3(side * 0.18f, 0.55f, 0.11f), 0.74f, 0.25f, 6, m.Snow);
                    AddCrystal(cliff, foot + new Vector3(-side * 1.04f, 0.32f, -0.32f),
                        foot + new Vector3(-side * 1.48f, 0.94f + i * 0.17f, -0.18f), 0.32f, m.Ice, m.Window);
                }
            }
            _landscape.Place(campus, "Cyan_Ice_And_Snow_Outcrops", cliff, collision: true);
        }

        private void BuildFoundryTower(Transform campus, string name, float x, float height, FoundryMaterials m)
        {
            Transform tower = CharacterPart(campus, name, new Vector3(x, 0f, 14f));
            var shell = new Batch();
            var trim = new Batch();
            shell.Frustum(Vector3.zero, Vector3.up * 2.3f, 2.88f, 2.66f, 12, m.Stone);
            shell.Frustum(Vector3.up * 2.3f, Vector3.up * 6.7f, 2.57f, 2.15f, 12, m.Iron);
            shell.Frustum(Vector3.up * 6.7f, Vector3.up * 10.2f, 2.15f, 1.55f, 12, m.Iron);
            shell.Frustum(Vector3.up * 10.2f, Vector3.up * (height - 1.5f), 1.55f, 1.53f, 12, m.Iron);
            shell.Frustum(Vector3.up * (height - 1.5f), Vector3.up * height, 0.92f, 0.92f, 12, m.Iron);
            foreach (float y in new[] { 2.38f, 6.74f, 10.22f, height - 2.65f, height - 1.59f, height })
            {
                float radius = y < 5f ? 2.71f : y < 8f ? 2.28f : y < height - 1f ? 1.71f : 1.03f;
                trim.Frustum(Vector3.up * (y - 0.13f), Vector3.up * (y + 0.13f), radius, radius, 12, m.Copper);
            }
            AddKilnFace(trim, new Vector3(0f, 3.75f, -2.45f), 1.36f, m);
            AddGear(trim, new Vector3(-1.35f, 6.56f, -2.04f), 0.69f, m.Copper, m.Iron);
            AddGear(trim, new Vector3(1.07f, 5.96f, -2.22f), 0.54f, m.Copper, m.Iron);
            for (int i = -2; i <= 2; i++)
            {
                trim.Box(new Vector3(i * 0.29f, 7.68f, -1.98f), new Vector3(0.17f, 1.0f, 0.18f), m.Hearth);
                trim.Box(new Vector3(i * 0.26f, height - 2.08f, -1.51f), new Vector3(0.12f, 0.63f, 0.20f), m.Hearth);
            }
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f;
                Vector3 direction = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                Vector3 following = Quaternion.Euler(0f, a + 30f, 0f) * Vector3.forward;
                Vector3 balcony = Vector3.up * (height - 2.62f);
                trim.Box(balcony + direction * 1.78f + Vector3.up * 0.36f, new Vector3(0.10f, 0.74f, 0.10f), m.Copper);
                Beam(trim, balcony + direction * 1.78f + Vector3.up * 0.74f,
                    balcony + following * 1.78f + Vector3.up * 0.74f, 0.085f, m.Copper);
                Beam(trim, direction * 2.60f + Vector3.up * 2.35f, direction * 1.55f + Vector3.up * 10.22f, 0.13f, m.Copper);
            }
            AddBanner(trim, new Vector3(2.29f * Mathf.Sign(x), height - 4.63f, -0.8f), 0.63f, 1.84f, m);
            _landscape.Place(tower, "Riveted_Furnace_Shell", shell, collision: true);
            _landscape.Place(tower, "Copper_Rings_Gears_And_Glow", trim);
            AddChimneySteam(tower, height);
        }

        private static void AddKilnFace(Batch batch, Vector3 center, float radius, FoundryMaterials m)
        {
            Vector3 front = center - Vector3.forward * 0.22f;
            batch.Box(center + Vector3.down * 0.37f * radius, new Vector3(radius * 2.12f, radius * 0.82f, 0.52f), m.Iron);
            batch.Box(front + Vector3.down * 0.29f * radius, new Vector3(radius * 1.44f, radius * 0.65f, 0.10f), m.Hearth);
            for (int i = 0; i < 12; i++)
            {
                float a = Mathf.PI * i / 12f, b = Mathf.PI * (i + 1) / 12f;
                Vector3 u = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                Vector3 v = new Vector3(Mathf.Cos(b), Mathf.Sin(b), 0f);
                batch.Triangle(front, front + v * radius * 0.73f, front + u * radius * 0.73f, m.Hearth);
                Beam(batch, front + u * radius * 0.84f, front + v * radius * 0.84f, radius * 0.23f,
                    i % 3 == 0 ? m.Iron : m.Copper);
            }
            for (int side = -1; side <= 1; side += 2)
                batch.Box(front + new Vector3(side * radius * 0.84f, -radius * 0.37f, 0f),
                    new Vector3(radius * 0.23f, radius * 0.85f, 0.28f), m.Copper);
        }

        private static void AddGear(Batch batch, Vector3 center, float radius, Material rim, Material hub)
        {
            batch.Ring(center, radius * 0.69f, radius * 0.89f, 20, rim);
            batch.Frustum(center + Vector3.forward * 0.06f, center - Vector3.forward * 0.14f,
                radius * 0.23f, radius * 0.22f, 10, hub);
            for (int i = 0; i < 12; i++)
            {
                Quaternion turn = Quaternion.Euler(0f, 0f, i * 30f);
                Vector3 radial = turn * Vector3.up;
                batch.Box(center + radial * (radius * 0.94f), new Vector3(radius * 0.25f, radius * 0.20f, 0.15f), rim, turn);
                if (i % 2 == 0)
                    batch.Box(center + radial * (radius * 0.42f), new Vector3(radius * 0.14f, radius * 0.71f, 0.13f), rim, turn);
            }
        }

        private static void Beam(Batch batch, Vector3 from, Vector3 to, float width, Material material)
        {
            Vector3 direction = to - from;
            batch.Box((from + to) * 0.5f, new Vector3(width, direction.magnitude, width), material,
                Quaternion.FromToRotation(Vector3.up, direction));
        }

        private static void AddFoundryLantern(Batch batch, Vector3 center, float scale, FoundryMaterials m)
        {
            batch.Box(center, new Vector3(0.24f, 0.35f, 0.24f) * scale, m.Hearth);
            batch.Box(center + Vector3.up * (0.21f * scale), new Vector3(0.36f, 0.12f, 0.36f) * scale, m.Iron);
            batch.Box(center - Vector3.up * (0.2f * scale), new Vector3(0.30f, 0.10f, 0.30f) * scale, m.Iron);
            for (int side = -1; side <= 1; side += 2)
            {
                batch.Box(center + new Vector3(side * 0.133f, 0f, 0.133f) * scale, new Vector3(0.047f, 0.4f, 0.047f) * scale, m.Copper);
                batch.Box(center + new Vector3(side * 0.133f, 0f, -0.133f) * scale, new Vector3(0.047f, 0.4f, 0.047f) * scale, m.Copper);
            }
        }

        private static void AddBanner(Batch batch, Vector3 top, float width, float height, FoundryMaterials m)
        {
            Vector3 a = top - Vector3.right * width * 0.5f, b = top + Vector3.right * width * 0.5f;
            Vector3 c = b - Vector3.up * height, d = a - Vector3.up * height;
            Vector3 nose = (c + d) * 0.5f + Vector3.up * (width * 0.28f);
            batch.Triangle(a, b, nose, m.Banner);
            batch.Triangle(a, nose, d, m.Banner);
            batch.Triangle(b, c, nose, m.Banner);
            Vector3 inset = Vector3.down * (height * 0.43f) + Vector3.back * 0.023f;
            Beam(batch, a + inset, nose + Vector3.up * height * 0.36f + Vector3.back * 0.023f, 0.066f, m.Stitch);
            Beam(batch, b + inset, nose + Vector3.up * height * 0.36f + Vector3.back * 0.023f, 0.066f, m.Stitch);
            Beam(batch, a - Vector3.right * 0.13f, b + Vector3.right * 0.13f, 0.098f, m.Copper);
        }
    }
}
