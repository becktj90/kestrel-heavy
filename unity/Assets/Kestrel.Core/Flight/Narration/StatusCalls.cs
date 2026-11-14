using System;
using System.Collections.Generic;

namespace Kestrel.Core.Flight.Narration
{
    /// <summary>A mission-status call that became true: the manifest event, the core stage it is about (0 for none),
    /// and whether it is dropped (not queued) when a line is already playing.</summary>
    public readonly struct StatusCue
    {
        public readonly string EventId;
        public readonly int LitStage;
        public readonly bool DropIfBusy;
        public StatusCue(string eventId, int litStage, bool dropIfBusy)
        {
            EventId = eventId; LitStage = litStage; DropIfBusy = dropIfBusy;
        }
    }

    /// <summary>
    /// B01 mission-status calls (narration trigger contract, kh-gen5 triggers.md B and C). Each call is raised only on
    /// the frame its fact becomes true for the rocket flying, and only when every input it needs was known; an input
    /// that cannot be read keeps the call silent (fail closed). Pure, so it is tested without a scene. The caller hands
    /// the cues to <see cref="CallTriggers.Raise"/>, which keeps the freshness and never-over-a-waiting-call rules.
    /// <list type="bullet">
    /// <item><c>status.ontrack</c>: the board verdict turns "On track" for the first time after "Climbing", with the live
    /// spare known and not negative. Once per flight.</item>
    /// <item><c>status.short</c>: the verdict turns "Short on speed" (LiveSpare held below zero, DECISIONS #90) with the
    /// spare still negative. Once, then again only after a clear.</item>
    /// <item><c>status.short.clear</c>: "Short on speed" back to "On track", with the spare known and not negative.</item>
    /// <item><c>status.speed</c>: the verdict turns "Speed is there" while the engines still burn (the takes say
    /// "stand by for cutoff"). Once.</item>
    /// <item><c>status.heating</c>: the verdict is "Heating hard" and dynamic pressure is still rising (before the q
    /// peak, the takes say "coming up on max-Q"), and no max-Q throttle beat in the last 5 s. Once.</item>
    /// <item><c>engine-out.ok</c>: 3 s after a tolerated engine-out the vehicle is intact and still climbing.</item>
    /// <item><c>landing.alt.1k|500|100|10</c>: during the booster's landing burn, its height above the landing surface
    /// first drops below the mark on a live step. A step that jumps over more than one mark (skip, settle) says none.
    /// Dropped, not queued, when a line is playing.</item>
    /// <item><c>landing.legs</c>: the booster's legs finish deploying on a live step of a planned landing.</item>
    /// </list>
    /// </summary>
    public sealed class StatusCalls
    {
        public const string OnTrack = "status.ontrack", Short = "status.short", ShortClear = "status.short.clear",
            Speed = "status.speed", Heating = "status.heating", EngineOut = "engine-out", EngineOutOk = "engine-out.ok",
            LandingLegs = "landing.legs";
        public static readonly string[] LandingAltEvents = { "landing.alt.1k", "landing.alt.500", "landing.alt.100", "landing.alt.10" };
        public static readonly double[] LandingAltMarksM = { 1000, 500, 100, 10 };
        /// <summary>Every event this class can raise (the manifest check and the docs read it).</summary>
        public static readonly string[] Events =
        {
            OnTrack, Short, ShortClear, Speed, Heating, EngineOutOk, LandingLegs,
            "landing.alt.1k", "landing.alt.500", "landing.alt.100", "landing.alt.10",
        };

        /// <summary>engine-out.ok is judged this long (sim seconds) after the engine-out.</summary>
        public const double EngineOutOkSec = 3;
        /// <summary>A step that reaches the engine-out.ok moment later than this is too late to say it.</summary>
        public const double EngineOutOkLateSec = 1;
        /// <summary>No status.heating within this long (sim seconds) after a max-Q throttle beat.</summary>
        public const double MaxQBeatQuietSec = 5;
        /// <summary>A booster step longer than this (sim seconds) is a jump (skip or settle), not a live step.</summary>
        public const double BoosterLiveStepSec = 1;
        public const double LegsDeployed = 0.999;

        readonly List<StatusCue> due = new List<StatusCue>();
        string lastVerdict;
        bool sawClimbing, onTrackSaid, speedSaid, heatingSaid, shortArmed = true, inShort, lost;
        double prevQ = double.NaN, lastMaxQBeat = double.NegativeInfinity;
        double eoAt = double.NaN; int eoLit; bool eoJudged;
        double prevAlt = double.NaN, prevAltT = double.NaN;
        double boosterAlt = double.NaN, boosterT = double.NaN, boosterLegs = double.NaN;
        bool boosterBurn, legsSaid;
        readonly bool[] altSaid = new bool[4];

        public StatusCalls() { Reset(); }

        public void Reset()
        {
            due.Clear();
            lastVerdict = null;
            sawClimbing = onTrackSaid = speedSaid = heatingSaid = inShort = lost = false;
            shortArmed = true;
            prevQ = double.NaN; lastMaxQBeat = double.NegativeInfinity;
            eoAt = double.NaN; eoLit = 0; eoJudged = false;
            prevAlt = prevAltT = double.NaN;
            boosterAlt = boosterT = boosterLegs = double.NaN;
            boosterBurn = legsSaid = false;
            for (int i = 0; i < altSaid.Length; i++) altSaid[i] = false;
        }

        /// <summary>Cues raised since the last call, in order. The list is cleared.</summary>
        public void Drain(List<StatusCue> into)
        {
            if (into != null) into.AddRange(due);
            due.Clear();
        }

        /// <summary>Ids raised and not yet drained. For tests.</summary>
        public IEnumerable<string> DueIds { get { foreach (var c in due) yield return c.EventId; } }

        /// <summary>
        /// One frame of the live board (<see cref="WebcastBoard.Pulse"/>) while the ascent flies (not the count, the
        /// climax coast or a lost vehicle).
        /// </summary>
        /// <param name="verdict">The board's verdict line this frame.</param>
        /// <param name="qPa">Live dynamic pressure.</param>
        /// <param name="thrusting">An engine is burning.</param>
        /// <param name="spareOk">Live spare (LiveSpare) is not negative; null when there is no live spare to read.</param>
        /// <param name="simT">Flight seconds.</param>
        public void OnVerdict(string verdict, double qPa, bool thrusting, bool? spareOk, double simT)
        {
            if (lost || string.IsNullOrEmpty(verdict)) return;
            bool changed = verdict != lastVerdict;
            bool rising = !double.IsNaN(prevQ) && !double.IsNaN(qPa) && qPa > prevQ;
            if (verdict == WebcastBoard.VerdictClimbing) sawClimbing = true;
            if (verdict == WebcastBoard.VerdictHeating)
            {
                if (!heatingSaid && rising && simT - lastMaxQBeat > MaxQBeatQuietSec)
                {
                    heatingSaid = true;
                    Add(Heating, 0, false);
                }
            }
            else if (changed && verdict == WebcastBoard.VerdictShort)
            {
                if (shortArmed && spareOk == false)
                {
                    shortArmed = false;
                    inShort = true;
                    Add(Short, 0, false);
                }
            }
            else if (changed && verdict == WebcastBoard.VerdictOnTrack)
            {
                if (inShort)
                {
                    if (spareOk == true)
                    {
                        inShort = false;
                        shortArmed = true;
                        onTrackSaid = true;
                        Add(ShortClear, 0, false);
                    }
                }
                else if (!onTrackSaid && sawClimbing && spareOk == true)
                {
                    onTrackSaid = true;
                    Add(OnTrack, 0, false);
                }
            }
            else if (changed && verdict == WebcastBoard.VerdictSpeed)
            {
                // The speed is there: a short call no longer stands, and it re-arms without a clear call.
                if (inShort) { inShort = false; shortArmed = true; }
                if (!speedSaid && thrusting)
                {
                    speedSaid = true;
                    Add(Speed, 0, false);
                }
            }
            lastVerdict = verdict;
            prevQ = qPa;
        }

        /// <summary>A sim or flight event. <paramref name="litStage"/>: the core stage it is about (0 when unknown).</summary>
        public void OnSimEvent(string type, int litStage, double simT)
        {
            if (string.IsNullOrEmpty(type)) return;
            switch (type)
            {
                case "failure":
                case "impact":
                    lost = true;
                    break;
                case EngineOut:
                    if (double.IsNaN(eoAt)) { eoAt = simT; eoLit = litStage; eoJudged = false; }
                    break;
                case AscentDirector.MaxQThrottleEvent:
                case AscentDirector.MaxQThroughEvent:
                    lastMaxQBeat = simT;
                    break;
            }
        }

        /// <summary>The ascent's clock and altitude, each frame.</summary>
        public void OnFlight(double simT, double altM)
        {
            if (!double.IsNaN(eoAt) && !eoJudged && simT - eoAt >= EngineOutOkSec)
            {
                eoJudged = true;
                bool onTime = simT - eoAt <= EngineOutOkSec + EngineOutOkLateSec;
                bool climbing = !double.IsNaN(prevAlt) && !double.IsNaN(altM) && simT > prevAltT && altM > prevAlt;
                if (!lost && onTime && climbing) Add(EngineOutOk, eoLit, false);
            }
            if (!double.IsNaN(simT) && !double.IsNaN(altM)) { prevAlt = altM; prevAltT = simT; }
        }

        /// <summary>
        /// One live step of the booster (RecoveryShow). <paramref name="planned"/>: the player gave it a landing and it
        /// is flying that landing (BoosterLanding.ShowsLanding). <paramref name="landingBurn"/>: its phase is the landing
        /// burn. <paramref name="altM"/>: height above the landing surface. <paramref name="legs01"/>: leg deployment, 0 to 1.
        /// </summary>
        public void OnBooster(bool planned, bool landingBurn, double altM, double legs01, double t)
        {
            if (!planned || double.IsNaN(t) || double.IsNaN(altM))
            {
                boosterAlt = boosterT = boosterLegs = double.NaN;
                boosterBurn = false;
                return;
            }
            bool live = !double.IsNaN(boosterT) && t > boosterT && t - boosterT <= BoosterLiveStepSec;
            if (live && landingBurn && boosterBurn)
            {
                int crossed = 0, which = -1;
                for (int i = 0; i < LandingAltMarksM.Length; i++)
                {
                    double h = LandingAltMarksM[i];
                    if (!altSaid[i] && boosterAlt > h && altM <= h) { crossed++; which = i; altSaid[i] = true; }
                }
                if (crossed == 1) Add(LandingAltEvents[which], 0, true);
            }
            else if (!double.IsNaN(boosterAlt))
            {
                // A jump or a step outside the burn: whatever it passed is not news any more.
                for (int i = 0; i < LandingAltMarksM.Length; i++)
                    if (boosterAlt > LandingAltMarksM[i] && altM <= LandingAltMarksM[i]) altSaid[i] = true;
            }
            if (!legsSaid && !double.IsNaN(boosterLegs) && !double.IsNaN(legs01) && boosterLegs < LegsDeployed && legs01 >= LegsDeployed)
            {
                legsSaid = true;
                if (live) Add(LandingLegs, 0, true);
            }
            boosterAlt = altM;
            boosterT = t;
            boosterLegs = legs01;
            boosterBurn = landingBurn;
        }

        void Add(string id, int lit, bool dropIfBusy) => due.Add(new StatusCue(id, lit, dropIfBusy));
    }
}
