using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>
    /// Pooled sound playback, music crossfading and ducking. All clips are synthesized at boot,
    /// music on a worker thread so the menu appears instantly.
    /// </summary>
    public static class Audio
    {
        static AudioSource[] pool;
        static int next;
        static AudioSource musicA, musicB;
        static bool aActive = true;
        static readonly AudioClip[] clips = new AudioClip[(int)Sfx.Count];
        static readonly float[] lastPlayed = new float[(int)Sfx.Count];
        static readonly Dictionary<Track, AudioClip> music = new Dictionary<Track, AudioClip>();
        static readonly Dictionary<Track, float[]> pendingMusic = new Dictionary<Track, float[]>();
        static readonly object musicLock = new object();
        static Track current = Track.None, wanted = Track.None;
        static float fade = 1f, duck = 1f, duckTimer;
        static bool ready;

        public static void Init(Transform root)
        {
            if (ready) return;
            ready = true;
            var go = new GameObject("Audio");
            go.transform.SetParent(root, false);
            pool = new AudioSource[28];
            for (int i = 0; i < pool.Length; i++)
            {
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false; s.spatialBlend = 0f; s.loop = false;
                pool[i] = s;
            }
            musicA = go.AddComponent<AudioSource>();
            musicB = go.AddComponent<AudioSource>();
            foreach (var m in new[] { musicA, musicB }) { m.loop = true; m.playOnAwake = false; m.spatialBlend = 0f; m.volume = 0f; }

            for (int i = 1; i < (int)Sfx.Count; i++)
            {
                var data = SoundBank.Make((Sfx)i);
                var c = AudioClip.Create(((Sfx)i).ToString(), data.Length, 1, SoundBank.Rate, false);
                c.SetData(data, 0);
                clips[i] = c;
            }

            // compose the music off the main thread
            var t = new Thread(() =>
            {
                foreach (var tr in new[] { Track.Menu, Track.Battle, Track.Domain, Track.Raid })
                {
                    float[] d;
                    try { d = MusicGen.Make(tr); } catch { d = new float[MusicGen.Rate]; }
                    lock (musicLock) pendingMusic[tr] = d;
                }
            }) { IsBackground = true, Name = "MusicGen" };
            t.Start();
        }

        public static void Play(Sfx id, Vector2 pos, float vol = 1f, float pitch = 1f)
        {
            if (!ready || id == Sfx.None) return;
            var clip = clips[(int)id];
            if (clip == null) return;
            // de-dupe: the same sound can't stack more than every 35ms
            float now = Time.unscaledTime;
            if (now - lastPlayed[(int)id] < 0.035f) return;
            lastPlayed[(int)id] = now;

            var s = pool[next];
            next = (next + 1) % pool.Length;
            s.clip = clip;
            s.volume = Mathf.Clamp01(vol) * Settings.Sfx * Settings.Master;
            s.pitch = Mathf.Clamp(pitch, 0.3f, 3f) * Mathf.Lerp(0.85f, 1f, Match.I != null ? Match.I.TimeScale : 1f);
            float camX = CameraRig.I != null ? CameraRig.I.transform.position.x : 0f;
            s.panStereo = Mathf.Clamp((pos.x - camX) / 14f, -0.6f, 0.6f);
            s.Play();
        }

        public static void Ui(Sfx id) => Play(id, Vector2.zero, 0.7f, 1f);

        /// <summary>The black flash: kill the music for a beat so the impact lands in silence.</summary>
        public static void BlackFlash()
        {
            duck = 0.05f; duckTimer = 0.9f;
            Play(Sfx.BlackFlash, Vector2.zero, 1f, Random.Range(0.97f, 1.03f));
        }

        public static void DomainOpen(DomainTheme theme)
        {
            Sfx id;
            switch (theme)
            {
                case DomainTheme.Void: case DomainTheme.Jackpot: case DomainTheme.Shore: case DomainTheme.Moon: case DomainTheme.Garden: id = Sfx.DomainOpenBright; break;
                case DomainTheme.Shrine: case DomainTheme.Womb: case DomainTheme.Palms: case DomainTheme.Caldera: id = Sfx.DomainOpenDeep; break;
                default: id = Sfx.DomainOpenDark; break;
            }
            duck = 0.25f; duckTimer = 1.5f;
            Play(id, Vector2.zero, 1f);
        }

        public static void PlayMusic(Track t) { wanted = t; }

        /// <summary>Called once per frame by GameRoot.</summary>
        public static void Update(float dt)
        {
            if (!ready) return;
            lock (musicLock)
            {
                if (pendingMusic.Count > 0)
                {
                    foreach (var kv in pendingMusic)
                    {
                        var c = AudioClip.Create("Music_" + kv.Key, kv.Value.Length, 1, MusicGen.Rate, false);
                        c.SetData(kv.Value, 0);
                        music[kv.Key] = c;
                    }
                    pendingMusic.Clear();
                }
            }

            if (duckTimer > 0f) { duckTimer -= dt; if (duckTimer <= 0f) duck = 1f; }
            else duck = Mathf.MoveTowards(duck, 1f, dt * 2f);

            if (wanted != current && (wanted == Track.None || music.ContainsKey(wanted)))
            {
                // start crossfade
                var incoming = aActive ? musicB : musicA;
                if (wanted != Track.None) { incoming.clip = music[wanted]; incoming.time = 0f; incoming.Play(); }
                aActive = !aActive;
                current = wanted;
                fade = 0f;
            }
            fade = Mathf.Min(1f, fade + dt / 1.2f);
            float target = Settings.Music * Settings.Master * 0.55f * duck;
            var active = aActive ? musicA : musicB;
            var other = aActive ? musicB : musicA;
            active.volume = current == Track.None ? 0f : target * fade;
            other.volume = target * (1f - fade) * (other.isPlaying ? 1f : 0f);
            if (fade >= 1f && other.isPlaying) other.Stop();
            float pitch = Match.I != null && !Match.I.Paused ? Mathf.Lerp(0.8f, 1f, Match.I.TimeScale) : 1f;
            active.pitch = pitch;
        }
    }
}
