using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Guide
{
    /// <summary>Hosts the subtitle stack on a UIDocument. G9 points this at the real guide engine.
    /// Until then it keeps a MockGuideEngine so the view still runs.</summary>
    [DisallowMultipleComponent]
    public sealed class GuideBoxDriver : MonoBehaviour
    {
        public UIDocument Document;
        public bool Compact;
        public float HeaderPoints;
        public GuidePlace Place = GuidePlace.Designer;

        GuideStage stage;
        GuideStackView view;
        VisualElement overlay;
        AudioSource blipSource;
        AudioClip blipClip;
        float blipUntil;
        MockGuideEngine owned;

        public GuideStage Stage => stage;
        public NarrationSubtitleSink Subtitles { get; private set; }
        public IGuideEngine Engine => stage != null ? stage.Engine : null;

        public void Use(IGuideEngine engine, NarrationSubtitles clips = null)
        {
            Ensure();
            stage.Use(engine, clips);
        }

        void OnEnable() => Ensure();

        void Ensure()
        {
            if (stage == null)
            {
                owned = new MockGuideEngine();
                var manifest = Resources.Load<TextAsset>("Narration/manifest");
                var clips = manifest != null ? NarrationSubtitles.Parse(manifest.text) : new NarrationSubtitles();
                stage = new GuideStage(owned, clips) { Voice = new DeviceGuideVoice(), Place = Place };
                Subtitles = new NarrationSubtitleSink { Stage = stage };
                var loaded = GuideArt.Load();
                stage.Glyphs = loaded.Glyphs;
            }
            if (view != null) return;
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
            view = new GuideStackView(overlay, stage)
            {
                Glyphs = art.Glyphs,
                Atlas = art.Atlas,
                Portrait = art.Portrait,
            };
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
            if (view == null) Ensure();
            if (stage == null || view == null) return;
            if (blipUntil > 0f && Time.unscaledTime >= blipUntil && blipSource != null) blipSource.Stop();
            stage.Place = Place;
            stage.ReduceMotionOverride = PlayerPrefs.GetInt(GuideNative.ReduceMotionPref, 0) != 0 || GuideNative.ReduceMotion;
            if (stage.Engine is MockGuideEngine mock) mock.Settings.Muted = Kestrel.Game.Audio.AudioPrefs.SfxMuted;
            ApplyScreen();
            view.Tick(Time.unscaledDeltaTime);
        }

        void OnBlip()
        {
            if (blipSource == null || blipClip == null) return;
            if (Kestrel.Game.Audio.AudioPrefs.SfxMuted) return;
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
            view.Screen = GuideScreen.FromPixels(Screen.width, Screen.height, safe.x, safe.y, safe.width, safe.height, dpr, HeaderPoints, Compact);
            float widthPt = Screen.width / dpr;
            float panelW = overlay != null && overlay.resolvedStyle.width > 1f ? overlay.resolvedStyle.width : widthPt;
            view.PanelPerPoint = widthPt > 1f ? panelW / widthPt : 1f;
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
