using System;
using System.Collections.Generic;
using Boreatek.World;
using UnityEngine;

namespace Boreatek.Core
{
    internal sealed class BoreatekExplorationMap
    {
        internal const int CellSize = 10;
        internal const int LayerCount = 5;
        internal const int NameLimit = 28;
        private const int MarkerLimit = 128;
        internal static readonly string[] Layers = { "Superficie", "Túneles", "Cuevas de hielo", "Geoda azul", "Abismo" };
        internal static readonly (string name, Vector2 point, int layer)[] Landmarks =
        {
            ("Forja Central", new Vector2(0f, 5f), 0),
            ("Herrería", new Vector2(-9.2f, 3.2f), 0),
            ("Comedor", new Vector2(-19.6f, -4.4f), 0),
            ("Mensajería", new Vector2(19f, -4.6f), 0),
            ("Viviendas", new Vector2(-18.4f, 9.4f), 0),
            ("Fundición", new Vector2(-17.9f, 23f), 0),
            ("Nave de montaje", new Vector2(19.3f, 23f), 0),
            ("Muelle de carga", new Vector2(25.8f, 2.2f), 0),
            ("Barrio de los Abedules", new Vector2(-41f, 3f), 0),
            ("Barrio de los Talleres", new Vector2(43f, 3f), 0),
            ("Barrio del Puente", new Vector2(0f, -41f), 0),
            ("Acceso minero", new Vector2(0f, 15f), 0),
            ("Bosque de Caramelos", new Vector2(52f, 2f), 0),
            ("Cementerio de Trineos", new Vector2(2f, -55f), 0),
            ("Galería de ventilación", new Vector2(0f, 28f), 1),
            ("Cuevas de hielo", new Vector2(0f, 58f), 2),
            ("Geoda azul", new Vector2(0f, 114f), 3),
            ("Abismo del Núcleo", new Vector2(0f, 172f), 4)
        };

        [Serializable]
        internal struct Pin
        {
            public Vector2 point;
            public int layer;
            public string name;
        }

        [Serializable]
        internal struct SaveState
        {
            public Vector3Int[] cells;
            public Pin[] pins;
        }

        private readonly BoreatekSurfaceChunks _surface;
        private readonly Dictionary<Vector3Int, Color32> _cells = new();
        private readonly RectInt[] _extents = new RectInt[LayerCount];
        private readonly bool[] _discovered = new bool[LayerCount];
        private readonly List<Pin> _pins = new();
        private Vector3Int _lastReveal = new Vector3Int(0, 0, -1);
        internal int Version { get; private set; }
        internal IReadOnlyList<Pin> Pins => _pins;

        internal BoreatekExplorationMap(BoreatekSurfaceChunks surface) => _surface = surface;

        internal int LayerAt(Vector3 position) => _surface.IsInProceduralCave(position) ? 2
            : position.y < -28f ? 4 : position.y < -16f ? 3 : position.y < -6f ? 2 : position.y < -1.5f ? 1 : 0;
        internal static Vector2 Planar(Vector3 point) => new Vector2(point.x, point.z);
        private static Vector3Int CellAt(Vector2 point, int layer) =>
            new Vector3Int(Mathf.FloorToInt(point.x / CellSize), Mathf.FloorToInt(point.y / CellSize), layer);

        internal void Reveal(Vector3 traveler)
        {
            Vector3Int center = CellAt(Planar(traveler), LayerAt(traveler));
            if (center == _lastReveal) return;
            _lastReveal = center;
            if (center.z != 0)
            {
                AddCell(center);
                return;
            }
            for (int z = -2; z <= 2; z++)
            {
                for (int x = -2; x <= 2; x++)
                {
                    if (x * x + z * z <= 5) AddCell(center + new Vector3Int(x, z, 0));
                }
            }
        }

        private void AddCell(Vector3Int cell)
        {
            if (_cells.ContainsKey(cell)) return;
            int layer = cell.z;
            float x = (cell.x + 0.5f) * CellSize, z = (cell.y + 0.5f) * CellSize;
            Color32 shade;
            if (layer == 0)
            {
                Color tint = BoreatekWorldBuilder.SnowTint(_surface.SnowWeights(x, z), x, z);
                float relief = Mathf.Clamp01((_surface.GroundHeight(x, z) + 1f) * 0.034f);
                shade = Color.Lerp(tint * 0.72f, tint, 0.24f + relief * 0.67f);
            }
            else
            {
                shade = layer switch
                {
                    1 => new Color32(94, 125, 140, 255),
                    2 => new Color32(81, 156, 181, 255),
                    3 => new Color32(68, 105, 159, 255),
                    _ => new Color32(138, 99, 154, 255)
                };
            }
            shade.a = 255;
            _cells.Add(cell, shade);
            RectInt previous = _extents[layer];
            _extents[layer] = _discovered[layer]
                ? new RectInt(Mathf.Min(previous.xMin, cell.x), Mathf.Min(previous.yMin, cell.y), 1, 1)
                : new RectInt(cell.x, cell.y, 1, 1);
            if (_discovered[layer])
            {
                _extents[layer].xMax = Mathf.Max(previous.xMax, cell.x + 1);
                _extents[layer].yMax = Mathf.Max(previous.yMax, cell.y + 1);
            }
            _discovered[layer] = true;
            Version++;
        }

        internal bool HasLayer(int layer) => _discovered[layer];
        internal bool HasCell(Vector2 point, int layer) => _cells.ContainsKey(CellAt(point, layer));
        internal bool TryReadCell(Vector2 point, int layer, out Color32 shade) =>
            _cells.TryGetValue(CellAt(point, layer), out shade);

        internal Rect BoundsOf(int layer)
        {
            RectInt bounds = _extents[layer];
            return new Rect(bounds.x * (float)CellSize, bounds.y * (float)CellSize, bounds.width * (float)CellSize,
                bounds.height * (float)CellSize);
        }

        internal bool PutPin(Vector2 point, int layer, string name, int editIndex = -1)
        {
            name = name?.Trim();
            if (string.IsNullOrEmpty(name) || name.Length > NameLimit || layer < 0 || layer >= LayerCount ||
                !float.IsFinite(point.x) || !float.IsFinite(point.y) || !HasCell(point, layer)) return false;
            var pin = new Pin { point = point, layer = layer, name = name };
            if (editIndex >= 0 && editIndex < _pins.Count) _pins[editIndex] = pin;
            else
            {
                if (_pins.Count >= MarkerLimit) return false;
                _pins.Add(pin);
            }
            Version++;
            return true;
        }

        internal bool RemovePin(int index)
        {
            if (index < 0 || index >= _pins.Count) return false;
            _pins.RemoveAt(index);
            Version++;
            return true;
        }

        internal SaveState Capture()
        {
            var cells = new Vector3Int[_cells.Count];
            _cells.Keys.CopyTo(cells, 0);
            return new SaveState { cells = cells, pins = _pins.ToArray() };
        }

        internal void Restore(SaveState state)
        {
            _cells.Clear();
            _pins.Clear();
            _lastReveal = new Vector3Int(0, 0, -1);
            Array.Clear(_extents, 0, LayerCount);
            Array.Clear(_discovered, 0, LayerCount);
            Version++;
            if (state.cells == null) return;
            foreach (Vector3Int cell in state.cells)
            {
                if (cell.z >= 0 && cell.z < LayerCount) AddCell(cell);
            }
            if (state.pins == null) return;
            foreach (Pin pin in state.pins)
                PutPin(pin.point, pin.layer, pin.name);
        }
    }
}
