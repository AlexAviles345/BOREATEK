using UnityEngine;
using Boreatek.AI;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;

namespace Boreatek.World
{
    public partial class BoreatekWorldBuilder
    {
        private enum EnemyPart { Torso, Head, Eyes, LeftArm, LeftForearm, LeftLeg, RightArm, RightForearm, RightLeg }

        private sealed class EnemyParts
        {
            private readonly Batch[] _parts = new Batch[9];

            internal Batch this[EnemyPart part] => _parts[(int)part] ??= new Batch();
        }

        private EnemyPose BuildEnemyRig(Transform parent, bool golem)
        {
            string species = golem ? "Golem" : "Soldier";
            EnemyParts source = null;
            Transform root = CharacterPart(parent, "Animated_Model", Vector3.zero);
            Vector3 waist = Vector3.up * (golem ? 1.17f : 0.80f);
            Vector3 neck = golem ? new Vector3(0f, 2.40f, 0.18f) : new Vector3(0f, 1.54f, 0f);
            Transform torso = CharacterPart(root, "Torso_Joint", waist);
            Transform head = CharacterPart(torso, "Head_Joint", neck - waist);
            Place(torso, EnemyPart.Torso, waist);
            Place(head, EnemyPart.Head, neck);
            Renderer eyes = Place(head, EnemyPart.Eyes, neck).GetComponent<Renderer>();

            EnemyPose.Limb left = BuildSide(-1);
            EnemyPose.Limb right = BuildSide(1);
            return new EnemyPose(root, torso, head, eyes, left, right, golem);

            GameObject Place(Transform joint, EnemyPart part, Vector3 pivot)
            {
                GameObject mesh = _landscape.PlaceShared(joint, species + "_" + part,
                    () => (source ??= golem ? BuildFrostGolemModel() : BuildLeadSoldierModel())[part]);
                mesh.transform.localPosition = -pivot;
                return mesh;
            }

            EnemyPose.Limb BuildSide(int side)
            {
                bool leftSide = side < 0;
                var (shoulder, elbow, hip) = EnemyJoints(side, golem);
                Transform arm = CharacterPart(torso, leftSide ? "Left_Shoulder" : "Right_Shoulder", shoulder - waist);
                Transform forearm = CharacterPart(arm, "Elbow", elbow - shoulder);
                Transform leg = CharacterPart(root, leftSide ? "Left_Hip" : "Right_Hip", hip);
                Place(arm, leftSide ? EnemyPart.LeftArm : EnemyPart.RightArm, shoulder);
                Place(forearm, leftSide ? EnemyPart.LeftForearm : EnemyPart.RightForearm, elbow);
                Place(leg, leftSide ? EnemyPart.LeftLeg : EnemyPart.RightLeg, hip);
                return new EnemyPose.Limb(arm, forearm, leg);
            }
        }

        private static (Vector3 shoulder, Vector3 elbow, Vector3 hip) EnemyJoints(int side, bool golem) => golem
            ? (new Vector3(side * 0.72f, 2.232f, -0.026f), new Vector3(side * 0.913f, 1.618f, 0.054f),
                new Vector3(side * 0.34f, 1.12f, -0.031f))
            : (new Vector3(side * 0.348f, 1.305f, -0.007f), new Vector3(side * 0.471f, 1.053f, 0.036f),
                new Vector3(side * 0.164f, 0.800f, 0f));

        private EnemyParts BuildLeadSoldierModel()
        {
            var parts = new EnemyParts();
            Batch model = parts[EnemyPart.Torso];
            Material iron = CharacterFinish(50, 52, 47, 0.26f, 0.44f);
            Material brass = CharacterFinish(151, 112, 58, 0.28f, 0.42f);
            Material verdigris = CharacterFinish(63, 114, 101, 0.30f, 0.26f);
            Material enamel = CharacterFinish(128, 52, 39, 0.33f, 0.22f);
            Material rust = CharacterFinish(89, 60, 39);
            Material bareMetal = CharacterFinish(171, 148, 105, 0.29f, 0.33f);
            Material eyeSocket = CharacterFinish(34, 26, 20);
            Material amber = CharacterFinish(255, 164, 49, 0.43f, glow: 1.55f);

            model.Frustum(new Vector3(0f, 0.74f, 0f), new Vector3(0f, 1.40f, 0f), 0.266f, 0.333f, 8, iron);
            model.Box(new Vector3(0f, 0.83f, 0.024f), new Vector3(0.56f, 0.18f, 0.43f), enamel);
            model.Frustum(new Vector3(0f, 1.08f, 0.08f), new Vector3(0f, 1.15f, 0.285f), 0.32f, 0.258f, 8, verdigris);
            model.Box(new Vector3(0f, 1.15f, 0.293f), new Vector3(0.278f, 0.155f, 0.032f), enamel);
            model.Box(new Vector3(-0.095f, 1.19f, 0.313f), new Vector3(0.068f, 0.021f, 0.008f), bareMetal);
            model.Frustum(new Vector3(0f, 1.09f, -0.213f), new Vector3(0f, 1.09f, -0.337f), 0.205f, 0.175f, 8, enamel);
            model.Frustum(new Vector3(0f, 1.09f, -0.329f), new Vector3(0f, 1.09f, -0.389f), 0.084f, 0.070f, 8, brass);
            model.Box(new Vector3(0f, 1.09f, -0.412f), new Vector3(0.31f, 0.065f, 0.039f), bareMetal,
                Quaternion.Euler(0f, 0f, 22f));
            model.Frustum(new Vector3(0f, 1.37f, 0f), new Vector3(0f, 1.58f, 0f), 0.145f, 0.125f, 10, bareMetal);

            model = parts[EnemyPart.Head];
            model.Ellipsoid(new Vector3(0f, 1.816f, 0f), new Vector3(0.337f, 0.347f, 0.305f), 22, 12, brass);
            model.Frustum(new Vector3(-0.043f, 1.877f, -0.254f), new Vector3(-0.053f, 1.887f, -0.312f),
                0.117f, 0.112f, 9, rust);
            model.Box(new Vector3(-0.053f, 1.887f, -0.317f), new Vector3(0.102f, 0.024f, 0.014f), iron,
                Quaternion.Euler(0f, 0f, 21f));
            model.Ellipsoid(new Vector3(0f, 1.619f, 0.119f), new Vector3(0.234f, 0.127f, 0.210f), 14, 8, rust);
            model.Box(new Vector3(0f, 1.683f, 0.297f), new Vector3(0.362f, 0.106f, 0.082f), eyeSocket);
            model.Box(new Vector3(0f, 1.614f, 0.309f), new Vector3(0.338f, 0.039f, 0.096f), brass);
            for (int tooth = -2; tooth <= 2; tooth++)
            {
                float x = tooth * 0.058f;
                model.Box(new Vector3(x, 1.721f, 0.337f), new Vector3(0.035f, 0.043f, 0.030f), bareMetal,
                    Quaternion.Euler(0f, 0f, tooth * 5f));
                model.Box(new Vector3(x + 0.019f, 1.642f, 0.341f), new Vector3(0.028f, 0.040f, 0.028f), brass,
                    Quaternion.Euler(0f, 0f, -tooth * 4f));
            }

            for (int side = -1; side <= 1; side += 2)
            {
                model = parts[EnemyPart.Head];
                var eye = new Vector3(side * 0.135f, 1.865f, 0.269f);
                model.Frustum(eye - Vector3.forward * 0.034f, eye + Vector3.forward * 0.025f, 0.103f, 0.107f, 14, rust);
                model.Frustum(eye + Vector3.forward * 0.024f, eye + Vector3.forward * 0.037f, 0.086f, 0.082f, 14, eyeSocket);
                parts[EnemyPart.Eyes].Ellipsoid(eye + Vector3.forward * 0.042f, new Vector3(0.057f, 0.062f, 0.025f), 14, 8, amber);

                var gear = new Vector3(side * 0.348f, 1.811f, -0.032f);
                Vector3 outward = Vector3.right * side;
                model.Frustum(gear - outward * 0.069f, gear + outward * 0.024f, 0.111f, 0.113f, 12, iron);
                for (int tooth = 0; tooth < 8; tooth++)
                {
                    Quaternion spin = Quaternion.AngleAxis(tooth * 45f, Vector3.right);
                    model.Box(gear + spin * (Vector3.up * 0.116f), new Vector3(0.057f, 0.044f, 0.038f), brass, spin);
                }
                model.Frustum(gear + outward * 0.020f, gear + outward * 0.043f, 0.040f, 0.033f, 8, bareMetal);
                Vector3 cable = gear + new Vector3(0f, -0.071f, -0.05f);
                Vector3 loop = new Vector3(side * 0.425f, 1.562f, -0.194f);
                Vector3 throat = new Vector3(side * 0.209f, 1.462f, -0.251f);
                model.Frustum(cable, loop, 0.023f, 0.027f, 8, rust);
                model.Frustum(loop, throat, 0.027f, 0.025f, 8, rust);
                model.Frustum(throat, new Vector3(side * 0.144f, 1.270f, -0.208f), 0.025f, 0.023f, 8, rust);

                var (shoulder, elbow, hip) = EnemyJoints(side, golem: false);
                var hand = new Vector3(side * 0.475f, 0.764f, 0.083f);
                model = parts[side < 0 ? EnemyPart.LeftArm : EnemyPart.RightArm];
                model.Ellipsoid(shoulder, new Vector3(0.148f, 0.152f, 0.177f), 12, 7, side < 0 ? enamel : verdigris);
                model.Frustum(shoulder, elbow, 0.113f, 0.074f, 7, bareMetal);
                model = parts[side < 0 ? EnemyPart.LeftForearm : EnemyPart.RightForearm];
                model.Frustum(elbow, hand, 0.132f, 0.100f, 7, side < 0 ? verdigris : enamel);
                model.Box(hand, new Vector3(0.193f, 0.128f, 0.179f), iron);
                for (int finger = -1; finger <= 1; finger++)
                {
                    model.Box(hand + new Vector3(finger * 0.059f, -0.080f, 0.040f),
                        new Vector3(0.047f, 0.086f, 0.10f), bareMetal, Quaternion.Euler(-22f, 0f, 0f));
                }

                model = parts[side < 0 ? EnemyPart.LeftLeg : EnemyPart.RightLeg];
                var knee = new Vector3(side * 0.219f, 0.468f, 0.045f);
                var ankle = new Vector3(side * 0.236f, 0.147f, 0.004f);
                model.Frustum(hip, knee, 0.122f, 0.091f, 7, iron);
                model.Frustum(knee, ankle, 0.145f, 0.117f, 8, verdigris);
                model.Box(knee + Vector3.forward * 0.105f, new Vector3(0.166f, 0.155f, 0.102f), enamel);
                model.Box(ankle + new Vector3(0f, -0.060f, 0.091f), new Vector3(0.294f, 0.158f, 0.426f), iron);
                model = parts[EnemyPart.Torso];
                for (int rivet = 0; rivet < 2; rivet++)
                {
                    var head = new Vector3(side * 0.183f, 1.071f + rivet * 0.164f, 0.275f);
                    model.Frustum(head, head + Vector3.forward * 0.024f, 0.023f, 0.017f, 7, bareMetal);
                }
            }
            return parts;
        }

        private EnemyParts BuildFrostGolemModel()
        {
            var parts = new EnemyParts();
            Batch model = parts[EnemyPart.Torso];
            Material bedrock = CharacterFinish(27, 49, 62);
            Material darkIce = CharacterFinish(28, 72, 101, 0.33f);
            Material glacialIce = CharacterFinish(63, 127, 155, 0.38f);
            Material frostedEdge = CharacterFinish(137, 197, 216, 0.34f);
            Material scrap = CharacterFinish(101, 69, 46, 0.24f, 0.32f);
            Material bolt = CharacterFinish(153, 121, 80, 0.26f, 0.39f);
            Material cyan = CharacterFinish(108, 230, 249, 0.45f, glow: 1.65f);

            model.Frustum(new Vector3(0f, 0.97f, -0.05f), new Vector3(-0.06f, 1.66f, 0.01f),
                0.49f, 0.55f, 6, bedrock);
            model.Frustum(new Vector3(-0.04f, 1.33f, -0.01f), new Vector3(0.01f, 2.21f, 0.017f),
                0.435f, 0.733f, 7, darkIce);
            model.Frustum(new Vector3(0.01f, 2.21f, 0.017f), new Vector3(0.11f, 2.56f, -0.045f),
                0.731f, 0.435f, 7, glacialIce);
            AddCrystal(model, new Vector3(-0.29f, 1.675f, 0.396f), new Vector3(-0.39f, 2.36f, 0.64f),
                0.24f, glacialIce, frostedEdge);
            AddCrystal(model, new Vector3(0.22f, 2.175f, 0.48f), new Vector3(0.19f, 1.53f, 0.48f),
                0.23f, darkIce, glacialIce);
            AddCrystal(model, new Vector3(0.032f, 1.69f, 0.517f), new Vector3(0.10f, 2.16f, 0.634f),
                0.045f, glacialIce, cyan);

            model = parts[EnemyPart.Head];
            model.Frustum(new Vector3(0.034f, 2.38f, 0.22f), new Vector3(-0.044f, 2.94f, 0.202f),
                0.308f, 0.417f, 6, darkIce);
            model.Frustum(new Vector3(-0.044f, 2.94f, 0.202f), new Vector3(0.028f, 3.19f, 0.137f),
                0.415f, 0.24f, 6, glacialIce);
            model.Box(new Vector3(0f, 2.82f, 0.564f), new Vector3(0.535f, 0.109f, 0.110f), bedrock);

            for (int side = -1; side <= 1; side += 2)
            {
                parts[EnemyPart.Eyes].Box(new Vector3(side * 0.133f, 2.825f, 0.625f), new Vector3(0.152f, 0.041f, 0.017f), cyan,
                    Quaternion.Euler(0f, 0f, side * 12f));
                parts[EnemyPart.Head].Box(new Vector3(side * 0.150f, 2.917f, 0.554f), new Vector3(0.300f, 0.111f, 0.177f), glacialIce,
                    Quaternion.Euler(0f, 0f, side * 15f));
                var (shoulder, elbow, hip) = EnemyJoints(side, golem: true);
                model = parts[side < 0 ? EnemyPart.LeftLeg : EnemyPart.RightLeg];
                var knee = new Vector3(side * 0.397f, 0.62f, 0.105f);
                var foot = new Vector3(side * 0.452f, 0.18f, 0.107f);
                model.Frustum(hip, knee, 0.280f, 0.254f, 5, darkIce);
                model.Frustum(knee, foot, 0.280f, 0.275f, 6, glacialIce);
                model.Frustum(foot + new Vector3(0f, -0.155f, 0.091f), foot + new Vector3(0f, 0.104f, 0.007f),
                    0.365f, 0.248f, 6, bedrock);
                AddCrystal(model, foot + new Vector3(0f, -0.071f, 0.285f), knee + Vector3.forward * 0.253f,
                    0.167f, glacialIce, frostedEdge);

                var fist = new Vector3(side * 0.933f, 0.996f, 0.145f);
                model = parts[side < 0 ? EnemyPart.LeftArm : EnemyPart.RightArm];
                model.Frustum(shoulder + Vector3.up * 0.22f, elbow, 0.396f, 0.275f, 6, glacialIce);
                var brace = new Vector3(side * 0.658f, 2.002f, 0.312f);
                model.Box(brace, new Vector3(0.340f, 0.131f, 0.230f), scrap, Quaternion.Euler(0f, 0f, side * 28f));
                model.Frustum(brace + Vector3.forward * 0.099f, brace + Vector3.forward * 0.139f, 0.046f, 0.037f, 7, bolt);
                model = parts[side < 0 ? EnemyPart.LeftForearm : EnemyPart.RightForearm];
                model.Frustum(elbow, fist, 0.345f, 0.323f, 6, darkIce);
                model.Frustum(fist, fist + new Vector3(-side * 0.032f, -0.273f, 0.043f), 0.323f, 0.198f, 6, glacialIce);
                for (int knuckle = -1; knuckle <= 1; knuckle++)
                {
                    Vector3 grip = fist + new Vector3(knuckle * 0.157f, -0.090f, 0.267f);
                    model.Frustum(grip, grip - new Vector3(0f, 0.207f, -0.031f), 0.077f, 0.050f, 5, frostedEdge);
                }
            }

            model = parts[EnemyPart.LeftArm];
            AddCrystal(model, new Vector3(-0.68f, 2.41f, -0.154f), new Vector3(-0.87f, 3.06f, -0.258f),
                0.225f, glacialIce, frostedEdge);
            AddCrystal(model, new Vector3(-0.52f, 2.44f, -0.228f), new Vector3(-0.48f, 2.89f, -0.497f),
                0.137f, darkIce, glacialIce);
            AddCrystal(parts[EnemyPart.RightArm], new Vector3(0.70f, 2.39f, -0.087f), new Vector3(1.04f, 2.81f, -0.159f),
                0.262f, darkIce, glacialIce);
            model = parts[EnemyPart.Torso];
            AddCrystal(model, new Vector3(0.18f, 2.18f, -0.404f), new Vector3(0.2f, 2.83f, -0.739f),
                0.279f, glacialIce, frostedEdge);
            model.Box(new Vector3(0.111f, 1.42f, -0.443f), new Vector3(0.70f, 0.132f, 0.18f), scrap,
                Quaternion.Euler(0f, 0f, 32f));
            return parts;
        }
    }
}
