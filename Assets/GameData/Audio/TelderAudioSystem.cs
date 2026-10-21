using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Audio
{
    [Serializable]
    public sealed class TelderAudioEvent
    {
        public string Id;
        public string[] Clips;
        public float Volume = 1f;
        public float PitchMin = 1f;
        public float PitchMax = 1f;
        public float Cooldown = 0.05f;
        public float MaxDistance = 24f;
    }

    [Serializable]
    public sealed class TelderAudioCatalog
    {
        public TelderAudioEvent[] Events;
        public string[] Music;
        public float MusicFadeSeconds = 1.4f;
    }

    /// <summary>
    /// Scene-independent sound system. All names/paths live in Resources/TelderAudio/audio_catalog.json.
    /// Does not own an AudioListener: the game's existing camera keeps its listener.
    /// </summary>
    [DefaultExecutionOrder(-32000)]
    public sealed class TelderAudioSystem : MonoBehaviour
    {
        private const string MusicVolumeKey = "TELDER_AUDIO_MUSIC_V1";
        private const string SfxVolumeKey = "TELDER_AUDIO_SFX_V1";
        private const int VoiceCount = 20;
        private static TelderAudioSystem instance;

        private readonly Dictionary<string, TelderAudioEvent> events =
            new Dictionary<string, TelderAudioEvent>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AudioClip> clips =
            new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, float> lastPlayed =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private readonly List<AudioSource> voices = new List<AudioSource>(VoiceCount);

        private AudioSource musicA;
        private AudioSource musicB;
        private AudioSource incoming;
        private AudioSource outgoing;
        private AudioSource activeMusic;
        private TelderAudioCatalog catalog;
        private int nextVoice;
        private int nextMusic;
        private float fadePosition;
        private bool crossfading;
        private float musicVolume;
        private float sfxVolume;

        public static float MusicVolume
        {
            get { return Instance.musicVolume; }
            set { Instance.SetMusicGain(Mathf.Clamp01(value)); }
        }
        public static float SfxVolume
        {
            get { return Instance.sfxVolume; }
            set { Instance.sfxVolume = Mathf.Clamp01(value); PlayerPrefs.SetFloat(SfxVolumeKey, Instance.sfxVolume); }
        }
        private void SetMusicGain(float value)
        {
            musicVolume = value;
            PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
            UpdateMusicGains(); // immediately audible while dragging the settings slider
        }
        private void UpdateMusicGains()
        {
            if (crossfading && incoming != null && outgoing != null)
            {
                float t = Mathf.Clamp01(fadePosition / Mathf.Max(0.05f,
                    catalog != null ? catalog.MusicFadeSeconds : 1.4f));
                incoming.volume = Mathf.Sin(t * Mathf.PI * .5f) * musicVolume;
                outgoing.volume = Mathf.Cos(t * Mathf.PI * .5f) * musicVolume;
            }
            else
            {
                if (musicA != null) musicA.volume = activeMusic == musicA ? musicVolume : 0f;
                if (musicB != null) musicB.volume = activeMusic == musicB ? musicVolume : 0f;
            }
        }
        private void OnApplicationPause(bool paused) { if (paused) PlayerPrefs.Save(); }
        private void OnApplicationQuit() { PlayerPrefs.Save(); }
        public static bool HasMusic { get { return Instance.activeMusic != null && Instance.activeMusic.clip != null; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap() { Ensure(); }

        public static TelderAudioSystem Instance { get { return Ensure(); } }

        private static TelderAudioSystem Ensure()
        {
            if (instance != null) return instance;
            var root = new GameObject("TelderAudio_Persistent");
            DontDestroyOnLoad(root);
            instance = root.AddComponent<TelderAudioSystem>();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 0.60f));
            sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 0.85f));
            var asset = UnityEngine.Resources.Load<TextAsset>("TelderAudio/audio_catalog");
            if (asset != null)
            {
                try { catalog = JsonUtility.FromJson<TelderAudioCatalog>(asset.text); }
                catch (Exception e) { Debug.LogWarning("TELDER AUDIO: invalid catalog: " + e.Message); }
            }
            if (catalog == null) catalog = new TelderAudioCatalog();
            if (catalog.Events != null)
                foreach (var item in catalog.Events)
                    if (item != null && !string.IsNullOrEmpty(item.Id)) events[item.Id] = item;

            musicA = CreateSource("MusicA", false);
            musicB = CreateSource("MusicB", false);
            activeMusic = musicA;
            for (int i = 0; i < VoiceCount; i++) voices.Add(CreateSource("Voice_" + i, false));
        }

        private AudioSource CreateSource(string label, bool loop)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f; // 2D/multi-dimensional; manual spatial fade for world sounds.
            source.ignoreListenerPause = false;
            return source;
        }

        private AudioClip LoadClip(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            AudioClip cached;
            if (clips.TryGetValue(path, out cached)) return cached;
            cached = UnityEngine.Resources.Load<AudioClip>(path);
            if (cached == null) Debug.LogWarning("TELDER AUDIO: missing Resources clip: " + path);
            clips[path] = cached;
            return cached;
        }

        private bool TryDefinition(string id, out TelderAudioEvent def)
        {
            if (events.TryGetValue(id, out def)) return true;
            // Allows e.g. mob.hurt.game:zombie with generic mob.hurt fallback.
            int split = id.LastIndexOf('.');
            if (split > 0 && events.TryGetValue(id.Substring(0, split), out def)) return true;
            def = null;
            return false;
        }

        public static void Play(string id) { Instance.PlayInternal(id, Vector3.zero, false, 0); }
        public static void PlayAt(string id, Vector3 position, int emitter = 0)
        {
            Instance.PlayInternal(id, position, true, emitter);
        }
        private void PlayInternal(string id, Vector3 position, bool spatial, int emitter)
        {
            if (string.IsNullOrEmpty(id) || sfxVolume <= 0f) return;
            TelderAudioEvent def;
            if (!TryDefinition(id, out def) || def.Clips == null || def.Clips.Length == 0) return;
            float now = Time.unscaledTime;
            string cooldownKey = id + ":" + emitter;
            float last;
            if (lastPlayed.TryGetValue(cooldownKey, out last) &&
                now - last < Mathf.Max(0f, def.Cooldown)) return;
            float attenuation = spatial ? Attenuation(position, def.MaxDistance) : 1f;
            if (attenuation <= 0f) return;
            var name = def.Clips[UnityEngine.Random.Range(0, def.Clips.Length)];
            var clip = LoadClip(name);
            if (clip == null) return;
            if (lastPlayed.Count > 4096) lastPlayed.Clear();
            lastPlayed[cooldownKey] = now;
            // A busy voice isn't stolen if an idle one exists.
            AudioSource voice = null;
            for (int i = 0; i < voices.Count; i++)
            {
                AudioSource candidate = voices[(nextVoice + i) % voices.Count];
                if (candidate.isPlaying) continue;
                voice = candidate;
                nextVoice = (nextVoice + i + 1) % voices.Count;
                break;
            }
            if (voice == null) return; // drop excess sounds instead of clipping/allocating.
            voice.clip = clip;
            float low = Mathf.Max(0.2f, def.PitchMin);
            float high = Mathf.Max(low, def.PitchMax);
            voice.pitch = UnityEngine.Random.Range(low, high);
            voice.volume = Mathf.Clamp01(def.Volume) * sfxVolume * attenuation;
            voice.panStereo = spatial ? Pan(position) : 0f;
            voice.loop = false;
            voice.Play();
        }

        private static float Pan(Vector3 at)
        {
            Camera cam = Camera.main;
            if (cam == null) return 0f;
            return Mathf.Clamp((at.x - cam.transform.position.x) / 15f, -0.65f, 0.65f);
        }
        public static float Attenuation(Vector3 at, float range)
        {
            Camera cam = Camera.main;
            if (cam == null) return 1f;
            float dist = Vector2.Distance(at, cam.transform.position);
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(dist / Mathf.Max(1f, range)));
        }

        /// <summary>Attach repeating sound to a world object, automatically removed with it.</summary>
        public static TelderAudioLoop AttachLoop(GameObject host, string eventId)
        {
            if (host == null) return null;
            var component = host.GetComponent<TelderAudioLoop>();
            if (component == null) component = host.AddComponent<TelderAudioLoop>();
            component.Setup(eventId);
            return component;
        }

        public static AudioClip GetLoopClip(string id)
        {
            TelderAudioEvent def;
            if (!Instance.TryDefinition(id, out def) || def.Clips == null || def.Clips.Length == 0) return null;
            return Instance.LoadClip(def.Clips[0]);
        }
        public static float LoopBaseVolume(string id)
        {
            TelderAudioEvent def;
            return Instance.TryDefinition(id, out def) ? Mathf.Clamp01(def.Volume) : 1f;
        }

        private void Update()
        {
            if (catalog.Music == null || catalog.Music.Length == 0) return;
            if (activeMusic != null && !crossfading)
            {
                activeMusic.volume = musicVolume;
                if (activeMusic.clip == null) StartMusic(activeMusic);
                else if (activeMusic.clip.length - activeMusic.time <= Mathf.Max(0.12f, catalog.MusicFadeSeconds))
                    BeginNextTrack();
            }
            if (!crossfading) return;
            fadePosition += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(fadePosition / Mathf.Max(0.05f, catalog.MusicFadeSeconds));
            // Equal-power crossfade to avoid a dip in the middle.
            incoming.volume = Mathf.Sin(t * Mathf.PI * 0.5f) * musicVolume;
            outgoing.volume = Mathf.Cos(t * Mathf.PI * 0.5f) * musicVolume;
            if (t >= 1f)
            {
                outgoing.Stop(); outgoing.clip = null;
                activeMusic = incoming;
                crossfading = false;
            }
        }

        private void StartMusic(AudioSource source)
        {
            for (int i = 0; i < catalog.Music.Length; i++)
            {
                var path = catalog.Music[nextMusic % catalog.Music.Length];
                nextMusic++;
                var clip = LoadClip(path);
                if (clip == null) continue;
                source.clip = clip;
                source.loop = false;
                source.volume = musicVolume;
                source.Play();
                return;
            }
        }

        private void BeginNextTrack()
        {
            outgoing = activeMusic;
            incoming = outgoing == musicA ? musicB : musicA;
            incoming.Stop(); incoming.clip = null;
            StartMusic(incoming);
            if (incoming.clip == null) return;
            incoming.volume = 0f;
            fadePosition = 0f;
            crossfading = true;
        }
    }

    [DisallowMultipleComponent]
    public sealed class TelderAudioLoop : MonoBehaviour
    {
        private AudioSource source;
        private string id;
        private float baseVolume;
        private float maxDistance = 28f;
        public void Setup(string eventId)
        {
            id = eventId;
            if (source == null) source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.clip = TelderAudioSystem.GetLoopClip(id);
            baseVolume = TelderAudioSystem.LoopBaseVolume(id);
            if (source.clip != null && isActiveAndEnabled) source.Play();
        }
        private void OnEnable()
        {
            if (source != null && source.clip != null && !source.isPlaying) source.Play();
        }
        private void Update()
        {
            if (source != null && source.clip != null)
            {
                source.volume = baseVolume * TelderAudioSystem.SfxVolume *
                                TelderAudioSystem.Attenuation(transform.position, maxDistance);
            }
        }
    }
}
