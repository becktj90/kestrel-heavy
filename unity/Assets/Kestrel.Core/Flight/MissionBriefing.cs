// Mission briefing for one launch (stepper S4). Offline and deterministic.
// The environment is the mission seed. The design hash is stored beside it and mixed
// the same way FailureModel mixes them, so a replay seed repeats the identical flight
// without reshuffling the weather while the rocket is edited.
// Wind uses Atmosphere.At and the existing bending formula. A calm profile is a no-op.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Flight
{
    /// <summary>Design hash plus mission seed. Restoring both repeats the briefing and the failure draw.</summary>
    public readonly struct ReplaySeed
    {
        public readonly ulong DesignHash;
        public readonly ulong MissionSeed;

        public ReplaySeed(ulong designHash, ulong missionSeed)
        {
            DesignHash = designHash;
            MissionSeed = missionSeed;
        }

        /// <summary>Same mix FailureModel.Fresh stores in its scratch.</summary>
        public ulong FailureRng => MissionBriefing.Mix(DesignHash, MissionSeed);

        /// <summary>Four uppercase hex digits, the seed shown on the briefing card.</summary>
        public string Label => (MissionSeed & 0xFFFFUL).ToString("X4");
    }

    public sealed class BonusGoal
    {
        public string Id, Label;
        public double MaxG = double.NaN;
    }

    /// <summary>One fictional pad. Latitude is the free inclination and the rotation boost.</summary>
    public sealed class LaunchSite
    {
        public string Id, Name, BargeName;
        public double LatitudeDeg;
        public bool Coastal, Barge;

        public double FreeInclinationDeg => Math.Abs(LatitudeDeg);
        public double RotationBoostMs => Earth.SiteRotationSpeed(LatitudeDeg);

        public bool InclinationOpen(double targetIncDeg) => targetIncDeg + 1e-9 >= FreeInclinationDeg;
    }

    /// <summary>
    /// Horizontal wind on a few altitude knots. East is the in-plane component the
    /// equatorial integrator can feel. North is out of that plane and still counts
    /// in dynamic pressure and angle of attack.
    /// </summary>
    public sealed class SeededWind
    {
        public readonly double[] AltitudeM, EastMs, NorthMs;
        public readonly double SurfaceMs, DirectionFromDeg, ShearPerSec;
        public readonly string Compass;
        public readonly bool Calm;

        public SeededWind(double[] altitudeM, double[] eastMs, double[] northMs, double surfaceMs, double directionFromDeg, double shearPerSec, string compass)
        {
            AltitudeM = altitudeM ?? new[] { 0d };
            EastMs = eastMs ?? new[] { 0d };
            NorthMs = northMs ?? new[] { 0d };
            SurfaceMs = surfaceMs;
            DirectionFromDeg = directionFromDeg;
            ShearPerSec = shearPerSec;
            Compass = compass ?? "";
            bool calm = true;
            int n = EastMs.Length < NorthMs.Length ? EastMs.Length : NorthMs.Length;
            for (int i = 0; i < n; i++)
            {
                if (EastMs[i] != 0.0 || NorthMs[i] != 0.0) { calm = false; break; }
            }
            Calm = calm;
        }

        /// <summary>Every knot is zero. Retarget returns the caller's airspeed unchanged.</summary>
        public static SeededWind CalmAir { get; } = new SeededWind(
            new[] { 0d, 20000d }, new[] { 0d, 0d }, new[] { 0d, 0d }, 0, 0, 0, "calm");

        /// <summary>Constant east and north until <paramref name="holdM"/>, then zero. For tests.</summary>
        public static SeededWind Uniform(double eastMs, double northMs, double holdM = 30000)
        {
            return new SeededWind(
                new[] { 0d, holdM, holdM + 1 },
                new[] { eastMs, eastMs, 0d },
                new[] { northMs, northMs, 0d },
                Math.Sqrt(eastMs * eastMs + northMs * northMs),
                0, 0, "test");
        }

        public double EastAt(double altM) => Calm ? 0 : Interp(AltitudeM, EastMs, altM);
        public double NorthAt(double altM) => Calm ? 0 : Interp(AltitudeM, NorthMs, altM);

        public double SpeedAt(double altM)
        {
            double e = EastAt(altM), n = NorthAt(altM);
            return Math.Sqrt(e * e + n * n);
        }

        /// <summary>Vertical speed shear (1/s) from the profile, not a second atmosphere.</summary>
        public double ShearAt(double altM)
        {
            if (Calm) return 0;
            const double h = 1.0;
            return (SpeedAt(altM + h) - SpeedAt(altM - h)) / (2.0 * h);
        }

        /// <summary>
        /// Subtract the east wind from the co-rotating air velocity and fold the north
        /// wind into the airspeed as an out-of-plane crosswind. North is not written
        /// into z, so a planar flight stays in the Headless plane. Calm, or a zero
        /// sample, returns <paramref name="vrelIn"/> and leaves the components alone,
        /// so a null or calm profile cannot move a golden step.
        /// </summary>
        public double Retarget(double altM, double tx, double ty, ref double avx, ref double avy, double vrelIn, double avz = 0)
        {
            if (Calm) return vrelIn;
            double east = EastAt(altM);
            double north = NorthAt(altM);
            if (east == 0.0 && north == 0.0) return vrelIn;
            if (east != 0.0)
            {
                avx -= east * tx;
                avy -= east * ty;
            }
            if (north == 0.0 && avz == 0.0) return Math.Sqrt(avx * avx + avy * avy);
            return Math.Sqrt(avx * avx + avy * avy + avz * avz + north * north);
        }

        /// <summary>
        /// Dynamic pressure, wind angle of attack and bending at one altitude.
        /// Density, pressure and sound speed come from Atmosphere.At. Bending is
        /// StructureModel's q·S·CN_α·α·|x_cp − x_cg|. Calm wind forces α = 0 and
        /// q = ½ ρ v² with that same density.
        /// </summary>
        public WindAero Sample(double altM, double airEast, double airNorth, double airUp, double areaM2, double cnAlpha, double xCpM, double xCgM)
        {
            double z = altM < 0 ? 0 : altM;
            var atm = Atmosphere.At(z);
            double we = EastAt(z), wn = NorthAt(z);
            double calmSpeed = Math.Sqrt(airEast * airEast + airNorth * airNorth + airUp * airUp);
            double speed, alpha;
            if (we == 0.0 && wn == 0.0)
            {
                speed = calmSpeed;
                alpha = 0;
            }
            else
            {
                double rx = airEast - we, ry = airNorth - wn, rz = airUp;
                speed = Math.Sqrt(rx * rx + ry * ry + rz * rz);
                alpha = Angle(airEast, airNorth, airUp, rx, ry, rz);
            }
            double q = 0.5 * atm.rho * speed * speed;
            double bend = StructureModel.BendingMomentNm(new LoadState
            {
                qPa = q,
                areaM2 = areaM2,
                cnAlpha = cnAlpha,
                alphaRad = alpha,
                xCpM = xCpM,
                xCgM = xCgM,
            });
            return new WindAero
            {
                Atmosphere = atm,
                AirSpeedMs = speed,
                QPa = q,
                AlphaRad = alpha,
                BendingNm = bend,
                ShearPerSec = ShearAt(z),
            };
        }

        static double Interp(double[] xs, double[] ys, double x)
        {
            int n = xs.Length < ys.Length ? xs.Length : ys.Length;
            if (n <= 0) return 0;
            if (n == 1 || x <= xs[0]) return ys[0];
            if (x >= xs[n - 1]) return ys[n - 1];
            int i = 1;
            while (i < n && xs[i] < x) i++;
            if (i >= n) return ys[n - 1];
            double span = xs[i] - xs[i - 1];
            if (!(span > 0)) return ys[i - 1];
            double u = (x - xs[i - 1]) / span;
            return ys[i - 1] + (ys[i] - ys[i - 1]) * u;
        }

        static double Angle(double ax, double ay, double az, double bx, double by, double bz)
        {
            double am = Math.Sqrt(ax * ax + ay * ay + az * az);
            double bm = Math.Sqrt(bx * bx + by * by + bz * bz);
            if (am < 1e-12 || bm < 1e-12) return 0;
            double cos = (ax * bx + ay * by + az * bz) / (am * bm);
            if (cos > 1) cos = 1;
            else if (cos < -1) cos = -1;
            return Math.Acos(cos);
        }
    }

    public struct WindAero
    {
        public AtmState Atmosphere;
        public double AirSpeedMs, QPa, AlphaRad, BendingNm, ShearPerSec;
    }

    /// <summary>One KH-n card: fictional payload, orbit, goal, bonuses, and the seeded pad, clock and wind.</summary>
    public sealed class MissionBriefing
    {
        public string FlightCode, ContractCode, ContractId, ContractName;
        public string PayloadName, PayloadKind, Title, Goal, Summary, StatusLine, EngineerLine;
        public int FlightNumber;
        public bool FreeBuild;
        public double MassKg, ApoKm, PeriKm, IncDeg;
        public string TargetKind;
        public readonly List<BonusGoal> Bonuses = new List<BonusGoal>();
        public LaunchSite Site;
        public string TimeId, TimeLabel, Lighting;
        public int Hour, DayOfYear;
        public SeededWind Wind;
        public ReplaySeed Replay;
        public string WindLine;
        public double WindKt;

        public ulong DesignHash => Replay.DesignHash;
        public ulong MissionSeed => Replay.MissionSeed;

        /// <summary>FailureModel.Fresh uses this mix. Briefing draws do not consume that stream.</summary>
        public static ulong Mix(ulong designHash, ulong missionSeed)
        {
            ulong rng = designHash ^ (missionSeed * 0x9E3779B97F4A7C15UL);
            if (rng == 0) rng = 0xA5A5A5A5A5A5A5A5UL;
            return rng;
        }

        /// <summary>Launch identity for the card. Separate from <see cref="Mix"/> so editing the rocket keeps the weather.</summary>
        public static ulong EnvironmentState(ulong missionSeed)
        {
            ulong s = missionSeed ^ 0xD1B54A32D192ED03UL;
            if (s == 0) s = 0xA5A5A5A5A5A5A5A5UL;
            return s;
        }

        /// <summary>splitmix64 uniform in [0, 1), the same step FailureModel uses.</summary>
        public static double Draw(ref ulong state)
        {
            ulong z = state += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return (z >> 11) * (1.0 / 9007199254740992.0);
        }

        public static int FlightNumberOf(ulong missionSeed)
        {
            if (missionSeed == 0) return 1;
            int n = (int)(missionSeed % 9999UL) + 1;
            return n < 1 ? 1 : n;
        }

        public static string FormatWind(double speedMs, string compass, double ktPerMs)
        {
            double kt = speedMs * ktPerMs;
            int shown = (int)Math.Round(kt, MidpointRounding.AwayFromZero);
            if (shown < 0) shown = 0;
            if (string.IsNullOrEmpty(compass)) compass = "calm";
            return shown.ToString(CultureInfo.InvariantCulture) + " kt " + compass;
        }

        public static double Rayleigh(double u, double scaleMs)
        {
            if (u < 1e-12) u = 1e-12;
            if (!(scaleMs > 0)) return 0;
            return scaleMs * Math.Sqrt(-2.0 * Math.Log(u));
        }

        public static MissionBriefing Load(string repoRoot, int missionIndex, ulong designHash, ulong missionSeed)
            => Compose(repoRoot, missionIndex, new ReplaySeed(designHash, missionSeed), false);

        public static MissionBriefing Free(string repoRoot, ulong designHash, ulong missionSeed)
            => Compose(repoRoot, 0, new ReplaySeed(designHash, missionSeed), true);

        public static MissionBriefing FromReplay(string repoRoot, ReplaySeed replay, int missionIndex, bool freeBuild)
            => Compose(repoRoot, missionIndex, replay, freeBuild);

        /// <summary>Opt in. Null wind and the default latitude stay on the golden path until this is called.</summary>
        public void Bind(SimOptions opts)
        {
            if (opts == null) throw new ArgumentNullException(nameof(opts));
            opts.wind = Wind ?? SeededWind.CalmAir;
            if (Site != null) opts.latitude = Site.LatitudeDeg;
        }

        static MissionBriefing Compose(string repoRoot, int missionIndex, ReplaySeed replay, bool freeBuild)
        {
            var campaign = Campaign.Load(repoRoot);
            var sites = SiteBook.Load(repoRoot);
            var weather = WeatherBook.Load(repoRoot);
            var raw = MiniJson.ParseObject(File.ReadAllText(Path.Combine(repoRoot, "data", "campaign.json")));
            string prefix = raw.Str("flightPrefix", "KH");
            if (string.IsNullOrEmpty(prefix)) prefix = "KH";

            CampaignMission mission = null;
            JsonObject row = null;
            if (!freeBuild)
            {
                mission = campaign.Mission(missionIndex);
                if (mission == null) throw new ArgumentOutOfRangeException(nameof(missionIndex), "no campaign mission " + missionIndex);
                foreach (JsonObject m in raw.Arr("missions"))
                    if ((int)m.Num("index") == missionIndex) { row = m; break; }
            }

            ulong stream = EnvironmentState(replay.MissionSeed);
            double uSurface = Draw(ref stream);
            double uDir = Draw(ref stream);
            double uShearA = Draw(ref stream);
            double uShearB = Draw(ref stream);
            double uTime = Draw(ref stream);
            double uSite = Draw(ref stream);
            double uDay = Draw(ref stream);

            double surface = Rayleigh(uSurface, weather.RayleighScaleMs);
            var dir = weather.Direction(uDir);
            double shear = weather.Shear(uShearA, uShearB);
            var time = weather.Time(uTime);
            int day = 1 + (int)(uDay * 365.0);
            if (day < 1) day = 1;
            if (day > 365) day = 365;

            bool barge = mission != null && mission.LandingMode == "barge";
            var site = sites.Pick(uSite, barge);
            var wind = weather.Profile(surface, dir, shear);

            var card = new MissionBriefing
            {
                Replay = replay,
                FlightNumber = FlightNumberOf(replay.MissionSeed),
                FreeBuild = freeBuild,
                Site = site,
                TimeId = time.Id,
                TimeLabel = time.Label,
                Lighting = time.Lighting,
                Hour = time.Hour,
                DayOfYear = day,
                Wind = wind,
                WindKt = surface * weather.KtPerMs,
                WindLine = FormatWind(surface, dir.Id, weather.KtPerMs),
            };
            card.FlightCode = prefix + "-" + card.FlightNumber.ToString(CultureInfo.InvariantCulture);

            if (freeBuild || mission == null)
            {
                card.ContractCode = "";
                card.ContractId = "free";
                card.ContractName = "Free build";
                card.PayloadName = "Your payload";
                card.PayloadKind = "Free build";
                card.Goal = "Pick a payload and an orbit. The wind, the clock and the pad are already set.";
                card.Title = "Free build";
                card.TargetKind = "free";
            }
            else
            {
                var brief = row != null ? row.Obj("briefing") : null;
                card.ContractCode = prefix + "-" + mission.Index.ToString(CultureInfo.InvariantCulture);
                card.ContractId = mission.Id;
                card.ContractName = mission.Name;
                card.PayloadName = brief != null ? brief.Str("payloadName", mission.Name) : mission.Name;
                card.PayloadKind = brief != null ? brief.Str("payloadKind", mission.PayloadId) : mission.PayloadId;
                card.MassKg = mission.MassKg;
                card.ApoKm = mission.ApoKm;
                card.PeriKm = mission.PeriKm;
                card.IncDeg = mission.IncDeg;
                card.TargetKind = mission.TargetKind;
                card.Goal = brief != null ? brief.Str("goal", mission.Name) : mission.Name;
                card.Title = card.PayloadName + " " + mission.Name;
                if (brief != null && brief.Arr("bonuses") != null)
                {
                    foreach (JsonObject b in brief.Arr("bonuses"))
                    {
                        var goal = new BonusGoal { Id = b.Str("id"), Label = b.Str("label") };
                        if (b.ContainsKey("maxG")) goal.MaxG = b.Num("maxG");
                        card.Bonuses.Add(goal);
                    }
                }
            }

            card.Summary = card.Title + " — " + card.Goal;
            string pad = site != null ? site.Name : "Pad";
            card.StatusLine = "Wind " + card.WindLine + " · " + card.TimeLabel + " launch · Pad: " + pad + " · Seed " + replay.Label;
            double lat = site != null ? site.LatitudeDeg : 0;
            double boost = site != null ? site.RotationBoostMs : 0;
            double freeInc = site != null ? site.FreeInclinationDeg : 0;
            card.EngineerLine = "wind " + surface.ToString("0.00", CultureInfo.InvariantCulture) + " m/s from " + dir.Id
                + ", shear " + shear.ToString("0.0000", CultureInfo.InvariantCulture) + " 1/s, lat "
                + lat.ToString("0.0", CultureInfo.InvariantCulture) + "°, free inclination "
                + freeInc.ToString("0.0", CultureInfo.InvariantCulture) + "°, rotation boost "
                + boost.ToString("0.0", CultureInfo.InvariantCulture) + " m/s. Failure mix "
                + replay.FailureRng.ToString("x16");
            return card;
        }
    }

    public sealed class WindDirection
    {
        public string Id;
        public double FromDeg;
    }

    public sealed class TimeOfDay
    {
        public string Id, Label, Lighting;
        public int Hour;
    }

    public sealed class SiteBook
    {
        public readonly List<LaunchSite> Sites = new List<LaunchSite>();

        public static SiteBook Load(string repoRoot)
            => FromJson(MiniJson.ParseObject(File.ReadAllText(Path.Combine(repoRoot, "data", "sites.json"))));

        public static SiteBook FromJson(JsonObject o)
        {
            var book = new SiteBook();
            var rows = o.Arr("sites");
            if (rows == null || rows.Count == 0) throw new InvalidOperationException("sites.json has no pads");
            foreach (JsonObject s in rows)
            {
                book.Sites.Add(new LaunchSite
                {
                    Id = s.Str("id"),
                    Name = s.Str("name"),
                    BargeName = s.Str("bargeName"),
                    LatitudeDeg = s.Num("latitudeDeg"),
                    Coastal = s.Bool("coastal"),
                    Barge = s.Bool("barge"),
                });
            }
            return book;
        }

        public LaunchSite Pick(double u, bool bargeOnly)
        {
            var pool = new List<LaunchSite>();
            for (int i = 0; i < Sites.Count; i++)
                if (!bargeOnly || Sites[i].Barge) pool.Add(Sites[i]);
            if (pool.Count == 0) pool = Sites;
            int n = (int)(u * pool.Count);
            if (n >= pool.Count) n = pool.Count - 1;
            if (n < 0) n = 0;
            return pool[n];
        }
    }

    public sealed class WeatherBook
    {
        public double KtPerMs = 3600.0 / 1852.0;
        public double JetAltKm = 10.5;
        public double ShearLayerM = 8000;
        public double SoftCapMs = 100;
        public double SoftCapExponent = 4;
        public double FadeAltKm = 20;
        public double RayleighScaleMs = 4.2;
        public double ShearMedianPerSec = 0.0045;
        public double ShearLogSigma = 0.45;
        public double DecayAboveJet = 0.15;
        public readonly List<double> LevelsKm = new List<double>();
        public readonly List<WindDirection> Directions = new List<WindDirection>();
        public readonly List<TimeOfDay> Times = new List<TimeOfDay>();

        public static WeatherBook Load(string repoRoot)
            => FromJson(MiniJson.ParseObject(File.ReadAllText(Path.Combine(repoRoot, "data", "weather.json"))));

        public static WeatherBook FromJson(JsonObject o)
        {
            var w = new WeatherBook();
            if (o.ContainsKey("ktPerMs")) w.KtPerMs = o.Num("ktPerMs");
            if (o.ContainsKey("jetAltKm")) w.JetAltKm = o.Num("jetAltKm");
            if (o.ContainsKey("shearLayerM")) w.ShearLayerM = o.Num("shearLayerM");
            if (o.ContainsKey("softCapMs")) w.SoftCapMs = o.Num("softCapMs");
            if (o.ContainsKey("softCapExponent")) w.SoftCapExponent = o.Num("softCapExponent");
            if (o.ContainsKey("fadeAltKm")) w.FadeAltKm = o.Num("fadeAltKm");
            if (o.ContainsKey("rayleighScaleMs")) w.RayleighScaleMs = o.Num("rayleighScaleMs");
            if (o.ContainsKey("shearMedianPerSec")) w.ShearMedianPerSec = o.Num("shearMedianPerSec");
            if (o.ContainsKey("shearLogSigma")) w.ShearLogSigma = o.Num("shearLogSigma");
            if (o.ContainsKey("decayAboveJet")) w.DecayAboveJet = o.Num("decayAboveJet");
            var levels = o.Arr("levelsKm");
            if (levels != null) foreach (var x in levels) if (x is double d) w.LevelsKm.Add(d);
            if (w.LevelsKm.Count == 0) w.LevelsKm.AddRange(new[] { 0d, 3, 6, 9, 12 });
            var dirs = o.Arr("directions");
            if (dirs != null)
            {
                foreach (JsonObject d in dirs)
                    w.Directions.Add(new WindDirection { Id = d.Str("id"), FromDeg = d.Num("fromDeg") });
            }
            if (w.Directions.Count == 0) w.Directions.Add(new WindDirection { Id = "W", FromDeg = 270 });
            var times = o.Arr("timesOfDay");
            if (times != null)
            {
                foreach (JsonObject t in times)
                    w.Times.Add(new TimeOfDay
                    {
                        Id = t.Str("id"),
                        Label = t.Str("label"),
                        Lighting = t.Str("lighting", t.Str("id")),
                        Hour = (int)t.Num("hour"),
                    });
            }
            if (w.Times.Count == 0) w.Times.Add(new TimeOfDay { Id = "noon", Label = "Noon", Lighting = "noon", Hour = 12 });
            return w;
        }

        public WindDirection Direction(double u) => Directions[Index(u, Directions.Count)];
        public TimeOfDay Time(double u) => Times[Index(u, Times.Count)];

        public double Shear(double u1, double u2)
        {
            if (u1 < 1e-12) u1 = 1e-12;
            double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            double median = ShearMedianPerSec > 0 ? ShearMedianPerSec : 0.0045;
            return Math.Exp(Math.Log(median) + ShearLogSigma * z);
        }

        public SeededWind Profile(double surfaceMs, WindDirection direction, double shearPerSec)
        {
            if (!(surfaceMs > 0)) surfaceMs = 0;
            double rawPeak = surfaceMs + shearPerSec * ShearLayerM;
            double jetPeak = SoftCap(rawPeak, SoftCapMs, SoftCapExponent);
            double top = LevelsKm[LevelsKm.Count - 1];
            int extra = FadeAltKm > top ? 1 : 0;
            var alt = new double[LevelsKm.Count + extra];
            var east = new double[alt.Length];
            var north = new double[alt.Length];
            double toward = ((direction != null ? direction.FromDeg : 0) + 180.0) * Math.PI / 180.0;
            double ce = Math.Sin(toward), cn = Math.Cos(toward);
            for (int i = 0; i < LevelsKm.Count; i++)
            {
                double speed = Shaped(LevelsKm[i], surfaceMs, jetPeak, top);
                alt[i] = LevelsKm[i] * 1000.0;
                east[i] = speed * ce;
                north[i] = speed * cn;
            }
            if (extra == 1)
            {
                alt[alt.Length - 1] = FadeAltKm * 1000.0;
                east[east.Length - 1] = 0;
                north[north.Length - 1] = 0;
            }
            return new SeededWind(alt, east, north, surfaceMs, direction != null ? direction.FromDeg : 0, shearPerSec, direction != null ? direction.Id : "");
        }

        double Shaped(double altKm, double surface, double jetPeak, double topKm)
        {
            if (altKm <= 0) return surface;
            double jet = JetAltKm > 0 ? JetAltKm : 10.5;
            double t = altKm / jet;
            if (t <= 1.0)
            {
                double s = t * t * (3.0 - 2.0 * t);
                return surface + (jetPeak - surface) * s;
            }
            double span = topKm - jet;
            double u = span > 0 ? (altKm - jet) / span : 1;
            if (u < 0) u = 0;
            if (u > 1) u = 1;
            return jetPeak - DecayAboveJet * (jetPeak - surface) * u;
        }

        static double SoftCap(double raw, double cap, double n)
        {
            if (raw <= 0 || cap <= 0) return 0;
            if (!(n > 0)) n = 4;
            double x = raw / cap;
            return raw / Math.Pow(1.0 + Math.Pow(x, n), 1.0 / n);
        }

        static int Index(double u, int count)
        {
            if (count <= 1) return 0;
            int n = (int)(u * count);
            if (n >= count) n = count - 1;
            if (n < 0) n = 0;
            return n;
        }
    }
}
