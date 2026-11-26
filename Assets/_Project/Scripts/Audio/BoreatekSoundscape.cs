using System.Collections.Generic;
using Boreatek.Core;
using Boreatek.Player;
using UnityEngine;

namespace Boreatek.Audio
{
    public sealed class BoreatekSoundscape : MonoBehaviour
    {
        private sealed class WorldVoice
        {
            internal readonly AudioSource Source;
            internal readonly AudioLowPassFilter Filter;
            internal float Gain, Obstruction, TargetObstruction;

            internal WorldVoice(AudioSource source)
            {
                Source = source;
                Filter = source.gameObject.AddComponent<AudioLowPassFilter>();
                Filter.enabled = false;
            }
        }

        private static BoreatekSoundscape _current;
        private readonly Dictionary<SoundBed, AudioClip> _beds = new Dictionary<SoundBed, AudioClip>();
        private readonly Dictionary<SoundCue, AudioClip> _effects = new Dictionary<SoundCue, AudioClip>();
        private readonly Dictionary<(SoundCue cue, int variant), AudioClip> _footfalls = new();
        private readonly AudioSource[] _mix = new AudioSource[2];
        private readonly WorldVoice[] _spatial = new WorldVoice[12];
        private readonly RaycastHit[] _obstructionHits = new RaycastHit[16];
        private AudioSource _ui, _storm;
        private TemperatureSystem _traveler;
        private Transform _listener;
        private int _selected, _nextVoice, _soundNumber, _footfallNumber;
        private float _obstructionTimer;
        private bool _title = true, _running;
        private SoundBed? _selectedBed;

        internal static BoreatekSoundscape Install(Transform world, TemperatureSystem traveler)
        {
            var root = new GameObject("Boreatek_Soundscape");
            root.transform.SetParent(world, false);
            var audio = root.AddComponent<BoreatekSoundscape>();
            audio._traveler = traveler;
            AudioListener listener = FindAnyObjectByType<AudioListener>();
            audio._listener = listener != null ? listener.transform : traveler.transform;
            audio._mix[0] = audio.CreateSource("Ambience A");
            audio._mix[1] = audio.CreateSource("Ambience B");
            audio._storm = audio.CreateSource("Blizzard");
            audio._ui = audio.CreateSource("Player and Interface");
            for (int i = 0; i < audio._spatial.Length; i++)
                audio._spatial[i] = new WorldVoice(audio.CreateSource($"World voice {i + 1}", spatial: true));
            _current = audio;
            return audio;
        }

        internal void SetScreen(bool title, bool running)
        {
            _title = title;
            _running = running;
            if (!_running)
            {
                foreach (WorldVoice voice in _spatial) voice.Source.Stop();
            }
        }

        internal static void Play(SoundCue cue, Vector3? position = null, float strength = 1f)
        {
            if (_current == null) return;
            _current.PlayCue(cue, position, strength);
        }

        private void PlayCue(SoundCue cue, Vector3? position, float strength)
        {
            if (position.HasValue && (!_running || (_traveler.transform.position - position.Value).sqrMagnitude > 32f * 32f))
                return;
            AudioClip clip = GetCue(cue);
            if (!position.HasValue)
            {
                _ui.PlayOneShot(clip, Mathf.Clamp01(strength) * 0.76f);
                return;
            }

            WorldVoice voice = FreeWorldVoice();
            AudioSource source = voice.Source;
            source.transform.position = position.Value;
            source.clip = clip;
            source.pitch = 0.94f + (_soundNumber++ % 7) * 0.02f;
            voice.Gain = Mathf.Clamp01(strength) * 0.81f;
            voice.Obstruction = voice.TargetObstruction = MeasureObstruction(ListenerPosition(), position.Value);
            ApplyObstruction(voice);
            source.Play();
        }

        private AudioClip GetCue(SoundCue cue)
        {
            if (BoreatekSoundBank.IsFootfall(cue))
            {
                int variant = _footfallNumber++ % BoreatekSoundBank.FootfallVariants;
                if (!_footfalls.TryGetValue((cue, variant), out AudioClip footfall))
                {
                    footfall = BoreatekSoundBank.MakeFootfall(cue, variant);
                    _footfalls.Add((cue, variant), footfall);
                }
                return footfall;
            }
            if (!_effects.TryGetValue(cue, out AudioClip clip))
            {
                clip = BoreatekSoundBank.MakeCue(cue);
                _effects.Add(cue, clip);
            }
            return clip;
        }

        private WorldVoice FreeWorldVoice()
        {
            for (int i = 0; i < _spatial.Length; i++)
            {
                var voice = _spatial[(_nextVoice + i) % _spatial.Length];
                if (!voice.Source.isPlaying) return voice;
            }
            return _spatial[_nextVoice++ % _spatial.Length];
        }

        private Vector3 ListenerPosition() => _listener != null
            ? _listener.position
            : _traveler.transform.position + Vector3.up;

        private float MeasureObstruction(Vector3 listener, Vector3 emitter)
        {
            const float margin = 0.08f;
            // Many effects use an actor's feet or a prop's base as their origin.
            Vector3 end = emitter + Vector3.up * 0.28f;
            Vector3 path = end - listener;
            float distance = path.magnitude;
            if (distance <= margin * 2f) return 0f;
            Vector3 direction = path / distance;
            Vector3 side = Vector3.Cross(Vector3.up, direction).normalized * 0.22f;
            int blocked = 0;
            for (int ray = -1; ray <= 1; ray++)
            {
                Vector3 origin = listener + direction * margin + side * ray;
                int hits = Physics.RaycastNonAlloc(origin, direction, _obstructionHits, distance - 2f * margin,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < hits; i++)
                {
                    Collider collider = _obstructionHits[i].collider;
                    if (collider is CharacterController || collider is CapsuleCollider) continue;
                    // A prop emitting the sound should not muffle its own effect.
                    if (collider.bounds.Contains(end) &&
                        (collider is BoxCollider or SphereCollider || collider is MeshCollider mesh && mesh.convex) &&
                        (collider.ClosestPoint(end) - end).sqrMagnitude < 0.0001f)
                        continue;
                    blocked++;
                    break;
                }
            }
            return blocked / 3f;
        }

        private static void ApplyObstruction(WorldVoice voice)
        {
            voice.Source.volume = voice.Gain * Mathf.Lerp(1f, 0.30f, voice.Obstruction);
            voice.Filter.enabled = voice.Obstruction > 0.01f;
            voice.Filter.cutoffFrequency = 22000f * Mathf.Pow(1500f / 22000f, voice.Obstruction);
        }

        private void UpdateWorldVoices(float dt)
        {
            if (!_running) return;
            _obstructionTimer -= dt;
            bool measure = _obstructionTimer <= 0f;
            if (measure) _obstructionTimer = 0.08f;
            Vector3 listener = measure ? ListenerPosition() : Vector3.zero;
            foreach (WorldVoice voice in _spatial)
            {
                if (!voice.Source.isPlaying) continue;
                if (measure)
                    voice.TargetObstruction = MeasureObstruction(listener, voice.Source.transform.position);
                if (voice.Obstruction == voice.TargetObstruction) continue;
                voice.Obstruction = Mathf.MoveTowards(voice.Obstruction, voice.TargetObstruction, dt * 9f);
                ApplyObstruction(voice);
            }
        }

        private void LateUpdate()
        {
            BiomeType biome = _traveler.CurrentBiome;
            SoundBed next = _title ? SoundBed.Title : biome switch
            {
                BiomeType.GlacialFlats or BiomeType.FrostValley => SoundBed.FrozenPlain,
                BiomeType.SnowyTaiga or BiomeType.FrostBirchForest => SoundBed.Forest,
                BiomeType.SnowDunes or BiomeType.GlacialMoraine or BiomeType.AlpineRidges => SoundBed.Ridge,
                _ => (SoundBed)biome
            };
            if (_selectedBed != next) SwitchBed(next);
            float dt = Time.unscaledDeltaTime;
            UpdateWorldVoices(dt);
            for (int i = 0; i < _mix.Length; i++)
            {
                AudioSource source = _mix[i];
                float target = i == _selected ? (_title ? 0.55f : _running ? 0.42f : 0.13f) : 0f;
                source.volume = Mathf.MoveTowards(source.volume, target, dt * 0.38f);
                if (i != _selected && source.volume <= 0f && source.isPlaying) source.Stop();
            }
            bool blizzard = !_title && biome != BiomeType.SantaIndustrialVillage && !BiomeRules.IsUnderground(biome) &&
                DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsBlindBlizzardActive;
            if (blizzard && !_storm.isPlaying) StartLoop(_storm, SoundBed.Storm);
            _storm.volume = Mathf.MoveTowards(_storm.volume, blizzard ? (_running ? 0.46f : 0.12f) : 0f, dt * 0.31f);
            if (_storm.volume <= 0f && !blizzard && _storm.isPlaying) _storm.Stop();
        }

        private void SwitchBed(SoundBed bed)
        {
            _selected = _mix[0].volume <= _mix[1].volume ? 0 : 1;
            StartLoop(_mix[_selected], bed);
            _selectedBed = bed;
        }

        private void StartLoop(AudioSource source, SoundBed bed)
        {
            if (!_beds.TryGetValue(bed, out AudioClip clip))
            {
                clip = BoreatekSoundBank.MakeBed(bed);
                _beds.Add(bed, clip);
            }
            source.clip = clip;
            source.loop = true;
            source.volume = 0f;
            source.Play();
        }

        private AudioSource CreateSource(string label, bool spatial = false)
        {
            var holder = new GameObject(label);
            holder.transform.SetParent(transform, false);
            var source = holder.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = spatial ? 1f : 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1.4f;
            source.maxDistance = 30f;
            source.dopplerLevel = 0f;
            source.volume = spatial || label == "Player and Interface" ? 1f : 0f;
            return source;
        }

        private void OnDestroy()
        {
            if (_current == this) _current = null;
            foreach (AudioClip clip in _beds.Values) Destroy(clip);
            foreach (AudioClip clip in _effects.Values) Destroy(clip);
            foreach (AudioClip clip in _footfalls.Values) Destroy(clip);
        }
    }
}
