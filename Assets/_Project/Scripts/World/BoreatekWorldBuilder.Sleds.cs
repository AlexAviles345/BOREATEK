using UnityEngine;
using UnityEngine.Rendering;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;

namespace Boreatek.World
{
    public partial class BoreatekWorldBuilder
    {
        private void BuildVillageSleds()
        {
            Transform yard = CharacterPart(_worldRoot.Find("Biome_01_SantaIndustrialVillage"), "Parada_De_Trineos", Vector3.zero);
            BuildSled(yard, false, new Vector3(-6.8f, 0.18f, -10.8f));
            BuildSled(yard, true, new Vector3(6.8f, 0.18f, -10.8f));
        }

        private void BuildSled(Transform parent, bool reindeer, Vector3 position)
        {
            Transform root = CharacterPart(parent, reindeer ? "Trineo_De_Renos" : "Trineo_De_Perros", position);
            root.localRotation = Quaternion.Euler(0f, reindeer ? 135f : 210f, 0f);
            Material wood = CharacterFinish(117, 73, 48);
            Material brass = CharacterFinish(175, 113, 68, 0.33f, 0.25f);
            Material iron = CharacterFinish(50, 65, 79, 0.42f, 0.46f);
            Material blanket = reindeer ? CharacterFinish(135, 52, 61) : CharacterFinish(50, 98, 105);
            var frame = new Batch();
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 0.54f;
                var heel = new Vector3(x, 0.08f, -0.99f);
                var nose = new Vector3(x, 0.08f, 0.75f);
                Beam(frame, heel, nose, 0.10f, iron);
                Beam(frame, nose, new Vector3(x, 0.22f, 1.0f), 0.10f, brass);
                Beam(frame, new Vector3(x, 0.22f, 1.0f), new Vector3(x, 0.42f, 1.14f), 0.085f, brass);
                for (int plinth = -1; plinth <= 1; plinth += 2)
                    Beam(frame, new Vector3(x, 0.12f, plinth * 0.56f), new Vector3(x, 0.35f, plinth * 0.56f), 0.12f, wood);
                Beam(frame, new Vector3(x, 0.63f, -0.69f), new Vector3(x, 0.43f, 0.75f), 0.083f, wood);
                Beam(frame, new Vector3(side * 0.34f, 0.42f, 0.65f), new Vector3(side * 0.39f, 0.43f, 1.43f), 0.058f, wood);
            }
            for (int i = -3; i <= 3; i++)
                frame.Box(new Vector3(0f, 0.32f, 0.24f * i), new Vector3(1.06f, 0.092f, 0.22f), wood);
            frame.Box(new Vector3(0f, 0.43f, -0.35f), new Vector3(0.94f, 0.17f, 0.64f), blanket);
            frame.Box(new Vector3(0f, 0.68f, -0.65f), new Vector3(1.01f, 0.37f, 0.12f), wood);
            frame.Box(new Vector3(0f, 0.72f, -0.566f), new Vector3(0.91f, 0.25f, 0.08f), blanket);
            frame.Box(new Vector3(0f, 0.42f, 1.38f), new Vector3(1.09f, 0.07f, 0.094f), brass);
            _landscape.Place(root, "Patines_Bastidor_Y_Asiento", frame);

            var collider = root.gameObject.AddComponent<CharacterController>();
            collider.center = Vector3.up * 0.77f;
            collider.radius = 0.61f;
            collider.height = 1.54f;
            collider.stepOffset = 0.28f;
            collider.skinWidth = 0.045f;
            collider.slopeLimit = 43f;
            var seat = CharacterPart(root, "Asiento_Del_Conductor", new Vector3(0f, 0.47f, -0.36f));
            var team = new SledAnimal[2];
            Material straps = _landscape.Own(new Material(Shader.Find("Sprites/Default")) { name = "Cuero del tiro" });
            for (int i = 0; i < team.Length; i++)
            {
                float side = i == 0 ? -1f : 1f;
                Transform hitch = CharacterPart(root, $"Enganche_{i}", new Vector3(side * 0.42f, 0.45f, 1.43f));
                team[i] = BuildSledAnimal(root, hitch, straps, new Vector3(side * 0.63f, 0f, i == 0 ? 2.51f : 2.69f), reindeer, i);
            }
            root.gameObject.AddComponent<BoreatekVillageSled>().Initialize(reindeer ? "renos" : "perros",
                reindeer ? "trineo de renos" : "trineo de perros", collider, seat, team, reindeer ? 14.2f : 11.8f, reindeer ? 71f : 93f);
        }

        private SledAnimal BuildSledAnimal(Transform sled, Transform hitch, Material straps, Vector3 position, bool reindeer, int index)
        {
            Transform animal = CharacterPart(sled, reindeer ? $"Reno_Joven_{index + 1}" : $"Perro_De_Tiro_{index + 1}", position);
            Transform torso = CharacterPart(animal, "Tronco", Vector3.zero);
            Material fur = reindeer ? CharacterFinish(index == 0 ? (byte)131 : (byte)104, 84, 59)
                : index == 0 ? CharacterFinish(79, 98, 115) : CharacterFinish(183, 174, 156);
            Material cream = CharacterFinish(218, 211, 187);
            Material dark = CharacterFinish(39, 48, 53);
            Material harness = CharacterFinish(144, 54, 55);
            Material eye = CharacterFinish(130, 169, 175, 0.47f);
            float bodyY = reindeer ? 0.71f : 0.48f;
            const float headZ = 0.49f;
            float hipZ = reindeer ? 0.29f : 0.28f;
            var coat = new Batch();
            coat.Ellipsoid(new Vector3(0f, bodyY, -0.03f), new Vector3(0.23f, reindeer ? 0.26f : 0.22f, 0.51f), 12, 7, fur);
            coat.Ellipsoid(new Vector3(0f, bodyY - 0.075f, 0.14f), new Vector3(0.19f, 0.19f, 0.34f), 10, 6, cream);
            coat.Frustum(new Vector3(0f, bodyY, 0.25f), new Vector3(0f, bodyY + (reindeer ? 0.34f : 0.19f), headZ),
                0.19f, 0.12f, 9, fur);
            coat.Box(new Vector3(0f, bodyY + 0.11f, -0.22f), new Vector3(0.41f, 0.071f, 0.13f), harness);
            coat.Box(new Vector3(0f, bodyY + 0.14f, 0.19f), new Vector3(0.45f, 0.085f, 0.083f), harness);
            _landscape.Place(torso, "Lomo_Pecho_Y_Arnes", coat);

            Transform head = CharacterPart(torso, "Cabeza", new Vector3(0f, bodyY + (reindeer ? 0.32f : 0.19f), headZ));
            var face = new Batch();
            face.Ellipsoid(new Vector3(0f, 0.04f, 0.06f), new Vector3(0.172f, 0.164f, 0.20f), 12, 6, fur);
            face.Ellipsoid(new Vector3(0f, -0.064f, 0.237f), new Vector3(0.137f, 0.091f, 0.19f), 10, 6, cream);
            face.Ellipsoid(new Vector3(0f, -0.048f, 0.379f), new Vector3(0.10f, 0.063f, 0.063f), 10, 5, dark);
            for (int side = -1; side <= 1; side += 2)
            {
                face.Ellipsoid(new Vector3(side * 0.13f, 0.062f, 0.167f), new Vector3(0.039f, 0.042f, 0.033f), 9, 5, dark);
                face.Ellipsoid(new Vector3(side * 0.143f, 0.067f, 0.191f), new Vector3(0.020f, 0.018f, 0.013f), 8, 4, eye);
                Vector3 ear = new Vector3(side * 0.115f, 0.145f, 0.019f);
                face.Frustum(ear, ear + new Vector3(side * (reindeer ? 0.23f : 0.06f), reindeer ? 0.118f : 0.22f, -0.08f),
                    0.072f, 0.012f, 6, fur);
                if (!reindeer) continue;
                Vector3 antler = new Vector3(side * 0.081f, 0.158f, 0.068f);
                Vector3 fork = antler + new Vector3(side * 0.079f, 0.22f, -0.064f);
                face.Frustum(antler, fork, 0.037f, 0.022f, 6, cream);
                face.Frustum(fork, fork + new Vector3(side * 0.078f, 0.17f, -0.095f), 0.023f, 0.006f, 6, cream);
                face.Frustum(fork, fork + new Vector3(side * 0.072f, 0.105f, 0.098f), 0.022f, 0.007f, 6, cream);
            }
            _landscape.Place(head, "Hocico_Orejas_Y_Ojos", face);
            Transform tail = CharacterPart(torso, "Cola", new Vector3(0f, bodyY + 0.06f, -0.48f));
            var plume = new Batch();
            plume.Frustum(Vector3.zero, new Vector3(0f, 0.11f, reindeer ? -0.18f : -0.35f), 0.092f, reindeer ? 0.028f : 0.054f, 7, fur);
            plume.Frustum(new Vector3(0f, 0.11f, reindeer ? -0.18f : -0.35f), new Vector3(0f, 0.21f, reindeer ? -0.23f : -0.47f),
                0.051f, 0.012f, 7, cream);
            _landscape.Place(tail, "Penacho", plume);

            var hips = new Transform[4];
            var ankles = new Transform[4];
            float upper = reindeer ? 0.27f : 0.16f, lower = reindeer ? 0.28f : 0.155f;
            for (int i = 0; i < hips.Length; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                bool front = i < 2;
                hips[i] = CharacterPart(torso, $"Pata_{i}", new Vector3(side * 0.14f, upper + lower + 0.069f, (front ? 1f : -1f) * hipZ));
                Vector3 knee = new Vector3(0f, -upper, front ? 0.026f : -0.056f);
                var thigh = new Batch();
                thigh.Frustum(Vector3.up * 0.06f, knee, front ? 0.095f : 0.111f, reindeer ? 0.049f : 0.061f, 8, fur);
                _landscape.Place(hips[i], "Muslo", thigh);
                ankles[i] = CharacterPart(hips[i], $"Corvejon_{i}", knee);
                var shin = new Batch();
                shin.Frustum(Vector3.zero, Vector3.down * lower, reindeer ? 0.052f : 0.064f, reindeer ? 0.039f : 0.052f, 8, cream);
                shin.Box(new Vector3(0f, -lower - 0.028f, 0.024f), new Vector3(reindeer ? 0.092f : 0.128f, 0.084f, 0.14f), dark);
                _landscape.Place(ankles[i], reindeer ? "Pezuna" : "Huella", shin);
            }
            Transform saddle = CharacterPart(torso, "Tiro", new Vector3(0f, bodyY + 0.045f, -0.44f));
            var trace = CharacterPart(sled, $"Correa_{index}", Vector3.zero).gameObject.AddComponent<LineRenderer>();
            trace.sharedMaterial = straps;
            trace.startColor = trace.endColor = new Color32(86, 53, 40, 255);
            trace.useWorldSpace = true;
            trace.shadowCastingMode = ShadowCastingMode.Off;
            trace.widthMultiplier = 0.025f;
            trace.positionCount = 6;
            var actor = animal.gameObject.AddComponent<SledAnimal>();
            actor.Initialize(torso, head, tail, saddle, hips, ankles, hitch, trace, reindeer, index == 0 ? 0.11f : 0.57f);
            return actor;
        }
    }
}
