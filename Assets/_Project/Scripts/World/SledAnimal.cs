using UnityEngine;

namespace Boreatek.World
{
    internal sealed class SledAnimal : MonoBehaviour
    {
        private Transform _torso, _head, _tail, _saddle;
        private Transform[] _hips, _ankles;
        private LineRenderer _trace;
        private Transform _hitch;
        private Vector3 _rest;
        private float _phase, _pace, _time, _offset;
        private bool _reindeer;

        internal void Initialize(Transform torso, Transform head, Transform tail, Transform saddle,
            Transform[] hips, Transform[] ankles, Transform hitch, LineRenderer trace, bool reindeer, float offset)
        {
            _torso = torso;
            _head = head;
            _tail = tail;
            _saddle = saddle;
            _hips = hips;
            _ankles = ankles;
            _hitch = hitch;
            _trace = trace;
            _rest = transform.localPosition;
            _reindeer = reindeer;
            _offset = offset;
            _phase = offset;
            UpdateTrace();
        }

        internal void Animate(float speed, float dt)
        {
            _time += dt;
            _pace = Mathf.MoveTowards(_pace, Mathf.Clamp01(speed * 0.22f), dt * 3.5f);
            float strideRate = Mathf.Min(speed / (_reindeer ? 2.46f : 2.15f), _reindeer ? 3.1f : 3.8f);
            _phase = Mathf.Repeat(_phase + strideRate * dt, 1f);
            Vector3 place = transform.parent.TransformPoint(_rest);
            if (Physics.Raycast(place + Vector3.up * 0.78f, Vector3.down, out RaycastHit ground, 1.84f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && ground.normal.y > 0.53f)
            {
                float difference = Mathf.Clamp(ground.point.y - place.y, -0.52f, 0.52f);
                Vector3 local = _rest + Vector3.up * difference;
                transform.localPosition = Vector3.Lerp(transform.localPosition, local, 1f - Mathf.Exp(-19f * dt));
                Vector3 forward = Vector3.ProjectOnPlane(transform.parent.forward, ground.normal).normalized;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(forward, ground.normal), 1f - Mathf.Exp(-7f * dt));
            }
            float beat = _phase * Mathf.PI * 2f;
            float breathing = Mathf.Sin(_time * 2.05f + _offset * 6f);
            float sniff = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(_time * 0.91f + _offset * 11f)), 12f);
            _torso.localPosition = Vector3.up * (breathing * (1f - _pace) * 0.014f + Mathf.Abs(Mathf.Sin(beat)) * _pace * 0.072f);
            _torso.localRotation = Quaternion.Euler(Mathf.Sin(beat) * _pace * (_reindeer ? 6f : 8f), 0f, Mathf.Cos(beat) * _pace * 3f);
            _head.localRotation = Quaternion.Euler(sniff * (1f - _pace) * (_reindeer ? 17f : 29f) - Mathf.Sin(beat) * _pace * 8f,
                Mathf.Sin(_time * 1.31f + _offset * 5f) * (1f - _pace) * 15f, Mathf.Sin(_time * 1.82f) * 3f);
            _tail.localRotation = Quaternion.Euler(-13f - _pace * 18f,
                Mathf.Sin(_time * (_reindeer ? 2.2f : 5.2f) + _offset * 8f) * (_reindeer ? 12f : 35f), 0f);
            for (int i = 0; i < _hips.Length; i++)
            {
                float wave = beat + (i == 0 || i == 3 ? 0f : Mathf.PI) + (i >= 2 ? _pace * 0.22f : 0f);
                float swing = Mathf.Sin(wave);
                float lift = Mathf.Max(0f, Mathf.Cos(wave));
                _hips[i].localRotation = Quaternion.Euler(swing * (_reindeer ? 34f : 38f) * _pace, 0f, 0f);
                _ankles[i].localRotation = Quaternion.Euler(lift * _pace * (i < 2 ? -42f : 45f), 0f, 0f);
            }
            UpdateTrace();
        }

        private void UpdateTrace()
        {
            Vector3 a = _hitch.position, b = _saddle.position;
            for (int i = 0; i < _trace.positionCount; i++)
            {
                float t = i / (float)(_trace.positionCount - 1);
                _trace.SetPosition(i, Vector3.Lerp(a, b, t) - Vector3.up * (Mathf.Sin(t * Mathf.PI) * (0.16f - _pace * 0.12f)));
            }
        }
    }
}
