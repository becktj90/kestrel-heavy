// JSON writer for the native save file. Round-trips through MiniJson; key order follows JsonObject.Order.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Kestrel.Core.Designer;
using Kestrel.Core.Json;

namespace Kestrel.Core.Save
{
    public static class JsonWriter
    {
        public static string Stringify(object value)
        {
            var sb = new StringBuilder();
            Write(sb, value);
            return sb.ToString();
        }

        static void Write(StringBuilder sb, object v)
        {
            if (v == null) { sb.Append("null"); return; }
            if (v is bool b) { sb.Append(b ? "true" : "false"); return; }
            if (v is string s) { sb.Append('"'); Escape(sb, s); sb.Append('"'); return; }
            if (v is double d) { sb.Append(Num(d)); return; }
            if (v is int n) { sb.Append(n.ToString(CultureInfo.InvariantCulture)); return; }
            if (v is long n64) { sb.Append(n64.ToString(CultureInfo.InvariantCulture)); return; }
            if (v is JsonObject o)
            {
                sb.Append('{');
                bool first = true;
                foreach (var k in o.Order)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append('"'); Escape(sb, k); sb.Append("\":");
                    Write(sb, o[k]);
                }
                sb.Append('}');
                return;
            }
            if (v is List<object> a)
            {
                sb.Append('[');
                for (int i = 0; i < a.Count; i++) { if (i > 0) sb.Append(','); Write(sb, a[i]); }
                sb.Append(']');
                return;
            }
            throw new InvalidOperationException("unsupported JSON value " + v.GetType().Name);
        }

        static string Num(double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d)) throw new InvalidOperationException("non-finite JSON number");
            if (JsonMut.IsInteger(d) && d >= long.MinValue && d <= long.MaxValue) return ((long)d).ToString(CultureInfo.InvariantCulture);
            return d.ToString("R", CultureInfo.InvariantCulture);
        }

        static void Escape(StringBuilder sb, string s)
        {
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
        }
    }
}
