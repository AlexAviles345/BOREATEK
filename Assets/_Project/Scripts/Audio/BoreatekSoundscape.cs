using System.Collections.Generic;
using Boreatek.Core;
using Boreatek.Player;
using UnityEngine;

namespace Boreatek.Audio
{
    public sealed class BoreatekSoundscape : MonoBehaviour
    {
        private static BoreatekSoundscape _current;
        private readonly Dictionary<SoundBed, AudioClip> _beds = new Dictionary<SoundBed, AudioClip>();
        private readonly Dictionary<SoundCue, AudioClip> _effects = new Dictionary<SoundCue, AudioClip>();
        private readonly AudioSource[] _mix = new AudioSource[2];
        private readonly AudioSource[] _spatial = new AudioSource[12];
        private AudioSource _ui, _storm;
        private TemperatureSystem _traveler;
        private int _selected, _nextVoice, _soundNumber;
        private bool _title = true, _running;
        private SoundBed? _selectedBed;

        internal static BoreatekSoundscape Install(Transform world, TemperatureSystem traveler)
        {
            var root = new GameObject("Boreatek_Soundscape");
            root.transform.SetParent(world, false);
            var audio = root.AddComponent<BoreatekSoundscape>();
            audio._traveler = traveler;
            audio._mix[0] = audio.CreateSource("Ambience A");
            audio._mix[1] = audio.CreateSource("Ambience B");
            audio._storm = audio.CreateSource("Blizzard");
            audio._ui = audio.CreateSource("Player and Interface");
            for (int i = 0; i < audio._spatial.Length; i++)
                audio._spatial[i] = audio.CreateSource($"World voice {i + 1}", spatial: true);
            _current = audio;
            return audio;
        }

        internal void SetScreen(bool title, bool running)
        {
            _title = title;
            _running = running;
            if (!_running)
            {
                foreach (AudioSource voice in _spatial) voice.Stop();
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
            if (!_effects.TryGetValue(cue, out AudioClip clip))
            {
                clip = BoreatekSoundBank.MakeCue(cue);
                _effects.Add(cue, clip);
            }
            if (!position.HasValue)
            {
                _ui.PlayOneShot(clip, Mathf.Clamp01(strength) * 0.76f);
                return;
            }

            AudioSource source = FreeWorldVoice();
            source.transform.position = position.Value;
            source.clip = clip;
            source.pitch = 0.94f + (_soundNumber++ % 7) * 0.02f;
            source.volume = Mathf.Clamp01(strength) * 0.81f;
            source.Play();
        }

        private AudioSource FreeWorldVoice()
        {
            for (int i = 0; i < _spatial.Length; i++)
            {
                var voice = _spatial[(_nextVoice + i) % _spatial.Length];
                if (!voice.isPlaying) return voice;
            }
            return _spatial[_nextVoice++ % _spatial.Length];
        }

        private void LateUpdate()
        {
            BiomeType biome = _traveler.CurrentBiome;
            SoundBed next = _title ? SoundBed.Title : (SoundBed)biome;
            if (_selectedBed != next) SwitchBed(next);
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < _mix.Length; i++)
            {
                AudioSource source = _mix[i];
                float target = i == _selected ? (_title ? 0.55f : _running ? 0.42f : 0.13f) : 0f;
                source.volume = Mathf.MoveTowards(source.volume, target, dt * 0.38f);
                if (i != _selected && source.volume <= 0f && source.isPlaying) source.Stop();
            }
            bool blizzard = !_title && biome > BiomeType.SantaIndustrialVillage && biome < BiomeType.VentilationTunnels &&
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
        }
    }
}
