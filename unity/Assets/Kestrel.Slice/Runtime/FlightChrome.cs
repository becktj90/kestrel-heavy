// In-flight pause and the one-thumb camera controls. The chase camera stays the follow pose;
// drag and pinch only orbit that offset, and a tap or the Follow button hands it back.
// The "Cam: Auto" button opens a five-chip picker (Auto / Above / Below / Chase / Pad). A chip applies,
// saves and closes; a tap outside or Esc closes with no change. C on a keyboard cycles with a 1 s toast.
// None of it pauses the flight. AscentHud places the picker and the toast (FlightHudLayout.CameraPicker).
using Kestrel.Core.Ui;
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
        public const string PickerName = UiIds.FlightCameraPicker;
        public const string ToastName = UiIds.FlightCameraToast;
        public static string ChipName(FlightViewMode mode) => UiIds.FlightCameraChip(mode);

        static readonly Color Amber = new Color(1f, 0.69f, 0f, 0.92f);
        static readonly Color Ink = new Color(0.08f, 0.08f, 0.1f);
        static readonly Color Panel = new Color(0.03f, 0.05f, 0.1f, 0.9f);

        VisualElement hud, menu, tools, picker;
        Label toast;
        string viewNote;
        float toastLeft;
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
        public bool PickerOpen => picker != null && picker.style.display.value == DisplayStyle.Flex;
        public string ToastText => toast != null && toast.style.display.value == DisplayStyle.Flex ? toast.text : null;

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

            follow = Tool(UiIds.FlightFollow, FlightView.FollowLabel, FlightView.FollowTip(false));
            follow.clicked += FollowRocket;
            cameraBtn = Tool(UiIds.FlightCamera, FlightView.Caption(viewMode), FlightView.Tip(viewMode));
            cameraBtn.clicked += TogglePicker;
            pause = Tool(UiIds.FlightPause, "Pause", "Pause. Freezes the flight.");
            pause.clicked += () => setPaused?.Invoke(true);
            tools.Add(follow);
            tools.Add(cameraBtn);
            tools.Add(pause);

            var webcast = hud.Q("webcast");
            if (webcast != null) hud.Insert(hud.IndexOf(webcast), tools);
            else hud.Add(tools);

            BuildPicker();

            menu = new VisualElement { name = UiIds.ScreenPause };
            menu.AddToClassList("hidden");
            // Hidden inline too, not only through the USS .hidden rule (a device test panel has no Slice.uss):
            // a closed menu is display:none and ignores picking, so it never takes a tap meant for the camera
            // picker. It stays the top layer so an OPEN menu covers the picker (and Show(true) closes it).
            menu.style.display = DisplayStyle.None;
            menu.pickingMode = PickingMode.Ignore;
            menu.style.position = Position.Absolute;
            menu.style.left = 0;
            menu.style.right = 0;
            menu.style.top = 0;
            menu.style.bottom = 0;
            menu.style.alignItems = Align.Center;
            menu.style.justifyContent = Justify.Center;
            // L3: a 0.78 scrim let the dock prompt read through the menu. Near-opaque, static (no fade, no flash).
            menu.style.backgroundColor = PauseScrim;
            var title = new Label { name = "pause-title", text = "Paused" };
            title.style.color = new Color(1f, 0.69f, 0f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 12;
            title.pickingMode = PickingMode.Ignore;
            menu.Add(title);
            menu.Add(MenuButton(UiIds.PauseResume, "Resume", "Resume the flight.", () => setPaused?.Invoke(false)));
            menu.Add(MenuButton(UiIds.PauseRestart, "Restart", "Restart. Flies the same design again.", () => restart?.Invoke()));
            menu.Add(MenuButton(UiIds.PauseExitHome, "Exit to Home", "Exit to Home.", () => exitHome?.Invoke()));
            music = MenuButton(UiIds.PauseMusic, "Music on", "Music. Tap to mute or unmute.", ToggleMusic);
            sfx = MenuButton(UiIds.PauseSfx, "Sound on", "Sound effects. Tap to mute or unmute.", ToggleSfx);
            narration = MenuButton(UiIds.PauseNarration, "Narration on", "Narration. Tap to mute or unmute.", ToggleNarration);
            menu.Add(music);
            menu.Add(sfx);
            menu.Add(narration);
            BuildPauseCard();
            hud.Add(menu);
            RefreshFollow();
            RefreshCamera();
            RefreshAudio();
            hud.RegisterCallback<GeometryChangedEvent>(_ => hud.schedule.Execute(() => Kestrel.Game.Shell.TextFit.Apply(menu)));
            // The padded content width is only known after layout (and changes with the safe pads): refit the row.
            hud.RegisterCallback<GeometryChangedEvent>(_ => { if (follow != null && !Pinned(follow)) StyleTools(hud, follow, cameraBtn, pause, lastUpp); });
        }

        public void Layout(float unitsPerPoint)
        {
            float upp = unitsPerPoint >= 0.5f ? unitsPerPoint : 1f;
            lastUpp = upp;
            float h = TouchPt * upp;
            StyleTools(hud, follow, cameraBtn, pause, upp);
            StylePicker(upp);
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
            ArrangePauseCard(upp);
            Kestrel.Game.Shell.TextFit.Apply(menu);
        }

        /// <summary>Pause backdrop: near-opaque ink. Static colour, no transition, so it is photosensitivity-safe.</summary>
        public static readonly Color PauseScrim = new Color(0.02f, 0.04f, 0.08f, 0.96f);
        public static readonly Color PauseCardInk = new Color(0.06f, 0.09f, 0.16f, 1f);
        public const string PauseCardName = "pause-card";
        public const float PauseCardWidthPt = 280f;

        /// <summary>
        /// The title and the six menu buttons sit on one opaque card (L3: "Narration on" landed on the dock's
        /// Auto-assist and the prompt showed through). Button names are unchanged (UiIds.Pause*).
        /// </summary>
        void BuildPauseCard()
        {
            if (menu == null || menu.Q(PauseCardName) != null) return;
            var card = new VisualElement { name = PauseCardName, pickingMode = PickingMode.Position };
            card.style.backgroundColor = PauseCardInk;
            card.style.flexDirection = FlexDirection.Column;
            card.style.alignItems = Align.Stretch;
            card.style.flexShrink = 1;
            card.style.maxWidth = Length.Percent(100);
            card.style.maxHeight = Length.Percent(100);
            card.style.overflow = Overflow.Hidden;
            var kids = new System.Collections.Generic.List<VisualElement>(menu.Children());
            foreach (var k in kids) { k.RemoveFromHierarchy(); card.Add(k); }
            menu.Add(card);
        }

        /// <summary>
        /// One column when it fits the HUD height, else two columns of three (a 1280x720 web panel or a short
        /// landscape phone), so every button stays a 44 pt target with no overlap and no scrolling.
        /// </summary>
        void ArrangePauseCard(float upp)
        {
            var card = menu != null ? menu.Q(PauseCardName) : null;
            if (card == null) return;
            float pad = 16f * upp;
            card.style.paddingLeft = pad; card.style.paddingRight = pad;
            card.style.paddingTop = 12f * upp; card.style.paddingBottom = pad;
            float r = 14f * upp;
            card.style.borderTopLeftRadius = r; card.style.borderTopRightRadius = r;
            card.style.borderBottomLeftRadius = r; card.style.borderBottomRightRadius = r;
            var buttons = card.Query<Button>().ToList();
            float panelH = hud != null ? hud.resolvedStyle.height : 0f;
            // #332: the card is centred on the panel, so its room is the panel less the deeper side pad on both
            // sides (it was the unpadded panel width, which let a wide card run under a notch or a 24 px pad).
            float panelW = hud != null ? hud.resolvedStyle.width : 0f;
            if (hud != null && panelW > 8f)
            {
                float pl = hud.resolvedStyle.paddingLeft, pr = hud.resolvedStyle.paddingRight;
                float side = Mathf.Max(float.IsNaN(pl) ? 0f : pl, float.IsNaN(pr) ? 0f : pr);
                panelW = Mathf.Floor(panelW - 2f * Mathf.Ceil(side - 0.001f) + 0.001f);
            }
            float row = (TouchPt + 8f) * upp;
            float oneCol = (22f * 1.4f + 12f) * upp + buttons.Count * row + 12f * upp + pad;
            bool two = panelH > 8f && oneCol > panelH - 16f * upp && panelW > 2f * PauseCardWidthPt * upp * 0.8f;
            float w = PauseCardWidthPt * upp * (two ? 2f : 1f);
            if (panelW > 8f && w > panelW - 16f * upp) w = panelW - 16f * upp;
            card.style.width = w;
            card.style.flexDirection = two ? FlexDirection.Row : FlexDirection.Column;
            card.style.flexWrap = two ? Wrap.Wrap : Wrap.NoWrap;
            var title = card.Q<Label>("pause-title");
            if (title != null)
            {
                title.style.width = Length.Percent(100);
                title.style.unityTextAlign = TextAnchor.MiddleCenter;
            }
            float gap = 8f * upp;
            for (int i = 0; i < buttons.Count; i++)
            {
                var b = buttons[i];
                b.style.flexGrow = 0;
                b.style.flexShrink = 0;
                b.style.marginLeft = 0;
                b.style.marginRight = two && i % 2 == 0 ? gap : 0;
                b.style.width = two ? (w - 2f * pad - gap) * 0.5f : StyleKeyword.Auto;
                if (!two) b.style.alignSelf = Align.Stretch;
            }
        }

        public void Show(bool paused)
        {
            Paused = paused;
            if (paused) { tracking = false; pinching = false; ClosePicker(); HideToast(); }
            if (menu != null)
            {
                menu.EnableInClassList("hidden", !paused);
                menu.style.display = paused ? DisplayStyle.Flex : DisplayStyle.None;
                // Open: the full-screen menu is the top layer and takes every tap. Closed: never pickable.
                menu.pickingMode = paused ? PickingMode.Position : PickingMode.Ignore;
                if (paused) menu.BringToFront();
            }
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
            TickToast(Time.unscaledDeltaTime);
            if (PickerOpen && Input.GetKeyDown(KeyCode.Escape)) ClosePicker();
            else if (Input.GetKeyDown(KeyCode.C)) CycleWithToast();
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
            if (began && PickerOpen && !OverPickerOrButton(pos))
            {
                // A tap outside closes the picker with no change, and does not start an orbit drag.
                ClosePicker();
                blockGesture = true;
                tracking = false;
                return;
            }
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

        bool OverPickerOrButton(Vector2 screen)
        {
            if (hud.panel == null) return false;
            var hit = hud.panel.Pick(RuntimePanelUtils.ScreenToPanel(hud.panel, screen));
            for (var ve = hit; ve != null; ve = ve.parent)
                if (ve == picker || ve == cameraBtn) return true;
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

        /// <summary>The old tap behaviour, kept for C and for callers that want the next view.</summary>
        public void CycleCamera() => Choose(FlightView.Next(viewMode));

        /// <summary>C on a keyboard: next view, no picker, a 1 s toast.</summary>
        public void CycleWithToast()
        {
            if (Paused) return;
            ClosePicker();
            Choose(FlightView.Next(viewMode));
            ShowToast(FlightView.Toast(viewMode));
        }

        /// <summary>Apply, save (through the bound callback) and close the picker.</summary>
        public void Choose(FlightViewMode mode)
        {
            viewMode = mode;
            if (FlightView.Chases(mode)) lastChase = mode;
            RefreshCamera();
            RefreshFollow();
            ClosePicker();
            viewChanged?.Invoke(viewMode);
        }

        public void TogglePicker()
        {
            if (PickerOpen) ClosePicker();
            else OpenPicker();
        }

        public void OpenPicker()
        {
            if (picker == null || Paused) return;
            HideToast();
            RefreshChips();
            picker.style.display = DisplayStyle.Flex;
            Restack(hud);
            Kestrel.Game.Shell.TextFit.Apply(picker);
        }

        public void ClosePicker()
        {
            if (picker != null) picker.style.display = DisplayStyle.None;
            RefreshNote();
        }

        /// <summary>Pad at long range: a short note in the toast slot while the picker is shut.</summary>
        public void SetViewNote(string note)
        {
            if (note == viewNote) return;
            viewNote = note;
            RefreshNote();
        }

        void ShowToast(string text)
        {
            if (toast == null) return;
            toast.text = text;
            toast.style.display = DisplayStyle.Flex;
            toast.style.opacity = 1f;
            toastLeft = FlightView.ToastSec;
        }

        void HideToast()
        {
            toastLeft = 0f;
            RefreshNote();
        }

        /// <summary>Unscaled seconds. No blink: the toast stays steady, then gives way to the note or nothing.</summary>
        public void TickToast(float dt)
        {
            if (toastLeft <= 0f) return;
            toastLeft -= dt;
            if (toastLeft <= 0f) HideToast();
        }

        void RefreshNote()
        {
            if (toast == null || toastLeft > 0f) return;
            bool show = !string.IsNullOrEmpty(viewNote) && !PickerOpen;
            toast.text = show ? viewNote : "";
            toast.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            toast.style.opacity = 0.85f;
        }

        void BuildPicker()
        {
            picker = new VisualElement { name = PickerName };
            picker.style.position = Position.Absolute;
            picker.style.display = DisplayStyle.None;
            picker.style.backgroundColor = Panel;
            picker.style.flexDirection = FlexDirection.Row;
            picker.style.flexWrap = Wrap.Wrap;
            // Until AscentHud seats it: above the tools row, centred.
            picker.style.left = Length.Percent(20);
            picker.style.right = Length.Percent(20);
            picker.style.bottom = 200;
            for (int i = 0; i < FlightView.Picker.Length; i++)
            {
                var mode = FlightView.Picker[i];
                var chip = new Button(() => Choose(mode))
                {
                    name = ChipName(mode),
                    text = FlightView.Name(mode),
                    tooltip = "Camera: " + FlightView.Name(mode) + ".",
                };
                chip.AddToClassList("flight-camera-chip");
                chip.style.unityFontStyleAndWeight = FontStyle.Bold;
                chip.style.borderTopWidth = 0;
                chip.style.borderRightWidth = 0;
                chip.style.borderBottomWidth = 0;
                chip.style.borderLeftWidth = 0;
                picker.Add(chip);
            }
            hud.Add(picker);

            toast = new Label { name = ToastName, text = "" };
            toast.pickingMode = PickingMode.Ignore;
            toast.style.position = Position.Absolute;
            toast.style.display = DisplayStyle.None;
            toast.style.color = Color.white;
            toast.style.backgroundColor = Panel;
            toast.style.unityFontStyleAndWeight = FontStyle.Bold;
            toast.style.unityTextAlign = TextAnchor.MiddleCenter;
            toast.style.whiteSpace = WhiteSpace.Normal;
            toast.style.left = Length.Percent(20);
            toast.style.right = Length.Percent(20);
            toast.style.bottom = 200;
            hud.Add(toast);
            RefreshChips();
        }

        void StylePicker(float upp)
        {
            if (picker == null) return;
            float r = 12f * upp;
            picker.style.borderTopLeftRadius = r;
            picker.style.borderTopRightRadius = r;
            picker.style.borderBottomLeftRadius = r;
            picker.style.borderBottomRightRadius = r;
            picker.style.paddingLeft = FlightHudLayout.PadPt * upp;
            picker.style.paddingRight = FlightHudLayout.PadPt * upp;
            picker.style.paddingTop = FlightHudLayout.PadPt * upp;
            picker.style.paddingBottom = FlightHudLayout.PadPt * upp;
            picker.Query<Button>(className: "flight-camera-chip").ForEach(chip =>
            {
                chip.style.minHeight = TouchPt * upp;
                chip.style.height = TouchPt * upp;
                chip.style.minWidth = FlightView.WidestChip() * upp;
                chip.style.marginLeft = 0;
                chip.style.marginTop = 0;
                chip.style.marginRight = FlightHudLayout.GapPt * upp;
                chip.style.marginBottom = FlightHudLayout.GapPt * upp;
                chip.style.borderTopLeftRadius = r;
                chip.style.borderTopRightRadius = r;
                chip.style.borderBottomLeftRadius = r;
                chip.style.borderBottomRightRadius = r;
                FitTool(chip, upp, FlightView.ChipFontPt);
            });
            if (toast != null)
            {
                toast.style.fontSize = Kestrel.Game.Shell.TextFit.Cap(toast, FlightView.ChipFontPt * upp);
                toast.style.borderTopLeftRadius = r;
                toast.style.borderTopRightRadius = r;
                toast.style.borderBottomLeftRadius = r;
                toast.style.borderBottomRightRadius = r;
            }
        }

        void RefreshChips()
        {
            if (picker == null) return;
            for (int i = 0; i < FlightView.Picker.Length; i++)
            {
                var mode = FlightView.Picker[i];
                var chip = picker.Q<Button>(ChipName(mode));
                if (chip == null) continue;
                bool on = mode == viewMode;
                chip.style.backgroundColor = on ? Amber : new Color(1f, 1f, 1f, 0.12f);
                chip.style.color = on ? Ink : Color.white;
                chip.tooltip = "Camera: " + FlightView.Name(mode) + (on ? " (current)." : ".");
            }
        }

        /// <summary>
        /// Seat the picker, its chips and the toast at panel-unit boxes from <see cref="FlightHudLayout"/>.
        /// AscentHud calls this for portrait and landscape; the boxes are relative to the HUD.
        /// </summary>
        public static void SeatPicker(VisualElement flightHud, HudBox pickerBox, HudBox[] chipBoxes)
        {
            if (flightHud == null || !pickerBox.Live) return;
            var p = flightHud.Q(PickerName);
            if (p != null)
            {
                p.style.left = pickerBox.X;
                p.style.top = pickerBox.Y;
                p.style.width = pickerBox.W;
                p.style.height = pickerBox.H;
                p.style.right = StyleKeyword.Auto;
                p.style.bottom = StyleKeyword.Auto;
                p.style.paddingLeft = 0; p.style.paddingRight = 0; p.style.paddingTop = 0; p.style.paddingBottom = 0;
                for (int i = 0; i < FlightView.Picker.Length && chipBoxes != null && i < chipBoxes.Length; i++)
                {
                    var chip = p.Q<Button>(ChipName(FlightView.Picker[i]));
                    var b = chipBoxes[i];
                    if (chip == null || !b.Live) continue;
                    chip.style.position = Position.Absolute;
                    chip.style.left = b.X - pickerBox.X;
                    chip.style.top = b.Y - pickerBox.Y;
                    chip.style.width = b.W;
                    chip.style.minWidth = b.W;
                    chip.style.height = b.H;
                    chip.style.minHeight = b.H;
                    chip.style.marginLeft = 0; chip.style.marginRight = 0; chip.style.marginTop = 0; chip.style.marginBottom = 0;
                }
            }
            var t = flightHud.Q(ToastName);
            if (t != null)
            {
                // The toast takes the bottom row of the picker's box: clear of the HUD by the same tests.
                float h = chipBoxes != null && chipBoxes.Length > 0 && chipBoxes[0].Live ? chipBoxes[0].H : pickerBox.H;
                t.style.left = pickerBox.X;
                t.style.top = pickerBox.Bottom - h;
                t.style.width = pickerBox.W;
                t.style.height = h;
                t.style.right = StyleKeyword.Auto;
                t.style.bottom = StyleKeyword.Auto;
            }
            Restack(flightHud);
        }

        /// <summary>
        /// Layer order at the end of the HUD: everything else, then the picker, the toast (never pickable), and
        /// the pause menu last. The picker is above the HUD and the tools row; an open pause menu covers it.
        /// AscentHud calls this after any pass that adds or brings HUD pieces to the front.
        /// </summary>
        public static void Restack(VisualElement flightHud)
        {
            if (flightHud == null) return;
            flightHud.Q(PickerName)?.BringToFront();
            flightHud.Q(ToastName)?.BringToFront();
            flightHud.Q(UiIds.ScreenPause)?.BringToFront();
        }

        void RefreshCamera()
        {
            if (cameraBtn == null) return;
            // The slot (and the caption set) comes from StyleTools' padded-row fit, not a fixed widest width.
            SetToolCaption(cameraBtn, FlightView.Caption(viewMode));
            cameraBtn.tooltip = FlightView.Tip(viewMode);
            RefreshChips();
        }

        /// <summary>The camera is chasing the rocket (no orbit, not the fixed Pad view).</summary>
        public bool Following => FlightView.Following(rig.Follow, viewMode);

        /// <summary>
        /// Follow: clear the manual orbit (the camera glides back, see SliceApp) and, from the fixed Pad view,
        /// return to Auto with the usual toast. While already following it changes nothing, and says so.
        /// </summary>
        public void FollowRocket()
        {
            if (Paused) return;
            // A toggle (#332): following, a tap lets go to the ground camera; otherwise it recentres and chases again.
            bool was = Following;
            if (!was && !rig.Follow && FlightView.Chases(viewMode))
            {
                // Orbited off a chasing view: Follow recentres it, and that is the visible change.
                rig = FlightCameraRig.Recenter(rig);
                RefreshFollow();
                return;
            }
            rig = FlightCameraRig.Recenter(rig);
            var next = FlightView.FollowToggle(viewMode, was, lastChase);
            if (next != viewMode)
            {
                Choose(next);
                ShowToast(FlightView.Toast(next));
            }
            RefreshFollow();
        }

        /// <summary>The last view that rides with the rocket, for Follow to come back to from Pad.</summary>
        FlightViewMode lastChase = FlightViewMode.Auto;

        void RefreshFollow()
        {
            if (follow == null) return;
            bool on = Following;
            SetToolCaption(follow, FlightView.FollowCaption(on));
            follow.tooltip = FlightView.FollowTip(on);
            // #332: Following was white at 12% under white text (about 1.1:1 on a pale sky). Now the dusk ink plate,
            // cream text and a teal edge (a state); Follow rocket stays solid amber with dark text (an action).
            // Kestrel.Core.Tests CountdownStackTests checks both at 4.5:1 or better over every backdrop.
            var plate = on ? CountdownStack.FollowingPlate : CountdownStack.FollowPlate;
            var ink = on ? CountdownStack.FollowingText : CountdownStack.FollowText;
            follow.style.backgroundColor = new Color(plate.R, plate.G, plate.B, plate.A);
            follow.style.color = new Color(ink.R, ink.G, ink.B, ink.A);
            float edge = on ? Mathf.Max(1f, Mathf.Round(CountdownStack.FollowEdgePt * (lastUpp > 0.5f ? lastUpp : 1f))) : 0f;
            var teal = new Color(CountdownStack.FollowingEdge.R, CountdownStack.FollowingEdge.G, CountdownStack.FollowingEdge.B, 1f);
            follow.style.borderTopWidth = edge; follow.style.borderBottomWidth = edge;
            follow.style.borderLeftWidth = edge; follow.style.borderRightWidth = edge;
            follow.style.borderTopColor = teal; follow.style.borderBottomColor = teal;
            follow.style.borderLeftColor = teal; follow.style.borderRightColor = teal;
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

        /// <summary>
        /// Size and dress the Follow / Cam / Pause row for the flex (portrait) layout. AscentHud calls this
        /// after it unpins the landscape boxes, so the row gets its own sizes back.
        /// </summary>
        public static void StyleTools(VisualElement flightHud, float unitsPerPoint)
        {
            if (flightHud == null) return;
            float upp = unitsPerPoint >= 0.5f ? unitsPerPoint : 1f;
            StyleTools(flightHud, flightHud.Q<Button>(UiIds.FlightFollow), flightHud.Q<Button>(UiIds.FlightCamera), flightHud.Q<Button>(UiIds.FlightPause), upp);
        }

        /// <summary>The control row uses the short caption set (FlightHudLayout.ToolSlots could not fit the full one).</summary>
        public const string ShortToolsClass = "kh-tools-short";

        /// <summary>
        /// The HUD's padded content box in whole panel pixels, rounded inward (left up, right down). Returns the
        /// whole-pixel width and <paramref name="leftPx"/> its left edge; <paramref name="inL"/> / <paramref name="inR"/> are what the row adds on each side of
        /// the padding edge to land on those pixels. NaN before the first layout pass.
        /// </summary>
        static float PaddedRowPx(VisualElement flightHud, out float inL, out float inR, out float leftPx)
        {
            inL = 0f; inR = 0f; leftPx = 0f;
            if (flightHud == null) return float.NaN;
            float w = flightHud.resolvedStyle.width;
            if (float.IsNaN(w) || w < 8f) return float.NaN;
            float pl = flightHud.resolvedStyle.paddingLeft, pr = flightHud.resolvedStyle.paddingRight;
            if (float.IsNaN(pl)) pl = 0f;
            if (float.IsNaN(pr)) pr = 0f;
            float l = Mathf.Ceil(pl - 0.001f), r = Mathf.Floor(w - pr + 0.001f);
            inL = Mathf.Max(0f, l - pl);
            leftPx = l;
            inR = Mathf.Max(0f, (w - pr) - r);
            return Mathf.Max(0f, r - l);
        }

        static void StyleTools(VisualElement flightHud, Button follow, Button cameraBtn, Button pause, float upp)
        {
            float h = TouchPt * upp;
            var tools = follow != null ? follow.parent : null;
            var fit = new FlightHudLayout.ToolRowFit
            {
                FollowW = FlightHudLayout.FollowWidthPt, CameraW = FlightView.WidestButton(), PauseW = FlightHudLayout.PauseWidthPt,
                FontPt = FlightHudLayout.ToolFontPt,
            };
            if (follow != null && !Pinned(follow) && tools != null && !Pinned(tools))
            {
                // #332 (335x720, 24 px pads): the row was three fixed slots (112 + widest Cam + 76 pt, no shrink)
                // plus Unity's 3 px button margins, and never looked at the safe pads, so Pause ran 13 px past the
                // padded content edge. Now the row sits in the HUD's padded content box rounded inward to whole
                // pixels, less the side margins, and FlightHudLayout.ToolSlots sizes the slots from that width.
                float side = Mathf.Round(FlightHudLayout.SideMarginPt * upp);
                float rowPx = PaddedRowPx(flightHud, out float inL, out float inR, out float leftPx);
                tools.style.marginLeft = inL + side;
                tools.style.marginRight = inR + side;
                if (!float.IsNaN(rowPx))
                {
                    // Points in the content box, as FlightHudLayout.Place lays the portrait frame out.
                    float pl = flightHud.resolvedStyle.paddingLeft, pr = flightHud.resolvedStyle.paddingRight;
                    if (float.IsNaN(pl)) pl = 0f;
                    if (float.IsNaN(pr)) pr = 0f;
                    float panelPt = (flightHud.resolvedStyle.width - pl - pr) / upp;
                    float leftPt = (leftPx - pl + side) / upp;
                    fit = FlightHudLayout.ToolSlots(panelPt, leftPt, leftPt + Mathf.Max(0f, rowPx - 2f * side) / upp);
                }
            }
            StyleTool(follow, upp, h, fit.FollowW);
            StyleTool(cameraBtn, upp, h, fit.CameraW);
            StyleTool(pause, upp, h, fit.PauseW);
            ShortCaptions(follow, fit.Short);
            ShortCaptions(cameraBtn, fit.Short);
            FitTool(follow, upp, fit.FontPt);
            FitTool(cameraBtn, upp, fit.FontPt);
            FitTool(pause, upp, fit.FontPt);
        }

        /// <summary>Show the full caption (kept in userData by SetToolCaption) or its short form.</summary>
        static void ShortCaptions(Button b, bool shortL)
        {
            if (b == null) return;
            b.EnableInClassList(ShortToolsClass, shortL);
            string full = b.userData as string ?? b.text;
            b.userData = full;
            b.text = FlightView.ShortToolCaption(full, shortL);
        }

        static void SetToolCaption(Button b, string full)
        {
            if (b == null) return;
            b.userData = full;
            b.text = FlightView.ShortToolCaption(full, ShortTools(b));
        }

        static bool ShortTools(Button b) => b != null && b.ClassListContains(ShortToolsClass);

        /// <summary>Landscape AscentHud pins the tools to exact panel boxes; their size is not ours to change then.</summary>
        static bool Pinned(VisualElement e) =>
            e.style.position.keyword == StyleKeyword.Undefined && e.style.position.value == Position.Absolute;

        static void StyleTool(Button b, float upp, float height, float widthPt)
        {
            if (b == null) return;
            if (!Pinned(b))
            {
                // Unpinned (flex row): our own size. A pinned button keeps AscentHud's box, so a later Layout
                // cannot push a point-sized min-width past the scaled box into its neighbour (1280x800 Cam/Pause).
                b.style.minHeight = height;
                b.style.height = height;
                // Whole panel pixels, rounded down: the three floors never sum past the row.
                float w = Mathf.Floor(widthPt * upp + 0.001f);
                b.style.minWidth = w;
                b.style.width = w;
                b.style.marginLeft = 0;
                b.style.marginRight = 0;
            }
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
