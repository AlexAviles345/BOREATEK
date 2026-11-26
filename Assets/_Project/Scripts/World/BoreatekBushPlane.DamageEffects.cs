using UnityEngine;
using UnityEngine.Rendering;

namespace Boreatek.World
{
    internal sealed partial class BoreatekBushPlane
    {
        private Material _damageMaterial;
        private MeshRenderer[] _airframeMeshes;
        private ParticleSystem _smoke, _fire, _sparks, _blast;
        private Light _fireLight;
        private float _blastFlash;

        private void InitializeDamageEffects()
        {
            _airframeMeshes = GetComponentsInChildren<MeshRenderer>();
            Shader shader = Resources.Load<Shader>("Boreatek/WindborneSnow");
            if (shader == null) return;
            _damageMaterial = new Material(shader) { name = "M_Avioneta_Impactos" };
            _smoke = CreateDamageEmitter("Humo_Del_Motor", new Vector3(0f, 1.25f, 1.89f), 104,
                new ParticleSystem.MinMaxCurve(1.6f, 3.8f), new ParticleSystem.MinMaxCurve(0.29f, 0.59f),
                new ParticleSystem.MinMaxCurve(0.62f, 1.45f),
                new Color(0.15f, 0.16f, 0.17f, 0.90f), new Color(0.37f, 0.38f, 0.40f, 0.72f), 3.1f, -0.08f);
            _fire = CreateDamageEmitter("Llamas_Del_Motor", new Vector3(0f, 1.17f, 1.97f), 86,
                new ParticleSystem.MinMaxCurve(0.37f, 0.88f), new ParticleSystem.MinMaxCurve(0.22f, 0.52f),
                new ParticleSystem.MinMaxCurve(1.15f, 2.65f),
                new Color(1f, 0.83f, 0.34f, 0.97f), new Color(0.85f, 0.16f, 0.075f, 0.75f), 2.1f, -0.19f);
            _sparks = CreateDamageEmitter("Chispas_De_Impacto", Vector3.zero, 52,
                new ParticleSystem.MinMaxCurve(0.16f, 0.53f), new ParticleSystem.MinMaxCurve(0.045f, 0.115f),
                new ParticleSystem.MinMaxCurve(2.5f, 6.9f),
                new Color(1f, 0.84f, 0.31f, 0.98f), new Color(1f, 0.24f, 0.08f, 0.87f), 0.42f, 0.64f, burst: true);
            _blast = CreateDamageEmitter("Explosion_De_La_Avioneta", Vector3.zero, 62,
                new ParticleSystem.MinMaxCurve(0.52f, 1.65f), new ParticleSystem.MinMaxCurve(0.70f, 1.57f),
                new ParticleSystem.MinMaxCurve(2.1f, 5.3f),
                new Color(1f, 0.78f, 0.31f, 0.98f), new Color(0.48f, 0.13f, 0.074f, 0.86f), 2.6f, -0.09f, burst: true);
            var light = new GameObject("Luz_Del_Motor_En_Llamas");
            light.transform.SetParent(transform, false);
            light.transform.localPosition = new Vector3(0f, 1.48f, 1.9f);
            _fireLight = light.AddComponent<Light>();
            _fireLight.type = LightType.Point;
            _fireLight.color = new Color(1f, 0.43f, 0.13f);
            _fireLight.range = 6f;
            _fireLight.shadows = LightShadows.None;
            _fireLight.enabled = false;
        }

        private ParticleSystem CreateDamageEmitter(string label, Vector3 offset, int capacity, ParticleSystem.MinMaxCurve lifetime,
            ParticleSystem.MinMaxCurve size, ParticleSystem.MinMaxCurve speed, Color start, Color after,
            float expansion, float gravity, bool burst = false)
        {
            var emitter = new GameObject(label);
            emitter.transform.SetParent(transform, false);
            emitter.transform.localPosition = offset;
            emitter.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var particles = emitter.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = !burst;
            main.useUnscaledTime = burst;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = capacity;
            main.startLifetime = lifetime;
            main.startSize = size;
            main.startSpeed = speed;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = gravity;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            var shape = particles.shape;
            shape.shapeType = burst ? ParticleSystemShapeType.Sphere : ParticleSystemShapeType.Cone;
            shape.radius = burst ? 0.17f : 0.23f;
            if (!burst) shape.angle = 26f;
            var fade = particles.colorOverLifetime;
            fade.enabled = true;
            var tint = new Gradient();
            tint.SetKeys(new[] { new GradientColorKey(start, 0f), new GradientColorKey(after, 0.68f) },
                new[] { new GradientAlphaKey(start.a, 0f), new GradientAlphaKey(after.a, 0.42f), new GradientAlphaKey(0f, 1f) });
            fade.color = tint;
            var grow = particles.sizeOverLifetime;
            grow.enabled = true;
            grow.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.75f, 1f, expansion));
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _damageMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return particles;
        }

        private void SetWreckVisuals()
        {
            if (_airframeMeshes == null) return;
            MaterialPropertyBlock charred = null;
            if (_wrecked)
            {
                charred = new MaterialPropertyBlock();
                charred.SetColor("_BaseColor", new Color(0.105f, 0.119f, 0.129f));
                charred.SetColor("_EmissionColor", Color.black);
            }
            foreach (MeshRenderer renderer in _airframeMeshes)
            {
                renderer.SetPropertyBlock(charred);
                if (renderer.transform.IsChildOf(_propeller)) renderer.enabled = !_wrecked;
            }
            SetCockpitView(false);
        }

        private void UpdateDamageEffects()
        {
            if (_smoke == null) return;
            float smoke = _wrecked ? 11f : Burning ? 31f : Mathf.Lerp(0f, 19f, Mathf.InverseLerp(SmokeThreshold, 26f, _hull));
            SetEmission(_smoke, smoke);
            SetEmission(_fire, Burning ? 48f : 0f);
            _blastFlash = Mathf.Max(0f, _blastFlash - Time.unscaledDeltaTime);
            _fireLight.enabled = Burning || _blastFlash > 0f;
            _fireLight.range = _blastFlash > 0f ? 12f : 6f;
            _fireLight.intensity = _blastFlash > 0f ? 15f * _blastFlash : 3.4f + Mathf.Sin(Time.time * 19f) * 0.86f;
        }

        private static void SetEmission(ParticleSystem particles, float rate)
        {
            var emission = particles.emission;
            emission.rateOverTime = rate;
            if (rate > 0f && !particles.isEmitting) particles.Play();
            else if (rate <= 0f && particles.isEmitting) particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        private void EmitImpactSparks(Vector3 point, int count)
        {
            if (_sparks == null) return;
            var burst = new ParticleSystem.EmitParams { position = point + Vector3.up * 0.07f, applyShapeToPosition = true };
            if (!_sparks.isPlaying) _sparks.Play();
            _sparks.Emit(burst, count);
        }

        private void EmitExplosion(Vector3 point)
        {
            if (_blast == null) return;
            var burst = new ParticleSystem.EmitParams { position = point, applyShapeToPosition = true };
            if (!_blast.isPlaying) _blast.Play();
            _blast.Emit(burst, 51);
            EmitImpactSparks(point, 25);
            _blastFlash = 0.48f;
        }

        private void ClearCollisionEffects()
        {
            if (_smoke == null) return;
            _smoke.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            _fire.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            _sparks.Clear();
            _blast.Clear();
            _blastFlash = 0f;
            _fireLight.enabled = false;
        }
    }
}
