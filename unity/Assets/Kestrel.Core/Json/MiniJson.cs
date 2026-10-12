// Minimal, dependency-free JSON reader for the engine-agnostic core (no UnityEngine,
// no System.Text.Json / Newtonsoft so it builds identically in Unity and plain dotnet).
// Objects -> Dictionary<string, object> (insertion order kept via JsonObject), arrays -> List<object>,
// numbers -> double, true/false -> bool, null -> null.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Kestrel.Core.Json
{
    public sealed class JsonObject : Dictionary<string, object>
    {
        public readonly List<string> Order = new List<string>();
        public new void Add(string k, object v) { if (!ContainsKey(k)) Order.Add(k); base[k] = v; }
        public double Num(string k, double def = 0) => TryGetValue(k, out var v) && v is double d ? d : def;
        public double? NumOrNull(string k) => TryGetValue(k, out var v) && v is double d ? d : (double?)null;
        public string Str(string k, string def = null) => TryGetValue(k, out var v) && v is string s ? s : def;
        public bool Bool(string k, bool def = false) => TryGetValue(k, out var v) && v is bool b ? b : def;
        public JsonObject Obj(string k) => TryGetValue(k, out var v) ? v as JsonObject : null;
        public List<object> Arr(string k) => TryGetValue(k, out var v) ? v as List<object> : null;
    }

    public static class MiniJson
    {
        public static object Parse(string text) { int i = 0; var v = Value(text, ref i); Ws(text, ref i); if (i != text.Length) throw Err(text, i, "trailing data"); return v; }
        public static JsonObject ParseObject(string text) => (JsonObject)Parse(text);

        static Exception Err(string s, int i, string m) => new FormatException($"JSON: {m} at {i}");
        static void Ws(string s, ref int i) { while (i < s.Length && (s[i] == ' ' || s[i] == '\n' || s[i] == '\r' || s[i] == '\t')) i++; }

        static object Value(string s, ref int i)
        {
            Ws(s, ref i);
            if (i >= s.Length) throw Err(s, i, "unexpected end");
            char c = s[i];
            if (c == '{')
            {
                var o = new JsonObject(); i++; Ws(s, ref i);
                if (s[i] == '}') { i++; return o; }
                while (true)
                {
                    Ws(s, ref i); var k = Str(s, ref i); Ws(s, ref i);
                    if (s[i++] != ':') throw Err(s, i, "expected :");
                    o.Add(k, Value(s, ref i)); Ws(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == '}') { i++; return o; }
                    throw Err(s, i, "expected , or }");
                }
            }
            if (c == '[')
            {
                var a = new List<object>(); i++; Ws(s, ref i);
                if (s[i] == ']') { i++; return a; }
                while (true)
                {
                    a.Add(Value(s, ref i)); Ws(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == ']') { i++; return a; }
                    throw Err(s, i, "expected , or ]");
                }
            }
            if (c == '"') return Str(s, ref i);
            if (s.Length - i >= 4 && string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
            if (s.Length - i >= 5 && string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
            if (s.Length - i >= 4 && string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
            int st = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            if (st == i) throw Err(s, i, "unexpected char");
            // .NET Core 3.0+ and Unity 2021+ parse round-trip doubles exactly (IEEE correctly rounded).
            return double.Parse(s.Substring(st, i - st), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        static string Str(string s, ref int i)
        {
            if (s[i] != '"') throw Err(s, i, "expected string");
            i++; var sb = new StringBuilder();
            while (s[i] != '"')
            {
                char c = s[i++];
                if (c != '\\') { sb.Append(c); continue; }
                char e = s[i++];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break; case 't': sb.Append('\t'); break; case 'r': sb.Append('\r'); break;
                    case 'b': sb.Append('\b'); break; case 'f': sb.Append('\f'); break;
                    case 'u': sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16)); i += 4; break;
                    default: sb.Append(e); break;
                }
            }
            i++; return sb.ToString();
        }
    }
}
