using Boreatek.AI;
using Boreatek.Player;
using UnityEngine;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;
using Species = Boreatek.AI.HostileWildlife.Species;

namespace Boreatek.World
{
    public partial class BoreatekWorldBuilder
    {
        internal HostileWildlife SpawnWildlife(Transform parent, BoreatekLandscapeGeometry owner, Vector3 site,
            Species species, PlayerController player, float yaw)
        {
            bool flies = species is Species.FrostMoth or Species.BlizzardWraith;
            string label = species switch
            {
                Species.FrostMoth => "Polilla_De_Escarcha",
                Species.CrystalBeetle => "Escarabajo_De_Cristal",
                Species.BlizzardWraith => "Espectro_De_Ventisca",
                _ => "Lobo_Nival"
            };
            Transform creature = CharacterPart(parent, label, Vector3.zero);
            creature.SetPositionAndRotation(site, Quaternion.Euler(0f, yaw, 0f));
            Transform rig = CharacterPart(creature, "Rig", Vector3.zero);
            Transform head, tail, frostOrb = null;
            Transform[] joints, feet;
            switch (species)
            {
                case Species.FrostMoth: BuildFrostMoth(owner, rig, out head, out tail, out joints, out feet); break;
                case Species.CrystalBeetle: BuildCrystalBeetle(owner, rig, out head, out tail, out joints, out feet); break;
                case Species.BlizzardWraith:
                    BuildBlizzardWraith(owner, rig, out head, out tail, out joints, out frostOrb);
                    feet = null;
                    break;
                default: BuildSnowWolf(owner, rig, out head, out tail, out joints, out feet); break;
            }

            var body = creature.gameObject.AddComponent<CharacterController>();
            body.radius = species == Species.CrystalBeetle ? 0.49f : flies ? 0.45f : 0.38f;
            body.height = species == Species.FrostMoth ? 0.94f : 1.13f;
            body.center = flies ? Vector3.zero : Vector3.up * (body.height * 0.5f);
            body.skinWidth = 0.028f;
            body.stepOffset = flies ? 0f : 0.28f;
            body.slopeLimit = 48f;
            var wildlife = creature.gameObject.AddComponent<HostileWildlife>();
            wildlife.Initialize(species, player, body, rig, head, tail, joints, feet, frostOrb);
            return wildlife;
        }

        private void BuildSnowWolf(BoreatekLandscapeGeometry owner, Transform rig, out Transform head,
            out Transform tail, out Transform[] legs, out Transform[] paws)
        {
            Material slate = CharacterFinish(76, 94, 115, smoothness: 0.16f);
            Material fur = CharacterFinish(126, 148, 165, smoothness: 0.13f);
            Material frost = CharacterFinish(198, 212, 219, smoothness: 0.13f);
            Material charcoal = CharacterFinish(32, 44, 54, smoothness: 0.25f);
            Material amber = CharacterFinish(237, 172, 99, smoothness: 0.40f, glow: 0.25f);
            var coat = new Batch();
            coat.Ellipsoid(new Vector3(0f, 0.57f, -0.10f), new Vector3(0.285f, 0.255f, 0.58f), 10, 6, fur);
            coat.Ellipsoid(new Vector3(0f, 0.66f, 0.10f), new Vector3(0.275f, 0.23f, 0.46f), 8, 5, slate);
            coat.Frustum(new Vector3(0f, 0.60f, 0.23f), new Vector3(0f, 0.85f, 0.55f), 0.263f, 0.16f, 7, fur);
            coat.Ellipsoid(new Vector3(0f, 0.48f, 0.29f), new Vector3(0.22f, 0.19f, 0.28f), 8, 5, frost);
            owner.Place(rig, "Abrigo_Y_Lomo", coat);

            head = CharacterPart(rig, "Cabeza", new Vector3(0f, 0.83f, 0.51f));
            var face = new Batch();
            face.Ellipsoid(new Vector3(0f, 0.035f, 0.022f), new Vector3(0.208f, 0.18f, 0.225f), 10, 6, slate);
            face.Frustum(new Vector3(0f, 0.003f, 0.12f), new Vector3(0f, -0.035f, 0.39f), 0.139f, 0.086f, 7, fur);
            face.Frustum(new Vector3(0f, -0.102f, 0.10f), new Vector3(0f, -0.090f, 0.34f), 0.125f, 0.056f, 6, frost);
            face.Ellipsoid(new Vector3(0f, -0.028f, 0.415f), new Vector3(0.092f, 0.057f, 0.050f), 8, 5, charcoal);
            foreach (int side in new[] { -1, 1 })
            {
                Vector3 earBase = new(side * 0.128f, 0.151f, -0.053f);
                Vector3 earTip = new(side * 0.201f, 0.386f, -0.139f);
                face.Frustum(earBase, earTip, 0.079f, 0.013f, 5, slate);
                face.Triangle(earBase + new Vector3(-0.043f, 0.035f, 0.062f), earBase + new Vector3(0.043f, 0.035f, 0.062f),
                    earTip + Vector3.forward * 0.021f, frost);
                face.Ellipsoid(new Vector3(side * 0.159f, 0.066f, 0.173f), new Vector3(0.046f, 0.029f, 0.040f), 8, 5, charcoal);
                face.Ellipsoid(new Vector3(side * 0.177f, 0.071f, 0.183f), new Vector3(0.025f, 0.017f, 0.024f), 8, 5, amber);
                face.Frustum(new Vector3(side * 0.105f, -0.061f, 0.257f), new Vector3(side * 0.09f, -0.141f, 0.26f),
                    0.019f, 0.006f, 5, frost);
            }
            owner.Place(head, "Hocico_Orejas_Y_Colmillos", face);

            tail = CharacterPart(rig, "Cola", new Vector3(0f, 0.60f, -0.55f));
            var tailMesh = new Batch();
            tailMesh.Frustum(Vector3.zero, new Vector3(0f, -0.09f, -0.31f), 0.102f, 0.126f, 7, slate);
            tailMesh.Frustum(new Vector3(0f, -0.09f, -0.31f), new Vector3(0.07f, -0.24f, -0.58f), 0.126f, 0.052f, 7, fur);
            tailMesh.Frustum(new Vector3(0.07f, -0.24f, -0.58f), new Vector3(0.095f, -0.27f, -0.72f), 0.052f, 0f, 6, frost);
            owner.Place(tail, "Penacho_De_Cola", tailMesh);

            legs = new Transform[4];
            paws = new Transform[4];
            for (int index = 0; index < legs.Length; index++)
            {
                int side = index % 2 == 0 ? -1 : 1;
                bool front = index < 2;
                Vector3 knee = new(0f, -0.26f, front ? 0.018f : -0.112f);
                legs[index] = CharacterPart(rig, $"Pata_{index}", new Vector3(side * 0.185f, 0.50f, front ? 0.32f : -0.38f));
                var limb = new Batch();
                limb.Frustum(Vector3.up * 0.074f, knee, front ? 0.093f : 0.133f, 0.061f, 7, fur);
                owner.Place(legs[index], "Muslo", limb);
                paws[index] = CharacterPart(legs[index], $"Corvejon_{index}", knee);
                var paw = new Batch();
                paw.Frustum(Vector3.zero, Vector3.down * 0.20f + Vector3.forward * 0.03f, 0.065f, 0.047f, 6, frost);
                paw.Ellipsoid(new Vector3(0f, -0.209f, 0.059f), new Vector3(0.078f, 0.034f, 0.105f), 8, 4, charcoal);
                owner.Place(paws[index], "Zarpa", paw);
            }
        }

        private void BuildFrostMoth(BoreatekLandscapeGeometry owner, Transform rig, out Transform head,
            out Transform tail, out Transform[] hinges, out Transform[] feet)
        {
            Material velvet = CharacterFinish(114, 108, 143, smoothness: 0.13f);
            Material ash = CharacterFinish(156, 171, 196, smoothness: 0.09f);
            Material lavender = CharacterFinish(113, 141, 192, smoothness: 0.07f);
            Material edge = CharacterFinish(66, 77, 129, smoothness: 0.09f);
            Material eye = CharacterFinish(139, 199, 214, smoothness: 0.10f, glow: 0.10f);
            Material ink = CharacterFinish(48, 56, 83, smoothness: 0.23f);
            var torso = new Batch();
            torso.Ellipsoid(new Vector3(0f, 0f, -0.20f), new Vector3(0.15f, 0.14f, 0.43f), 10, 6, velvet);
            torso.Ellipsoid(new Vector3(0f, 0.015f, 0.08f), new Vector3(0.19f, 0.18f, 0.24f), 10, 6, ash);
            for (int row = 0; row < 3; row++)
            {
                float z = -0.34f + row * 0.12f;
                torso.Frustum(new Vector3(0f, 0.01f, z), new Vector3(0f, 0.01f, z + 0.045f),
                    0.124f + row * 0.013f, 0.126f + row * 0.013f, 9, edge);
            }
            owner.Place(rig, "Torax_Y_Abdomen", torso);
            head = CharacterPart(rig, "Cabeza", new Vector3(0f, 0.01f, 0.27f));
            var feelers = new Batch();
            feelers.Ellipsoid(Vector3.zero, new Vector3(0.14f, 0.135f, 0.14f), 10, 6, velvet);
            foreach (int side in new[] { -1, 1 })
            {
                Vector3 basePoint = new(side * 0.055f, 0.085f, 0.07f);
                Vector3 end = new(side * 0.31f, 0.27f, 0.23f);
                feelers.Ellipsoid(new Vector3(side * 0.097f, 0.025f, 0.071f), new Vector3(0.054f, 0.05f, 0.048f), 8, 5, ink);
                feelers.Ellipsoid(new Vector3(side * 0.116f, 0.035f, 0.104f), new Vector3(0.023f, 0.027f, 0.022f), 7, 4, eye);
                feelers.Frustum(basePoint, end, 0.024f, 0.006f, 6, lavender);
                for (int whisker = 1; whisker <= 4; whisker++)
                {
                    Vector3 start = Vector3.Lerp(basePoint, end, whisker * 0.197f);
                    feelers.Frustum(start, start + new Vector3(side * 0.025f, 0.068f, 0.046f), 0.010f, 0.003f, 4, ash);
                }
            }
            owner.Place(head, "Ojos_Y_Antenas", feelers);
            tail = null;
            feet = null;
            hinges = new Transform[2];
            for (int index = 0; index < hinges.Length; index++)
            {
                float sign = index == 0 ? -1f : 1f;
                hinges[index] = CharacterPart(rig, $"Ala_{index}", new Vector3(sign * 0.13f, 0.036f, 0.06f));
                AddMothWing(owner, hinges[index], sign, ash, lavender, edge, ink, eye);
            }
        }

        private static void AddMothWing(BoreatekLandscapeGeometry owner, Transform hinge, float side, Material ash,
            Material lavender, Material edge, Material ink, Material pupil)
        {
            const float halfThickness = 0.018f;
            Vector3 P(float x, float y, float z) => new(side * x, y, z);
            var wing = new Batch();
            Vector3 heart = P(0.40f, 0.045f, 0.035f);
            var outline = new[]
            {
                P(0f, 0, 0.22f), P(0.42f, 0.022f, 0.49f), P(0.83f, 0.032f, 0.62f),
                P(0.99f, 0.005f, 0.32f), P(0.81f, -0.035f, 0.04f), P(0.78f, -0.028f, -0.14f),
                P(0.49f, -0.035f, -0.55f), P(0.20f, -0.017f, -0.49f), P(0f, 0f, -0.20f)
            };
            void Skin(Vector3 a, Vector3 b, Vector3 c, Material material, float face)
            {
                Vector3 offset = Vector3.up * (face * halfThickness);
                if (Vector3.Cross(b - a, c - a).y * face < 0f) (b, c) = (c, b);
                wing.Triangle(a + offset, b + offset, c + offset, material);
            }

            // The eye spots share the skin's vertices with the surrounding scales.
            void Pattern(Vector3 a, Vector3 b, Vector3 c, Material color, float face)
            {
                Vector3 center = (a + b + c) / 3f;
                Vector3[] rim = { a, (a + b) * 0.5f, b, (b + c) * 0.5f, c, (c + a) * 0.5f };
                for (int segment = 0; segment < rim.Length; segment++)
                {
                    Vector3 start = rim[segment], end = rim[(segment + 1) % rim.Length];
                    float size = segment % 2 == 0 ? 0.40f : 0.75f;
                    float nextSize = segment % 2 == 0 ? 0.75f : 0.40f;
                    Vector3 darkA = Vector3.Lerp(center, start, size), darkB = Vector3.Lerp(center, end, nextSize);
                    Vector3 eyeA = Vector3.Lerp(center, darkA, 0.51f), eyeB = Vector3.Lerp(center, darkB, 0.51f);
                    Skin(start, end, darkB, color, face);
                    Skin(start, darkB, darkA, color, face);
                    Skin(darkA, darkB, eyeB, ink, face);
                    Skin(darkA, eyeB, eyeA, ink, face);
                    Skin(eyeA, eyeB, center, pupil, face);
                }
            }

            Vector3 thickness = Vector3.up * halfThickness;
            for (int i = 0; i < outline.Length; i++)
            {
                Vector3 a = outline[i], b = outline[(i + 1) % outline.Length];
                Vector3 innerA = Vector3.Lerp(heart, a, 0.83f), innerB = Vector3.Lerp(heart, b, 0.83f);
                Material vein = i % 3 == 0 ? ash : lavender;
                foreach (float face in new[] { 1f, -1f })
                {
                    Skin(innerA, a, b, edge, face);
                    Skin(innerA, b, innerB, edge, face);
                    if (i == 2 || i == 6) Pattern(heart, innerA, innerB, vein, face);
                    else Skin(heart, innerA, innerB, vein, face);
                }
                Vector3 outside = (a + b) * 0.5f - heart;
                if (Vector3.Dot(Vector3.Cross(-thickness, b - a), outside) < 0f) (a, b) = (b, a);
                wing.Quad(a + thickness, a - thickness, b - thickness, b + thickness, edge);
            }
            owner.Place(hinge, "Escamas_Y_Ocelos", wing);
        }
    }
}
