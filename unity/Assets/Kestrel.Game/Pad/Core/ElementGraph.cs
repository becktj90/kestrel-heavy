// Tagged pad element graph (D1). Every built pad element registers here: id, category, tags, parent, bounds.
// Engine-free so the later inspect mode, wear/damage and timeline tracks can query it without Unity.
using System;
using System.Collections.Generic;
using Kestrel.Kit.Contracts;

namespace Kestrel.Pad
{
    public enum ElementCategory { Site, Structure, Level, Member, Deck, SwingArm, Connector, Pipe, Valve, Tank, Light, Prop }

    public sealed class PadElement
    {
        public string Id; public ElementCategory Category; public string ParentId;
        public readonly HashSet<string> Tags = new HashSet<string>(StringComparer.Ordinal);
        /// <summary>Axis-aligned bounds in pad space (centre, full size), metres. Used for inspect picking.</summary>
        public Vec3 Center, Size;
        public string DisplayName;
        public bool HasTag(string t) => Tags.Contains(t);
    }

    public sealed class ElementGraph
    {
        readonly Dictionary<string, PadElement> _byId = new Dictionary<string, PadElement>(StringComparer.Ordinal);
        readonly Dictionary<string, List<string>> _children = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        readonly List<string> _roots = new List<string>();
        readonly List<string> _order = new List<string>();

        public int Count => _byId.Count;
        public IReadOnlyList<string> Roots => _roots;
        public IEnumerable<PadElement> All { get { foreach (var id in _order) yield return _byId[id]; } }

        /// <summary>Adds an element. Ids are unique; the parent (if any) must already exist, so the graph is always a forest.</summary>
        public PadElement Add(string id, ElementCategory cat, string parentId = null, params string[] tags)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("element id is empty");
            if (_byId.ContainsKey(id)) throw new ArgumentException($"duplicate element id {id}");
            if (parentId != null && !_byId.ContainsKey(parentId)) throw new ArgumentException($"{id}: unknown parent {parentId}");
            var e = new PadElement { Id = id, Category = cat, ParentId = parentId, DisplayName = id };
            if (tags != null) foreach (var t in tags) if (!string.IsNullOrEmpty(t)) e.Tags.Add(t);
            _byId[id] = e; _order.Add(id); _children[id] = new List<string>();
            if (parentId == null) _roots.Add(id); else _children[parentId].Add(id);
            return e;
        }

        public bool Contains(string id) => id != null && _byId.ContainsKey(id);
        public PadElement Get(string id) => id != null && _byId.TryGetValue(id, out var e) ? e : null;
        public PadElement Parent(string id) => Get(Get(id)?.ParentId);
        public IReadOnlyList<string> ChildIds(string id) => _children.TryGetValue(id, out var c) ? c : (IReadOnlyList<string>)Array.Empty<string>();

        public IEnumerable<PadElement> Descendants(string id)
        {
            var stack = new Stack<string>(); for (int i = ChildIds(id).Count - 1; i >= 0; i--) stack.Push(ChildIds(id)[i]);
            while (stack.Count > 0) { var c = stack.Pop(); yield return _byId[c]; var k = _children[c]; for (int i = k.Count - 1; i >= 0; i--) stack.Push(k[i]); }
        }

        /// <summary>Root-first chain of ids, e.g. site/tower/tower.l03/tower.l03.col.ne.</summary>
        public List<string> PathTo(string id)
        {
            var p = new List<string>(); for (var e = Get(id); e != null; e = Get(e.ParentId)) p.Add(e.Id);
            p.Reverse(); return p;
        }

        /// <summary>Filter by any combination; null = don't care. allTags must all be present. Optional subtree root.</summary>
        public List<PadElement> Query(ElementCategory? category = null, string tag = null, string under = null, params string[] allTags)
        {
            var r = new List<PadElement>();
            IEnumerable<PadElement> src = under == null ? All : Descendants(under);
            foreach (var e in src)
            {
                if (category.HasValue && e.Category != category.Value) continue;
                if (tag != null && !e.Tags.Contains(tag)) continue;
                bool ok = true; if (allTags != null) foreach (var t in allTags) if (!e.Tags.Contains(t)) { ok = false; break; }
                if (ok) r.Add(e);
            }
            return r;
        }

        /// <summary>Smallest-volume element whose bounds contain the point (inspect-mode picking seed).</summary>
        public PadElement PickAt(Vec3 p, ElementCategory? category = null)
        {
            PadElement best = null; double bestV = double.MaxValue;
            foreach (var e in All)
            {
                if (category.HasValue && e.Category != category.Value) continue;
                if (Math.Abs(p.X - e.Center.X) > e.Size.X * 0.5f || Math.Abs(p.Y - e.Center.Y) > e.Size.Y * 0.5f || Math.Abs(p.Z - e.Center.Z) > e.Size.Z * 0.5f) continue;
                double v = (double)e.Size.X * e.Size.Y * e.Size.Z; if (v < bestV) { bestV = v; best = e; }
            }
            return best;
        }

        public Dictionary<ElementCategory, int> CountByCategory()
        { var d = new Dictionary<ElementCategory, int>(); foreach (var e in All) d[e.Category] = d.TryGetValue(e.Category, out var n) ? n + 1 : 1; return d; }
    }
}
