using Kestrel.Core.Guide;
using Kestrel.Game.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Guide
{
    /// <summary>Pops Thrum's portrait on menu and campaign screens. Ascent hides her.
    /// A mock engine is used only when the script cannot be loaded.</summary>
    [DisallowMultipleComponent]
    public sealed class GuideBoxDriver : MonoBehaviour
    {
        public UIDocument Document;
        public bool Compact;
        public float HeaderPoints;
        public GuidePlace Place = GuidePlace.Designer;

        /// <summary>Home sets this to the bottom of its top band and wordmark (panel units from the panel top) while it is
        /// on screen, so the corner pop rests under them instead of on the gear. Null or 0 keeps the plain corner.</summary>
        public static System.Func<float> ClearTopPanel;

        GuideStage stage;
        GuidePopView pop;
        GuideClipPlayer voice;
        VisualElement overlay;
        AudioSource blipSource;
        AudioClip blipClip;
        float blipUntil;
        float shownAt;
        string shownKey = "";
        GuideScreen screen = new GuideScreen(375f, 667f, 20f, 0f, 0f, 0f, 0f, false);
        float panelPerPoint = 1f;
        readonly GuideSeenWatch seenWatch = new GuideSeenWatch();

        public GuideStage Stage => stage;
        public NarrationSubtitleSink Subtitles { get; private set; }
        public IGuideEngine Engine => stage != null ? stage.Engine : null;
        public GuideClipPlayer Voice => voice;

        public void Use(IGuideEngine engine, NarrationSubtitles clips = null)
        {
            Ensure();
            stage.Use(engine, clips);
        }

        public void Silence()
        {
            shownKey = "";
            if (voice != null) voice.Stop();
            if (pop != null) pop.Hide();
        }

        void OnEnable() => Ensure();

        void Ensure()
        {
            if (stage == null)
            {
                IGuideEngine engine = GuideSession.Engine;
                if (engine == null)
                {
                    var live = GuideHost.TryLoad();
                    if (live != null)
                    {
                        GuideSession.Bind(live);
                        engine = live;
                    }
                }
                if (engine == null) engine = new MockGuideEngine();
                var manifest = Resources.Load<TextAsset>("Narration/manifest");
                var clips = manifest != null ? NarrationSubtitles.Parse(manifest.text) : new NarrationSubtitles();
                stage = new GuideStage(engine, clips) { Voice = new DeviceGuideVoice(), Place = Place };
                Subtitles = new NarrationSubtitleSink { Stage = stage };
                var loaded = GuideArt.Load();
                stage.Glyphs = loaded.Glyphs;
            }
            if (voice == null)
            {
                voice = GetComponent<GuideClipPlayer>();
                if (voice == null) voice = gameObject.AddComponent<GuideClipPlayer>();
                voice.Use(GuideClipPlayer.ReadManifest(RepoRoot()));
            }
            if (pop != null) return;
            var art = GuideArt.Load();
            var doc = Document != null ? Document : GetComponent<UIDocument>();
            var parent = doc != null ? doc.rootVisualElement : null;
            if (parent == null) return;
            overlay = new VisualElement { name = "guide-overlay", pickingMode = PickingMode.Ignore };
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.right = 0;
            overlay.style.top = 0;
            overlay.style.bottom = 0;
            parent.Add(overlay);
            pop = new GuidePopView(overlay)
            {
                Glyphs = art.Glyphs,
                Atlas = art.Atlas,
                Portrait = art.Portrait,
            };
            pop.Tapped += OnTap;
            overlay.RegisterCallback<GeometryChangedEvent>(_ => ApplyScreen());
            blipSource = gameObject.AddComponent<AudioSource>();
            blipSource.playOnAwake = false;
            blipSource.spatialBlend = 0f;
            blipSource.loop = false;
            blipClip = Resources.Load<AudioClip>("Sfx/quindar");
            stage.Blip += OnBlip;
        }

        void Update()
        {
            if (pop == null) Ensure();
            if (stage == null || pop == null) return;
            if (blipUntil > 0f && Time.unscaledTime >= blipUntil && blipSource != null) blipSource.Stop();
            stage.Place = Place;
            stage.ReduceMotionOverride = PlayerPrefs.GetInt(GuideNative.ReduceMotionPref, 0) != 0 || GuideNative.ReduceMotion;
            if (stage.Engine != null && stage.Engine.Settings != null)
                stage.Engine.Settings.Muted = AudioPrefs.SfxMuted;
            bool paused = Time.timeScale <= 0f;
            var director = FindAnyObjectByType<AudioDirector>();
            if (director != null)
            {
                voice.Bind(director);
                if (director.Held) paused = true;
            }
            voice.Hold(paused);
            ApplyScreen();
            if (GuideClipPlayer.InFlight || !GuidePopLayout.Shows(Place))
            {
                if (voice.Sounding || voice.HasClip || voice.CardId.Length > 0) Silence();
                else pop.Hide();
                return;
            }
            stage.Tick(Time.unscaledDeltaTime);
            var live = stage.Engine as LiveGuideEngine;
            if (live != null && seenWatch.Consume(live.State.SeenLines.Count, live.State.FirstRunDone))
                GuideHost.Save(live);
            Present(live);
        }

        void Present(LiveGuideEngine live)
        {
            var models = stage.Models();
            GuideBoxModel model = null;
            for (int i = 0; i < models.Length; i++)
            {
                if (models[i] != null && models[i].Speaker == GuideSpeaker.Thrum)
                {
                    model = models[i];
                    break;
                }
            }
            if (model == null)
            {
                shownKey = "";
                if (voice.HasClip || voice.Sounding || voice.CardId.Length > 0) voice.Stop();
                pop.Hide();
                return;
            }
            string key = model.Id + ":" + model.Page + ":" + model.CardId;
            if (key != shownKey)
            {
                shownKey = key;
                shownAt = Time.unscaledTime;
            }
            var settings = stage.Engine.Settings ?? new GuideSettings();
            // Voice is the narration bus (NarrationMuted). Sound only silences the blip. Pause is Hold.
            bool voiceOn = live != null && live.ClipVoice && !AudioPrefs.NarrationMuted;
            bool wantsClip = voiceOn && model.Speaks;
            if (wantsClip) voice.Play(model.CardId, true);
            else if (voice.HasClip || voice.Sounding || voice.CardId.Length > 0) voice.Stop();
            bool missing = !model.Speaks || !voice.TryCard(model.CardId, out _) || voice.Missing;
            bool caption = GuideCaption.Visible(voiceOn, AudioPrefs.NarrationMuted, missing);
            float glyph = GuideCaption.Glyph(settings.Size);
            bool reduce = stage.ReduceMotionOverride || settings.ReduceMotion;
            float slide = GuidePopLayout.SlideSeconds > 0f ? (Time.unscaledTime - shownAt) / GuidePopLayout.SlideSeconds : 1f;
            float clear = 0f;
            if (Place == GuidePlace.Menu && ClearTopPanel != null && panelPerPoint > 0f)
            {
                try { clear = ClearTopPanel() / panelPerPoint; }
                catch (System.Exception) { clear = 0f; }
                if (float.IsNaN(clear) || clear < 0f) clear = 0f;
            }
            var frame = GuidePopLayout.Place(screen, slide, reduce, caption, clear);
            string line = GuideCaption.Line(caption ? model.Shown : model.Full, frame.CaptionW, glyph);
            pop.Show(frame, panelPerPoint, caption, line, glyph);
        }

        void OnTap()
        {
            if (stage == null || !GuidePopLayout.Shows(Place)) return;
            var models = stage.Models();
            int index = -1;
            for (int i = 0; i < models.Length; i++)
            {
                if (models[i] != null && models[i].Speaker == GuideSpeaker.Thrum) { index = i; break; }
            }
            if (index < 0) return;
            var model = models[index];
            var settings = stage.Engine.Settings ?? new GuideSettings();
            var live = stage.Engine as LiveGuideEngine;
            bool voiceOn = live != null && live.ClipVoice && !AudioPrefs.NarrationMuted;
            bool missing = !model.Speaks || voice.Missing || !voice.HasClip;
            bool caption = GuideCaption.Visible(voiceOn, AudioPrefs.NarrationMuted, missing);
            bool done = model.Shown != null && model.Full != null && model.Shown.Length >= model.Full.Length;
            bool canPlay = voiceOn && model.Speaks && voice.HasClip && !voice.Missing;
            var action = GuidePopLayout.OnTap(caption, done, canPlay, voice.Sounding);
            if (action == GuidePopAction.Finish)
            {
                stage.Tap(index);
                return;
            }
            if (action == GuidePopAction.Replay)
            {
                voice.Replay();
                return;
            }
            if (!done) stage.Tap(index);
            stage.Tap(index);
            shownKey = "";
        }

        void OnBlip()
        {
            if (blipSource == null || blipClip == null) return;
            if (AudioPrefs.SfxMuted) return;
            blipSource.Stop();
            blipSource.clip = blipClip;
            blipSource.volume = 0.12f;
            blipSource.time = 0f;
            blipSource.Play();
            blipUntil = Time.unscaledTime + GuidePacing.BlipSeconds;
        }

        void ApplyScreen()
        {
            float dpr = Screen.dpi >= 10f ? Screen.dpi / 160f : 2f;
            var safe = Screen.safeArea;
            screen = GuideScreen.FromPixels(Screen.width, Screen.height, safe.x, safe.y, safe.width, safe.height, dpr, HeaderPoints, Compact);
            float widthPt = Screen.width / dpr;
            float panelW = overlay != null && overlay.resolvedStyle.width > 1f ? overlay.resolvedStyle.width : widthPt;
            panelPerPoint = widthPt > 1f ? panelW / widthPt : 1f;
        }

        static string RepoRoot()
        {
            try { return Kestrel.Core.Data.Catalog.FindRepoRoot(System.IO.Directory.GetCurrentDirectory()); }
            catch (System.Exception) { return ""; }
        }
    }

    public sealed class GuideArt
    {
        public Texture2D Portrait;
        public Texture2D Atlas;
        public GlyphTable Glyphs;

        public static GuideArt Load()
        {
            var art = new GuideArt();
            art.Portrait = Point(Resources.Load<Texture2D>("Guide/thrum"));
            art.Atlas = Point(Resources.Load<Texture2D>("Guide/atlas"));
            var json = Resources.Load<TextAsset>("Guide/glyphs");
            if (json != null && !string.IsNullOrEmpty(json.text)) art.Glyphs = GlyphTable.Parse(json.text);
            return art;
        }

        public static Texture2D Point(Texture2D tex)
        {
            if (tex != null) tex.filterMode = FilterMode.Point;
            return tex;
        }
    }
}
