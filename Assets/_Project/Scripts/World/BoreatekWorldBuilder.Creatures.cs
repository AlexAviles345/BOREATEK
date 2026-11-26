using UnityEngine;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;

namespace Boreatek.World
{
    public partial class BoreatekWorldBuilder
    {
        private void BuildCrystalBeetle(BoreatekLandscapeGeometry owner, Transform rig,
            out Transform head, out Transform vent, out Transform[] legs, out Transform[] feet)
        {
            Material chitin = CharacterFinish(49, 53, 65, smoothness: 0.22f);
            Material carapace = CharacterFinish(65, 112, 126, smoothness: 0.29f);
            Material ice = CharacterFinish(120, 178, 191, smoothness: 0.25f);
            Material ember = CharacterFinish(186, 104, 62, smoothness: 0.31f, glow: 0.22f);
            var shell = new Batch();
            shell.Ellipsoid(new Vector3(0f, 0.41f, -0.02f), new Vector3(0.48f, 0.28f, 0.62f), 10, 6, chitin);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 shoulder = new Vector3(side * 0.235f, 0.56f, 0.12f);
                shell.Ellipsoid(shoulder, new Vector3(0.252f, 0.30f, 0.57f), 9, 5, carapace);
                for (int plate = 0; plate < 3; plate++)
                {
                    Vector3 footing = shoulder + new Vector3(0f, 0.17f, -0.32f + plate * 0.24f);
                    Vector3 tip = footing + new Vector3(side * 0.09f, 0.24f + plate * 0.07f, -0.09f);
                    shell.Frustum(footing, tip, 0.115f + plate * 0.018f, 0f, 5, plate % 2 == 0 ? ice : carapace);
                }
            }
            owner.Place(rig, "Caparazon_Facetado", shell);
            head = CharacterPart(rig, "Mandibulas", new Vector3(0f, 0.43f, 0.43f));
            var jaws = new Batch();
            jaws.Ellipsoid(Vector3.zero, new Vector3(0.285f, 0.207f, 0.31f), 10, 5, chitin);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 root = new(side * 0.16f, -0.045f, 0.17f);
                Vector3 bend = root + new Vector3(side * 0.11f, 0.01f, 0.29f);
                jaws.Frustum(root, bend, 0.091f, 0.075f, 6, carapace);
                jaws.Frustum(bend, bend + new Vector3(-side * 0.15f, 0.035f, 0.20f), 0.076f, 0.008f, 6, ice);
                jaws.Ellipsoid(new Vector3(side * 0.165f, 0.091f, 0.219f),
                    new Vector3(0.066f, 0.040f, 0.038f), 8, 4, ember);
            }
            owner.Place(head, "Cabeza_Y_Mandibulas", jaws);
            vent = CharacterPart(rig, "Abdomen_Articulado", new Vector3(0f, 0.35f, -0.51f));
            var abdomen = new Batch();
            abdomen.Frustum(Vector3.zero, new Vector3(0f, -0.03f, -0.21f), 0.26f, 0.18f, 7, chitin);
            abdomen.Frustum(new Vector3(0f, -0.02f, -0.18f), new Vector3(0f, -0.03f, -0.245f), 0.13f, 0.09f, 7, ember);
            owner.Place(vent, "Placa_Posterior", abdomen);

            legs = new Transform[6];
            feet = new Transform[6];
            for (int i = 0; i < legs.Length; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float spread = (1 - i / 2) * 0.14f;
                legs[i] = CharacterPart(rig, $"Pata_{i}", new Vector3(side * 0.30f, 0.36f, 0.32f - i / 2 * 0.34f));
                Vector3 knee = new(side * 0.36f, 0.060f, spread);
                var limb = new Batch();
                limb.Frustum(Vector3.zero, knee, 0.077f, 0.055f, 6, chitin);
                owner.Place(legs[i], "Coxa_Y_Femur", limb);
                feet[i] = CharacterPart(legs[i], $"Tarso_{i}", knee);
                var claw = new Batch();
                claw.Frustum(Vector3.zero, new Vector3(side * 0.085f, -0.36f, spread * 0.38f), 0.053f, 0.014f, 6, carapace);
                owner.Place(feet[i], "Garra", claw);
            }
        }

        private void BuildBlizzardWraith(BoreatekLandscapeGeometry owner, Transform rig, out Transform head,
            out Transform tail, out Transform[] ribbons, out Transform orb)
        {
            Material blue = CharacterFinish(61, 89, 123, smoothness: 0.20f);
            Material cloth = CharacterFinish(99, 153, 174, smoothness: 0.13f);
            Material rime = CharacterFinish(155, 199, 208, smoothness: 0.18f);
            Material hollow = CharacterFinish(41, 59, 86, smoothness: 0.09f);
            Material shine = CharacterFinish(86, 188, 214, smoothness: 0.14f, glow: 0.31f);
            var mantle = new Batch();
            mantle.Frustum(new Vector3(0f, 0.30f, -0.015f), new Vector3(0f, -0.04f, 0.02f), 0.24f, 0.36f, 8, cloth);
            mantle.Frustum(new Vector3(0f, -0.04f, 0.02f), new Vector3(0.04f, -0.51f, -0.17f), 0.36f, 0.115f, 8, blue);
            mantle.Frustum(new Vector3(0f, 0.05f, 0.20f), new Vector3(0f, 0.10f, 0.282f), 0.13f, 0.070f, 7, shine);
            owner.Place(rig, "Manto_De_Ventisca", mantle);
            head = CharacterPart(rig, "Mascara_Congelada", new Vector3(0f, 0.35f, 0.08f));
            var mask = new Batch();
            mask.Ellipsoid(new Vector3(0f, 0.06f, -0.018f), new Vector3(0.265f, 0.305f, 0.225f), 9, 5, blue);
            mask.Frustum(new Vector3(0f, 0.19f, 0.113f), new Vector3(0f, -0.095f, 0.22f), 0.199f, 0.066f, 7, rime);
            mask.Ellipsoid(new Vector3(0f, 0.03f, 0.169f), new Vector3(0.158f, 0.115f, 0.084f), 8, 5, hollow);
            for (int side = -1; side <= 1; side += 2)
            {
                mask.Frustum(new Vector3(side * 0.061f, 0.041f, 0.237f), new Vector3(side * 0.137f, 0.071f, 0.198f),
                    0.023f, 0.014f, 5, shine);
            }
            owner.Place(head, "Yelmo_Y_Ojos", mask);
            tail = CharacterPart(rig, "Estela_Inferior", new Vector3(0.015f, -0.36f, -0.10f));
            var plume = new Batch();
            plume.Frustum(Vector3.zero, new Vector3(0.12f, -0.38f, -0.27f), 0.18f, 0.092f, 6, cloth);
            plume.Frustum(new Vector3(0.12f, -0.38f, -0.27f), new Vector3(0.07f, -0.46f, -0.48f), 0.092f, 0f, 6, blue);
            owner.Place(tail, "Remolino", plume);
            ribbons = new Transform[4];
            for (int i = 0; i < ribbons.Length; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float height = i < 2 ? 0.05f : -0.16f;
                ribbons[i] = CharacterPart(rig, $"Falda_De_Escarcha_{i}", new Vector3(side * 0.245f, height, -0.011f));
                var ribbon = new Batch();
                Vector3 elbow = new(side * 0.32f, -0.15f, 0.12f - i / 2 * 0.24f);
                Vector3 end = new(side * 0.44f, -0.36f, -0.37f - i / 2 * 0.14f);
                ribbon.Frustum(Vector3.zero, elbow, 0.12f, 0.094f, 5, cloth);
                ribbon.Frustum(elbow, end, 0.094f, 0f, 5, rime);
                owner.Place(ribbons[i], "Cinta_Facetada", ribbon);
            }
            Transform charge = CharacterPart(rig, "Foco_De_Escarcha", new Vector3(0f, 0.09f, 0.58f));
            var shard = new Batch();
            shard.Frustum(Vector3.back * 0.30f, Vector3.zero, 0.014f, 0.143f, 6, cloth);
            shard.Frustum(Vector3.zero, Vector3.forward * 0.25f, 0.143f, 0.008f, 6, shine);
            orb = owner.Place(charge, "Cristal_De_Descarga", shard).transform;
            orb.gameObject.SetActive(false);
        }
    }
}
