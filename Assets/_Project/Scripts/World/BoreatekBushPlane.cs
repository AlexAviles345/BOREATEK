using System;
using Boreatek.Audio;
using Boreatek.Core;
using Boreatek.Economy;
using Boreatek.Player;
using UnityEngine;

namespace Boreatek.World
{
    internal sealed class BoreatekBushPlane : MonoBehaviour, IInteractable
    {
        internal const float TankCapacity = 24f;
        internal const int CanLiters = 8;
        internal const int CanPrice = 10;
        private const float RotationSpeed = 16.5f;
        private const float TakeoffClearance = 1.65f;
        private readonly RaycastHit[] _hits = new RaycastHit[32];
        private readonly Vector3[] _exitOffsets =
        {
            new(1.5f, 0f, -1.15f), new(-1.5f, 0f, -1.15f), new(2f, 0f, -1.4f), new(-2f, 0f, -1.4f),
            new(1.5f, 0f, 1.2f), new(-1.5f, 0f, 1.2f)
        };

        private Transform _seat, _propeller, _elevator, _rudder, _leftAileron, _rightAileron;
        private Renderer _glazing;
        private AudioSource _engine, _wind;
        private PlayerController _pilot;
        private Vector3 _parkingPosition;
        private float _parkingHeading;
        private float _fuel = 9f;
        private float _speed, _verticalSpeed, _throttle, _heading, _pitch, _bank, _altitude, _rpm;
        private float _takeoffAssist;
        private bool _airborne, _emergencyDescent;

        internal Transform Seat => _seat;
        internal bool CanExit => !_airborne && _speed < 0.85f;
        internal bool PilotAboard => _pilot != null;

        [Serializable]
        internal struct SaveState
        {
            public bool valid, airborne, piloting, emergencyDescent;
            public Vector3 position;
            public float fuel, heading, pitch, bank, speed, climb, throttle, takeoffAssist;
        }

        internal void Initialize(Transform seat, Renderer glazing, Transform propeller, Transform elevator,
            Transform rudder, Transform leftAileron, Transform rightAileron)
        {
            _seat = seat;
            _glazing = glazing;
            _propeller = propeller;
            _elevator = elevator;
            _rudder = rudder;
            _leftAileron = leftAileron;
            _rightAileron = rightAileron;
            _parkingPosition = transform.position;
            _parkingHeading = _heading = transform.eulerAngles.y;
            _engine = CreateLoop(propeller.gameObject, wind: false);
            _wind = CreateLoop(seat.gameObject, wind: true);
        }

        public string GetInteractionPrompt() => _pilot == null ? "[E] Pilotar la avioneta" :
            $"Gasolina {_fuel:0.0}/{TankCapacity:0} L  ·  {_speed * 3.6f:0} km/h  ·  {_altitude:0} m\n" +
            "W/S Motor · ↑/↓ Morro · A/D Girar · Espacio Freno · R Repostar";

        public bool CanInteract(GameObject interactor) => _pilot == null && !_airborne && interactor != null &&
            (interactor.transform.position - _seat.position).sqrMagnitude <= 4.6f * 4.6f;

        public void Interact(GameObject interactor)
        {
            if (CanInteract(interactor) && interactor.TryGetComponent(out PlayerController player))
                player.TryMountPlane(this);
        }

        internal bool Board(PlayerController player, bool resuming)
        {
            if (_pilot != null || (!resuming && !CanInteract(player.gameObject))) return false;
            _pilot = player;
            return true;
        }

        internal void Release()
        {
            _pilot = null;
            _speed = _throttle = _verticalSpeed = 0f;
            SetCockpitView(false);
        }

        internal void SetCockpitView(bool inside) => _glazing.forceRenderingOff = inside;

        internal void Refill(InventoryAndShop inventory)
        {
            if (!CanExit)
            {
                _pilot.PublishStatus("Detén la avioneta en tierra para repostar.");
                return;
            }
            if (_fuel >= TankCapacity - 0.05f)
            {
                _pilot.PublishStatus("El tanque ya está lleno.");
                return;
            }
            if (!inventory.TryUseAviationCanister())
            {
                _pilot.PublishStatus("Compra bidones de gasolina en el surtidor de la estación aérea.");
                return;
            }
            float added = Mathf.Min(CanLiters, TankCapacity - _fuel);
            _fuel += added;
            BoreatekSoundscape.Play(SoundCue.PlaneRefuel, transform.position, 0.72f);
            _pilot.PublishStatus($"Repostaste {added:0.0} L de gasolina polar.");
        }

        internal void Fly(float powerChange, float nose, float turn, bool brake, float dt)
        {
            if (dt <= 0f) return;
            int steps = Mathf.CeilToInt(dt / 0.025f);
            float step = dt / steps;
            for (int i = 0; i < steps; i++) AdvanceFlight(powerChange, nose, turn, brake, step);
        }

        private void AdvanceFlight(float powerChange, float nose, float turn, bool brake, float dt)
        {
            if (_emergencyDescent)
            {
                SettleAfterImpact(dt);
                return;
            }
            _throttle = brake && !_airborne ? 0f : Mathf.Clamp01(_throttle + powerChange * 0.70f * dt);
            if (_fuel > 0f && _throttle > 0.02f)
            {
                _fuel = Mathf.Max(0f, _fuel - (0.016f + 0.096f * _throttle) * dt);
                if (_fuel == 0f) _pilot?.PublishStatus("El tanque se vació. Baja el morro suavemente para planear.");
            }
            if (_fuel <= 0f) _throttle = 0f;

            _pitch = Mathf.MoveTowards(_pitch, nose * 22f, 26f * dt);
            _bank = Mathf.MoveTowards(_bank, _airborne ? turn * 38f : 0f, 48f * dt);
            float headRate = _airborne ? _bank * 0.54f : turn * Mathf.Lerp(11f, 36f, _speed / 12f);
            _heading = Mathf.Repeat(_heading + headRate * dt, 360f);
            float targetSpeed = _throttle * (_airborne ? 35f : 29f);
            if (_airborne) targetSpeed = Mathf.Max(targetSpeed, 8.7f) - _pitch * 0.38f;
            _speed = Mathf.MoveTowards(_speed, Mathf.Max(0f, targetSpeed), (brake && !_airborne ? 13f :
                _airborne ? 3.35f : targetSpeed > _speed ? 7.4f : 2.6f) * dt);
            if (!_airborne && _speed >= RotationSpeed && _pitch >= 6f && _throttle >= 0.45f)
            {
                _airborne = true;
                _takeoffAssist = 0.92f;
                _verticalSpeed = 2.25f;
            }
            if (_airborne)
            {
                float lift = Mathf.InverseLerp(11f, 22f, _speed) * Mathf.Cos(_bank * Mathf.Deg2Rad);
                float climb = Mathf.Sin(_pitch * Mathf.Deg2Rad) * _speed * lift - Mathf.Lerp(6.6f, 0.48f, lift);
                if (_takeoffAssist > 0f) climb = Mathf.Max(climb, 2.45f);
                _verticalSpeed = Mathf.MoveTowards(_verticalSpeed, climb, 7.1f * dt);
            }

            Quaternion heading = Quaternion.Euler(0f, _heading, 0f);
            Vector3 travel = heading * Vector3.forward * (_speed * dt);
            Vector3 next = transform.position + travel;
            if (_airborne) next.y = Mathf.Min(148f, next.y + _verticalSpeed * dt);
            bool hasGround = FindGround(next, out RaycastHit ground);
            bool touchingDown = _airborne && hasGround && next.y <= ground.point.y + 0.03f;
            Vector3 sweep = _airborne && !touchingDown ? next - transform.position : travel;
            if (BlocksTravel(sweep))
            {
                if (_airborne) BeginEmergencyDescent("Impacto. La avioneta se detuvo y está bajando.");
                else StopTaxi();
                return;
            }
            if (!_airborne)
            {
                if (!hasGround || ground.normal.y < 0.72f || ground.point.y > transform.position.y + 0.44f)
                {
                    StopTaxi();
                    return;
                }
                next.y = ground.point.y + 0.025f;
            }
            else if (touchingDown)
            {
                if (_verticalSpeed < -7.5f || ground.normal.y < 0.75f || Mathf.Abs(_bank) > 26f)
                {
                    BeginEmergencyDescent("Aterrizaje brusco. La avioneta se detuvo.");
                    return;
                }
                _airborne = false;
                _takeoffAssist = 0f;
                _throttle = Mathf.Min(_throttle, 0.24f);
                _verticalSpeed = 0f;
                next.y = ground.point.y + 0.025f;
                BoreatekSoundscape.Play(SoundCue.PlaneTouchdown, next, 0.82f);
                _pilot?.PublishStatus("En tierra. Frena con Espacio y pulsa E para bajar.");
            }

            _altitude = hasGround ? Mathf.Max(0f, next.y - ground.point.y) : Mathf.Max(0f, next.y);
            _takeoffAssist = Mathf.Max(0f, _takeoffAssist - dt);
            transform.SetPositionAndRotation(next, Attitude());
        }

        private Quaternion Attitude()
        {
            float clearance = Mathf.InverseLerp(0.05f, TakeoffClearance, _altitude);
            return Quaternion.Euler(_airborne ? -_pitch * clearance : 0f, _heading, _airborne ? -_bank * clearance : 0f);
        }

        private void BeginEmergencyDescent(string message)
        {
            _speed = _throttle = _takeoffAssist = 0f;
            _emergencyDescent = true;
            _pilot?.PublishStatus(message);
        }

        private void SettleAfterImpact(float dt)
        {
            Vector3 position = transform.position;
            bool hasGround = FindGround(position, out RaycastHit ground);
            _pitch = Mathf.MoveTowards(_pitch, 0f, 58f * dt);
            _bank = Mathf.MoveTowards(_bank, 0f, 58f * dt);
            float floor = hasGround ? ground.point.y + 0.025f : position.y - 10f;
            position.y = Mathf.MoveTowards(position.y, floor, 10f * dt);
            _altitude = hasGround ? Mathf.Max(0f, position.y - floor) : Mathf.Max(0f, position.y);
            if (hasGround && position.y <= floor + 0.005f)
            {
                _airborne = _emergencyDescent = false;
                _pitch = _bank = _verticalSpeed = 0f;
                BoreatekSoundscape.Play(SoundCue.PlaneTouchdown, position, 0.83f);
                _pilot?.PublishStatus("Avioneta detenida. Puedes bajar con E o volver a arrancar.");
            }
            transform.SetPositionAndRotation(position, Attitude());
        }

        private void StopTaxi()
        {
            if (_speed > 1.5f) _pilot?.PublishStatus("Paso obstruido. Gira hacia terreno despejado.");
            _speed = _throttle = 0f;
            transform.rotation = Quaternion.Euler(0f, _heading, 0f);
        }

        internal void ReturnToStation()
        {
            _airborne = false;
            _emergencyDescent = false;
            _speed = _pitch = _bank = _throttle = _verticalSpeed = _altitude = 0f;
            _takeoffAssist = 0f;
            _heading = _parkingHeading;
            transform.SetPositionAndRotation(_parkingPosition, Quaternion.Euler(0f, _heading, 0f));
        }

        private bool BlocksTravel(Vector3 travel)
        {
            float distance = travel.magnitude;
            if (distance < 0.001f) return false;
            Vector3 direction = travel / distance;
            return HullCast(new Vector3(0f, 1.20f, 0.23f), new Vector3(0.54f, 0.69f, 2.89f), direction, distance) ||
                HullCast(new Vector3(0f, 2.25f, 0.30f), new Vector3(3.95f, 0.16f, 0.70f), direction, distance);
        }

        private bool HullCast(Vector3 center, Vector3 size, Vector3 direction, float distance)
        {
            Quaternion orientation = _takeoffAssist > 0f && _altitude < TakeoffClearance
                ? Quaternion.Euler(0f, _heading, 0f) : transform.rotation;
            int count = Physics.BoxCastNonAlloc(transform.position + orientation * center, size, direction, _hits,
                orientation, distance + 0.065f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hits[i];
                if (hit.collider.transform.IsChildOf(transform) || (_pilot != null && hit.collider.transform.IsChildOf(_pilot.transform)))
                    continue;
                if (hit.normal.y > 0.72f && hit.point.y < transform.position.y + 0.36f) continue;
                return true;
            }
            return false;
        }

        private bool FindGround(Vector3 position, out RaycastHit ground)
        {
            Vector3 probe = position;
            probe.y = Mathf.Max(position.y, transform.position.y) + 1f;
            int count = Physics.RaycastNonAlloc(probe, Vector3.down, _hits, _airborne ? 260f : 3f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            ground = default;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hits[i];
                if (hit.collider.transform.IsChildOf(transform) || (_pilot != null && hit.collider.transform.IsChildOf(_pilot.transform)))
                    continue;
                if (ground.collider == null || hit.distance < ground.distance) ground = hit;
            }
            return ground.collider != null;
        }

        internal bool FindExit(out Vector3 landing)
        {
            foreach (Vector3 offset in _exitOffsets)
            {
                if (!FindGround(transform.TransformPoint(offset), out RaycastHit ground) || ground.normal.y < 0.72f) continue;
                landing = ground.point + Vector3.up * 0.045f;
                if (!Physics.CheckCapsule(landing + Vector3.up * 0.30f, landing + Vector3.up * 0.94f, 0.29f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return true;
            }
            landing = _parkingPosition + Vector3.back * 2.45f;
            return false;
        }

        internal SaveState CaptureSave() => new SaveState
        {
            valid = true, position = transform.position, fuel = _fuel, heading = _heading, pitch = _pitch, bank = _bank,
            speed = _speed, throttle = _throttle, climb = _verticalSpeed, airborne = _airborne, piloting = _pilot != null,
            emergencyDescent = _emergencyDescent, takeoffAssist = _takeoffAssist
        };

        internal void RestoreSave(SaveState save)
        {
            if (!save.valid || !float.IsFinite(save.position.x) || !float.IsFinite(save.position.y) ||
                !float.IsFinite(save.position.z) || !float.IsFinite(save.heading)) return;
            _heading = Mathf.Repeat(save.heading, 360f);
            _fuel = float.IsFinite(save.fuel) ? Mathf.Clamp(save.fuel, 0f, TankCapacity) : 9f;
            _airborne = save.airborne && save.piloting;
            _emergencyDescent = _airborne && save.emergencyDescent;
            _takeoffAssist = _airborne && !_emergencyDescent ? Bound(save.takeoffAssist, 0f, 0.92f) : 0f;
            _pitch = _airborne ? Bound(save.pitch, -22f, 22f) : 0f;
            _bank = _airborne ? Bound(save.bank, -38f, 38f) : 0f;
            _speed = save.piloting ? Bound(save.speed, 0f, 45f) : 0f;
            _throttle = save.piloting ? Bound(save.throttle, 0f, 1f) : 0f;
            _verticalSpeed = _airborne ? Bound(save.climb, -23f, 23f) : 0f;
            _altitude = _airborne ? Mathf.Max(0f, save.position.y -
                (FindGround(save.position, out RaycastHit ground) ? ground.point.y : 0f)) : 0f;
            transform.SetPositionAndRotation(save.position, Attitude());
        }

        private static float Bound(float value, float min, float max) => float.IsFinite(value) ? Mathf.Clamp(value, min, max) : 0f;

        private static AudioSource CreateLoop(GameObject sourceObject, bool wind)
        {
            var source = sourceObject.AddComponent<AudioSource>();
            source.clip = BoreatekSoundBank.MakePlaneLoop(wind);
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = wind ? 0.20f : 0.85f;
            source.minDistance = 3f;
            source.maxDistance = 45f;
            source.dopplerLevel = 0.15f;
            source.volume = 0f;
            return source;
        }

        private void LateUpdate()
        {
            if (_engine == null) return;
            float dt = Time.deltaTime;
            _rpm = Mathf.MoveTowards(_rpm, _throttle > 0.02f && _fuel > 0f ? 0.34f + _throttle * 0.66f : 0f, dt * 1.35f);
            float air = _airborne ? Mathf.InverseLerp(8f, 35f, _speed) : 0f;
            _propeller.Rotate(0f, 0f, Mathf.Max(_rpm * 3700f, air * 320f) * dt, Space.Self);
            _elevator.localRotation = Quaternion.Euler(_pitch * 0.68f, 0f, 0f);
            _rudder.localRotation = Quaternion.Euler(0f, _bank * 0.31f, 0f);
            _leftAileron.localRotation = Quaternion.Euler(_bank * 0.38f, 0f, 0f);
            _rightAileron.localRotation = Quaternion.Euler(-_bank * 0.38f, 0f, 0f);
            MixLoop(_engine, dt > 0f ? _rpm * 0.54f : 0f, 0.67f + _rpm * 0.67f);
            MixLoop(_wind, dt > 0f && _pilot != null ? air * 0.29f : 0f, 0.84f + air * 0.38f);
        }

        private static void MixLoop(AudioSource source, float volume, float pitch)
        {
            source.pitch = pitch;
            source.volume = Mathf.MoveTowards(source.volume, volume, Time.unscaledDeltaTime * 1.6f);
            if (!source.isPlaying && source.volume > 0.005f) source.Play();
            if (source.isPlaying && source.volume <= 0.001f) source.Stop();
        }

        private void OnDestroy()
        {
            if (_engine != null) Destroy(_engine.clip);
            if (_wind != null) Destroy(_wind.clip);
        }
    }
}
