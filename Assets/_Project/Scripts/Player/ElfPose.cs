using UnityEngine;

namespace Boreatek.Player
{
    internal sealed class ElfPose
    {
        internal readonly struct Arm
        {
            internal readonly Transform Shoulder, Elbow;

            internal Arm(Transform shoulder, Transform elbow)
            {
                Shoulder = shoulder;
                Elbow = elbow;
            }
        }

        internal readonly struct Leg
        {
            internal readonly Transform Hip, Ankle;
            internal readonly Vector3 Rest;

            internal Leg(Transform hip, Transform ankle)
            {
                Hip = hip;
                Ankle = ankle;
                Rest = hip.localPosition;
            }
        }

        internal enum Gesture { Reach, Place, Throw, Consume }

        private readonly Transform _waist, _neck;
        private readonly Vector3 _waistRest;
        private readonly Arm _leftArm, _rightArm;
        private readonly Leg _leftLeg, _rightLeg;
        private Vector3 _travel, _hitDirection, _gestureDirection;
        private float _pace, _run, _cycle, _air, _landing, _hit, _gestureTime;
        private bool _grounded = true;
        private Gesture _gesture;
        private const float HitDuration = 0.34f;
        internal const float GestureDuration = 0.42f;
        internal Transform Waist => _waist;
        internal Transform Neck => _neck;
        internal Arm LeftArm => _leftArm;
        internal Arm RightArm => _rightArm;

        internal ElfPose(Transform waist, Transform neck, Arm leftArm, Arm rightArm, Leg leftLeg, Leg rightLeg)
        {
            _waist = waist;
            _neck = neck;
            _waistRest = waist.localPosition;
            _leftArm = leftArm;
            _rightArm = rightArm;
            _leftLeg = leftLeg;
            _rightLeg = rightLeg;
        }

        internal void UpdateTravel(Vector3 localTravel, float pace, bool running, bool grounded, float dt)
        {
            float blend = 1f - Mathf.Exp(-12f * dt);
            _travel = Vector3.Lerp(_travel, localTravel, blend);
            _pace = Mathf.Lerp(_pace, grounded ? Mathf.Clamp01(pace) : 0f, blend);
            _run = Mathf.Lerp(_run, running ? 1f : 0f, blend);
            _air = Mathf.Lerp(_air, grounded ? 0f : 1f, blend);
            _cycle = Mathf.Repeat(_cycle + Mathf.Lerp(2.35f, 3.20f, _run) * _pace * dt, 1f);
            if (!_grounded && grounded) _landing = 0.22f;
            _grounded = grounded;
        }

        internal void BeginGesture(Gesture gesture, Vector3 direction)
        {
            _gesture = gesture;
            _gestureDirection = direction;
            _gestureTime = GestureDuration;
        }

        internal void ReactToHit(Vector3 away)
        {
            _hitDirection = away;
            _hit = HitDuration;
        }

        internal void SitOnSled()
        {
            _waist.localPosition = _waistRest - Vector3.up * 0.11f;
            _waist.localRotation = Quaternion.Euler(8f, 0f, 0f);
            _neck.localRotation = Quaternion.Euler(-6f, 0f, 0f);
            _leftLeg.Hip.localPosition = _leftLeg.Rest - Vector3.up * 0.18f;
            _rightLeg.Hip.localPosition = _rightLeg.Rest - Vector3.up * 0.18f;
            _leftLeg.Hip.localRotation = Quaternion.Euler(-72f, 0f, -8f);
            _rightLeg.Hip.localRotation = Quaternion.Euler(-72f, 0f, 8f);
            _leftLeg.Ankle.localRotation = _rightLeg.Ankle.localRotation = Quaternion.Euler(46f, 0f, 0f);
            _leftArm.Shoulder.localRotation = Quaternion.Euler(-46f, 8f, 0f);
            _rightArm.Shoulder.localRotation = Quaternion.Euler(-46f, -8f, 0f);
            _leftArm.Elbow.localRotation = _rightArm.Elbow.localRotation = Quaternion.Euler(-23f, 0f, 0f);
        }

        internal void Reset()
        {
            _travel = Vector3.zero;
            _pace = _run = _cycle = _air = _landing = _hit = _gestureTime = 0f;
            _grounded = true;
            Update(0f);
        }

        internal void Update(float dt, PickaxeStroke strike = default, Vector3 aim = default, bool sword = false)
        {
            float phase = _cycle * 2f * Mathf.PI;
            float stride = Mathf.Cos(phase) * _pace;
            float recoilProgress = 1f - _hit / HitDuration;
            float recoil = Mathf.SmoothStep(0f, 1f, recoilProgress / 0.18f) *
                (1f - Mathf.SmoothStep(0f, 1f, (recoilProgress - 0.18f) / 0.82f));
            float action = Mathf.Sin(Mathf.PI * _gestureTime / GestureDuration);
            float landing = Mathf.Sin(Mathf.PI * (_landing / 0.22f));
            float twist = stride * 8f;
            if (sword) twist += strike.Raise * 23f - strike.Contact * 36f - strike.Follow * 16f;
            float lean = _travel.z * _pace * Mathf.Lerp(4f, 12f, _run) + action * (_gesture == Gesture.Place ? 22f : 5f);
            lean += recoil * _hitDirection.z * 23f;
            float roll = -_travel.x * _pace * 10f - stride * 4f - recoil * _hitDirection.x * 23f;
            _waist.localPosition = _waistRest + Vector3.up * (Mathf.Abs(Mathf.Sin(phase)) * _pace * 0.044f -
                landing * 0.065f - action * (_gesture == Gesture.Place ? 0.035f : 0f)) + _hitDirection * (recoil * 0.07f);
            _waist.localRotation = Quaternion.Euler(lean, twist + action * _gestureDirection.x * 13f, roll);
            _neck.localRotation = Quaternion.Euler(-lean * 0.50f, -twist * 0.6f, -roll * 0.55f);

            PoseLeg(_leftLeg, phase, -1f);
            PoseLeg(_rightLeg, phase + Mathf.PI, 1f);
            float armSwing = stride * Mathf.Lerp(24f, 39f, _run);
            float reach = _gesture == Gesture.Throw ? 121f : (_gesture == Gesture.Place ? 45f : _gesture == Gesture.Consume ? 101f : 81f);
            float aimTilt = Mathf.Clamp(Mathf.Asin(Mathf.Clamp(aim.y, -1f, 1f)) * Mathf.Rad2Deg, -45f, 55f);
            float toolPitch = sword ? -48f * strike.Raise - (77f + aimTilt * 0.52f) * strike.Contact - 51f * strike.Follow
                : -137f * strike.Raise + (-69f - aimTilt * 0.65f) * strike.Contact + (-31f - aimTilt * 0.54f) * strike.Follow;
            _leftArm.Shoulder.localRotation = Quaternion.Euler(
                Mathf.Lerp(armSwing, -reach - _gestureDirection.y * 25f, action) - recoil * 19f -
                _air * 23f - strike.Contact * 22f,
                action * _gestureDirection.x * 35f, action * 8f + recoil * 14f);
            _leftArm.Elbow.localRotation = Quaternion.Euler(-Mathf.Abs(stride) * 13f - action * 19f - recoil * 26f, 0f, 0f);
            _rightArm.Shoulder.localRotation = Quaternion.Euler(
                (-armSwing * 0.56f - recoil * 18f - _air * 16f) * (1f - strike.Weight) + toolPitch,
                sword ? strike.Raise * 36f - strike.Contact * 64f - strike.Follow * 56f : strike.Raise * 13f + strike.Contact * 6f,
                -recoil * 12f + (sword ? strike.Raise * 63f + strike.Contact * 11f - strike.Follow * 23f :
                    strike.Raise * 34f + strike.Contact * 41f + strike.Follow * 12f));
            _rightArm.Elbow.localRotation = Quaternion.Euler(
                (-Mathf.Abs(stride) * 11f - recoil * 20f) * (1f - strike.Weight) -
                strike.Raise * 55f + strike.Contact * 13f + strike.Follow * 23f, 0f, 0f);

            _landing = Mathf.Max(0f, _landing - dt);
            _hit = Mathf.Max(0f, _hit - dt);
            _gestureTime = Mathf.Max(0f, _gestureTime - dt);
        }

        private void PoseLeg(Leg leg, float phase, float side)
        {
            float stride = Mathf.Cos(phase) * Mathf.Lerp(33f, 54f, _run) * _pace;
            float lift = Mathf.Max(0f, -Mathf.Sin(phase)) * _pace;
            float pitch = -stride * _travel.z + _air * (side > 0f ? -25f : 18f);
            float roll = stride * _travel.x;
            leg.Hip.localRotation = Quaternion.Euler(pitch, 0f, roll);
            float stance = Vector3.Dot(leg.Hip.localRotation * Vector3.down, Vector3.down);
            float compression = (1f - stance) * -leg.Ankle.localPosition.y * (1f - _air);
            leg.Hip.localPosition = leg.Rest + Vector3.up * (lift * Mathf.Lerp(0.074f, 0.125f, _run) - compression);
            leg.Ankle.localRotation = Quaternion.Euler(-pitch * 0.88f + lift * 18f, 0f, -roll * 0.92f);
        }
    }
}
