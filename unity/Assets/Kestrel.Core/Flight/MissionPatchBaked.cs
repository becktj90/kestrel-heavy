// Baked KH-1..KH-20 badges. Files live in Resources/MissionBadges and are read from the
// checkout when one is available. The player loads the same art with Resources.Load.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;

namespace Kestrel.Core.Flight
{
    static class MissionPatchBaked
    {
        static readonly Dictionary<string, byte[]> Decoded = new Dictionary<string, byte[]>();
        static readonly Dictionary<string, int> DecodedW = new Dictionary<string, int>();

        public static void Clear()
        {
            Decoded.Clear();
            DecodedW.Clear();
        }

        public static bool Has(int index)
        {
            string dir = DirectoryOrNull();
            if (dir == null || index < 1 || index > MissionPatchArt.CampaignCount) return false;
            return File.Exists(Path.Combine(dir, FileName(index, PatchChrome.Open)));
        }

        public static byte[] Read(int index)
        {
            byte[] rgba = Load(index, PatchChrome.Open, out int w, out int h, out _);
            if (rgba == null || w != h) return null;
            return rgba;
        }

        public static byte[] Rgba(int index, int size, PatchChrome chrome)
        {
            byte[] rgba = Load(index, chrome, out int w, out int h, out bool treated);
            if (rgba == null) return null;
            return Present(rgba, w, h, size, treated ? PatchChrome.Open : chrome);
        }

        public static byte[] Present(byte[] rgba, int width, int height, int size, PatchChrome chrome)
        {
            if (rgba == null || width < 1 || height < 1 || rgba.Length < width * height * 4) return null;
            if (size < 16) size = 16;
            if (size > 1024) size = 1024;
            byte[] scaled = Scale(rgba, width, height, size);
            if (chrome == PatchChrome.Medal) PatchPlan.PaintGoldBorder(scaled, size);
            else if (chrome == PatchChrome.Locked) PatchPlan.PaintLocked(scaled, size);
            return scaled;
        }

        public static string ResourcePath(int index, PatchChrome chrome)
        {
            string stem = "MissionBadges/kh-" + index.ToString(CultureInfo.InvariantCulture);
            if (chrome == PatchChrome.Medal) return stem + "-medal";
            if (chrome == PatchChrome.Locked) return stem + "-locked";
            return stem;
        }

        static byte[] Load(int index, PatchChrome chrome, out int w, out int h, out bool treated)
        {
            w = 0;
            h = 0;
            treated = false;
            string dir = DirectoryOrNull();
            if (dir == null || index < 1 || index > MissionPatchArt.CampaignCount) return null;
            if (chrome != PatchChrome.Open)
            {
                string variant = Path.Combine(dir, FileName(index, chrome));
                if (File.Exists(variant))
                {
                    treated = true;
                    return Decode(variant, out w, out h);
                }
            }
            string path = Path.Combine(dir, FileName(index, PatchChrome.Open));
            if (!File.Exists(path)) return null;
            return Decode(path, out w, out h);
        }

        static string FileName(int index, PatchChrome chrome)
        {
            string stem = "kh-" + index.ToString(CultureInfo.InvariantCulture);
            if (chrome == PatchChrome.Medal) return stem + "-medal.png";
            if (chrome == PatchChrome.Locked) return stem + "-locked.png";
            return stem + ".png";
        }

        static string DirectoryOrNull()
        {
            string root = Data.Catalog.RepoRootOrNull();
            if (string.IsNullOrEmpty(root)) return null;
            string dir = Path.Combine(root, "unity", "Assets", "Resources", "MissionBadges");
            return Directory.Exists(dir) ? dir : null;
        }

        static byte[] Decode(string path, out int w, out int h)
        {
            w = 0;
            h = 0;
            if (Decoded.TryGetValue(path, out byte[] cached))
            {
                w = h = DecodedW[path];
                return cached;
            }
            byte[] file;
            try { file = File.ReadAllBytes(path); }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
            if (!TryDecode(file, out byte[] rgba, out w, out h)) return null;
            Decoded[path] = rgba;
            DecodedW[path] = w;
            return rgba;
        }

        static bool TryDecode(byte[] data, out byte[] rgba, out int w, out int h)
        {
            rgba = null;
            w = 0;
            h = 0;
            if (data == null || data.Length < 8) return false;
            if (data[0] != 137 || data[1] != 80 || data[2] != 78 || data[3] != 71) return false;
            int pos = 8;
            int color = -1;
            int bit = 0;
            int interlace = 0;
            var idat = new MemoryStream();
            while (pos + 8 <= data.Length)
            {
                int len = Be(data, pos);
                if (len < 0 || pos + 12 + len > data.Length) return false;
                string type = "" + (char)data[pos + 4] + (char)data[pos + 5] + (char)data[pos + 6] + (char)data[pos + 7];
                int start = pos + 8;
                if (type == "IHDR")
                {
                    if (len < 13) return false;
                    w = Be(data, start);
                    h = Be(data, start + 4);
                    bit = data[start + 8];
                    color = data[start + 9];
                    interlace = data[start + 12];
                }
                else if (type == "IDAT")
                {
                    idat.Write(data, start, len);
                }
                else if (type == "IEND")
                {
                    break;
                }
                pos += 12 + len;
            }
            if (w < 1 || h < 1 || w > 4096 || h > 4096 || bit != 8 || interlace != 0) return false;
            int bpp = color == 6 ? 4 : color == 2 ? 3 : 0;
            if (bpp == 0) return false;
            byte[] raw;
            try { raw = Inflate(idat.ToArray()); }
            catch (InvalidDataException) { return false; }
            int stride = w * bpp;
            if (raw == null || raw.Length < (stride + 1) * h) return false;
            rgba = new byte[w * h * 4];
            var prev = new byte[stride];
            var row = new byte[stride];
            int i = 0;
            for (int y = 0; y < h; y++)
            {
                int filt = raw[i++];
                Buffer.BlockCopy(raw, i, row, 0, stride);
                i += stride;
                if (!Unfilter(filt, row, prev, bpp)) return false;
                int dst = y * w * 4;
                if (bpp == 4)
                {
                    Buffer.BlockCopy(row, 0, rgba, dst, stride);
                }
                else
                {
                    for (int x = 0; x < w; x++)
                    {
                        rgba[dst++] = row[x * 3];
                        rgba[dst++] = row[x * 3 + 1];
                        rgba[dst++] = row[x * 3 + 2];
                        rgba[dst++] = 255;
                    }
                }
                var swap = prev;
                prev = row;
                row = swap;
            }
            return true;
        }

        static bool Unfilter(int filt, byte[] row, byte[] prev, int bpp)
        {
            int stride = row.Length;
            if (filt == 0) return true;
            if (filt == 1)
            {
                for (int x = 0; x < stride; x++)
                {
                    byte left = x >= bpp ? row[x - bpp] : (byte)0;
                    row[x] = (byte)(row[x] + left);
                }
                return true;
            }
            if (filt == 2)
            {
                for (int x = 0; x < stride; x++) row[x] = (byte)(row[x] + prev[x]);
                return true;
            }
            if (filt == 3)
            {
                for (int x = 0; x < stride; x++)
                {
                    byte left = x >= bpp ? row[x - bpp] : (byte)0;
                    row[x] = (byte)(row[x] + ((left + prev[x]) >> 1));
                }
                return true;
            }
            if (filt == 4)
            {
                for (int x = 0; x < stride; x++)
                {
                    byte a = x >= bpp ? row[x - bpp] : (byte)0;
                    byte b = prev[x];
                    byte c = x >= bpp ? prev[x - bpp] : (byte)0;
                    row[x] = (byte)(row[x] + Paeth(a, b, c));
                }
                return true;
            }
            return false;
        }

        static byte Paeth(byte a, byte b, byte c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a);
            int pb = Math.Abs(p - b);
            int pc = Math.Abs(p - c);
            if (pa <= pb && pa <= pc) return a;
            if (pb <= pc) return b;
            return c;
        }

        static int Be(byte[] data, int o)
        {
            return (data[o] << 24) | (data[o + 1] << 16) | (data[o + 2] << 8) | data[o + 3];
        }

        static byte[] Inflate(byte[] zlib)
        {
            if (zlib == null || zlib.Length < 6) throw new InvalidDataException("zlib");
            using (var ms = new MemoryStream(zlib, 2, zlib.Length - 6))
            using (var def = new DeflateStream(ms, CompressionMode.Decompress))
            using (var raw = new MemoryStream())
            {
                def.CopyTo(raw);
                return raw.ToArray();
            }
        }

        public static byte[] Scale(byte[] src, int sw, int sh, int size)
        {
            if (sw == size && sh == size)
            {
                var copy = new byte[sw * sh * 4];
                Buffer.BlockCopy(src, 0, copy, 0, copy.Length);
                return copy;
            }
            var dst = new byte[size * size * 4];
            for (int y = 0; y < size; y++)
            {
                int y0 = (int)((long)y * sh / size);
                int y1 = (int)((long)(y + 1) * sh / size);
                if (y1 <= y0) y1 = Math.Min(sh, y0 + 1);
                for (int x = 0; x < size; x++)
                {
                    int x0 = (int)((long)x * sw / size);
                    int x1 = (int)((long)(x + 1) * sw / size);
                    if (x1 <= x0) x1 = Math.Min(sw, x0 + 1);
                    int r = 0, g = 0, b = 0, a = 0, n = 0;
                    for (int yy = y0; yy < y1; yy++)
                    {
                        int row = yy * sw * 4;
                        for (int xx = x0; xx < x1; xx++)
                        {
                            int i = row + xx * 4;
                            r += src[i];
                            g += src[i + 1];
                            b += src[i + 2];
                            a += src[i + 3];
                            n++;
                        }
                    }
                    int o = (y * size + x) * 4;
                    dst[o] = (byte)(r / n);
                    dst[o + 1] = (byte)(g / n);
                    dst[o + 2] = (byte)(b / n);
                    dst[o + 3] = (byte)(a / n);
                }
            }
            return dst;
        }
    }
}
