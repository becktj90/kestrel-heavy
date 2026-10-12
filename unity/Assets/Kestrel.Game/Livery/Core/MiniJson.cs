// Minimal strict JSON reader (objects, arrays, strings, numbers, true/false/null). Engine-free; no packages needed.
// Objects -> Dictionary<string, object> (duplicate keys are an error), arrays -> List<object>, numbers -> double.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Kestrel.Livery
{
    public static class MiniJson
    {
        public static object Parse(string s)
        {
            int i = 0; var v = Value(s, ref i); Ws(s, ref i);
            if (i != s.Length) throw new FormatException($"trailing data at {i}");
            return v;
        }

        static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

        static object Value(string s, ref int i)
        {
            Ws(s, ref i);
            if (i >= s.Length) throw new FormatException("unexpected end");
            char c = s[i];
            if (c == '{') return Obj(s, ref i);
            if (c == '[') return Arr(s, ref i);
            if (c == '"') return Str(s, ref i);
            if (Lit(s, ref i, "true")) return true;
            if (Lit(s, ref i, "false")) return false;
            if (Lit(s, ref i, "null")) return null;
            return Num(s, ref i);
        }

        static bool Lit(string s, ref int i, string w)
        { if (string.CompareOrdinal(s, i, w, 0, w.Length) == 0) { i += w.Length; return true; } return false; }

        static Dictionary<string, object> Obj(string s, ref int i)
        {
            var d = new Dictionary<string, object>(); i++; Ws(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return d; }
            while (true)
            {
                Ws(s, ref i); if (i >= s.Length || s[i] != '"') throw new FormatException($"key expected at {i}");
                var k = Str(s, ref i); Ws(s, ref i);
                if (i >= s.Length || s[i] != ':') throw new FormatException($"':' expected at {i}"); i++;
                if (d.ContainsKey(k)) throw new FormatException($"duplicate key '{k}'");
                d[k] = Value(s, ref i); Ws(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == '}') { i++; return d; }
                throw new FormatException($"',' or '}}' expected at {i}");
            }
        }

        static List<object> Arr(string s, ref int i)
        {
            var a = new List<object>(); i++; Ws(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return a; }
            while (true)
            {
                a.Add(Value(s, ref i)); Ws(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == ']') { i++; return a; }
                throw new FormatException($"',' or ']' expected at {i}");
            }
        }

        static string Str(string s, ref int i)
        {
            var b = new StringBuilder(); i++;
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return b.ToString();
                if (c != '\\') { b.Append(c); continue; }
                if (i >= s.Length) break;
                char e = s[i++];
                switch (e)
                {
                    case '"': b.Append('"'); break; case '\\': b.Append('\\'); break; case '/': b.Append('/'); break;
                    case 'b': b.Append('\b'); break; case 'f': b.Append('\f'); break; case 'n': b.Append('\n'); break;
                    case 'r': b.Append('\r'); break; case 't': b.Append('\t'); break;
                    case 'u': b.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture)); i += 4; break;
                    default: throw new FormatException($"bad escape at {i}");
                }
            }
            throw new FormatException("unterminated string");
        }

        static double Num(string s, ref int i)
        {
            int st = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            if (st == i) throw new FormatException($"value expected at {st}");
            return double.Parse(s.Substring(st, i - st), NumberStyles.Float, CultureInfo.InvariantCulture);
        }
    }
}
