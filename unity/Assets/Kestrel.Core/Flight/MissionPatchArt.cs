// Original circular mission patches for KH-1..KH-20, plus a deterministic patch for
// any contract or free-flight id. Embroidery is drawn in MissionPatchRaster.
// Player-facing lines never carry a raw mission id.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;

namespace Kestrel.Core.Flight
{
    public enum PatchAct { FirstLight, Regulars, Heavy, FarShore }

    public enum PatchChrome { Open, Locked, Medal }

    [Flags]
    public enum PatchMarks
    {
        None = 0,
        Rocket = 1 << 0,
        ArcShort = 1 << 1,
        ArcTall = 1 << 2,
        ArcReturn = 1 << 3,
        ArcTransfer = 1 << 4,
        Orbit = 1 << 5,
        Barge = 1 << 6,
        Moon = 1 << 7,
        Box = 1 << 8,
        Lantern = 1 << 9,
        Cloud = 1 << 10,
        Sun = 1 << 11,
        Capsule = 1 << 12,
        Window = 1 << 13,
        Scope = 1 << 14,
        Rack = 1 << 15,
        Dish = 1 << 16,
        Legs = 1 << 17,
        Horizon = 1 << 18,
        Strap = 1 << 19,
        Coast = 1 << 20,
        Probe = 1 << 21,
    }

    public struct PatchPalette
    {
        public byte FR, FG, FB, RR, RG, RB, TR, TG, TB, AR, AG, AB, IR, IG, IB;
        public int Field => (FR << 16) | (FG << 8) | FB;
        public int Rim => (RR << 16) | (RG << 8) | RB;
        public int Thread => (TR << 16) | (TG << 8) | TB;
        public int Accent => (AR << 16) | (AG << 8) | AB;
    }

    public sealed class MissionPatchSpec
    {
        public int Index;
        public string MissionId;
        public string Flight;
        public string Name;
        public string Motto;
        public string Anatomy;
        public string ActName;
        public PatchAct Act;
        public int Stars;
        public ulong EmblemSeed;
        public PatchMarks Marks;

        public string ArcName => string.IsNullOrEmpty(Name) ? "" : Name.ToUpperInvariant();
    }

    /// <summary>Where the rim lettering and the stars sit. Shared by the raster and the tests.</summary>
    public sealed class PatchLayout
    {
        public int Size;
        public bool NameOnArc;
        public bool FlightFits;
        public bool EmblemInside;
        public bool StarsInside;
        public bool StarsSeparated;
        public int StarCount;
        public bool GoldRing;
        public bool GreyOutline;
        public bool ArcFontCovers;
    }

    public static class PatchGrid
    {
        /// <summary>
        /// Columns for the patch collection in UI Toolkit panel units
        /// (SE 375, Pro Max about 308, iPad about 500).
        /// </summary>
        public static int Columns(float panelWidth)
        {
            if (panelWidth >= 460f) return 3;
            return 2;
        }

        public static float PatchSize(float panelWidth)
        {
            if (panelWidth < 32f) panelWidth = 375f;
            int cols = Columns(panelWidth);
            float gap = 8f * (cols + 1);
            float cell = (panelWidth - gap) / cols;
            float patch = (float)Math.Floor(cell - 28f);
            if (patch > 148f) patch = 148f;
            if (patch < 72f) patch = 72f;
            return patch;
        }
    }

    public static class MissionPatchArt
    {
        public const int CampaignCount = 20;
        public const int UiTextureCap = 384;

        static readonly MissionPatchSpec[] Table = BuildTable();

        public static IReadOnlyList<MissionPatchSpec> CampaignPatches => Table;

        public static MissionPatchSpec ForIndex(int index)
        {
            if (index < 1 || index > Table.Length) return null;
            return Table[index - 1];
        }

        public static MissionPatchSpec Resolve(string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId)) return Generated("free", free: true);
            string key = missionId.Trim();
            if (IsFreeId(key)) return Generated(key, free: true);
            var hit = ById(key);
            if (hit != null) return hit;
            if (key.Length > 3 && key.StartsWith("KH-", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(key.Substring(3), NumberStyles.Integer, CultureInfo.InvariantCulture, out int kh))
            {
                var byKh = ForIndex(kh);
                if (byKh != null) return byKh;
            }
            if (int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
            {
                var byN = ForIndex(n);
                if (byN != null) return byN;
            }
            return Generated(key, free: false);
        }

        public static PatchPalette Palette(PatchAct act)
        {
            switch (act)
            {
                case PatchAct.Regulars:
                    return Pal(0x0F, 0x3E, 0x48, 0xE8, 0x5A, 0x2A, 0xF6, 0xF0, 0xE4, 0x7F, 0xCF, 0xC6, 0x10, 0x18, 0x16);
                case PatchAct.Heavy:
                    return Pal(0x2A, 0x24, 0x28, 0xC6, 0xA1, 0x5A, 0xF3, 0xEE, 0xE6, 0xC4, 0x45, 0x36, 0x14, 0x0E, 0x0C);
                case PatchAct.FarShore:
                    return Pal(0x1A, 0x21, 0x48, 0xC9, 0xD2, 0xE6, 0xF7, 0xF4, 0xEC, 0xE6, 0xC5, 0x6A, 0x0C, 0x10, 0x20);
                default:
                    return Pal(0x14, 0x2C, 0x4A, 0xD8, 0x6A, 0x32, 0xF4, 0xE7, 0xCC, 0xF0, 0xC1, 0x4A, 0x1A, 0x12, 0x0C);
            }
        }

        public static string ActLabel(PatchAct act)
        {
            switch (act)
            {
                case PatchAct.Regulars: return "Regulars";
                case PatchAct.Heavy: return "Heavy";
                case PatchAct.FarShore: return "Far Shore";
                default: return "First Light";
            }
        }

        public static PatchLayout Layout(MissionPatchSpec spec, int size, PatchChrome chrome = PatchChrome.Open)
        {
            var plan = PatchPlan.Build(spec ?? Resolve("free"), size);
            return new PatchLayout
            {
                Size = plan.Size,
                NameOnArc = plan.NameOnArc,
                FlightFits = plan.FlightFits,
                EmblemInside = plan.EmblemInside,
                StarsInside = plan.StarsInside,
                StarsSeparated = plan.StarsSeparated,
                StarCount = plan.StarCount,
                GoldRing = chrome == PatchChrome.Medal,
                GreyOutline = chrome == PatchChrome.Locked,
                ArcFontCovers = plan.ArcFontCovers,
            };
        }

        static readonly System.Collections.Generic.Dictionary<string, byte[]> RgbaCache = new System.Collections.Generic.Dictionary<string, byte[]>();
        const int RgbaCacheCap = 96;

        public static byte[] Rgba(string missionId, int size, PatchChrome chrome = PatchChrome.Open)
            => Rgba(Resolve(missionId), size, chrome);

        public static byte[] Rgba(MissionPatchSpec spec, int size, PatchChrome chrome = PatchChrome.Open)
        {
            spec = spec ?? Resolve("free");
            if (size < 16) size = 16;
            if (size > 1024) size = 1024;
            string key = (spec.MissionId ?? spec.Flight ?? "free") + "\n" + size.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n" + ((int)chrome).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (RgbaCache.TryGetValue(key, out var cached) && cached != null) return cached;
            var drawn = MissionPatchRaster.Draw(spec, size, chrome);
            if (RgbaCache.Count >= RgbaCacheCap) RgbaCache.Clear();
            RgbaCache[key] = drawn;
            return drawn;
        }

        public static byte[] Png(string missionId, int size, PatchChrome chrome = PatchChrome.Open)
            => EncodePng(size < 16 ? 16 : size > 1024 ? 1024 : size, Rgba(missionId, size, chrome));

        public static int WriteAll(string directory, int size)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("directory", nameof(directory));
            if (size < 32) size = 32;
            if (size > 1024) size = 1024;
            Directory.CreateDirectory(directory);
            int n = 0;
            for (int i = 0; i < Table.Length; i++)
            {
                var spec = Table[i];
                File.WriteAllBytes(Path.Combine(directory, "kh-" + spec.Index.ToString(CultureInfo.InvariantCulture) + ".png"), Png(spec.MissionId, size, PatchChrome.Open));
                n++;
            }
            File.WriteAllBytes(Path.Combine(directory, "kh-7-medal.png"), Png("m7", size, PatchChrome.Medal));
            File.WriteAllBytes(Path.Combine(directory, "kh-5-locked.png"), Png("m5", size, PatchChrome.Locked));
            File.WriteAllBytes(Path.Combine(directory, "contract.png"), Png("kc-pier-7", size, PatchChrome.Open));
            File.WriteAllBytes(Path.Combine(directory, "free.png"), Png("free", size, PatchChrome.Open));
            return n + 4;
        }

        public static ulong Hash(string text)
        {
            ulong h = 14695981039346656037UL;
            string s = text ?? "";
            for (int i = 0; i < s.Length; i++)
            {
                h ^= s[i];
                h *= 1099511628211UL;
            }
            return h;
        }

        public static ulong PixelHash(byte[] rgba)
        {
            ulong h = 14695981039346656037UL;
            if (rgba == null) return h;
            for (int i = 0; i < rgba.Length; i++)
            {
                h ^= rgba[i];
                h *= 1099511628211UL;
            }
            return h;
        }

        static MissionPatchSpec ById(string id)
        {
            for (int i = 0; i < Table.Length; i++)
                if (string.Equals(Table[i].MissionId, id, StringComparison.OrdinalIgnoreCase)) return Table[i];
            return null;
        }

        static bool IsFreeId(string key)
        {
            if (key.Equals("free", StringComparison.OrdinalIgnoreCase)) return true;
            if (key.Equals("sandbox", StringComparison.OrdinalIgnoreCase)) return true;
            if (key.StartsWith("free-", StringComparison.OrdinalIgnoreCase)) return true;
            if (key.StartsWith("sandbox-", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        static MissionPatchSpec Generated(string id, bool free)
        {
            ulong h = Hash(id);
            bool job = !free && (id.IndexOf("contract", StringComparison.OrdinalIgnoreCase) >= 0
                || id.StartsWith("kc", StringComparison.OrdinalIgnoreCase)
                || id.StartsWith("job", StringComparison.OrdinalIgnoreCase));
            var marks = PatchMarks.Orbit;
            if ((h & 1UL) != 0) marks |= PatchMarks.Rocket;
            if ((h & 2UL) != 0) marks |= PatchMarks.Barge;
            if ((h & 4UL) != 0) marks |= PatchMarks.Moon;
            if ((h & 8UL) != 0) marks |= PatchMarks.ArcTransfer;
            int stars = 1 + (int)(h % 3UL);
            var act = (PatchAct)((h >> 8) % 4UL);
            string adj = Adj[(int)((h >> 16) % (ulong)Adj.Length)];
            string noun = Noun[(int)((h >> 24) % (ulong)Noun.Length)];
            string anatomy = "An orbit ring and " + (stars == 1 ? "one star" : "stars");
            if ((marks & PatchMarks.Barge) != 0) anatomy += ", Haven's barge";
            if ((marks & PatchMarks.Moon) != 0) anatomy += ", a moon";
            if ((marks & PatchMarks.Rocket) != 0) anatomy += ", a booster";
            anatomy += ". The same flight always matches.";
            return new MissionPatchSpec
            {
                Index = 0,
                MissionId = id,
                Flight = free ? "FREE" : job ? "JOB" : "OPEN",
                Name = adj + " " + noun,
                Motto = adj + " " + noun.ToLowerInvariant() + ".",
                Anatomy = anatomy,
                Act = act,
                ActName = ActLabel(act),
                Stars = stars,
                Marks = marks,
                EmblemSeed = h,
            };
        }

        static readonly string[] Adj = { "Steady", "Quiet", "Bright", "Keen", "Fair", "Bold", "Clear", "Swift", "Warm", "True" };
        static readonly string[] Noun = { "Work", "Sky", "Pier", "Deck", "Watch", "Line", "Hold", "Run", "Wake", "Path" };

        static MissionPatchSpec[] BuildTable()
        {
            var rows = new[]
            {
                Row(1, "First Hop", "Hop to it.", "A pebble, a short hop, and the ground. No satellite stars yet.", PatchAct.FirstLight, 0, PatchMarks.Box | PatchMarks.ArcShort | PatchMarks.Horizon),
                Row(2, "Edge of Space", "Hold the lantern high.", "A lantern and a tall arc that stops at the edge of space.", PatchAct.FirstLight, 0, PatchMarks.Lantern | PatchMarks.ArcTall),
                Row(3, "Tiny Orbit", "A mote in a circle.", "One star for Mote, and the first orbit ring.", PatchAct.FirstLight, 1, PatchMarks.Orbit | PatchMarks.Box),
                Row(4, "Weather Eye", "The tern sees the weather.", "A weather eye, one star, and a tilted orbit ring.", PatchAct.FirstLight, 1, PatchMarks.Cloud | PatchMarks.Sun | PatchMarks.Orbit),
                Row(5, "Heavy Lifter", "Heavy, but polite.", "A heavy stack with strap-ons, one star, and an orbit ring.", PatchAct.FirstLight, 1, PatchMarks.Rocket | PatchMarks.Strap | PatchMarks.Orbit),
                Row(6, "Come Back", "Home is a heading.", "The booster turns back to the pad. One star stays in the ring.", PatchAct.Regulars, 1, PatchMarks.Rocket | PatchMarks.ArcReturn | PatchMarks.Horizon),
                Row(7, "Haven at Sea", "Haven holds the deck.", "Haven's barge, a long transfer arc, and one star.", PatchAct.Regulars, 1, PatchMarks.Barge | PatchMarks.ArcTransfer),
                Row(8, "Crew Day", "Soft hands, full nest.", "A crew capsule and its orbit. People, so no satellite stars.", PatchAct.Regulars, 0, PatchMarks.Capsule | PatchMarks.Window | PatchMarks.Orbit),
                Row(9, "Moon Shot", "Lumen, go long.", "A moon, a long coast, and one star for the probe.", PatchAct.FarShore, 1, PatchMarks.Moon | PatchMarks.ArcTransfer | PatchMarks.Probe),
                Row(10, "Big Reuse", "Twins on the turn.", "Two boosters turn for the pad, with one star on a transfer arc.", PatchAct.Regulars, 1, PatchMarks.Rocket | PatchMarks.Strap | PatchMarks.ArcReturn),
                Row(11, "Dawn Ledger", "Dawn keeps the books.", "A dawn sun, one weather star, and a tilted orbit ring.", PatchAct.Regulars, 1, PatchMarks.Sun | PatchMarks.Horizon | PatchMarks.Orbit),
                Row(12, "Glass Meridian", "Keep the glass still.", "A telescope, one star, and a high orbit ring.", PatchAct.Regulars, 1, PatchMarks.Scope | PatchMarks.Orbit),
                Row(13, "Flock Yard", "Open the yard.", "A rack of small satellites, one star each, over Haven's barge.", PatchAct.Regulars, 6, PatchMarks.Rack | PatchMarks.Barge | PatchMarks.Orbit),
                Row(14, "Harbor Trunk", "A full trunk.", "A cargo capsule with strap-ons and an orbit ring. No satellite stars.", PatchAct.Regulars, 0, PatchMarks.Capsule | PatchMarks.Strap | PatchMarks.Orbit),
                Row(15, "Relay West", "Hold the west line.", "A relay dish, one star, a transfer arc, and Haven's barge.", PatchAct.Regulars, 1, PatchMarks.Dish | PatchMarks.Barge | PatchMarks.ArcTransfer),
                Row(16, "Second Watch", "Same care, second watch.", "A crew capsule on second watch, and Haven's barge waiting.", PatchAct.Heavy, 0, PatchMarks.Capsule | PatchMarks.Window | PatchMarks.Barge),
                Row(17, "Lumen Down", "Legs down.", "A moon and a lander with legs. One star for the craft.", PatchAct.FarShore, 1, PatchMarks.Moon | PatchMarks.Legs),
                Row(18, "Nightjar", "Quiet on the night side.", "A dark moon, a quiet probe, and a long coast arc.", PatchAct.FarShore, 1, PatchMarks.Moon | PatchMarks.ArcTall | PatchMarks.Coast | PatchMarks.Probe),
                Row(19, "Keel Reuse", "The keel comes home.", "A flown booster returns to the pad, with one star on the transfer.", PatchAct.Heavy, 1, PatchMarks.Rocket | PatchMarks.ArcReturn | PatchMarks.Strap | PatchMarks.Horizon),
                Row(20, "Far Hold", "Hold the far side.", "A moon, a wide hold, and one star far from the pier.", PatchAct.FarShore, 1, PatchMarks.Moon | PatchMarks.Orbit | PatchMarks.Coast),
            };
            return rows;
        }

        static MissionPatchSpec Row(int index, string name, string motto, string anatomy, PatchAct act, int stars, PatchMarks marks)
        {
            return new MissionPatchSpec
            {
                Index = index,
                MissionId = "m" + index.ToString(CultureInfo.InvariantCulture),
                Flight = "KH-" + index.ToString(CultureInfo.InvariantCulture),
                Name = name,
                Motto = motto,
                Anatomy = anatomy,
                Act = act,
                ActName = ActLabel(act),
                Stars = stars,
                Marks = marks,
                EmblemSeed = Hash(((uint)marks).ToString(CultureInfo.InvariantCulture) + "|" + stars.ToString(CultureInfo.InvariantCulture) + "|" + ((int)act).ToString(CultureInfo.InvariantCulture)),
            };
        }

        static PatchPalette Pal(byte fr, byte fg, byte fb, byte rr, byte rg, byte rb, byte tr, byte tg, byte tb, byte ar, byte ag, byte ab, byte ir, byte ig, byte ib)
        {
            return new PatchPalette
            {
                FR = fr, FG = fg, FB = fb, RR = rr, RG = rg, RB = rb, TR = tr, TG = tg, TB = tb,
                AR = ar, AG = ag, AB = ab, IR = ir, IG = ig, IB = ib,
            };
        }

        public static byte[] EncodePng(int size, byte[] rgba)
        {
            int stride = size * 4;
            var raw = new byte[(stride + 1) * size];
            for (int y = 0; y < size; y++)
            {
                int row = y * (stride + 1);
                raw[row] = 0;
                Buffer.BlockCopy(rgba, y * stride, raw, row + 1, stride);
            }
            var zlib = Zlib(raw);
            var png = new byte[8 + 12 + 13 + 12 + zlib.Length + 12];
            byte[] sig = { 137, 80, 78, 71, 13, 10, 26, 10 };
            Buffer.BlockCopy(sig, 0, png, 0, 8);
            int o = 8;
            o = Chunk(png, o, "IHDR", Ihdr(size, size));
            o = Chunk(png, o, "IDAT", zlib);
            o = Chunk(png, o, "IEND", new byte[0]);
            if (o != png.Length) Array.Resize(ref png, o);
            return png;
        }

        static byte[] Ihdr(int w, int h)
        {
            var b = new byte[13];
            Be(b, 0, w);
            Be(b, 4, h);
            b[8] = 8;
            b[9] = 6;
            return b;
        }

        static int Chunk(byte[] png, int o, string type, byte[] data)
        {
            Be(png, o, data.Length);
            png[o + 4] = (byte)type[0];
            png[o + 5] = (byte)type[1];
            png[o + 6] = (byte)type[2];
            png[o + 7] = (byte)type[3];
            Buffer.BlockCopy(data, 0, png, o + 8, data.Length);
            uint crc = Crc(png, o + 4, 4 + data.Length);
            Be(png, o + 8 + data.Length, crc);
            return o + 12 + data.Length;
        }

        static void Be(byte[] b, int o, int v)
        {
            b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v;
        }

        static void Be(byte[] b, int o, uint v) => Be(b, o, unchecked((int)v));

        static byte[] Zlib(byte[] raw)
        {
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0x78);
                ms.WriteByte(0x9C);
                using (var def = new DeflateStream(ms, CompressionLevel.Fastest, true))
                    def.Write(raw, 0, raw.Length);
                uint a = Adler(raw);
                ms.WriteByte((byte)(a >> 24));
                ms.WriteByte((byte)(a >> 16));
                ms.WriteByte((byte)(a >> 8));
                ms.WriteByte((byte)a);
                return ms.ToArray();
            }
        }

        static uint Adler(byte[] data)
        {
            uint s1 = 1, s2 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                s1 = (s1 + data[i]) % 65521;
                s2 = (s2 + s1) % 65521;
            }
            return (s2 << 16) | s1;
        }

        static uint Crc(byte[] data, int start, int len)
        {
            uint c = 0xFFFFFFFF;
            for (int i = 0; i < len; i++)
            {
                c ^= data[start + i];
                for (int k = 0; k < 8; k++)
                    c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            }
            return c ^ 0xFFFFFFFF;
        }
    }

    sealed class PatchDrawPlan
    {
        public int Size;
        public float Cx, Cy, Outer, Field, Rim, TextR;
        public int NameScale;
        public bool NameOnArc;
        public float NameSpan;
        public int FlightScale;
        public float FlightY;
        public bool FlightFits;
        public bool ArcFontCovers;
        public float EmblemCx, EmblemCy, EmblemR;
        public bool EmblemInside;
        public float[] StarX = new float[8];
        public float[] StarY = new float[8];
        public float StarR;
        public int StarCount;
        public bool StarsInside;
        public bool StarsSeparated;
    }

    static class PatchPlan
    {
        public const float ArcMaxDeg = 168f;

        public static PatchDrawPlan Build(MissionPatchSpec spec, int size)
        {
            if (size < 16) size = 16;
            if (size > 1024) size = 1024;
            var plan = new PatchDrawPlan { Size = size, StarCount = spec.Stars < 0 ? 0 : spec.Stars > 8 ? 8 : spec.Stars };
            plan.Cx = (size - 1) * 0.5f;
            plan.Cy = plan.Cx;
            plan.Outer = size * 0.5f - 1.25f;
            plan.Rim = plan.Outer * 0.175f;
            if (plan.Rim < 8f) plan.Rim = 8f;
            if (plan.Rim > plan.Outer * 0.46f) plan.Rim = plan.Outer * 0.46f;
            plan.Field = plan.Outer - plan.Rim;
            plan.TextR = plan.Field + plan.Rim * 0.40f;

            string arc = spec.ArcName ?? "";
            string flight = spec.Flight ?? "";
            plan.ArcFontCovers = MissionPatchFont.Covers(arc) && MissionPatchFont.Covers(flight);
            plan.NameScale = FitArcScale(arc, plan.Rim, plan.TextR, out plan.NameSpan);
            plan.NameOnArc = plan.NameScale > 0;
            plan.FlightScale = FitFlight(flight, plan, out plan.FlightY);
            plan.FlightFits = plan.FlightScale > 0;

            float field = plan.Field;
            plan.EmblemR = field * (plan.StarCount > 1 ? 0.34f : 0.42f);
            plan.EmblemCx = plan.Cx;
            plan.EmblemCy = plan.Cy - (plan.StarCount > 1 ? field * 0.06f : 0f);
            if ((spec.Marks & PatchMarks.Barge) != 0) plan.EmblemCy -= field * 0.04f;
            plan.EmblemInside = Dist(plan.EmblemCx, plan.EmblemCy, plan.Cx, plan.Cy) + plan.EmblemR <= field * 0.98f;

            PlaceStars(plan);
            return plan;
        }

        static int FitArcScale(string text, float rim, float textR, out float spanDeg)
        {
            spanDeg = 0f;
            if (string.IsNullOrEmpty(text) || textR < 4f) return 0;
            int max = MaxScale(rim);
            for (int s = max; s >= 1; s--)
            {
                if (7f * s > rim + 1.5f) continue;
                float width = text.Length * 6f * s;
                float deg = (float)(width / textR * 180.0 / Math.PI);
                if (deg <= ArcMaxDeg)
                {
                    spanDeg = deg < 36f ? 36f : deg;
                    return s;
                }
            }
            return 0;
        }

        static int FitFlight(string text, PatchDrawPlan plan, out float y)
        {
            y = plan.Cy;
            if (string.IsNullOrEmpty(text)) return 0;
            int max = MaxScale(plan.Rim);
            for (int s = max; s >= 1; s--)
            {
                float width = (text.Length - 1) * 6f * s + 5f * s;
                float half = width * 0.5f;
                float yTry = plan.Cy + plan.Field + plan.Rim * 0.28f;
                if (Inside(plan, half, yTry, s)) { y = yTry; return s; }
                yTry = plan.Cy + plan.Field * 0.62f;
                if (Inside(plan, half, yTry, s)) { y = yTry; return s; }
            }
            return 0;
        }

        static bool Inside(PatchDrawPlan plan, float half, float y, int scale)
        {
            float dx = half;
            float dy = Math.Abs(y - plan.Cy) + 3.6f * scale;
            return Math.Sqrt(dx * dx + dy * dy) <= plan.Outer - 0.75f;
        }

        static int MaxScale(float rim)
        {
            if (rim >= 36f) return 4;
            if (rim >= 26f) return 3;
            if (rim >= 16f) return 2;
            return 1;
        }

        static void PlaceStars(PatchDrawPlan plan)
        {
            int n = plan.StarCount;
            plan.StarsInside = true;
            plan.StarsSeparated = true;
            if (n == 0) { plan.StarR = 0; return; }
            float field = plan.Field;
            plan.StarR = field * (n >= 6 ? 0.065f : 0.085f);
            if (n == 1)
            {
                float ang = -42f * (float)(Math.PI / 180.0);
                float rad = field * 0.62f;
                plan.StarX[0] = plan.Cx + (float)Math.Sin(ang) * rad;
                plan.StarY[0] = plan.Cy - (float)Math.Cos(ang) * rad;
            }
            else
            {
                float span = n >= 6 ? 130f : 96f;
                float rad = field * 0.66f;
                for (int i = 0; i < n; i++)
                {
                    float t = n == 1 ? 0f : (i / (float)(n - 1) - 0.5f) * span;
                    float ang = (180f + t) * (float)(Math.PI / 180.0);
                    plan.StarX[i] = plan.Cx + (float)Math.Sin(ang) * rad;
                    plan.StarY[i] = plan.Cy - (float)Math.Cos(ang) * rad;
                }
            }
            for (int guard = 0; guard < 6; guard++)
            {
                bool ok = true;
                for (int i = 0; i < n; i++)
                {
                    if (Dist(plan.StarX[i], plan.StarY[i], plan.Cx, plan.Cy) + plan.StarR > field * 0.96f) ok = false;
                    if (Dist(plan.StarX[i], plan.StarY[i], plan.EmblemCx, plan.EmblemCy) < plan.EmblemR * 0.92f + plan.StarR) ok = false;
                    for (int j = i + 1; j < n; j++)
                        if (Dist(plan.StarX[i], plan.StarY[i], plan.StarX[j], plan.StarY[j]) < plan.StarR * 2.15f) ok = false;
                }
                plan.StarsInside = true;
                plan.StarsSeparated = true;
                if (ok) return;
                plan.StarR *= 0.86f;
                if (n > 1)
                {
                    float span = n >= 6 ? 148f : 110f;
                    float rad = field * (0.70f + guard * 0.01f);
                    for (int i = 0; i < n; i++)
                    {
                        float t = (i / (float)(n - 1) - 0.5f) * span;
                        float ang = (180f + t) * (float)(Math.PI / 180.0);
                        plan.StarX[i] = plan.Cx + (float)Math.Sin(ang) * rad;
                        plan.StarY[i] = plan.Cy - (float)Math.Cos(ang) * rad;
                    }
                }
            }
            for (int i = 0; i < n; i++)
            {
                if (Dist(plan.StarX[i], plan.StarY[i], plan.Cx, plan.Cy) + plan.StarR > field + 0.5f) plan.StarsInside = false;
                for (int j = i + 1; j < n; j++)
                    if (Dist(plan.StarX[i], plan.StarY[i], plan.StarX[j], plan.StarY[j]) < plan.StarR * 1.7f) plan.StarsSeparated = false;
            }
        }

        static float Dist(float x0, float y0, float x1, float y1)
        {
            float dx = x0 - x1, dy = y0 - y1;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
