using Boreatek.Core;
using Boreatek.Economy;
using UnityEngine;
using UnityEngine.Rendering;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;

namespace Boreatek.World
{
    public partial class BoreatekWorldBuilder
    {
        private Transform[] BuildHeldEquipment(Transform hand)
        {
            var models = new Transform[(int)EquipmentSlot.Sword + 1];
            for (int i = 1; i < models.Length; i++)
            {
                var slot = (EquipmentSlot)i;
                var grip = CharacterPart(hand, $"Held_{slot}", Vector3.zero);
                if (slot == EquipmentSlot.Beacon)
                {
                    GameObject beacon = _landscape.PlaceShared(grip, "Calefactor_Con_Base_Y_Asa",
                        () => InventoryAndShop.ModelBeacon(_landscape));
                    beacon.transform.SetLocalPositionAndRotation(new Vector3(0.018f, -0.27f, 0.075f),
                        Quaternion.Euler(2f, 0f, 2f));
                    beacon.transform.localScale = Vector3.one * 0.36f;
                }
                else _landscape.PlaceShared(grip, $"Equipment_{slot}", () => BuildEquipmentMesh(slot));
                models[i] = grip;
                grip.gameObject.SetActive(false);
            }
            return models;
        }

        private Transform BuildFirstPersonGlove(Transform cameraPivot)
        {
            Transform grip = CharacterPart(cameraPivot, "FirstPerson_Grip_And_Mitten", Vector3.zero);
            var mesh = new Batch();
            Material mitten = CharacterFinish(132, 119, 95);
            Material cuff = CharacterFinish(190, 177, 148);
            Material cloth = CharacterFinish(91, 105, 68);
            mesh.Frustum(new Vector3(0.165f, -0.215f, -0.157f), new Vector3(0.057f, -0.061f, -0.048f),
                0.071f, 0.053f, 10, cloth);
            mesh.Frustum(new Vector3(0.078f, -0.083f, -0.071f), new Vector3(0.051f, -0.047f, -0.041f),
                0.058f, 0.058f, 10, cuff);
            mesh.Ellipsoid(new Vector3(0.032f, -0.021f, -0.023f), new Vector3(0.064f, 0.054f, 0.061f), 11, 5, mitten);
            mesh.Ellipsoid(new Vector3(-0.016f, 0.005f, 0.016f), new Vector3(0.029f, 0.032f, 0.044f), 9, 5, mitten);
            _landscape.Place(grip, "Camera_Mitten_And_Cuff", mesh).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            grip.gameObject.SetActive(false);
            return grip;
        }

        private Batch BuildEquipmentMesh(EquipmentSlot slot)
        {
            var mesh = new Batch();
            Material brass = CharacterFinish(181, 119, 65, 0.47f, 0.35f);
            Material iron = CharacterFinish(47, 66, 77, 0.34f, 0.27f);
            Material red = CharacterFinish(185, 54, 56, 0.38f);
            Material ivory = CharacterFinish(237, 219, 180, 0.36f);
            Material glow = CharacterFinish(245, 154, 52, 0.44f, 0f, 0.72f);
            switch (slot)
            {
                case EquipmentSlot.Chocolate:
                    mesh.Frustum(Vector3.down * 0.105f, Vector3.up * 0.197f, 0.102f, 0.098f, 11, red);
                    mesh.Frustum(Vector3.down * 0.028f, Vector3.up * 0.116f, 0.104f, 0.102f, 11, ivory);
                    mesh.Frustum(Vector3.up * 0.18f, Vector3.up * 0.261f, 0.097f, 0.084f, 11, brass);
                    mesh.Frustum(Vector3.up * 0.258f, Vector3.up * 0.284f, 0.083f, 0.081f, 11, iron);
                    mesh.Box(new Vector3(0f, 0.04f, -0.101f), new Vector3(0.083f, 0.091f, 0.011f), brass);
                    break;
                case EquipmentSlot.Cookie:
                {
                    Material biscuit = CharacterFinish(169, 98, 48);
                    Vector3 center = new Vector3(0.005f, 0.113f, 0.012f);
                    mesh.Frustum(center + Vector3.back * 0.033f, center + Vector3.forward * 0.035f, 0.141f, 0.135f, 9, biscuit);
                    for (int i = 0; i < 6; i++)
                    {
                        float angle = i * 2.39f;
                        Vector3 dot = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (0.046f + i * 0.011f);
                        foreach (float side in new[] { -1f, 1f })
                            mesh.Frustum(dot + Vector3.forward * (side * 0.033f), dot + Vector3.forward * (side * 0.042f),
                                0.018f, 0.015f, 6, ivory);
                    }
                    break;
                }
                case EquipmentSlot.Peppermint:
                {
                    Vector3 point = new Vector3(-0.033f, -0.108f, 0.014f);
                    for (int band = 0; band < 8; band++)
                    {
                        Vector3 end = point + Vector3.up * 0.039f;
                        mesh.Frustum(point, end, 0.031f, 0.031f, 8, band % 2 == 0 ? ivory : red);
                        point = end;
                    }
                    Vector3 center = point + Vector3.right * 0.072f;
                    for (int band = 1; band <= 7; band++)
                    {
                        float angle = Mathf.PI * (1f - band / 7f);
                        Vector3 end = center + 0.072f * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                        mesh.Frustum(point, end, 0.031f, 0.031f, 8, band % 2 == 0 ? red : ivory);
                        point = end;
                    }
                    break;
                }
                case EquipmentSlot.Garland:
                {
                    Vector3 center = new Vector3(0f, 0.081f, 0.01f);
                    for (int i = 0; i < 14; i++)
                    {
                        Quaternion a = Quaternion.Euler(0f, 0f, i * 360f / 14f);
                        Vector3 from = center + a * new Vector3(0f, 0.135f, -0.014f);
                        Vector3 to = center + Quaternion.Euler(0f, 0f, (i + 1f) * 360f / 14f) * new Vector3(0f, 0.135f, -0.014f);
                        mesh.Frustum(from, to, 0.015f, 0.015f, 5, iron);
                        mesh.Frustum(from + Vector3.forward * 0.039f, to + Vector3.forward * 0.039f, 0.015f, 0.015f, 5, iron);
                        if (i % 3 == 0)
                            mesh.Frustum(from, from + a * new Vector3(0f, 0.045f, 0.022f), 0.021f, 0.013f, 6, i == 6 ? red : glow);
                    }
                    break;
                }
                case EquipmentSlot.Firecracker:
                    mesh.Frustum(Vector3.down * 0.108f, Vector3.up * 0.18f, 0.082f, 0.076f, 9, red);
                    mesh.Frustum(Vector3.down * 0.108f, Vector3.down * 0.076f, 0.084f, 0.084f, 9, brass);
                    mesh.Frustum(Vector3.up * 0.157f, Vector3.up * 0.188f, 0.080f, 0.075f, 9, brass);
                    mesh.Frustum(Vector3.up * 0.187f, new Vector3(0.026f, 0.236f, 0f), 0.015f, 0.012f, 6, iron);
                    mesh.Frustum(new Vector3(0.026f, 0.236f, 0f), new Vector3(0.058f, 0.23f, -0.01f), 0.012f, 0.010f, 6, iron);
                    break;
                case EquipmentSlot.Extractor:
                    mesh.Box(new Vector3(0f, 0.048f, 0.014f), new Vector3(0.233f, 0.19f, 0.163f), iron);
                    mesh.Box(new Vector3(0f, 0.17f, 0.008f), new Vector3(0.163f, 0.099f, 0.14f), brass);
                    mesh.Box(new Vector3(0f, 0.182f, -0.064f), new Vector3(0.096f, 0.041f, 0.014f), glow);
                    foreach (float side in new[] { -1f, 1f })
                    {
                        mesh.Frustum(new Vector3(0.101f * side, 0.076f, 0f),
                            new Vector3(0.158f * side, -0.024f, 0.04f), 0.035f, 0.028f, 6, brass);
                        mesh.Box(new Vector3(0.068f * side, -0.059f, 0.031f), new Vector3(0.079f, 0.071f, 0.13f), iron);
                    }
                    break;
                case EquipmentSlot.Sword:
                    BuildForgeSword(mesh, brass, iron, glow);
                    break;
            }
            return mesh;
        }

        private void BuildForgeSword(Batch mesh, Material brass, Material iron, Material glow)
        {
            Material steel = CharacterFinish(120, 175, 196, 0.47f, 0.49f);
            Material edge = CharacterFinish(192, 227, 233, 0.43f, 0.30f);
            Material grip = CharacterFinish(100, 60, 45);
            mesh.Frustum(Vector3.down * 0.164f, Vector3.up * 0.125f, 0.033f, 0.032f, 9, grip);
            mesh.Frustum(Vector3.down * 0.182f, Vector3.down * 0.143f, 0.048f, 0.044f, 8, brass);
            for (int wrap = 0; wrap < 5; wrap++)
            {
                Vector3 at = Vector3.up * (-0.119f + wrap * 0.045f);
                mesh.Frustum(at, at + Vector3.up * 0.017f, 0.035f, 0.036f, 8, iron);
            }
            mesh.Box(Vector3.up * 0.128f, new Vector3(0.264f, 0.054f, 0.083f), brass);
            foreach (float side in new[] { -1f, 1f })
                mesh.Frustum(new Vector3(side * 0.097f, 0.132f, 0f),
                    new Vector3(side * 0.16f, 0.172f, 0f), 0.043f, 0.027f, 7, brass);
            Vector3[] bevel = { new(-0.081f, 0f, 0f), new(0f, 0f, -0.031f), new(0.081f, 0f, 0f), new(0f, 0f, 0.031f) };
            for (int i = 0; i < bevel.Length; i++)
            {
                Vector3 a = bevel[i], b = bevel[(i + 1) % bevel.Length];
                Vector3 tipA = Vector3.up * 0.548f + a * 0.81f, tipB = Vector3.up * 0.548f + b * 0.81f;
                Material face = i % 2 == 0 ? steel : edge;
                mesh.Quad(Vector3.up * 0.144f + a, tipA, tipB, Vector3.up * 0.144f + b, face);
                mesh.Triangle(tipA, Vector3.up * 0.665f, tipB, face);
            }
            foreach (float side in new[] { -1f, 1f })
                mesh.Box(new Vector3(0f, 0.244f, side * 0.032f), new Vector3(0.013f, 0.146f, 0.012f), glow);
        }
    }
}
