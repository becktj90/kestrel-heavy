// One undoable pass that clears the guided-step warnings.
// Up to 12 changes, cheapest first. The design is replaced only with moves that
// improve the warning list, and a trip that already has speed to spare is not
// pushed short on purpose. The player sees one plain before/after summary.
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core;
using Kestrel.Core.Data;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Designer.Stepper
{
    public sealed class StepperFixResult
    {
        public Edit Edit = Edit.None;
        public string Instruction;
        public string Field;
        public bool Cleared;
    }

    /// <summary>Max-Q throttle as a percent. Saves may store 95 or the older 0.95 fraction.</summary>
    public static class MaxQThrottle
    {
        public const double FloorPct = 40;
        public const double StepPct = 5;
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static double Percent(JsonObject design)
        {
            var guidance = design?.Obj("guidance");
            if (guidance == null || !guidance.ContainsKey("maxQThrottle")) return 100;
            double raw = guidance.Num("maxQThrottle");
            if (double.IsNaN(raw)) return 100;
            return raw > 1.5 ? raw : raw * 100.0;
        }

        public static string Label(JsonObject design)
            => Math.Round(Percent(design)).ToString("0", Inv) + "%";

        /// <summary>Store a percent in the 40–100 band, snapped to 5. Unset 100% is left unset.</summary>
        public static Edit SetPercent(JsonObject design, double pct)
        {
            if (design == null || double.IsNaN(pct)) return Edit.None;
            pct = Math.Round(pct / StepPct) * StepPct;
            if (pct < FloorPct) pct = FloorPct;
            if (pct > 100) pct = 100;
            var guidance = design.Obj("guidance");
            if (guidance == null)
            {
                guidance = new JsonObject();
                JsonMut.Set(design, "guidance", guidance);
            }
            bool stored = guidance.ContainsKey("maxQThrottle");
            double raw = stored ? guidance.Num("maxQThrottle") : 100;
            bool asPercent = stored && raw > 1.5;
            if (asPercent && Math.Abs(raw - pct) < 0.01) return Edit.None;
            if (!stored && Math.Abs(pct - 100) < 0.01) return Edit.None;
            string from = Label(design);
            JsonMut.Set(guidance, "maxQThrottle", pct);
            return new Edit("Max-Q throttle", from, Math.Round(pct).ToString("0", Inv) + "%");
        }

        public static Edit Nudge(JsonObject design, int dir)
            => SetPercent(design, Percent(design) + dir * StepPct);
    }

    public static class StepperFix
    {
        public const int MaxMoves = 12;
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        sealed class Picture
        {
            public int Score;
            public double Margin = double.NaN;
            public double Twr;
            public double QLim;
            public double Peak;
            public double? MaxQ;
            public bool Measured;
            public bool Flown;
            public readonly HashSet<string> Codes = new HashSet<string>();
            readonly HashSet<string> Blocks = new HashSet<string>();

            public bool Has(string code) => code != null && Codes.Contains(code);
            public bool Air => Has("maxq-high") || Has("structural-maxq") || Has("risk-structural-maxq");
            public bool Tumble => Has("tumble") || Has("risk-tumble");
            public bool Structural => Has("structural-maxq") || Has("risk-structural-maxq");

            public void Add(string code, bool block)
            {
                if (string.IsNullOrEmpty(code)) return;
                if (Codes.Add(code))
                {
                    Score += block ? 100 : 10;
                    if (block) Blocks.Add(code);
                    return;
                }
                if (block && Blocks.Add(code)) Score += 90;
            }
        }

        sealed class Candidate
        {
            public int Cost;
            public int Order;
            public bool Air;
            public bool Resim;
            public bool Carry;
            public bool CoverWalls;
            public Func<JsonObject, Edit> Apply;
        }

        public static StepperFixResult Apply(Catalog catalog, JsonObject design, StepCheck check)
        {
            var result = new StepperFixResult();
            result.Field = check != null && !string.IsNullOrEmpty(check.Field) ? check.Field : "stages";
            if (catalog == null || design == null || check == null || string.IsNullOrEmpty(check.Code))
            {
                result.Instruction = "There is no single change for this warning.";
                return result;
            }

            var work = JsonMut.CloneObject(design);
            var now = Look(catalog, work);
            if (now.Score <= 0)
            {
                result.Cleared = true;
                result.Instruction = "Nothing to change.";
                return result;
            }

            var notes = new List<Edit>();
            string tradeoff = null;
            for (int step = 0; step < MaxMoves && now.Score > 0; step++)
            {
                var cands = Candidates(catalog, work, now);
                cands.Sort((a, b) => a.Cost != b.Cost ? a.Cost.CompareTo(b.Cost) : a.Order.CompareTo(b.Order));
                bool took = false;
                for (int i = 0; i < cands.Count; i++)
                {
                    var cand = cands[i];
                    var trial = JsonMut.CloneObject(work);
                    Edit edit;
                    try { edit = cand.Apply(trial); }
                    catch (Exception) { continue; }
                    if (!edit.changed) continue;

                    var extras = new List<Edit>();
                    Picture after;
                    try { after = Settle(catalog, work, trial, now, cand, extras); }
                    catch (Exception) { continue; }
                    if (after == null) continue;
                    if (now.Measured && !after.Measured) continue;
                    if (MarginBroken(now, after))
                    {
                        tradeoff = MarginTradeoff(cand, now, after);
                        continue;
                    }
                    if (!now.Tumble && after.Tumble) continue;
                    if (Blowup(now, after)) continue;
                    // A throttle cap now holds the kick for the whole max-Q bucket. Reject a
                    // move that turns a clean flight into a breakup or an empty tank.
                    if (BrokeTheFlight(catalog, work, trial)) continue;

                    bool drop = after.Score < now.Score;
                    double qDrop = now.MaxQ.HasValue && after.MaxQ.HasValue ? now.MaxQ.Value - after.MaxQ.Value : 0;
                    bool airBetter = cand.Air && qDrop >= 0.2 && !(!now.Structural && after.Structural);
                    if (!drop && !airBetter) continue;

                    Replace(work, trial);
                    if (edit.changed) notes.Add(edit);
                    for (int n = 0; n < extras.Count; n++) if (extras[n].changed) notes.Add(extras[n]);
                    now = after;
                    took = true;
                    break;
                }
                if (!took) break;
            }

            string summary = Summarize(notes);
            bool changed = HangarSession.Snapshot(work) != HangarSession.Snapshot(design);
            if (changed) Replace(design, work);
            result.Cleared = now.Score <= 0 && changed;
            if (now.Score <= 0)
                result.Instruction = string.IsNullOrEmpty(summary) ? "Nothing to change." : summary;
            else
                result.Instruction = Explain(catalog, design, now, summary, tradeoff);
            result.Edit = changed ? new Edit(string.IsNullOrEmpty(summary) ? "Updated the rocket" : summary) : Edit.None;
            if (!changed) result.Cleared = false;
            return result;
        }

        static Picture Settle(Catalog catalog, JsonObject from, JsonObject trial, Picture before, Candidate cand, List<Edit> extras)
        {
            if (before.Measured && cand.Carry)
                CarryAirLoad(catalog, from, trial);
            else if (before.Measured && cand.Resim)
                ReferenceAscent.Run(catalog, trial);
            else if (before.Measured)
                CarryAirLoad(catalog, from, trial);

            var after = Look(catalog, trial);
            if (!cand.CoverWalls || !after.Measured || !after.MaxQ.HasValue || !(after.QLim > 0)) return after;
            if (after.Tumble) return after;
            if (after.MaxQ.Value <= StepperChecks.MaxQAmberFraction * after.QLim + 0.05) return after;
            var donor = JsonMut.CloneObject(trial);
            var raised = RaiseStrength(trial, after.MaxQ.Value, after.QLim);
            if (!raised.changed) return after;
            CarryAirLoad(catalog, donor, trial);
            extras.Add(raised);
            return Look(catalog, trial);
        }

        static bool MarginBroken(Picture before, Picture after)
        {
            if (double.IsNaN(before.Margin) || double.IsNaN(after.Margin)) return false;
            return before.Margin >= -1e-6 && after.Margin < -1e-4;
        }

        static bool Blowup(Picture before, Picture after)
        {
            if (!before.MaxQ.HasValue || !after.MaxQ.HasValue) return false;
            return after.MaxQ.Value > 200 && after.MaxQ.Value > before.MaxQ.Value * 2;
        }

        static string MarginTradeoff(Candidate cand, Picture before, Picture after)
        {
            if (cand.Air)
                return "Easing off another 5 percent at Max-Q would leave the trip short on speed.";
            if (!double.IsNaN(after.Margin))
                return "That change would leave the trip " + N0(Math.Abs(after.Margin) * 100) + " percent short on speed.";
            return "That change would leave the trip short on speed.";
        }

        static List<Candidate> Candidates(Catalog catalog, JsonObject design, Picture now)
        {
            var list = new List<Candidate>();
            int n = 0;
            void Add(int cost, bool air, bool resim, bool carry, bool cover, Func<JsonObject, Edit> apply)
            {
                list.Add(new Candidate
                {
                    Cost = cost, Order = n++, Air = air, Resim = resim, Carry = carry, CoverWalls = cover, Apply = apply,
                });
            }

            if (now.Has("bad-mixture"))
                Add(1, false, false, true, false, ZeroMixture);

            if (now.Has("interstage-shape"))
                Add(2, false, false, now.Measured, false, MatchJoints);

            if (now.Has("payload-g") || now.Has("overg") || now.Has("risk-overg"))
                Add(2, false, now.Measured, false, false, d => LowerG(catalog, d));

            if (now.Has("landing-hardware") || now.Has("rcs-for-landing"))
                Add(3, false, false, true, false, Gear);

            if (now.Air && now.Measured)
                Add(3, false, false, true, false, d => RaiseStrength(d, now.MaxQ ?? 0, now.QLim));

            if (now.Has("unstable") || now.Has("tumble") || now.Has("risk-tumble") || now.Has("overstable"))
                Add(4, false, now.Flown, now.Measured && !now.Flown, false, d => Steady(catalog, d, now.Has("overstable")));

            if (now.Air)
                Add(4, true, true, false, true, EaseThrottle);

            // Extra propellant sheds dynamic pressure and peak g without freezing the kick.
            if (now.Air || now.Has("payload-g") || now.Has("overg") || now.Has("risk-overg"))
                Add(5, true, true, false, false, d => Soften(catalog, d));

            if (now.Has("payload-doesnt-fit") || now.Has("fairing-narrow"))
                Add(5, false, now.Measured, !now.Measured, false, d => GrowFairing(catalog, d));

            if (now.Has("twr-low") || now.Has("twr-marginal") || now.Has("zero-engines") || now.Has("weak-upper") || now.Has("engine-out"))
                Add(5, false, now.Measured, false, false, d => MorePush(catalog, d, now));

            if (now.Has("dv-margin") || now.Has("prop-depletion") || now.Has("weak-upper-dv") || now.Has("risk-prop-depletion")
                || (!double.IsNaN(now.Margin) && now.Margin < DesignerLimits.Active.minMargin))
                Add(6, false, now.Measured, false, false, d => MoreFuel(catalog, d, now));

            Add(9, false, now.Measured, false, false, d => Fallback(catalog, d, now));
            return list;
        }

        static Picture Look(Catalog catalog, JsonObject design)
        {
            var picture = new Picture();
            StepperReport report;
            try { report = StepperModel.Evaluate(catalog, design); }
            catch (Exception)
            {
                picture.Score = 1000;
                return picture;
            }
            var readout = report.Readout;
            if (readout != null)
            {
                picture.Margin = readout.margin;
                picture.Twr = readout.liftoffTWR;
                picture.QLim = readout.qLim_kPa ?? 0;
                picture.Peak = readout.peakG ?? 0;
                if (!readout.maxQStale && readout.maxQ_kPa.HasValue)
                {
                    picture.MaxQ = readout.maxQ_kPa.Value;
                    picture.Measured = true;
                }
            }
            if (report.Advisories != null)
            {
                for (int i = 0; i < report.Advisories.Count; i++)
                {
                    var advisory = report.Advisories[i];
                    if (advisory == null || advisory.severity == AdvisorySeverity.Note) continue;
                    picture.Add(advisory.code, advisory.severity == AdvisorySeverity.Block);
                }
            }
            for (int i = 1; i < report.Checks.Length; i++)
            {
                var step = report.Checks[i];
                if (step == null) continue;
                Consider(picture, step.Code, step.Gate);
                for (int f = 0; f < step.Findings.Count; f++)
                {
                    var finding = step.Findings[f];
                    if (finding == null) continue;
                    Consider(picture, finding.Code, finding.Level == "red" ? StepGate.Red : StepGate.Amber);
                }
            }
            if (ReferenceAscent.TryGet(ReferenceAscent.CanonicalKey(catalog, design), out var flight) && flight.hit)
                picture.Flown = flight.flown;
            return picture;
        }

        static void Consider(Picture picture, string code, StepGate gate)
        {
            if (string.IsNullOrEmpty(code)) return;
            if (code == "risks-stale" || code == "landing-need-stale" || code == "recovery-skip") return;
            if (gate == StepGate.Pass || gate == StepGate.Skip) return;
            picture.Add(code, gate == StepGate.Red);
        }

        static Edit ZeroMixture(JsonObject design)
        {
            double from = 0;
            bool any = false;
            void One(JsonObject stage)
            {
                if (stage == null || !stage.ContainsKey("mixtureOffsetPct")) return;
                double value = stage.Num("mixtureOffsetPct");
                if (Math.Abs(value) <= MixtureOffset.HazardPct) return;
                if (!any) from = value;
                JsonMut.Set(stage, "mixtureOffsetPct", 0d);
                any = true;
            }
            var stages = design.Arr("stages");
            if (stages != null)
                for (int i = 0; i < stages.Count; i++) One(stages[i] as JsonObject);
            One(design.Obj("strapOns"));
            if (!any) return Edit.None;
            return new Edit("Mixture offset", N(from) + "%", "0%");
        }

        static Edit LowerG(Catalog catalog, JsonObject design)
        {
            double cap = PayloadCap(catalog, design);
            double current = ShownG(design, cap);
            double target = Math.Max(1.5, cap - 0.5);
            target = Math.Round(target * 2, MidpointRounding.AwayFromZero) / 2.0;
            if (target > cap) target = cap;
            if (current <= target + 1e-6) return Edit.None;
            return SetG(design, current, target);
        }

        static Edit Gear(JsonObject design)
        {
            var legs = HangarEditor.SetLegs(design, true);
            var fins = HangarEditor.SetGridFins(design, true);
            var jets = HangarEditor.SetRcs(design, true);
            if (!legs.changed && !fins.changed && !jets.changed) return Edit.None;
            return new Edit("Landing gear", "off", "on");
        }

        static Edit RaiseStrength(JsonObject design, double maxQ, double qLim)
        {
            var lim = DesignerLimits.Active;
            double sf = design.Num("safetyFactor", 1.4);
            if (!(maxQ > 0) || !(qLim > 0) || !(sf > 0)) return Edit.None;
            double line = StepperChecks.MaxQAmberFraction * qLim;
            if (maxQ <= line + 1e-6) return Edit.None;
            double need = sf * maxQ / line;
            double target = Ceil(need, lim.safetyStep, lim.safetyMin, lim.safetyMax);
            if (target <= sf + 1e-6) return Edit.None;
            var edit = HangarEditor.SetSafetyFactor(design, target);
            if (!edit.changed) return Edit.None;
            return new Edit("Strength", edit.from, edit.to);
        }

        static Edit Steady(Catalog catalog, JsonObject design, bool overstable)
        {
            if (overstable) return Loosen(design);
            var lim = DesignerLimits.Active;
            double span = design.Obj("aero")?.Num("finSpanM", 1) ?? 1;
            bool on = design.Obj("aero") != null && design.Obj("aero").Bool("fins");
            double start = on ? span : Math.Max(lim.finSpanMin, 1);
            double chosen = 0;
            for (double trySpan = start; trySpan <= lim.finSpanMax + 1e-6; trySpan += Math.Max(0.5, lim.finSpanStep))
            {
                var probe = JsonMut.CloneObject(design);
                HangarEditor.SetFins(probe, true);
                HangarEditor.SetFinSpan(probe, trySpan);
                var look = Look(catalog, probe);
                if (!look.Has("unstable") && !look.Has("tumble") && !look.Has("overstable"))
                {
                    chosen = probe.Obj("aero").Num("finSpanM", trySpan);
                    break;
                }
            }
            if (!(chosen > 0)) return Edit.None;
            string from = on ? N(span) + " m" : "off";
            HangarEditor.SetFins(design, true);
            HangarEditor.SetFinSpan(design, chosen);
            return new Edit("Fins", from, N(design.Obj("aero").Num("finSpanM", chosen)) + " m");
        }

        static Edit Loosen(JsonObject design)
        {
            var aero = design.Obj("aero");
            var lim = DesignerLimits.Active;
            double span = aero?.Num("finSpanM", 1) ?? 1;
            if (span > lim.finSpanMin + 1e-6)
            {
                var edit = HangarEditor.SetFinSpan(design, span - Math.Max(0.5, lim.finSpanStep * 4));
                if (!edit.changed) return Edit.None;
                return new Edit("Fins", edit.from, edit.to);
            }
            var off = HangarEditor.SetFins(design, false);
            return off.changed ? new Edit("Fins", "on", "off") : Edit.None;
        }

        static Edit EaseThrottle(JsonObject design)
        {
            double cur = MaxQThrottle.Percent(design);
            if (cur <= MaxQThrottle.FloorPct + 0.01) return Edit.None;
            return MaxQThrottle.SetPercent(design, cur - MaxQThrottle.StepPct);
        }

        static Edit GrowFairing(Catalog catalog, JsonObject design)
        {
            var fairing = design.Obj("fairing");
            double length = fairing != null ? fairing.Num("length", 1) : 1;
            double diameter = fairing != null ? fairing.Num("diameter", 1) : 1;
            double volume = PayloadVolume(catalog, design);
            var lim = DesignerLimits.Active;
            double targetD = diameter;
            double targetL = length;
            if (volume > 0 && diameter > 0)
            {
                double radius = PayloadFit.RadiusFactor * diameter;
                double denom = Math.PI * radius * radius * PayloadFit.LengthFactor;
                if (denom > 0) targetL = volume / denom * 1.08;
            }
            targetL = Ceil(Math.Max(targetL, length + 0.5), 0.1, lim.fairingLengthMin, lim.fairingLengthMax);
            if (targetL <= length + 1e-6 && diameter < lim.diameterMax - 0.05)
            {
                targetD = Ceil(diameter + 0.5, 0.1, lim.diameterMin, lim.diameterMax);
                double radius = PayloadFit.RadiusFactor * targetD;
                double denom = Math.PI * radius * radius * PayloadFit.LengthFactor;
                if (volume > 0 && denom > 0) targetL = Ceil(volume / denom * 1.08, 0.1, lim.fairingLengthMin, lim.fairingLengthMax);
            }
            var edit = ApplyFairing(design, targetL, targetD);
            if (!edit.changed)
            {
                edit = ApplyFairing(design, lim.fairingLengthMax, Math.Min(lim.diameterMax, Math.Max(targetD, diameter + 1)));
            }
            if (!edit.changed) return Edit.None;
            var look = Look(catalog, design);
            if (!double.IsNaN(look.Margin) && look.Margin < 0)
            {
                try
                {
                    var solved = DvMarginFix.Solve(catalog, design);
                    if (solved != null && solved.Solved && solved.Changed && solved.Design != null)
                        Replace(design, solved.Design);
                }
                catch (Exception) { }
            }
            return new Edit("Fairing", N(length) + " m", N(design.Obj("fairing")?.Num("length") ?? targetL) + " m");
        }

        static Edit ApplyFairing(JsonObject design, double length, double diameter)
        {
            var lengthEdit = HangarEditor.SetFairingLength(design, length);
            var widthEdit = HangarEditor.SetDiameter(design, PartRef.Fairing, diameter);
            if (!lengthEdit.changed && !widthEdit.changed) return Edit.None;
            return new Edit("Fairing", null, null);
        }

        static Edit MorePush(Catalog catalog, JsonObject design, Picture now)
        {
            Edit liftoff = Edit.None;
            if (now.Has("twr-low") || now.Has("twr-marginal") || now.Has("zero-engines") || now.Has("engine-out"))
                liftoff = LiftoffEngines(design, now.Twr);
            Edit upper = now.Has("weak-upper") ? StrengthenUpper(catalog, design) : Edit.None;
            if (liftoff.changed && upper.changed)
                return new Edit("Engines " + liftoff.from + " → " + liftoff.to + ". " + upper.what + " " + upper.from + " → " + upper.to);
            return upper.changed ? upper : liftoff;
        }

        /// <summary>
        /// One more engine on the weak upper stage. A bell that does not fit widens that stage
        /// (and the stage under it, when a wider top would look top-heavy) in the same change.
        /// </summary>
        static Edit StrengthenUpper(Catalog catalog, JsonObject design)
        {
            int stage = WeakStage(catalog, design);
            int fromCount = Count(design, stage);
            double fromDia = StageAt(design, stage)?.Num("diameter") ?? 0;
            string engineFrom = null, engineTo = null;
            for (int n = 0; n < 6; n++)
            {
                Picture look;
                try { look = Look(catalog, design); }
                catch (Exception) { break; }
                if (!look.Has("weak-upper")) break;
                int next = Count(design, stage) + 1;
                if (next > DesignerLimits.Active.engineCountMax) break;
                bool swapped = false;
                if (!EnginesFit(catalog, StageAt(design, stage), next))
                {
                    if (!WidenToFit(catalog, design, stage, next))
                    {
                        if (!StrongerEngine(catalog, design, stage, out engineFrom, out engineTo)) break;
                        swapped = true;
                    }
                }
                if (!swapped)
                {
                    var added = HangarEditor.SetEngineCount(design, PartRef.Stage(stage), Count(design, stage) + 1);
                    if (!added.changed) break;
                }
                HoldMargin(catalog, design);
                TopUp(catalog, design);
            }
            int toCount = Count(design, stage);
            double toDia = StageAt(design, stage)?.Num("diameter") ?? fromDia;
            bool width = Math.Abs(toDia - fromDia) > 0.01;
            if (toCount == fromCount && !width && engineFrom == null) return Edit.None;
            if (engineFrom != null && toCount == fromCount && !width)
                return new Edit("Upper engine", engineFrom, engineTo);
            string from = fromCount.ToString(Inv);
            string to = toCount.ToString(Inv);
            if (width)
            {
                from = from + " at " + N(fromDia) + " m";
                to = to + " at " + N(toDia) + " m";
            }
            return new Edit(width ? "Upper stage" : "Upper engines", from, to);
        }

        static void HoldMargin(Catalog catalog, JsonObject design)
        {
            Picture look;
            try { look = Look(catalog, design); }
            catch (Exception) { return; }
            if (double.IsNaN(look.Margin) || look.Margin >= -1e-6) return;
            try
            {
                var solved = DvMarginFix.Solve(catalog, design);
                if (solved != null && solved.Solved && solved.Changed && solved.Design != null)
                    Replace(design, solved.Design);
            }
            catch (Exception) { }
        }

        static bool EnginesFit(Catalog catalog, JsonObject stage, int count)
        {
            double bell = BellDiameter(catalog, stage);
            double dia = stage?.Num("diameter") ?? 0;
            if (!(bell > 0) || !(dia > 0) || count < 1) return true;
            return count * bell * bell <= DesignerLimits.Active.pack * dia * dia + 1e-6;
        }

        static double BellDiameter(Catalog catalog, JsonObject stage)
        {
            if (stage == null || catalog == null) return 0;
            if (!catalog.EngineById.TryGetValue(stage.Str("engine") ?? "", out var engine)) return 0;
            return EnginePoint.Of(engine, stage.Str("nozzle")).diameter;
        }

        static bool WidenToFit(Catalog catalog, JsonObject design, int stage, int count)
        {
            var body = StageAt(design, stage);
            double bell = BellDiameter(catalog, body);
            if (!(bell > 0) || body == null) return false;
            var lim = DesignerLimits.Active;
            double need = bell * Math.Sqrt(count / Math.Max(lim.pack, 0.01));
            need = Ceil(need, 0.1, lim.diameterMin, lim.diameterMax);
            HangarEditor.SetDiameter(design, PartRef.Stage(stage), need);
            for (int i = stage; i > 0; i--)
            {
                var up = StageAt(design, i);
                var below = StageAt(design, i - 1);
                if (up == null || below == null) break;
                if (up.Num("diameter") <= below.Num("diameter") * lim.topHeavyRatio + 1e-6) break;
                double belowNeed = Ceil(up.Num("diameter") / lim.topHeavyRatio + 0.05, 0.1, lim.diameterMin, lim.diameterMax);
                HangarEditor.SetDiameter(design, PartRef.Stage(i - 1), belowNeed);
                if (up.Num("diameter") > StageAt(design, i - 1).Num("diameter") * lim.topHeavyRatio + 1e-6)
                    return false;
            }
            var stages = design.Arr("stages");
            int top = (stages?.Count ?? 1) - 1;
            if (stage == top)
            {
                var fairing = design.Obj("fairing");
                var topStage = StageAt(design, top);
                if (fairing != null && topStage != null && fairing.Num("diameter") + 1e-6 < topStage.Num("diameter") * lim.fairingNarrowRatio)
                    HangarEditor.SetDiameter(design, PartRef.Fairing, topStage.Num("diameter") * lim.fairingNarrowRatio);
            }
            MatchJoints(design);
            return EnginesFit(catalog, StageAt(design, stage), count);
        }

        static Edit MatchJoints(JsonObject design)
        {
            var stages = design?.Arr("stages");
            if (stages == null) return Edit.None;
            string from = null, to = null;
            for (int i = 1; i < stages.Count; i++)
            {
                var up = stages[i] as JsonObject;
                var below = stages[i - 1] as JsonObject;
                if (up == null || below == null) continue;
                string shape = DesignRules.Agree(below.Num("diameter"), up.Num("diameter"));
                var edit = HangarEditor.SetInterstageShape(design, i, shape);
                if (!edit.changed) continue;
                if (from == null) { from = edit.from; to = edit.to; }
            }
            return from == null ? Edit.None : new Edit("Joint", from, to);
        }

        static bool StrongerEngine(Catalog catalog, JsonObject design, int stage, out string from, out string to)
        {
            from = to = null;
            var body = StageAt(design, stage);
            if (body == null || catalog == null || !catalog.EngineById.TryGetValue(body.Str("engine") ?? "", out var now))
                return false;
            int count = Math.Max(1, (int)body.Num("engineCount"));
            double dia = body.Num("diameter");
            var lim = DesignerLimits.Active;
            Engine best = null;
            foreach (var engine in catalog.Engines)
            {
                if (engine == null || engine.propellant != now.propellant || engine.id == now.id) continue;
                if (!(engine.thrustVac > now.thrustVac)) continue;
                double bell = engine.diameter;
                if (!(bell > 0) || count * bell * bell > lim.pack * dia * dia + 1e-6) continue;
                if (best == null || engine.thrustVac < best.thrustVac) best = engine;
            }
            if (best == null) return false;
            var edit = HangarEditor.SetEngine(catalog, design, PartRef.Stage(stage), best.id);
            if (!edit.changed) return false;
            from = edit.from;
            to = edit.to;
            return true;
        }

        static JsonObject StageAt(JsonObject design, int index)
        {
            var stages = design?.Arr("stages");
            if (stages == null || index < 0 || index >= stages.Count) return null;
            return stages[index] as JsonObject;
        }

        static Edit LiftoffEngines(JsonObject design, double twr)
        {
            int count = Count(design, 0);
            int max = DesignerLimits.Active.engineCountMax;
            int guess = count + 1;
            if (twr > 0.05)
                guess = (int)Math.Ceiling(count * StepperChecks.PassTwr / twr - 1e-9);
            if (guess < count + 1) guess = count + 1;
            if (guess > max) guess = max;
            var edit = HangarEditor.SetEngineCount(design, PartRef.Stage(0), guess);
            if (!edit.changed) return Edit.None;
            return new Edit("Engines", edit.from, edit.to);
        }

        static Edit MoreFuel(Catalog catalog, JsonObject design, Picture now)
        {
            if (!ShortOnSpeed(now)) return Edit.None;
            bool solved = false;
            if (!double.IsNaN(now.Margin) && now.Margin < DesignerLimits.Active.minMargin)
            {
                try
                {
                    var fix = DvMarginFix.Solve(catalog, design);
                    if (fix != null && fix.Solved && fix.Changed && fix.Design != null)
                    {
                        Replace(design, fix.Design);
                        TopUp(catalog, design);
                        MatchJoints(design);
                        solved = true;
                    }
                }
                catch (Exception) { }
            }
            bool any = solved;
            for (int bump = 0; bump < 10; bump++)
            {
                Picture look;
                try { look = Look(catalog, design); }
                catch (Exception) { break; }
                if (!ShortOnSpeed(look) && look.Twr >= StepperChecks.PassTwr && !look.Has("weak-upper")) break;
                if (look.Has("weak-upper")) StrengthenUpper(catalog, design);
                if (!ShortOnSpeed(look)) break;
                if (!BumpProp(design, 1.15)) break;
                any = true;
                TopUp(catalog, design);
                MatchJoints(design);
                if (!now.Measured) continue;
                try { ReferenceAscent.Run(catalog, design); }
                catch (Exception) { break; }
            }
            if (!any) return Edit.None;
            return new Edit("Fuel", solved ? "short" : "light", solved ? "enough" : "more");
        }

        static bool ShortOnSpeed(Picture picture)
        {
            if (picture == null) return false;
            if (picture.Has("dv-margin") || picture.Has("prop-depletion") || picture.Has("risk-prop-depletion") || picture.Has("weak-upper-dv"))
                return true;
            return !double.IsNaN(picture.Margin) && picture.Margin < DesignerLimits.Active.minMargin;
        }

        static void TopUp(Catalog catalog, JsonObject design)
        {
            Picture look;
            try { look = Look(catalog, design); }
            catch (Exception) { return; }
            if (look.Twr >= StepperChecks.PassTwr && !look.Has("twr-low") && !look.Has("twr-marginal")) return;
            LiftoffEngines(design, look.Twr > 0.05 ? look.Twr : 0.2);
        }

        static bool BrokeTheFlight(Catalog catalog, JsonObject before, JsonObject after)
            => !Failed(catalog, before) && Failed(catalog, after);

        static bool Failed(Catalog catalog, JsonObject design)
        {
            if (catalog == null || design == null) return false;
            if (!ReferenceAscent.TryGet(ReferenceAscent.CanonicalKey(catalog, design), out var flight) || !flight.hit || !flight.flown)
                return false;
            return flight.failure != null;
        }

        /// <summary>
        /// Add propellant until the air load and the peak g calm down. A max-Q throttle
        /// cap holds the kick for the whole bucket, so it is the wrong way to cool this rocket.
        /// </summary>
        static Edit Soften(Catalog catalog, JsonObject design)
        {
            var best = JsonMut.CloneObject(design);
            bool any = false;
            for (int i = 0; i < 10; i++)
            {
                Picture look;
                try { look = Look(catalog, best); }
                catch (Exception) { break; }
                bool air = look.Air;
                bool gee = look.Has("payload-g") || look.Has("overg") || look.Has("risk-overg");
                if (!air && !gee) break;
                var trial = JsonMut.CloneObject(best);
                bool bumped = false;
                if (air) bumped |= BumpStage(trial, 0, 1.08);
                if (gee) bumped |= BumpStage(trial, -1, 1.12);
                if (!bumped) break;
                try { ReferenceAscent.Run(catalog, trial); }
                catch (Exception) { break; }
                if (Failed(catalog, trial)) break;
                Replace(best, trial);
                any = true;
            }
            if (!any) return Edit.None;
            Replace(design, best);
            return new Edit("Fuel", "light", "more");
        }

        static bool BumpStage(JsonObject design, int index, double scale)
        {
            var stages = design != null ? design.Arr("stages") : null;
            if (stages == null || stages.Count == 0) return false;
            if (index < 0) index = stages.Count - 1;
            if (index >= stages.Count) return false;
            var stage = stages[index] as JsonObject;
            if (stage == null) return false;
            double mass = stage.Num("propMass");
            if (!(mass > 0)) return false;
            var edit = HangarEditor.SetPropMass(design, PartRef.Stage(index), mass * scale);
            return edit.changed;
        }

        static bool BumpProp(JsonObject design, double scale)
        {
            var stages = design.Arr("stages");
            if (stages == null) return false;
            bool any = false;
            for (int i = 0; i < stages.Count; i++)
            {
                var stage = stages[i] as JsonObject;
                if (stage == null) continue;
                double mass = stage.Num("propMass");
                if (!(mass > 0)) continue;
                var edit = HangarEditor.SetPropMass(design, PartRef.Stage(i), mass * scale);
                if (edit.changed) any = true;
            }
            return any;
        }

        static Edit Fallback(Catalog catalog, JsonObject design, Picture now)
        {
            if (catalog == null) return Edit.None;
            EngineeringReport report;
            try { report = Validate.Engineering(catalog, design); }
            catch (Exception) { return Edit.None; }
            var advisories = Advisories.From(report, null, design);
            for (int i = 0; i < advisories.Count; i++)
            {
                var advisory = advisories[i];
                if (advisory == null || advisory.severity == AdvisorySeverity.Note) continue;
                if (advisory.code == "maxq-high" || advisory.code == "structural-maxq") continue;
                if (advisory.code != null && advisory.code.StartsWith("risk-", StringComparison.Ordinal)) continue;
                if (!now.Has(advisory.code)) continue;
                var edit = AdvisoryFix.Apply(catalog, design, advisory);
                if (edit.changed) return edit;
            }
            return Edit.None;
        }

        static string Summarize(List<Edit> notes)
        {
            var order = new List<string>();
            var from = new Dictionary<string, string>();
            var to = new Dictionary<string, string>();
            for (int i = 0; i < notes.Count; i++)
            {
                var note = notes[i];
                if (!note.changed || string.IsNullOrEmpty(note.what)) continue;
                if (!from.ContainsKey(note.what))
                {
                    order.Add(note.what);
                    from[note.what] = note.from;
                }
                if (!string.IsNullOrEmpty(note.to)) to[note.what] = note.to;
            }
            if (order.Count == 0) return "";
            var parts = new List<string>();
            for (int i = 0; i < order.Count; i++)
            {
                string label = order[i];
                string a = from[label];
                to.TryGetValue(label, out string b);
                if (!string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b)) parts.Add(label + " " + a + " → " + b);
                else parts.Add(label);
            }
            return string.Join(". ", parts) + ".";
        }

        static string Explain(Catalog catalog, JsonObject design, Picture now, string summary, string tradeoff)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(summary)) parts.Add(summary);
            if (now.Air && now.MaxQ.HasValue)
            {
                var lim = DesignerLimits.Active;
                double sf = design.Num("safetyFactor", 1.4);
                double throttle = MaxQThrottle.Percent(design);
                string line = "Air load is " + N1(now.MaxQ.Value) + " kPa and the walls hold " + N1(now.QLim) + " kPa. The warning line is "
                    + N0(lim.maxQWarnKPa) + " kPa.";
                if (sf >= lim.safetyMax - 1e-6 && throttle <= MaxQThrottle.FloorPct + 0.1)
                    line = "Strength is already " + N(sf) + ", the maximum. Max-Q throttle is already " + N0(throttle)
                        + " percent, the minimum. " + line + " Fly with this air push, or lighten the cargo.";
                parts.Add(line);
            }
            if (now.Has("payload-g") || now.Has("overg") || now.Has("risk-overg"))
            {
                double cap = PayloadCap(catalog, design);
                string peak = now.Peak > 0 ? " The climb peaks at " + N1(now.Peak) + " g." : "";
                parts.Add("The cargo allows " + N1(cap) + " g." + peak + " Holding the push lower makes the rocket tumble, so the gentler limit stays off.");
            }
            if (now.Has("dv-margin") || now.Has("weak-upper-dv") || now.Has("prop-depletion") || now.Has("risk-prop-depletion"))
            {
                string how = "";
                if (!double.IsNaN(now.Margin))
                    how = now.Margin < 0
                        ? " The trip is " + N0(Math.Abs(now.Margin) * 100) + " percent short on speed."
                        : " Only " + N0(now.Margin * 100) + " percent of speed is spare.";
                parts.Add("Still short on speed for this trip." + how + " Lighten the cargo, or pick a nearer stop.");
            }
            if (now.Has("weak-upper"))
                parts.Add("An upper stage is still short on push. Another engine does not fit without a wider rocket than the limits allow.");
            if (!string.IsNullOrEmpty(tradeoff)) parts.Add(tradeoff);
            if (parts.Count == 0)
                parts.Add("No combination clears every warning without giving up the trip.");
            return Clean(string.Join(" ", parts));
        }

        static void CarryAirLoad(Catalog catalog, JsonObject from, JsonObject to)
        {
            string source = ReferenceAscent.CanonicalKey(catalog, from);
            if (!ReferenceAscent.TryGet(source, out var flight) || !flight.hit || !(flight.qPa > 0)) return;
            ReferenceAscent.Store(ReferenceAscent.CanonicalKey(catalog, to), flight.qPa, flight.altM, flight.maxG);
        }

        static double PayloadCap(Catalog catalog, JsonObject design)
        {
            double cap = 8;
            string type = design.Obj("payload")?.Str("type");
            if (type != null && catalog != null && catalog.PayloadById.TryGetValue(type, out var payload) && payload.maxG > 0 && payload.maxG < cap)
                cap = payload.maxG;
            return cap;
        }

        static double PayloadVolume(Catalog catalog, JsonObject design)
        {
            string type = design.Obj("payload")?.Str("type");
            if (type != null && catalog != null && catalog.PayloadById.TryGetValue(type, out var payload)) return payload.volume;
            return 0;
        }

        static double ShownG(JsonObject design, double fallback)
        {
            var guidance = design.Obj("guidance");
            if (guidance != null && guidance.ContainsKey("gLimit")) return guidance.Num("gLimit");
            return fallback;
        }

        static Edit SetG(JsonObject design, double from, double to)
        {
            var guidance = design.Obj("guidance");
            if (guidance == null)
            {
                guidance = new JsonObject();
                JsonMut.Set(design, "guidance", guidance);
            }
            if (guidance.ContainsKey("gLimit") && Math.Abs(guidance.Num("gLimit") - to) < 1e-6) return Edit.None;
            JsonMut.Set(guidance, "gLimit", to);
            return new Edit("g limit", N1(from) + " g", N1(to) + " g");
        }

        static int WeakStage(Catalog catalog, JsonObject design)
        {
            try
            {
                var readout = StepperModel.Evaluate(catalog, design).Readout;
                if (readout == null) return 1;
                int best = -1;
                double twr = double.MaxValue;
                for (int i = 0; i < readout.stages.Count; i++)
                {
                    var stage = readout.stages[i];
                    if (stage.designIndex <= 0) continue;
                    if (stage.twrVac < twr) { twr = stage.twrVac; best = stage.designIndex; }
                }
                int count = design.Arr("stages")?.Count ?? 0;
                if (best > 0 && best < count) return best;
            }
            catch (Exception) { }
            int n = design.Arr("stages")?.Count ?? 1;
            return n > 1 ? n - 1 : 0;
        }

        static int Count(JsonObject design, int stage)
        {
            var stages = design.Arr("stages");
            if (stages == null || stage < 0 || stage >= stages.Count) return 1;
            return (int)((JsonObject)stages[stage]).Num("engineCount", 1);
        }

        static void Replace(JsonObject target, JsonObject source)
        {
            target.Clear();
            target.Order.Clear();
            foreach (var key in source.Order) target.Add(key, JsonMut.Clone(source[key]));
            HangarEditor.Normalize(target);
        }

        static double Ceil(double value, double step, double min, double max)
        {
            if (!(step > 0)) step = 0.05;
            double n = Math.Ceiling((value - 1e-9) / step) * step;
            if (n < min) n = min;
            if (n > max) n = max;
            return Math.Round(n, 6);
        }

        static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text)) return "No combination clears every warning.";
            if (PlayerText.Leak(text)) text = PlayerText.ForPlayer(text);
            return text;
        }

        static string N(double value) => value.ToString("0.00", Inv);
        static string N1(double value) => value.ToString("0.0", Inv);
        static string N0(double value) => Math.Round(value).ToString("0", Inv);
    }

    /// <summary>Player-facing check lines. Real numbers, no snake-case keys.</summary>
    public static class StepperPlain
    {
        public static string Kid(StepCheck check)
        {
            if (check == null) return "";
            string kid = Clean(check.Kid);
            if (!string.IsNullOrEmpty(kid)) return kid;
            return check.Gate == StepGate.Pass ? "Ready." : "Check this step.";
        }

        public static string Numbers(StepCheck check)
        {
            if (check == null || string.IsNullOrEmpty(check.Code)) return "";
            switch (check.Code)
            {
                case "maxq-high":
                case "structural-maxq":
                    if (check.Value > 0 && check.Limit > 0)
                        return "Air load " + check.Value.ToString("0.0", Inv) + " kPa is past the "
                            + check.Limit.ToString("0.0", Inv) + " kPa warning line.";
                    break;
                case "twr-low":
                case "twr-marginal":
                case "zero-engines":
                    if (check.Limit > 0)
                        return "Liftoff thrust ratio " + check.Value.ToString("0.00", Inv)
                            + ". Need " + check.Limit.ToString("0.00", Inv) + ".";
                    break;
                case "payload-g":
                case "overg":
                    if (check.Value > 0 || check.Limit > 0)
                        return "Peak " + check.Value.ToString("0.0", Inv) + " g. Cargo limit "
                            + check.Limit.ToString("0.0", Inv) + " g.";
                    break;
            }
            return Clean(check.Engineer);
        }

        public static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string s = PlayerText.ForPlayer(text);
            return PlayerText.Leak(s) ? "" : s;
        }

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    }
}
