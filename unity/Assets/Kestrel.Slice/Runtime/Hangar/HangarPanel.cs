// The Hangar panel: readout strip, tabs (Stack, Part, Look, Data, Designs), and the scrolling editor for the
// selected part. A thin view over Kestrel.Core.Hangar.HangarSession: every control calls a HangarEditor op through
// the session, so undo, validation and the data sheet follow automatically.
// Value changes only refresh labels (a held stepper must not be rebuilt under the finger); the content is rebuilt
// when its structure changes (another part, tab, layer, engine or stage count).
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public sealed class HangarPanel
    {
        public enum Tab { Stack, Part, Look, Data, Designs }
        public const string EngineerPref = "kh.hangar.engineer";

        readonly HangarSession session;
        readonly DesignLibrary library;
        readonly HangarUi ui;
        readonly VisualElement root;
        readonly VisualElement strip, tabs, content;
        readonly ScrollView scroll;
        readonly List<Action> refreshers = new List<Action>();
        readonly Dictionary<Tab, Button> tabButtons = new Dictionary<Tab, Button>();
        readonly HashSet<string> expanded = new HashSet<string>();
        Tab tab = Tab.Part;
        string structure;
        string status = "";
        string pendingConfirm;
        bool engineer;

        Catalog C => session.catalog;
        HangarData H => session.data;
        JsonObject D => session.Design;

        /// <summary>The player asked to see a part (tap on a row): SliceApp focuses the camera on it.</summary>
        public event Action<PartRef> FocusRequested;
        /// <summary>Exploded view toggle from the toolbar.</summary>
        public event Action<bool> ExplodeRequested;
        public bool ExplodedOn { get; private set; }
        public bool Engineer => engineer;

        public HangarPanel(HangarSession session, DesignLibrary library, VisualElement body, float upp)
        {
            this.session = session;
            this.library = library;
            ui = new HangarUi { Upp = upp };
            root = body;
            engineer = PlayerPrefs.GetInt(EngineerPref, 0) != 0;
            root.Clear();
            strip = ui.Row(0f);
            strip.tooltip = "Data sheet. Tap for every number.";
            strip.RegisterCallback<ClickEvent>(_ => Show(Tab.Data));
            root.Add(strip);
            tabs = ui.Row(2f);
            root.Add(tabs);
            scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.style.flexShrink = 1;
            content = ui.Column();
            scroll.Add(content);
            root.Add(scroll);
            BuildTabs();
            session.Changed += OnChanged;
            session.SelectionChanged += _ => Rebuild();
            Rebuild();
        }

        public void Detach()
        {
            session.Changed -= OnChanged;
        }

        /// <summary>Units per point changed (first layout on a new device): rebuild at the new size.</summary>
        public void SetScale(float upp)
        {
            if (Mathf.Approximately(upp, ui.Upp)) return;
            ui.Upp = upp;
            BuildTabs();
            Rebuild();
        }

        public void SetContentMax(float units)
        {
            scroll.style.maxHeight = units > 0 ? units : StyleKeyword.Null;
        }

        public void SetEngineer(bool on)
        {
            if (engineer == on) return;
            engineer = on;
            PlayerPrefs.SetInt(EngineerPref, on ? 1 : 0);
            Rebuild();
        }

        public void Show(Tab t)
        {
            tab = t;
            Rebuild();
        }

        void OnChanged()
        {
            string key = StructureKey();
            if (key != structure) Rebuild();
            else Refresh();
        }

        void BuildTabs()
        {
            tabs.Clear();
            tabButtons.Clear();
            foreach (Tab t in Enum.GetValues(typeof(Tab)))
            {
                var tt = t;
                var b = ui.Btn(t.ToString(), () => Show(tt), t + " tab", minWidthPt: 56f);
                b.style.flexGrow = 1;
                b.style.paddingLeft = ui.U(4f); b.style.paddingRight = ui.U(4f);
                tabButtons[t] = b;
                tabs.Add(b);
            }
        }

        string StructureKey()
        {
            var sel = session.Selected;
            var body = HangarParts.Body(D, sel);
            var look = D.Obj("look");
            int stages = D.Arr("stages")?.Count ?? 0;
            return string.Join("|", tab, engineer, sel, stages, HangarParts.BoosterCount(D),
                body?.Str("engine"), body?.Str("propellant"), body?.Str("kind"),
                D.Obj("aero")?.Bool("fins"), look?.Arr("decals")?.Count ?? 0, look?.Str("livery"),
                tab == Tab.Data ? AdvisorySignature() : "", tab == Tab.Designs ? library.Count.ToString(CultureInfo.InvariantCulture) + status + pendingConfirm + expanded.Count : "",
                tab == Tab.Data ? string.Join(",", expanded) : "", tab == Tab.Stack ? string.Join(",", Names()) : "");
        }

        string AdvisorySignature()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var a in session.Advisories) sb.Append(a.code).Append(a.part).Append(';');
            return sb.ToString();
        }

        IEnumerable<string> Names()
        {
            foreach (var p in HangarParts.List(D)) yield return p.label;
        }

        void Rebuild()
        {
            refreshers.Clear();
            content.Clear();
            switch (tab)
            {
                case Tab.Stack: BuildStack(); break;
                case Tab.Part: BuildPart(); break;
                case Tab.Look: BuildLook(); break;
                case Tab.Data: BuildData(); break;
                case Tab.Designs: BuildDesigns(); break;
            }
            structure = StructureKey();
            Refresh();
        }

        void Refresh()
        {
            foreach (var t in tabButtons) ui.SetChip(t.Value, t.Key == tab);
            BuildStrip();
            foreach (var r in refreshers) r();
        }

        void BuildStrip()
        {
            strip.Clear();
            foreach (var c in HangarFormat.Cells(session.Sheet, engineer))
            {
                var cell = ui.Column();
                cell.style.flexGrow = 1;
                cell.style.flexBasis = 0;
                cell.style.alignItems = Align.Center;
                cell.Add(ui.Text(c.label, HangarUi.SmallPt, HangarUi.Dim));
                var v = ui.Text(c.value, engineer ? HangarUi.BodyPt : HangarUi.TitlePt, StateColor(c.state), bold: true);
                v.style.unityTextAlign = TextAnchor.MiddleCenter;
                cell.Add(v);
                cell.tooltip = c.label + " " + c.value + (c.hint != null ? ", " + c.hint : "");
                strip.Add(cell);
            }
        }

        static Color StateColor(CellState s) => s == CellState.Bad ? HangarUi.Red : s == CellState.Warn ? HangarUi.Amber : s == CellState.Good ? HangarUi.Green : HangarUi.TextColor;

        // ---------------- edits ----------------

        void Do(Func<JsonObject, Edit> op, string key = null)
        {
            try { session.Apply(op, key); }
            catch (Exception e) { Debug.LogWarning("[Hangar] edit failed: " + e.Message); }
        }

        void End() => session.EndGesture();

        // ---------------- Stack ----------------

        void BuildStack()
        {
            content.Add(ui.Section("Your rocket, top to bottom"));
            var list = HangarParts.List(D);
            int n = D.Arr("stages")?.Count ?? 0;
            foreach (var info in list)
            {
                var p = info.part;
                var card = ui.CardBox();
                if (info.depth > 0) card.style.marginLeft = ui.U(16f);
                var row = ui.Row(0f);
                var name = ui.Column(); name.style.flexGrow = 1; name.style.flexShrink = 1;
                name.Add(ui.Text(engineer ? info.label : info.kidLabel + (p.kind == PartKind.Stage && n > 1 ? $" ({p.index + 1})" : ""), HangarUi.BodyPt, bold: true));
                var sub = ui.Text("", HangarUi.SmallPt, HangarUi.Dim);
                name.Add(sub);
                refreshers.Add(() => sub.text = HangarFormat.Subtitle(C, D, p));
                name.RegisterCallback<ClickEvent>(_ => { session.Select(p); FocusRequested?.Invoke(p); Show(Tab.Part); });
                name.tooltip = info.label + ". Tap to edit.";
                row.Add(name);
                if (p.kind == PartKind.Stage)
                {
                    int i = p.index;
                    if (i < n - 1) row.Add(ui.Btn("▲", () => Do(d => HangarEditor.MoveStage(d, i, i + 1)), $"Move stage {i + 1} up"));
                    if (i > 0) row.Add(ui.Btn("▼", () => Do(d => HangarEditor.MoveStage(d, i, i - 1)), $"Move stage {i + 1} down"));
                    if (n > DesignerLimits.Active.stagesMin) row.Add(ui.Btn("×", () => Do(d => HangarEditor.RemoveStage(d, i)), $"Remove stage {i + 1}", bg: new Color(0.6f, 0.15f, 0.15f, 0.6f)));
                }
                card.Add(row);
                refreshers.Add(() => card.style.borderLeftWidth = session.Selected == p ? ui.U(4f) : 0f);
                card.style.borderLeftColor = HangarUi.ChipOn;
                content.Add(card);
            }
            var add = ui.Row();
            if (n < DesignerLimits.Active.stagesMax)
            {
                add.Add(ui.Btn("+ Stage on top", () => Do(d => HangarEditor.AddStage(C, d, n)), "Add a stage on top"));
                add.Add(ui.Btn("+ Stage at bottom", () => Do(d => HangarEditor.AddStage(C, d, 0)), "Add a stage at the bottom"));
            }
            content.Add(add);
            content.Add(BoosterCountChips());
            if (engineer)
            {
                content.Add(ui.Section("Whole rocket"));
                content.Add(ui.Chips("Avionics", Ids(HangarEditor.AvionicsTiers), () => D.Obj("avionics")?.Str("tier"), id => Do(d => HangarEditor.SetAvionics(d, id)), refreshers));
                content.Add(ui.Stepper("Safety factor", () => (D.Num("safetyFactor", 1.4)).ToString("0.00", CultureInfo.InvariantCulture), () => "Stronger structure weighs more",
                    (dir, key) => Do(d => HangarEditor.SetSafetyFactor(d, d.Num("safetyFactor", 1.4) + dir * DesignerLimits.Active.safetyStep), key), End, "safety", refreshers));
            }
        }

        VisualElement BoosterCountChips()
        {
            var counts = new List<HangarUi.Choice>();
            foreach (var k in DesignerLimits.Active.strapOnCounts) counts.Add(new HangarUi.Choice(k.ToString(CultureInfo.InvariantCulture), k == 0 ? "None" : k.ToString(CultureInfo.InvariantCulture)));
            return ui.Chips(engineer ? "Strap-on boosters" : "Side boosters", counts, () => HangarParts.BoosterCount(D).ToString(CultureInfo.InvariantCulture),
                id => Do(d => HangarEditor.SetBoosterCount(C, d, int.Parse(id, CultureInfo.InvariantCulture))), refreshers);
        }

        // ---------------- Part ----------------

        void BuildPart()
        {
            var p = session.Selected;
            var info = HangarParts.List(D).Find(x => x.part == p);
            var title = ui.Row();
            var t = ui.Text(info != null ? (engineer ? info.label : info.kidLabel) : HangarParts.Short(p), HangarUi.TitlePt, bold: true);
            t.style.flexGrow = 1;
            title.Add(t);
            title.Add(ui.Btn("Show", () => FocusRequested?.Invoke(p), "Point the camera at this part"));
            content.Add(title);
            switch (p.kind)
            {
                case PartKind.Stage: BuildEngined(p); if (p.index == 0) BuildRecovery(); break;
                case PartKind.Boosters: BuildBoosters(); BuildEngined(p); break;
                case PartKind.Interstage: BuildInterstage(p.index); break;
                case PartKind.Fairing: BuildFairing(); break;
                case PartKind.Payload: BuildPayload(); break;
            }
            AddPartIssues(p);
        }

        JsonObject Body(PartRef p) => HangarParts.Body(D, p);

        void BuildEngined(PartRef p)
        {
            string k = p.ToString();
            var engines = new List<HangarUi.Choice>();
            foreach (var e in HangarEditor.EngineChoices(C, D, p))
                engines.Add(new HangarUi.Choice(e.id, HangarFormat.EngineChip(e), $"{e.label}: {HangarFormat.Kn(Math.Max(e.thrustSL, e.thrustVac))}, Isp {e.ispVac:0} s"));
            content.Add(ui.Section(engineer ? "Engines" : "Engines (the push)"));
            content.Add(ui.Chips("Engine type", engines, () => Body(p)?.Str("engine"), id => Do(d => HangarEditor.SetEngine(C, d, p, id)), refreshers));
            content.Add(ui.Stepper("How many engines", () => ((int)(Body(p)?.Num("engineCount") ?? 0)).ToString(CultureInfo.InvariantCulture),
                () => EngineHint(p), (dir, key) => Do(d => HangarEditor.SetEngineCount(d, p, (int)(HangarParts.Body(d, p)?.Num("engineCount") ?? 1) + dir), key), End, k + ".engines", refreshers));
            var noz = HangarEditor.NozzleChoices(C, D, p);
            if (noz.Count > 1)
            {
                var nc = new List<HangarUi.Choice>();
                foreach (var id in noz) nc.Add(new HangarUi.Choice(id, id == "default" ? "Standard bell" : HangarFormat.Cap(id) + " bell", id == "extended" ? "Longer nozzle: more push in space, less at the pad" : "The engine's standard nozzle"));
                content.Add(ui.Chips("Nozzle extension", nc, () => Body(p)?.Str("nozzle") ?? "default", id => Do(d => HangarEditor.SetNozzle(C, d, p, id)), refreshers));
            }
            var pats = new List<HangarUi.Choice>();
            foreach (var pl in DesignerLimits.Active.patterns) pats.Add(new HangarUi.Choice(pl.id, PatternName(pl.id, pl.fixedCount ? pl.count : 0)));
            content.Add(ui.Chips("Engine layout", pats, () => Body(p)?.Obj("cluster")?.Str("pattern") ?? "auto", id => Do(d => HangarEditor.SetClusterPattern(d, p, id)), refreshers));
            if (engineer)
            {
                content.Add(ui.Stepper("Thrust tune", () => Signed(Body(p)?.Obj("tune")?.Num("thrustPct") ?? 0) + "%", () => "Push harder, wear out faster",
                    (dir, key) => Do(d => { var tu = HangarParts.Body(d, p)?.Obj("tune"); return HangarEditor.SetTune(d, p, (tu?.Num("thrustPct") ?? 0) + dir, tu?.Num("ispPct") ?? 0); }, key), End, k + ".tuneT", refreshers));
                content.Add(ui.Stepper("Isp tune", () => Signed(Body(p)?.Obj("tune")?.Num("ispPct") ?? 0) + "%", null,
                    (dir, key) => Do(d => { var tu = HangarParts.Body(d, p)?.Obj("tune"); return HangarEditor.SetTune(d, p, tu?.Num("thrustPct") ?? 0, (tu?.Num("ispPct") ?? 0) + dir); }, key), End, k + ".tuneI", refreshers));
            }

            content.Add(ui.Section(engineer ? "Tank" : "Fuel tank"));
            var props = new List<HangarUi.Choice>();
            foreach (var pr in C.Propellants)
            {
                bool burns = false;
                foreach (var e in C.Engines) if (e.propellant == pr.id) { burns = true; break; }
                if (burns && (engineer || KidPropellant(pr.id))) props.Add(new HangarUi.Choice(pr.id, PropName(pr), pr.label));
            }
            content.Add(ui.Chips("Fuel", props, () => Body(p)?.Str("propellant"), id => Do(d => HangarEditor.SetPropellant(C, d, p, id)), refreshers));
            content.Add(ui.Stepper("Fuel load", () => HangarFormat.Tons(Body(p)?.Num("propMass") ?? 0), () => engineer ? null : "More fuel goes further but weighs more",
                (dir, key) => Do(d => HangarEditor.SetPropMass(d, p, HangarFormat.StepProp(HangarParts.Body(d, p)?.Num("propMass") ?? 0, dir)), key), End, k + ".prop", refreshers));
            content.Add(ui.Stepper("Tank length", () => HangarFormat.Metres(HangarEditor.TankLength(C, Body(p))), () => "Longer tank, more fuel",
                (dir, key) => Do(d => HangarEditor.SetTankLength(C, d, p, HangarEditor.TankLength(C, HangarParts.Body(d, p)) + dir * 0.5), key), End, k + ".len", refreshers));
            content.Add(ui.Stepper("Width", () => HangarFormat.Metres(Body(p)?.Num("diameter") ?? 0), () => "Wider fits more engines",
                (dir, key) => Do(d => HangarEditor.SetDiameter(d, p, (HangarParts.Body(d, p)?.Num("diameter") ?? 1) + dir * 0.1), key), End, k + ".dia", refreshers));
            var mats = new List<HangarUi.Choice>();
            foreach (var m in C.Materials) mats.Add(new HangarUi.Choice(m.id, m.label));
            content.Add(ui.Chips("Material", mats, () => Body(p)?.Str("material"), id => Do(d => HangarEditor.SetMaterial(C, d, p, id)), refreshers));
            if (engineer && p.kind == PartKind.Stage)
            {
                var nameField = new TextField { value = Body(p)?.Str("name") ?? "", maxLength = 24 };
                nameField.label = "Stage name";
                nameField.style.fontSize = ui.U(HangarUi.BodyPt);
                nameField.isDelayed = true;
                int idx = p.index;
                nameField.RegisterValueChangedCallback(e => Do(d => HangarEditor.SetStageName(d, idx, e.newValue)));
                content.Add(nameField);
            }
        }

        string EngineHint(PartRef p)
        {
            var s = session.Sheet;
            if (s == null) return null;
            var row = s.parts.Find(x => x.part == p);
            if (row == null) return null;
            double thrust = p.kind == PartKind.Stage && p.index > 0 ? row.thrustVac_N : row.thrustSL_N;
            return engineer ? $"{HangarFormat.Kn(thrust)} · Isp {row.ispSL_s:0}/{row.ispVac_s:0} s" : $"Push {HangarFormat.Kn(thrust)}";
        }

        void BuildBoosters()
        {
            content.Add(BoosterCountChips());
            content.Add(ui.Chips("Booster type", new List<HangarUi.Choice> { new HangarUi.Choice("solid", "Solid"), new HangarUi.Choice("liquid", "Liquid") },
                () => D.Obj("strapOns")?.Str("kind"), id => Do(d => HangarEditor.SetBoosterKind(C, d, id)), refreshers));
            if (engineer && D.Obj("strapOns")?.Str("kind") == "liquid")
                content.Add(ui.Switch("Crossfeed", "Boosters feed the core", () => D.Obj("strapOns")?.Bool("crossfeed") ?? false, on => Do(d => HangarEditor.SetCrossfeed(d, on)), refreshers));
        }

        void BuildRecovery()
        {
            var a = D.Obj("aero");
            content.Add(ui.Section(engineer ? "Aero and recovery" : "Fins and landing"));
            content.Add(ui.Switch("Fins", "Steady in the air", () => D.Obj("aero")?.Bool("fins") ?? false, on => Do(d => HangarEditor.SetFins(d, on)), refreshers));
            if (a != null && a.Bool("fins"))
            {
                content.Add(ui.Stepper("Fin count", () => ((int)(D.Obj("aero")?.Num("finCount", 4) ?? 4)).ToString(CultureInfo.InvariantCulture), null,
                    (dir, key) => Do(d => HangarEditor.SetFinCount(d, (int)(d.Obj("aero")?.Num("finCount", 4) ?? 4) + dir), key), End, "fins.count", refreshers));
                content.Add(ui.Stepper("Fin size", () => HangarFormat.Metres(D.Obj("aero")?.Num("finSpanM", 1) ?? 1), () => "Bigger fins move the push point down",
                    (dir, key) => Do(d => HangarEditor.SetFinSpan(d, (d.Obj("aero")?.Num("finSpanM", 1) ?? 1) + dir * DesignerLimits.Active.finSpanStep), key), End, "fins.span", refreshers));
            }
            content.Add(ui.Switch("Landing legs", null, () => D.Obj("landing")?.Bool("legs") ?? false, on => Do(d => HangarEditor.SetLegs(d, on)), refreshers));
            content.Add(ui.Switch("Grid fins", "Steer on the way down", () => D.Obj("landing")?.Bool("gridFins") ?? false, on => Do(d => HangarEditor.SetGridFins(d, on)), refreshers));
            content.Add(ui.Switch(engineer ? "RCS" : "Steering jets", null, () => D.Obj("landing")?.Bool("rcs") ?? false, on => Do(d => HangarEditor.SetRcs(d, on)), refreshers));
            content.Add(ui.Stepper("Landing fuel", () => HangarFormat.Pct(Reserve()), () => "Fuel kept to fly back",
                (dir, key) => Do(d => HangarEditor.SetLandingReserve(d, 0, Math.Round(Reserve() * 100 + dir) / 100.0), key), End, "landing", refreshers));
        }

        double Reserve()
        {
            var s = Body(PartRef.Stage(0));
            return s == null || !(s.Num("propMass") > 0) ? 0 : s.Num("landingPropMass") / s.Num("propMass");
        }

        void BuildInterstage(int i)
        {
            var shapes = new List<HangarUi.Choice>
            {
                new HangarUi.Choice("straight", "Straight"), new HangarUi.Choice("flare", "Flare", "Wider at the bottom: steadier"), new HangarUi.Choice("shoulder", "Shoulder", "Wider at the top: wobblier"),
            };
            content.Add(ui.Chips("Joint shape", shapes, () => Body(PartRef.Interstage(i))?.Obj("interstage")?.Str("shape") ?? "straight", id => Do(d => HangarEditor.SetInterstageShape(d, i, id)), refreshers));
            if (engineer)
            {
                content.Add(ui.Stepper("Interstage mass", () => Body(PartRef.Interstage(i))?.ContainsKey("interstageMass") == true ? HangarFormat.Kg(Body(PartRef.Interstage(i)).Num("interstageMass")) : "auto",
                    () => "Override; Auto derives it from the cone",
                    (dir, key) => Do(d => HangarEditor.SetInterstageMass(d, i, Math.Max(0, (HangarParts.Body(d, PartRef.Interstage(i))?.Num("interstageMass") ?? 0) + dir * 100)), key), End, "is" + i, refreshers));
                content.Add(ui.Btn("Auto mass", () => Do(d => HangarEditor.SetInterstageMass(d, i, null)), "Derive the interstage mass"));
            }
        }

        void BuildFairing()
        {
            content.Add(ui.Chips("Nose shape", new List<HangarUi.Choice>
            {
                new HangarUi.Choice("ogive", "Ogive"), new HangarUi.Choice("von-karman", "Von Kármán"), new HangarUi.Choice("conic", "Cone"), new HangarUi.Choice("blunt", "Blunt"),
            }, () => D.Obj("aero")?.Str("nose"), id => Do(d => HangarEditor.SetNose(d, id)), refreshers));
            content.Add(ui.Stepper("Width", () => HangarFormat.Metres(D.Obj("fairing")?.Num("diameter") ?? 0), () => "At least half the top stage",
                (dir, key) => Do(d => HangarEditor.SetDiameter(d, PartRef.Fairing, (d.Obj("fairing")?.Num("diameter") ?? 1) + dir * 0.1), key), End, "fair.d", refreshers));
            content.Add(ui.Stepper("Length", () => HangarFormat.Metres(D.Obj("fairing")?.Num("length") ?? 0), () => "Room for the cargo",
                (dir, key) => Do(d => HangarEditor.SetFairingLength(d, (d.Obj("fairing")?.Num("length") ?? 1) + dir * 0.5), key), End, "fair.l", refreshers));
            var mats = new List<HangarUi.Choice>();
            foreach (var m in C.Materials) mats.Add(new HangarUi.Choice(m.id, m.label));
            content.Add(ui.Chips("Material", mats, () => D.Obj("fairing")?.Str("material"), id => Do(d => HangarEditor.SetMaterial(C, d, PartRef.Fairing, id)), refreshers));
            if (engineer)
                content.Add(ui.Stepper("Release height", () => ((D.Obj("fairing")?.Num("jettisonAltitudeKm", 110) ?? 110)).ToString("0", CultureInfo.InvariantCulture) + " km", null,
                    (dir, key) => Do(d => HangarEditor.SetJettisonAltitude(d, (d.Obj("fairing")?.Num("jettisonAltitudeKm", 110) ?? 110) + dir * 5), key), End, "fair.j", refreshers));
        }

        void BuildPayload()
        {
            var pls = new List<HangarUi.Choice>();
            foreach (var x in C.Payloads) pls.Add(new HangarUi.Choice(x.id, x.displayName ?? x.label, x.label));
            content.Add(ui.Chips("Cargo", pls, () => D.Obj("payload")?.Str("type"), id => Do(d => HangarEditor.SetPayloadType(C, d, id)), refreshers));
            content.Add(ui.Stepper("Cargo mass", () => HangarFormat.Kg(D.Obj("payload")?.Num("mass") ?? 0), () => "Heavier cargo needs more speed",
                (dir, key) => Do(d => { double m = d.Obj("payload")?.Num("mass") ?? 0; return HangarEditor.SetPayloadMass(d, m + dir * (m < 1000 ? 50 : m < 10000 ? 250 : 1000)); }, key), End, "pl.m", refreshers));
            var orbits = new List<HangarUi.Choice>();
            foreach (var kv in C.Orbits) orbits.Add(new HangarUi.Choice(kv.Key, kv.Value.label));
            content.Add(ui.Chips(engineer ? "Orbit" : "Where to?", orbits, () => D.Obj("payload")?.Str("orbit"), id => Do(d => HangarEditor.SetOrbit(C, d, id)), refreshers));
        }

        /// <summary>Issues on this part. Rebuilt in place on every change so held steppers above it are never torn down.</summary>
        void AddPartIssues(PartRef p)
        {
            var box = ui.Column();
            string last = null;
            refreshers.Add(() =>
            {
                var mine = session.Advisories.FindAll(a => a.part == p);
                var sig = new System.Text.StringBuilder();
                foreach (var a in mine) sig.Append(a.code).Append(a.engineer).Append(';');
                if (sig.ToString() == last) return;
                last = sig.ToString();
                box.Clear();
                if (mine.Count == 0) return;
                box.Add(ui.Section("To check"));
                foreach (var a in mine) box.Add(AdvisoryCard(a, a.code + p));
            });
            content.Add(box);
        }

        // ---------------- Look ----------------

        void BuildLook()
        {
            var h = H;
            var progress = session.progress;
            var name = new TextField { value = D.Obj("livery")?.Str("name") ?? "", maxLength = HangarEditor.VehicleNameMax, isDelayed = true };
            name.label = "Rocket name";
            name.style.fontSize = ui.U(HangarUi.BodyPt);
            name.RegisterValueChangedCallback(e => Do(d => HangarEditor.SetVehicleName(d, e.newValue)));
            content.Add(name);
            if (h == null) return;

            var liveries = new List<HangarUi.Choice>();
            foreach (var l in h.liveries) liveries.Add(new HangarUi.Choice(l.id, Words(l.id), progress.Earned(l.earn) ? Words(l.id) : Words(l.id) + ". " + l.earn.Describe(), !progress.Earned(l.earn)));
            content.Add(ui.Chips("Livery", liveries, () => D.Obj("look")?.Str("livery"), id => Do(d => HangarEditor.ApplyLivery(h, progress, d, id)), refreshers));
            content.Add(ui.Swatches("Main colour", h.palette, () => D.Obj("livery")?.Str("primary"), hex => Do(d => HangarEditor.SetPaint(d, hex, null)), refreshers));
            content.Add(ui.Swatches("Stripe colour", h.palette, () => D.Obj("livery")?.Str("accent"), hex => Do(d => HangarEditor.SetPaint(d, null, hex)), refreshers));

            content.Add(ui.Section("Finishes"));
            foreach (var slot in h.finishSlots)
            {
                if (!SlotPresent(slot)) continue;
                var s = slot;
                var fs = new List<HangarUi.Choice> { new HangarUi.Choice("", "Paint") };
                foreach (var f in h.FinishesFor(s)) fs.Add(new HangarUi.Choice(f.id, Words(f.id), progress.Earned(f.earn) ? Words(f.id) : Words(f.id) + ". " + f.earn.Describe(), !progress.Earned(f.earn)));
                content.Add(ui.Chips(HangarFormat.Cap(s), fs, () => D.Obj("look")?.Obj("finishes")?.Str(s) ?? "", id => Do(d => HangarEditor.SetFinish(h, progress, d, s, id.Length == 0 ? null : id)), refreshers));
            }

            content.Add(ui.Section($"Decals (up to {h.maxDecals})"));
            foreach (var slot in h.decalSlots)
            {
                if (slot.auto || !DecalSlotPresent(slot)) continue;
                var sl = slot;
                var ds = new List<HangarUi.Choice> { new HangarUi.Choice("", "None") };
                foreach (var dd in h.DecalsFor(sl.id)) ds.Add(new HangarUi.Choice(dd.id, Words(dd.id), progress.Earned(dd.earn) ? Words(dd.id) : Words(dd.id) + ". " + dd.earn.Describe(), !progress.Earned(dd.earn)));
                content.Add(ui.Chips(Words(sl.id), ds, () => DecalIn(sl.id)?.Str("id") ?? "", id => Do(d => PlaceDecal(d, sl.id, id)), refreshers));
                int at = DecalIndex(sl.id);
                if (at >= 0)
                {
                    var row = ui.Row();
                    foreach (var sc in h.decalScales) { var v = sc; row.Add(ui.Btn(v.ToString("0.#", CultureInfo.InvariantCulture) + "×", () => Do(d => HangarEditor.SetDecalScale(h, d, DecalIndex(sl.id), v)), "Decal size " + v)); }
                    foreach (var r in h.decalRotations) { var v = r; row.Add(ui.Btn(v + "°", () => Do(d => HangarEditor.SetDecalRotation(h, d, DecalIndex(sl.id), v)), "Turn decal " + v + " degrees")); }
                    content.Add(row);
                }
            }
        }

        Edit PlaceDecal(JsonObject d, string slot, string decalId)
        {
            int at = DecalIndex(slot);
            var x = at >= 0 ? DecalIn(slot) : null;
            double scale = x?.Num("scale", 1) ?? 1; int rot = (int)(x?.Num("rotate") ?? 0);
            if (at >= 0 && x?.Str("id") == decalId) return Edit.None;
            var removed = at >= 0 ? HangarEditor.RemoveDecal(d, at) : Edit.None;
            if (decalId.Length == 0) return removed;
            var added = HangarEditor.AddDecal(H, session.progress, d, decalId, slot, scale, rot);
            return added.changed ? added : removed;
        }

        JsonObject DecalIn(string slot)
        {
            var list = D.Obj("look")?.Arr("decals");
            if (list == null) return null;
            foreach (var o in list) if (o is JsonObject j && j.Str("slot") == slot) return j;
            return null;
        }

        int DecalIndex(string slot)
        {
            var list = D.Obj("look")?.Arr("decals");
            if (list == null) return -1;
            for (int i = 0; i < list.Count; i++) if (list[i] is JsonObject j && j.Str("slot") == slot) return i;
            return -1;
        }

        bool SlotPresent(string slot)
        {
            switch (slot)
            {
                case "booster": return HangarParts.BoosterCount(D) > 0;
                case "interstage": return (D.Arr("stages")?.Count ?? 0) > 1;
                case "fins": return D.Obj("aero")?.Bool("fins") ?? false;
                case "legs": return D.Obj("landing")?.Bool("legs") ?? false;
                case "gridfins": return D.Obj("landing")?.Bool("gridFins") ?? false;
                default: return true;
            }
        }

        bool DecalSlotPresent(DecalSlot s) => s.part == "booster" ? HangarParts.BoosterCount(D) > 0 : s.part != "interstage" || (D.Arr("stages")?.Count ?? 0) > 1;

        // ---------------- Data ----------------

        void BuildData()
        {
            var s = session.Sheet;
            content.Add(ui.Switch("Engineer numbers", "Every number, plus the exact warning text", () => engineer, SetEngineer, refreshers));
            var adv = session.Advisories;
            content.Add(ui.Section(adv.Count == 0 ? "All good" : $"Things to check ({adv.Count})"));
            if (adv.Count == 0) content.Add(ui.Text(Experience.Ready, HangarUi.BodyPt, HangarUi.Green));
            for (int i = 0; i < adv.Count; i++) content.Add(AdvisoryCard(adv[i], adv[i].code + i));
            if (!session.LaunchEnabled)
                content.Add(ui.Btn("Make it fly", () =>
                {
                    var changes = session.MakeItFly();
                    status = changes.Count == 0 ? "Nothing to change." : "Changed: " + string.Join(", ", changes);
                    Rebuild();
                }, "Fix the blocking problems automatically", bg: HangarUi.Amber, fg: HangarUi.Ink));
            if (!string.IsNullOrEmpty(status) && tab == Tab.Data) content.Add(ui.Text(status, HangarUi.SmallPt, HangarUi.Dim));
            if (s == null) return;

            content.Add(ui.Section("Whole rocket"));
            var card = ui.CardBox();
            void Line(string k, Func<string> v)
            {
                var r = ui.Row(0f);
                var kl = ui.Text(k, HangarUi.BodyPt, HangarUi.Dim); kl.style.flexGrow = 1;
                var vl = ui.Text("", HangarUi.BodyPt, bold: true);
                r.Add(kl); r.Add(vl);
                refreshers.Add(() => vl.text = v());
                card.Add(r);
            }
            DataSheet S() => session.Sheet ?? s;
            Line(engineer ? "Liftoff mass" : "Weight", () => HangarFormat.Tons(S().liftoffMass_kg));
            Line(engineer ? "Liftoff thrust" : "Push at liftoff", () => HangarFormat.Kn(S().liftoffThrust_N));
            Line(engineer ? "Liftoff TWR" : "Push ÷ weight", () => HangarFormat.Ratio(S().liftoffTwr));
            Line(engineer ? "Δv (vacuum)" : "Speed it can reach", () => HangarFormat.Ms(S().totalDeltaV_mps));
            Line(engineer ? "Δv needed" : "Speed the trip needs", () => HangarFormat.Ms(S().requiredDeltaV_mps) + " · " + S().orbitLabel);
            Line("Cost", () => HangarFormat.Kc(S().cost_Kc) + " of " + HangarFormat.Kc(S().budget_Kc));
            Line(engineer ? "Stability" : "Steadiness", () => engineer ? $"{S().stabilityCal:0.00} cal · {S().StabilityWord}" : S().StabilityWord);
            if (engineer)
            {
                Line("CG / CP from base", () => $"{HangarFormat.Metres(S().cg_m)} / {HangarFormat.Metres(S().cp_m)}");
                Line("Length · ref. diameter", () => $"{HangarFormat.Metres(S().length_m)} · {HangarFormat.Metres(S().refDiameter_m)}");
                Line("Dry mass", () => HangarFormat.Tons(S().dryMass_kg));
                Line("Max-Q", () => S().maxQ_kPa.HasValue ? $"{S().maxQ_kPa.Value:0.0} kPa" : "after a test flight");
                Line("Reliability", () => (S().reliability * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%");
            }
            content.Add(card);
            content.Add(StabilityBar());

            content.Add(ui.Section("Stage by stage"));
            for (int i = 0; i < s.phases.Count; i++)
            {
                int k = i;
                var c = ui.CardBox();
                c.Add(ui.Text(s.phases[i].label, HangarUi.BodyPt, bold: true));
                var t = ui.Text("", HangarUi.SmallPt, HangarUi.Dim);
                refreshers.Add(() =>
                {
                    var ph = S().phases.Count > k ? S().phases[k] : null;
                    if (ph == null) { t.text = ""; return; }
                    t.text = engineer
                        ? $"Δv {HangarFormat.Ms(ph.deltaV_mps)}{(ph.deltaVSL_mps.HasValue ? $" (SL {HangarFormat.Ms(ph.deltaVSL_mps.Value)})" : "")} · TWR {HangarFormat.Ratio(ph.twr)} (vac {HangarFormat.Ratio(ph.twrVac)})\nStart {HangarFormat.Tons(ph.startMass_kg)} · dry {HangarFormat.Tons(ph.dryMass_kg)} · fuel {HangarFormat.Tons(ph.propMass_kg)} · {HangarFormat.Kn(ph.thrust_N)}"
                        : $"Adds {HangarFormat.Ms(ph.deltaV_mps)} of speed · push {HangarFormat.Ratio(ph.twr)}× its weight";
                });
                c.Add(t);
                content.Add(c);
            }
        }

        /// <summary>A bar the length of the rocket with the CG (weight) and CP (air push) marked. CG above CP is steady.</summary>
        VisualElement StabilityBar()
        {
            var col = ui.Column();
            col.Add(ui.Text(engineer ? "CG above CP is stable" : "Weight point (W) above air-push point (P) flies straight", HangarUi.SmallPt, HangarUi.Dim));
            var bar = new VisualElement();
            bar.style.height = ui.U(22f);
            bar.style.backgroundColor = HangarUi.ChipOff;
            ui.Radius(bar.style, 11f);
            var cg = ui.Text(engineer ? "CG" : "W", HangarUi.SmallPt, HangarUi.Amber, bold: true); cg.style.position = Position.Absolute;
            var cp = ui.Text(engineer ? "CP" : "P", HangarUi.SmallPt, HangarUi.ChipOn, bold: true); cp.style.position = Position.Absolute;
            bar.Add(cg); bar.Add(cp);
            refreshers.Add(() =>
            {
                var s = session.Sheet;
                float len = s != null && s.length_m > 0 ? (float)s.length_m : 1f;
                cg.style.left = Length.Percent(Mathf.Clamp01((float)((s?.cg_m ?? 0) / len)) * 94f);
                cp.style.left = Length.Percent(Mathf.Clamp01((float)((s?.cp_m ?? 0) / len)) * 94f);
            });
            col.Add(bar);
            var ends = ui.Row(0f);
            var b = ui.Text("Base", HangarUi.SmallPt, HangarUi.Dim); b.style.flexGrow = 1;
            ends.Add(b); ends.Add(ui.Text("Nose", HangarUi.SmallPt, HangarUi.Dim));
            col.Add(ends);
            return col;
        }

        VisualElement AdvisoryCard(Advisory a, string key)
        {
            var c = ui.CardBox();
            var col = a.severity == AdvisorySeverity.Block ? HangarUi.Red : a.severity == AdvisorySeverity.Warn ? HangarUi.Amber : HangarUi.Dim;
            c.style.borderLeftWidth = ui.U(4f);
            c.style.borderLeftColor = col;
            var head = ui.Row(0f);
            var h = ui.Text((a.severity == AdvisorySeverity.Block ? "Fix: " : "") + a.headline, HangarUi.BodyPt, col, bold: true);
            h.style.flexGrow = 1;
            head.Add(h);
            bool open = engineer || expanded.Contains(key);
            if (!engineer) head.Add(ui.Btn(open ? "Less" : "More", () => { if (!expanded.Add(key)) expanded.Remove(key); Rebuild(); }, open ? "Hide the details" : "Show the engineer details"));
            head.Add(ui.Btn("Show", () => { session.Select(a.part); FocusRequested?.Invoke(a.part); Show(Tab.Part); }, "Go to the part"));
            c.Add(head);
            c.Add(ui.Text(a.kid, HangarUi.BodyPt));
            if (open && !string.IsNullOrEmpty(a.engineer) && a.engineer != a.kid) c.Add(ui.Text(a.engineer, HangarUi.SmallPt, HangarUi.Dim));
            c.tooltip = a.headline + ". " + a.kid;
            return c;
        }

        // ---------------- Designs ----------------

        void BuildDesigns()
        {
            content.Add(ui.Section("This design"));
            var name = new TextField { value = session.SavedName ?? D.Str("name") ?? "", maxLength = DesignLibrary.NameMax };
            name.label = "Name";
            name.style.fontSize = ui.U(HangarUi.BodyPt);
            content.Add(name);
            var row = ui.Row();
            row.Add(ui.Btn(session.SavedId == null ? "Save" : "Save", () => SaveResult(session.SaveTo(library, name.value)), "Save this design", bg: HangarUi.ChipOn, fg: HangarUi.Ink));
            if (session.SavedId != null) row.Add(ui.Btn("Save as new", () => SaveResult(session.SaveAsNew(library, library.UniqueName(name.value))), "Save a copy in a new slot"));
            content.Add(row);
            var st = ui.Text("", HangarUi.SmallPt, HangarUi.Dim);
            refreshers.Add(() => st.text = !string.IsNullOrEmpty(status) ? status : session.Dirty ? "Not saved yet." : "Saved.");
            content.Add(st);

            var saves = library.List();
            content.Add(ui.Section($"Saved ({saves.Count} of {DesignLibrary.MaxSlots})"));
            foreach (var m in saves)
            {
                var info = m;
                var c = ui.CardBox();
                var title = ui.Text(info.name + (info.id == session.SavedId ? "  (open)" : ""), HangarUi.BodyPt, bold: true);
                c.Add(title);
                var r = ui.Row(0f);
                if (pendingConfirm == "delete:" + info.id)
                {
                    r.Add(ui.Text($"Delete {info.name}? This can't be undone.", HangarUi.SmallPt, HangarUi.Amber));
                    r.Add(ui.Btn("Delete", () => { library.Delete(info.id); pendingConfirm = null; status = "Deleted " + info.name + "."; Rebuild(); }, "Delete for good", bg: HangarUi.Red));
                    r.Add(ui.Btn("Keep", () => { pendingConfirm = null; Rebuild(); }));
                }
                else if (pendingConfirm == "open:" + info.id)
                {
                    r.Add(ui.Text("Your changes aren't saved. Open anyway?", HangarUi.SmallPt, HangarUi.Amber));
                    r.Add(ui.Btn("Open", () => { pendingConfirm = null; Open(info.id); }));
                    r.Add(ui.Btn("Stay", () => { pendingConfirm = null; Rebuild(); }));
                }
                else
                {
                    r.Add(ui.Btn("Open", () => { if (session.Dirty && info.id != session.SavedId) { pendingConfirm = "open:" + info.id; Rebuild(); } else Open(info.id); }, "Open " + info.name));
                    r.Add(ui.Btn("Copy", () => { var o = library.Duplicate(info.id); status = o.ok ? "Copied as " + o.name + "." : Problem(o.error); Rebuild(); }, "Copy " + info.name));
                    r.Add(ui.Btn("Rename", () => { var o = library.Rename(info.id, name.value); status = o.ok ? "Renamed to " + o.name + "." : Problem(o.error); Rebuild(); }, "Rename it to the name above"));
                    r.Add(ui.Btn("Delete", () => { pendingConfirm = "delete:" + info.id; Rebuild(); }, "Delete " + info.name));
                }
                r.style.flexWrap = Wrap.Wrap;
                c.Add(r);
                content.Add(c);
            }

            if (H != null && H.templates.Count > 0)
            {
                content.Add(ui.Section("Start from a template"));
                foreach (var t in H.templates)
                {
                    var tpl = t;
                    var c = ui.CardBox();
                    var r = ui.Row(0f);
                    var col = ui.Column(); col.style.flexGrow = 1; col.style.flexShrink = 1;
                    col.Add(ui.Text(tpl.name, HangarUi.BodyPt, bold: true));
                    col.Add(ui.Text(engineer ? tpl.engineerBlurb : tpl.kidBlurb, HangarUi.SmallPt, HangarUi.Dim));
                    r.Add(col);
                    if (pendingConfirm == "tpl:" + tpl.id)
                    {
                        r.Add(ui.Btn("Start", () => { pendingConfirm = null; Fork(tpl); }, "Start anyway"));
                        r.Add(ui.Btn("Stay", () => { pendingConfirm = null; Rebuild(); }));
                    }
                    else r.Add(ui.Btn("Start", () => { if (session.Dirty) { pendingConfirm = "tpl:" + tpl.id; status = "Your changes aren't saved. Start anyway?"; Rebuild(); } else Fork(tpl); }, "Start from " + tpl.name));
                    c.Add(r);
                    content.Add(c);
                }
            }
        }

        void SaveResult(SaveOutcome o)
        {
            status = o.ok ? "Saved as " + o.name + "." : Problem(o.error);
            Rebuild();
        }

        static string Problem(string code) => code == "full" ? $"All {DesignLibrary.MaxSlots} slots are full. Delete one first." : code == "missing" ? "That save is gone." : "Couldn't save (" + code + ").";

        void Open(string id)
        {
            var d = library.Load(id);
            if (d == null) { status = Problem("missing"); Rebuild(); return; }
            session.Load(d, id, library.NameOf(id));
            status = "Opened " + session.SavedName + ".";
            Show(Tab.Part);
        }

        void Fork(StarterTemplate t)
        {
            session.Load(HangarEditor.ForkTemplate(t), null, t.name);
            status = "Started from " + t.name + ".";
            Show(Tab.Stack);
        }

        // ---------------- words ----------------

        static List<HangarUi.Choice> Ids(IEnumerable<string> ids)
        {
            var o = new List<HangarUi.Choice>();
            foreach (var id in ids) o.Add(new HangarUi.Choice(id, HangarFormat.Cap(id)));
            return o;
        }

        static string Signed(double v) => (v > 0 ? "+" : "") + v.ToString("0", CultureInfo.InvariantCulture);

        static bool KidPropellant(string id) => id == "kerolox" || id == "methalox" || id == "hydrolox" || id == "solid-apcp";

        static string PropName(Propellant p)
        {
            switch (p.id)
            {
                case "kerolox": return "Kerosene";
                case "methalox": return "Methane";
                case "hydrolox": return "Hydrogen";
                case "solid-apcp": return "Solid";
                default: return p.label ?? p.id;
            }
        }

        static string PatternName(string id, int count)
        {
            switch (id)
            {
                case "auto": return "Auto";
                case "custom": return "Custom";
                case "single": return "Single";
                case "row2": return "Pair";
                case "row3": return "Row of 3";
                case "ring7": return "Ring of 7";
                default: return count > 0 ? count + " ring" : id;
            }
        }

        /// <summary>"finish.bare_metal" or "decal_star_ring" → "Bare metal" / "Star ring". Labels are localisation keys today.</summary>
        public static string Words(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            string s = id;
            foreach (var pre in new[] { "decal_", "slot_" }) if (s.StartsWith(pre, StringComparison.Ordinal)) s = s.Substring(pre.Length);
            s = s.Replace('_', ' ');
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }
    }
}
