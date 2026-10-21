// Which Thrum beat each screen asks for. Numbers stay in {tokens}; the box fills them.
// Kid text is the whole line. The engineer card is added only while depth is Engineer.
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using Kestrel.Core.Physics;

namespace Kestrel.Game.Guide
{
    /// <summary>Numbers the post-flight card already settled. Thrum quotes these and does not score the flight again.</summary>
    public sealed class GuideReport
    {
        public int Stamp;
        public bool Passed;
        public string MissionId = "";
        public int MissionIndex;
        public string Kind = "";
        public string Landing = "";
        public int Stars;
        public string Medal = "";
        public string Timing = "";
        public bool PerfectTiming;
        public string Bonus = "";
        public string Part = "";
        public string Paint = "";
        public double SparePct = double.NaN;
        public double UnderBudgetKc = double.NaN;
        public double QKpa = double.NaN;
        public double QLimKpa = double.NaN;
        public double DvShort = double.NaN;
        public double ApoKm = double.NaN;
        public double GoalKm = double.NaN;
        public double PeriKm = double.NaN;
    }

    public interface IGuideReport
    {
        void ShowReport(GuideReport facts);
    }

    public static class GuideSession
    {
        public static LiveGuideEngine Engine { get; private set; }
        public static IGuideReport Report => Engine;

        public static void Bind(LiveGuideEngine engine) => Engine = engine;
    }

    public sealed class GuideCallNote
    {
        public string Call = "";
        public bool Open;
        public string Grade = "";
        public double QKpa = double.NaN;
        public double QLimKpa = double.NaN;
        public double Flux = double.NaN;
        public int EnginesLeft;
    }

    public static class GuideHooks
    {
        public static void Home(LiveGuideEngine engine)
        {
            if (engine == null || engine.State.FirstRunDone) return;
            engine.Welcome();
        }

        public static void MissionSelect(LiveGuideEngine engine, string missionId, bool contracts)
        {
            if (engine == null) return;
            if (!string.IsNullOrEmpty(missionId))
                engine.Offer("guide.brief", missionId, null, "map", false);
            if (contracts)
                engine.Offer("guide.brief", "contract", null, "map", false);
        }

        public static void Briefing(LiveGuideEngine engine, MissionContext ctx, MissionBriefing card)
        {
            if (engine == null) return;
            bool contract = ctx != null && ctx.Mode == MissionMode.Contract;
            string id = ctx != null ? ctx.MissionId : null;
            if (contract) engine.Offer("guide.brief", "contract", null, "brief", false);
            else if (!string.IsNullOrEmpty(id)) engine.Offer("guide.brief", id, null, "brief", false);
            bool bonus = (ctx != null && ctx.BonusGoals.Count > 0) || (card != null && card.Bonuses.Count > 0);
            if (bonus) engine.Offer("guide.brief", "bonus", null, "brief", false);
            if (card == null || card.Wind == null || card.Wind.Calm || !(card.WindKt > 0.5)) return;
            string dir = string.IsNullOrEmpty(card.Wind.Compass) ? Compass(card.Wind.DirectionFromDeg) : card.Wind.Compass;
            if (string.IsNullOrEmpty(dir)) return;
            var tokens = new Dictionary<string, string>
            {
                ["kt"] = Num(card.WindKt, "0"),
                ["dir"] = dir,
            };
            engine.Offer("guide.brief", "wind", tokens, "brief", false);
        }

        public static void Designer(LiveGuideEngine engine, int step, StepperReport report, bool engineer)
        {
            if (engine == null || step < 1 || step > 7) return;
            engine.SetEngineer(engineer);
            string stepId = step.ToString(CultureInfo.InvariantCulture);
            engine.Settings.StepId = stepId;
            engine.Offer("guide.step", stepId, null, stepId, false);
            var check = CheckAt(report, step);
            string variant = Variant(step, check);
            if (engine.SameStep(stepId + "|" + variant + "|" + Fingerprint(report))) return;
            OfferVariant(engine, step, variant, check, report, stepId);
            if (step == 7) Prelaunch(engine, report, stepId);
        }

        public static void Ascent(LiveGuideEngine engine, IList<GuideCallNote> notes, bool auto, bool engineer)
        {
            if (engine == null) return;
            engine.SetEngineer(engineer);
            if (auto) engine.Offer("guide.asc", "auto", null, "", true);
            if (notes == null) return;
            for (int i = 0; i < notes.Count; i++) OfferCall(engine, notes[i]);
        }

        public static void FlightBus(LiveGuideEngine engine, string id, Dictionary<string, string> tokens)
        {
            if (engine == null || string.IsNullOrEmpty(id)) return;
            if (id == "flight.begin")
            {
                engine.BeginFlight();
                return;
            }
            if (id == "liftoff") engine.Offer("liftoff", "", tokens, "", true);
            else if (id == "meco") engine.Offer("meco", "", tokens, "", true);
            else if (id == "seco" || id == "cutoff") engine.Offer("seco", "", tokens, "", true);
            else if (id == "fairing") engine.Offer("fairing", "", tokens, "", true);
            else if (id == "fuel-low") engine.Offer("fuel-low", "", tokens, "", true);
            else if (id == "deploy") engine.Offer("deploy", "", tokens, "", true);
            else if (id == "landing") engine.Offer("landing", "", tokens, "", true);
            else if (id == "orbit") engine.Offer("guide.asc", "orbit", tokens, "", true);
            else if (id == "splash") engine.Offer("splash", "", tokens, "", true);
            else if (id == "tip") engine.Offer("tip", "", tokens, "", true);
            else if (id == "touchdown") engine.Offer("touchdown", tokens != null && tokens.ContainsKey("grade") ? tokens["grade"] : "perfect", tokens, "", true);
            else if (id.StartsWith("abort-", StringComparison.Ordinal)) engine.Offer(id, "", tokens, "", true);
            else if (id == "failure" && tokens != null && tokens.TryGetValue("kind", out var kind))
                engine.Offer("abort", kind, tokens, "", true);
        }

        public static void ShowReport(LiveGuideEngine engine, GuideReport facts, ref int stamp)
        {
            if (engine == null || facts == null) return;
            if (facts.Stamp != 0 && facts.Stamp == stamp) return;
            stamp = facts.Stamp;
            engine.SetEngineer(engine.Settings.Engineer);
            if (facts.Passed) OfferWin(engine, facts);
            else OfferLoss(engine, facts);
            if (facts.Stars >= 1 && facts.Stars <= 3)
                engine.Offer("guide.medal", "star" + facts.Stars.ToString(CultureInfo.InvariantCulture), null, "report", true);
            if (!string.IsNullOrEmpty(facts.Medal))
                engine.Offer("guide.medal", facts.Medal, null, "report", true);
            if (!string.IsNullOrEmpty(facts.Timing) && facts.Timing != facts.Medal)
                engine.Offer("guide.medal", facts.Timing, null, "report", true);
            if (facts.PerfectTiming)
                engine.Offer("guide.medal", "perfect", null, "report", true);
            if (!string.IsNullOrEmpty(facts.Bonus))
                engine.Offer("guide.medal", "bonus", Token("goal", facts.Bonus), "report", true);
            if (!string.IsNullOrEmpty(facts.Paint))
                engine.Offer("guide.medal", "cosmetic", Token("name", facts.Paint), "report", true);
        }

        /// <summary>Copies the debrief. Spare fuel and the Kc gap are the same formulas the card already used.</summary>
        public static GuideReport FromFlight(FlightDebrief debrief, SimResult result, MissionContext mission, AscentFlightScore score)
        {
            if (debrief == null) return null;
            string missionId = !string.IsNullOrEmpty(debrief.MissionId) ? debrief.MissionId : (mission != null ? mission.MissionId ?? "" : "");
            string landingMedal = "";
            string timing = "";
            if (debrief.Medals != null)
            {
                for (int i = 0; i < debrief.Medals.Count; i++)
                {
                    string medal = debrief.Medals[i] ?? "";
                    if (medal.StartsWith("timing-", StringComparison.Ordinal)) timing = medal.Substring("timing-".Length);
                    else if (medal == "gold" || medal == "silver" || medal == "bronze") landingMedal = medal;
                }
            }
            var ev = result != null ? result.failure : null;
            string kind = debrief.Kind ?? "";
            if (kind == "pass") kind = "";
            double goal = mission != null && mission.Target != null ? mission.Target.ApoKm : double.NaN;
            double apo = result != null ? MissionScore.FlownApoAltM(result) / 1000.0 : double.NaN;
            double peri = result != null ? result.orbit.periAlt / 1000.0 : double.NaN;
            double q = double.NaN;
            double qlim = double.NaN;
            if (kind == "structural-maxq")
            {
                if (ev != null && ev.cause == "q")
                {
                    q = ev.value / 1000.0;
                    qlim = ev.limit / 1000.0;
                }
                else if (result != null && result.maxQ > 0 && result.qLimPa > 0)
                {
                    q = result.maxQ / 1000.0;
                    qlim = result.qLimPa / 1000.0;
                }
            }
            int stamp = unchecked((int)debrief.Seed) ^ (missionId ?? "").GetHashCode() ^ (debrief.Stars * 17);
            if (stamp == 0) stamp = 1;
            return new GuideReport
            {
                Stamp = stamp,
                Passed = debrief.Passed,
                MissionId = missionId ?? "",
                MissionIndex = MissionIndex(missionId),
                Kind = kind,
                Landing = LandingOf(result),
                Stars = debrief.Stars,
                Medal = landingMedal,
                Timing = timing,
                PerfectTiming = timing == "gold" || (score != null && score.Decided > 0 && score.Late == 0 && score.Perfect == score.Decided),
                Part = Join(debrief.Unlocks),
                Paint = PaintOf(debrief.Cosmetics),
                SparePct = SparePct(debrief, result, mission),
                UnderBudgetKc = UnderBudget(debrief, mission),
                QKpa = q,
                QLimKpa = qlim,
                ApoKm = apo,
                PeriKm = peri,
                GoalKm = goal,
                DvShort = Shortfall(kind, result, mission),
            };
        }

        static void OfferWin(LiveGuideEngine engine, GuideReport facts)
        {
            engine.Offer("guide.win", "1", null, "report", true);
            string spare = Num(facts.SparePct, "0");
            if (spare != null && facts.SparePct > 0)
                engine.Offer("guide.win", "margin", Token("pct", spare), "report", true);
            string kc = Num(facts.UnderBudgetKc, "0.##");
            if (kc != null && facts.UnderBudgetKc > 0)
                engine.Offer("guide.win", "budget", Token("kc", kc), "report", true);
            if (!string.IsNullOrEmpty(facts.Part))
                engine.Offer("guide.win", "unlock", Token("part", facts.Part), "report", true);
            if (facts.MissionIndex >= 10)
                engine.Offer("guide.win", "final", null, "report", true);
            else if (facts.MissionIndex >= 1)
                engine.Offer("guide.win", "next", Token("n", (facts.MissionIndex + 1).ToString(CultureInfo.InvariantCulture)), "report", true);
        }

        static void OfferLoss(LiveGuideEngine engine, GuideReport facts)
        {
            string kind = facts.Kind ?? "";
            if (kind == "miss")
            {
                string apo = Num(facts.ApoKm, "0");
                string goal = Num(facts.GoalKm, "0");
                if (apo != null && goal != null)
                    engine.Offer("flight.end", "miss", new Dictionary<string, string> { ["apo"] = apo, ["goal"] = goal }, "report", true);
            }
            else if (kind == "structural-maxq")
            {
                string q = Num(facts.QKpa, "0");
                string lim = Num(facts.QLimKpa, "0");
                if (q != null && lim != null)
                    engine.Offer("abort", kind, new Dictionary<string, string> { ["q"] = q, ["qlim"] = lim }, "report", true);
            }
            else if (kind == "prop-depletion")
            {
                string dv = Num(facts.DvShort, "0");
                if (dv != null) engine.Offer("abort", kind, Token("dv", dv), "report", true);
            }
            else if (!string.IsNullOrEmpty(kind))
                engine.Offer("abort", kind, null, "report", true);
            if (facts.Landing == "splash") engine.Offer("splash", "", null, "report", true);
            else if (facts.Landing == "tip") engine.Offer("tip", "", null, "report", true);
            int streak = facts.MissionId != null ? engine.BumpStreak(facts.MissionId, false) : 0;
            if (streak >= 3) engine.Offer("guide.fail", "repeat", null, "report", true);
            engine.Offer("guide.fail", "retry", null, "report", true);
        }

        static void OfferVariant(LiveGuideEngine engine, int step, string variant, StepCheck check, StepperReport report, string stepId)
        {
            if (string.IsNullOrEmpty(variant)) return;
            var tokens = VariantTokens(variant, check, report);
            if (tokens == null && NeedsTokens(variant)) return;
            engine.Offer("guide.step", variant, tokens, stepId, false);
        }

        static bool NeedsTokens(string variant)
            => variant == "2.pass" || variant == "2.fail.mix" || variant == "3.fail.dv" || variant == "5.fail.reserve";

        static Dictionary<string, string> VariantTokens(string variant, StepCheck check, StepperReport report)
        {
            var read = report != null ? report.Readout : null;
            if (variant == "2.pass")
            {
                double twr = read != null ? read.liftoffTWR : (check != null ? check.Value : double.NaN);
                string text = Num(twr, "0.00");
                return text == null ? null : Token("twr", text);
            }
            if (variant == "2.fail.mix")
            {
                double pct = check != null ? Math.Abs(check.Value) : double.NaN;
                string text = Num(pct, "0.#");
                return text == null ? null : Token("pct", text);
            }
            if (variant == "3.fail.dv")
            {
                double dv = double.NaN;
                if (read != null && read.margin < 0 && read.required > 0) dv = -read.margin * read.required;
                else if (check != null && check.Value < 0 && check.Limit > 0) dv = -check.Value * check.Limit;
                string text = Num(dv, "0");
                return text == null ? null : Token("dv", text);
            }
            if (variant == "5.fail.reserve")
            {
                double pct = double.NaN;
                if (check != null && check.Limit > 0) pct = Math.Max(1, (check.Limit - check.Value) / check.Limit * 100.0);
                string text = Num(pct, "0");
                return text == null ? null : Token("pct", text);
            }
            return null;
        }

        static void Prelaunch(LiveGuideEngine engine, StepperReport report, string stepId)
        {
            bool red = report != null && report.HasRedRisk;
            var read = report != null ? report.Readout : null;
            if (red) engine.Offer("guide.pre", "red", null, stepId, false);
            else engine.Offer("guide.pre", "green", null, stepId, false);
            if (report == null) return;
            var seen = new HashSet<string>();
            if (report.RedRisks != null)
            {
                for (int i = 0; i < report.RedRisks.Count; i++)
                    OfferRisk(engine, report.RedRisks[i], read, stepId, seen);
            }
            if (read != null && read.predictedFailure != null)
                OfferKind(engine, read.predictedFailure.kind, read, stepId, seen, double.NaN);
            var check = CheckAt(report, 7);
            if (check != null) OfferKind(engine, check.Code, read, stepId, seen, double.NaN);
        }

        static void OfferRisk(LiveGuideEngine engine, FlightRisk risk, QuickReadout read, string stepId, HashSet<string> seen)
        {
            if (risk == null) return;
            OfferKind(engine, risk.kind, read, stepId, seen, risk.odds);
        }

        static void OfferKind(LiveGuideEngine engine, string kind, QuickReadout read, string stepId, HashSet<string> seen, double odds)
        {
            if (string.IsNullOrEmpty(kind) || !seen.Add(kind)) return;
            string when = PreWhen(kind);
            if (when == null) return;
            Dictionary<string, string> tokens = null;
            if (when == "maxq")
            {
                string q = read != null ? Num(read.maxQ_kPa ?? double.NaN, "0") : null;
                string lim = read != null ? Num(read.qLim_kPa ?? double.NaN, "0") : null;
                if (q == null || lim == null) return;
                tokens = new Dictionary<string, string> { ["q"] = q, ["qlim"] = lim };
            }
            else if (when == "odds")
            {
                if (double.IsNaN(odds) || odds < 0) return;
                string pct = Num(odds <= 1 ? odds * 100.0 : odds, "0");
                if (pct == null) return;
                tokens = Token("pct", pct);
            }
            engine.Offer("guide.pre", when, tokens, stepId, false);
        }

        static string PreWhen(string kind)
        {
            switch (kind)
            {
                case "structural-maxq":
                case "maxq-high": return "maxq";
                case "twr-low":
                case "twr-marginal": return "twr";
                case "unstable":
                case "tumble":
                case "overstable": return "stability";
                case "overg":
                case "payload-g": return "overg";
                case "cryo-material":
                case "cryo-composite": return "cryo";
                case "separation-collision": return "sep";
                case "leg-collapse":
                case "landing-reserve": return "leg";
                case "engine-out": return "odds";
                default: return null;
            }
        }

        static void OfferCall(LiveGuideEngine engine, GuideCallNote note)
        {
            if (note == null || string.IsNullOrEmpty(note.Call)) return;
            string grade = note.Grade ?? "";
            switch (note.Call)
            {
                case "MaxQ":
                    if (note.Open)
                        engine.Offer("maxq", "warn", QTokens(note), "", true);
                    if (grade == "Perfect") engine.Offer("maxq", "perfect", null, "", true);
                    else if (grade == "Late") engine.Offer("maxq", "late", null, "", true);
                    break;
                case "Pitch":
                    if (note.Open) engine.Offer("guide.asc", "pitch", null, "", true);
                    break;
                case "Staging":
                    if (note.Open) engine.Offer("sep", "", null, "", true);
                    if (grade == "Perfect" || grade == "Good") engine.Offer("sep-clear", "", null, "", true);
                    else if (grade == "Late") engine.Offer("sep-wide", "", null, "", true);
                    break;
                case "Fairing":
                    if (note.Open)
                    {
                        string flux = Num(note.Flux, "0");
                        engine.Offer("fairing", "", flux == null ? null : Token("flux", flux), "", true);
                    }
                    break;
                case "EngineOut":
                    if (note.Open) engine.Offer("guide.asc", "engineout", null, "", true);
                    if (grade == "Late") engine.Offer("guide.asc", "engineout.bad", null, "", true);
                    else if (grade == "Perfect" || grade == "Good")
                        engine.Offer("guide.asc", "engineout.ok", Token("left", Math.Max(0, note.EnginesLeft).ToString(CultureInfo.InvariantCulture)), "", true);
                    break;
                case "WindShear":
                    if (note.Open) engine.Offer("guide.asc", "shear", null, "", true);
                    break;
                case "FuelBalance":
                    if (note.Open) engine.Offer("fuel-low", "", null, "", true);
                    break;
            }
        }

        static Dictionary<string, string> QTokens(GuideCallNote note)
        {
            string q = Num(note.QKpa, "0");
            string lim = Num(note.QLimKpa, "0");
            if (q == null && lim == null) return null;
            var tokens = new Dictionary<string, string>();
            if (q != null) tokens["q"] = q;
            if (lim != null) tokens["qlim"] = lim;
            return tokens;
        }

        static string Fingerprint(StepperReport report)
        {
            if (report == null) return "";
            var read = report.Readout;
            string q = read != null ? Num(read.maxQ_kPa ?? double.NaN, "0") : null;
            string lim = read != null ? Num(read.qLim_kPa ?? double.NaN, "0") : null;
            string twr = read != null ? Num(read.liftoffTWR, "0.00") : null;
            string text = (report.HasRedRisk ? "R" : "G") + ":" + (q ?? "-") + ":" + (lim ?? "-") + ":" + (twr ?? "-");
            if (report.RedRisks == null) return text;
            for (int i = 0; i < report.RedRisks.Count; i++)
            {
                var risk = report.RedRisks[i];
                if (risk == null || string.IsNullOrEmpty(risk.kind)) continue;
                text += "|" + risk.kind + ":" + (Num(risk.odds, "0.##") ?? "-");
            }
            return text;
        }

        static string Variant(int step, StepCheck check)
        {
            if (check == null && step != 2) return "";
            switch (step)
            {
                case 2:
                    if (Has(check, "twr-low")) return "2.fail.twr";
                    if (Has(check, "bad-mixture")) return "2.fail.mix";
                    if (check != null && check.Gate == StepGate.Pass) return "2.pass";
                    return "";
                case 3:
                    if (Has(check, "dv-margin")) return "3.fail.dv";
                    if (Has(check, "maxq-high")) return "3.warn.maxq";
                    return "";
                case 4:
                    if (Has(check, "payload-doesnt-fit") || Has(check, "fairing-narrow")) return "4.fail.fit";
                    if (Has(check, "tumble") || Has(check, "unstable")) return "4.fail.tumble";
                    return "";
                case 5:
                    if (Has(check, "recovery-skip")) return "5.skip";
                    if (Has(check, "landing-reserve")) return "5.fail.reserve";
                    return "";
                default:
                    return "";
            }
        }

        static bool Has(StepCheck check, string code)
        {
            if (check == null || string.IsNullOrEmpty(code)) return false;
            if (check.Code == code) return true;
            if (check.Findings == null) return false;
            for (int i = 0; i < check.Findings.Count; i++)
                if (check.Findings[i] != null && check.Findings[i].Code == code) return true;
            return false;
        }

        static StepCheck CheckAt(StepperReport report, int step)
        {
            if (report == null || report.Checks == null || step < 0 || step >= report.Checks.Length) return null;
            return report.Checks[step];
        }

        static double SparePct(FlightDebrief debrief, SimResult result, MissionContext mission)
        {
            if (debrief == null || !debrief.Passed || result == null || !(result.idealDeltaV > 1)) return double.NaN;
            double need = Need(mission);
            if (!(need > 1)) return double.NaN;
            double margin = (result.idealDeltaV - need) / need;
            return margin > 0 ? margin * 100.0 : double.NaN;
        }

        static double UnderBudget(FlightDebrief debrief, MissionContext mission)
        {
            if (debrief == null || !debrief.Passed || !debrief.EconomySettled || debrief.FreeBuild) return double.NaN;
            if (mission == null || !(mission.BudgetKc > debrief.CostKc) || debrief.CostKc < 0) return double.NaN;
            return mission.BudgetKc - debrief.CostKc;
        }

        static double Shortfall(string kind, SimResult result, MissionContext mission)
        {
            if (kind != "prop-depletion" || result == null || !(result.idealDeltaV > 1)) return double.NaN;
            double need = Need(mission);
            if (!(need > result.idealDeltaV)) return double.NaN;
            return need - result.idealDeltaV;
        }

        static double Need(MissionContext mission)
        {
            if (mission == null || mission.Target == null) return double.NaN;
            return MissionScore.RequiredDeltaV(mission.Target.Kind, mission.Target.ApoKm, mission.Target.PeriKm, mission.Target.C3Km2PerS2);
        }

        static string LandingOf(SimResult result)
        {
            if (result == null || result.events == null) return "";
            string found = "";
            for (int i = 0; i < result.events.Count; i++)
            {
                var ev = result.events[i];
                if (ev == null) continue;
                if (ev.type == "splash") found = "splash";
                else if (ev.type == "tip") found = "tip";
            }
            return found;
        }

        static int MissionIndex(string missionId)
        {
            if (string.IsNullOrEmpty(missionId) || missionId.Length < 2) return 0;
            if (missionId[0] != 'm' && missionId[0] != 'M') return 0;
            return int.TryParse(missionId.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : 0;
        }

        static string Join(IList<string> ids)
        {
            if (ids == null || ids.Count == 0) return "";
            var parts = new List<string>();
            for (int i = 0; i < ids.Count; i++)
                if (!string.IsNullOrEmpty(ids[i])) parts.Add(ids[i]);
            return parts.Count == 0 ? "" : string.Join(", ", parts);
        }

        static string PaintOf(IList<string> ids)
        {
            if (ids == null || ids.Count == 0) return "";
            var names = new List<string>();
            for (int i = 0; i < ids.Count; i++)
            {
                if (string.IsNullOrEmpty(ids[i])) continue;
                names.Add(FlightDebrief.CosmeticName(ids[i]));
            }
            return names.Count == 0 ? "" : string.Join(", ", names);
        }

        public static string Num(double value, string format)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return null;
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        public static string Compass(double deg)
        {
            if (double.IsNaN(deg)) return "";
            string[] names = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            double turned = deg % 360.0;
            if (turned < 0) turned += 360.0;
            int index = (int)Math.Round(turned / 45.0) % 8;
            return names[index];
        }

        static Dictionary<string, string> Token(string key, string value)
            => new Dictionary<string, string> { [key] = value ?? "" };
    }
}
