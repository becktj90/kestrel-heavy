// Step controls. The edit calls are the same HangarEditor and DesignerV3Edits paths the ten tabs use,
// so Guided and All parts change one design. Simple shows the step's one to three controls.
// Engineer adds a More section for the advanced fields.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using Kestrel.Slice;
using Kestrel.Slice.DesignerV3;
using UnityEngine.UIElements;

namespace Kestrel.Slice.DesignerV3.Stepper
{
    public static class StepperControls
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        static Campaign campaign;
        static bool campaignTried;

        public static void Build(DesignerStep step, bool engineer, bool moreOpen, Action toggleMore,
            VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers, Action<FlightRisk> onRisk)
        {
            var ui = bridge.Ui;
            switch (step)
            {
                case DesignerStep.Mission:
                    Mission(host, bridge, refreshers);
                    break;
                case DesignerStep.Engines:
                    Propellant(host, bridge, refreshers);
                    EngineRow(host, bridge, refreshers);
                    Cluster(host, bridge, refreshers);
                    Mixture(host, bridge, refreshers);
                    if (engineer) More(host, ui, moreOpen, toggleMore, "Chamber pressure, expansion, gimbal, and throttle", box =>
                    {
                        PressurePc(box, bridge, refreshers);
                        Expansion(box, bridge, refreshers);
                        Gimbal(box, bridge, refreshers);
                        Throttle(box, bridge, refreshers);
                    });
                    break;
                case DesignerStep.Tanks:
                    Fuel(host, bridge, refreshers);
                    Stages(host, bridge, refreshers);
                    Safety(host, bridge, refreshers);
                    if (engineer) More(host, ui, moreOpen, toggleMore, "Tank pressure, insulation, material, and avionics", box =>
                    {
                        TankPressure(box, bridge, refreshers);
                        Insulation(box, bridge, refreshers);
                        Material(box, bridge, refreshers);
                        Avionics(box, bridge, refreshers);
                    });
                    break;
                case DesignerStep.Aero:
                    Nose(host, bridge, refreshers);
                    Fins(host, bridge, refreshers);
                    Fairing(host, bridge, refreshers);
                    MaxQThrottle(host, bridge, refreshers);
                    if (engineer) More(host, ui, moreOpen, toggleMore, "g limit", box =>
                    {
                        GLimit(box, bridge, refreshers);
                    });
                    break;
                case DesignerStep.Recovery:
                    RecoveryMode(host, bridge, refreshers);
                    Legs(host, bridge, refreshers);
                    Reserve(host, bridge, refreshers, engineer);
                    break;
                case DesignerStep.Look:
                    Look(host, bridge, refreshers);
                    break;
                default:
                    Review(host, bridge, refreshers, onRisk, engineer);
                    break;
            }
        }

        public static bool IsEngineerField(string field)
        {
            switch (Canonical(field))
            {
                case "pc":
                case "expansionRatio":
                case "gimbal":
                case "throttle":
                case "pressure":
                case "insulation":
                case "material":
                case "avionics":
                case "gLimit":
                    return true;
                default:
                    return false;
            }
        }

        public static string Canonical(string field)
        {
            switch (field)
            {
                case "payload.type": return "payload";
                case "payload.orbit": return "orbit";
                case "engineParams.pc": return "pc";
                default: return field;
            }
        }

        static void More(VisualElement host, HangarUi ui, bool open, Action toggle, string tip, Action<VisualElement> fill)
        {
            var button = ui.Btn(open ? "Less" : "More", toggle, tip, minWidthPt: 72f);
            button.name = "stepper-more";
            host.Add(button);
            var box = ui.Column();
            box.name = "stepper-more-body";
            box.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            fill(box);
            host.Add(box);
        }

        static void Mission(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var ui = bridge.Ui;
            var choices = new List<HangarUi.Choice> { new HangarUi.Choice("free", "Free build", "No set mission. You pick the cargo.") };
            var book = CampaignBook();
            if (book != null)
            {
                for (int i = 0; i < book.Missions.Count; i++)
                {
                    var m = book.Missions[i];
                    if (m == null || string.IsNullOrEmpty(m.Id)) continue;
                    choices.Add(new HangarUi.Choice(m.Id, m.Name ?? m.Id, m.Name));
                }
            }
            var chips = ui.Chips("Mission", choices, () =>
            {
                string id = bridge.Design.Str("missionId");
                return string.IsNullOrEmpty(id) ? "free" : id;
            }, id => bridge.Apply(d => SetMission(bridge.Catalog, d, id, book), null), refreshers);
            chips.name = "field-mission";
            host.Add(chips);

            var payloads = new List<HangarUi.Choice>();
            foreach (var pl in bridge.Catalog.Payloads)
                payloads.Add(new HangarUi.Choice(pl.id, pl.label ?? pl.displayName ?? pl.id));
            var payload = ui.Chips("Payload", payloads, () => bridge.Design.Obj("payload")?.Str("type"),
                id => bridge.Apply(d => HangarEditor.SetPayloadType(bridge.Catalog, d, id), null), refreshers);
            payload.name = "field-payload";
            host.Add(payload);

            var orbits = new List<HangarUi.Choice>();
            foreach (var o in bridge.Catalog.Orbits.Values)
                orbits.Add(new HangarUi.Choice(o.id, o.label ?? o.id));
            var orbit = ui.Chips("Orbit", orbits, () => bridge.Design.Obj("payload")?.Str("orbit"),
                id => bridge.Apply(d => HangarEditor.SetOrbit(bridge.Catalog, d, id), null), refreshers);
            orbit.name = "field-orbit";
            host.Add(orbit);
        }

        static Edit SetMission(Catalog catalog, JsonObject d, string id, Campaign book)
        {
            string old = d.Str("missionId");
            if (id == "free" || string.IsNullOrEmpty(id))
            {
                if (string.IsNullOrEmpty(old) || old == "free") return Edit.None;
                d.Remove("missionId");
                d.Order.Remove("missionId");
                var mo = d.Obj("mission");
                if (mo != null && mo.ContainsKey("requiresLanding"))
                {
                    mo.Remove("requiresLanding");
                    mo.Order.Remove("requiresLanding");
                }
                return new Edit("Mission", old, "Free build");
            }
            CampaignMission mission = book?.ById(id);
            if (mission == null) return Edit.None;
            var box = d.Obj("mission");
            if (box == null) { box = new JsonObject(); JsonMut.Set(d, "mission", box); }
            JsonMut.Set(box, "id", mission.Id);
            JsonMut.Set(box, "requiresLanding", mission.LandingRequired);
            JsonMut.Set(d, "missionId", mission.Id);
            if (!string.IsNullOrEmpty(mission.PayloadId) && catalog.PayloadById.ContainsKey(mission.PayloadId))
                HangarEditor.SetPayloadType(catalog, d, mission.PayloadId);
            if (mission.MassKg > 0) HangarEditor.SetPayloadMass(d, mission.MassKg);
            return new Edit("Mission", string.IsNullOrEmpty(old) ? "Free build" : old, mission.Name ?? mission.Id);
        }

        static void Propellant(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var stage = Stage0(bridge.Design);
            string current = stage?.Str("propellant");
            var choices = new List<HangarUi.Choice>();
            foreach (var id in HangarEditor.PropellantChoices(bridge.Catalog, current))
            {
                bridge.Catalog.PropellantById.TryGetValue(id, out var p);
                choices.Add(new HangarUi.Choice(id, Family(id, p?.label), p?.label));
            }
            var chips = bridge.Ui.Chips("Propellant", choices, () => Stage0(bridge.Design)?.Str("propellant"),
                id => bridge.Apply(d => HangarEditor.SetPropellant(bridge.Catalog, d, PartRef.Stage(0), id), null), refreshers);
            chips.name = "field-propellant";
            host.Add(chips);
        }

        static void EngineRow(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var p = PartRef.Stage(0);
            var row = bridge.Ui.Stepper("Engine", () => EngineLabel(bridge), () => "Minus and plus change how many. Tap the name for another engine.",
                (dir, key) => bridge.Apply(d => HangarEditor.SetEngineCount(d, p, (int)(HangarParts.Body(d, p)?.Num("engineCount") ?? 1) + dir), key),
                bridge.EndGesture, "eng.count", refreshers, "Stage 1 engines");
            row.name = "field-engineCount";
            var labels = row.Query<Label>().ToList();
            if (labels.Count > 1)
            {
                labels[1].name = "field-engineCount-value";
                labels[1].pickingMode = PickingMode.Position;
                labels[1].RegisterCallback<ClickEvent>(_ => CycleEngine(bridge, 1));
            }
            host.Add(row);
        }

        static void CycleEngine(DesignerStepperBridge bridge, int dir)
        {
            var choices = HangarEditor.EngineChoices(bridge.Catalog, bridge.Design, PartRef.Stage(0));
            if (choices.Count == 0) return;
            string current = Stage0(bridge.Design)?.Str("engine");
            int at = 0;
            for (int i = 0; i < choices.Count; i++) if (choices[i].id == current) { at = i; break; }
            int next = (at + dir) % choices.Count;
            if (next < 0) next += choices.Count;
            bridge.Apply(d => HangarEditor.SetEngine(bridge.Catalog, d, PartRef.Stage(0), choices[next].id), null);
        }

        static string EngineLabel(DesignerStepperBridge bridge)
        {
            string id = Stage0(bridge.Design)?.Str("engine");
            bridge.Catalog.EngineById.TryGetValue(id ?? "", out var engine);
            int count = (int)(Stage0(bridge.Design)?.Num("engineCount") ?? 1);
            return HangarFormat.EngineChip(engine) + " × " + count.ToString(Inv);
        }

        static void Cluster(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var choices = new List<HangarUi.Choice>();
            foreach (var pl in DesignerLimits.Active.patterns)
                choices.Add(new HangarUi.Choice(pl.id, pl.id));
            var chips = bridge.Ui.Chips("Cluster", choices, () => Stage0(bridge.Design)?.Obj("cluster")?.Str("pattern") ?? "auto",
                id => bridge.Apply(d => HangarEditor.SetClusterPattern(d, PartRef.Stage(0), id), null), refreshers);
            chips.name = "field-cluster";
            host.Add(chips);
        }

        static void Mixture(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var row = bridge.Ui.Stepper("Mixture offset",
                () => Signed(Stage0(bridge.Design)?.Num("mixtureOffsetPct") ?? 0, "0.0") + " %",
                () => "Keep this near zero for an even burn",
                (dir, key) => bridge.Apply(d => DesignerKnobs.NudgeMixture(d, 0, dir), key),
                bridge.EndGesture, "mix", refreshers);
            NameValue(row, "mixtureOffsetPct");
            host.Add(row);
        }

        static void PressurePc(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var row = bridge.Ui.Stepper("Chamber pressure",
                () => DesignerV3Edits.ShownPc(bridge.Catalog, bridge.Design, 0).ToString("0", Inv) + " bar",
                () => "Higher pressure: more push, and a chance of popping an engine",
                (dir, key) => bridge.Apply(d => DesignerKnobs.NudgePc(bridge.Catalog, d, 0, dir), key),
                bridge.EndGesture, "eng.pc", refreshers);
            NameValue(row, "pc");
            host.Add(row);
        }

        static void Expansion(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var row = bridge.Ui.Stepper("Expansion ratio",
                () => DesignerV3Edits.ShownExpansion(bridge.Catalog, bridge.Design, 0).ToString("0", Inv),
                () => "Bigger nozzle: better in space, worse on the pad",
                (dir, key) => bridge.Apply(d => DesignerKnobs.NudgeExpansion(bridge.Catalog, d, 0, dir), key),
                bridge.EndGesture, "eng.eps", refreshers);
            NameValue(row, "expansionRatio");
            host.Add(row);
        }

        static void Gimbal(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var row = bridge.Ui.Stepper("Gimbal",
                () => DesignerV3Edits.ShownGimbal(bridge.Catalog, bridge.Design, 0).ToString("0.0", Inv) + "°",
                () => "How far the engines can steer",
                (dir, key) => bridge.Apply(d => DesignerKnobs.NudgeGimbal(bridge.Catalog, d, 0, dir), key),
                bridge.EndGesture, "eng.gimbal", refreshers);
            NameValue(row, "gimbal");
            host.Add(row);
        }

        static void Throttle(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var row = bridge.Ui.Stepper("Min throttle",
                () => (DesignerKnobs.ShownThrottle(bridge.Catalog, bridge.Design, 0) * 100).ToString("0", Inv) + "%",
                () => "How far the engines can ease off",
                (dir, key) => bridge.Apply(d => DesignerKnobs.NudgeThrottle(bridge.Catalog, d, 0, dir), key),
                bridge.EndGesture, "eng.thr", refreshers);
            NameValue(row, "throttle");
            host.Add(row);
        }

        static void Fuel(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var p = PartRef.Stage(0);
            var row = bridge.Ui.Stepper("Fuel load", () => HangarFormat.Tons(Stage0(bridge.Design)?.Num("propMass") ?? 0),
                () => "More fuel goes farther but weighs more",
                (dir, key) => bridge.Apply(d => HangarEditor.SetPropMass(d, p, HangarFormat.StepProp(HangarParts.Body(d, p)?.Num("propMass") ?? 0, dir)), key),
                bridge.EndGesture, "tank.prop", refreshers);
            NameValue(row, "propMass");
            host.Add(row);
        }

        static void Stages(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var row = bridge.Ui.Stepper("Stages", () => (bridge.Design.Arr("stages")?.Count ?? 0).ToString(Inv),
                () => "Each stage drops empty weight on the way up",
                (dir, key) => bridge.Apply(d => StepStages(bridge.Catalog, d, dir), key),
                bridge.EndGesture, "stages.count", refreshers);
            NameValue(row, "stages");
            host.Add(row);
        }

        static Edit StepStages(Catalog catalog, JsonObject d, int dir)
        {
            int n = d.Arr("stages")?.Count ?? 0;
            if (dir > 0) return HangarEditor.AddStage(catalog, d, n);
            if (dir < 0) return HangarEditor.RemoveStage(d, n - 1);
            return Edit.None;
        }

        static void Safety(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var row = bridge.Ui.Stepper("Strength (safety factor)",
                () => bridge.Design.Num("safetyFactor", 1.4).ToString("0.00", Inv),
                () => "Stronger walls bend less and weigh more",
                (dir, key) => bridge.Apply(d => HangarEditor.SetSafetyFactor(d, d.Num("safetyFactor", 1.4) + dir * DesignerLimits.Active.safetyStep), key),
                bridge.EndGesture, "sf", refreshers);
            NameValue(row, "safetyFactor");
            host.Add(row);
        }

        static void TankPressure(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var row = bridge.Ui.Stepper("Pressure",
                () => (Stage0(bridge.Design)?.Obj("tank")?.Num("pressureBar") ?? 3).ToString("0.0", Inv) + " bar",
                () => "Higher pressure needs a thicker wall",
                (dir, key) => bridge.Apply(d => DesignerKnobs.NudgePressure(d, 0, dir), key),
                bridge.EndGesture, "tank.p", refreshers);
            NameValue(row, "pressure");
            host.Add(row);
        }

        static void Insulation(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var choices = new List<HangarUi.Choice>
            {
                new HangarUi.Choice("none", "None"), new HangarUi.Choice("foam", "Foam"), new HangarUi.Choice("mli", "MLI"),
            };
            var chips = bridge.Ui.Chips("Insulation", choices, () => Stage0(bridge.Design)?.Obj("tank")?.Str("insulation") ?? "none",
                id => bridge.Apply(d => DesignerV3Edits.SetInsulation(d, 0, id), null), refreshers);
            chips.name = "field-insulation";
            host.Add(chips);
        }

        static void Material(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var choices = new List<HangarUi.Choice>();
            foreach (var m in bridge.Catalog.Materials) choices.Add(new HangarUi.Choice(m.id, m.label ?? m.id));
            var chips = bridge.Ui.Chips("Material", choices, () => Stage0(bridge.Design)?.Str("material"),
                id => bridge.Apply(d => HangarEditor.SetMaterial(bridge.Catalog, d, PartRef.Stage(0), id), null), refreshers);
            chips.name = "field-material";
            host.Add(chips);
        }

        static void Avionics(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var choices = new List<HangarUi.Choice>();
            foreach (var id in HangarEditor.AvionicsTiers) choices.Add(new HangarUi.Choice(id, HangarFormat.Cap(id)));
            var chips = bridge.Ui.Chips("Avionics", choices, () => bridge.Design.Obj("avionics")?.Str("tier"),
                id => bridge.Apply(d => HangarEditor.SetAvionics(d, id), null), refreshers);
            chips.name = "field-avionics";
            host.Add(chips);
        }

        static void Nose(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var choices = new List<HangarUi.Choice>();
            foreach (var id in DesignerKnobs.Noses) choices.Add(new HangarUi.Choice(id, HangarFormat.Cap(id)));
            var chips = bridge.Ui.Chips("Nose", choices, () => bridge.Design.Obj("aero")?.Str("nose") ?? "ogive",
                id => bridge.Apply(d => DesignerKnobs.SetNose(d, id), null), refreshers);
            chips.name = "field-nose";
            host.Add(chips);
        }

        static void Fins(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var box = bridge.Ui.Column();
            box.name = "field-fins";
            box.Add(bridge.Ui.Switch("Fins", "Keep the pointy end up", () => bridge.Design.Obj("aero")?.Bool("fins") ?? false,
                on => bridge.Apply(d => HangarEditor.SetFins(d, on), null), refreshers));
            if (bridge.Design.Obj("aero")?.Bool("fins") == true)
            {
                var count = bridge.Ui.Stepper("Fin count", () => ((int)(bridge.Design.Obj("aero")?.Num("finCount", 4) ?? 4)).ToString(Inv), () => null,
                    (dir, key) => bridge.Apply(d => HangarEditor.SetFinCount(d, (int)(d.Obj("aero")?.Num("finCount", 4) ?? 4) + dir), key),
                    bridge.EndGesture, "fins.count", refreshers);
                count.name = "field-finCount";
                box.Add(count);
                var span = bridge.Ui.Stepper("Fin size", () => (bridge.Design.Obj("aero")?.Num("finSpanM", 1) ?? 1).ToString("0.0", Inv) + " m",
                    () => "Bigger fins hold the point up",
                    (dir, key) => bridge.Apply(d => HangarEditor.SetFinSpan(d, (d.Obj("aero")?.Num("finSpanM", 1) ?? 1) + dir * DesignerLimits.Active.finSpanStep), key),
                    bridge.EndGesture, "fins.span", refreshers);
                span.name = "field-finSpan";
                box.Add(span);
            }
            host.Add(box);
        }

        static void Fairing(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var row = bridge.Ui.Stepper("Fairing size", FairingText, () => "Wide and long enough for the cargo",
                (dir, key) => bridge.Apply(d => HangarEditor.SetFairingLength(d, (d.Obj("fairing")?.Num("length") ?? 1) + dir * 0.5), key),
                bridge.EndGesture, "fair.l", refreshers, "Fairing length");
            NameValue(row, "fairing");
            host.Add(row);
            var width = bridge.Ui.Stepper("Fairing width",
                () => HangarFormat.Metres(bridge.Design.Obj("fairing")?.Num("diameter") ?? 0),
                () => "Wide enough for the cargo",
                (dir, key) => bridge.Apply(d => HangarEditor.SetDiameter(d, PartRef.Fairing, (d.Obj("fairing")?.Num("diameter") ?? 1) + dir * 0.1), key),
                bridge.EndGesture, "fair.d", refreshers);
            NameValue(width, "fairingDiameter");
            host.Add(width);
            string FairingText()
            {
                var f = bridge.Design.Obj("fairing");
                return HangarFormat.Metres(f?.Num("diameter") ?? 0) + " × " + HangarFormat.Metres(f?.Num("length") ?? 0);
            }
        }

        static void MaxQThrottle(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var row = bridge.Ui.Stepper("Max-Q throttle",
                () => Kestrel.Core.Designer.Stepper.MaxQThrottle.Label(bridge.Design),
                () => "Ease off while the air pushes hardest",
                (dir, key) => bridge.Apply(d => Kestrel.Core.Designer.Stepper.MaxQThrottle.Nudge(d, dir), key),
                bridge.EndGesture, "g.q", refreshers);
            NameValue(row, "maxQThrottle");
            host.Add(row);
        }

        static void GLimit(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var row = bridge.Ui.Stepper("g limit",
                () => (bridge.Design.Obj("guidance")?.Num("gLimit") ?? 4).ToString("0.0", Inv) + " g",
                () => "The autopilot holds the push under this",
                (dir, key) => bridge.Apply(d => DesignerKnobs.NudgeGuidance(d, "gLimit", dir), key),
                bridge.EndGesture, "g.g", refreshers);
            NameValue(row, "gLimit");
            host.Add(row);
        }

        static void RecoveryMode(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var choices = new List<HangarUi.Choice>
            {
                new HangarUi.Choice("expendable", "Expend"),
                new HangarUi.Choice("rtls", "Pad"),
                new HangarUi.Choice("barge", "Haven barge"),
            };
            var chips = bridge.Ui.Chips("Where it comes down", choices,
                () => StepperChecks.RecoveryMode(bridge.Design, null),
                id => bridge.Apply(d => DesignerV3Edits.SetRecoveryMode(d, id), null), refreshers);
            chips.name = "field-recoveryMode";
            host.Add(chips);
        }

        static void Legs(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var row = bridge.Ui.Switch("Landing legs", "Needed if it comes home",
                () => bridge.Design.Obj("landing")?.Bool("legs") ?? false,
                on => bridge.Apply(d => HangarEditor.SetLegs(d, on), null), refreshers);
            row.name = "field-legs";
            host.Add(row);
        }

        static void Reserve(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers, bool engineer)
        {
            var row = bridge.Ui.Stepper("Landing reserve", ReserveText, () => TradeHint(bridge, engineer),
                (dir, key) => bridge.Apply(d => HangarEditor.SetLandingReserve(d, 0, ReserveFraction(Stage0(d)) + dir * 0.01), key),
                bridge.EndGesture, "land.reserve", refreshers);
            NameValue(row, "landingReserve");
            host.Add(row);
            string ReserveText() => (ReserveFraction(Stage0(bridge.Design)) * 100).ToString("0", Inv) + "%";
        }

        static string TradeHint(DesignerStepperBridge bridge, bool engineer)
        {
            try
            {
                if (bridge == null || bridge.Catalog == null || bridge.Design == null) return "Fuel held back for the trip home";
                var trade = RecoveryTradeoff.Measure(bridge.Catalog, Design.FromJson(bridge.Design));
                string line = engineer ? trade.Engineer : trade.Kid;
                return string.IsNullOrEmpty(line) ? "Fuel held back for the trip home" : line;
            }
            catch (Exception)
            {
                return "Fuel held back for the trip home";
            }
        }

        static double ReserveFraction(JsonObject stage)
        {
            if (stage == null) return 0;
            double prop = stage.Num("propMass");
            if (!(prop > 0)) return 0;
            return stage.Num("landingPropMass") / prop;
        }

        static void Look(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers)
        {
            var ui = bridge.Ui;
            var palette = bridge.Palette;
            if (palette == null || palette.Count == 0)
                palette = new List<string> { "#F2F2F2", "#1EC8FF", "#1A3C6E", "#E8EEF5", "#F0C14A", "#C45A48" };
            var paint = ui.Swatches("Paint", palette, () => bridge.Design.Obj("livery")?.Str("primary") ?? "#F2F2F2",
                hex => bridge.Apply(d => HangarEditor.SetPaint(d, hex, null), null), refreshers);
            paint.name = "field-paint";
            host.Add(paint);
            var accent = ui.Swatches("Accent", palette, () => bridge.Design.Obj("livery")?.Str("accent") ?? "#1EC8FF",
                hex => bridge.Apply(d => HangarEditor.SetPaint(d, null, hex), null), refreshers);
            accent.name = "field-accent";
            host.Add(accent);

            var name = new TextField { value = VehicleName(bridge.Design), maxLength = HangarEditor.VehicleNameMax, isDelayed = true };
            name.label = "Name";
            name.name = "field-name";
            name.tooltip = "The name painted on the rocket. This does not change the flight.";
            name.style.fontSize = ui.U(HangarUi.BodyPt);
            name.style.minHeight = ui.U(HangarUi.TargetPt);
            name.RegisterValueChangedCallback(e => bridge.Apply(d => HangarEditor.SetVehicleName(d, e.newValue), null));
            host.Add(name);
            refreshers.Add(() =>
            {
                string now = VehicleName(bridge.Design);
                if (name.value != now) name.SetValueWithoutNotify(now);
            });
        }

        static void Review(VisualElement host, DesignerStepperBridge bridge, List<Action> refreshers, Action<FlightRisk> onRisk, bool engineer)
        {
            var ui = bridge.Ui;
            string seed = bridge.Design.Str("seed") ?? bridge.Design.Obj("mission")?.Str("seed") ?? bridge.Design.Str("missionSeed");
            var caption = ui.Text("Pre-flight" + (string.IsNullOrEmpty(seed) ? "" : " · seed " + seed), HangarUi.SmallPt, HangarUi.Dim, bold: true);
            caption.name = "stepper-seed";
            host.Add(caption);
            var trade = ui.Text(TradeHint(bridge, engineer), HangarUi.BodyPt, HangarUi.Dim);
            trade.name = "recovery-tradeoff";
            trade.style.whiteSpace = WhiteSpace.Normal;
            host.Add(trade);
            refreshers.Add(() => trade.text = TradeHint(bridge, engineer));

            QuickReadout readout = null;
            try { readout = Readout.Quick(bridge.Catalog, bridge.Design); }
            catch (Exception) { readout = null; }
            var risks = readout?.risks;
            if (risks == null || risks.Count == 0)
            {
                host.Add(ui.Text("No risk rows yet. The check below is the latest gate.", HangarUi.BodyPt, HangarUi.Dim));
            }
            else
            {
                for (int i = 0; i < risks.Count; i++)
                {
                    var risk = risks[i];
                    if (risk == null) continue;
                    var captured = risk;
                    string words = engineer && !string.IsNullOrEmpty(risk.engineer) ? risk.engineer : (risk.kid ?? risk.kind ?? "Risk");
                    string mark = risk.level == "red" ? "✕" : risk.level == "amber" ? "!" : "✓";
                    var button = ui.Btn(mark + "  " + words, () => onRisk?.Invoke(captured), "Tap to open the part that changes this", minWidthPt: 120f);
                    button.name = "risk-" + (risk.kind ?? "x");
                    button.style.whiteSpace = WhiteSpace.Normal;
                    button.style.height = StyleKeyword.Auto;
                    button.style.minHeight = ui.U(HangarUi.TargetPt);
                    if (risk.level == "red") button.style.backgroundColor = HangarUi.Red;
                    else if (risk.level == "amber") button.style.backgroundColor = HangarUi.Amber;
                    if (risk.level == "red" || risk.level == "amber") button.style.color = HangarUi.Ink;
                    host.Add(button);
                }
            }

            host.Add(ui.Section("Bonus objectives"));
            var bonuses = bridge.Design.Arr("bonuses") ?? bridge.Design.Obj("mission")?.Arr("bonuses");
            if (bonuses == null || bonuses.Count == 0)
            {
                var empty = ui.Text("No bonus goals on this flight.", HangarUi.BodyPt, HangarUi.Dim);
                empty.name = "stepper-bonuses";
                host.Add(empty);
            }
            else
            {
                var box = ui.Column();
                box.name = "stepper-bonuses";
                for (int i = 0; i < bonuses.Count; i++)
                {
                    string line = BonusLine(bonuses[i]);
                    if (!string.IsNullOrEmpty(line)) box.Add(ui.Text("★  " + line, HangarUi.BodyPt));
                }
                host.Add(box);
            }
        }

        static string BonusLine(object item)
        {
            if (item is string s) return s;
            if (item is JsonObject o)
            {
                string label = o.Str("label") ?? o.Str("kid") ?? o.Str("name") ?? o.Str("id");
                string state = o.Str("state") ?? o.Str("status");
                if (string.IsNullOrEmpty(label)) return null;
                return string.IsNullOrEmpty(state) ? label : label + " (" + state + ")";
            }
            return null;
        }

        static void NameValue(VisualElement row, string id)
        {
            row.name = "field-" + id;
            var labels = row.Query<Label>().ToList();
            if (labels.Count > 1) labels[1].name = "field-" + id + "-value";
        }

        static JsonObject Stage0(JsonObject design)
        {
            var stages = design?.Arr("stages");
            return stages != null && stages.Count > 0 ? stages[0] as JsonObject : null;
        }

        static string VehicleName(JsonObject design)
        {
            string name = design?.Obj("livery")?.Str("name");
            return string.IsNullOrEmpty(name) ? design?.Str("name") ?? "" : name;
        }

        static string Family(string id, string label)
        {
            switch (id)
            {
                case "kerolox": return "Kerolox";
                case "methalox": return "Methalox";
                case "hydrolox": return "Hydrolox";
                default: return label ?? id;
            }
        }

        static string Signed(double value, string format)
        {
            string n = value.ToString(format, Inv);
            return value > 0 ? "+" + n : n;
        }

        public static Campaign SharedCampaign() => CampaignBook();

        /// <summary>Drop the cached campaign so a later test with a real checkout can load it.</summary>
        public static void ResetShared()
        {
            campaign = null;
            campaignTried = false;
        }

        static Campaign CampaignBook()
        {
            if (campaignTried) return campaign;
            campaignTried = true;
            try
            {
                string root = Catalog.RepoRootOrNull();
                if (!string.IsNullOrEmpty(root)) campaign = Campaign.Load(root);
            }
            catch (Exception) { campaign = null; }
            if (campaign != null) return campaign;
            try
            {
                var asset = UnityEngine.Resources.Load<UnityEngine.TextAsset>("KestrelData/campaign");
                if (asset != null && !string.IsNullOrWhiteSpace(asset.text))
                    campaign = Campaign.FromJson(MiniJson.ParseObject(asset.text));
            }
            catch (Exception) { campaign = null; }
            return campaign;
        }
    }
}
