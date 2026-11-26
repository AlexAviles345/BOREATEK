using UnityEngine;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;

namespace Boreatek.World
{
    public partial class BoreatekWorldBuilder
    {
        private Material _worldSignInk;
        private void BuildForgeHall(Transform campus, FoundryMaterials m)
        {
            Transform hall = CharacterPart(campus, "Nave_Central_De_La_Forja", Vector3.zero);
            var shell = new Batch();
            var trim = new Batch();
            const float front = 0.55f, back = 13.2f, halfWidth = 5.9f, eave = 5.95f;
            shell.Box(new Vector3(0f, FoundryFloor - 0.16f, (front + back) * 0.5f),
                new Vector3(halfWidth * 2f, 0.32f, back - front), m.Stone);
            Wall(shell, true, front, -halfWidth, -2.09f, FoundryFloor, eave, m.Stone);
            Wall(shell, true, front, 2.09f, halfWidth, FoundryFloor, eave, m.Stone);
            Wall(shell, true, front, -2.09f, 2.09f, 3.53f, eave, m.Stone);
            AddFoundryFacing(trim, front - 0.17f, -halfWidth, -2.13f, eave, m);
            AddFoundryFacing(trim, front - 0.17f, 2.13f, halfWidth, eave, m);
            AddFoundryFloorPlates(trim, halfWidth * 2f, back - front, (front + back) * 0.5f, m);
            Wall(shell, true, back, -3.52f, 3.52f, FoundryFloor, eave, m.Stone);
            Wall(shell, true, back, -halfWidth, -3.52f, 2.55f, eave, m.Stone);
            Wall(shell, true, back, 3.52f, halfWidth, 2.55f, eave, m.Stone);

            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * halfWidth;
                Wall(shell, false, x, front, 2.05f, FoundryFloor, eave, m.Stone);
                Wall(shell, false, x, 2.05f, 5.47f, 2.75f, eave, m.Stone);
                Wall(shell, false, x, 5.47f, 8.21f, FoundryFloor, eave, m.Stone);
                Wall(shell, false, x, 8.21f, 10.87f, 2.75f, eave, m.Stone);
                Wall(shell, false, x, 10.87f, back, FoundryFloor, eave, m.Stone);
                for (int i = 0; i < 4; i++)
                {
                    float z = 1.6f + i * 3.1f;
                    AddFoundryWindow(trim, new Vector3(x, 4.35f, z), 1.15f, 1.4f, Quaternion.Euler(0f, 90f, 0f), m);
                }
                for (int i = 0; i < 3; i++)
                {
                    float z = 2.05f + 3.65f * i;
                    Vector3 mount = new Vector3(side * 5.42f, 3.1f, z);
                    AddFoundryLantern(trim, mount, 1.0f, m);
                    Beam(trim, mount + Vector3.up * 0.26f, mount + Vector3.up * 0.42f + Vector3.right * (side * 0.4f), 0.085f, m.Iron);
                }
                for (int i = 0; i < 17; i++)
                {
                    float z = 0.9f + i * 0.59f;
                    trim.Box(new Vector3(side * 1.35f, FoundryFloor + 0.012f, z), new Vector3(0.24f, 0.023f, 0.55f), m.Iron);
                    trim.Box(new Vector3(side * 1.35f, FoundryFloor + 0.027f, z), new Vector3(0.112f, 0.018f, 0.21f),
                        i % 3 == 0 ? m.Hearth : m.Copper);
                }
                float beamZ = front - 0.19f;
                Vector3 basePoint = new Vector3(side * 2.04f, FoundryFloor, beamZ);
                Beam(trim, basePoint, basePoint + Vector3.up * 3.44f, 0.32f, m.Timber);
                Beam(trim, basePoint + Vector3.up * 2.43f, new Vector3(side * 1.15f, 3.48f, beamZ), 0.26f, m.Timber);
                AddFoundryLantern(trim, new Vector3(side * 2.43f, 2.61f, beamZ - 0.15f), 1.08f, m);
                AddFoundryWindow(trim, new Vector3(side * 4.08f, 2.31f, front - 0.03f), 1.29f, 1.34f, Quaternion.identity, m, warm: true);
                Vector3 pipe = new Vector3(side * 3.8f, 5.2f, 1.04f);
                trim.Frustum(pipe, pipe + Vector3.forward * 10.4f, 0.22f, 0.22f, 10, m.Copper);
                for (int i = 0; i < 6; i++)
                {
                    Vector3 collar = pipe + Vector3.forward * (i * 1.87f);
                    trim.Frustum(collar, collar + Vector3.forward * 0.15f, 0.28f, 0.28f, 10, m.Iron);
                }
            }
            Beam(trim, new Vector3(-2.14f, 3.47f, front - 0.19f), new Vector3(2.14f, 3.47f, front - 0.19f), 0.28f, m.Timber);
            AddGable(shell, trim, halfWidth, front, back, eave, 2.5f, m);
            AddFoundrySign(hall, "FORJA CENTRAL", new Vector3(0f, 3.95f, front - 0.24f));
            AddFoundryLight(hall, new Vector3(-2.8f, 3.6f, 3.1f), 2.7f, 7f);
            AddFoundryLight(hall, new Vector3(3.15f, 3.8f, 7.45f), 2.8f, 7f);
            _landscape.Place(hall, "Foundation_Walls_And_Roof", shell, collision: true);
            _landscape.Place(hall, "Trusses_Pipes_Windows_And_Lanterns", trim);
        }

        private Transform BuildFoundryWing(Transform campus, string name, Vector3 position, float width, float depth,
            int side, FoundryMaterials m, string sign = null, bool lightInterior = true, bool industrial = false)
        {
            Transform room = CharacterPart(campus, name, position);
            var shell = new Batch();
            var trim = new Batch();
            float x = width * 0.5f, z = depth * 0.5f;
            float eave = industrial ? 5.8f : 3.47f;
            shell.Box(new Vector3(0f, FoundryFloor - 0.28f, 0f), new Vector3(width, 0.56f, depth), m.Stone);
            shell.Quad(new Vector3(-1.08f, 0.045f, -z - 1.06f), new Vector3(-1.08f, FoundryFloor, -z + 0.01f),
                new Vector3(1.08f, FoundryFloor, -z + 0.01f), new Vector3(1.08f, 0.045f, -z - 1.06f), m.Stone);
            Wall(shell, true, -z, -x, -1.12f, FoundryFloor, eave, m.Stone);
            Wall(shell, true, -z, 1.12f, x, FoundryFloor, eave, m.Stone);
            Wall(shell, true, -z, -1.12f, 1.12f, 2.69f, eave, m.Stone);
            AddFoundryFacing(trim, -z - 0.17f, -x, -1.14f, eave, m);
            AddFoundryFacing(trim, -z - 0.17f, 1.14f, x, eave, m);
            AddFoundryFloorPlates(trim, width, depth, 0f, m);
            Wall(shell, true, z, -x, x, FoundryFloor, eave, m.Stone);
            Wall(shell, false, side * x, -z, z, FoundryFloor, eave, m.Stone);
            float inner = -side * x;
            if (sign != null)
            {
                Wall(shell, false, inner, -z, z, FoundryFloor, eave, m.Stone);
            }
            else
            {
                float passageStart = name == "Sala_De_Vapor" ? -1.72f : 2.05f - position.z;
                float passageEnd = name == "Sala_De_Vapor" ? 0.87f : 5.47f - position.z;
                Wall(shell, false, inner, -z, passageStart, FoundryFloor, eave, m.Stone);
                Wall(shell, false, inner, passageEnd, z, FoundryFloor, eave, m.Stone);
                Wall(shell, false, inner, passageStart, passageEnd, 2.53f, eave, m.Stone);
                foreach (float jamb in new[] { passageStart, passageEnd })
                    trim.Box(new Vector3(inner, (FoundryFloor + 2.53f) * 0.5f, jamb),
                        new Vector3(0.36f, 2.53f - FoundryFloor, 0.13f), m.Timber);
            }
            AddGable(shell, trim, x, -z, z, eave, industrial ? 2.7f : 1.62f, m);
            for (int i = 0; i < 3; i++)
            {
                float windowZ = Mathf.Lerp(-z * 0.66f, z * 0.66f, i * 0.5f);
                AddFoundryWindow(trim, new Vector3(side * x, 1.91f, windowZ), 0.91f, 1.08f,
                    Quaternion.Euler(0f, 90f, 0f), m, warm: true);
            }
            for (int dir = -1; dir <= 1; dir += 2)
            {
                Vector3 foot = new Vector3(dir * 1.16f, FoundryFloor, -z - 0.22f);
                Beam(trim, foot, foot + Vector3.up * 2.6f, 0.24f, m.Timber);
                AddFoundryLantern(trim, foot + new Vector3(dir * 0.16f, 1.94f, -0.13f), 0.78f, m);
            }
            AddFoundryLantern(trim, new Vector3(side * (x - 0.46f), 2.76f, 0.45f), 0.86f, m);
            if (lightInterior) AddFoundryLight(room, new Vector3(0f, 2.62f, 0.13f), 2.5f, 6.0f);
            string label = sign ?? (name == "Herreria" ? "HERRERÍA" : name == "Almacen" ? "ALMACÉN" : "SALA DE VAPOR");
            AddFoundrySign(room, label, new Vector3(0f, 2.99f, -z - 0.24f));
            _landscape.Place(room, "Shell_And_Walkable_Floor", shell, collision: true);
            _landscape.Place(room, "Timber_And_Fittings", trim);
            return room;
        }

        private static void Wall(Batch shell, bool acrossX, float fixedAxis, float from, float to, float bottom, float top,
            Material material)
        {
            var extents = new Vector3(to - from, top - bottom, 0.32f);
            var center = new Vector3((from + to) * 0.5f, (top + bottom) * 0.5f, fixedAxis);
            if (!acrossX)
            {
                (extents.x, extents.z) = (extents.z, extents.x);
                (center.x, center.z) = (center.z, center.x);
            }
            shell.Box(center, extents, material);
        }

        private static void AddFoundryFacing(Batch trim, float z, float from, float to, float top, FoundryMaterials m)
        {
            float width = to - from;
            int blocks = Mathf.CeilToInt(width / 0.69f);
            float module = width / blocks;
            for (int row = 0; row < 3; row++)
            {
                for (int block = 0; block < blocks; block++)
                {
                    float x = from + (block + 0.5f) * module;
                    trim.Box(new Vector3(x, 0.39f + row * 0.29f, z),
                        new Vector3(module - 0.038f, 0.249f, 0.10f + (block + row) % 2 * 0.024f),
                        (block + row * 2) % 4 == 0 ? m.Iron : m.Stone);
                }
            }
            int planks = Mathf.CeilToInt(width / 0.43f);
            float plank = width / planks;
            for (int i = 0; i < planks; i++)
            {
                float x = from + (i + 0.5f) * plank;
                trim.Box(new Vector3(x, (top + 1.11f) * 0.5f, z), new Vector3(plank - 0.018f, top - 1.11f, 0.061f), m.Timber);
            }
            trim.Box(new Vector3((from + to) * 0.5f, 1.094f, z - 0.045f), new Vector3(width, 0.15f, 0.14f), m.Iron);
            foreach (float x in new[] { from + 0.13f, to - 0.13f })
            {
                trim.Box(new Vector3(x, (top + 1.06f) * 0.5f, z - 0.046f), new Vector3(0.245f, top - 1.06f, 0.21f), m.Timber);
                trim.Box(new Vector3(x, top - 0.39f, z - 0.151f), new Vector3(0.31f, 0.36f, 0.09f), m.Iron);
            }
        }

        private static void AddFoundryFloorPlates(Batch trim, float width, float depth, float centerZ, FoundryMaterials m)
        {
            int columns = Mathf.CeilToInt(width / 1.57f);
            int rows = Mathf.CeilToInt(depth / 1.35f);
            float cellX = width / columns, cellZ = depth / rows;
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    float x = (column + 0.5f) * cellX - width * 0.5f;
                    float z = centerZ + (row + 0.5f) * cellZ - depth * 0.5f;
                    trim.Box(new Vector3(x, FoundryFloor + 0.003f, z), new Vector3(cellX - 0.027f, 0.01f, cellZ - 0.023f),
                        (row + column * 3) % 5 == 0 ? m.Stone : m.Iron);
                }
            }
        }

        private static void AddGable(Batch shell, Batch trim, float halfWidth, float front, float back, float eave, float rise,
            FoundryMaterials m)
        {
            float slant = Mathf.Sqrt(halfWidth * halfWidth + rise * rise);
            float angle = Mathf.Atan2(rise, halfWidth) * Mathf.Rad2Deg;
            float depth = back - front;
            for (int side = -1; side <= 1; side += 2)
            {
                Quaternion tilt = Quaternion.Euler(0f, 0f, -side * angle);
                var center = new Vector3(side * halfWidth * 0.5f, eave + rise * 0.5f, (front + back) * 0.5f);
                shell.Box(center, new Vector3(slant + 0.55f, 0.22f, depth + 0.82f), m.Roof, tilt);
                for (int band = 0; band < 4; band++)
                {
                    Vector3 drift = center + tilt * new Vector3(side * (band % 2 - 0.5f) * 0.19f, 0.16f, -depth * 0.39f + band * depth * 0.26f);
                    trim.Box(drift, new Vector3(slant + 0.39f, 0.14f, depth * 0.255f), m.Snow, tilt);
                }
                Beam(trim, new Vector3(side * halfWidth, eave - 0.07f, front - 0.39f),
                    new Vector3(side * halfWidth, eave - 0.07f, back + 0.42f), 0.23f, m.Timber);
            }
            foreach (float end in new[] { front, back })
            {
                Vector3 left = new Vector3(-halfWidth, eave - 0.12f, end - 0.10f);
                Vector3 right = new Vector3(halfWidth, eave - 0.12f, end - 0.10f);
                Vector3 peak = new Vector3(0f, eave + rise, end - 0.10f);
                Vector3 thickness = Vector3.forward * 0.20f;
                shell.Triangle(left, peak, right, m.Timber);
                shell.Triangle(right + thickness, peak + thickness, left + thickness, m.Timber);
                shell.Quad(left, left + thickness, peak + thickness, peak, m.Timber);
                shell.Quad(peak, peak + thickness, right + thickness, right, m.Timber);
            }
            for (int i = 0; i < 4; i++)
            {
                float z = Mathf.Lerp(front - 0.19f, back + 0.10f, i / 3f);
                Vector3 peak = new Vector3(0f, eave + rise - 0.04f, z);
                Vector3 l = new Vector3(-halfWidth, eave - 0.15f, z), r = new Vector3(halfWidth, eave - 0.15f, z);
                Beam(trim, l, peak, 0.28f, m.Timber);
                Beam(trim, peak, r, 0.28f, m.Timber);
                Beam(trim, l, r, 0.24f, m.Timber);
                Beam(trim, new Vector3(0f, eave - 0.16f, z), peak, 0.20f, m.Timber);
                trim.Box(peak - Vector3.up * 0.59f, new Vector3(0.65f, 0.32f, 0.28f), m.Copper);
            }
        }

        private static void AddFoundryWindow(Batch trim, Vector3 center, float width, float height, Quaternion rotation,
            FoundryMaterials m, bool warm = false)
        {
            trim.Box(center, new Vector3(width + 0.16f, height + 0.15f, 0.53f), m.Timber, rotation);
            trim.Box(center, new Vector3(width, height, 0.62f), warm ? m.Hearth : m.Window, rotation);
            trim.Box(center, new Vector3(width + 0.04f, 0.074f, 0.665f), m.Iron, rotation);
            for (int side = -1; side <= 1; side += 2)
            {
                trim.Box(center + rotation * (Vector3.right * (width * 0.18f * side)),
                    new Vector3(0.07f, height + 0.03f, 0.66f), m.Iron, rotation);
            }
        }

        private void AddFoundrySign(Transform parent, string label, Vector3 position)
        {
            TextMesh text = CharacterPart(parent, "Sign_" + label, position).gameObject.AddComponent<TextMesh>();
            text.text = label;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 56;
            text.characterSize = 0.045f;
            text.color = new Color32(236, 177, 101, 255);
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Shader shader = Resources.Load<Shader>("Boreatek/WorldSign");
            if (font != null && shader != null)
            {
                text.font = font;
                font.RequestCharactersInTexture(label, text.fontSize, FontStyle.Normal);
                if (_worldSignInk == null) _worldSignInk = _landscape.Own(new Material(shader));
                _worldSignInk.mainTexture = font.material.mainTexture;
                text.GetComponent<MeshRenderer>().sharedMaterial = _worldSignInk;
            }
            float width = Mathf.Clamp(label.Length * 0.144f + 0.45f, 1.36f, 3.25f);
            Material board = _landscape.Finish(new Color32(35, 50, 64, 255));
            Material edge = _landscape.Finish(new Color32(115, 78, 54, 255));
            var backing = new Batch();
            backing.Box(new Vector3(0f, 0f, 0.115f), new Vector3(width, 0.42f, 0.18f), board);
            backing.Box(new Vector3(0f, -0.193f, 0.048f), new Vector3(width + 0.09f, 0.052f, 0.095f), edge);
            _landscape.Place(text.transform, "Placa_De_Hierro", backing);
        }

        private static void AddFoundryLight(Transform parent, Vector3 at, float intensity, float range)
        {
            Light light = CharacterPart(parent, "Amber_Lantern_Light", at).gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = range;
            light.intensity = intensity;
            light.color = new Color(1.0f, 0.66f, 0.37f);
            light.shadows = LightShadows.None;
        }
    }
}
