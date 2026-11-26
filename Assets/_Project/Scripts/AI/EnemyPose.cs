using UnityEngine;

namespace Boreatek.AI
{
    internal sealed class EnemyPose
    {
        internal readonly struct Limb
        {
            internal readonly Transform Arm, Forearm, Leg;

            internal Limb(Transform arm, Transform forearm, Transform leg)
            {
                Arm = arm;
                Forearm = forearm;
                Leg = leg;
            }
        }

        private readonly Transform _root, _torso, _head;
        private readonly Renderer _eyes;
        private readonly Limb _left, _right;
        private readonly bool _heavy;
        private readonly Vector3 _torsoRest;
        private float _time, _stride, _moving;

        internal EnemyPose(Transform root, Transform torso, Transform head, Renderer eyes, Limb left, Limb right, bool heavy)
        {
            _root = root;
            _torso = torso;
            _head = head;
            _eyes = eyes;
            _left = left;
            _right = right;
            _heavy = heavy;
            _torsoRest = torso.localPosition;
        }

        internal void Animate(float dt, float travel, float attack, float hurt, float wake)
        {
            _time += dt;
            _stride = Mathf.Repeat(_stride + travel / (_heavy ? 1.55f : 1.22f), 1f);
            _moving = Mathf.MoveTowards(_moving, Mathf.Clamp01(travel / Mathf.Max(0.001f, dt * 1.8f)), dt * 7f);
            float windup = Rise(0f, 0.39f, attack) - Rise(0.39f, 0.50f, attack);
            float contact = Rise(0.39f, 0.50f, attack) - Rise(0.56f, 0.76f, attack);
            float recoil = Mathf.Sin(hurt * Mathf.PI) * 0.9f;
            float sweep = Mathf.Sin(_stride * 2f * Mathf.PI) * _moving;
            float bob = Mathf.Abs(sweep) * (_heavy ? 0.09f : 0.055f);
            float leftAttack = _heavy ? -127f * windup - 50f * contact : -30f * windup - 23f * contact;
            float rightAttack = _heavy ? -133f * windup - 55f * contact : -67f * windup - 103f * contact;

            _root.localPosition = Vector3.down * (wake * (_heavy ? 0.24f : 0.19f));
            _root.localRotation = Quaternion.identity;
            _torso.localPosition = _torsoRest + new Vector3(0f, bob, contact * (_heavy ? 0.24f : 0.34f));
            _torso.localRotation = Quaternion.Euler(4f * _moving - windup * 15f + contact * (_heavy ? 31f : 13f) -
                recoil * 17f + wake * 19f, sweep * 8f + (_heavy ? 0f : windup * 27f - contact * 23f), recoil * 13f);
            _head.localRotation = Quaternion.Euler(-contact * 14f + recoil * 19f + wake * 11f,
                Mathf.Sin(_time * 1.8f) * (1f - _moving) * (1f - contact) * 7f, -sweep * 4f);
            AnimateLimb(_left, -sweep, leftAttack, (_heavy ? 8f : 20f) * windup, recoil, wake);
            AnimateLimb(_right, sweep, rightAttack, (_heavy ? -10f : -24f) * windup, recoil, wake);
            _eyes.enabled = wake < 0.78f;
        }

        internal void Collapse(float progress)
        {
            float stumble = Rise(0f, 0.36f, progress);
            float fall = Rise(0.22f, 0.91f, progress);
            _root.localPosition = new Vector3(fall * 0.44f, -stumble * 0.15f + fall * 0.14f, -fall * 0.20f);
            _root.localRotation = Quaternion.Euler(-fall * (_heavy ? 67f : 81f), 0f, fall * 26f);
            _torso.localPosition = _torsoRest;
            _torso.localRotation = Quaternion.Euler(-14f * stumble + fall * 9f, fall * -17f, 12f * stumble);
            _head.localRotation = Quaternion.Euler(24f * stumble, fall * 12f, -fall * 23f);
            _left.Leg.localRotation = Quaternion.Euler(18f * stumble, 0f, -9f * fall);
            _right.Leg.localRotation = Quaternion.Euler(-12f * stumble, 0f, 11f * fall);
            _left.Arm.localRotation = Quaternion.Euler(-26f * stumble, 0f, -27f * fall);
            _right.Arm.localRotation = Quaternion.Euler(-32f * stumble, 0f, 32f * fall);
            _left.Forearm.localRotation = Quaternion.Euler(-37f * stumble, 0f, 0f);
            _right.Forearm.localRotation = Quaternion.Euler(-45f * stumble, 0f, 0f);
            _eyes.enabled = progress < 0.43f;
        }

        private void AnimateLimb(Limb limb, float sweep, float attack, float twist, float recoil, float wake)
        {
            limb.Leg.localRotation = Quaternion.Euler(sweep * (_heavy ? 17f : 27f) - wake * 10f, 0f, 0f);
            limb.Arm.localRotation = Quaternion.Euler(-sweep * 23f + attack - recoil * 15f + wake * 22f,
                twist, recoil * 12f);
            limb.Forearm.localRotation = Quaternion.Euler(-Mathf.Abs(sweep) * 15f +
                Mathf.Min(26f, -attack * 0.25f) - recoil * 26f, 0f, 0f);
        }

        private static float Rise(float start, float end, float value) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, value));
    }
}
