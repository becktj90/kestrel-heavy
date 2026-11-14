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

        /// <summary>Panel pixels from the panel bottom that the opening sheet must stay above. The two arguments are the
        /// sheet column's left and width in panel pixels, so a control beside the sheet does not lift it. 0 keeps the menu footer.</summary>
        public static System.Func<float, float, float> ClearBottomPanel;

        /// <summary>Home sets this while it is up: true keeps the corner pop off the menu (Starter Home, home.thrum off).</summary>
        public static System.Func<bool> HideOnMenu;

        bool Hidden()
        {
            if (Place != GuidePlace.Menu || HideOnMenu == null) return false;
            try { return HideOnMenu(); }
            catch (System.Exception) { return false; }
        }

        /// <summary>Settings > Show corner character. Off unless the player switched it on (no stored value is off).</summary>
        public static bool SpriteOn()
            => Kestrel.Core.Features.ExperienceSettings.CornerCharacter(HasPref, ReadPref);

        // Cached once so the per-frame check allocates nothing.
        static readonly System.Func<string, bool> HasPref = key => PlayerPrefs.HasKey(key);
        static readonly System.Func<string, int> ReadPref = key => PlayerPrefs.GetInt(key, 0);

        /// <summary>True while the corner portrait's views exist. False whenever the setting is off.</summary>
        public bool HasSprite => pop != null;

        GuideStage stage;
        GuidePopView pop;
        GuideStackView welcomeStack;
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
            if (!SpriteOn()) return;   // off: no overlay, no portrait view, no blip source
            var art = GuideArt.Load();
            var doc = Document != null ? Document : GetComponent<UIDocument>();
            var parent = doc != null ? doc.rootVisualElement : null;
            if (parent == null) return;
            // The opening sheet may already own the overlay. A second one stacks on Home.
            if (overlay == null)
            {
                overlay = new VisualElement { name = "guide-overlay", pickingMode = PickingMode.Ignore };
                overlay.style.position = Position.Absolute;
                overlay.style.left = 0;
                overlay.style.right = 0;
                overlay.style.top = 0;
                overlay.style.bottom = 0;
                parent.Add(overlay);
                overlay.RegisterCallback<GeometryChangedEvent>(_ => ApplyScreen());
            }
            pop = new GuidePopView(overlay)
            {
                Glyphs = art.Glyphs,
                Atlas = art.Atlas,
                Portrait = art.Portrait,
            };
            pop.Tapped += OnTap;
            if (blipSource != null) return;
            blipSource = gameObject.AddComponent<AudioSource>();
            blipSource.playOnAwake = false;
            blipSource.spatialBlend = 0f;
            blipSource.loop = false;
            blipClip = Resources.Load<AudioClip>("Sfx/quindar");
            stage.Blip += OnBlip;
        }

        /// <summary>Builds or removes the corner portrait to match the setting. Cheap when nothing changed.
        /// The overlay is absolute and ignores picking, so removing it leaves no gap in the layout.</summary>
        public void SyncSprite()
        {
            var live = stage != null ? stage.Engine as LiveGuideEngine : null;
            bool welcome = WelcomePlaying(live);
            if (SpriteOn())
            {
                if (pop == null) Ensure();
                if (welcome) EnsureWelcomeStack();
                return;
            }
            // The portrait follows the setting even while the first-run sheet is up.
            DropPortrait();
            if (welcome)
            {
                // Welcome keeps its own stack without the corner pop; drop any pop left from an earlier On.
                // Lines that are not the welcome are still dropped below once the intro is finished.
                if (pop != null)
                {
                    Silence();
                    if (stage != null) stage.Blip -= OnBlip;
                    pop.Pop?.RemoveFromHierarchy();
                    pop = null;
                    if (blipSource != null)
                    {
                        if (Application.isPlaying) Destroy(blipSource);
                        else DestroyImmediate(blipSource);
                        blipSource = null;
                    }
                    blipClip = null;
                    blipUntil = 0f;
                    shownKey = "";
                }
                EnsureWelcomeStack();
                return;
            }
            DropSprite();
            // Off, and no opening: nothing consumes the queue, so keep it empty.
            if (live != null) live.DropQueued();
        }

        void DropPortrait()
        {
            if (pop == null && blipSource == null && welcomeStack != null) return;
            Silence();
            if (stage != null) stage.Blip -= OnBlip;
            if (pop != null)
            {
                pop.Tapped -= OnTap;
                pop.Pop.RemoveFromHierarchy();
                pop = null;
            }
            if (blipSource != null)
            {
                if (Application.isPlaying) Destroy(blipSource);
                else DestroyImmediate(blipSource);
                blipSource = null;
            }
            blipClip = null;
            blipUntil = 0f;
            shownKey = "";
            if (welcomeStack != null) return;
            if (overlay != null) overlay.RemoveFromHierarchy();
            overlay = null;
        }

        void EnsureWelcomeStack()
        {
            if (welcomeStack != null || stage == null) return;
            var doc = Document != null ? Document : GetComponent<UIDocument>();
            var parent = doc != null ? doc.rootVisualElement : null;
            if (parent == null) return;
            if (overlay == null)
            {
                overlay = new VisualElement { name = "guide-overlay", pickingMode = PickingMode.Ignore };
                overlay.style.position = Position.Absolute;
                overlay.style.left = 0;
                overlay.style.right = 0;
                overlay.style.top = 0;
                overlay.style.bottom = 0;
                parent.Add(overlay);
            }
            var art = GuideArt.Load();
            welcomeStack = new GuideStackView(overlay, stage)
            {
                Glyphs = art.Glyphs,
                Atlas = art.Atlas,
                Portrait = art.Portrait,
                PanelPerPoint = panelPerPoint,
                Screen = screen,
            };
        }

        void TearDownWelcomeStack()
        {
            if (welcomeStack == null) return;
            welcomeStack.Root?.Q("guide-stack")?.RemoveFromHierarchy();
            welcomeStack = null;
            if (pop == null && overlay != null)
            {
                overlay.RemoveFromHierarchy();
                overlay = null;
            }
        }

        void DropSprite()
        {
            TearDownWelcomeStack();
            if (pop == null && overlay == null && blipSource == null) return;
            Silence();
            if (stage != null) stage.Blip -= OnBlip;
            if (overlay != null) overlay.RemoveFromHierarchy();
            if (blipSource != null)
            {
                if (Application.isPlaying) Destroy(blipSource);
                else DestroyImmediate(blipSource);
            }
            overlay = null;
            pop = null;
            blipSource = null;
            blipClip = null;
            blipUntil = 0f;
            shownKey = "";
        }

        void Update()
        {
            SyncSprite();
            if (stage == null) return;
            var liveEngine = stage.Engine as LiveGuideEngine;
            bool welcome = WelcomePlaying(liveEngine);
            if (!welcome && pop == null) return;
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
            if (welcome)
            {
                FitWelcomeToShell();
                ApplyMenuClearance();
            }
            if (GuideClipPlayer.InFlight || !GuidePopLayout.Shows(Place) || (!welcome && Hidden()))
            {
                if (voice != null && (voice.Sounding || voice.HasClip || voice.CardId.Length > 0)) Silence();
                else if (pop != null) pop.Hide();
                return;
            }
            stage.Tick(Time.unscaledDeltaTime);
            if (liveEngine != null && seenWatch.Consume(liveEngine.State.SeenLines.Count, liveEngine.State.FirstRunDone))
                GuideHost.Save(liveEngine);
            if (welcome)
            {
                EnsureWelcomeStack();
                if (welcomeStack != null)
                {
                    welcomeStack.Screen = screen;
                    welcomeStack.PanelPerPoint = panelPerPoint;
                }
                welcomeStack.Tick(Time.unscaledDeltaTime);
                if (pop != null) pop.Hide();
                if (!WelcomePlaying(liveEngine)) TearDownWelcomeStack();
                return;
            }
            TearDownWelcomeStack();
            if (voice == null) return;
            Present(liveEngine);
        }

        static bool WelcomePlaying(LiveGuideEngine live) => live != null && OpeningIntroSequence.Active(live);

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

        /// <summary>The opening sheet uses the shell's safe rect, which is the area Home captures check. Screen.safeArea
        /// in the editor is the full frame, so a sheet placed from it sits in the inset and fails that check.</summary>
        void FitWelcomeToShell()
        {
            var doc = Document != null ? Document : GetComponent<UIDocument>();
            var root = doc != null ? doc.rootVisualElement : null;
            var safe = root != null ? root.Q("safe") : null;
            if (root == null || safe == null) return;
            if (safe.worldBound.width < 1f || root.worldBound.width < 1f) return;
            // Lay the sheet out in panel pixels. Screen.dpi on the capture runner is not the device scale,
            // and treating the panel as 2x points makes a 600 pt column cover a 1280-wide web Home.
            panelPerPoint = 1f;
            float left = safe.worldBound.xMin - root.worldBound.xMin;
            float top = safe.worldBound.yMin - root.worldBound.yMin;
            float right = root.worldBound.xMax - safe.worldBound.xMax;
            float bottom = root.worldBound.yMax - safe.worldBound.yMax;
            if (left < 0f) left = 0f;
            if (top < 0f) top = 0f;
            if (right < 0f) right = 0f;
            if (bottom < 0f) bottom = 0f;
            var title = root.Q("mm-title");
            if (title != null && title.worldBound.height > 1f)
            {
                float titleBottom = title.worldBound.yMax - root.worldBound.yMin;
                if (titleBottom > top) top = titleBottom;
            }
            screen = new GuideScreen(root.worldBound.width, root.worldBound.height, top, bottom, left, right, 0f, screen.Compact, 0f);
        }

        /// <summary>Home reports how far up from the panel bottom its controls reach, so the opening sheet sits above them.</summary>
        void ApplyMenuClearance()
        {
            if (Place != GuidePlace.Menu || ClearBottomPanel == null || panelPerPoint <= 0f) return;
            var doc = Document != null ? Document : GetComponent<UIDocument>();
            var root = doc != null ? doc.rootVisualElement : null;
            float origin = root != null ? root.worldBound.xMin : 0f;
            float left = origin + screen.ColumnX * panelPerPoint;
            float width = screen.ColumnWidth * panelPerPoint;
            float px;
            try { px = ClearBottomPanel(left, width); }
            catch (System.Exception) { px = 0f; }
            if (float.IsNaN(px) || px < 1f) return;
            float pt = px / panelPerPoint + 8f;
            // Leave the sheet on screen, under the title. A control in the header must not shove it above y=0.
            float maxFooter = screen.Height - screen.DockTop - 8f;
            if (maxFooter < 0f) maxFooter = 0f;
            if (pt > maxFooter) pt = maxFooter;
            if (pt > screen.Footer) screen = screen.WithFooter(pt);
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
