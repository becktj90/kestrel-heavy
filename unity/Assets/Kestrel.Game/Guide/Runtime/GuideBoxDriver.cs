using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Guide
{
    /// <summary>Hosts the subtitle stack on a UIDocument and reads the live guide engine.
    /// A mock is used only when the script cannot be loaded.</summary>
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
        readonly GuideSeenWatch seenWatch = new GuideSeenWatch();

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
            if (stage.Engine != null && stage.Engine.Settings != null)
                stage.Engine.Settings.Muted = Kestrel.Game.Audio.AudioPrefs.SfxMuted;
            ApplyScreen();
            view.Tick(Time.unscaledDeltaTime);
            var live = stage.Engine as LiveGuideEngine;
            if (live != null && seenWatch.Consume(live.State.SeenLines.Count, live.State.FirstRunDone))
                GuideHost.Save(live);
            ApplyClearance();
            GuideHost.Voice(this);
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
            var screen = GuideScreen.FromPixels(Screen.width, Screen.height, safe.x, safe.y, safe.width, safe.height, dpr, HeaderPoints, Compact);
            float widthPt = Screen.width / dpr;
            float panelW = overlay != null && overlay.resolvedStyle.width > 1f ? overlay.resolvedStyle.width : widthPt;
            view.PanelPerPoint = widthPt > 1f ? panelW / widthPt : 1f;
            if (Place == GuidePlace.Menu)
                screen = screen.WithFooter(MeasureFooter(view.PanelPerPoint));
            view.Screen = screen;
        }

        float MeasureFooter(float panelPerPoint)
        {
            if (overlay == null || overlay.parent == null || panelPerPoint <= 0f) return GuidePacing.MenuFooterPt;
            string[] names = { "briefing-build", "briefing-back", "missions-home", "home-launch" };
            VisualElement anchor = null;
            for (int i = 0; i < names.Length; i++)
            {
                var found = overlay.parent.Q(names[i]);
                if (found == null || found.resolvedStyle.display == DisplayStyle.None) continue;
                if (found.resolvedStyle.height < 1f) continue;
                anchor = found;
                break;
            }
            if (anchor == null) return GuidePacing.MenuFooterPt;
            float bottom = overlay.worldBound.yMax;
            float top = anchor.worldBound.yMin;
            if (float.IsNaN(top) || float.IsNaN(bottom) || bottom <= 1f || top <= 1f || top >= bottom) return GuidePacing.MenuFooterPt;
            // Distance down to the panel bottom. The key already sits inside the safe area, so this
            // is the whole reserve; the layout does not add the safe inset a second time.
            float pt = (bottom - top) / panelPerPoint;
            return pt > GuidePacing.MenuFooterPt ? pt : GuidePacing.MenuFooterPt;
        }

        void ApplyClearance()
        {
            if (overlay == null || overlay.parent == null || view == null) return;
            float px = Place == GuidePlace.Menu ? view.ClearancePx : 0f;
            Pad("mission-scroll", px);
            Pad("briefing-scroll", px);
        }

        void Pad(string name, float px)
        {
            var el = overlay.parent.Q(name);
            if (el == null) return;
            var style = el.style.marginBottom;
            // A set length keeps keyword Undefined and stores the pixels on value.
            float cur = style.keyword == StyleKeyword.Undefined ? style.value.value : -1f;
            if (Mathf.Abs(cur - px) > 0.5f) el.style.marginBottom = px;
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
