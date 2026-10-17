// Orthographic shade of a PayloadForms list. Same boxes the URP mesh builds.
// Transparent ground, so a card can sit the picture on navy. No textures and no paid art.
using System;
using System.IO.Compression;

namespace Kestrel.Core.Flight
{
    public static class PayloadPortrait
    {
        public const int CardSize = 128;

        public static byte[] Rgba(string form, int size = CardSize)
        {
            if (size < 16) size = 16;
            if (size > 512) size = 512;
            var parts = PayloadForms.Of(form);
            var pix = new byte[size * size * 4];
            var depth = new float[size * size];
            for (int i = 0; i < depth.Length; i++) depth[i] = float.NegativeInfinity;

            var eye = Norm(new Vec(0.82f, 0.48f, 0.72f));
            var worldUp = new Vec(0, 1, 0);
            var right = Norm(Cross(eye, worldUp));
            var up = Norm(Cross(right, eye));
            var light = Norm(new Vec(0.32f, 0.9f, 0.28f));

            int n = parts.Length;
            var proj = new Vec[n * 8];
            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
            for (int p = 0; p < n; p++)
            {
                Corners(parts[p], eye, right, up, proj, p * 8, out float x0, out float y0, out float x1, out float y1);
                if (x0 < minX) minX = x0;
                if (y0 < minY) minY = y0;
                if (x1 > maxX) maxX = x1;
                if (y1 > maxY) maxY = y1;
            }
            float span = Math.Max(maxX - minX, maxY - minY);
            if (!(span > 1e-4f)) span = 1;
            float fit = size * 0.78f / span;
            float cx = (minX + maxX) * 0.5f;
            float cy = (minY + maxY) * 0.5f;

            var faces = new Face[n * 6];
            int fc = 0;
            for (int p = 0; p < n; p++)
            {
                var part = parts[p];
                int b = p * 8;
                // Corner order is iz, iy, ix: 0 -x-y-z, 1 +x-y-z, 2 -x+y-z, 3 +x+y-z, 4 -x-y+z, 5 +x-y+z, 6 -x+y+z, 7 +x+y+z.
                AddFace(faces, ref fc, proj, b, 2, 3, 7, 6, part, light, new Vec(0, 1, 0));
                AddFace(faces, ref fc, proj, b, 0, 4, 5, 1, part, light, new Vec(0, -1, 0));
                AddFace(faces, ref fc, proj, b, 1, 3, 7, 5, part, light, new Vec(1, 0, 0));
                AddFace(faces, ref fc, proj, b, 0, 2, 6, 4, part, light, new Vec(-1, 0, 0));
                AddFace(faces, ref fc, proj, b, 4, 5, 7, 6, part, light, new Vec(0, 0, 1));
                AddFace(faces, ref fc, proj, b, 0, 1, 3, 2, part, light, new Vec(0, 0, -1));
            }
            Array.Sort(faces, 0, fc, FaceOrder.FarFirst);

            for (int i = 0; i < fc; i++)
            {
                var f = faces[i];
                if (f.Shade < 0.02f) continue;
                var a = Screen(proj[f.A], cx, cy, fit, size);
                var b2 = Screen(proj[f.B], cx, cy, fit, size);
                var c = Screen(proj[f.C], cx, cy, fit, size);
                var d = Screen(proj[f.D], cx, cy, fit, size);
                Tri(pix, depth, size, a, b2, c, f.Z, f.Cr, f.Cg, f.Cb);
                Tri(pix, depth, size, a, c, d, f.Z, f.Cr, f.Cg, f.Cb);
            }
            Ink(pix, size);
            return pix;
        }

        public static byte[] Png(string form, int size = CardSize) => EncodePng(size, size, Rgba(form, size));

        public static byte[] EncodePng(int w, int h, byte[] rgba)
        {
            int stride = w * 4;
            var raw = new byte[(stride + 1) * h];
            for (int y = 0; y < h; y++)
            {
                int row = y * (stride + 1);
                raw[row] = 0;
                Buffer.BlockCopy(rgba, y * stride, raw, row + 1, stride);
            }
            var zlib = Zlib(raw);
            var png = new byte[8 + 12 + 13 + 12 + zlib.Length + 12];
            int o = 0;
            byte[] sig = { 137, 80, 78, 71, 13, 10, 26, 10 };
            Buffer.BlockCopy(sig, 0, png, 0, 8);
            o = 8;
            o = Chunk(png, o, "IHDR", Ihdr(w, h));
            o = Chunk(png, o, "IDAT", zlib);
            o = Chunk(png, o, "IEND", new byte[0]);
            if (o != png.Length) Array.Resize(ref png, o);
            return png;
        }

        struct Face
        {
            public int A, B, C, D;
            public float Z, Shade;
            public byte Cr, Cg, Cb;
        }

        sealed class FaceOrder : System.Collections.Generic.IComparer<Face>
        {
            public static readonly FaceOrder FarFirst = new FaceOrder();
            public int Compare(Face x, Face y) => x.Z.CompareTo(y.Z);
        }

        struct Vec
        {
            public float X, Y, Z;
            public Vec(float x, float y, float z) { X = x; Y = y; Z = z; }
        }

        static void Corners(PayloadPart p, Vec eye, Vec right, Vec up, Vec[] proj, int at, out float minX, out float minY, out float maxX, out float maxY)
        {
            float hx = p.W * 0.5f, hy = p.H * 0.5f, hz = p.D * 0.5f;
            float[] s = { -1, 1 };
            minX = float.PositiveInfinity; minY = minX; maxX = float.NegativeInfinity; maxY = maxX;
            int k = 0;
            for (int iz = 0; iz < 2; iz++)
            for (int iy = 0; iy < 2; iy++)
            for (int ix = 0; ix < 2; ix++)
            {
                var w = new Vec(p.X + s[ix] * hx, p.Y + s[iy] * hy, p.Z + s[iz] * hz);
                float sx = Dot(w, right);
                float sy = Dot(w, up);
                float sz = Dot(w, eye);
                proj[at + k] = new Vec(sx, sy, sz);
                if (sx < minX) minX = sx;
                if (sy < minY) minY = sy;
                if (sx > maxX) maxX = sx;
                if (sy > maxY) maxY = sy;
                k++;
            }
        }

        static void AddFace(Face[] faces, ref int n, Vec[] proj, int b, int a, int c, int d, int e, PayloadPart part, Vec light, Vec normal)
        {
            float shade = 0.22f + 0.78f * Math.Max(0f, Dot(normal, light));
            byte r = (byte)Math.Min(255, part.R * shade);
            byte g = (byte)Math.Min(255, part.G * shade);
            byte bl = (byte)Math.Min(255, part.B * shade);
            float z = (proj[b + a].Z + proj[b + c].Z + proj[b + d].Z + proj[b + e].Z) * 0.25f;
            faces[n++] = new Face { A = b + a, B = b + c, C = b + d, D = b + e, Z = z, Shade = shade, Cr = r, Cg = g, Cb = bl };
        }

        static Vec Screen(Vec p, float cx, float cy, float fit, int size)
        {
            float x = (p.X - cx) * fit + size * 0.5f;
            float y = (cy - p.Y) * fit + size * 0.5f;
            return new Vec(x, y, p.Z);
        }

        static void Tri(byte[] pix, float[] depth, int size, Vec a, Vec b, Vec c, float z, byte r, byte g, byte bl)
        {
            int x0 = (int)Math.Floor(Math.Min(a.X, Math.Min(b.X, c.X)));
            int y0 = (int)Math.Floor(Math.Min(a.Y, Math.Min(b.Y, c.Y)));
            int x1 = (int)Math.Ceiling(Math.Max(a.X, Math.Max(b.X, c.X)));
            int y1 = (int)Math.Ceiling(Math.Max(a.Y, Math.Max(b.Y, c.Y)));
            if (x0 < 0) x0 = 0;
            if (y0 < 0) y0 = 0;
            if (x1 >= size) x1 = size - 1;
            if (y1 >= size) y1 = size - 1;
            float area = Edge(b, c, a);
            if (Math.Abs(area) < 1e-4f) return;
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var p = new Vec(x + 0.5f, y + 0.5f, 0);
                float w0 = Edge(b, c, p) / area;
                float w1 = Edge(c, a, p) / area;
                float w2 = Edge(a, b, p) / area;
                if (w0 < -0.001f || w1 < -0.001f || w2 < -0.001f) continue;
                int i = y * size + x;
                if (z <= depth[i]) continue;
                depth[i] = z;
                int o = i * 4;
                pix[o] = r; pix[o + 1] = g; pix[o + 2] = bl; pix[o + 3] = 255;
            }
        }

        static float Edge(Vec a, Vec b, Vec p) => (p.X - a.X) * (b.Y - a.Y) - (p.Y - a.Y) * (b.X - a.X);

        static void Ink(byte[] pix, int size)
        {
            var src = (byte[])pix.Clone();
            for (int y = 1; y < size - 1; y++)
            for (int x = 1; x < size - 1; x++)
            {
                int o = (y * size + x) * 4;
                if (src[o + 3] == 0) continue;
                bool edge = src[((y - 1) * size + x) * 4 + 3] == 0
                    || src[((y + 1) * size + x) * 4 + 3] == 0
                    || src[(y * size + x - 1) * 4 + 3] == 0
                    || src[(y * size + x + 1) * 4 + 3] == 0;
                if (!edge) continue;
                pix[o] = (byte)(pix[o] * 0.35f);
                pix[o + 1] = (byte)(pix[o + 1] * 0.35f);
                pix[o + 2] = (byte)(pix[o + 2] * 0.35f);
            }
        }

        static float Dot(Vec a, Vec b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        static Vec Cross(Vec a, Vec b) => new Vec(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

        static Vec Norm(Vec v)
        {
            float m = (float)Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
            if (m < 1e-6f) return new Vec(0, 0, 1);
            return new Vec(v.X / m, v.Y / m, v.Z / m);
        }

        static byte[] Ihdr(int w, int h)
        {
            var b = new byte[13];
            Be(b, 0, w);
            Be(b, 4, h);
            b[8] = 8; b[9] = 6;
            return b;
        }

        static int Chunk(byte[] png, int o, string type, byte[] data)
        {
            Be(png, o, data.Length);
            png[o + 4] = (byte)type[0]; png[o + 5] = (byte)type[1]; png[o + 6] = (byte)type[2]; png[o + 7] = (byte)type[3];
            Buffer.BlockCopy(data, 0, png, o + 8, data.Length);
            uint crc = Crc(png, o + 4, 4 + data.Length);
            Be(png, o + 8 + data.Length, (int)crc);
            return o + 12 + data.Length;
        }

        static void Be(byte[] b, int o, int v)
        {
            b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v;
        }

        static byte[] Zlib(byte[] raw)
        {
            using (var ms = new System.IO.MemoryStream())
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
}
