using UnityEngine;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;

namespace Boreatek.World
{
    public partial class BoreatekWorldBuilder
    {
        internal static readonly Vector2 AirfieldLocation = new(-55f, -23f);

        internal static bool IsAirfieldClearance(float x, float z) => x is > -120f and < -48f && z is > -35f and < -12f;

        private static float RunwayGrade(float x, float z)
        {
            float west = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-124f, -116f, x));
            float east = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-52f, -45f, x));
            float edge = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5.8f, 11f, Mathf.Abs(z - AirfieldLocation.y)));
            return west * east * edge;
        }

        private void BuildVillageAirfield()
        {
            Transform village = _worldRoot.Find("Biome_01_SantaIndustrialVillage");
            Transform airfield = CharacterPart(village, "Estacion_Aerea_Polar", Vector3.zero);
            var strip = new Batch();
            Material packedSnow = _landscape.Finish(new Color32(139, 167, 182, 255));
            Material white = _landscape.Finish(new Color32(225, 236, 238, 255));
            Material apron = _landscape.Finish(new Color32(70, 86, 100, 255));
            Material yellow = _landscape.Finish(new Color32(218, 158, 84, 255), 0.2f);
            var foundry = new FoundryMaterials(_landscape);
            float runwayZ = AirfieldLocation.y;
            strip.Box(new Vector3(-84f, 0.004f, runwayZ), new Vector3(64f, 0.027f, 10.0f), packedSnow);
            for (int x = -113; x <= -53; x += 6)
                strip.Box(new Vector3(x, 0.025f, runwayZ), new Vector3(2.65f, 0.017f, 0.13f), white);
            foreach (float end in new[] { -114f, -54f })
                for (int lane = -2; lane <= 2; lane++)
                    strip.Box(new Vector3(end, 0.025f, runwayZ + lane * 0.70f), new Vector3(1.45f, 0.019f, 0.20f), white);
            for (int x = -113; x < -53; x += 10)
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector3 p = SurfacePoint(x, runwayZ + side * 5.45f);
                    strip.Box(p + Vector3.up * 0.12f, new Vector3(0.34f, 0.25f, 0.34f), apron);
                    strip.Box(p + Vector3.up * 0.28f, new Vector3(0.25f, 0.14f, 0.25f), yellow);
                }
            strip.Box(new Vector3(-54.7f, 0.035f, -31.3f), new Vector3(8.8f, 0.13f, 6.1f), apron);
            _landscape.Place(airfield, "Pista_Nevada_Y_Balizas", strip);

            Transform service = CharacterPart(airfield, "Cobertizo_De_Combustible", SurfacePoint(-55.5f, -33.8f));
            var station = new Batch();
            station.Box(new Vector3(0f, 1.35f, -0.91f), new Vector3(4.9f, 2.70f, 0.22f), foundry.Timber);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 post = new Vector3(side * 2.30f, 0f, 0.96f);
                Beam(station, post, post + Vector3.up * 2.7f, 0.24f, foundry.Iron);
                station.Box(new Vector3(side * 2.38f, 0.44f, -0.10f), new Vector3(0.18f, 0.88f, 1.77f), foundry.Timber);
                station.Box(new Vector3(side * 1.72f, 2.39f, 0.92f), new Vector3(0.44f, 0.66f, 0.19f), yellow);
            }
            station.Box(new Vector3(0f, 2.74f, -0.03f), new Vector3(5.33f, 0.24f, 2.48f), foundry.Iron, Quaternion.Euler(-8f, 0f, 0f));
            station.Box(new Vector3(0f, 2.91f, -0.03f), new Vector3(5.45f, 0.12f, 2.52f), foundry.Snow, Quaternion.Euler(-8f, 0f, 0f));
            station.Box(new Vector3(0f, 2.10f, 1.02f), new Vector3(2.9f, 0.43f, 0.12f), foundry.Iron);
            _landscape.Place(service, "Cobertizo_Y_Marquesina", station, collision: true);
            Transform stationSign = CharacterPart(service, "Rotulo", new Vector3(0f, 2.08f, 1.10f));
            stationSign.localRotation = Quaternion.Euler(0f, 180f, 0f);
            AddFoundrySign(stationSign, "ESTACION AEREA", Vector3.zero);
            AddFoundryLight(service, new Vector3(0f, 2.30f, 1.3f), 2.45f, 7.3f);

            Transform pump = CharacterPart(service, "Surtidor_De_Gasolina_Polar", new Vector3(-0.83f, 0f, 0.70f));
            var dispenser = new Batch();
            Material red = _landscape.Finish(new Color32(169, 40, 42, 255), smoothness: 0.45f);
            dispenser.Box(new Vector3(0f, 0.105f, 0f), new Vector3(0.65f, 0.21f, 0.60f), foundry.Iron);
            dispenser.Box(new Vector3(0f, 0.71f, 0f), new Vector3(0.52f, 1.14f, 0.45f), red);
            dispenser.Box(new Vector3(0f, 0.99f, 0.232f), new Vector3(0.39f, 0.31f, 0.04f), foundry.Iron);
            dispenser.Box(new Vector3(0f, 1.02f, 0.26f), new Vector3(0.28f, 0.12f, 0.018f), yellow);
            dispenser.Frustum(new Vector3(0.24f, 0.93f, 0f), new Vector3(0.49f, 0.28f, 0.21f), 0.034f, 0.033f, 6, foundry.Iron);
            dispenser.Frustum(new Vector3(0.49f, 0.28f, 0.21f), new Vector3(0.37f, 0.77f, 0.28f), 0.033f, 0.032f, 6, foundry.Iron);
            dispenser.Box(new Vector3(0.37f, 0.79f, 0.29f), new Vector3(0.11f, 0.15f, 0.047f), red);
            _landscape.Place(pump, "Dispensador_Y_Manguera", dispenser, collision: true);
            pump.gameObject.AddComponent<AviationFuelPump>();

            var freight = new Batch();
            foreach (float x in new[] { 0.85f, 1.63f })
            {
                Vector3 tank = new Vector3(x, 0.09f, 0.20f);
                freight.Frustum(tank, tank + Vector3.up * 0.72f, 0.30f, 0.29f, 10, red);
                freight.Frustum(tank + Vector3.up * 0.14f, tank + Vector3.up * 0.21f, 0.313f, 0.313f, 10, foundry.Iron);
                freight.Frustum(tank + Vector3.up * 0.54f, tank + Vector3.up * 0.62f, 0.312f, 0.312f, 10, foundry.Iron);
            }
            _landscape.Place(service, "Barriles_De_Gasolina", freight, collision: true);
            BuildRedBushPlane(airfield);
        }

        private void BuildRedBushPlane(Transform airfield)
        {
            var home = StarterMeshFoot(AirfieldLocation.x, AirfieldLocation.y) + Vector3.up * 0.027f;
            Transform plane = CharacterPart(airfield, "Avioneta_Roja", home);
            plane.localRotation = Quaternion.Euler(0f, 270f, 0f);
            Material red = _landscape.Finish(new Color32(178, 28, 41, 255), smoothness: 0.53f);
            Material bright = _landscape.Finish(new Color32(212, 43, 52, 255), smoothness: 0.48f);
            Material cream = _landscape.Finish(new Color32(228, 221, 194, 255), smoothness: 0.42f);
            Material dark = _landscape.Finish(new Color32(37, 46, 56, 255), smoothness: 0.45f);
            Material steel = _landscape.Finish(new Color32(106, 126, 139, 255), smoothness: 0.53f);
            Material glass = _landscape.Finish(new Color32(47, 95, 118, 255), smoothness: 0.78f);
            var hull = new Batch();
            BuildPlaneHull(hull, red, bright, cream);
            Vector3 rearLeft = new(-0.44f, 1.56f, -0.62f), rearRight = new(0.44f, 1.56f, -0.62f);
            Vector3 windshieldLeft = new(-0.52f, 1.56f, 1.17f), windshieldRight = new(0.52f, 1.56f, 1.17f);
            Vector3 cabinTop = new(0f, 2.25f, -0.32f), windTop = new(0f, 2.24f, 0.66f);
            var windows = new Batch();
            windows.Quad(windshieldLeft, windshieldRight, windTop + Vector3.right * 0.43f, windTop - Vector3.right * 0.43f, glass);
            foreach (int side in new[] { -1, 1 })
            {
                Vector3 rear = side < 0 ? rearLeft : rearRight;
                Vector3 front = side < 0 ? windshieldLeft : windshieldRight;
                Vector3 topRear = cabinTop + Vector3.right * (side * 0.43f);
                Vector3 topFront = windTop + Vector3.right * (side * 0.43f);
                if (side < 0) windows.Quad(rear, front, topFront, topRear, glass);
                else windows.Quad(front, rear, topRear, topFront, glass);
                Beam(hull, rear, topRear, 0.075f, red);
                Beam(hull, front, topFront, 0.082f, red);
                Beam(hull, topRear, topFront, 0.09f, red);
                Beam(hull, rear, front, 0.083f, cream);
                Vector3 frameBase = Vector3.Lerp(rear, front, 0.48f);
                Beam(hull, frameBase, Vector3.Lerp(topRear, topFront, 0.48f), 0.065f, red);
                hull.Box(frameBase + Vector3.right * (side * 0.055f) + Vector3.down * 0.10f,
                    new Vector3(0.04f, 0.04f, 0.21f), steel);
                Beam(hull, new Vector3(side * 0.36f, 0.58f, 0.13f), new Vector3(side * 2.74f, 2.20f, 0.39f), 0.075f, cream);
                Beam(hull, new Vector3(side * 0.3f, 0.66f, 0.52f), new Vector3(side * 0.82f, 0.19f, 0.69f), 0.10f, steel);
                Beam(hull, new Vector3(side * 0.32f, 0.63f, -0.24f), new Vector3(side * 0.82f, 0.19f, 0.69f), 0.08f, steel);
                hull.Box(new Vector3(side * 0.82f, 0.088f, 0.66f), new Vector3(0.30f, 0.13f, 1.36f), dark);
                Beam(hull, new Vector3(side * 0.82f, 0.11f, 1.31f), new Vector3(side * 0.82f, 0.245f, 1.61f), 0.176f, cream);
                hull.Box(new Vector3(side * 0.82f, 0.20f, 0.67f), new Vector3(0.22f, 0.21f, 0.39f), steel);
            }
            Beam(hull, windshieldLeft, windshieldRight, 0.072f, cream);
            Beam(hull, windTop - Vector3.right * 0.46f, windTop + Vector3.right * 0.46f, 0.087f, red);
            hull.Box(new Vector3(0f, 2.219f, 0.11f), new Vector3(0.91f, 0.08f, 1.05f), red);
            hull.Box(new Vector3(0f, 1.43f, 0.84f), new Vector3(0.82f, 0.16f, 0.49f), dark);
            for (int dial = -1; dial <= 1; dial++)
                hull.Frustum(new Vector3(dial * 0.20f, 1.465f, 0.58f), new Vector3(dial * 0.20f, 1.465f, 0.56f),
                    0.060f, 0.058f, 10, cream);
            hull.Box(new Vector3(0f, 0.88f, -0.29f), new Vector3(0.55f, 0.12f, 0.55f), dark);
            hull.Box(new Vector3(0f, 1.17f, -0.50f), new Vector3(0.54f, 0.58f, 0.10f), dark);
            Beam(hull, new Vector3(0f, 0.40f, -2.65f), new Vector3(0f, 0.115f, -2.88f), 0.10f, steel);
            hull.Box(new Vector3(0f, 0.065f, -2.88f), new Vector3(0.17f, 0.087f, 0.57f), dark);
            _landscape.Place(plane, "Fuselaje_Struts_Cabina_Y_Esquis", hull);
            Renderer glazing = _landscape.Place(plane, "Cristales_De_Cabina", windows).GetComponent<Renderer>();
            Transform seat = CharacterPart(plane, "Asiento_De_Piloto", new Vector3(0f, 0.93f, -0.19f));

            var wings = new Batch();
            AddPlaneWing(wings, 0.0f, 1.07f, -0.51f, 4.04f, 0.66f, -0.63f, 2.26f, 0.13f, bright, cream);
            AddPlaneWing(wings, 0.0f, -2.20f, -2.87f, 1.36f, -2.55f, -3.02f, 1.12f, 0.082f, red, cream);
            AddFin(wings, new Vector3(0f, 1.15f, -2.1f), new Vector3(0f, 2.24f, -2.75f),
                new Vector3(0f, 2.10f, -3.03f), new Vector3(0f, 1.1f, -3.08f), 0.055f, red);
            Material port = _landscape.Finish(new Color32(238, 70, 69, 255), 0.95f);
            Material starboard = _landscape.Finish(new Color32(69, 227, 132, 255), 0.95f);
            wings.Ellipsoid(new Vector3(-3.97f, 2.32f, 0.13f), new Vector3(0.11f, 0.07f, 0.15f), 8, 5, port);
            wings.Ellipsoid(new Vector3(3.97f, 2.32f, 0.13f), new Vector3(0.11f, 0.07f, 0.15f), 8, 5, starboard);
            _landscape.Place(plane, "Alas_Estabilizador_Y_Luces", wings);
            Transform left = MakePlaneSurface(plane, "Aleron_Izquierdo", new Vector3(-2.97f, 2.26f, -0.55f), new Vector3(1.67f, 0.12f, 0.37f), red);
            Transform right = MakePlaneSurface(plane, "Aleron_Derecho", new Vector3(2.97f, 2.26f, -0.55f), new Vector3(1.67f, 0.12f, 0.37f), red);
            Transform elevator = MakePlaneSurface(plane, "Elevador", new Vector3(0f, 1.12f, -2.91f), new Vector3(2.45f, 0.09f, 0.32f), cream);
            Transform rudder = MakePlaneSurface(plane, "Timon", new Vector3(0f, 1.62f, -3.0f), new Vector3(0.10f, 0.91f, 0.28f), bright);

            Transform propeller = CharacterPart(plane, "Helice", new Vector3(0f, 1.14f, 2.79f));
            var blades = new Batch();
            blades.Frustum(Vector3.back * 0.12f, Vector3.forward * 0.22f, 0.21f, 0.058f, 10, cream);
            foreach (int direction in new[] { -1, 1 })
            {
                blades.Box(new Vector3(0.032f * direction, direction * 0.47f, 0f), new Vector3(0.18f, 0.69f, 0.06f),
                    dark, Quaternion.Euler(0f, 9f, 7f));
                blades.Box(new Vector3(-0.02f * direction, direction * 0.84f, 0f), new Vector3(0.13f, 0.175f, 0.049f),
                    cream, Quaternion.Euler(0f, 9f, 7f));
            }
            _landscape.Place(propeller, "Palas_Y_Cono_De_Helice", blades);
            AddPlaneCollider(plane, new Vector3(0f, 1.07f, 0.20f), new Vector3(1.10f, 1.36f, 4.92f));
            AddPlaneCollider(plane, new Vector3(0f, 2.20f, 0.24f), new Vector3(7.92f, 0.25f, 1.54f));
            var flight = plane.gameObject.AddComponent<BoreatekBushPlane>();
            flight.Initialize(seat, glazing, propeller, elevator, rudder, left, right);
        }

        private static void BuildPlaneHull(Batch batch, Material matte, Material enamel, Material cream)
        {
            var sections = new (float length, float y, float width, float depth)[]
            {
                (-3.18f, 1.10f, 0.09f, 0.10f), (-2.53f, 1.04f, 0.21f, 0.19f), (-0.85f, 1.07f, 0.47f, 0.39f),
                (0.90f, 1.10f, 0.57f, 0.48f), (1.73f, 1.12f, 0.45f, 0.38f), (2.59f, 1.14f, 0.32f, 0.31f),
                (2.73f, 1.14f, 0.29f, 0.29f)
            };
            Vector3 Ring(int row, int index)
            {
                var section = sections[row];
                float angle = index * Mathf.PI / 6f;
                return new Vector3(section.width * Mathf.Cos(angle), section.y + section.depth * Mathf.Sin(angle), section.length);
            }
            for (int i = 0; i < 12; i++)
            {
                for (int row = 0; row < sections.Length - 1; row++)
                    batch.Quad(Ring(row, i), Ring(row, i + 1), Ring(row + 1, i + 1), Ring(row + 1, i),
                        i is 0 or 5 && row > 0 && row < 4 ? cream : i < 6 ? enamel : matte);
                batch.Triangle(Ring(0, i + 1), Ring(0, i), new Vector3(0f, sections[0].y, sections[0].length), matte);
                batch.Triangle(Ring(sections.Length - 1, i), Ring(sections.Length - 1, i + 1),
                    new Vector3(0f, sections[^1].y, sections[^1].length), matte);
            }
        }

        private static void AddPlaneWing(Batch batch, float root, float front, float rear, float span, float tipFront, float tipRear,
            float y, float thickness, Material top, Material bottom)
        {
            Vector3 a = new(-span, y, tipRear), b = new(-root, y, rear), c = new(-root, y, front), d = new(-span, y, tipFront);
            Vector3 lift = Vector3.up * thickness;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                bool mirror = sign > 0;
                Vector3 m = mirror ? new Vector3(-1f, 1f, 1f) : Vector3.one;
                Vector3 q0 = Vector3.Scale(mirror ? d : a, m), q1 = Vector3.Scale(mirror ? c : b, m);
                Vector3 q2 = Vector3.Scale(mirror ? b : c, m), q3 = Vector3.Scale(mirror ? a : d, m);
                batch.Quad(q0 + lift, q3 + lift, q2 + lift, q1 + lift, top);
                batch.Quad(q0 - lift, q1 - lift, q2 - lift, q3 - lift, bottom);
                batch.Quad(q0 - lift, q3 - lift, q3 + lift, q0 + lift, top);
                batch.Quad(q0 - lift, q0 + lift, q1 + lift, q1 - lift, top);
                batch.Quad(q2 - lift, q2 + lift, q3 + lift, q3 - lift, top);
            }
        }

        private static void AddFin(Batch batch, Vector3 a, Vector3 b, Vector3 c, Vector3 d, float width, Material material)
        {
            Vector3 side = Vector3.right * width;
            batch.Quad(a - side, b - side, c - side, d - side, material);
            batch.Quad(a + side, d + side, c + side, b + side, material);
            batch.Quad(a - side, b - side, b + side, a + side, material);
            batch.Quad(b - side, c - side, c + side, b + side, material);
            batch.Quad(c - side, d - side, d + side, c + side, material);
        }

        private Transform MakePlaneSurface(Transform plane, string name, Vector3 position, Vector3 shape, Material material)
        {
            Transform hinge = CharacterPart(plane, name, position);
            var slab = new Batch();
            slab.Box(Vector3.back * shape.z * 0.45f, shape, material);
            _landscape.Place(hinge, name + "_Malla", slab);
            return hinge;
        }

        private static void AddPlaneCollider(Transform plane, Vector3 center, Vector3 size)
        {
            var collider = plane.gameObject.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
        }
    }
}
