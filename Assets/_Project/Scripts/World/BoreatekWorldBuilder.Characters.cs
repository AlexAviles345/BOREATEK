using UnityEngine;
using Boreatek.Player;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;

namespace Boreatek.World
{
    public partial class BoreatekWorldBuilder
    {
        private readonly struct ElfVisuals
        {
            internal readonly Transform Root, Pickaxe, LowerGauge, UpperGauge, RightGrip;
            internal readonly Renderer Head, LampLens, Crystal;
            internal readonly Light Headlamp;
            internal readonly ElfPose Pose;

            internal ElfVisuals(Transform root, Transform pickaxe, Transform lowerGauge, Transform upperGauge,
                Renderer head, Renderer lampLens, Renderer crystal, Light headlamp, ElfPose pose, Transform rightGrip)
            {
                Root = root;
                Pickaxe = pickaxe;
                LowerGauge = lowerGauge;
                UpperGauge = upperGauge;
                Head = head;
                LampLens = lampLens;
                Crystal = crystal;
                Headlamp = headlamp;
                Pose = pose;
                RightGrip = rightGrip;
            }
        }

        private ElfVisuals BuildElfModel(Transform player, Color32? workUniform = null)
        {
            Transform rig = CharacterPart(player, "ElfEngineer_ChibiModel3D", Vector3.zero);
            Material cloth = CharacterFinish(83, 94, 58);
            Material olive = CharacterFinish(105, 117, 73);
            Material leather = CharacterFinish(104, 59, 42, 0.24f);
            Material fur = CharacterFinish(190, 177, 148);
            Material brass = CharacterFinish(185, 132, 66, 0.49f, 0.46f);
            if (workUniform.HasValue)
            {
                cloth = _landscape.Finish(workUniform.Value, smoothness: 0.19f);
                olive = _landscape.Finish(Color.Lerp(workUniform.Value, new Color(0.58f, 0.44f, 0.31f), 0.28f));
            }
            Vector3 waist = Vector3.up * 0.37f;
            Transform torso = CharacterPart(rig, "Waist", waist);
            Transform body = CharacterPart(torso, "Torso", -waist);
            BuildElfCoat(body, cloth, olive, leather, fur, brass);
            Vector3 neck = new Vector3(0f, 0.817f, -0.025f);
            Transform headJoint = CharacterPart(body, "Neck", neck);
            Renderer head = BuildElfHead(CharacterPart(headJoint, "Head", -neck), fur, brass, !workUniform.HasValue,
                out Light lamp, out Renderer lens);
            ElfPose.Arm left = BuildElfArm(body, -1, cloth, olive, fur, out _);
            ElfPose.Arm right = BuildElfArm(body, 1, cloth, olive, fur, out Transform toolHand);
            var pose = new ElfPose(torso, headJoint, left, right,
                BuildElfLeg(rig, -1, cloth, fur), BuildElfLeg(rig, 1, cloth, fur));
            if (workUniform.HasValue)
            {
                var apron = new Batch();
                apron.Box(new Vector3(0f, 0.514f, 0.266f), new Vector3(0.292f, 0.35f, 0.035f), leather);
                apron.Box(new Vector3(0f, 0.42f, 0.29f), new Vector3(0.134f, 0.084f, 0.025f), brass);
                _landscape.Place(body, "Leather_Work_Apron", apron);
                return new ElfVisuals(rig, null, null, null, head, null, null, null, pose, toolHand);
            }

            BuildElfBackpack(body, brass, leather, out Transform lowerGauge, out Transform upperGauge);
            Transform pickaxe = BuildElfPickaxe(toolHand, leather, brass, out Renderer crystal);
            return new ElfVisuals(rig, pickaxe, lowerGauge, upperGauge, head, lens, crystal, lamp, pose, toolHand);
        }

        private void BuildElfCoat(Transform rig, Material cloth, Material olive, Material leather, Material fur, Material brass)
        {
            var body = new Batch();
            body.Ellipsoid(new Vector3(0f, 0.525f, 0f), new Vector3(0.294f, 0.309f, 0.223f), 16, 8, cloth);
            body.Frustum(new Vector3(0f, 0.285f, 0f), new Vector3(0f, 0.344f, 0f), 0.246f, 0.297f, 14, leather);
            body.Ellipsoid(new Vector3(0f, 0.785f, -0.014f), new Vector3(0.271f, 0.097f, 0.217f), 16, 6, fur);
            body.Box(new Vector3(0f, 0.526f, 0.222f), new Vector3(0.051f, 0.415f, 0.031f), leather);
            for (int row = 0; row < 4; row++)
            {
                float y = 0.393f + row * 0.084f;
                body.Frustum(new Vector3(0.009f, y, 0.238f), new Vector3(0.009f, y, 0.250f), 0.014f, 0.011f, 7, brass);
                for (int column = -2; column <= 2; column++)
                {
                    if (column == 0) continue;
                    float x = column * 0.086f + (row % 2 == 0 ? 0f : Mathf.Sign(column) * 0.014f);
                    float z = 0.219f - Mathf.Abs(column) * 0.021f - Mathf.Abs(y - 0.55f) * 0.24f;
                    body.Box(new Vector3(x, y, z), new Vector3(0.077f, 0.077f, 0.026f), olive,
                        Quaternion.Euler(0f, 14f * column, 45f));
                }
            }

            for (int side = -1; side <= 1; side += 2)
            {
                body.Box(new Vector3(side * 0.163f, 0.445f, 0.193f), new Vector3(0.116f, 0.115f, 0.027f),
                    leather, Quaternion.Euler(0f, side * 15f, side * -5f));
                body.Box(new Vector3(side * 0.149f, 0.724f, 0.164f), new Vector3(0.064f, 0.165f, 0.029f),
                    leather, Quaternion.Euler(-12f, 0f, side * -13f));
                body.Box(new Vector3(side * 0.167f, 0.678f, 0.192f), new Vector3(0.069f, 0.045f, 0.018f), brass);
                body.Box(new Vector3(side * 0.167f, 0.678f, 0.203f), new Vector3(0.042f, 0.021f, 0.01f), leather);
            }
            _landscape.Place(rig, "Elf_QuiltedCoat", body);
        }

        private ElfPose.Leg BuildElfLeg(Transform rig, int side, Material cloth, Material fur)
        {
            string label = side < 0 ? "Left" : "Right";
            Transform hip = CharacterPart(rig, label + "Hip", new Vector3(side * 0.151f, 0.336f, 0f));
            Transform ankle = CharacterPart(hip, label + "Ankle", new Vector3(0f, -0.215f, 0f));
            var trouser = new Batch();
            trouser.Frustum(Vector3.up * 0.03f, Vector3.down * 0.16f, 0.098f, 0.086f, 9, cloth);
            _landscape.Place(hip, label + "Trouser", trouser);

            var foot = new Batch();
            Material sole = CharacterFinish(49, 46, 43);
            Material boot = CharacterFinish(86, 69, 56, 0.23f);
            foot.Ellipsoid(new Vector3(0f, 0.083f, 0.015f), new Vector3(0.103f, 0.118f, 0.116f), 11, 6, boot);
            foot.Ellipsoid(new Vector3(0f, -0.032f, 0.075f), new Vector3(0.121f, 0.081f, 0.183f), 12, 6, boot);
            foot.Ellipsoid(new Vector3(0f, -0.082f, 0.075f), new Vector3(0.126f, 0.035f, 0.188f), 12, 5, sole);
            foot.Frustum(new Vector3(0f, 0.108f, 0.018f), new Vector3(0f, 0.144f, 0.012f), 0.106f, 0.106f, 10, fur);
            for (int tread = 0; tread < 3; tread++)
            {
                foot.Box(new Vector3(0f, -0.101f, -0.037f + 0.098f * tread), new Vector3(0.221f, 0.028f, 0.034f), sole);
                foot.Box(new Vector3(0f, 0.026f + tread * 0.021f, 0.183f - tread * 0.017f),
                    new Vector3(0.106f, 0.015f, 0.017f), fur, Quaternion.Euler(0f, 0f, -7f + 7f * tread));
            }
            _landscape.Place(ankle, label + "Boot", foot);
            return new ElfPose.Leg(hip, ankle);
        }

        private ElfPose.Arm BuildElfArm(Transform body, int side, Material cloth, Material olive, Material fur,
            out Transform hand)
        {
            string label = side < 0 ? "Left" : "Right";
            var shoulder = new Vector3(side * 0.236f, 0.698f, 0.016f);
            var elbow = new Vector3(side * 0.335f, 0.529f, 0.056f) - shoulder;
            var wrist = new Vector3(side * 0.022f, -0.062f, 0.100f);
            Transform upper = CharacterPart(body, label + "Shoulder", shoulder);
            Transform lower = CharacterPart(upper, label + "Elbow", elbow);
            hand = CharacterPart(lower, label + "Grip", wrist + new Vector3(side * 0.004f, -0.002f, 0.070f));

            var sleeve = new Batch();
            sleeve.Ellipsoid(Vector3.zero, new Vector3(0.095f, 0.104f, 0.110f), 11, 6, olive);
            sleeve.Frustum(Vector3.zero, elbow, 0.106f, 0.085f, 10, cloth);
            _landscape.Place(upper, label + "Sleeve", sleeve);
            var cuff = new Batch();
            Material glove = CharacterFinish(132, 119, 95);
            cuff.Frustum(Vector3.zero, wrist, 0.090f, 0.077f, 10, olive);
            cuff.Frustum(wrist * 0.76f, wrist, 0.089f, 0.091f, 10, fur);
            cuff.Ellipsoid(wrist + new Vector3(0f, -0.008f, 0.045f), new Vector3(0.075f, 0.067f, 0.079f), 12, 6, glove);
            cuff.Ellipsoid(wrist + new Vector3(-side * 0.053f, 0.014f, 0.059f),
                new Vector3(0.033f, 0.034f, 0.042f), 8, 5, glove);
            _landscape.Place(lower, label + "MittenAndCuff", cuff);
            return new ElfPose.Arm(upper, lower);
        }

        private Renderer BuildElfHead(Transform rig, Material fur, Material brass, bool powered,
            out Light lamp, out Renderer lens)
        {
            var head = new Batch();
            Material skin = CharacterFinish(230, 172, 141, 0.32f);
            Material blush = CharacterFinish(208, 121, 105, 0.31f);
            Material hair = CharacterFinish(72, 42, 32);
            Material white = CharacterFinish(244, 232, 206, 0.29f);
            Material iris = CharacterFinish(115, 80, 45, 0.35f);
            Material pupil = CharacterFinish(22, 21, 20, 0.46f);
            Material shell = CharacterFinish(77, 96, 76, 0.44f, 0.36f);
            Material steel = CharacterFinish(49, 64, 60, 0.42f, 0.38f);
            Material lensBase = CharacterFinish(143, 97, 45, 0.47f);

            head.Ellipsoid(new Vector3(0f, 1.004f, -0.069f), new Vector3(0.253f, 0.239f, 0.224f), 16, 8, fur);
            head.Ellipsoid(new Vector3(0f, 1.005f, 0.04f), new Vector3(0.244f, 0.224f, 0.206f), 20, 12, skin);
            for (int side = -1; side <= 1; side += 2)
            {
                var earBase = new Vector3(side * 0.215f, 1.008f, 0.022f);
                var earMiddle = new Vector3(side * 0.286f, 1.054f, 0.019f);
                var earTip = new Vector3(side * 0.421f, 1.143f, 0.006f);
                head.Frustum(earBase, earMiddle, 0.063f, 0.061f, 9, skin);
                head.Frustum(earMiddle, earTip, 0.064f, 0f, 9, skin);
                Vector3 inset = Vector3.forward * 0.045f;
                head.Ellipsoid(earMiddle + inset, new Vector3(0.064f, 0.026f, 0.013f), 11, 6, blush,
                    Quaternion.Euler(0f, 0f, side * 32f));

                head.Ellipsoid(new Vector3(side * 0.102f, 1.026f, 0.220f), new Vector3(0.065f, 0.065f, 0.032f),
                    14, 8, white, Quaternion.Euler(0f, side * 14f, side * -4f));
                head.Ellipsoid(new Vector3(side * 0.101f, 1.026f, 0.248f), new Vector3(0.036f, 0.042f, 0.016f), 13, 7, iris);
                head.Ellipsoid(new Vector3(side * 0.099f, 1.029f, 0.261f), new Vector3(0.024f, 0.031f, 0.009f), 11, 7, pupil);
                head.Ellipsoid(new Vector3(side * 0.099f - 0.008f, 1.044f, 0.271f), new Vector3(0.009f, 0.012f, 0.004f), 8, 5, white);
                head.Ellipsoid(new Vector3(side * 0.153f, 0.958f, 0.200f), new Vector3(0.055f, 0.036f, 0.021f),
                    12, 6, blush, Quaternion.Euler(0f, side * 26f, side * 4f));
                head.Frustum(new Vector3(side * 0.055f, 1.094f, 0.228f), new Vector3(side * 0.11f, 1.112f, 0.226f), 0.013f, 0.017f, 7, hair);
                head.Frustum(new Vector3(side * 0.11f, 1.112f, 0.226f), new Vector3(side * 0.154f, 1.097f, 0.213f), 0.017f, 0.007f, 7, hair);
            }
            head.Ellipsoid(new Vector3(0f, 0.981f, 0.256f), new Vector3(0.059f, 0.039f, 0.054f), 15, 8, blush);
            head.Frustum(new Vector3(-0.085f, 0.935f, 0.229f), new Vector3(-0.033f, 0.919f, 0.237f), 0.005f, 0.007f, 6, hair);
            head.Frustum(new Vector3(-0.033f, 0.919f, 0.237f), new Vector3(0.038f, 0.921f, 0.237f), 0.007f, 0.007f, 6, hair);
            head.Frustum(new Vector3(0.038f, 0.921f, 0.237f), new Vector3(0.085f, 0.936f, 0.229f), 0.007f, 0.005f, 6, hair);

            head.Ellipsoid(new Vector3(0f, 1.167f, -0.017f), new Vector3(0.280f, 0.229f, 0.259f), 22, 10, shell, dome: true);
            head.Frustum(new Vector3(0f, 1.149f, -0.014f), new Vector3(0f, 1.178f, -0.014f), 0.291f, 0.28f, 24, steel);
            for (int i = -2; i <= 2; i++)
            {
                float x = i * 0.064f;
                head.Frustum(new Vector3(x - 0.025f, 1.163f, 0.203f),
                    new Vector3(x + 0.012f, 1.112f + Mathf.Abs(i) * 0.009f, 0.219f - Mathf.Abs(i) * 0.013f),
                    0.039f, 0f, 6, hair);
            }

            var lampFace = new Vector3(0f, 1.263f, 0.294f);
            head.Frustum(lampFace - Vector3.forward * 0.083f, lampFace, 0.083f, 0.079f, 18, brass);
            head.Frustum(lampFace, lampFace + Vector3.forward * 0.01f, 0.061f, 0.058f, 18, lensBase);
            Renderer headRenderer = _landscape.Place(rig, "Elf_HeadAndMiningHelmet", head).GetComponent<Renderer>();
            lamp = null;
            lens = null;
            if (!powered) return headRenderer;

            var lampGlow = new Batch();
            Material warmGlow = CharacterFinish(255, 204, 122, 0.50f, 0f, 1.65f);
            lampGlow.Frustum(lampFace + Vector3.forward * 0.011f, lampFace + Vector3.forward * 0.014f, 0.047f, 0.044f, 16, warmGlow);
            lens = _landscape.Place(rig, "Elf_HeadlampGlow", lampGlow).GetComponent<Renderer>();
            lamp = CharacterPart(rig, "Helmet_HeadlampSpotLight", lampFace + Vector3.forward * 0.10f).gameObject.AddComponent<Light>();
            lamp.type = LightType.Spot;
            lamp.color = new Color(1.0f, 0.90f, 0.70f);
            lamp.intensity = 5.5f;
            lamp.range = 24f;
            lamp.spotAngle = 62f;
            return headRenderer;
        }

        private void BuildElfBackpack(Transform rig, Material brass, Material leather, out Transform lower, out Transform upper)
        {
            var tank = new Batch();
            Material buckle = CharacterFinish(109, 88, 62, 0.45f, 0.33f);
            Vector3 tip = new Vector3(0f, 0.811f, -0.333f);
            Vector3 foot = new Vector3(0f, 0.354f, -0.333f);
            tank.Frustum(foot, tip, 0.150f, 0.15f, 18, brass);
            tank.Ellipsoid(tip, new Vector3(0.148f, 0.067f, 0.149f), 18, 7, brass);
            tank.Ellipsoid(foot, new Vector3(0.148f, 0.061f, 0.149f), 18, 7, brass);
            tank.Frustum(tip + Vector3.up * 0.037f, tip + Vector3.up * 0.119f, 0.047f, 0.041f, 10, buckle);
            tank.Frustum(tip + Vector3.up * 0.114f, tip + Vector3.up * 0.137f, 0.061f, 0.055f, 9, brass);
            for (int band = 0; band < 2; band++)
            {
                Vector3 ring = foot + Vector3.up * (0.068f + band * 0.304f);
                tank.Frustum(ring, ring + Vector3.up * 0.042f, 0.158f, 0.158f, 18, leather);
            }
            Vector3 tubing1 = new Vector3(0.121f, 0.864f, -0.349f);
            Vector3 tubing2 = new Vector3(0.205f, 0.746f, -0.369f);
            Vector3 tubing3 = new Vector3(0.192f, 0.393f, -0.354f);
            tank.Frustum(tip + Vector3.up * 0.093f, tubing1, 0.022f, 0.021f, 7, buckle);
            tank.Frustum(tubing1, tubing2, 0.021f, 0.019f, 7, leather);
            tank.Frustum(tubing2, tubing3, 0.019f, 0.019f, 7, leather);
            tank.Frustum(tubing3, foot + Vector3.up * 0.018f, 0.019f, 0.02f, 7, buckle);
            _landscape.Place(rig, "Elf_PressurizedBrassTank", tank);
            Shader paint = Resources.Load<Shader>("Boreatek/GaugePaint") ??
                Shader.Find("Universal Render Pipeline/Unlit") ?? _urpLitShader;
            Material bezel = GaugePaint(paint, new Color32(185, 132, 66, 255));
            Material paper = GaugePaint(paint, new Color32(233, 219, 188, 255));
            Material marking = GaugePaint(paint, new Color32(56, 44, 43, 255));
            Material enamel = GaugePaint(paint, new Color32(176, 46, 49, 255));
            lower = BuildElfGauge(rig, "Elf_LowerGauge", new Vector3(0.070f, 0.513f, -0.474f), bezel, paper, marking, enamel);
            upper = BuildElfGauge(rig, "Elf_UpperGauge", new Vector3(0.070f, 0.679f, -0.474f), bezel, paper, marking, enamel);
        }

        private Material GaugePaint(Shader shader, Color color) =>
            _landscape.Own(new Material(shader) { name = "M_Elf_Gauge_Paint", color = color * 0.72f });

        private Transform BuildElfGauge(Transform rig, string name, Vector3 position, Material bezel, Material paper,
            Material marking, Material enamel)
        {
            Transform gauge = CharacterPart(rig, name, position);
            gauge.localRotation = Quaternion.Euler(0f, -22f, 0f);
            var dial = new Batch();
            dial.Frustum(Vector3.zero, Vector3.back * 0.034f, 0.061f, 0.059f, 16, bezel);
            dial.Frustum(Vector3.back * 0.034f, Vector3.back * 0.037f, 0.048f, 0.048f, 16, paper);
            for (int tick = 0; tick < 5; tick++)
            {
                Quaternion pose = Quaternion.Euler(0f, 0f, -120f + tick * 60f);
                dial.Box(pose * (Vector3.up * 0.039f) + Vector3.back * 0.037f,
                    new Vector3(0.005f, 0.011f, 0.002f), marking, pose);
            }
            _landscape.Place(gauge, "DialAndTicks", dial);
            Transform needle = CharacterPart(gauge, "NeedlePivot", Vector3.back * 0.041f);
            var pointer = new Batch();
            pointer.Box(new Vector3(0f, 0.014f, 0f), new Vector3(0.006f, 0.041f, 0.005f), enamel);
            _landscape.Place(needle, "TemperatureNeedle", pointer);
            return needle;
        }

        private Transform BuildElfPickaxe(Transform rig, Material leather, Material brass, out Renderer crystalRenderer)
        {
            Transform grip = CharacterPart(rig, "IndustrialPickaxe_Pivot", Vector3.zero);
            grip.localRotation = Quaternion.Euler(11f, -14f, -21f);
            var frame = new Batch();
            var crystals = new Batch();
            Material wood = CharacterFinish(112, 69, 46);
            Material steel = CharacterFinish(95, 116, 132, 0.53f, 0.40f);
            Material ruby = CharacterFinish(225, 44, 67, 0.41f, 0f, 2.1f);
            frame.Frustum(Vector3.down * 0.245f, Vector3.up * 0.475f, 0.034f, 0.027f, 10, wood);
            for (int i = 0; i < 8; i++)
            {
                Vector3 fold = Vector3.up * (-0.167f + 0.042f * i);
                frame.Frustum(fold, fold + Vector3.up * 0.018f, 0.039f, 0.040f, 9, leather);
            }
            frame.Frustum(Vector3.up * 0.355f, Vector3.up * 0.469f, 0.050f, 0.049f, 10, brass);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 hub = new Vector3(side * 0.035f, 0.443f, 0.012f);
                Vector3 shoulder = new Vector3(side * 0.166f, 0.411f, 0.020f);
                Vector3 point = new Vector3(side * 0.239f, 0.364f, 0.031f);
                frame.Frustum(hub, shoulder, 0.058f, 0.043f, 8, steel);
                frame.Frustum(shoulder, point, 0.039f, 0.026f, 7, steel);
                Vector3 socket = PickaxeStroke.BladeBase;
                socket.x *= side;
                frame.Frustum(socket, point, 0.043f, 0.035f, 7, brass);
                Vector3 crystalTip = PickaxeStroke.BladeTip;
                crystalTip.x *= side;
                AddCrystal(crystals, socket, crystalTip, 0.033f, ruby, ruby);
            }
            _landscape.Place(grip, "Elf_PickaxeSteelAndLeather", frame);
            crystalRenderer = _landscape.Place(grip, "Elf_TwinCandyCrystalTips", crystals).GetComponent<Renderer>();
            return grip;
        }

        private Material CharacterFinish(byte r, byte g, byte b, float smoothness = 0.18f, float metallic = 0f, float glow = 0f)
        {
            return _landscape.Finish(new Color32(r, g, b, 255), glow, smoothness, metallic);
        }

        private static Transform CharacterPart(Transform parent, string name, Vector3 offset)
        {
            Transform part = new GameObject(name).transform;
            part.SetParent(parent, false);
            part.localPosition = offset;
            return part;
        }
    }
}
