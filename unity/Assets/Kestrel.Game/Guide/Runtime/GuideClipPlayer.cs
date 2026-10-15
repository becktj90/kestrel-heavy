// Plays one Thrum card the way mission control plays a take: Resources/Narration, voice bus,
// duck while it sounds, pause with the flight, stop when Voice is off.
using System;
using System.Collections;
using System.IO;
using Kestrel.Core.Guide;
using Kestrel.Game.Audio;
using UnityEngine;
using UnityEngine.Networking;

namespace Kestrel.Game.Guide
{
    /// <summary>One guide clip at a time. A missing Resources asset or a missing file stays quiet.</summary>
    [DisallowMultipleComponent]
    public sealed class GuideClipPlayer : MonoBehaviour
    {
        public const string ManifestResource = "Narration/guide/manifest";

        AudioDirector director;
        AudioSource voice;
        GuideClipCatalog catalog = GuideClipCatalog.Empty;
        Coroutine fileLoad;
        int token;
        bool loading;
        float startedAt;
        float playFor;
        bool sawAudio;
        bool ducked;

        public string CardId { get; private set; } = "";
        public string LastResource { get; private set; } = "";
        public bool Missing { get; private set; }
        public bool HasClip { get; private set; }
        public bool Sounding { get; private set; }
        public bool Held { get; private set; }

        public void Bind(AudioDirector audio) => director = audio;

        public void Use(GuideClipCatalog next) => catalog = next ?? GuideClipCatalog.Empty;

        public bool TryCard(string cardId, out GuideClip clip)
        {
            clip = null;
            return catalog != null && catalog.TryCard(cardId, out clip);
        }

        /// <summary>Pause with the flight. Unpause continues the same sample.</summary>
        public void Hold(bool on)
        {
            Held = on;
            if (voice == null || !HasClip) return;
            if (on) { if (voice.isPlaying) voice.Pause(); }
            else if (Sounding) voice.UnPause();
        }

        public void Play(string cardId, bool allowed)
        {
            if (!allowed || string.IsNullOrEmpty(cardId))
            {
                Stop();
                return;
            }
            if (catalog == null || !catalog.TryCard(cardId, out var clip))
            {
                if (CardId == cardId && Missing) return;
                Missing = true;
                HasClip = false;
                Sounding = false;
                loading = false;
                CardId = cardId;
                LastResource = "";
                StopVoice();
                return;
            }
            if (CardId == clip.Id && (Missing || HasClip || Sounding || loading)) return;
            int mine = ++token;
            CardId = clip.Id;
            LastResource = clip.Resource;
            Missing = false;
            HasClip = false;
            Sounding = false;
            loading = false;
            EnsureSource();
            var loaded = Resources.Load<AudioClip>(clip.Resource);
            if (loaded != null)
            {
                StartClip(loaded, clip.Duration);
                return;
            }
            if (!string.IsNullOrEmpty(clip.FullPath) && File.Exists(clip.FullPath))
            {
                loading = true;
                if (fileLoad != null) StopCoroutine(fileLoad);
                fileLoad = StartCoroutine(LoadFile(clip.FullPath, clip.Duration, mine));
                return;
            }
            Missing = true;
            StopVoice();
        }

        public void Replay()
        {
            if (voice == null || voice.clip == null || Held) return;
            voice.time = 0f;
            voice.volume = MixerLevels.Bus(false, MixerLevels.Voice, 1f);
            if (Application.isPlaying) voice.Play();
            startedAt = Time.unscaledTime;
            sawAudio = false;
            Sounding = true;
            Missing = false;
            HasClip = true;
            Duck(true);
        }

        public void Stop()
        {
            token++;
            CardId = "";
            HasClip = false;
            Sounding = false;
            loading = false;
            if (fileLoad != null)
            {
                StopCoroutine(fileLoad);
                fileLoad = null;
            }
            StopVoice();
        }

        public static GuideClipCatalog ReadManifest(string repoRoot)
        {
            var fromRepo = GuideClipCatalog.Load(repoRoot);
            if (fromRepo.Count > 0) return fromRepo;
            var asset = Resources.Load<TextAsset>(ManifestResource);
            if (asset == null || string.IsNullOrEmpty(asset.text)) return GuideClipCatalog.Empty;
            try { return GuideClipCatalog.Parse(asset.text, repoRoot); }
            catch (FormatException) { return GuideClipCatalog.Empty; }
        }

        void Update()
        {
            if (Held || !Sounding || voice == null || !Application.isPlaying) return;
            if (voice.isPlaying) { sawAudio = true; return; }
            if (!sawAudio && Time.unscaledTime < startedAt + 0.1f) return;
            if (Time.unscaledTime < startedAt + playFor && !sawAudio) return;
            Sounding = false;
            Duck(false);
        }

        void EnsureSource()
        {
            if (voice != null) return;
            var child = new GameObject("ThrumVoice");
            child.transform.SetParent(transform, false);
            voice = child.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0f;
            voice.dopplerLevel = 0f;
            voice.loop = false;
        }

        void StartClip(AudioClip clip, float duration)
        {
            EnsureSource();
            if (fileLoad != null)
            {
                StopCoroutine(fileLoad);
                fileLoad = null;
            }
            playFor = Mathf.Max(0.05f, duration > 0f ? duration : clip.length);
            startedAt = Time.unscaledTime;
            sawAudio = false;
            HasClip = true;
            Missing = false;
            Sounding = !Held;
            voice.Stop();
            voice.clip = clip;
            voice.volume = MixerLevels.Bus(false, MixerLevels.Voice, 1f);
            if (Application.isPlaying && !Held) voice.Play();
            else Sounding = false;
            Duck(!Held);
        }

        IEnumerator LoadFile(string path, float duration, int mine)
        {
            AudioType type = AudioType.MPEG;
            if (path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)) type = AudioType.WAV;
            else if (path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)) type = AudioType.OGGVORBIS;
            using (var req = UnityWebRequestMultimedia.GetAudioClip("file://" + path, type))
            {
                yield return req.SendWebRequest();
                fileLoad = null;
                loading = false;
                if (mine != token) yield break;
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Missing = true;
                    StopVoice();
                    yield break;
                }
                var clip = DownloadHandlerAudioClip.GetContent(req);
                if (clip == null)
                {
                    Missing = true;
                    StopVoice();
                    yield break;
                }
                StartClip(clip, duration);
            }
        }

        void StopVoice()
        {
            sawAudio = false;
            Sounding = false;
            if (voice != null) voice.Stop();
            Duck(false);
        }

        void Duck(bool on)
        {
            if (ducked == on) return;
            ducked = on;
            if (director == null) director = FindAnyObjectByType<AudioDirector>();
            director?.SetVoiceDuck(on);
        }
    }
}
