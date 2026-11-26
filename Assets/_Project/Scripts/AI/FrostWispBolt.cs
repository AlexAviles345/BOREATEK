using Boreatek.Audio;
using Boreatek.Player;
using UnityEngine;

namespace Boreatek.AI
{
    internal sealed class FrostWispBolt : MonoBehaviour
    {
        private const float Speed = 13f;
        private readonly RaycastHit[] _hits = new RaycastHit[12];
        private HostileWildlife _caster;
        private PlayerController _target;
        private Vector3 _direction;
        private float _damage, _life = 2.0f;

        internal static void Launch(HostileWildlife caster, Transform crystal, PlayerController target, Vector3 aim, float damage)
        {
            Vector3 path = aim - crystal.position;
            if (path.sqrMagnitude < 0.01f) return;
            var shot = new GameObject("Descarga_De_Escarcha");
            shot.transform.SetParent(caster.transform.parent, false);
            shot.transform.SetPositionAndRotation(crystal.position, Quaternion.LookRotation(path));
            shot.AddComponent<MeshFilter>().sharedMesh = crystal.GetComponent<MeshFilter>().sharedMesh;
            var renderer = shot.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = crystal.GetComponent<MeshRenderer>().sharedMaterials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var bolt = shot.AddComponent<FrostWispBolt>();
            bolt._caster = caster;
            bolt._target = target;
            bolt._direction = path.normalized;
            bolt._damage = damage;
        }

        private void Update()
        {
            if (Time.deltaTime > 0f) Advance(Time.deltaTime);
        }

        private void Advance(float dt)
        {
            float distance = Speed * Mathf.Min(dt, _life);
            int count = Physics.SphereCastNonAlloc(transform.position, 0.20f, _direction,
                _hits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            RaycastHit closest = default;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hits[i];
                if (_caster != null && hit.collider.transform.IsChildOf(_caster.transform)) continue;
                if (closest.collider == null || hit.distance < closest.distance) closest = hit;
            }
            if (closest.collider != null)
            {
                BoreatekSoundscape.Play(SoundCue.Ice, closest.point, 0.59f);
                if (_target != null && closest.collider.transform.IsChildOf(_target.transform) &&
                    !HostileWildlife.InSanctuary(_target.transform.position) && _target.TryGetComponent(out TemperatureSystem vitals))
                {
                    vitals.ApplyCombatDamage(_damage, transform.position);
                    _target.PublishStatus($"La descarga de escarcha te alcanzó. −{_damage:0} HP.");
                }
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }
            transform.position += _direction * distance;
            transform.Rotate(0f, 0f, 360f * dt);
            _life -= dt;
            if (_life <= 0f) Destroy(gameObject);
        }
    }
}
