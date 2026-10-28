// Phase 1 "Starter" designer screen (PR 5). Mission header, a clear window onto the procedural rocket on
// the pad (SliceApp's HangarRocketView draws it behind this panel), the four controls, one hint line, one
// readiness line and an always-visible Launch, with Back top-left and Reset top-right. Every element is
// pinned to a Kestrel.Core.Designer.StarterLayout box, so the Core size tests and the EditMode device test
// check the same rectangles. No animation: state changes are instant (photosensitivity-safe).
// Designer tiers (docs/DESIGNER_TIERS_SPEC.md, step 2): a 44 pt "?" ends each row and opens a teal Kid Why? card under
// it (replacing the hint line), the "Speed to reach orbit" bar sits over the readiness line, an X-ray button in the
// rocket window opens StarterXrayView, and the Kid / Builder / Engineer switch shows once a later PR turns on
// designer.tierBuilder / designer.tierEngineer. All copy is data/designer-tiers.json (DesignerTierText); without it
// the screen is the PR 5 Starter screen. Badge and Launch logic are untouched.
using System;
using System.Collections.Generic;
using Kestrel.Core.Designer;
using Kestrel.Core.Features;
using Kestrel.Core.Flight;
using Kestrel.Core.Ui;
using Kestrel.Game.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public sealed class StarterPanel
    {
        public const string RootName = UiIds.ScreenStarter;

        static readonly Color SheetBg = new Color(8f / 255f, 12f / 255f, 20f / 255f, 0.88f);
        static readonly Color CornerBg = new Color(8f / 255f, 12f / 255f, 20f / 255f, 0.80f);
        static readonly Color Ink = new Color(0.95f, 0.95f, 0.95f);
        static readonly Color Muted = new Color(0.78f, 0.82f, 0.90f);
        static readonly Color Accent = new Color(1f, 176f / 255f, 0f);
        static readonly Color AccentInk = new Color(20f / 255f, 20f / 255f, 30f / 255f);
        static readonly Color OptionBg = new Color(1f, 1f, 1f, 0.10f);
        static readonly Color ReadyGreen = new Color(126f / 255f, 224f / 255f, 138f / 255f);
        static readonly Color TightAmber = new Color(1f, 196f / 255f, 92f / 255f);
        static readonly Color ShortRed = new Color(1f, 128f / 255f, 128f / 255f);
        static readonly Color LaunchRed = new Color(200f / 255f, 16f / 255f, 46f / 255f);
        static readonly Color LaunchOff = new Color(90f / 255f, 90f / 255f, 100f / 255f);
        /// <summary>Tier chip and "?" selected: amber #f5a524 with dark text (mockup).</summary>
        static readonly Color TierAmber = new Color(245f / 255f, 165f / 255f, 36f / 255f);
        static readonly Color WhyTeal = new Color(14f / 255f, 92f / 255f, 98f / 255f, 0.96f);
        static readonly Color TrackBg = new Color(1f, 1f, 1f, 0.16f);
        static readonly Color NeedInk = new Color(1f, 1f, 1f, 0.95f);

        sealed class Row
        {
            public VisualElement Box;
            public Label Label;
            public Button[] Options;
            public Button Help;
            public StarterKnob Knob;
        }

        public event Action LaunchRequested;
        public event Action BackRequested;
        /// <summary>The rocket window moved or resized (a Why? card opened or closed): the host re-frames its camera.</summary>
        public event Action RocketBoxChanged;
        /// <summary>The X-ray view opened (true) or closed (false).</summary>
        public event Action<bool> XrayToggled;

        /// <summary>The tier copy (data/designer-tiers.json). Null: no "?", speed bar, X-ray or switch.</summary>
        public DesignerTierText Copy { get; }
        /// <summary>The flags that offer the Builder / Engineer chips. Read on every Refresh.</summary>
        public Func<FeatureFlags> Flags { get; set; } = () => FeatureFlags.Active;
        /// <summary>Where the tier is stored (kh.settings.tier through ExperienceSettings). PlayerPrefs by default.</summary>
        public Func<string, bool> PrefHas { get; set; } = PlayerPrefs.HasKey;
        public Func<string, int> PrefGet { get; set; } = k => PlayerPrefs.GetInt(k, 0);
        public Action<string, int> PrefSet { get; set; } = (k, v) => { PlayerPrefs.SetInt(k, v); PlayerPrefs.Save(); };
        public ExperienceTier Tier { get; private set; } = ExperienceTier.Starter;
        public StarterKnob WhyOpen { get; private set; } = StarterKnob.None;
        public StarterSpeed Speed { get; private set; } = new StarterSpeed();
        public StarterXrayView Xray { get; private set; }
        public bool XrayOpen => Xray != null && Xray.Root.style.display.value != DisplayStyle.None;

        public StarterRocket Rocket { get; }
        public VisualElement Root { get; }
        public StarterFrame Frame { get; private set; }
        public StarterLayoutInput Input { get; private set; }
        public bool LaunchReady { get; private set; }
        public StarterKnob LastKnob { get; private set; } = StarterKnob.None;
        public bool Visible => Root.resolvedStyle.display != DisplayStyle.None && shown;

        readonly VisualElement rocketBox, header, sheet;
        readonly Label title, subtitle, hint, badge, readiness, fuelLess, fuelMore;
        readonly Button back, reset, launch;
        readonly Row engine, stages, fuel, boosters;
        readonly VisualElement whyCard, speedTrack, speedFill, speedNeed, tierSwitch;
        readonly StarterGlyph whyIcon;
        readonly Label whyText, speedLabel, speedStatus;
        readonly Button xrayButton;
        readonly Dictionary<ExperienceTier, Button> chips = new Dictionary<ExperienceTier, Button>();
        List<ExperienceTier> shownTiers = new List<ExperienceTier>();
        HudBox lastRocket;
        readonly VisualElement[] fuelDots = new VisualElement[StarterRocket.FuelNotches];
        float upp = 1f, wPt, hPt, safeT, safeB, safeL, safeR;
        bool wide, reserveExit, shown = true, laidOut;

        public StarterPanel(StarterRocket rocket, VisualElement parent, int insertAt = -1)
            : this(rocket, parent, insertAt, DesignerTiersBoot.Load()) { }

        public StarterPanel(StarterRocket rocket, VisualElement parent, int insertAt, DesignerTierText copy)
        {
            Rocket = rocket ?? throw new ArgumentNullException(nameof(rocket));
            Copy = copy;
            Root = new VisualElement { name = RootName, pickingMode = PickingMode.Ignore };
            Root.style.position = Position.Absolute;
            Root.style.left = 0f; Root.style.top = 0f; Root.style.right = 0f; Root.style.bottom = 0f;

            rocketBox = new VisualElement { name = UiIds.StarterRocket, pickingMode = PickingMode.Ignore };
            rocketBox.tooltip = "Your rocket on the pad.";
            Root.Add(rocketBox);

            header = new VisualElement { name = "starter-header", pickingMode = PickingMode.Ignore };
            title = Text(UiIds.StarterTitle, 20f, Ink, true, true);
            subtitle = Text(UiIds.StarterSubtitle, 14f, Muted, false, true);
            header.Add(title);
            header.Add(subtitle);

            sheet = new VisualElement { name = UiIds.StarterSheet };
            sheet.style.backgroundColor = SheetBg;
            Root.Add(sheet);

            engine = MakeRow(UiIds.StarterEngine, "engine", StarterScreen.EngineLabel, StarterRocket.EngineOrder.Count, k => UiIds.StarterEngineOption(StarterRocket.EngineOrder[k]), k => Pick(StarterKnob.Engine, () => Rocket.Engine = StarterRocket.EngineOrder[k]));
            stages = MakeRow(UiIds.StarterStages, "stages", StarterScreen.StagesLabel, StarterRocket.StageValues.Count, k => UiIds.StarterStagesOption(StarterRocket.StageValues[k]), k => Pick(StarterKnob.Stages, () => Rocket.Stages = StarterRocket.StageValues[k]));
            fuel = MakeRow(UiIds.StarterFuel, "fuel", StarterScreen.FuelLabel, StarterRocket.FuelNotches, k => UiIds.StarterFuelNotch(k + 1), k => Pick(StarterKnob.FuelNotch, () => Rocket.FuelNotch = k));
            boosters = MakeRow(UiIds.StarterBoosters, "boosters", StarterScreen.BoostersLabel, StarterRocket.BoosterValues.Count, k => UiIds.StarterBoostersOption(StarterRocket.BoosterValues[k]), k => Pick(StarterKnob.Boosters, () => Rocket.Boosters = StarterRocket.BoosterValues[k]));
            for (int i = 0; i < StarterRocket.EngineOrder.Count; i++) engine.Options[i].text = StarterScreen.OptionText(StarterRocket.EngineOrder[i]);
            for (int i = 0; i < StarterRocket.StageValues.Count; i++) stages.Options[i].text = StarterRocket.StageValues[i].ToString(System.Globalization.CultureInfo.InvariantCulture);
            for (int i = 0; i < StarterRocket.BoosterValues.Count; i++) boosters.Options[i].text = StarterScreen.BoosterText(StarterRocket.BoosterValues[i]);
            engine.Label.tooltip = StarterScreen.EngineHint;
            stages.Label.tooltip = StarterScreen.StagesHint;
            fuel.Label.tooltip = StarterScreen.FuelHint;
            boosters.Label.tooltip = StarterScreen.BoostersHint;
            for (int i = 0; i < StarterRocket.FuelNotches; i++)
            {
                var dot = new VisualElement { name = "starter-fuel-dot-" + (i + 1), pickingMode = PickingMode.Ignore };
                dot.style.position = Position.Absolute;
                fuelDots[i] = dot;
                fuel.Options[i].Add(dot);
            }
            fuelLess = Text("starter-label-less", 13f, Muted, false, false);
            fuelLess.text = StarterScreen.FuelLess;
            fuelMore = Text("starter-label-more", 13f, Muted, false, false);
            fuelMore.text = StarterScreen.FuelMore;
            fuelMore.style.unityTextAlign = TextAnchor.MiddleRight;
            fuel.Box.Add(fuelLess);
            fuel.Box.Add(fuelMore);
            engine.Knob = StarterKnob.Engine; stages.Knob = StarterKnob.Stages; fuel.Knob = StarterKnob.FuelNotch; boosters.Knob = StarterKnob.Boosters;
            foreach (var row in new[] { engine, stages, fuel, boosters })
            {
                var r = row;
                r.Help = new Button(() => ToggleWhy(r.Knob)) { name = UiIds.StarterWhy(r.Knob), text = Copy?.WhyButton ?? "?" };
                Style(r.Help, OptionBg, Ink, true);
                r.Help.tooltip = Copy?.WhySpokenFor(r.Label.text) ?? "";
                r.Help.style.display = Copy != null && Copy.WhyFor(r.Knob) != null ? DisplayStyle.Flex : DisplayStyle.None;
                r.Box.Add(r.Help);
            }

            // The open Why? card (one at a time), under its row.
            whyCard = new VisualElement { name = UiIds.StarterWhyCard, pickingMode = PickingMode.Ignore };
            whyCard.style.backgroundColor = WhyTeal;
            whyIcon = new StarterGlyph("") { name = UiIds.StarterWhyIcon };
            whyText = Text(UiIds.StarterWhyText, 14f, Color.white, false, true);
            whyCard.Add(whyIcon);
            whyCard.Add(whyText);
            whyCard.style.display = DisplayStyle.None;
            sheet.Add(whyCard);

            // The speed bar: label, Enough / Short, a track with the fill and the need marker.
            speedLabel = Text(UiIds.StarterSpeedLabel, 13f, Muted, true, false);
            speedStatus = Text(UiIds.StarterSpeedStatus, 13f, ReadyGreen, true, false);
            speedStatus.style.unityTextAlign = TextAnchor.MiddleRight;
            speedTrack = new VisualElement { name = UiIds.StarterSpeedTrack, pickingMode = PickingMode.Ignore };
            speedTrack.style.backgroundColor = TrackBg;
            speedFill = new VisualElement { name = UiIds.StarterSpeedFill, pickingMode = PickingMode.Ignore };
            speedFill.style.position = Position.Absolute;
            speedNeed = new VisualElement { name = UiIds.StarterSpeedNeed, pickingMode = PickingMode.Ignore };
            speedNeed.style.position = Position.Absolute;
            speedNeed.style.backgroundColor = NeedInk;
            speedTrack.Add(speedFill);
            speedTrack.Add(speedNeed);
            sheet.Add(speedLabel); sheet.Add(speedStatus); sheet.Add(speedTrack);

            hint = Text(UiIds.StarterHint, 13f, Muted, false, true);
            badge = Text(UiIds.StarterBadge, 16f, ReadyGreen, true, false);
            readiness = Text(UiIds.StarterReadiness, 16f, Ink, false, true);
            sheet.Add(hint);
            sheet.Add(badge);
            sheet.Add(readiness);

            launch = new Button(() => { if (LaunchReady) LaunchRequested?.Invoke(); }) { name = UiIds.StarterLaunch, text = StarterScreen.LaunchText };
            Style(launch, LaunchRed, Color.white, true);
            sheet.Add(launch);

            back = new Button(() => BackRequested?.Invoke()) { name = UiIds.StarterBack, text = StarterScreen.BackText, tooltip = StarterScreen.BackHint };
            Style(back, CornerBg, Color.white, true);
            reset = new Button(() => { LastKnob = StarterKnob.None; Rocket.Reset(); Refresh(); }) { name = UiIds.StarterReset, text = StarterScreen.ResetText, tooltip = StarterScreen.ResetHint };
            Style(reset, CornerBg, Color.white, true);
            Root.Add(back);
            Root.Add(reset);
            Root.Add(header); // re-parented into the sheet on landscape web

            // Kid / Builder / Engineer (only drawn when a later PR offers more than Kid).
            tierSwitch = new VisualElement { name = UiIds.StarterTierSwitch, pickingMode = PickingMode.Ignore };
            foreach (var t in DesignerTiers.Order)
            {
                var tier = t;
                var chip = new Button(() => ChooseTier(tier)) { name = UiIds.StarterTierChip(tier), text = Copy?.TierLabel(tier) ?? "" };
                Style(chip, OptionBg, Ink, true);
                chip.tooltip = (Copy?.TierLabel(tier) ?? "") + ". " + (Copy?.SwitchHint ?? "");
                chips[tier] = chip;
                tierSwitch.Add(chip);
            }
            tierSwitch.style.display = DisplayStyle.None;
            Root.Add(tierSwitch);

            xrayButton = new Button(OpenXray) { name = UiIds.StarterXrayButton, text = Copy?.XrayButton ?? "", tooltip = Copy?.XrayButtonHint ?? "" };
            Style(xrayButton, CornerBg, Color.white, true);
            xrayButton.style.borderLeftWidth = 1.5f; xrayButton.style.borderRightWidth = 1.5f; xrayButton.style.borderTopWidth = 1.5f; xrayButton.style.borderBottomWidth = 1.5f;
            xrayButton.style.borderLeftColor = Muted; xrayButton.style.borderRightColor = Muted; xrayButton.style.borderTopColor = Muted; xrayButton.style.borderBottomColor = Muted;
            xrayButton.style.display = DisplayStyle.None;
            Root.Add(xrayButton);

            Rocket.Changed += OnRocketChanged;
            if (parent != null)
            {
                if (insertAt >= 0 && insertAt <= parent.childCount) parent.Insert(insertAt, Root);
                else parent.Add(Root);
            }
            Refresh();
        }

        public void Detach()
        {
            Rocket.Changed -= OnRocketChanged;
            Root.RemoveFromHierarchy();
        }

        public void SetVisible(bool on)
        {
            shown = on;
            Root.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Top of the sheet in panel units (portrait), for the rocket framing above it. NaN before the first layout.</summary>
        public float SheetTopPanel => Frame == null ? float.NaN : Frame.Sheet.Y * upp;

        /// <summary>Lay out for a panel of <paramref name="widthPt"/> x <paramref name="heightPt"/> points; <paramref name="unitsPerPoint"/>
        /// converts points to panel units (1 on landscape web, where the 1280x720 panel's units are the type-cap points).</summary>
        public void Layout(float widthPt, float heightPt, float safeTop, float safeBottom, float safeLeft, float safeRight,
            float unitsPerPoint, bool wideWeb, bool reserveWebExit)
        {
            wPt = widthPt; hPt = heightPt; safeT = safeTop; safeB = safeBottom; safeL = safeLeft; safeR = safeRight;
            upp = unitsPerPoint > 0f ? unitsPerPoint : 1f;
            wide = wideWeb; reserveExit = reserveWebExit;
            laidOut = true;
            Relayout();
        }

        /// <summary>Selection, allowed options, hint and readiness from the rocket; re-places when a line count changes.</summary>
        public void Refresh()
        {
            var r = Rocket;
            Select(engine, k => r.Engine == StarterRocket.EngineOrder[k], k => r.IsEngineAllowed(StarterRocket.EngineOrder[k]));
            Select(stages, k => r.Stages == StarterRocket.StageValues[k], k => r.IsStagesAllowed(StarterRocket.StageValues[k]));
            Select(fuel, k => r.FuelNotch == k, k => r.IsFuelNotchAllowed(k));
            for (int k = 0; k < fuel.Options.Length; k++)
            {
                bool filled = k <= r.FuelNotch;
                fuelDots[k].style.backgroundColor = filled ? (k == r.FuelNotch ? AccentInk : Accent) : new Color(1f, 1f, 1f, 0.28f);
                fuel.Options[k].style.backgroundColor = k == r.FuelNotch ? Accent : OptionBg;
                fuel.Options[k].tooltip = StarterScreen.FuelSpoken(k);
            }
            fuel.Box.tooltip = StarterScreen.FuelSpoken(r.FuelNotch);
            Select(boosters, k => r.Boosters == StarterRocket.BoosterValues[k], k => r.IsBoostersAllowed(StarterRocket.BoosterValues[k]));
            boosters.Box.EnableInClassList("starter-locked", r.BoostersLocked);
            boosters.Label.style.color = r.BoostersLocked ? Muted : Ink;
            boosters.Label.tooltip = r.BoostersLocked ? StarterScreen.BoostersLockedHint : StarterScreen.BoostersHint;

            title.text = StarterScreen.Title(r.Mission);
            subtitle.text = StarterScreen.Subtitle(r.Mission);
            hint.text = StarterScreen.Hint(r, LastKnob);
            // The existing readout as plain information. Launch is off only on a hard Validate block, whose one
            // plain reason is the line; a rocket that looks short still launches (and may explode).
            LaunchReady = StarterScreen.Readiness(r, out var b, out var rest);
            badge.text = b;
            badge.style.color = b == StarterScreen.ReadyBadge ? ReadyGreen : b == StarterScreen.TightBadge ? TightAmber : ShortRed;
            readiness.text = rest;
            readiness.tooltip = StarterScreen.Spoken(b, rest);
            launch.SetEnabled(LaunchReady);
            launch.style.backgroundColor = LaunchReady ? LaunchRed : LaunchOff;
            launch.tooltip = LaunchReady ? "Launch this rocket." : rest;
            RefreshTiers();
            Relayout();
        }

        /// <summary>The tier extras: which chips show and which is on, the "?" states, the open card's text, the speed bar.</summary>
        void RefreshTiers()
        {
            var r = Rocket;
            var flags = Flags?.Invoke();
            shownTiers = Copy != null ? DesignerTiers.Shown(flags) : new List<ExperienceTier> { ExperienceTier.Starter };
            Tier = DesignerTiers.Resolve(flags, PrefHas, PrefGet);
            foreach (var kv in chips)
            {
                bool shown = shownTiers.Contains(kv.Key), on = kv.Key == Tier;
                kv.Value.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
                kv.Value.EnableInClassList("starter-on", on);
                kv.Value.style.backgroundColor = on ? TierAmber : OptionBg;
                kv.Value.style.color = on ? AccentInk : Ink;
            }
            if (Xray != null) Xray.Engineer = Tier == ExperienceTier.Engineer;

            if (WhyOpen == StarterKnob.Boosters && !r.BoostersVisible) WhyOpen = StarterKnob.None;
            foreach (var row in new[] { engine, stages, fuel, boosters })
            {
                bool on = row.Knob == WhyOpen;
                row.Help.EnableInClassList("starter-on", on);
                row.Help.style.backgroundColor = on ? TierAmber : OptionBg;
                row.Help.style.color = on ? AccentInk : Ink;
            }
            var w = Copy?.WhyFor(WhyOpen);
            whyText.text = Copy != null ? Copy.WhyText(WhyOpen, r) : "";
            whyText.tooltip = whyText.text;
            whyIcon.Kind = w?.Icon ?? "";

            Speed = StarterSpeedBar.Of(r, Copy);
            speedLabel.text = Speed.Label;
            speedStatus.text = Speed.Status;
            speedStatus.style.color = Speed.Enough ? ReadyGreen : ShortRed;
            speedFill.style.backgroundColor = Speed.Enough ? ReadyGreen : TightAmber;
            speedTrack.tooltip = Speed.Spoken;
            xrayButton.text = Copy?.XrayButton ?? "";
        }

        /// <summary>A "?" tap: opens that row's Why? card (closing any other), or closes it when it is already open.</summary>
        public void ToggleWhy(StarterKnob knob)
        {
            if (Copy == null || Copy.WhyFor(knob) == null) return;
            WhyOpen = WhyOpen == knob ? StarterKnob.None : knob;
            Refresh();
        }

        /// <summary>A chip tap: stores the tier (DesignerTiers.Choose); a chip that is not offered does nothing.</summary>
        public void ChooseTier(ExperienceTier t)
        {
            DesignerTiers.Choose(Flags?.Invoke(), t, PrefHas, PrefGet, PrefSet);
            Refresh();
        }

        /// <summary>Open the X-ray view on the rocket as it is now.</summary>
        public void OpenXray()
        {
            if (Copy == null) return;
            if (Xray == null)
            {
                Xray = new StarterXrayView(Copy, Root);
                Xray.Closed += CloseXray;
            }
            Xray.Engineer = Tier == ExperienceTier.Engineer;
            Covered(true);
            Xray.Root.style.display = DisplayStyle.Flex;
            Xray.Root.BringToFront();
            Xray.Show(StarterXray.Build(Rocket, Copy));
            if (laidOut) Xray.Layout(wPt, hPt, safeT, safeB, safeL, safeR, upp, reserveExit);
            XrayToggled?.Invoke(true);
        }

        public void CloseXray()
        {
            if (Xray == null) return;
            Xray.Root.style.display = DisplayStyle.None;
            Covered(false);
            XrayToggled?.Invoke(false);
        }

        /// <summary>While the X-ray is open the Starter screen under it is hidden (not drawn, not tappable, not read out).</summary>
        void Covered(bool on)
        {
            foreach (var child in Root.Children())
            {
                if (Xray != null && child == Xray.Root) continue;
                child.style.visibility = on ? Visibility.Hidden : StyleKeyword.Null;
            }
        }

        void OnRocketChanged(StarterRocket _) => Refresh();

        void Pick(StarterKnob knob, Action set)
        {
            LastKnob = knob;
            set();
            Refresh(); // an unchanged value raises no Changed, but the hint still follows the tap
        }

        void Relayout()
        {
            if (!laidOut) return;
            var r = Rocket;
            Input = new StarterLayoutInput
            {
                Width = wPt, Height = hPt, SafeTop = safeT, SafeBottom = safeB, SafeLeft = safeL, SafeRight = safeR,
                Wide = wide, ReserveExit = reserveExit, BoostersVisible = r.BoostersVisible,
                Title = title.text, Subtitle = subtitle.text, Hint = hint.text, Badge = badge.text, Readiness = readiness.text,
                Engines = Texts(engine), Stages = Texts(stages), Boosters = Texts(boosters),
                Help = Copy != null && Copy.Why.Count > 0, HelpText = Copy?.WhyButton ?? "?",
                WhyOpen = WhyOpen, WhyText = whyText.text,
                Speed = Speed.Visible, SpeedLabel = Speed.Label, SpeedStatus = Speed.Status,
                Tiers = shownTiers.Count > 1 && Copy != null ? shownTiers.ConvertAll(t => Copy.TierLabel(t)).ToArray() : null,
                Xray = Copy != null, XrayText = Copy?.XrayButton ?? "",
            };
            var f = StarterLayout.Place(Input);
            Frame = f;
            Apply(f);
            if (Xray != null) Xray.Layout(wPt, hPt, safeT, safeB, safeL, safeR, upp, reserveExit);
            if (lastRocket.X != f.Rocket.X || lastRocket.Y != f.Rocket.Y || lastRocket.W != f.Rocket.W || lastRocket.H != f.Rocket.H)
            {
                bool first = !lastRocket.Live;
                lastRocket = f.Rocket;
                if (!first) RocketBoxChanged?.Invoke();
            }
        }

        void Apply(StarterFrame f)
        {
            var zero = new HudBox();
            Pin(rocketBox, f.Rocket, zero);
            Pin(sheet, f.Sheet, zero);
            float rad = (f.Wide ? 18f : 18f) * upp;
            sheet.style.borderTopLeftRadius = rad; sheet.style.borderTopRightRadius = rad;
            sheet.style.borderBottomLeftRadius = f.Wide ? rad : 0f; sheet.style.borderBottomRightRadius = f.Wide ? rad : 0f;

            var headerParent = f.HeaderInSheet ? sheet : Root;
            if (header.parent != headerParent) headerParent.Add(header);
            Pin(header, f.Header, f.HeaderInSheet ? f.Sheet : zero);
            Pin(title, f.Title, f.Header); Font(title, f.TitlePt);
            subtitle.style.display = f.Subtitle.Live ? DisplayStyle.Flex : DisplayStyle.None;
            if (f.Subtitle.Live) { Pin(subtitle, f.Subtitle, f.Header); Font(subtitle, f.SubtitlePt); }

            Corner(back, f.Back, f.CornerPt);
            Corner(reset, f.Reset, f.CornerPt);

            PlaceRow(engine, f.Engine, f.Sheet, f.LabelPt, f.OptionPt);
            PlaceRow(stages, f.Stages, f.Sheet, f.LabelPt, f.OptionPt);
            PlaceRow(fuel, f.Fuel, f.Sheet, f.LabelPt, f.OptionPt);
            Pin(fuelLess, f.Fuel.Less, f.Fuel.Row); Font(fuelLess, f.EndPt);
            Pin(fuelMore, f.Fuel.More, f.Fuel.Row); Font(fuelMore, f.EndPt);
            for (int k = 0; k < fuelDots.Length && k < f.Fuel.Options.Length; k++)
            {
                float d = 14f * upp, ow = f.Fuel.Options[k].W * upp, oh = f.Fuel.Options[k].H * upp;
                var dot = fuelDots[k];
                dot.style.width = d; dot.style.height = d;
                dot.style.left = (ow - d) * 0.5f; dot.style.top = (oh - d) * 0.5f;
                dot.style.borderTopLeftRadius = d * 0.5f; dot.style.borderTopRightRadius = d * 0.5f;
                dot.style.borderBottomLeftRadius = d * 0.5f; dot.style.borderBottomRightRadius = d * 0.5f;
            }
            boosters.Box.style.display = f.Boosters != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (f.Boosters != null) PlaceRow(boosters, f.Boosters, f.Sheet, f.LabelPt, f.OptionPt);

            hint.style.display = f.Hint.Live ? DisplayStyle.Flex : DisplayStyle.None;
            if (f.Hint.Live) { Pin(hint, f.Hint, f.Sheet); Font(hint, f.HintPt); }

            // Tier switch.
            tierSwitch.style.display = f.Switch.Live ? DisplayStyle.Flex : DisplayStyle.None;
            if (f.Switch.Live)
            {
                Pin(tierSwitch, f.Switch, zero);
                Radius(tierSwitch, 12f);
                tierSwitch.style.overflow = Overflow.Hidden;
                for (int k = 0; k < shownTiers.Count && k < f.Chips.Length; k++)
                {
                    var chip = chips[shownTiers[k]];
                    Pin(chip, f.Chips[k], f.Switch);
                    Font(chip, f.ChipPt);
                    chip.style.paddingLeft = StarterLayout.ChipPadX * upp; chip.style.paddingRight = StarterLayout.ChipPadX * upp;
                }
            }

            // "?" buttons, the Why? card, the speed bar, the X-ray button.
            foreach (var row in new[] { engine, stages, fuel, boosters })
            {
                var rf = row == engine ? f.Engine : row == stages ? f.Stages : row == fuel ? f.Fuel : f.Boosters;
                bool on = rf != null && rf.Help.Live;
                row.Help.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
                if (!on) continue;
                Pin(row.Help, rf.Help, rf.Row);
                Font(row.Help, f.OptionPt);
                Radius(row.Help, 22f);
                row.Label.style.whiteSpace = rf.LabelWraps ? WhiteSpace.Normal : WhiteSpace.NoWrap;
            }
            whyCard.style.display = f.Why.Live ? DisplayStyle.Flex : DisplayStyle.None;
            if (f.Why.Live)
            {
                Pin(whyCard, f.Why, f.Sheet);
                Radius(whyCard, 12f);
                Pin(whyIcon, f.WhyIcon, f.Why);
                Pin(whyText, f.WhyText, f.Why);
                Font(whyText, f.WhyPt);
                whyIcon.MarkDirtyRepaint();
            }
            bool speedOn = f.SpeedBar.Live;
            speedLabel.style.display = speedOn ? DisplayStyle.Flex : DisplayStyle.None;
            speedStatus.style.display = speedOn && f.SpeedStatus.Live ? DisplayStyle.Flex : DisplayStyle.None;
            speedTrack.style.display = speedOn ? DisplayStyle.Flex : DisplayStyle.None;
            if (speedOn)
            {
                Pin(speedLabel, f.SpeedLabel, f.Sheet); Font(speedLabel, f.SpeedPt);
                if (f.SpeedStatus.Live) { Pin(speedStatus, f.SpeedStatus, f.Sheet); Font(speedStatus, f.SpeedPt); }
                Pin(speedTrack, f.SpeedBar, f.Sheet);
                Radius(speedTrack, StarterLayout.SpeedBarH * 0.5f);
                float tw = f.SpeedBar.W * upp, th = f.SpeedBar.H * upp;
                speedFill.style.left = 0f; speedFill.style.top = 0f;
                speedFill.style.width = tw * (float)Speed.Fill; speedFill.style.height = th;
                Radius(speedFill, StarterLayout.SpeedBarH * 0.5f);
                float mw = 2f * upp, mh = th + 6f * upp;
                speedNeed.style.width = mw; speedNeed.style.height = mh;
                speedNeed.style.left = Mathf.Clamp(tw * (float)Speed.NeedAt - mw * 0.5f, 0f, Mathf.Max(0f, tw - mw));
                speedNeed.style.top = -3f * upp;
            }
            xrayButton.style.display = f.Xray.Live ? DisplayStyle.Flex : DisplayStyle.None;
            if (f.Xray.Live) Corner(xrayButton, f.Xray, StarterLayout.XrayPt);
            badge.style.display = f.Badge.Live ? DisplayStyle.Flex : DisplayStyle.None;
            if (f.Badge.Live) { Pin(badge, f.Badge, f.Sheet); Font(badge, f.ReadinessPt); }
            Pin(readiness, f.Readiness, f.Sheet); Font(readiness, f.ReadinessPt);
            Pin(launch, f.Launch, f.Sheet); Font(launch, f.CtaPt);
            Radius(launch, 14f);
        }

        void PlaceRow(Row row, StarterRowFrame rf, HudBox sheetBox, float labelPt, float optionPt)
        {
            Pin(row.Box, rf.Row, sheetBox);
            Pin(row.Label, rf.Label, rf.Row);
            Font(row.Label, labelPt);
            for (int k = 0; k < row.Options.Length; k++)
            {
                var b = row.Options[k];
                if (k >= rf.Options.Length) { b.style.display = DisplayStyle.None; continue; }
                b.style.display = DisplayStyle.Flex;
                Pin(b, rf.Options[k], rf.Row);
                Font(b, optionPt);
                Radius(b, 10f);
                b.style.paddingLeft = StarterLayout.OptionPadX * upp; b.style.paddingRight = StarterLayout.OptionPadX * upp;
            }
        }

        void Corner(Button b, HudBox box, float pt)
        {
            Pin(b, box, new HudBox());
            Font(b, pt);
            Radius(b, 12f);
            b.style.paddingLeft = StarterLayout.ButtonPadX * upp; b.style.paddingRight = StarterLayout.ButtonPadX * upp;
        }

        Row MakeRow(string rowName, string key, string label, int n, Func<int, string> optionName, Action<int> pick)
        {
            var row = new Row { Box = new VisualElement { name = rowName, pickingMode = PickingMode.Ignore } };
            row.Label = Text("starter-label-" + key, 16f, Ink, true, false);
            row.Label.text = label;
            row.Label.style.unityTextAlign = TextAnchor.MiddleLeft;
            row.Box.Add(row.Label);
            row.Options = new Button[n];
            for (int k = 0; k < n; k++)
            {
                int at = k;
                var b = new Button(() => pick(at)) { name = optionName(k) };
                Style(b, OptionBg, Ink, true);
                row.Options[k] = b;
                row.Box.Add(b);
            }
            sheet.Add(row.Box);
            return row;
        }

        static void Select(Row row, Func<int, bool> selected, Func<int, bool> allowed)
        {
            for (int k = 0; k < row.Options.Length; k++)
            {
                var b = row.Options[k];
                bool on = selected(k), ok = allowed(k);
                // A value the mission does not offer (KH-10/KH-19's landing pair) is greyed and not tappable.
                b.SetEnabled(ok);
                b.EnableInClassList("starter-on", on);
                b.style.backgroundColor = on ? Accent : OptionBg;
                b.style.color = on ? AccentInk : Ink;
                b.style.opacity = ok ? 1f : 0.35f;
            }
        }

        static string[] Texts(Row row)
        {
            var o = new string[row.Options.Length];
            for (int k = 0; k < o.Length; k++) o[k] = row.Options[k].text ?? "";
            return o;
        }

        static Label Text(string name, float pt, Color color, bool bold, bool wraps)
        {
            var l = new Label { name = name, pickingMode = PickingMode.Ignore };
            l.style.color = color;
            l.style.fontSize = pt;
            l.style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
            l.style.whiteSpace = wraps ? WhiteSpace.Normal : WhiteSpace.NoWrap;
            l.style.marginLeft = 0f; l.style.marginRight = 0f; l.style.marginTop = 0f; l.style.marginBottom = 0f;
            l.style.paddingLeft = 0f; l.style.paddingRight = 0f; l.style.paddingTop = 0f; l.style.paddingBottom = 0f;
            l.style.unityTextAlign = wraps ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            return l;
        }

        static void Style(Button b, Color bg, Color ink, bool bold)
        {
            b.style.backgroundColor = bg;
            b.style.color = ink;
            b.style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
            b.style.whiteSpace = WhiteSpace.NoWrap;
            b.style.unityTextAlign = TextAnchor.MiddleCenter;
            b.style.borderLeftWidth = 0f; b.style.borderRightWidth = 0f; b.style.borderTopWidth = 0f; b.style.borderBottomWidth = 0f;
            b.style.marginLeft = 0f; b.style.marginRight = 0f; b.style.marginTop = 0f; b.style.marginBottom = 0f;
            b.style.paddingTop = 0f; b.style.paddingBottom = 0f;
        }

        void Radius(VisualElement e, float pt)
        {
            float r = pt * upp;
            e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r; e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r;
        }

        void Font(VisualElement e, float pt) => e.style.fontSize = pt * upp;

        /// <summary>Absolute box in points, relative to its parent's box, scaled to panel units.</summary>
        void Pin(VisualElement e, HudBox b, HudBox parentBox)
        {
            if (e == null) return;
            e.style.position = Position.Absolute;
            e.style.left = (b.X - parentBox.X) * upp;
            e.style.top = (b.Y - parentBox.Y) * upp;
            e.style.width = Mathf.Max(0f, b.W) * upp;
            e.style.height = Mathf.Max(0f, b.H) * upp;
            e.style.right = StyleKeyword.Auto;
            e.style.bottom = StyleKeyword.Auto;
            e.style.minWidth = StyleKeyword.Null; e.style.minHeight = StyleKeyword.Null;
            e.style.flexShrink = 0f;
        }
    }
}
