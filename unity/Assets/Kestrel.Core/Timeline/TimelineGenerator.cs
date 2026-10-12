// Pure timeline generator (docs/MISSION_EVENTS.md §2–§3).
// Generate(vehicle, pad, mission, seed) → ordered MissionEvents.
// No Unity types, no clock, no IO. Ascent times come from Headless.Simulate.
using System;
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Economy;
using Kestrel.Core.Params;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Timeline
{
    public static class TimelineGenerator
    {
        public static Timeline Generate(Catalog catalog, Design vehicle, Pad pad, Mission mission, uint seed)
            => Generate(catalog, vehicle, pad, mission, seed, null);

        /// <summary>
        /// When mission.latitude is null, latitude comes from registry.LaunchLatitude (data/params.json flight.launchLatitude).
        /// An explicit mission latitude still wins. A null registry falls back to Stack.LAUNCH_LATITUDE.
        /// </summary>
        public static Timeline Generate(Catalog catalog, Design vehicle, Pad pad, Mission mission, uint seed, Registry registry)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (vehicle == null) throw new ArgumentNullException(nameof(vehicle));
            if (vehicle.stages == null || vehicle.stages.Count == 0) throw new ArgumentException("vehicle has no stages");
            var b = new Builder(catalog, vehicle, pad ?? Pad.For(catalog, vehicle), mission ?? new Mission(), seed, registry);
            return b.Run();
        }

        public static Timeline Generate(Catalog catalog, Design vehicle, Pad pad, Mission mission)
            => Generate(catalog, vehicle, pad, mission, 1);

        sealed class Part
        {
            public string key, name, family, group;
            public int designIndex;
            public bool upper;
            public StagePart part;
            public Propellant prop;
            public Engine engine;
            public long propCost;
        }

        sealed class Item
        {
            public MissionEvent Ev;
            public int Seq;
        }

        sealed class Builder
        {
            readonly Catalog c;
            readonly Design d;
            readonly Pad pad;
            readonly Mission mission;
            readonly uint seed;
            readonly string mode;
            readonly double latitude;
            readonly List<Part> parts = new List<Part>();
            readonly List<Item> items = new List<Item>();
            readonly List<MissionEvent> branches = new List<MissionEvent>();
            readonly Dictionary<string, string> absent = new Dictionary<string, string>();
            readonly HashSet<string> present = new HashSet<string>();
            readonly HashSet<string> hardware = new HashSet<string>();
            readonly HashSet<string> families = new HashSet<string>();
            readonly Dictionary<string, MissionEvent> branchById = new Dictionary<string, MissionEvent>();
            int seq;
            string recovery;
            bool transfer, suborbital, crewed, needsAbort, hasFairing, throttleable;
            int liftoffEngines;
            long propRefund;
            string orbitLabel = "";

            public Builder(Catalog catalog, Design vehicle, Pad pad, Mission mission, uint seed, Registry registry)
            {
                c = catalog; d = vehicle; this.pad = pad; this.mission = mission; this.seed = seed;
                mode = TimelineClock.NormalizeMode(mission.mode);
                latitude = mission.latitude ?? registry?.LaunchLatitude ?? Stack.LAUNCH_LATITUDE;
            }

            public Timeline Run()
            {
                var req = Stack.RequiredDeltaV(c, d.payloadOrbit, latitude);
                orbitLabel = req.orbit?.label ?? (d.payloadOrbit ?? "orbit");
                suborbital = req.orbit != null && req.orbit.periKm < 0;
                transfer = req.orbit != null && !suborbital && req.orbit.apoKm > req.orbit.periKm + 1000;
                Payload payload = null;
                if (!string.IsNullOrEmpty(d.payloadType)) c.PayloadById.TryGetValue(d.payloadType, out payload);
                crewed = payload != null && payload.crewed;
                needsAbort = payload != null && (payload.needsAbort || payload.crewed);
                hasFairing = d.fairingDiameter > 0 && d.fairingLength > 0;
                recovery = ResolveRecovery();
                BuildParts();
                BuildHardware();
                var quote = CostModel.PriceDesign(c, d);
                propRefund = 0;
                foreach (var line in quote.lines) if (line.cat == "propellant") propRefund += line.amount;
                foreach (var p in parts)
                {
                    long cost = 0;
                    foreach (var line in quote.lines) if (line.cat == "propellant" && line.group == p.group) cost += line.amount;
                    p.propCost = cost;
                }

                var serial = Stack.SerialStages(c, d);
                var simStages = new List<SimStage>(serial.stages.Count);
                foreach (var s in serial.stages) simStages.Add(s.ToSim());
                var opts = new SimOptions
                {
                    payload = serial.payload, targetAlt = req.targetAlt, latitude = latitude,
                    // Burnout still lands on the propellant boundary. A 1 s step keeps a
                    // debug-build generate under a few milliseconds. Max-Q tolerance is
                    // 5 s, so the peak step stays inside the band.
                    dt = 1, sampleEvery = 2, tMax = 2500,
                };
                var sim = Headless.Simulate(simStages, opts);

                EmitCountdown();
                EmitFlight(serial.stages, sim);
                EmitRecovery();
                EmitBranches();
                EmitFamilyAbsences();
                return Finish(sim);
            }

            void BuildParts()
            {
                if (d.StrapOnCount > 0) parts.Add(Make(d.strapOns, "so", d.strapOns.name ?? "Boosters", -1, false, "strapons"));
                for (int i = 0; i < d.stages.Count; i++)
                {
                    var s = d.stages[i];
                    parts.Add(Make(s, "s" + (i + 1), string.IsNullOrEmpty(s.name) ? "Stage " + (i + 1) : s.name, i, i > 0, "stage" + i));
                }
                liftoffEngines = 0;
                throttleable = false;
                foreach (var p in parts)
                {
                    if (p.upper) continue;
                    int n = p.part.engineCount * (p.designIndex < 0 ? Math.Max(1, p.part.count) : 1);
                    liftoffEngines += n;
                    if (p.engine.throttle != null && p.engine.throttle.Length >= 2 && p.engine.throttle[1] - p.engine.throttle[0] > 0.02)
                        throttleable = true;
                }
                if (liftoffEngines < 1) liftoffEngines = 1;
            }

            Part Make(StagePart part, string key, string name, int index, bool upper, string group)
            {
                if (!c.PropellantById.TryGetValue(part.propellant ?? "", out var prop))
                    throw new ArgumentException("unknown propellant " + part.propellant);
                if (!c.EngineById.TryGetValue(part.engine ?? "", out var engine))
                    throw new ArgumentException("unknown engine " + part.engine);
                var family = FamilyOf(prop);
                families.Add(family);
                return new Part { key = key, name = name, family = family, group = group, designIndex = index, upper = upper, part = part, prop = prop, engine = engine };
            }

            static string FamilyOf(Propellant p)
            {
                if (p.storability == "solid") return "solid";
                if (p.id == "hybrid" || (p.fuel == "HTPB" && p.oxidizer == "N2O")) return "hybrid";
                if (p.id == "hydrolox" || p.fuel == "LH2") return "hydrolox";
                if (p.id == "methalox" || p.fuel == "CH4") return "methalox";
                if (p.storability == "storable" && (p.fuel == "UDMH" || p.fuel == "MMH" || p.oxidizer == "N2O4")) return "hypergolic";
                return "kerolox";
            }

            void BuildHardware()
            {
                hardware.Add("vehicle");
                if (pad.crewArms) hardware.Add("crewArms");
                if (pad.strongback) hardware.Add("strongback");
                if (pad.umbilical) hardware.Add("umbilical");
                if (pad.deluge) hardware.Add("deluge");
                if (pad.internalPower) hardware.Add("internalPower");
                if (hasFairing) hardware.Add("fairing");
                if (d.legs) hardware.Add("legs");
                if (d.gridFins) hardware.Add("gridFins");
                if (d.rcs) hardware.Add("rcs");
                if (d.payloadMass > 0) hardware.Add("payload");
                if (crewed) hardware.Add("crew");
                if (needsAbort) hardware.Add("abortSystem");
                if (mission.stageParachute) hardware.Add("parachute:stage");
                if (mission.fairingParachute) hardware.Add("parachute:fairing");
                foreach (var p in parts)
                {
                    hardware.Add("stage:" + p.key);
                    hardware.Add("engine:" + p.engine.id);
                    hardware.Add("propellant:" + p.prop.id);
                    if (p.part.landingPropMass > 0) hardware.Add("landingProp:" + p.key);
                }
            }

            string ResolveRecovery()
            {
                string asked = mission.recovery;
                if (!string.IsNullOrEmpty(asked))
                {
                    if (asked == "expendable" || asked == "droneship" || asked == "rtls" || asked == "parachute") return asked;
                }
                var s0 = d.stages[0];
                if (!(s0.landingPropMass > 0) || !d.legs) return "expendable";
                if (!c.EngineById.TryGetValue(s0.engine ?? "", out var e) || !e.restart || e.restarts < 2) return "expendable";
                return "droneship";
            }

            void EmitCountdown()
            {
                var early = AddReal("term.hold.early", "Built-in hold", "T-2h hold", TimelinePhase.Terminal,
                    TimelineClock.RealHoldEarly, TimelineCriticality.Safety, TimelineVisibility.Engineer,
                    "Built-in readiness hold at real T-2 h. The count is allowed to stop here for a poll. " + Note(TimelineClock.RealHoldEarly),
                    null, Hw("vehicle"), true, TimelineTriggerFrom(TimelinePhase.Terminal));

                bool anyClock = false;
                foreach (var p in parts) if (OnClock(p) && pad.umbilical) anyClock = true;
                MissionEvent go = null;
                if (anyClock)
                {
                    go = AddReal("count.prop.go", "Go for propellant load", "Go prop", TimelinePhase.Load,
                        TimelineClock.RealPropGo, TimelineCriticality.Critical, TimelineVisibility.Standard,
                        "Go for propellant load. The count anchor is real T-38:00, before fuel and oxidizer flow. " + Note(TimelineClock.RealPropGo),
                        new[] { early.id }, Hw("vehicle", "umbilical"), true, "padLoadData");
                }
                else Absent("count.prop.go", "Go for propellant load is absent because nothing on this vehicle loads propellant on the clock. Solids are armed, hypergolics are loaded off-clock, or the pad has no umbilical.");

                string loadParent = go != null ? go.id : early.id;
                foreach (var p in parts) EmitLoad(p, loadParent);

                var hold = AddReal("term.hold.late", "Built-in hold", "T-4m hold", TimelinePhase.Terminal,
                    TimelineClock.RealHoldLate, TimelineCriticality.Safety, TimelineVisibility.Standard,
                    "Built-in readiness hold at real T-4 min, with a poll before the terminal count. " + Note(TimelineClock.RealHoldLate),
                    new[] { loadParent }, Hw("vehicle"), true, TimelineTriggerFrom(TimelinePhase.Terminal));

                MissionEvent strongback = null;
                if (pad.strongback)
                    strongback = AddReal("pad.strongback", "Strongback retract", "Strongback", TimelinePhase.Pad, -180,
                        TimelineCriticality.Nominal, TimelineVisibility.Standard,
                        "The strongback retracts because this pad has one. A pad without a strongback does not get this event. " + Note(-180),
                        new[] { hold.id }, Hw("strongback"), false, TimelineTriggerFrom(TimelinePhase.Pad));
                else Absent("pad.strongback", "Strongback retract is absent because pad " + pad.id + " has no strongback.");

                string padParent = strongback != null ? strongback.id : hold.id;
                MissionEvent arms = null;
                if (pad.crewArms)
                    arms = AddReal("pad.arms", "Access arms retract", "Arms back", TimelinePhase.Pad, -130,
                        TimelineCriticality.Nominal, TimelineVisibility.Standard,
                        "Crew and access arms retract because this pad has them. " + Note(-130),
                        new[] { padParent }, Hw("crewArms"), false, TimelineTriggerFrom(TimelinePhase.Pad));
                else Absent("pad.arms", "Access-arm retract is absent because pad " + pad.id + " has no crew or access arms.");

                var poll = AddReal("term.poll", "Go/no-go poll", "Go/no-go", TimelinePhase.Terminal, -100,
                    TimelineCriticality.Safety, TimelineVisibility.Standard,
                    "Holdable go/no-go poll. A no-go scrubs. An unplanned hold is status 'held' on this event, not a second event. " + Note(-100),
                    new[] { arms != null ? arms.id : padParent }, Hw("vehicle"), true, TimelineTriggerFrom(TimelinePhase.Terminal));

                foreach (var p in parts) if (OnClock(p) && pad.umbilical) EmitPress(p, poll.id);

                var count = AddReal("term.count", "Terminal count", "Terminal", TimelinePhase.Terminal,
                    TimelineClock.RealGoLaunch, TimelineCriticality.Critical, TimelineVisibility.Kid,
                    "Terminal count and go for launch. The anchor is real T-0:45. " + Note(TimelineClock.RealGoLaunch),
                    new[] { poll.id }, Hw("vehicle"), false, TimelineTriggerFrom(TimelinePhase.Terminal),
                    narration: "terminal", music: "CUE-06");

                var seqr = AddReal("term.sequencer", "Auto-sequencer", "Sequencer", TimelinePhase.Terminal, -20,
                    TimelineCriticality.Critical, TimelineVisibility.Engineer,
                    "The auto-sequencer arms and runs the commit steps from here to release. " + Note(-20),
                    new[] { count.id }, Hw("vehicle"), false, TimelineTriggerFrom(TimelinePhase.Terminal));

                MissionEvent prev = seqr;
                if (pad.internalPower)
                    prev = AddReal("pad.internal", "Internal power", "Internal", TimelinePhase.Pad, -30,
                        TimelineCriticality.Nominal, TimelineVisibility.Standard,
                        "The vehicle is on internal power because this pad provides that handover. " + Note(-30),
                        new[] { seqr.id }, Hw("internalPower"), false, TimelineTriggerFrom(TimelinePhase.Pad),
                        narration: "internal", music: "CUE-06");
                else Absent("pad.internal", "Internal power is absent because pad " + pad.id + " has no internal-power handover.");

                // -30 is before -20. The precondition still points at the sequencer, which is later.
                // Fix: internal power is a pad step before the sequencer. Re-parent below by time sort;
                // the check requires preconditions to precede, so internal must not depend on the sequencer.
                if (pad.internalPower) prev.preconditions = new List<string> { count.id };

                if (pad.deluge)
                    prev = AddReal("pad.deluge", "Deluge", "Deluge", TimelinePhase.Pad, -16,
                        TimelineCriticality.Nominal, TimelineVisibility.Kid,
                        "Deluge water is up because this pad has a deluge. " + Note(-16),
                        new[] { count.id }, Hw("deluge"), false, TimelineTriggerFrom(TimelinePhase.Pad),
                        narration: "deluge", music: "CUE-06");
                else Absent("pad.deluge", "Deluge is absent because pad " + pad.id + " has no deluge.");

                if (pad.umbilical) { /* drop is emitted next to ignition */ }
                else Absent("pad.umbilical", "Umbilical drop is absent because pad " + pad.id + " has no umbilical.");

                foreach (var p in parts) if (p.family == "solid") EmitSolid(p, count.id);

                MissionEvent stagger = null;
                if (liftoffEngines > 1)
                {
                    double lead = Math.Min(12, 0.2 * (liftoffEngines - 1));
                    stagger = AddReal("ign.stagger", "Staggered ignition", "Stagger", TimelinePhase.Ignition,
                        TimelineClock.RealIgnition - lead, TimelineCriticality.Critical, TimelineVisibility.Standard,
                        liftoffEngines + " engines light in a stagger so thrust comes up together. A single chamber does not stagger. " + Note(TimelineClock.RealIgnition - lead),
                        new[] { count.id }, Hw("vehicle"), false, TimelineTriggerFrom(TimelinePhase.Ignition));
                }
                else Absent("ign.stagger", "Staggered ignition is absent because one engine lights at commit, so there is nothing to stagger.");

                string ignParent = stagger != null ? stagger.id : count.id;
                var light = AddReal("ign.light", "Ignition", "Ignition", TimelinePhase.Ignition,
                    TimelineClock.RealIgnition, TimelineCriticality.Critical, TimelineVisibility.Kid,
                    "Ignition at real T-0:03. The ascent sim starts at this commit; liftoff is the separate release. " + Note(TimelineClock.RealIgnition),
                    new[] { ignParent }, Hw("vehicle"), false, TimelineTriggerFrom(TimelinePhase.Ignition),
                    narration: "ignition", music: "CUE-07");

                var thrust = AddReal("ign.thrustok", "Thrust OK", "Thrust OK", TimelinePhase.Ignition, -1,
                    TimelineCriticality.Safety, TimelineVisibility.Standard,
                    "Thrust is above the commit gate. Below the gate, the count recycles or aborts on the pad. " + Note(-1),
                    new[] { light.id }, Hw("vehicle"), false, TimelineTriggerFrom(TimelinePhase.Ignition));

                if (pad.umbilical)
                    AddReal("pad.umbilical", "Umbilical drop", "Umbilical", TimelinePhase.Pad, -0.6,
                        TimelineCriticality.Critical, TimelineVisibility.Standard,
                        "Umbilicals drop because this pad has them, after thrust is in the gate. " + Note(-0.6),
                        new[] { thrust.id }, Hw("umbilical"), false, TimelineTriggerFrom(TimelinePhase.Pad));

                var release = AddAt("ign.release", "Release", "Release", TimelinePhase.Ignition,
                    Game(-0.2), TimelineCriticality.Critical, TimelineVisibility.Standard,
                    "Hold-down release. The sequencer commands it; the vehicle is free. " + Note(-0.2),
                    new[] { thrust.id }, Hw("vehicle"),
                    Trigger.Action("autopilot", "release"), null, false, 0);

                AddAt("term.liftoff", "Liftoff", "Liftoff", TimelinePhase.Terminal, 0,
                    TimelineCriticality.Critical, TimelineVisibility.Kid,
                    "Liftoff. The sim calls it when altitude passes 1 m. Nominal is T+0; the actual is the sim liftoff aligned to that zero.",
                    new[] { release.id }, Hw("vehicle"),
                    Trigger.Sim("alt>1"), 0, false, 0.5, narration: "liftoff", music: "CUE-07", telemetry: new[] { "alt" });
            }

            void EmitLoad(Part p, string parent)
            {
                if (p.family == "solid") return;
                if (p.family == "hypergolic")
                {
                    string gate = present.Contains("term.hold.early") ? "term.hold.early" : parent;
                    var off = AddReal("count.hyper." + p.key + ".offclock", p.name + " hypergolic load", "Off-clock", TimelinePhase.Load, -4000,
                        TimelineCriticality.Safety, TimelineVisibility.Engineer,
                        p.name + " flies " + p.prop.label + ", which is loaded off the clock before the count, behind a safety gate. It is already done when the count starts. " + Note(-4000),
                        new[] { parent }, Hw("stage:" + p.key, "propellant:" + p.prop.id), true, "padLoadData",
                        cost: p.propCost, status: TimelineStatus.Done);
                    AddReal("count.hyper." + p.key + ".padclear", p.name + " pad clear", "Pad clear", TimelinePhase.Load, -3900,
                        TimelineCriticality.Safety, TimelineVisibility.Engineer,
                        "Pad is clear of the hypergolic load crew before the count is allowed to start. " + Note(-3900),
                        new[] { off.id }, Hw("stage:" + p.key), false, "padLoadData", status: TimelineStatus.Done);
                    // Parent of the off-clock load is the T-2 h hold, which is earlier than go-for-prop.
                    off.preconditions = new List<string> { gate };
                    return;
                }
                if (!pad.umbilical)
                {
                    Absent("count.load." + p.key, p.name + " flies " + p.prop.label + ", but pad " + pad.id + " has no umbilical, so the on-clock load is omitted.");
                    return;
                }
                bool upper = p.upper;
                if (p.family == "kerolox") EmitKerolox(p, parent, upper);
                else if (p.family == "hydrolox") EmitHydrolox(p, parent, upper);
                else if (p.family == "methalox") EmitMethalox(p, parent, upper);
                else if (p.family == "hybrid") EmitHybrid(p, parent);
            }

            void EmitKerolox(Part p, string parent, bool upper)
            {
                double fuelT = upper ? -1200 : TimelineClock.RealFuelLox;
                double loxT = upper ? TimelineClock.RealUpperLox : -2060;
                double topT = upper ? -600 : -700;
                var fuel = AddReal("count.fuel." + p.key + ".load", p.name + " fuel load", Fit("Fuel " + p.key), TimelinePhase.Load, fuelT,
                    TimelineCriticality.Nominal, TimelineVisibility.Standard,
                    p.name + " loads " + p.prop.fuel + " (" + p.prop.label + "). Anchor for a first stage is real T-35:00. " + Note(fuelT),
                    new[] { parent }, Hw("stage:" + p.key, "propellant:" + p.prop.id, "umbilical"), true, "padLoadData", cost: p.propCost);
                AddReal("count.lox." + p.key + ".load", p.name + " LOX load", Fit("LOX " + p.key), TimelinePhase.Load, loxT,
                    TimelineCriticality.Nominal, TimelineVisibility.Standard,
                    p.name + " loads LOX beside the fuel. An upper stage uses the real T-16:00 LOX anchor. " + Note(loxT),
                    new[] { fuel.id }, Hw("stage:" + p.key, "propellant:" + p.prop.id, "umbilical"), false, "padLoadData");
                AddReal("count.lox." + p.key + ".top", p.name + " LOX topping", Fit("LOX top " + p.key), TimelinePhase.Load, topT,
                    TimelineCriticality.Nominal, TimelineVisibility.Engineer,
                    p.name + " keeps LOX topped as it boils off. " + Note(topT),
                    new[] { fuel.id }, Hw("stage:" + p.key, "propellant:" + p.prop.id, "umbilical"), true, "padLoadData");
                EmitChill(p, topT < -420 ? -420 : topT + 40);
            }

            void EmitHydrolox(Part p, string parent, bool upper)
            {
                double t0 = upper ? TimelineClock.RealUpperLox : -2200;
                double step = upper ? 80 : 200;
                var chill = AddReal("count.lh2." + p.key + ".chill", p.name + " LH2 chilldown", "LH2 chill", TimelinePhase.Load, t0,
                    TimelineCriticality.Nominal, TimelineVisibility.Engineer,
                    p.name + " chills the hydrogen lines. " + p.prop.label + " is a deep cryogen, so chilldown comes before fill. " + Note(t0),
                    new[] { parent }, Hw("stage:" + p.key, "propellant:" + p.prop.id, "umbilical"), false, "padLoadData");
                var slow = AddReal("count.lh2." + p.key + ".slow", p.name + " LH2 slow fill", "LH2 slow", TimelinePhase.Load, t0 + step,
                    TimelineCriticality.Nominal, TimelineVisibility.Standard,
                    p.name + " slow-fills LH2 after chilldown. " + Note(t0 + step),
                    new[] { chill.id }, Hw("stage:" + p.key, "propellant:" + p.prop.id, "umbilical"), false, "padLoadData", cost: p.propCost);
                var fast = AddReal("count.lh2." + p.key + ".fast", p.name + " LH2 fast fill", "LH2 fast", TimelinePhase.Load, t0 + 2 * step,
                    TimelineCriticality.Nominal, TimelineVisibility.Standard,
                    p.name + " fast-fills LH2 once the lines are cold. " + Note(t0 + 2 * step),
                    new[] { slow.id }, Hw("stage:" + p.key, "propellant:" + p.prop.id, "umbilical"), false, "padLoadData");
                var leak = AddReal("count.lh2." + p.key + ".leak", p.name + " hydrogen leak check", "H2 sniff", TimelinePhase.Load, t0 + 3 * step,
                    TimelineCriticality.Safety, TimelineVisibility.Engineer,
                    p.name + " gets a hydrogen sniff. A leak above the pad limit scrubs. " + Note(t0 + 3 * step),
                    new[] { fast.id }, Hw("stage:" + p.key, "propellant:" + p.prop.id), false, "padLoadData");
                AddReal("count.lox." + p.key + ".load", p.name + " LOX load", Fit("LOX " + p.key), TimelinePhase.Load, t0 + 4 * step,
                    TimelineCriticality.Nominal, TimelineVisibility.Standard,
                    p.name + " loads LOX for the hydrogen stage. " + Note(t0 + 4 * step),
                    new[] { chill.id }, Hw("stage:" + p.key, "propellant:" + p.prop.id, "umbilical"), false, "padLoadData");
                var top = AddReal("count.lh2." + p.key + ".top", p.name + " LH2 topping", "LH2 top", TimelinePhase.Load, t0 + 5 * step,
                    TimelineCriticality.Nominal, TimelineVisibility.Engineer,
                    p.name + " tops LH2. Hydrogen keeps boiling off, so topping is part of the count. " + Note(t0 + 5 * step),
                    new[] { leak.id }, Hw("stage:" + p.key, "propellant:" + p.prop.id, "umbilical"), true, "padLoadData");
                AddReal("count.lh2." + p.key + ".replenish", p.name + " LH2 replenish", "LH2 replen", TimelinePhase.Load, t0 + 6 * step,
                    TimelineCriticality.Nominal, TimelineVisibility.Engineer,
                    p.name + " replenishes LH2 through the hold. " + Note(t0 + 6 * step),
                    new[] { top.id }, Hw("stage:" + p.key, "propellant:" + p.prop.id, "umbilical"), true, "padLoadData");
                EmitChill(p, Math.Max(-360, t0 + 6 * step + 40));
            }

            void EmitMethalox(Part p, string parent, bool upper)
            {
                double chillT = upper ? -1100 : -2200;
                double loadT = upper ? TimelineClock.RealUpperLox : TimelineClock.RealFuelLox;
                var cCh4 = AddReal("count.ch4." + p.key + ".chill", p.name + " methane chilldown", "CH4 chill", TimelinePhase.Load, chillT,
                    TimelineCriticality.Nominal, TimelineVisibility.Engineer,
                    p.name + " chills the methane side. Methalox chills both propellants before fill. " + Note(chillT),
                    new[] { parent }, Hw("stage:" + p.key, "propellant:" + p.prop.id, "umbilical"), false, "padLoadData");
                AddReal("count.lox." + p.key + ".chill", p.name + " LOX chilldown", "LOX chill", TimelinePhase.Load, chillT + 40,
                    TimelineCriticality.Nominal, TimelineVisibility.Engineer,
                    p.name + " chills the LOX side. " + Note(chillT + 40),
                    new[] { cCh4.id }, Hw("stage:" + p.key, "propellant:" + p.prop.id, "umbilical"), false, "padLoadData");
                var ch4 = AddReal("count.ch4." + p.key + ".load", p.name + " methane load", Fit("CH4 " + p.key), TimelinePhase.Load, loadT,
                    TimelineCriticality.Nominal, TimelineVisibility.Standard,
                    p.name + " loads " + p.prop.fuel + ". " + Note(loadT),
                    new[] { cCh4.id }, Hw("stage:" + p.key, "propellant:" + p.prop.id, "umbilical"), false, "padLoadData", cost: p.propCost);
                AddReal("count.lox." + p.key + ".load", p.name + " LOX load", Fit("LOX " + p.key), TimelinePhase.Load, loadT + 40,
                    TimelineCriticality.Nominal, TimelineVisibility.Standard,
                    p.name + " loads LOX with the methane. " + Note(loadT + 40),
                    new[] { ch4.id }, Hw("stage:" + p.key, "propellant:" + p.prop.id, "umbilical"), false, "padLoadData");
                double topT = upper ? -600 : -700;
                AddReal("count.lox." + p.key + ".top", p.name + " topping", Fit("Top " + p.key), TimelinePhase.Load, topT,
                    TimelineCriticality.Nominal, TimelineVisibility.Engineer,
                    p.name + " tops methane and LOX. " + Note(topT),
                    new[] { ch4.id }, Hw("stage:" + p.key, "propellant:" + p.prop.id, "umbilical"), true, "padLoadData");
                EmitChill(p, -420);
            }

            void EmitHybrid(Part p, string parent)
            {
                var load = AddReal("count.n2o." + p.key + ".load", p.name + " oxidizer load", "N2O load", TimelinePhase.Load, -1800,
                    TimelineCriticality.Nominal, TimelineVisibility.Standard,
                    p.name + " is a hybrid: the grain is already cast, and only " + p.prop.oxidizer + " is loaded. " + Note(-1800),
                    new[] { parent }, Hw("stage:" + p.key, "propellant:" + p.prop.id, "umbilical"), false, "padLoadData", cost: p.propCost);
                EmitChill(p, -420, load.id);
            }

            void EmitChill(Part p, double real, string parent = null)
            {
                if (real > -70) real = -70;
                AddReal("count.chill." + p.key, p.name + " engine chill", Fit("Chill " + p.key), TimelinePhase.Load, real,
                    TimelineCriticality.Nominal, TimelineVisibility.Standard,
                    p.name + " chills " + p.engine.label + " before terminal count. The anchor is real T-7:00 when the stage is the booster. " + Note(real),
                    parent == null ? new[] { "count.prop.go" } : new[] { parent },
                    Hw("stage:" + p.key, "engine:" + p.engine.id, "umbilical"), false, "padLoadData");
            }

            void EmitPress(Part p, string pollId)
            {
                if (p.family == "solid" || p.family == "hypergolic") return;
                double real = p.upper ? -50 : TimelineClock.RealTankPress;
                AddReal("count.press." + p.key, p.name + " tank press", Fit("Press " + p.key), TimelinePhase.Load, real,
                    TimelineCriticality.Critical, TimelineVisibility.Standard,
                    p.name + " tanks pressurize. The booster anchor is real T-1:00. " + Note(real),
                    new[] { pollId }, Hw("stage:" + p.key, "propellant:" + p.prop.id), false, "padLoadData",
                    narration: "tankpress");
            }

            void EmitSolid(Part p, string countId)
            {
                var arm = AddReal("count.solid." + p.key + ".arm", p.name + " safe and arm", "Arm motor", TimelinePhase.Load, -12,
                    TimelineCriticality.Safety, TimelineVisibility.Standard,
                    p.name + " is a solid (" + p.prop.label + "). There is no propellant load. Safe/arm devices arm before ignition. " + Note(-12),
                    new[] { countId }, Hw("stage:" + p.key, "propellant:" + p.prop.id), false, "padLoadData", cost: p.propCost);
                AddReal("count.solid." + p.key + ".igniter", p.name + " igniter check", "Igniter ck", TimelinePhase.Load, -8,
                    TimelineCriticality.Safety, TimelineVisibility.Engineer,
                    p.name + " igniter is checked after arming and before commit. " + Note(-8),
                    new[] { arm.id }, Hw("stage:" + p.key, "engine:" + p.engine.id), false, "padLoadData");
            }

            void EmitFlight(List<SerialStage> serial, SimResult sim)
            {
                double t0 = 0;
                bool haveLiftoff = false;
                foreach (var ev in sim.events) if (ev.type == "liftoff") { t0 = ev.t; haveLiftoff = true; break; }
                if (!haveLiftoff)
                    foreach (var it in items) if (it.Ev.id == "term.liftoff") it.Ev.actual = null;
                double Align(double t) => TimelineClock.Round(t - t0);
                var end = new Dictionary<int, double>();
                var ign = new Dictionary<int, double>();
                foreach (var ev in sim.events)
                {
                    if (ev.stage == null) continue;
                    int idx = ev.stage.Value - 1;
                    if (ev.type == "ignition")
                    {
                        // separation.stage is the new index, not 1-based. Ignition stage is 1-based.
                        if (ev.type == "ignition") ign[ev.stage.Value - 1] = Align(ev.t);
                    }
                    if (ev.type == "burnout" || ev.type == "cutoff")
                    {
                        int ended = ev.stage.Value - 1;
                        if (!end.ContainsKey(ended)) end[ended] = Align(ev.t);
                    }
                    if (ev.type == "separation")
                    {
                        // separation.stage equals the 1-based index of the stage that ended.
                        int ended = ev.stage.Value - 1;
                        if (!end.ContainsKey(ended)) end[ended] = Align(ev.t);
                    }
                }
                // Ignition events use 1-based stage numbers. The loop above stored them.
                // Re-read ignitions explicitly so a separation cannot be mistaken for one.
                ign.Clear();
                foreach (var ev in sim.events)
                    if (ev.type == "ignition" && ev.stage != null) ign[ev.stage.Value - 1] = Align(ev.t);

                double maxq = sim.maxQ > 0 ? Align(sim.maxQTime) : double.NaN;
                MissionEvent maxEvent = null;
                if (sim.maxQ > 0)
                {
                    maxEvent = AddAt("asc.maxq", "Max-Q", "Max-Q", TimelinePhase.Ascent, maxq,
                        TimelineCriticality.Critical, TimelineVisibility.Kid,
                        "Max-Q is the sim peak dynamic pressure (" + Math.Round(sim.maxQ).ToString(System.Globalization.CultureInfo.InvariantCulture) + " Pa), not a scripted clock time. The public band is about T+1:07 to T+1:12 on a medium kerolox ascent; this vehicle's sim time is " + TimelineClock.Clock(maxq) + ".",
                        new[] { "term.liftoff" }, Hw("vehicle"),
                        Trigger.Sim("q==max"), maxq, false, 5, narration: "maxq", music: "CUE-08", telemetry: new[] { "q", "alt" });
                }
                else Absent("asc.maxq", "Max-Q is absent because the sim dynamic pressure never rose above zero.");

                if (throttleable && maxEvent != null)
                    AddAt("asc.throttle", "Throttle bucket", "Throttle", TimelinePhase.Ascent, maxq,
                        TimelineCriticality.Nominal, TimelineVisibility.Engineer,
                        "Throttle bucket through Max-Q because a liftoff engine can throttle. The sim condition is a throttle below full near peak q.",
                        new[] { "asc.maxq" }, Hw("vehicle"),
                        Trigger.Sim("throttle<1 && near(q,max)"), null, false, 1, music: "CUE-08", telemetry: new[] { "q" });
                else if (!throttleable)
                    Absent("asc.throttle", "Throttle bucket is absent because no liftoff engine can throttle (the throttle range is closed).");
                else Absent("asc.throttle", "Throttle bucket is absent because Max-Q did not occur.");

            bool anyBooster = false;
            string transferEndId = null;
            string mecoId = null;
                double mecoT = double.NaN;
                string lastUpperEnd = null;
                double lastUpperEndT = double.NaN;
                bool anyUpper = false;

                for (int i = 0; i < serial.Count; i++)
                {
                    var st = serial[i];
                    if (!end.TryGetValue(i, out var endT)) continue;
                    bool boosterPhase = st.role == "strapons+core";
                    if (boosterPhase)
                    {
                        anyBooster = true;
                        AddAt("stg.booster", "Booster separation", "Booster sep", TimelinePhase.Staging, endT,
                            TimelineCriticality.Critical, TimelineVisibility.Kid,
                            "Strap-on boosters separate when their sim phase burns out at " + TimelineClock.Clock(endT) + ". The core keeps burning.",
                            new[] { Prior("asc.maxq", endT, "term.liftoff") }, Hw("vehicle"),
                            Trigger.Sim("burnout:boosters"), endT, false, 0.5, narration: "sep", music: "CUE-10");
                        continue;
                    }
                    bool coreContinue = st.role == "core" && st.index == 0 && i > 0 && serial[i - 1].role == "strapons+core";
                    if (ign.TryGetValue(i, out var ignT) && st.index > 0)
                        EmitUpperStart(st, ignT, endT);
                    if (st.index <= 0)
                    {
                        bool solid = IsSolidDesign(st.index);
                        string id = solid ? "asc.burnout" : "asc.meco";
                        string label = solid ? "Burnout" : "MECO";
                        string full = solid ? "Motor burnout" : "Main engine cutoff";
                        string why = solid
                            ? "The solid motor burns out at " + TimelineClock.Clock(endT) + ". It has no throttle and no cutoff command, so this is burnout rather than MECO."
                            : "Main engine cutoff at sim " + TimelineClock.Clock(endT) + (coreContinue ? ", after the boosters have already separated." : ".") + " A medium kerolox public anchor is about T+2:25; this time is the sim burnout.";
                        string cutoffParent = anyBooster ? Prior("stg.booster", endT, "asc.maxq") : Prior("asc.maxq", endT, "term.liftoff");
                        var ev = AddAt(id, full, label, TimelinePhase.Ascent, endT,
                            TimelineCriticality.Critical, TimelineVisibility.Kid, why,
                            new[] { cutoffParent },
                            Hw("stage:s1", "engine:" + d.stages[0].engine),
                            Trigger.Sim(solid ? "burnout:s1" : "cutoff:s1"), endT, false, 1,
                            narration: solid ? null : "meco", music: "CUE-08", telemetry: new[] { "v", "alt" });
                        mecoId = ev.id;
                        mecoT = endT;
                        if (solid) Absent("asc.meco", "MECO is absent because " + d.stages[0].name + " is a solid motor. It burns out; it does not shut down.");
                        bool moreDesign = d.stages.Count > 1;
                        if (moreDesign)
                            AddAt("stg.sep.1", "Stage separation", "SEP s1", TimelinePhase.Staging, endT,
                                TimelineCriticality.Critical, TimelineVisibility.Kid,
                                "Stage 1 separates at sim " + TimelineClock.Clock(endT) + ", the same instant the sim records separation. The timeline orders it with the cutoff.",
                                new[] { ev.id }, Hw("stage:s1"),
                                Trigger.Sim("sep:s1"), endT, false, 0.5, narration: "sep", music: "CUE-10");
                        else Absent("stg.sep.1", "Stage separation is absent because the vehicle has a single stage.");
                    }
                }
                if (!anyBooster) Absent("stg.booster", "Booster separation is absent because no strap-on boosters are attached.");

                for (int i = 0; i < serial.Count; i++)
                {
                    if (serial[i].index <= 0) continue;
                    anyUpper = true;
                    if (!end.TryGetValue(i, out var endT)) continue;
                    if (!ign.ContainsKey(i)) continue;
                    var part = parts.Find(p => p.designIndex == serial[i].index);
                    if (part == null) continue;
                    lastUpperEnd = EmitUpperEnd(part, endT, ref transferEndId);
                    lastUpperEndT = endT;
                }
                if (!anyUpper)
                {
                    Absent("stg.ses.s2.1", "SES-1 is absent because this vehicle has no upper stage.");
                    Absent("stg.seco.s2.1", "SECO-1 is absent because this vehicle has no upper stage.");
                    Absent("stg.ses.s2.2", "SES-2 is absent because this vehicle has no upper stage, so there is no transfer restart.");
                }

                EmitFairing(sim, t0, maxq);
                EmitOrbitAndBeyond(sim, t0, mecoId, mecoT, lastUpperEnd, lastUpperEndT, transferEndId);
            }

            /// <summary>A precondition must already be in the sequence at or before this nominal.</summary>
            string Prior(string candidate, double at, string fallback)
            {
                if (!string.IsNullOrEmpty(candidate) && present.Contains(candidate) && NominalOf(candidate) <= at + 1e-6)
                    return candidate;
                if (!string.IsNullOrEmpty(fallback) && fallback != candidate && present.Contains(fallback) && NominalOf(fallback) <= at + 1e-6)
                    return fallback;
                return "term.liftoff";
            }

            void EmitUpperStart(SerialStage st, double ignT, double endT)
            {
                var part = parts.Find(p => p.designIndex == st.index);
                if (part == null) return;
                string sep = "stg.sep." + st.index;
                string parent = Prior(sep, ignT, Prior("asc.meco", ignT, Prior("asc.burnout", ignT, "term.liftoff")));
                if (!part.engine.restart)
                {
                    AddAt("stg.light." + part.key, part.name + " ignition", "Upper light", TimelinePhase.Staging, ignT,
                        TimelineCriticality.Critical, TimelineVisibility.Kid,
                        part.name + " lights at " + TimelineClock.Clock(ignT) + ". " + part.engine.label + " is not restartable, so this start is not called SES.",
                        new[] { parent }, Hw("stage:" + part.key, "engine:" + part.engine.id),
                        Trigger.Sim("ignition:" + part.key), ignT, false, 0.5, music: "CUE-10");
                    Absent("stg.ses." + part.key + ".1", "SES-1 is absent on " + part.name + " because " + part.engine.id + " has restart set false.");
                    Absent("stg.seco." + part.key + ".1", "SECO-1 is absent on " + part.name + " because " + part.engine.id + " is not restartable. Cutoff is stg.light's matching burnout, not a SECO.");
                    Absent("stg.ses." + part.key + ".2", "SES-2 is absent on " + part.name + " because " + part.engine.id + " cannot restart.");
                    return;
                }
                AddAt("stg.ses." + part.key + ".1", part.name + " SES-1", Fit("SES-1 " + part.key), TimelinePhase.Staging, ignT,
                    TimelineCriticality.Critical, TimelineVisibility.Kid,
                    "SES-1 because " + part.engine.label + " (" + part.engine.id + ") is restartable (" + part.engine.restarts + " restarts) and this is its first start, at sim " + TimelineClock.Clock(ignT) + ". A public anchor for a medium kerolox upper start is about T+2:36; this time is the sim ignition.",
                    new[] { parent }, Hw("stage:" + part.key, "engine:" + part.engine.id),
                    Trigger.Sim("ignition:" + part.key), ignT, false, 0.5, narration: "ses1", music: "CUE-10", telemetry: new[] { "v", "alt" });
            }

            string EmitUpperEnd(Part part, double endT, ref string transferEndId)
            {
                string parentStart = part.engine.restart ? "stg.ses." + part.key + ".1" : "stg.light." + part.key;
                if (!part.engine.restart)
                {
                    AddAt("stg.cutoff." + part.key, part.name + " cutoff", "Upper cutoff", TimelinePhase.Staging, endT,
                        TimelineCriticality.Critical, TimelineVisibility.Kid,
                        part.name + " ends its only burn at " + TimelineClock.Clock(endT) + ". The engine cannot restart, so this is a cutoff, not SECO.",
                        new[] { parentStart }, Hw("stage:" + part.key, "engine:" + part.engine.id),
                        Trigger.Sim("cutoff:" + part.key), endT, false, 0.5, music: "CUE-10", telemetry: new[] { "v", "alt" });
                    return "stg.cutoff." + part.key;
                }
                string seco1 = "stg.seco." + part.key + ".1";
                AddAt(seco1, part.name + " SECO-1", Fit("SECO-1 " + part.key), TimelinePhase.Staging, endT,
                    TimelineCriticality.Critical, TimelineVisibility.Kid,
                    "SECO-1 because " + part.engine.id + " is restartable and the ascent burn ends at sim " + TimelineClock.Clock(endT) + ".",
                    new[] { parentStart }, Hw("stage:" + part.key, "engine:" + part.engine.id),
                    Trigger.Sim("cutoff:" + part.key), endT, false, 0.5, narration: "seco", music: "CUE-11", telemetry: new[] { "v", "alt" });
                bool wantSecond = transfer;
                if (wantSecond && part.engine.restarts >= 1)
                {
                    double ses2 = TimelineClock.Round(endT + 1200);
                    AddAt("stg.ses." + part.key + ".2", part.name + " SES-2", Fit("SES-2 " + part.key), TimelinePhase.Transfer, ses2,
                        TimelineCriticality.Critical, TimelineVisibility.Standard,
                        "SES-2 because " + part.engine.id + " is restartable (" + part.engine.restarts + " restarts) and " + orbitLabel + " needs a transfer burn after SECO-1. The ascent sim flies one continuous upper burn, so this restart is planned, not a second sim ignition.",
                        new[] { "stg.seco." + part.key + ".1" }, Hw("stage:" + part.key, "engine:" + part.engine.id),
                        Trigger.After("stg.seco." + part.key + ".1", 1200), null, false, 1, music: "CUE-10");
                    double seco2 = TimelineClock.Round(ses2 + 300);
                    transferEndId = "stg.seco." + part.key + ".2";
                    AddAt(transferEndId, part.name + " SECO-2", Fit("SECO-2 " + part.key), TimelinePhase.Transfer, seco2,
                        TimelineCriticality.Critical, TimelineVisibility.Standard,
                        "SECO-2 closes the planned transfer burn. It is not taken from the ascent sim, which has already ended the first upper burn.",
                        new[] { "stg.ses." + part.key + ".2" }, Hw("stage:" + part.key, "engine:" + part.engine.id),
                        Trigger.After("stg.ses." + part.key + ".2", 300), null, false, 1, music: "CUE-11");
                    return seco1;
                }
                if (!wantSecond)
                    Absent("stg.ses." + part.key + ".2", "SES-2 is absent on " + part.name + " because " + orbitLabel + " does not need a transfer burn. The engine can restart, and the mission does not ask it to.");
                else
                    Absent("stg.ses." + part.key + ".2", "SES-2 is absent on " + part.name + " because " + part.engine.id + " has " + part.engine.restarts + " restarts, which is not enough for a second start.");
                return seco1;
            }

            bool IsSolidDesign(int index)
            {
                if (index < 0 || index >= d.stages.Count) return false;
                return c.PropellantById.TryGetValue(d.stages[index].propellant ?? "", out var p) && p.storability == "solid";
            }

            void EmitFairing(SimResult sim, double t0, double maxq)
            {
                if (!hasFairing)
                {
                    Absent("stg.fairing", "Fairing separation is absent because the vehicle has no fairing.");
                    return;
                }
                double altNeed = d.fairingJettisonAltitudeKm * 1000;
                double? hit = null;
                if (sim.samples != null)
                {
                    foreach (var s in sim.samples)
                    {
                        double sampleT = TimelineClock.Round(s.t - t0);
                        bool afterQ = double.IsNaN(maxq) || sampleT + 1e-9 >= maxq;
                        if (s.alt >= altNeed && afterQ) { hit = sampleT; break; }
                    }
                }
                if (hit == null) hit = CoastAltitude(sim, t0, altNeed);
                if (hit == null)
                {
                    Absent("stg.fairing", "Fairing separation is absent because the sim state never reaches " + d.fairingJettisonAltitudeKm.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " km, the jettison altitude.");
                    return;
                }
                double t = hit.Value;
                if (!double.IsNaN(maxq) && t < maxq) t = maxq;
                string parent = Prior("asc.maxq", t, "term.liftoff");
                string expr = "alt>=" + Math.Round(altNeed).ToString(System.Globalization.CultureInfo.InvariantCulture) + " && t>maxQ";
                AddAt("stg.fairing", "Fairing separation", "Fairing", TimelinePhase.Staging, t,
                    TimelineCriticality.Nominal, TimelineVisibility.Kid,
                    "Fairing separates once heating has fallen (after Max-Q) and altitude reaches " + d.fairingJettisonAltitudeKm.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " km. Time " + TimelineClock.Clock(t) + " is from the sim, or from a ballistic coast when the sim stops at burnout below that altitude.",
                    new[] { parent }, Hw("fairing"),
                    Trigger.Sim(expr), t, false, 1, narration: "fairing", music: "CUE-10", telemetry: new[] { "alt", "q" });
            }

            /// <summary>Ballistic coast from the sim's final state. The ascent sim stops at burnout.</summary>
            static double? CoastAltitude(SimResult sim, double t0, double altNeed)
            {
                double x = sim.x, y = sim.y, vx = sim.vx, vy = sim.vy;
                double r0 = Math.Sqrt(x * x + y * y);
                if (r0 <= 1) return null;
                if (r0 - Gravity.R_EARTH >= altNeed) return TimelineClock.Round(sim.time - t0);
                double step = 2;
                double t = sim.time - t0;
                for (int i = 0; i < 400; i++)
                {
                    double r2 = x * x + y * y;
                    double r = Math.Sqrt(r2);
                    double gk = -Gravity.MU_EARTH / (r2 * r);
                    vx += gk * x * step;
                    vy += gk * y * step;
                    x += vx * step;
                    y += vy * step;
                    t += step;
                    double alt = Math.Sqrt(x * x + y * y) - Gravity.R_EARTH;
                    if (alt >= altNeed) return TimelineClock.Round(t);
                    if (alt < 0) return null;
                }
                return null;
            }

            void EmitOrbitAndBeyond(SimResult sim, double t0, string mecoId, double mecoT, string upperEndId, double upperEndT, string transferEndId)
            {
                // The sim orbit is the end of the continuous ascent burn (SECO-1 / cutoff),
                // not the planned transfer. SES-2/SECO-2 stay later and do not parent insertion.
                string insertParent = upperEndId ?? mecoId ?? "term.liftoff";
                double insertT = upperEndId != null ? upperEndT : (mecoId != null ? mecoT : 0);
                if (sim.outcome == "orbit")
                {
                    foreach (var ev in sim.events) if (ev.type == "orbit") { insertT = TimelineClock.Round(ev.t - t0); break; }
                    double parentNom = NominalOf(insertParent);
                    if (insertT < parentNom) insertT = parentNom;
                    AddAt("orb.insert", "Orbit insertion", "Orbit", TimelinePhase.Orbit, insertT,
                        TimelineCriticality.Critical, TimelineVisibility.Kid,
                        "Orbit insertion from the sim outcome at the end of the ascent burn. A later transfer burn, when the mission has one, is planned after this and is not what the ascent sim flew.",
                        new[] { insertParent }, Hw("vehicle"),
                        Trigger.Sim("orbit"), insertT, false, 1, music: "CUE-11", telemetry: new[] { "alt", "v" });
                    insertParent = "orb.insert";
                }
                else Absent("orb.insert", "Orbit insertion is absent because the sim outcome is " + sim.outcome + ", not orbit.");

                if (d.payloadMass <= 0)
                {
                    Absent("dep.payload", "Payload deploy is absent because the vehicle carries no payload mass.");
                    string after = transferEndId != null && present.Contains(transferEndId) ? transferEndId : insertParent;
                    double afterT = after == transferEndId ? NominalOf(after) : insertT;
                    EmitDisposal(after, afterT);
                    return;
                }

                string deployParent = insertParent;
                double deployT;
                string deployWhy;
                if (transferEndId != null && present.Contains(transferEndId))
                {
                    deployParent = transferEndId;
                    deployT = TimelineClock.Round(NominalOf(transferEndId) + 30);
                    deployWhy = "Payload deploys after the transfer burn (SECO-2), 30 s later. The ascent sim does not fly that second burn.";
                }
                else if (sim.outcome == "orbit")
                {
                    deployT = TimelineClock.Round(insertT + 30);
                    deployWhy = "Payload deploys 30 s after orbit insertion.";
                }
                else
                {
                    double r = Math.Sqrt(sim.x * sim.x + sim.y * sim.y);
                    double coast = 0;
                    if (r > 1)
                    {
                        double vr = (sim.vx * sim.x + sim.vy * sim.y) / r;
                        double g = Gravity.MU_EARTH / (r * r);
                        if (vr > 0 && g > 0) coast = vr / g;
                    }
                    deployT = TimelineClock.Round((sim.time - t0) + coast);
                    if (mecoId != null && deployT < mecoT) deployT = mecoT;
                    deployParent = mecoId ?? "term.liftoff";
                    deployWhy = "Payload deploys at the ballistic apogee estimated from the sim state at burnout (" + TimelineClock.Clock(deployT) + "). The ascent sim stops at burnout and does not fly the coast.";
                }
                AddAt("dep.payload", "Payload deploy", "Deploy", TimelinePhase.Deploy, deployT,
                    TimelineCriticality.Critical, TimelineVisibility.Kid, deployWhy,
                    new[] { deployParent }, Hw("payload"),
                    Trigger.After(deployParent, TimelineClock.Round(deployT - (deployParent == insertParent || deployParent == upperEndId || deployParent == mecoId ? NominalOf(deployParent) : 0))),
                    deployT, false, 1, narration: "deploy", music: "CUE-11");

                // The delay above is messy if NominalOf isn't the parent's nominal. Recompute delay properly.
                var deploy = items[items.Count - 1].Ev;
                if (deploy.id == "dep.payload" && deploy.trigger != null && deploy.trigger.kind == "after")
                {
                    double pNom = NominalOf(deployParent);
                    deploy.trigger.delay = TimelineClock.Round(Math.Max(0, deploy.nominal - pNom));
                }

                EmitDisposal(deployParent == "dep.payload" ? "dep.payload" : "dep.payload", deployT);
            }

            void EmitDisposal(string afterId, double afterT)
            {
                if (suborbital || d.stages.Count < 2)
                {
                    Absent("disp.deorbit", suborbital
                        ? "Deorbit is absent because the flight is suborbital and comes back on its own."
                        : "Deorbit is absent because there is no upper stage left to dispose of.");
                    return;
                }
                var upper = parts.Find(p => p.upper);
                if (upper == null)
                {
                    Absent("disp.deorbit", "Deorbit is absent because there is no upper stage.");
                    return;
                }
                int used = 1 + (transfer && upper.engine.restart && upper.engine.restarts >= 1 ? 1 : 0);
                int maxStarts = upper.engine.restart ? upper.engine.restarts + 1 : 1;
                if (!upper.engine.restart || used >= maxStarts)
                {
                    Absent("disp.deorbit", "Deorbit is absent because " + upper.engine.id + " has no restart left after the mission burns (starts used " + used + " of " + maxStarts + ").");
                    return;
                }
                double t = TimelineClock.Round(afterT + 60);
                AddAt("disp.deorbit", "Deorbit burn", "Deorbit", TimelinePhase.Disposal, t,
                    TimelineCriticality.Nominal, TimelineVisibility.Engineer,
                    "Deorbit because " + upper.engine.id + " still has a restart after the mission burns (" + used + " used, " + upper.engine.restarts + " restarts on the engine). The ascent sim does not fly this burn.",
                    new[] { afterId }, Hw("stage:" + upper.key, "engine:" + upper.engine.id),
                    Trigger.After(afterId, 60), null, false, 1);
            }

            void EmitRecovery()
            {
                var s0 = d.stages[0];
                c.EngineById.TryGetValue(s0.engine ?? "", out var engine);
                bool legs = d.legs;
                bool prop = s0.landingPropMass > 0;
                int restarts = engine != null && engine.restart ? engine.restarts : 0;
                string parent = present.Contains("asc.meco") ? "asc.meco" : (present.Contains("asc.burnout") ? "asc.burnout" : "term.liftoff");
                double baseT = NominalOf(parent);

                if (recovery == "expendable")
                {
                    Absent("rec.boostback", "Boostback is absent because recovery is expendable.");
                    Absent("rec.entry", "Entry burn is absent because recovery is expendable.");
                    Absent("rec.landing", "Landing burn is absent because recovery is expendable.");
                }
                else if (recovery == "parachute")
                {
                    Absent("rec.boostback", "Boostback is absent because this recovery is a parachute, not a propulsive return.");
                    Absent("rec.entry", "Entry burn is absent because this recovery is a parachute.");
                    Absent("rec.landing", "Landing burn is absent because this recovery is a parachute.");
                }
                else if (recovery == "droneship" || recovery == "rtls")
                {
                    int need = recovery == "rtls" ? 3 : 2;
                    if (!legs || !prop || engine == null || !engine.restart || restarts < need)
                    {
                        string why = "Propulsive " + recovery + " is absent because it needs legs, landing propellant, and " + need + " restarts after ascent. This stage has legs=" + (legs ? "yes" : "no") + ", landing propellant " + s0.landingPropMass.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " kg, restarts " + restarts + ".";
                        Absent("rec.boostback", why);
                        Absent("rec.entry", why);
                        Absent("rec.landing", why);
                    }
                    else
                    {
                        var hw = Hw("legs", "landingProp:s1", "engine:" + engine.id);
                        if (recovery == "rtls")
                        {
                            AddAt("rec.boostback", "Boostback burn", "Boostback", TimelinePhase.Recovery, TimelineClock.Round(baseT + 20),
                                TimelineCriticality.Critical, TimelineVisibility.Standard,
                                "Boostback because recovery is return-to-launch-site and " + engine.id + " has " + restarts + " restarts, enough for boostback, entry, and landing. The ascent sim does not fly the return.",
                                new[] { parent }, hw, Trigger.After(parent, 20), null, false, 1, music: "CUE-13");
                        }
                        else Absent("rec.boostback", "Boostback is absent because recovery is a downrange droneship. Entry and landing burns still happen; the booster does not fly back to the pad.");

                        double entryDelay = recovery == "rtls" ? 200 : 180;
                        string entryParent = recovery == "rtls" ? "rec.boostback" : parent;
                        double entryBase = recovery == "rtls" ? NominalOf("rec.boostback") : baseT;
                        AddAt("rec.entry", "Entry burn", "Entry", TimelinePhase.Recovery, TimelineClock.Round(entryBase + (recovery == "rtls" ? 180 : entryDelay)),
                            TimelineCriticality.Critical, TimelineVisibility.Kid,
                            "Entry burn for the " + recovery + " recovery, after " + parent + ". Not simulated; the ascent model stops at cutoff.",
                            new[] { entryParent }, hw, Trigger.After(entryParent, recovery == "rtls" ? 180 : entryDelay), null, false, 1,
                            narration: "entry", music: "CUE-13");
                        double landDelay = recovery == "rtls" ? 280 : 240;
                        AddAt("rec.landing", "Landing burn", "Landing", TimelinePhase.Recovery, TimelineClock.Round(NominalOf("rec.entry") + landDelay),
                            TimelineCriticality.Critical, TimelineVisibility.Kid,
                            "Landing burn closes the " + recovery + " recovery. Touchdown is scored after this burn; it is not a separate generated event.",
                            new[] { "rec.entry" }, hw, Trigger.After("rec.entry", landDelay), null, false, 1,
                            narration: "landing", music: "CUE-13", telemetry: new[] { "alt", "v" });
                    }
                }

                if (mission.stageParachute || recovery == "parachute" && mission.stageParachute)
                    AddAt("rec.chute.stage", "Stage parachute", "Chute", TimelinePhase.Recovery, TimelineClock.Round(baseT + 40),
                        TimelineCriticality.Critical, TimelineVisibility.Kid,
                        "Stage parachute because the mission carries parachute hardware on the stage.",
                        new[] { parent }, Hw("parachute:stage"), Trigger.After(parent, 40), null, false, 1);
                else Absent("rec.chute.stage", "Stage parachute is absent because the mission has no stage parachute hardware.");

                if (mission.fairingParachute && hasFairing)
                    AddAt("rec.chute.fairing", "Fairing parachute", "Chute fair", TimelinePhase.Recovery, TimelineClock.Round((present.Contains("stg.fairing") ? NominalOf("stg.fairing") : baseT) + 20),
                        TimelineCriticality.Nominal, TimelineVisibility.Standard,
                        "Fairing parachute because the mission carries parachute hardware on the fairing.",
                        new[] { present.Contains("stg.fairing") ? "stg.fairing" : parent }, Hw("parachute:fairing", "fairing"),
                        Trigger.After(present.Contains("stg.fairing") ? "stg.fairing" : parent, 20), null, false, 1);
                else Absent("rec.chute.fairing", hasFairing
                    ? "Fairing parachute is absent because the mission has no fairing parachute hardware."
                    : "Fairing parachute is absent because the vehicle has no fairing.");

                Absent("rec.touchdown", "Touchdown is absent as its own event. It is the grade after the landing burn, and the ascent sim does not fly it.");
            }

            void EmitBranches()
            {
                var scrub = EnsureBranch("scrub", "Scrub", "Scrub", TimelinePhase.Terminal,
                    "Scrub ends the count. Propellant cost is refunded in Kc; development cost is not.",
                    "term.poll", Hw("vehicle"), refund: propRefund);
                Rule("term.poll", "no-go on the poll", "scrub");
                foreach (var e in items)
                    if (e.Ev.id.IndexOf(".leak", StringComparison.Ordinal) >= 0)
                        Rule(e.Ev.id, "hydrogen sniff above the pad limit", "scrub");

                EnsureBranch("recycle", "Recycle", "Recycle", TimelinePhase.Terminal,
                    "Recycle sends the count back to the terminal count after a thrust failure before release.",
                    "ign.thrustok", Hw("vehicle"));
                Rule("ign.thrustok", "thrust below the commit gate before release", "recycle");

                EnsureBranch("abort.pad", "Pad abort", "Abort pad", TimelinePhase.Ignition,
                    "Pad abort when thrust or the release commit fails and the vehicle is still on the pad.",
                    "ign.thrustok", Hw("vehicle"), narration: "abort", music: "CUE-14");
                Rule("ign.thrustok", "engine below commit thrust at release", "abort.pad");

                if (present.Contains("asc.maxq"))
                {
                    EnsureBranch("abort.maxq", "Max-Q abort", "Abort Max-Q", TimelinePhase.Ascent,
                        "Abort when dynamic pressure exceeds the vehicle limit at Max-Q.",
                        "asc.maxq", Hw("vehicle"), narration: "abort", music: "CUE-14");
                    Rule("asc.maxq", "dynamic pressure above the vehicle limit", "abort.maxq");
                }
                else Absent("abort.maxq", "Max-Q abort is absent because Max-Q itself is absent.");

                if (present.Contains("stg.sep.1") || present.Contains("stg.booster"))
                {
                    string parent = present.Contains("stg.sep.1") ? "stg.sep.1" : "stg.booster";
                    EnsureBranch("abort.staging", "Separation abort", "Abort sep", TimelinePhase.Staging,
                        "Abort when separation recontacts. The nominal timeline does not fly this branch.",
                        parent, Hw("vehicle"), narration: "abort", music: "CUE-14");
                    if (present.Contains("stg.sep.1")) Rule("stg.sep.1", "recontact in the separation corridor", "abort.staging");
                    if (present.Contains("stg.booster")) Rule("stg.booster", "booster recontact", "abort.staging");
                }
                else Absent("abort.staging", "Separation abort is absent because this vehicle does not separate a stage or a booster.");

                if (needsAbort)
                {
                    var crewHw = new List<string> { "abortSystem" };
                    if (hardware.Contains("crew")) crewHw.Add("crew");
                    EnsureBranch("abort.crew", "Crew abort", "Crew abort", TimelinePhase.Ignition,
                        "Crew abort because the payload is flagged needs-abort. The branch exists from ignition onward.",
                        "ign.light", crewHw, narration: "abort", music: "CUE-14");
                    Rule("ign.light", "crew abort threshold", "abort.crew");
                }
                else Absent("abort.crew", "Crew abort is absent because the payload is not crewed and does not need an abort system.");

                Absent("term.hold.unplanned", "An unplanned hold is not its own event. A holdable event, starting with the go/no-go poll, takes status 'held'.");
            }

            void EmitFamilyAbsences()
            {
                if (!families.Contains("kerolox")) Absent("count.fuel.s1.load", "Kerolox fuel load is absent because no stage flies a kerolox propellant.");
                if (!families.Contains("hydrolox")) Absent("count.lh2.s1.chill", "Hydrogen chilldown, slow/fast fill, leak check, and replenish are absent because no stage flies hydrolox.");
                if (!families.Contains("methalox")) Absent("count.ch4.s1.chill", "Methane chilldown is absent because no stage flies methalox.");
                if (!families.Contains("hypergolic")) Absent("count.hyper.s1.offclock", "Off-clock hypergolic load is absent because no stage flies a storable hypergolic.");
                if (!families.Contains("solid")) Absent("count.solid.s1.arm", "Solid arming is absent because no stage is a solid motor.");
                if (!families.Contains("hybrid")) Absent("count.n2o.s1.load", "Hybrid oxidizer load is absent because no stage is a hybrid.");
            }

            Timeline Finish(SimResult sim)
            {
                items.Sort((a, b) =>
                {
                    int cmp = a.Ev.nominal.CompareTo(b.Ev.nominal);
                    return cmp != 0 ? cmp : a.Seq.CompareTo(b.Seq);
                });
                var events = new List<MissionEvent>(items.Count);
                foreach (var it in items) events.Add(it.Ev);
                branches.Sort((a, b) =>
                {
                    int cmp = a.nominal.CompareTo(b.nominal);
                    if (cmp != 0) return cmp;
                    return string.CompareOrdinal(a.id, b.id);
                });
                var abs = new List<Absence>(absent.Count);
                var keys = new List<string>(absent.Keys);
                keys.Sort(StringComparer.Ordinal);
                foreach (var k in keys) abs.Add(new Absence { id = k, explainWhy = absent[k] });
                var hw = new List<string>(hardware);
                hw.Sort(StringComparer.Ordinal);
                var engines = new List<StageEngineFact>();
                foreach (var p in parts)
                    engines.Add(new StageEngineFact { stageKey = p.key, engineId = p.engine.id, restart = p.engine.restart, restarts = p.engine.restarts });
                engines.Sort((a, b) => string.CompareOrdinal(a.stageKey, b.stageKey));
                var tl = new Timeline
                {
                    vehicleId = d.id, padId = pad.id,
                    missionId = string.IsNullOrEmpty(mission.id) ? "msn." + (d.id ?? "vehicle") : mission.id,
                    mode = mode, recovery = recovery, seed = seed, latitude = latitude,
                    hardware = hw, engines = engines, events = events, branches = branches, absent = abs,
                };
                tl.checks = TimelineChecks.Evaluate(tl);
                // sim outcome is not a schema field; checks cover the sequence. Keep the warning-free path.
                if (sim.warnings != null && sim.warnings.Count > 0) { /* catalog warnings stay on the sim, not the timeline */ }
                return tl;
            }

            void Absent(string id, string why)
            {
                if (present.Contains(id) || absent.ContainsKey(id)) return;
                absent[id] = why;
            }

            static bool OnClock(Part p) => p.family == "kerolox" || p.family == "hydrolox" || p.family == "methalox" || p.family == "hybrid";

            string Note(double real)
            {
                var game = TimelineClock.Clock(Game(real));
                var wall = TimelineClock.Clock(real);
                if (mode == "engineer") return "Game time keeps the real " + wall + ".";
                return "Real " + wall + " compresses to " + game + " in " + mode + " mode.";
            }

            double Game(double real) => TimelineClock.Round(TimelineClock.GameSeconds(real, mode));

            double NominalOf(string id)
            {
                foreach (var it in items) if (it.Ev.id == id) return it.Ev.nominal;
                foreach (var b in branches) if (b.id == id) return b.nominal;
                return 0;
            }

            static string Fit(string s) => s.Length <= 12 ? s : s.Substring(0, 12);

            static List<string> Hw(params string[] ids)
            {
                var list = new List<string>();
                foreach (var id in ids) if (!string.IsNullOrEmpty(id)) list.Add(id);
                return list;
            }

            static string TimelineTriggerFrom(string phase) => phase == TimelinePhase.Load ? "padLoadData" : "liftoff";

            MissionEvent AddReal(string id, string full, string shortL, string phase, double real, string crit, string vis, string why,
                string[] pres, List<string> hw, bool holdable, string from,
                long cost = 0, string status = null, string narration = null, string music = null, string[] telemetry = null)
            {
                double nominal = Game(real);
                return AddAt(id, full, shortL, phase, nominal, crit, vis, why, pres, hw,
                    Trigger.TRel(nominal, from), null, holdable, phase == TimelinePhase.Ascent ? 0.5 : -1,
                    cost, 0, status, narration, music, telemetry);
            }

            MissionEvent AddAt(string id, string full, string shortL, string phase, double nominal, string crit, string vis, string why,
                string[] pres, List<string> hw, Trigger trigger, double? actual, bool holdable, double tolerance,
                long cost = 0, long refund = 0, string status = null, string narration = null, string music = null, string[] telemetry = null)
            {
                if (!present.Add(id)) throw new InvalidOperationException("duplicate event " + id);
                if (absent.ContainsKey(id)) absent.Remove(id);
                if (shortL != null && shortL.Length > 12) shortL = shortL.Substring(0, 12);
                double tol = tolerance >= 0 ? tolerance : (mode == "kid" ? 0.4 : mode == "engineer" ? 0.02 : 0.15);
                string state = status ?? TimelineStatus.Pending;
                var ev = new MissionEvent
                {
                    id = id,
                    name = "tl." + id + ".name",
                    shortLabel = "tl." + id + ".short",
                    phase = phase,
                    trigger = trigger,
                    preconditions = pres == null ? new List<string>() : new List<string>(pres),
                    holdable = holdable,
                    criticality = crit,
                    nominal = TimelineClock.Round(nominal),
                    actual = actual.HasValue ? (double?)TimelineClock.Round(actual.Value) : null,
                    tolerance = tol,
                    variance = Variance(id, tol),
                    status = new EventStatus { state = state, progress = state == TimelineStatus.Done ? 1 : 0 },
                    links = new EventLinks
                    {
                        narration = narration == null ? new List<string>() : new List<string> { narration },
                        sfx = new List<string> { id },
                        music = music == null ? new List<string>() : new List<string> { music },
                        telemetry = telemetry == null ? new List<string>() : new List<string>(telemetry),
                        pad = hw ?? new List<string>(),
                        codex = new List<string> { "codex." + id },
                    },
                    visibility = vis,
                    economy = new EconomyHooks { costOnDone = cost, refundOnScrub = refund, currency = "Kc" },
                    strings = new EventStrings { name = full, shortLabel = shortL, explainWhy = why, anomaly = "" },
                };
                items.Add(new Item { Ev = ev, Seq = seq++ });
                return ev;
            }

            double Variance(string id, double tol)
            {
                if (tol <= 0) return 0;
                uint h = seed == 0 ? 1u : seed;
                for (int i = 0; i < id.Length; i++) h = unchecked(h * 16777619u ^ id[i]);
                double u = (h & 0xFFFFFF) / (double)0x1000000;
                return TimelineClock.Round((u - 0.5) * 2 * tol);
            }

            MissionEvent EnsureBranch(string id, string full, string shortL, string phase, string why, string parent, List<string> hw,
                long refund = 0, string narration = null, string music = null)
            {
                if (branchById.TryGetValue(id, out var existing)) return existing;
                double nominal = NominalOf(parent);
                var ev = new MissionEvent
                {
                    id = id,
                    name = "tl." + id + ".name",
                    shortLabel = "tl." + id + ".short",
                    phase = phase,
                    trigger = Trigger.Action("autopilot", id),
                    preconditions = new List<string> { parent },
                    holdable = false,
                    criticality = TimelineCriticality.Safety,
                    nominal = nominal,
                    actual = null,
                    tolerance = 0,
                    variance = 0,
                    status = new EventStatus { state = TimelineStatus.Pending, progress = 0 },
                    links = new EventLinks
                    {
                        narration = narration == null ? new List<string>() : new List<string> { narration },
                        sfx = new List<string> { id },
                        music = music == null ? new List<string>() : new List<string> { music },
                        pad = hw,
                        codex = new List<string> { "codex." + id },
                    },
                    visibility = TimelineVisibility.Kid,
                    economy = new EconomyHooks { refundOnScrub = refund, currency = "Kc" },
                    strings = new EventStrings { name = full, shortLabel = shortL, explainWhy = why, anomaly = why },
                };
                branches.Add(ev);
                branchById[id] = ev;
                return ev;
            }

            void Rule(string eventId, string condition, string branchId)
            {
                foreach (var it in items)
                {
                    if (it.Ev.id != eventId) continue;
                    it.Ev.abortRules.Add(new AbortRule { condition = condition, branchId = branchId });
                    it.Ev.blocks.Add(branchId);
                    if (string.IsNullOrEmpty(it.Ev.strings.anomaly)) it.Ev.strings.anomaly = condition;
                    return;
                }
            }
        }
    }
}
