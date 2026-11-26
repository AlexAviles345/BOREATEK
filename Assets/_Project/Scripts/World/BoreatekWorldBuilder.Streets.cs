using UnityEngine;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;

namespace Boreatek.World
{
    public partial class BoreatekWorldBuilder
    {
        private static readonly Vector2[][] VillageStreetLinks =
        {
            new[] { new Vector2(-13.8f,-8.9f), new Vector2(-22.2f,-9.7f), new Vector2(-27f,-14.8f), new Vector2(-41f,-15f) },
            new[] { new Vector2(13.8f,-8.1f), new Vector2(22.5f,-8.8f), new Vector2(29f,-15f), new Vector2(41f,-15f) },
            new[] { new Vector2(0f,-13.4f), new Vector2(0f,-18.8f), new Vector2(-6.9f,-23f), new Vector2(-7f,-31.1f) },
            new[] { new Vector2(0f,-18.8f), new Vector2(6.9f,-23f), new Vector2(7f,-31.1f) },
            new[] { new Vector2(-15f,10f), new Vector2(-18.6f,11.1f), new Vector2(-24.8f,13f), new Vector2(-28f,23.9f), new Vector2(-41f,24f) },
            new[] { new Vector2(14.8f,7.7f), new Vector2(20.5f,11.1f), new Vector2(26.7f,11.6f), new Vector2(29.1f,24f), new Vector2(41f,24f) }
        };

        private void BuildVillageStreets(Transform campus, FoundryMaterials m)
        {
            Transform neighborhood = CharacterPart(campus, "Village_Through_Streets", Vector3.zero);
            var paving = new Batch();
            foreach (Vector2[] link in VillageStreetLinks)
            {
                for (int i = 1; i < link.Length; i++) Pave(paving, link[i - 1], link[i], 3.8f, m);
            }
            _landscape.Place(neighborhood, "Village_Paved_Lanes", paving, collision: true);

            var furnishings = new Batch();
            Vector2[] lamps =
            {
                new Vector2(-23.8f,-12.8f), new Vector2(24.1f,-12.1f), new Vector2(-35.9f,-17.7f),
                new Vector2(35.7f,-18.0f), new Vector2(-3.0f,-18.7f), new Vector2(8.8f,-26.9f),
                new Vector2(-9.3f,-27.0f), new Vector2(-27.4f,16.9f), new Vector2(29.9f,18.1f),
                new Vector2(-43.4f,-3.0f), new Vector2(43.6f,-2.4f), new Vector2(-42.9f,11.8f),
                new Vector2(43.3f,11.0f), new Vector2(-9.3f,-43.3f), new Vector2(9.2f,-43.8f)
            };
            for (int i = 0; i < lamps.Length; i++)
            {
                Vector3 foot = SurfacePoint(lamps[i].x, lamps[i].y);
                AddStreetLantern(furnishings, foot, i % 2 == 0, m);
                if (i % 2 == 0)
                    AddFoundryLight(neighborhood, foot + new Vector3(-0.61f, 2.68f, 0f), 1.35f, 6f);
            }

            var restStops = new (Vector2 point, float facing)[]
            {
                (new Vector2(-25.4f,-17.2f), 22f), (new Vector2(25.4f,-17.6f), -22f),
                (new Vector2(-9.8f,-31.3f), 90f), (new Vector2(9.8f,-30.2f), -90f),
                (new Vector2(-43.9f,20.2f), 90f), (new Vector2(43.5f,20.6f), -90f)
            };
            foreach (var stop in restStops)
                AddStreetBench(furnishings, SurfacePoint(stop.point.x, stop.point.y), stop.facing, m);
            _landscape.Place(neighborhood, "Street_Lights_Benches_And_Stone_Bases", furnishings, collision: true);

            var details = new Batch();
            AddStreetGarland(neighborhood, details, new Vector2(-4.15f,-16.2f), new Vector2(4.15f,-16.2f), m);
            AddStreetGarland(neighborhood, details, new Vector2(-32.9f,-17.55f), new Vector2(-32.9f,-12.6f), m);
            AddStreetGarland(neighborhood, details, new Vector2(32.2f,-17.55f), new Vector2(32.2f,-12.6f), m);
            _landscape.Place(neighborhood, "Warm_Bulbs_And_Bunting", details);

            Vector2[] trees =
            {
                new Vector2(-26.8f,-20.5f), new Vector2(-22.15f,-20.4f), new Vector2(25.9f,-20.5f),
                new Vector2(21.5f,-19.9f), new Vector2(-5.2f,-29.0f), new Vector2(5.2f,-29.3f),
                new Vector2(-9.8f,-46.6f), new Vector2(9.8f,-47.4f), new Vector2(-44.3f,20f),
                new Vector2(46f,20.9f), new Vector2(-29.2f,5.7f), new Vector2(30.1f,6.2f)
            };
            for (int i = 0; i < trees.Length; i++)
                SpawnSceneryPine(neighborhood, SurfacePoint(trees[i].x, trees[i].y), 3.05f + (i % 4) * 0.41f,
                    $"street:{i}", birch: i % 3 == 0);
        }

        private static void AddStreetLantern(Batch batch, Vector3 foot, bool left, FoundryMaterials m)
        {
            Vector3 hand = (left ? Vector3.left : Vector3.right) * 0.61f;
            batch.Frustum(foot - Vector3.up * 0.12f, foot + Vector3.up * 0.30f, 0.30f, 0.21f, 7, m.Stone);
            batch.Frustum(foot + Vector3.up * 0.27f, foot + Vector3.up * 2.98f, 0.14f, 0.086f, 7, m.Iron);
            Beam(batch, foot + Vector3.up * 2.89f, foot + Vector3.up * 2.89f + hand, 0.092f, m.Copper);
            Beam(batch, foot + Vector3.up * 2.58f, foot + Vector3.up * 2.89f + hand * 0.79f, 0.071f, m.Copper);
            AddFoundryLantern(batch, foot + Vector3.up * 2.64f + hand, 0.86f, m);
        }

        private static void AddStreetBench(Batch batch, Vector3 foot, float facing, FoundryMaterials m)
        {
            Quaternion frame = Quaternion.Euler(0f, facing, 0f);
            batch.Box(foot + Vector3.up * 0.40f, new Vector3(1.64f, 0.12f, 0.51f), m.Timber, frame);
            batch.Box(foot + frame * new Vector3(0f, 0.66f, 0.22f), new Vector3(1.64f, 0.31f, 0.13f), m.Timber, frame);
            foreach (float x in new[] { -0.59f, 0.59f })
                batch.Box(foot + frame * new Vector3(x, 0.18f, 0.04f), new Vector3(0.13f, 0.40f, 0.44f), m.Iron, frame);
            batch.Box(foot + frame * new Vector3(0.25f, 0.48f, 0.10f), new Vector3(0.67f, 0.056f, 0.28f), m.Snow, frame);
        }

        private void AddStreetGarland(Transform parent, Batch batch, Vector2 left, Vector2 right, FoundryMaterials m)
        {
            Vector3 a = SurfacePoint(left.x, left.y), b = SurfacePoint(right.x, right.y);
            var poles = new Batch();
            foreach (Vector3 foot in new[] { a, b })
            {
                poles.Frustum(foot - Vector3.up * 0.12f, foot + Vector3.up * 0.27f, 0.25f, 0.20f, 7, m.Stone);
                poles.Frustum(foot + Vector3.up * 0.24f, foot + Vector3.up * 3.81f, 0.12f, 0.079f, 7, m.Timber);
            }
            _landscape.Place(parent, "Garland_Anchors", poles, collision: true);
            Vector3 previous = a + Vector3.up * 3.73f;
            for (int bulb = 1; bulb <= 12; bulb++)
            {
                float t = bulb / 12f;
                Vector3 point = Vector3.Lerp(a, b, t) + Vector3.up * (3.73f - Mathf.Sin(t * Mathf.PI) * 0.37f);
                Beam(batch, previous, point, 0.037f, m.Iron);
                if (bulb % 2 == 1)
                    batch.Frustum(point + Vector3.down * 0.17f, point, 0.081f, 0.058f, 6, m.Hearth);
                previous = point;
            }
        }
    }
}
