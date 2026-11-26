using Boreatek.Economy;
using UnityEngine;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;

namespace Boreatek.World
{
    public partial class BoreatekWorldBuilder
    {
        internal static readonly Vector2 MerchantLocation = new Vector2(6.45f, -4.70f);

        private void BuildChristmasMerchant(Transform village)
        {
            Transform stall = CharacterPart(village, "Puesto_De_Bruma",
                new Vector3(MerchantLocation.x, 0.16f, MerchantLocation.y));
            Material oak = CharacterFinish(111, 70, 52);
            Material darkOak = CharacterFinish(74, 53, 47);
            Material cranberry = CharacterFinish(171, 56, 68);
            Material ribbon = CharacterFinish(225, 170, 91, 0.32f);
            Material spruce = CharacterFinish(46, 79, 76);
            Material needles = CharacterFinish(65, 106, 93);
            Material snow = CharacterFinish(186, 206, 218);
            Material lamp = CharacterFinish(255, 193, 107, 0.24f, glow: 0.62f);

            var counter = new Batch();
            counter.Box(new Vector3(-0.59f, 0.35f, -0.83f), new Vector3(1.70f, 0.66f, 0.51f), darkOak);
            for (int plank = 0; plank < 6; plank++)
                counter.Box(new Vector3(-1.27f + plank * 0.27f, 0.37f, -1.099f),
                    new Vector3(0.25f, 0.55f, 0.048f), oak);
            counter.Box(new Vector3(-0.59f, 0.68f, -0.83f), new Vector3(1.84f, 0.095f, 0.65f), oak);
            counter.Box(new Vector3(-0.61f, 0.733f, -0.91f), new Vector3(1.33f, 0.026f, 0.36f), cranberry);
            _landscape.Place(stall, "Mostrador_De_Madera", counter, collision: true);

            var goods = new Batch();
            Vector3 cup = new Vector3(-1.12f, 0.744f, -0.83f);
            goods.Frustum(cup, cup + Vector3.up * 0.22f, 0.104f, 0.087f, 10, cranberry);
            goods.Frustum(cup + Vector3.up * 0.21f, cup + Vector3.up * 0.26f, 0.096f, 0.079f, 10, ribbon);
            goods.Box(new Vector3(-0.64f, 0.773f, -0.87f), new Vector3(0.31f, 0.058f, 0.24f), needles,
                Quaternion.Euler(0f, 13f, 0f));
            WrapPresent(goods, new Vector3(-0.19f, 0.744f, -0.84f), new Vector3(0.28f, 0.25f, 0.21f), spruce, ribbon);
            WrapPresent(goods, new Vector3(0.29f, 0.01f, -0.79f), new Vector3(0.49f, 0.38f, 0.42f), cranberry, ribbon);
            WrapPresent(goods, new Vector3(1.21f, 0.01f, -0.54f), new Vector3(0.39f, 0.29f, 0.37f), needles, snow);
            _landscape.Place(stall, "Provisiones_Y_Regalos", goods);
            AddFoundrySign(stall, "PUESTO DE BRUMA", new Vector3(-0.59f, 0.384f, -1.142f));

            Transform tree = CharacterPart(stall, "Pino_Navideno_De_Bruma", new Vector3(0.95f, 0.01f, 0.65f));
            var trunk = new Batch();
            trunk.Frustum(Vector3.zero, Vector3.up * 0.38f, 0.44f, 0.49f, 10, cranberry);
            trunk.Frustum(Vector3.up * 0.29f, Vector3.up * 0.36f, 0.50f, 0.50f, 10, ribbon);
            trunk.Frustum(Vector3.up * 0.31f, Vector3.up * 1.35f, 0.17f, 0.12f, 9, darkOak);
            _landscape.Place(tree, "Maceta_Y_Tronco", trunk, collision: true);

            var branches = new Batch();
            for (int level = 0; level < 5; level++)
            {
                float y = 0.65f + level * 0.48f;
                float radius = 1.02f - level * 0.17f;
                branches.Frustum(Vector3.up * y, Vector3.up * (y + 0.95f), radius, 0.028f, 10,
                    level % 2 == 0 ? spruce : needles);
                branches.Frustum(Vector3.up * (y + 0.15f), Vector3.up * (y + 0.97f), radius * 0.856f, 0.032f, 10, snow);
                float bulbRadius = radius * 0.78f;
                for (int bauble = 0; bauble < 9; bauble++)
                {
                    float angle = bauble * (Mathf.PI * 2f / 9f) + level * 1.15f;
                    Vector3 point = new Vector3(Mathf.Cos(angle) * bulbRadius, y + 0.23f, Mathf.Sin(angle) * bulbRadius);
                    branches.Ellipsoid(point, Vector3.one * (level > 2 ? 0.054f : 0.069f), 7, 5,
                        bauble % 3 == 0 ? cranberry : lamp);
                }
            }
            Vector3 star = Vector3.up * 3.73f;
            for (int tip = 0; tip < 10; tip++)
            {
                Vector3 Point(int index)
                {
                    float angle = index * Mathf.PI * 0.2f;
                    float length = index % 2 == 0 ? 0.25f : 0.107f;
                    return star + new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f) * length;
                }
                Vector3 a = Point(tip), b = Point(tip + 1);
                branches.Triangle(star + Vector3.back * 0.075f, a, b, lamp);
                branches.Triangle(star + Vector3.forward * 0.075f, b, a, ribbon);
            }
            _landscape.Place(tree, "Ramas_Luces_Y_Estrella", branches);
            AddFoundryLight(tree, new Vector3(0f, 1.45f, -0.85f), 1.46f, 5f);

            Transform trader = CharacterPart(stall, "Bruma_Comerciante", new Vector3(-0.55f, 0f, -0.11f));
            trader.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var body = trader.gameObject.AddComponent<CapsuleCollider>();
            body.center = Vector3.up * (ElfHeight * 0.53f);
            body.height = ElfHeight * 1.12f;
            body.radius = 0.31f;
            ElfVisuals costume = BuildElfModel(trader, new Color32(115, 59, 84, 255), festiveHat: true);
            trader.gameObject.AddComponent<WorkshopMerchant>().Initialize(costume.Pose);
        }

        private static void WrapPresent(Batch mesh, Vector3 at, Vector3 size, Material paper, Material ribbon)
        {
            Vector3 center = at + Vector3.up * (size.y * 0.5f);
            mesh.Box(center, size, paper);
            mesh.Box(center, new Vector3(size.x * 0.16f, size.y + 0.009f, size.z + 0.009f), ribbon);
            mesh.Box(center + Vector3.up * (size.y * 0.5f), new Vector3(size.x + 0.009f, 0.012f, size.z * 0.16f), ribbon);
            foreach (int side in new[] { -1, 1 })
                mesh.Box(center + new Vector3(size.x * 0.094f * side, size.y * 0.5f + 0.025f, 0f),
                    new Vector3(0.091f, 0.035f, 0.041f), ribbon, Quaternion.Euler(0f, 32f * side, 12f * side));
        }
    }
}
