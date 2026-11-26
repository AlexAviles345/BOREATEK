using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Boreatek.World
{
    internal sealed class SnowFootprints : MonoBehaviour
    {
        private const int Capacity = 192;
        private const int BootLoad = 5;
        private struct Print
        {
            internal Vector3 Center, Across, Forward;
            internal float Born, Lifetime, Opacity;
            internal bool Left, Carried;
        }

        private readonly Print[] _prints = new Print[Capacity];
        private readonly List<Vector3> _vertices = new(Capacity * 4);
        private readonly List<Vector2> _uv = new(Capacity * 4);
        private readonly List<Color32> _colors = new(Capacity * 4);
        private readonly List<int> _indices = new(Capacity * 6);
        private Mesh _mesh;
        private Material _material;
        private int _next, _bootSnow;
        private bool _left, _visible;

        internal static SnowFootprints Install(Transform world)
        {
            Shader shader = Resources.Load<Shader>("Boreatek/SnowFootprints");
            if (shader == null) return null;
            var root = new GameObject("Temporary_Snow_Footprints");
            root.transform.SetParent(world, false);
            var system = root.AddComponent<SnowFootprints>();
            system._mesh = new Mesh { name = "Boot impressions", hideFlags = HideFlags.DontSave };
            system._mesh.MarkDynamic();
            root.AddComponent<MeshFilter>().sharedMesh = system._mesh;
            system._material = new Material(shader) { hideFlags = HideFlags.DontSave };
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = system._material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        internal void ResetBoots() => _bootSnow = 0;

        internal void Step(RaycastHit ground, bool snow, Vector3 forward)
        {
            _left = !_left;
            if (!snow && _bootSnow == 0) return;
            Vector3 lateral = Vector3.Cross(Vector3.up, forward).normalized * (_left ? -0.115f : 0.115f);
            if (Physics.Raycast(ground.point + lateral + Vector3.up * 0.19f, Vector3.down, out RaycastHit boot,
                0.38f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && boot.collider == ground.collider)
                ground = boot;
            if (ground.normal.y < 0.61f) return;
            Vector3 toe = Vector3.ProjectOnPlane(forward, ground.normal).normalized;
            float opacity = snow ? 0.44f : 0.34f * _bootSnow / BootLoad;
            _bootSnow = snow ? BootLoad : _bootSnow - 1;
            _prints[_next] = new Print
            {
                Center = ground.point + ground.normal * 0.015f,
                Across = Vector3.Cross(ground.normal, toe).normalized * 0.091f,
                Forward = toe * 0.162f,
                Born = Time.time,
                Lifetime = snow ? 24f : 5f,
                Opacity = opacity,
                Carried = !snow,
                Left = _left
            };
            _next = (_next + 1) % Capacity;
            _visible = true;
        }

        private void LateUpdate()
        {
            if (!_visible || Time.deltaTime <= 0f) return;
            _vertices.Clear();
            _uv.Clear();
            _colors.Clear();
            _indices.Clear();
            float now = Time.time;
            foreach (Print print in _prints)
            {
                float remaining = print.Lifetime - (now - print.Born);
                if (remaining <= 0f) continue;
                float fade = Mathf.SmoothStep(0f, 1f, remaining / (print.Carried ? 3.5f : 10f));
                var tint = print.Carried ? new Color32(203, 225, 241, 255) : new Color32(48, 84, 110, 255);
                tint.a = (byte)Mathf.RoundToInt(255f * print.Opacity * fade);
                Append(print, tint);
            }
            _mesh.Clear();
            _visible = _vertices.Count > 0;
            if (!_visible) return;
            _mesh.SetVertices(_vertices);
            _mesh.SetUVs(0, _uv);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_indices, 0);
        }

        private void Append(Print print, Color32 tint)
        {
            Vector3 center = transform.InverseTransformPoint(print.Center);
            int index = _vertices.Count;
            _vertices.Add(center - print.Across - print.Forward);
            _vertices.Add(center - print.Across + print.Forward);
            _vertices.Add(center + print.Across + print.Forward);
            _vertices.Add(center + print.Across - print.Forward);
            float left = print.Left ? 1f : 0f;
            _uv.Add(new Vector2(left, 0f));
            _uv.Add(new Vector2(left, 1f));
            _uv.Add(new Vector2(1f - left, 1f));
            _uv.Add(new Vector2(1f - left, 0f));
            for (int i = 0; i < 4; i++) _colors.Add(tint);
            _indices.Add(index);
            _indices.Add(index + 1);
            _indices.Add(index + 2);
            _indices.Add(index);
            _indices.Add(index + 2);
            _indices.Add(index + 3);
        }

        private void OnDestroy()
        {
            Destroy(_mesh);
            Destroy(_material);
        }
    }
}
