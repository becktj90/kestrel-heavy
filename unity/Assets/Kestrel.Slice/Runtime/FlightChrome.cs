// In-flight pause and the one-thumb camera controls. The chase camera stays the follow pose;
// drag and pinch only orbit that offset, and a tap or the Follow button hands it back.
using System;
using Kestrel.Core.Flight;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    [DisallowMultipleComponent]
    public sealed class FlightChrome : MonoBehaviour
    {
        public const float TouchPt = 44f;

        VisualElement hud, menu, tools;
        Button pause, follow, cameraBtn, music, sfx, narration;
        FlightRig rig = FlightCameraRig.Following();
        Action<bool> setPaused;
        Action<FlightViewMode> viewChanged;
        FlightViewMode viewMode;
        bool tracking, blockGesture, pinching;
        float pinchDist, lastUpp = 1f;

        public bool Paused { get; private set; }
        public bool Follow => rig.Follow;
        public FlightRig Rig => rig;
        public FlightViewMode View => viewMode;

        public void Bind(VisualElement flightHud, Action<bool> onPause, Action restart, Action exitHome, Action<FlightViewMode> onView = null, FlightViewMode initial = FlightViewMode.Auto)
        {
            hud = flightHud;
            setPaused = onPause;
            viewChanged = onView;
            viewMode = initial;
            if (hud == null) return;
            hud.pickingMode = PickingMode.Ignore;

            tools = new VisualElement { name = "flight-tools" };
            tools.style.flexDirection = FlexDirection.Row;
            tools.style.justifyContent = Justify.SpaceBetween;
            tools.style.marginBottom = 8;
            tools.style.flexShrink = 0;

            follow = Tool("follow", "Follow rocket", "Follow the rocket. Recenters the camera.");
            follow.clicked += () => { rig = FlightCameraRig.Recenter(rig); RefreshFollow(); };
            cameraBtn = Tool("flight-camera", FlightView.Name(viewMode), "Camera. Cycles Auto, Chase, Above, Below and Pad.");
            cameraBtn.clicked += CycleCamera;
            pause = Tool("pause", "Pause", "Pause. Freezes the flight.");
            pause.clicked += () => setPaused?.Invoke(true);
            tools.Add(follow);
            tools.Add(cameraBtn);
            tools.Add(pause);

            var webcast = hud.Q("webcast");
            if (webcast != null) hud.Insert(hud.IndexOf(webcast), tools);
            else hud.Add(tools);

            menu = new VisualElement { name = "pause-menu" };
            menu.AddToClassList("hidden");
            menu.style.position = Position.Absolute;
            menu.style.left = 0;
            menu.style.right = 0;
            menu.style.top = 0;
            menu.style.bottom = 0;
            menu.style.alignItems = Align.Center;
            menu.style.justifyContent = Justify.Center;
            menu.style.backgroundColor = new Color(0.02f, 0.04f, 0.08f, 0.78f);
            var title = new Label { name = "pause-title", text = "Paused" };
            title.style.color = new Color(1f, 0.69f, 0f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 12;
            title.pickingMode = PickingMode.Ignore;
            menu.Add(title);
            menu.Add(MenuButton("resume", "Resume", "Resume the flight.", () => setPaused?.Invoke(false)));
            menu.Add(MenuButton("restart", "Restart", "Restart. Flies the same design again.", () => restart?.Invoke()));
            menu.Add(MenuButton("exit-home", "Exit to Home", "Exit to Home.", () => exitHome?.Invoke()));
            music = MenuButton("pause-music", "Music on", "Music. Tap to mute or unmute.", ToggleMusic);
            sfx = MenuButton("pause-sfx", "Sound on", "Sound effects. Tap to mute or unmute.", ToggleSfx);
            narration = MenuButton("pause-narration", "Narration on", "Narration. Tap to mute or unmute.", ToggleNarration);
            menu.Add(music);
            menu.Add(sfx);
            menu.Add(narration);
            hud.Add(menu);
            RefreshFollow();
            RefreshCamera();
            RefreshAudio();
            hud.RegisterCallback<GeometryChangedEvent>(_ => hud.schedule.Execute(() => Kestrel.Game.Shell.TextFit.Apply(menu)));
        }

        public void Layout(float unitsPerPoint)
        {
            float upp = unitsPerPoint >= 0.5f ? unitsPerPoint : 1f;
            lastUpp = upp;
            float h = TouchPt * upp;
            StyleTool(follow, upp, h, FlightHudLayout.FollowWidthPt);
            StyleTool(cameraBtn, upp, h, FlightView.ButtonWidth(viewMode));
            StyleTool(pause, upp, h, FlightHudLayout.PauseWidthPt);
            if (follow != null) follow.style.fontSize = 13f * upp;
            FitTool(follow, upp);
            FitTool(cameraBtn, upp, FlightView.ButtonFontPt);
            FitTool(pause, upp);
            if (menu == null) return;
            var title = menu.Q<Label>("pause-title");
            if (title != null) title.style.fontSize = Kestrel.Game.Shell.TextFit.Cap(title, 22f * upp);
            menu.Query<Button>().ForEach(b =>
            {
                b.style.minHeight = h;
                b.style.minWidth = h;
                b.style.whiteSpace = WhiteSpace.Normal;
                b.style.textOverflow = TextOverflow.Clip;
                float fitted = Kestrel.Game.Shell.TextFit.Cap(b, 17f * upp);
                b.style.fontSize = fitted;
                b.style.marginTop = 8f * upp;
                b.style.height = StyleKeyword.Auto;
                b.style.maxWidth = Length.Percent(100);
            });
            Kestrel.Game.Shell.TextFit.Apply(menu);
        }

        public void Show(bool paused)
        {
            Paused = paused;
            if (paused) { tracking = false; pinching = false; }
            if (menu != null) menu.EnableInClassList("hidden", !paused);
            RefreshAudio();
        }

        public void OnFlightStarted()
        {
            rig = FlightCameraRig.Recenter(rig);
            tracking = false;
            pinching = false;
            Show(false);
            RefreshFollow();
            RefreshCamera();
        }

        public Vector3 Frame(Vector3 chaseOffset) => FlightCameraRig.Orbit(chaseOffset, rig);

        public void TickInput()
        {
            if (Paused || hud == null || hud.ClassListContains("hidden")) return;
            if (Input.touchCount >= 2)
            {
                tracking = false;
                Pinch(Input.GetTouch(0).position, Input.GetTouch(1).position);
                return;
            }
            pinching = false;
            if (Input.touchCount == 1)
            {
                var t = Input.GetTouch(0);
                Pointer(t.position, t.phase == TouchPhase.Began, t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled);
                return;
            }
            Pointer(Input.mousePosition, Input.GetMouseButtonDown(0), Input.GetMouseButtonUp(0));
        }

        void Pointer(Vector2 pos, bool began, bool ended)
        {
            if (began)
            {
                blockGesture = OverControl(pos);
                tracking = !blockGesture;
                if (tracking) rig = FlightCameraRig.Begin(rig, pos);
            }
            if (!tracking || blockGesture) return;
            if (!ended) rig = FlightCameraRig.Drag(rig, pos);
            else
            {
                rig = FlightCameraRig.End(rig, pos);
                tracking = false;
            }
            RefreshFollow();
        }

        void Pinch(Vector2 a, Vector2 b)
        {
            float dist = Vector2.Distance(a, b);
            if (dist < 12f) return;
            if (!pinching) { pinching = true; pinchDist = dist; return; }
            if (pinchDist > 1f) rig = FlightCameraRig.Pinch(rig, dist / pinchDist);
            pinchDist = dist;
            RefreshFollow();
        }

        bool OverControl(Vector2 screen)
        {
            if (hud.panel == null) return false;
            var hit = hud.panel.Pick(RuntimePanelUtils.ScreenToPanel(hud.panel, screen));
            for (var ve = hit; ve != null; ve = ve.parent)
                if (ve is Button) return true;
            return false;
        }

        void ToggleMusic()
        {
            Kestrel.Game.Audio.AudioPrefs.SetMusicMuted(!Kestrel.Game.Audio.AudioPrefs.MusicMuted);
            RefreshAudio();
        }

        void ToggleSfx()
        {
            Kestrel.Game.Audio.AudioPrefs.SetSfxMuted(!Kestrel.Game.Audio.AudioPrefs.SfxMuted);
            RefreshAudio();
        }

        void ToggleNarration()
        {
            Kestrel.Game.Audio.AudioPrefs.SetNarrationMuted(!Kestrel.Game.Audio.AudioPrefs.NarrationMuted);
            RefreshAudio();
        }

        void RefreshAudio()
        {
            if (music != null) Label(music, "Music", Kestrel.Game.Audio.AudioPrefs.MusicMuted);
            if (sfx != null) Label(sfx, "Sound", Kestrel.Game.Audio.AudioPrefs.SfxMuted);
            if (narration != null) Label(narration, "Narration", Kestrel.Game.Audio.AudioPrefs.NarrationMuted);
        }

        static void Label(Button button, string name, bool muted)
        {
            button.text = Kestrel.Game.Audio.AudioPrefs.Caption(name, muted);
        }

        void CycleCamera()
        {
            viewMode = FlightView.Next(viewMode);
            RefreshCamera();
            viewChanged?.Invoke(viewMode);
        }

        void RefreshCamera()
        {
            if (cameraBtn == null) return;
            string name = FlightView.Name(viewMode);
            cameraBtn.text = name;
            cameraBtn.tooltip = "Camera: " + name + ". Tap for the next view.";
            float upp = lastUpp > 0.5f ? lastUpp : 1f;
            float w = FlightView.ButtonWidth(viewMode) * upp;
            cameraBtn.style.minWidth = w;
            cameraBtn.style.width = w;
            FitTool(cameraBtn, upp, FlightView.ButtonFontPt);
        }

        void RefreshFollow()
        {
            if (follow == null) return;
            follow.text = "Follow rocket";
            follow.tooltip = rig.Follow
                ? "Camera is following the rocket. Tap to recenter."
                : "Follow the rocket. Recenters the camera.";
            follow.style.backgroundColor = rig.Follow
                ? new Color(1f, 1f, 1f, 0.12f)
                : new Color(1f, 0.69f, 0f, 0.92f);
            follow.style.color = rig.Follow ? Color.white : new Color(0.08f, 0.08f, 0.1f);
        }

        static Button Tool(string name, string text, string tip)
        {
            var b = new Button { name = name, text = text, tooltip = tip };
            b.style.color = Color.white;
            b.style.backgroundColor = new Color(0.03f, 0.05f, 0.1f, 0.82f);
            b.style.borderTopWidth = 0;
            b.style.borderRightWidth = 0;
            b.style.borderBottomWidth = 0;
            b.style.borderLeftWidth = 0;
            b.style.unityFontStyleAndWeight = FontStyle.Bold;
            return b;
        }

        static void FitTool(Button b, float upp, float fontPt = 13f)
        {
            if (b == null) return;
            b.style.fontSize = fontPt * upp;
            b.style.whiteSpace = WhiteSpace.NoWrap;
            b.style.overflow = Overflow.Visible;
            b.style.unityTextAlign = TextAnchor.MiddleCenter;
            b.style.paddingLeft = 2f * upp;
            b.style.paddingRight = 2f * upp;
            b.style.paddingTop = 0f;
            b.style.paddingBottom = 0f;
            b.style.flexShrink = 0f;
        }

        static void StyleTool(Button b, float upp, float height, float widthPt)
        {
            if (b == null) return;
            b.style.minHeight = height;
            b.style.height = height;
            b.style.minWidth = widthPt * upp;
            b.style.width = widthPt * upp;
            b.style.fontSize = 16f * upp;
            float r = 12f * upp;
            b.style.borderTopLeftRadius = r;
            b.style.borderTopRightRadius = r;
            b.style.borderBottomLeftRadius = r;
            b.style.borderBottomRightRadius = r;
        }

        static Button MenuButton(string name, string text, string tip, Action click)
        {
            var b = new Button(click) { name = name, text = text, tooltip = tip };
            b.style.color = Color.white;
            b.style.backgroundColor = new Color(0.12f, 0.16f, 0.24f, 0.95f);
            b.style.unityFontStyleAndWeight = FontStyle.Bold;
            b.style.borderTopWidth = 0;
            b.style.borderRightWidth = 0;
            b.style.borderBottomWidth = 0;
            b.style.borderLeftWidth = 0;
            return b;
        }
    }
}
