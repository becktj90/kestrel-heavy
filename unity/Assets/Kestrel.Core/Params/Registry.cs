// Product parameters from data/params.json. Portrait is a hard lock (decision 7).
using System.IO;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Params
{
    public sealed class Registry
    {
        public readonly JsonObject Raw;
        public string Orientation => Raw.Obj("display").Str("orientation");
        public string BigScreen => Raw.Obj("display").Str("bigScreen");
        public double RefreshHz => Raw.Obj("display").Num("refreshHz");
        public string Quality => Raw.Obj("display").Str("quality");
        public bool BatterySaver => Raw.Obj("display").Bool("batterySaver");
        public bool SafeFlash => Raw.Obj("comfort").Bool("safeFlash");
        public string ReducedMotion => Raw.Obj("comfort").Str("reducedMotion");
        public string FailurePenalty => Raw.Obj("career").Str("failurePenalty");
        public string DefaultLayer => Raw.Obj("designer").Obj("layers").Str("default");
        public string StandardUnlock => Raw.Obj("designer").Obj("layers").Str("standardUnlock");
        public string EngineerToggle => Raw.Obj("designer").Obj("layers").Str("engineer");
        public double WindLimitMs => Raw.Obj("weather").Num("windLimitMs");
        public bool KidHold => Raw.Obj("weather").Bool("kidHold");
        public bool RelaxedHold => Raw.Obj("weather").Bool("relaxedHold");

        /// <summary>
        /// data/params.json flight.launchLatitude. A missing or non-finite value uses Stack.LAUNCH_LATITUDE.
        /// </summary>
        public double LaunchLatitude
        {
            get
            {
                var flight = Raw?.Obj("flight");
                if (flight != null && flight.TryGetValue("launchLatitude", out var v) && v is double d && !double.IsNaN(d) && !double.IsInfinity(d))
                    return d;
                return Stack.LAUNCH_LATITUDE;
            }
        }

        public Registry(JsonObject raw) { Raw = raw; }

        public static Registry Load(string repoRoot)
            => new Registry(MiniJson.ParseObject(File.ReadAllText(Path.Combine(repoRoot, "data", "params.json"))));
    }
}
