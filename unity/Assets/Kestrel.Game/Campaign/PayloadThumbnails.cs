// t461 "Improve rendering of payload": the mission card and map nodes show a lit 3D render of each payload form
// (gold MLI with a crinkle normal, blue solar cell grids, dishes and whips, a detailed bus; a warm key and a cool rim
// on the dark-blue Dusk backdrop). PayloadRender shades it on the CPU in pure C# (no camera, no shader variants, so it
// is the same in the editor, on iOS and on WebGL). Here the pixels become a RenderTexture at the element's physical
// size (points x panel DPR) so the art is crisp at 2x and 3x, cached per form, size and backdrop, and re-rendered only
// when one of those changes. Renders are time-sliced (a few ms a frame) so a board of twenty nodes never drops a frame.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Kestrel.Core.Flight;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.MissionSelect
{
    public static class PayloadThumbnails
    {
        /// <summary>Per-frame CPU budget for pending renders.</summary>
        public const double FrameBudgetMs = 4.0;

        /// <summary>Rows shaded between budget checks.</summary>
        const int RowsPerStep = 4;

        /// <summary>Most cached textures kept; the least recently used is released past this.</summary>
        public const int MaxCached = 64;

        struct Key : IEquatable<Key>
        {
            public string Form;
            public int Px;
            public PayloadBackdrop Backdrop;
            public bool Equals(Key o) => Px == o.Px && Backdrop == o.Backdrop && string.Equals(Form, o.Form, StringComparison.Ordinal);
            public override bool Equals(object o) => o is Key k && Equals(k);
            public override int GetHashCode() => ((Form ?? "").GetHashCode() * 397 ^ Px) * 31 ^ (int)Backdrop;
        }

        static readonly Dictionary<Key, Texture> Cache = new Dictionary<Key, Texture>();
        static readonly LinkedList<Key> Lru = new LinkedList<Key>();
        static readonly List<PayloadRender.Job> Pending = new List<PayloadRender.Job>();
        static readonly List<Key> PendingKeys = new List<Key>();
        static readonly List<WeakReference<PayloadThumb>> Waiting = new List<WeakReference<PayloadThumb>>();
        static readonly Stopwatch Clock = new Stopwatch();

        /// <summary>Completed renders since load (a test hook: an unchanged form and size must not render again).</summary>
        public static int Renders { get; private set; }

        public static int PendingCount => Pending.Count;

        /// <summary>Physical pixel size for <paramref name="points"/> on a panel scaled by <paramref name="dpr"/>.</summary>
        public static int PixelsFor(float points, float dpr)
        {
            if (float.IsNaN(points) || points <= 0f) points = 32f;
            if (float.IsNaN(dpr) || dpr <= 0f) dpr = 1f;
            int px = (int)Math.Ceiling(points * dpr - 0.01f);
            return Math.Max(PayloadRender.MinPx, Math.Min(PayloadRender.MaxPx, px));
        }

        static Key KeyOf(string form, int px, PayloadBackdrop backdrop) =>
            new Key { Form = string.IsNullOrEmpty(form) ? "box" : form, Px = Math.Max(PayloadRender.MinPx, Math.Min(PayloadRender.MaxPx, px)), Backdrop = backdrop };

        /// <summary>The cached render, or null (and a render is queued) when it is not ready.</summary>
        public static Texture Get(string form, int px, PayloadBackdrop backdrop, bool urgent = false)
        {
            var key = KeyOf(form, px, backdrop);
            if (TryCached(key, out var tex)) return tex;
            Queue(key, urgent);
            return null;
        }

        /// <summary>Renders now (tests, captures). Returns the cached texture.</summary>
        public static Texture RenderNow(string form, int px, PayloadBackdrop backdrop)
        {
            var key = KeyOf(form, px, backdrop);
            if (TryCached(key, out var tex)) return tex;
            int i = PendingKeys.IndexOf(key);
            PayloadRender.Job job;
            if (i >= 0)
            {
                job = Pending[i];
                Pending.RemoveAt(i);
                PendingKeys.RemoveAt(i);
            }
            else job = PayloadRender.Begin(key.Form, key.Px, key.Backdrop);
            while (!job.Step(64)) { }
            return Store(key, job);
        }

        /// <summary>Any cached render of <paramref name="form"/> on <paramref name="backdrop"/>, the size nearest
        /// <paramref name="px"/> (shown scaled while the exact size renders).</summary>
        public static Texture Nearest(string form, int px, PayloadBackdrop backdrop)
        {
            string f = string.IsNullOrEmpty(form) ? "box" : form;
            Texture best = null;
            int gap = int.MaxValue;
            foreach (var kv in Cache)
            {
                if (kv.Key.Backdrop != backdrop || !string.Equals(kv.Key.Form, f, StringComparison.Ordinal) || !Alive(kv.Value)) continue;
                int g = Math.Abs(kv.Key.Px - px);
                if (g < gap) { gap = g; best = kv.Value; }
            }
            return best;
        }

        static bool TryCached(Key key, out Texture tex)
        {
            if (Cache.TryGetValue(key, out tex))
            {
                if (Alive(tex))
                {
                    Lru.Remove(key);
                    Lru.AddFirst(key);
                    return true;
                }
                // A lost RenderTexture (WebGL context loss, device reset): draw it again.
                Drop(key);
            }
            tex = null;
            return false;
        }

        static bool Alive(Texture tex)
        {
            if (tex == null) return false;
            var rt = tex as RenderTexture;
            return rt == null || rt.IsCreated();
        }

        static void Queue(Key key, bool urgent)
        {
            int i = PendingKeys.IndexOf(key);
            if (i >= 0)
            {
                if (urgent && i > 0)
                {
                    var job = Pending[i];
                    Pending.RemoveAt(i);
                    PendingKeys.RemoveAt(i);
                    Pending.Insert(0, job);
                    PendingKeys.Insert(0, key);
                }
                return;
            }
            var j = PayloadRender.Begin(key.Form, key.Px, key.Backdrop);
            if (urgent)
            {
                Pending.Insert(0, j);
                PendingKeys.Insert(0, key);
            }
            else
            {
                Pending.Add(j);
                PendingKeys.Add(key);
            }
        }

        internal static void Wait(PayloadThumb thumb)
        {
            for (int i = 0; i < Waiting.Count; i++)
                if (Waiting[i].TryGetTarget(out var t) && ReferenceEquals(t, thumb)) return;
            Waiting.Add(new WeakReference<PayloadThumb>(thumb));
        }

        /// <summary>Shades pending renders for up to <paramref name="budgetMs"/>. True when nothing is left.</summary>
        public static bool Pump(double budgetMs = FrameBudgetMs)
        {
            if (Pending.Count == 0) return true;
            Clock.Restart();
            bool finished = false;
            while (Pending.Count > 0 && Clock.Elapsed.TotalMilliseconds < budgetMs)
            {
                var job = Pending[0];
                if (!job.Step(RowsPerStep)) continue;
                var key = PendingKeys[0];
                Pending.RemoveAt(0);
                PendingKeys.RemoveAt(0);
                Store(key, job);
                finished = true;
            }
            if (finished) Notify();
            return Pending.Count == 0;
        }

        static readonly Stopwatch Since = Stopwatch.StartNew();
        static double _lastFrameMs = -1000.0;

        /// <summary>
        /// One budgeted <see cref="Pump"/> per frame however many callers ask (the board's clock and every waiting
        /// thumb's scheduler), so twenty node thumbs cost one budget a frame, not twenty.
        /// </summary>
        public static void PumpFrame()
        {
            if (Pending.Count == 0) return;
            double now = Since.Elapsed.TotalMilliseconds;
            if (now - _lastFrameMs < 12.0) return;
            Pump(FrameBudgetMs);
            _lastFrameMs = Since.Elapsed.TotalMilliseconds;
        }

        /// <summary>Finishes every pending render now (tests and captures).</summary>
        public static void Flush()
        {
            while (Pending.Count > 0)
            {
                var job = Pending[0];
                var key = PendingKeys[0];
                Pending.RemoveAt(0);
                PendingKeys.RemoveAt(0);
                while (!job.Step(64)) { }
                Store(key, job);
            }
            Notify();
        }

        static void Notify()
        {
            for (int i = Waiting.Count - 1; i >= 0; i--)
            {
                if (!Waiting[i].TryGetTarget(out var t) || t.Refresh()) Waiting.RemoveAt(i);
            }
        }

        static Texture Store(Key key, PayloadRender.Job job)
        {
            var tex = Upload(key, job.Pixels);
            Drop(key);
            Cache[key] = tex;
            Lru.AddFirst(key);
            Renders++;
            while (Lru.Count > MaxCached)
            {
                var old = Lru.Last.Value;
                Drop(old);
            }
            return tex;
        }

        static void Drop(Key key)
        {
            if (Cache.TryGetValue(key, out var tex))
            {
                Cache.Remove(key);
                Release(tex);
            }
            Lru.Remove(key);
        }

        static void Release(Texture tex)
        {
            if (tex == null) return;
            if (tex is RenderTexture rt) rt.Release();
            if (Application.isPlaying) UnityEngine.Object.Destroy(tex);
            else UnityEngine.Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// The shaded pixels (rows from the top) into a RenderTexture. Unity texture rows start at the bottom, so the
        /// rows are flipped on the way into the staging Texture2D. With no graphics device (batchmode -nographics)
        /// the Texture2D itself is the result.
        /// </summary>
        static Texture Upload(Key key, byte[] rgba)
        {
            int px = key.Px;
            int stride = px * 4;
            var upright = new byte[rgba.Length];
            for (int y = 0; y < px; y++)
                Buffer.BlockCopy(rgba, y * stride, upright, (px - 1 - y) * stride, stride);
            var staging = new Texture2D(px, px, TextureFormat.RGBA32, false)
            {
                name = "KH_PayloadThumb_" + key.Form + "_" + px,
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            staging.SetPixelData(upright, 0);
            staging.Apply(false, false);
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return staging;

            var rt = new RenderTexture(px, px, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                name = "KH_PayloadThumbRT_" + key.Form + "_" + px,
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                antiAliasing = 1,
            };
            if (!rt.Create())
            {
                Release(rt);
                return staging;
            }
            Graphics.Blit(staging, rt);
            Release(staging);
            return rt;
        }

        /// <summary>Releases every cached render (domain reload, tests).</summary>
        public static void Clear()
        {
            foreach (var kv in Cache) Release(kv.Value);
            Cache.Clear();
            Lru.Clear();
            Pending.Clear();
            PendingKeys.Clear();
            Waiting.Clear();
        }

        /// <summary>The image for a UI background: a RenderTexture or (no device) a Texture2D.</summary>
        public static StyleBackground Style(Texture tex)
        {
            if (tex is RenderTexture rt) return new StyleBackground(Background.FromRenderTexture(rt));
            if (tex is Texture2D t2) return new StyleBackground(t2);
            return new StyleBackground(StyleKeyword.None);
        }
    }

    /// <summary>
    /// A square payload picture that sizes its render to its own layout: points x the panel's scaledPixelsPerPoint,
    /// rounded up to whole pixels. It asks again only when the form, the size or the backdrop changes; until the exact
    /// render lands it shows the nearest cached size of the same form, then the flat portrait.
    /// </summary>
    public class PayloadThumb : VisualElement
    {
        string _form;
        PayloadBackdrop _backdrop;
        int _px;
        Texture _shown;
        readonly bool _urgent;
        readonly float _pointsHint;

        public string Form => _form;
        public int Px => _px;
        public PayloadBackdrop Backdrop => _backdrop;

        /// <summary>The exact render (form, size, backdrop) is on screen.</summary>
        public bool Ready { get; private set; }

        public Texture Shown => _shown;

        public PayloadThumb(string name, string form, PayloadBackdrop backdrop, float pointsHint, bool urgent)
        {
            this.name = name;
            pickingMode = PickingMode.Ignore;
            _form = string.IsNullOrEmpty(form) ? "box" : form;
            _backdrop = backdrop;
            _pointsHint = pointsHint;
            _urgent = urgent;
            style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            RegisterCallback<GeometryChangedEvent>(_ => Refresh());
            RegisterCallback<AttachToPanelEvent>(_ => Refresh());
            Placeholder();
        }

        public void SetForm(string form, PayloadBackdrop backdrop)
        {
            string f = string.IsNullOrEmpty(form) ? "box" : form;
            if (f == _form && backdrop == _backdrop) return;
            _form = f;
            _backdrop = backdrop;
            Ready = false;
            Placeholder();
            Refresh();
        }

        /// <summary>Physical pixels for the current layout (the hint before layout runs).</summary>
        public int WantedPx()
        {
            float pts = layout.width;
            if (float.IsNaN(pts) || pts < 1f) pts = resolvedStyle.width;
            if (float.IsNaN(pts) || pts < 1f) pts = _pointsHint;
            float dpr = panel != null ? panel.scaledPixelsPerPoint : 1f;
            return PayloadThumbnails.PixelsFor(pts, dpr);
        }

        /// <summary>Shows the exact render when cached, else queues it. True when the exact render is shown.</summary>
        public bool Refresh()
        {
            int px = WantedPx();
            if (Ready && px == _px && _shown != null && IsLive(_shown)) return true;
            _px = px;
            var tex = PayloadThumbnails.Get(_form, px, _backdrop, _urgent);
            if (tex != null)
            {
                Show(tex);
                Ready = true;
                return true;
            }
            Ready = false;
            var near = PayloadThumbnails.Nearest(_form, px, _backdrop);
            if (near != null) Show(near);
            PayloadThumbnails.Wait(this);
            ArmPump();
            return false;
        }

        bool _pumping;

        /// <summary>Screens without a clock of their own (briefing, mission list) still get their render: the thumb
        /// pumps the shared queue (once a frame across all thumbs) until it is ready or leaves the panel.</summary>
        void ArmPump()
        {
            if (_pumping || panel == null) return;
            var item = schedule.Execute(() =>
            {
                PayloadThumbnails.PumpFrame();
                if (!Ready) Refresh();
            });
            if (item == null) return;
            _pumping = true;
            item.Every(16).Until(() =>
            {
                bool stop = Ready || panel == null;
                if (stop) _pumping = false;
                return stop;
            });
        }

        static bool IsLive(Texture t) => t != null && (!(t is RenderTexture rt) || rt.IsCreated());

        void Show(Texture tex)
        {
            if (ReferenceEquals(tex, _shown)) return;
            _shown = tex;
            style.backgroundImage = PayloadThumbnails.Style(tex);
        }

        void Placeholder()
        {
            var near = PayloadThumbnails.Nearest(_form, WantedPx(), _backdrop);
            if (near != null)
            {
                Show(near);
                return;
            }
            // The flat portrait (cheap, same silhouette) until the lit render lands.
            var flat = PayloadArt.Portrait(_form, Math.Max(32, (int)Math.Ceiling(_pointsHint)));
            if (flat != null) Show(flat);
        }
    }
}
