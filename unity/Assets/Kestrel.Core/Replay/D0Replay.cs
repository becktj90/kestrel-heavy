// D0 arcade contract. Matter is not re-simulated here; U4/U5 rewrite the mission.
// This locks the input clock, FNV-1a, portrait viewport, seeds, and the recorded expect block.
using System.Collections.Generic;

namespace Kestrel.Core.Replay
{
    public static class D0Replay
    {
        public const int ViewportW = 430, ViewportH = 932;
        public const string PortraitSnap = "{\"w\":430,\"h\":932}";
        public const double FrameMs = 1000.0 / 60.0;
        public static readonly string[] Missions = { "KH-1", "KH-2", "KH-3", "KH-4", "KH-5" };
        public static readonly string[] Difficulties = { "KID", "CADET", "PAD_RAT" };

        public static int SeedFor(string mission, string difficulty)
        {
            int mi = IndexOf(Missions, mission), di = IndexOf(Difficulties, difficulty);
            return 1000 + mi * 10 + di;
        }

        static int IndexOf(string[] a, string s) { for (int i = 0; i < a.Length; i++) if (a[i] == s) return i; return -1; }

        /// <summary>FNV-1a of a JS string (char codes, imul 0x01000193), lowercase 8 hex digits.</summary>
        public static string Fnv1a(string s)
        {
            uint h = 0x811c9dc5;
            for (int i = 0; i < s.Length; i++)
            {
                h ^= s[i];
                h = unchecked((uint)((int)h * (int)0x01000193));
            }
            return h.ToString("x8");
        }

        /// <summary>Held ArrowLeft=1, ArrowRight=2, Space=4, sampled every 30 frames after applying events at frame &lt;= t.</summary>
        public static int[] InputMasks(IList<(int frame, string key, int down)> input, int frames)
        {
            int left = 0, right = 0, space = 0, ei = 0;
            var masks = new List<int>();
            for (int f = 0; f < frames; f += 30)
            {
                while (ei < input.Count && input[ei].frame <= f)
                {
                    var ev = input[ei++];
                    int bit = ev.down != 0 ? 1 : 0;
                    if (ev.key == "ArrowLeft") left = bit;
                    else if (ev.key == "ArrowRight") right = bit;
                    else if (ev.key == "Space") space = bit;
                }
                int m = 0;
                if (left != 0) m |= 1;
                if (right != 0) m |= 2;
                if (space != 0) m |= 4;
                masks.Add(m);
            }
            return masks.ToArray();
        }

        public static string GradeLanding(string kind, bool recovered, double vy, double dx, double tilt, double landingVy, double landingTol)
        {
            if (kind != "deck") return null;
            vy = System.Math.Abs(FalsyZero(vy));
            dx = System.Math.Abs(FalsyZero(dx));
            tilt = System.Math.Abs(FalsyZero(tilt));
            double limVy = System.Math.Max(0.1, double.IsNaN(landingVy) || landingVy == 0 ? 6 : landingVy);
            double limDx = System.Math.Max(1, double.IsNaN(landingTol) || landingTol == 0 ? 36 : landingTol);
            if (!recovered) return "bronze";
            bool Meets(double rVy, double rDx, double rTilt) => vy <= limVy * rVy && dx <= limDx * rDx && tilt <= rTilt;
            if (Meets(0.55, 0.4, 4)) return "gold";
            if (Meets(0.8, 0.75, 8)) return "silver";
            return "bronze";
        }

        static double FalsyZero(double v) => double.IsNaN(v) || v == 0 ? 0 : v;
    }
}
