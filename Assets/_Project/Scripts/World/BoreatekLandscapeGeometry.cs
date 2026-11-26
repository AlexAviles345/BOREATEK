using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Boreatek.World
{
    /// <summary>Owns the transient geometry and shared materials of one generated environment.</summary>
    public sealed class BoreatekLandscapeGeometry : MonoBehaviour
    {
        private readonly List<Object> _assets = new List<Object>();
        private readonly Dictionary<(Color, float, float, float), Material> _materials =
            new Dictionary<(Color, float, float, float), Material>();
        private readonly Dictionary<string, (Mesh mesh, Material[] materials)> _sharedMeshes =
            new Dictionary<string, (Mesh mesh, Material[] materials)>();
        private Shader _shader;

        internal void Initialize(Shader shader) => _shader = shader;

        internal Material Finish(Color color, float emission = 0f, float smoothness = 0.2f, float metallic = 0f)
        {
            var key = (color, emission, smoothness, metallic);
            if (_materials.TryGetValue(key, out Material cached))
            {
                return cached;
            }

            var material = Own(new Material(_shader) { name = "M_Boreatek_Scenery", color = color });
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (emission > 0f && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
            }
            _materials.Add(key, material);
            return material;
        }

        internal T Own<T>(T asset) where T : Object
        {
            asset.hideFlags = HideFlags.DontSave;
            _assets.Add(asset);
            return asset;
        }

        internal static void PaintStructure(Renderer renderer, BoreatekLandscapeGeometry shared, Color color, float emission = 0f)
        {
            if (shared != null)
            {
                renderer.sharedMaterial = shared.Finish(color, emission);
                return;
            }

            Material material = renderer.material;
            material.color = color;
            if (emission > 0f && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
            }
        }

        internal GameObject Place(Transform parent, string name, Batch batch, bool collision = false)
        {
            Mesh mesh = Own(batch.Build("SM_" + name));
            return PlaceMesh(parent, name, mesh, batch.Materials.ToArray(), collision);
        }

        internal GameObject PlaceShared(Transform parent, string name, System.Func<Batch> build)
        {
            if (!_sharedMeshes.TryGetValue(name, out var model))
            {
                Batch batch = build();
                model = (Own(batch.Build("SM_" + name)), batch.Materials.ToArray());
                _sharedMeshes.Add(name, model);
            }
            return PlaceMesh(parent, name, model.mesh, model.materials, collision: false);
        }

        private static GameObject PlaceMesh(Transform parent, string name, Mesh mesh, Material[] materials, bool collision)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterials = materials;
            if (collision) obj.AddComponent<MeshCollider>().sharedMesh = mesh;
            return obj;
        }

        private void OnDestroy()
        {
            foreach (Object asset in _assets)
            {
                if (asset == null) continue;
                if (Application.isPlaying) Destroy(asset);
                else DestroyImmediate(asset);
            }
        }

        internal sealed class Batch
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Vector3> _normals = new List<Vector3>();
            private readonly List<List<int>> _triangles = new List<List<int>>();
            private readonly Dictionary<Material, int> _slots = new Dictionary<Material, int>();
            internal readonly List<Material> Materials = new List<Material>();

            internal void Triangle(Vector3 a, Vector3 b, Vector3 c, Material material)
            {
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                AddTriangle((a, normal), (b, normal), (c, normal), GetIndices(material));
            }

            private void AddTriangle((Vector3 p, Vector3 n) a, (Vector3 p, Vector3 n) b,
                (Vector3 p, Vector3 n) c, List<int> indices)
            {
                int index = _vertices.Count;
                _vertices.Add(a.p);
                _vertices.Add(b.p);
                _vertices.Add(c.p);
                _normals.Add(a.n);
                _normals.Add(b.n);
                _normals.Add(c.n);
                indices.Add(index);
                indices.Add(index + 1);
                indices.Add(index + 2);
            }

            internal void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Material material)
            {
                Triangle(a, b, c, material);
                Triangle(a, c, d, material);
            }

            internal void Ellipsoid(Vector3 center, Vector3 radii, int sides, int bands, Material material,
                Quaternion? rotation = null, bool dome = false)
            {
                Quaternion orientation = rotation ?? Quaternion.identity;
                float low = dome ? 0f : -Mathf.PI * 0.5f;
                float high = Mathf.PI * 0.5f;
                var indices = GetIndices(material);
                for (int band = 0; band < bands; band++)
                {
                    float phi0 = Mathf.Lerp(low, high, band / (float)bands);
                    float phi1 = Mathf.Lerp(low, high, (band + 1f) / bands);
                    for (int side = 0; side < sides; side++)
                    {
                        float a = side * 2f * Mathf.PI / sides, b = (side + 1) * 2f * Mathf.PI / sides;
                        var u0 = EllipsoidPoint(center, radii, orientation, a, phi0);
                        var v0 = EllipsoidPoint(center, radii, orientation, b, phi0);
                        var u1 = EllipsoidPoint(center, radii, orientation, a, phi1);
                        var v1 = EllipsoidPoint(center, radii, orientation, b, phi1);
                        if (band < bands - 1) AddTriangle(u0, u1, v1, indices);
                        if (dome || band > 0) AddTriangle(u0, v1, v0, indices);
                    }
                }
            }

            private static (Vector3, Vector3) EllipsoidPoint(Vector3 center, Vector3 radii, Quaternion rotation,
                float azimuth, float elevation)
            {
                float horizontal = Mathf.Cos(elevation);
                var unit = new Vector3(horizontal * Mathf.Cos(azimuth), Mathf.Sin(elevation), horizontal * Mathf.Sin(azimuth));
                var normal = new Vector3(unit.x / radii.x, unit.y / radii.y, unit.z / radii.z).normalized;
                return (center + rotation * Vector3.Scale(unit, radii), rotation * normal);
            }

            internal void Box(Vector3 center, Vector3 size, Material material, Quaternion? rotation = null)
            {
                Quaternion orientation = rotation ?? Quaternion.identity;
                Vector3 x = orientation * (Vector3.right * size.x * 0.5f);
                Vector3 y = orientation * (Vector3.up * size.y * 0.5f);
                Vector3 z = orientation * (Vector3.forward * size.z * 0.5f);
                Vector3 a = center - x - y - z, b = center + x - y - z;
                Vector3 c = center + x + y - z, d = center - x + y - z;
                Vector3 e = center - x - y + z, f = center + x - y + z;
                Vector3 g = center + x + y + z, h = center - x + y + z;
                Quad(a, d, c, b, material);
                Quad(e, f, g, h, material);
                Quad(a, e, h, d, material);
                Quad(b, c, g, f, material);
                Quad(d, h, g, c, material);
                Quad(a, b, f, e, material);
            }

            internal void Frustum(Vector3 from, Vector3 to, float lowerRadius, float upperRadius, int sides, Material material)
            {
                Vector3 axis = (to - from).normalized;
                Vector3 reference = Mathf.Abs(axis.z) > 0.95f ? Vector3.up : Vector3.forward;
                Vector3 x = Vector3.Cross(axis, reference).normalized;
                Vector3 y = Vector3.Cross(x, axis);
                for (int i = 0; i < sides; i++)
                {
                    float a = i * 2f * Mathf.PI / sides, b = (i + 1) * 2f * Mathf.PI / sides;
                    Vector3 u = x * Mathf.Cos(a) + y * Mathf.Sin(a);
                    Vector3 v = x * Mathf.Cos(b) + y * Mathf.Sin(b);
                    Vector3 a0 = from + u * lowerRadius, b0 = from + v * lowerRadius;
                    Vector3 a1 = to + u * upperRadius, b1 = to + v * upperRadius;
                    if (upperRadius > 0f)
                    {
                        Quad(a0, a1, b1, b0, material);
                        Triangle(to, b1, a1, material);
                    }
                    else
                    {
                        Triangle(a0, to, b0, material);
                    }
                    Triangle(from, a0, b0, material);
                }
            }

            internal void Ring(Vector3 center, float inner, float outer, int sides, Material material)
            {
                for (int i = 0; i < sides; i++)
                {
                    float a = i * 2f * Mathf.PI / sides, b = (i + 1) * 2f * Mathf.PI / sides;
                    var u = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                    var v = new Vector3(Mathf.Cos(b), Mathf.Sin(b), 0f);
                    Quad(center + u * inner, center + v * inner, center + v * outer, center + u * outer, material);
                }
            }

            private List<int> GetIndices(Material material)
            {
                if (!_slots.TryGetValue(material, out int index))
                {
                    index = Materials.Count;
                    _slots.Add(material, index);
                    Materials.Add(material);
                    _triangles.Add(new List<int>());
                }
                return _triangles[index];
            }

            internal Mesh Build(string name)
            {
                var mesh = new Mesh { name = name };
                if (_vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(_vertices);
                mesh.SetNormals(_normals);
                mesh.subMeshCount = _triangles.Count;
                for (int i = 0; i < _triangles.Count; i++) mesh.SetTriangles(_triangles[i], i, false);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
