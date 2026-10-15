using System;

namespace Kestrel.Game.Guide
{
    /// <summary>Reveals a line at 25 / 40 / 70 characters per second. Reduce Motion and Instant show it whole.
    /// A blip is due at most every three characters, and never when muted.</summary>
    public sealed class Typewriter
    {
        public string Full { get; private set; } = "";
        public int Shown { get; private set; }
        public bool Done => Shown >= (Full != null ? Full.Length : 0);
        public bool BlipPending { get; private set; }

        float carry;
        float cps;
        bool whole;
        bool muted;
        int nextBlip = GuidePacing.BlipEvery;

        public string Visible
        {
            get
            {
                if (string.IsNullOrEmpty(Full) || Shown <= 0) return "";
                int n = Shown > Full.Length ? Full.Length : Shown;
                return Full.Substring(0, n);
            }
        }

        public void Start(string text, SubtitleSpeed speed, bool reduceMotion, bool muted)
        {
            Full = text ?? "";
            this.muted = muted;
            whole = GuidePacing.ShowWhole(speed, reduceMotion);
            cps = GuidePacing.Cps(speed);
            Shown = whole ? Full.Length : 0;
            carry = 0f;
            BlipPending = false;
            nextBlip = GuidePacing.BlipEvery;
        }

        public void Finish()
        {
            Shown = Full != null ? Full.Length : 0;
            BlipPending = false;
        }

        public void Tick(float dt)
        {
            BlipPending = false;
            if (Done || whole || !(dt > 0f) || !(cps > 0f)) return;
            carry += dt * cps;
            int add = (int)carry;
            if (add <= 0) return;
            carry -= add;
            int before = Shown;
            int len = Full.Length;
            Shown = Shown + add;
            if (Shown > len) Shown = len;
            if (muted || Shown <= before) return;
            if (Shown >= nextBlip)
            {
                BlipPending = true;
                while (nextBlip <= Shown) nextBlip += GuidePacing.BlipEvery;
            }
        }
    }
}
