using UnityEngine;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;

namespace Boreatek.World
{
    public partial class BoreatekWorldBuilder
    {
        private void AddFoundryStations(Transform campus, FoundryMaterials m)
        {
            Transform smithy = campus.Find("Herreria");
            Transform packing = campus.Find("Almacen");
            Transform steam = campus.Find("Sala_De_Vapor");

            var anvil = new Batch();
            anvil.Frustum(new Vector3(0f, FoundryFloor, 0f), new Vector3(0f, FoundryFloor + 0.44f, 0f), 0.31f, 0.27f, 8, m.Timber);
            anvil.Box(new Vector3(0f, FoundryFloor + 0.48f, 0f), new Vector3(0.43f, 0.12f, 0.34f), m.Iron);
            anvil.Box(new Vector3(0f, FoundryFloor + 0.61f, 0f), new Vector3(0.68f, 0.14f, 0.29f), m.Iron);
            anvil.Box(new Vector3(0.02f, FoundryFloor + 0.705f, 0.04f), new Vector3(0.23f, 0.06f, 0.15f), m.Hearth);
            _landscape.Place(smithy, "Anvil_And_Hot_Workpiece", anvil, collision: true);

            var smithProps = new Batch();
            Vector3 kiln = new Vector3(-2.10f, FoundryFloor + 1.07f, 2.02f);
            smithProps.Box(kiln + Vector3.forward * 0.22f, new Vector3(1.72f, 2.12f, 1.05f), m.Iron);
            AddKilnFace(smithProps, kiln - Vector3.forward * 0.32f, 0.67f, m);
            smithProps.Frustum(kiln + Vector3.up * 0.78f, kiln + Vector3.up * 2.04f, 0.30f, 0.27f, 10, m.Copper);
            AddWorkbench(smithProps, new Vector3(-2.08f, FoundryFloor, -1.44f), 1.73f, m);
            for (int i = 0; i < 5; i++)
            {
                float px = -2.68f + i * 0.22f;
                smithProps.Box(new Vector3(px, 1.19f, -1.47f), new Vector3(0.033f, 0.37f, 0.07f), m.Copper,
                    Quaternion.Euler(17f, 0f, 9f - i * 5f));
            }
            _landscape.Place(smithy, "Firebox_Tools_And_Repair_Bench", smithProps, collision: true);

            var storage = new Batch();
            AddWorkbench(storage, new Vector3(0.03f, FoundryFloor, -0.34f), 1.88f, m);
            AddCrate(storage, new Vector3(1.95f, FoundryFloor, 1.61f), 0.87f, m);
            AddCrate(storage, new Vector3(1.95f, FoundryFloor + 0.87f, 1.61f), 0.70f, m);
            AddCrate(storage, new Vector3(2.13f, FoundryFloor, -2.03f), 0.78f, m);
            for (int row = 0; row < 3; row++)
            {
                float y = FoundryFloor + 0.34f + row * 0.74f;
                storage.Box(new Vector3(0.42f, y, 3.10f), new Vector3(3.31f, 0.12f, 0.78f), m.Timber);
                for (int column = 0; column < 3; column++)
                    AddCrate(storage, new Vector3(-0.60f + column * 0.91f, y + 0.06f, 3.12f), 0.53f + column % 2 * 0.05f, m);
            }
            foreach (float x in new[] { -1.22f, 2.13f })
                storage.Box(new Vector3(x, FoundryFloor + 0.98f, 3.10f), new Vector3(0.15f, 1.96f, 0.68f), m.Iron);
            AddGear(storage, new Vector3(0.53f, 1.14f, -0.30f), 0.15f, m.Copper, m.Iron);
            _landscape.Place(packing, "Parts_Shelves_Crates_And_Dispatch_Table", storage, collision: true);

            var boiler = new Batch();
            Vector3 boilerBase = new Vector3(0.20f, FoundryFloor, 0.87f);
            boiler.Frustum(boilerBase, boilerBase + Vector3.up * 2.08f, 0.72f, 0.68f, 14, m.Iron);
            boiler.Frustum(boilerBase + Vector3.up * 2.06f, boilerBase + Vector3.up * 2.44f, 0.67f, 0.35f, 14, m.Copper);
            for (int ring = 0; ring < 3; ring++)
            {
                Vector3 collar = boilerBase + Vector3.up * (0.27f + ring * 0.66f);
                boiler.Frustum(collar, collar + Vector3.up * 0.106f, 0.745f, 0.745f, 14, m.Copper);
            }
            Vector3 dial = boilerBase + new Vector3(-0.29f, 1.39f, -0.69f);
            boiler.Frustum(dial, dial + Vector3.back * 0.082f, 0.20f, 0.19f, 14, m.Copper);
            boiler.Frustum(dial + Vector3.back * 0.081f, dial + Vector3.back * 0.092f, 0.16f, 0.15f, 14, m.Window);
            Beam(boiler, dial + Vector3.back * 0.104f, dial + new Vector3(0.11f, 0.055f, -0.104f), 0.023f, m.Iron);
            Vector3 elbow = boilerBase + new Vector3(1.29f, 1.33f, 0f);
            boiler.Frustum(boilerBase + Vector3.up * 1.33f, elbow, 0.125f, 0.125f, 8, m.Copper);
            boiler.Frustum(elbow, elbow + Vector3.up * 1.40f, 0.125f, 0.125f, 8, m.Copper);
            _landscape.Place(steam, "Pressure_Boiler_Pipe_And_Gauge", boiler, collision: true);

            Transform wheel = CharacterPart(steam, "Working_Pressure_Valve", boilerBase + new Vector3(0.20f, 0.96f, -0.83f));
            var wheelMesh = new Batch();
            AddGear(wheelMesh, Vector3.zero, 0.26f, m.Banner, m.Copper);
            _landscape.Place(wheel, "Valve_Wheel", wheelMesh);

            SpawnFoundryWorker(smithy, "Duende_Herrero", FoundryWorker.Job.Smith, new Color32(135, 69, 57, 255),
                new Vector3(0.20f, FoundryFloor, 0.64f), 180f, 0.59f, m);
            SpawnFoundryWorker(packing, "Duende_Almacenista", FoundryWorker.Job.Packer, new Color32(74, 115, 110, 255),
                new Vector3(-0.21f, FoundryFloor, 0.45f), 180f, 0.21f, m);
            SpawnFoundryWorker(steam, "Duende_Mecanico_De_Vapor", FoundryWorker.Job.Mechanic, new Color32(90, 100, 132, 255),
                new Vector3(0.31f, FoundryFloor, -0.59f), 0f, 0.34f, m, wheel);
        }

        private void SpawnFoundryWorker(Transform room, string name, FoundryWorker.Job job, Color32 uniform,
            Vector3 position, float yaw, float phase, FoundryMaterials m, Transform valve = null)
        {
            Transform worker = CharacterPart(room, name, position);
            worker.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var body = worker.gameObject.AddComponent<CapsuleCollider>();
            body.center = Vector3.up * (ElfHeight * 0.5f);
            body.height = ElfHeight;
            body.radius = 0.25f;
            ElfVisuals rig = BuildElfModel(worker, uniform);
            if (job == FoundryWorker.Job.Smith)
            {
                var hammer = new Batch();
                hammer.Frustum(Vector3.down * 0.13f, Vector3.up * 0.26f, 0.025f, 0.019f, 8, m.Timber);
                hammer.Box(Vector3.up * 0.26f, new Vector3(0.26f, 0.10f, 0.12f), m.Iron);
                hammer.Box(new Vector3(0.121f, 0.26f, 0f), new Vector3(0.039f, 0.11f, 0.13f), m.Copper);
                _landscape.Place(rig.RightGrip, "Smith_Hammer", hammer).transform.localRotation = Quaternion.Euler(32f, 0f, -19f);
            }
            else if (job == FoundryWorker.Job.Packer)
            {
                var part = new Batch();
                AddGear(part, Vector3.up * 0.05f, 0.093f, m.Copper, m.Iron);
                _landscape.Place(rig.RightGrip, "Part_Being_Sorted", part);
            }
            worker.gameObject.AddComponent<FoundryWorker>().Initialize(rig.Pose, job, valve, phase);
        }

        private static void AddWorkbench(Batch batch, Vector3 at, float width, FoundryMaterials m)
        {
            batch.Box(at + Vector3.up * 0.83f, new Vector3(width, 0.16f, 0.68f), m.Timber);
            batch.Box(at + new Vector3(0f, 0.72f, -0.30f), new Vector3(width + 0.08f, 0.14f, 0.10f), m.Iron);
            for (int side = -1; side <= 1; side += 2)
                batch.Box(at + new Vector3(side * (width * 0.5f - 0.13f), 0.38f, 0f), new Vector3(0.16f, 0.76f, 0.49f), m.Iron);
        }

        private static void AddCrate(Batch batch, Vector3 floor, float size, FoundryMaterials m)
        {
            Vector3 center = floor + Vector3.up * size * 0.5f;
            batch.Box(center, Vector3.one * size, m.Timber);
            for (int side = -1; side <= 1; side += 2)
            {
                batch.Box(center + Vector3.up * (side * size * 0.37f), new Vector3(size + 0.05f, size * 0.12f, size + 0.04f), m.Iron);
                Vector3 front = center + Vector3.back * (size * 0.52f);
                Beam(batch, front + new Vector3(side * size * 0.35f, -size * 0.31f, 0f),
                    front + new Vector3(-side * size * 0.35f, size * 0.31f, 0f), size * 0.075f, m.Copper);
            }
        }

        private void AddChimneySteam(Transform tower, float height)
        {
            if (!Application.isPlaying) return;
            Shader shader = Resources.Load<Shader>("Boreatek/WindborneSnow");
            if (shader == null) return;
            Transform vent = CharacterPart(tower, "Chimney_Steam", Vector3.up * (height + 0.04f));
            var smoke = vent.gameObject.AddComponent<ParticleSystem>();
            smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            smoke.useAutoRandomSeed = false;
            smoke.randomSeed = (uint)(20260000 + Mathf.RoundToInt(height * 10f));
            var main = smoke.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4.1f, 6f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.55f, 0.92f);
            main.startColor = new Color(0.67f, 0.73f, 0.81f, 0.30f);
            main.maxParticles = 40;
            var shape = smoke.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.51f;
            var emit = smoke.emission;
            emit.rateOverTime = 5f;
            var movement = smoke.velocityOverLifetime;
            movement.enabled = true;
            movement.x = 0.62f;
            movement.y = 1.36f;
            movement.z = -0.17f;
            var grow = smoke.sizeOverLifetime;
            grow.enabled = true;
            grow.size = new ParticleSystem.MinMaxCurve(2.9f, AnimationCurve.Linear(0f, 0.26f, 1f, 1f));
            var fade = smoke.colorOverLifetime;
            fade.enabled = true;
            var color = new Gradient();
            color.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.31f, 0f), new GradientAlphaKey(0.87f, 0.19f), new GradientAlphaKey(0f, 1f) });
            fade.color = color;
            var renderer = vent.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _landscape.Own(new Material(shader) { name = "M_Foundry_ChimneySteam" });
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            smoke.Play();
        }
    }
}
