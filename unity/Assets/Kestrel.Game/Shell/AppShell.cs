// App bootstrap. Lives in the generated Boot scene (the only object there): loads data, creates
// the camera, quality service, portrait guard, UI root and scene flow, then enters Home.
// Nothing here is placed by hand in a scene; ShellSetup only adds this component.
using Kestrel.Game.Audio;
using Kestrel.Game.Layout;
using Kestrel.Game.Quality;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Shell
{
    [DefaultExecutionOrder(-1000)]
    public sealed class AppShell : MonoBehaviour
    {
        public PanelSettings PanelSettings;
        public StyleSheet Tokens;

        public static AppShell Instance { get; private set; }
        public VisualSpecData Spec { get; private set; }
        public SceneFlow Flow { get; private set; }
        public PortraitGuard Portrait { get; private set; }
        public QualityService Quality { get; private set; }
        public ThemedFillCamera Fill { get; private set; }
        /// <summary>Full-bleed UI root (backgrounds) and its safe-area child (all interactive UI).</summary>
        public VisualElement Root { get; private set; }
        public VisualElement Safe { get; private set; }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;

            // Committed Boot.unity cannot reference the generated PanelSettings asset; load it by path.
            if (PanelSettings == null) PanelSettings = Resources.Load<PanelSettings>("UI/KH_ShellPanelSettings");
            Spec = VisualSpecData.Load();
            Quality = gameObject.AddComponent<QualityService>();

            var camGo = new GameObject("ShellCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<Camera>();
            Fill = camGo.AddComponent<ThemedFillCamera>();
            Fill.Spec = Spec;

            Portrait = gameObject.AddComponent<PortraitGuard>();
            Portrait.Spec = Spec;

            // Configure the UIDocument before it enables so it attaches with our PanelSettings.
            var uiGo = new GameObject("ShellUI");
            uiGo.SetActive(false);
            uiGo.transform.SetParent(transform, false);
            var doc = uiGo.AddComponent<UIDocument>();
            doc.panelSettings = PanelSettings;
            uiGo.SetActive(true);
            Root = doc.rootVisualElement;
            Root.AddToClassList("kh-root");
            if (Tokens != null) Root.styleSheets.Add(Tokens);
            Safe = new VisualElement { name = "safe" };
            Safe.AddToClassList(SafeAreaRoot.SafeClass);
            Root.Add(Safe);
            Portrait.FrameChanged += OnFrame;
            Root.RegisterCallback<GeometryChangedEvent>(_ => OnFrame(Portrait.Frame));

            var listener = camGo.AddComponent<AudioListener>();
            var music = gameObject.AddComponent<Kestrel.Game.Audio.MusicPlayer>();
            Flow = gameObject.AddComponent<SceneFlow>();
            Flow.Leaving += () => listener.enabled = false;
            Flow.Entered += screen =>
            {
                listener.enabled = ShellAudio.ShellListenerOn(screen);
                if (screen == AppScreen.Home) music.Play(MusicBed.Menu);
                else if (screen == AppScreen.Flight) music.Play(MusicBed.Flight);
                else music.Play(MusicBed.Hangar);
            };
        }

        void Start() => Flow.Go(AppScreen.Home);

        /// <summary>Real game entry until Graphics Dev moves the Design panel into the shell: Slice.unity owns
        /// the pad, rocket, panel and flight. The shell (camera + UI) is torn down so Slice renders alone.</summary>
        public const string SliceScene = "Slice";
        public void EnterSlice()
        {
            if (!Application.CanStreamedLevelBeLoaded(SliceScene)) { Debug.LogError("[Kestrel] Slice.unity is not in Build Settings"); return; }
            Destroy(gameObject);
            UnityEngine.SceneManagement.SceneManager.LoadScene(SliceScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void OnFrame(PortraitFrame f)
        {
            if (f.Content.W <= 0) return;
            SafeAreaRoot.Apply(Safe, f);
        }
    }
}
