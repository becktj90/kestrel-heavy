// One undoable change for a guided-step warning.
// The edit is kept only when a fresh evaluation no longer shows that flag.
// Otherwise the design stays as it was and Instruction names the control and the amount.
using System;
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

    public static class StepperFix
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static StepperFixResult Apply(Catalog catalog, JsonObject design, StepCheck check)
        {
            var result = new StepperFixResult();
            if (catalog == null || design == null || check == null || string.IsNullOrEmpty(check.Code))
            {
                result.Field = check != null ? check.Field : null;
                result.Instruction = "There is no single change for this warning.";
                return result;
            }
            result.Field = string.IsNullOrEmpty(check.Field) ? "stages" : check.Field;
            switch (check.Code)
            {
                case "maxq-high":
                case "structural-maxq":
                    return MaxQ(catalog, design, check);
                case "overg":
                case "payload-g":
                    return GLimit(catalog, design, check);
                case "twr-low":
                case "twr-marginal":
                case "zero-engines":
                    return Twr(catalog, design, check);
                case "dv-margin":
                case "prop-depletion":
                case "weak-upper-dv":
                    return DeltaV(catalog, design, check);
                case "tumble":
                case "unstable":
                case "payload-doesnt-fit":
                case "fairing-narrow":
                    return Aero(catalog, design, check);
                case "bad-mixture":
                    return Mixture(design, check);
                default:
                    return Unreachable(check);
            }
        }

        static StepperFixResult MaxQ(Catalog catalog, JsonObject design, StepCheck check)
        {
            double maxQ, qLim, sf;
            ReadMaxQ(catalog, design, check, out maxQ, out qLim, out sf);
            var lim = DesignerLimits.Active;
            double need = sf;
            if (maxQ > 0 && qLim > 0 && sf > 0)
                need = sf * maxQ / (StepperChecks.MaxQAmberFraction * qLim);
            double target = Ceil(need + lim.safetyStep * 0.01, lim.safetyStep, lim.safetyMin, lim.safetyMax);
            if (!(need > sf + 1e-6) || target <= sf + 1e-6)
                return Say("safetyFactor",
                    "Strength is already " + N(sf) + ". Air load is " + N1(maxQ) + " kPa and the walls hold " + N1(qLim) + " kPa. Lower the Max-Q throttle on Aero by 5 percent.");
            if (need > lim.safetyMax + 1e-4)
                return Say("safetyFactor",
                    "Strength would have to go from " + N(sf) + " to " + N(need) + ", past the " + N(lim.safetyMax) + " limit. Air load is " + N1(maxQ) + " kPa. Lower the Max-Q throttle on Aero by 5 percent.");

            var clone = JsonMut.CloneObject(design);
            var edit = HangarEditor.SetSafetyFactor(clone, target);
            CarryAirLoad(catalog, design, clone);
            if (!edit.changed || !Cleared(catalog, clone, check))
                return Say("safetyFactor",
                    "Raising Strength from " + N(sf) + " to " + N(target) + " leaves the air load at " + N1(maxQ) + " kPa. Lower the Max-Q throttle on Aero by 5 percent.");
            HangarEditor.SetSafetyFactor(design, target);
            CarryAirLoad(catalog, clone, design);
            return Done(edit, "safetyFactor");
        }

        static StepperFixResult GLimit(Catalog catalog, JsonObject design, StepCheck check)
        {
            double cap = PayloadCap(catalog, design);
            double current = ShownG(design, cap);
            double target = Math.Max(1.5, cap - 0.5);
            target = Math.Round(target * 2, MidpointRounding.AwayFromZero) / 2.0;
            if (target > cap) target = cap;
            double peak = CachedPeak(catalog, design);
            if (current <= target + 1e-6)
                return Say("gLimit",
                    "The g limit is already " + N1(current) + " g. The cargo allows " + N1(cap) + " g"
                    + (peak > 0 ? " and the last climb peaked at " + N1(peak) + " g" : "")
                    + ". Take 1 engine off the top stage.");

            var clone = JsonMut.CloneObject(design);
            var edit = SetG(clone, current, target);
            bool flown = false;
            try
            {
                ReferenceAscent.Run(catalog, clone);
                flown = true;
            }
            catch (Exception)
            {
                flown = false;
            }
            if (flown && edit.changed && Cleared(catalog, clone, check))
            {
                SetG(design, current, target);
                return Done(edit, "gLimit");
            }
            return Say("gLimit",
                "Set the g limit from " + N1(current) + " g to " + N1(target) + " g. The cargo allows " + N1(cap) + " g"
                + (peak > 0 ? " and the last climb peaked at " + N1(peak) + " g" : "") + ".");
        }

        static StepperFixResult Twr(Catalog catalog, JsonObject design, StepCheck check)
        {
            var stage = Stage0(design);
            int count = stage == null ? 1 : (int)stage.Num("engineCount", 1);
            int max = DesignerLimits.Active.engineCountMax;
            double twr = 0;
            try
            {
                var readout = StepperModel.Evaluate(catalog, design).Readout;
                if (readout != null) twr = readout.liftoffTWR;
            }
            catch (Exception) { twr = 0; }

            int guess = count + 1;
            if (twr > 0.05)
                guess = (int)Math.Ceiling(count * StepperChecks.PassTwr / twr - 1e-9);
            if (guess < count + 1) guess = count + 1;
            if (guess > max) guess = max;

            int found = -1;
            Edit foundEdit = Edit.None;
            for (int n = guess; n <= max; n++)
            {
                var clone = JsonMut.CloneObject(design);
                var edit = HangarEditor.SetEngineCount(clone, PartRef.Stage(0), n);
                if (!edit.changed) continue;
                if (!Cleared(catalog, clone, check)) continue;
                var gate = StepperModel.Evaluate(catalog, clone)[DesignerStep.Engines];
                if (gate.Gate != StepGate.Pass) continue;
                found = n;
                foundEdit = edit;
                break;
            }
            if (found < 0)
                return Say("engineCount",
                    "Liftoff push is " + N(twr) + " with " + count + " engines. It needs " + N(StepperChecks.PassTwr) + ". The first stage holds at most " + max + " engines. Take fuel off until the push clears " + N(StepperChecks.PassTwr) + ".");
            HangarEditor.SetEngineCount(design, PartRef.Stage(0), found);
            return Done(foundEdit, "engineCount");
        }

        static StepperFixResult DeltaV(Catalog catalog, JsonObject design, StepCheck check)
        {
            DvFixResult solved;
            try { solved = DvMarginFix.Solve(catalog, design); }
            catch (Exception)
            {
                return Say("propMass", "The speed for this trip could not be worked out. Add fuel, or lighten the cargo by 500 kg.");
            }
            if (solved != null && solved.Solved && solved.Changed && solved.Design != null && Cleared(catalog, solved.Design, check))
            {
                Replace(design, solved.Design);
                return Done(new Edit("Fixed the speed shortfall"), "propMass");
            }
            string why = solved != null ? solved.Reason : null;
            if (string.IsNullOrEmpty(why))
                why = "Still short on speed. Add fuel, or lighten the cargo by 500 kg.";
            if (PlayerText.Leak(why)) why = PlayerText.ForPlayer(why);
            if (!HasDigit(why))
                why = why + " You are " + N0(Shortfall(check)) + " m/s short.";
            return Say("propMass", why);
        }

        static StepperFixResult Aero(Catalog catalog, JsonObject design, StepCheck check)
        {
            if (check.Code == "tumble" || check.Code == "unstable")
                return Fins(catalog, design, check);
            return Fairing(catalog, design, check);
        }

        static StepperFixResult Fins(Catalog catalog, JsonObject design, StepCheck check)
        {
            var aero = design.Obj("aero");
            bool on = aero != null && aero.Bool("fins");
            double span = aero != null ? aero.Num("finSpanM", 1) : 1;
            var lim = DesignerLimits.Active;
            var clone = JsonMut.CloneObject(design);
            Edit edit;
            if (!on) edit = HangarEditor.SetFins(clone, true);
            else edit = HangarEditor.SetFinSpan(clone, Math.Min(lim.finSpanMax, span + Math.Max(0.5, lim.finSpanStep * 4)));
            if (edit.changed && Cleared(catalog, clone, check))
            {
                if (!on) HangarEditor.SetFins(design, true);
                else HangarEditor.SetFinSpan(design, Math.Min(lim.finSpanMax, span + Math.Max(0.5, lim.finSpanStep * 4)));
                return Done(edit, "fins");
            }
            if (!on)
                return Say("fins", "Turn fins on. Balance is " + N(check.Value) + " calibers, and fins are off.");
            return Say("fins",
                "Make the fins longer, from " + N(span) + " m toward " + N(lim.finSpanMax) + " m. Balance is " + N(check.Value) + " calibers.");
        }

        static StepperFixResult Fairing(Catalog catalog, JsonObject design, StepCheck check)
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

            var clone = JsonMut.CloneObject(design);
            var edit = ApplyFairing(clone, targetL, targetD);
            if (!edit.changed || !Cleared(catalog, clone, check))
            {
                targetL = lim.fairingLengthMax;
                targetD = Math.Min(lim.diameterMax, Math.Max(targetD, diameter + 1));
                clone = JsonMut.CloneObject(design);
                edit = ApplyFairing(clone, targetL, targetD);
            }
            if (edit.changed && Cleared(catalog, clone, check))
            {
                ApplyFairing(design, targetL, targetD);
                return Done(edit, "fairing");
            }
            return Say("fairing",
                "Lengthen the fairing from " + N(length) + " m to " + N(targetL) + " m and widen it from " + N(diameter) + " m to " + N(targetD) + " m so the cargo fits.");
        }

        static Edit ApplyFairing(JsonObject design, double length, double diameter)
        {
            var lengthEdit = HangarEditor.SetFairingLength(design, length);
            var widthEdit = HangarEditor.SetDiameter(design, PartRef.Fairing, diameter);
            if (!lengthEdit.changed && !widthEdit.changed) return Edit.None;
            string from = N(length) + " m";
            string to = N(design.Obj("fairing")?.Num("length") ?? length) + " m";
            return new Edit("Fairing size", from, to);
        }

        static StepperFixResult Mixture(JsonObject design, StepCheck check)
        {
            var stage = Stage0(design);
            double now = stage != null ? stage.Num("mixtureOffsetPct") : 0;
            if (Math.Abs(now) <= 1e-6)
                return Say("mixtureOffsetPct", "Mixture offset is already 0.0 percent. Keep it inside 6 percent.");
            var clone = JsonMut.CloneObject(design);
            JsonMut.Set(Stage0(clone), "mixtureOffsetPct", 0d);
            if (!Cleared(null, clone, check) && !MixtureCleared(clone))
                return Say("mixtureOffsetPct", "Set mixture offset from " + N(now) + " percent to 0.0 percent.");
            JsonMut.Set(stage, "mixtureOffsetPct", 0d);
            return Done(new Edit("Mixture offset", N(now) + "%", "0.0%"), "mixtureOffsetPct");
        }

        static bool MixtureCleared(JsonObject design)
        {
            var stage = Stage0(design);
            double now = stage != null ? stage.Num("mixtureOffsetPct") : 0;
            return Math.Abs(now) <= MixtureOffset.HazardPct;
        }

        static StepperFixResult Unreachable(StepCheck check)
        {
            string field = string.IsNullOrEmpty(check.Field) ? "stages" : check.Field;
            string name = ControlName(field);
            string amount = check.Value != 0 || check.Limit != 0
                ? " It reads " + N(check.Value) + " against " + N(check.Limit) + "."
                : " Change it by 1 step.";
            string kid = string.IsNullOrEmpty(check.Kid) ? "This warning has no single change." : check.Kid;
            return Say(field, kid + " Use " + name + "." + amount);
        }

        static bool Cleared(Catalog catalog, JsonObject design, StepCheck before)
        {
            if (before == null || design == null) return false;
            if (before.Code == "bad-mixture") return MixtureCleared(design);
            if (catalog == null) return false;
            StepperReport report;
            try { report = StepperModel.Evaluate(catalog, design); }
            catch (Exception) { return false; }
            var now = report[before.Step];
            if (now == null) return false;
            if (before.Code == "maxq-high" || before.Code == "structural-maxq")
            {
                var readout = report.Readout;
                if (readout == null || readout.maxQStale || !readout.maxQ_kPa.HasValue || !readout.qLim_kPa.HasValue) return false;
                if (readout.maxQ_kPa.Value > StepperChecks.MaxQAmberFraction * readout.qLim_kPa.Value) return false;
                return now.Code != before.Code || now.Gate == StepGate.Pass;
            }
            if (before.Code == "twr-low" || before.Code == "twr-marginal" || before.Code == "zero-engines")
                return now.Gate == StepGate.Pass && now.Code != "twr-low" && now.Code != "twr-marginal" && now.Code != "zero-engines";
            if (before.Code == "dv-margin" || before.Code == "prop-depletion" || before.Code == "weak-upper-dv")
                return now.Code != before.Code && (report.Readout == null || report.Readout.margin >= 0);
            if (before.Code == "payload-doesnt-fit" || before.Code == "fairing-narrow")
                return now.Code != before.Code && report.Readout != null && report.Readout.fairingFits == true;
            if (before.Code == "payload-g" || before.Code == "overg")
                return now.Code != before.Code && now.Gate != StepGate.Red && now.Code != "payload-g" && now.Code != "overg";
            return now.Code != before.Code && Rank(now.Gate) <= Rank(before.Gate);
        }

        static int Rank(StepGate gate)
        {
            if (gate == StepGate.Pass || gate == StepGate.Skip) return 0;
            if (gate == StepGate.Amber) return 1;
            return 2;
        }

        static void ReadMaxQ(Catalog catalog, JsonObject design, StepCheck check, out double maxQ, out double qLim, out double sf)
        {
            sf = design.Num("safetyFactor", 1.4);
            maxQ = check.Value;
            qLim = 0;
            if (check.Code == "maxq-high" && check.Limit > 0)
                qLim = check.Limit / StepperChecks.MaxQAmberFraction;
            else if (check.Limit > 0) qLim = check.Limit;
            try
            {
                var readout = StepperModel.Evaluate(catalog, design).Readout;
                if (readout != null && readout.maxQ_kPa.HasValue && !readout.maxQStale) maxQ = readout.maxQ_kPa.Value;
                if (readout != null && readout.qLim_kPa.HasValue) qLim = readout.qLim_kPa.Value;
            }
            catch (Exception) { }
        }

        static void CarryAirLoad(Catalog catalog, JsonObject from, JsonObject to)
        {
            string source = ReferenceAscent.CanonicalKey(catalog, from);
            if (!ReferenceAscent.TryGet(source, out var flight) || !flight.hit || !(flight.qPa > 0)) return;
            ReferenceAscent.Store(ReferenceAscent.CanonicalKey(catalog, to), flight.qPa, flight.altM, flight.maxG);
        }

        static double CachedPeak(Catalog catalog, JsonObject design)
        {
            if (!ReferenceAscent.TryGet(ReferenceAscent.CanonicalKey(catalog, design), out var flight) || !flight.hit) return 0;
            return flight.maxG;
        }

        static double PayloadCap(Catalog catalog, JsonObject design)
        {
            double cap = 8;
            string type = design.Obj("payload")?.Str("type");
            if (type != null && catalog.PayloadById.TryGetValue(type, out var payload) && payload.maxG > 0 && payload.maxG < cap)
                cap = payload.maxG;
            return cap;
        }

        static double PayloadVolume(Catalog catalog, JsonObject design)
        {
            string type = design.Obj("payload")?.Str("type");
            if (type != null && catalog.PayloadById.TryGetValue(type, out var payload)) return payload.volume;
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

        static double Shortfall(StepCheck check)
        {
            if (check.Value < 0 && check.Limit == 0) return Math.Abs(check.Value);
            return Math.Abs(check.Value);
        }

        static JsonObject Stage0(JsonObject design) => design?.Arr("stages")?[0] as JsonObject;

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

        static StepperFixResult Done(Edit edit, string field)
            => new StepperFixResult { Edit = edit, Field = field, Cleared = true };

        static StepperFixResult Say(string field, string instruction)
            => new StepperFixResult
            {
                Field = field,
                Instruction = Clean(instruction),
                Cleared = false,
            };

        static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text)) return "No single change clears this warning.";
            if (PlayerText.Leak(text)) text = PlayerText.ForPlayer(text);
            return text;
        }

        static bool HasDigit(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            for (int i = 0; i < text.Length; i++)
                if (text[i] >= '0' && text[i] <= '9') return true;
            return false;
        }

        static string ControlName(string field)
        {
            switch (field)
            {
                case "safetyFactor": return "Strength";
                case "gLimit": return "the g limit";
                case "engineCount": return "the engine count";
                case "propMass": return "the fuel load";
                case "fins": return "the fins";
                case "fairing": return "the fairing";
                case "mixtureOffsetPct": return "the mixture offset";
                case "maxQThrottle": return "the Max-Q throttle";
                default: return "this control";
            }
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
