// Mutate and clone MiniJson trees without a writer. New keys append; existing keys keep their place.
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Json;

namespace Kestrel.Core.Designer
{
    public static class JsonMut
    {
        public static void Set(JsonObject o, string k, object v)
        {
            if (!o.ContainsKey(k)) o.Order.Add(k);
            o[k] = v;
        }

        public static JsonObject CloneObject(JsonObject o) => (JsonObject)Clone(o);

        public static object Clone(object v)
        {
            if (v is JsonObject o)
            {
                var n = new JsonObject();
                foreach (var k in o.Order) n.Add(k, Clone(o[k]));
                return n;
            }
            if (v is List<object> a)
            {
                var n = new List<object>(a.Count);
                foreach (var x in a) n.Add(Clone(x));
                return n;
            }
            return v;
        }

        public static bool IsInteger(object v)
        {
            if (!(v is double d) || double.IsNaN(d) || double.IsInfinity(d)) return false;
            return d == Math.Truncate(d) && Math.Abs(d) <= 9007199254740991d;
        }

        public static string Shown(JsonObject o, string k)
        {
            if (o == null || !o.ContainsKey(k)) return "undefined";
            return ShownValue(o[k]);
        }

        public static string ShownValue(object v)
        {
            if (v == null) return "null";
            if (v is string s) return s;
            if (v is bool b) return b ? "true" : "false";
            if (v is double d)
            {
                if (double.IsNaN(d)) return "NaN";
                if (double.IsPositiveInfinity(d)) return "Infinity";
                if (double.IsNegativeInfinity(d)) return "-Infinity";
                if (IsInteger(d)) return ((long)d).ToString(CultureInfo.InvariantCulture);
                return d.ToString("R", CultureInfo.InvariantCulture);
            }
            return "undefined";
        }

        public static bool TruthyNumber(JsonObject o, string k)
        {
            if (o == null || !o.ContainsKey(k) || !(o[k] is double d)) return false;
            return d != 0 && !double.IsNaN(d);
        }
    }
}
