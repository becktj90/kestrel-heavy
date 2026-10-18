// On-disk save store. This is the durable copy; it retires the WKWebView SAVE_BRIDGE mirror.
// Keys and the 1 MB cap follow docs/SAVE_BRIDGE.md. localStorage does not enforce them.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Kestrel.Core.Designer;
using Kestrel.Core.Json;

namespace Kestrel.Core.Save
{
    public sealed class SaveException : Exception
    {
        public readonly string Code;
        public SaveException(string code, string message) : base(message) { Code = code; }
    }

    public sealed class NativeStore
    {
        public const int Schema = 1;
        public const string Prefix = "kh.save.v1.";
        public const string DesignPrefix = Prefix + "design.";
        public const string ProgressKey = Prefix + "progress";
        /// <summary>Career bank, unlocks, and contract counters. Schema stays <see cref="Schema"/>.</summary>
        public const string CareerKey = Prefix + "career";
        /// <summary>Per-mission bests. Schema stays <see cref="Schema"/>.</summary>
        public const string MissionsKey = Prefix + "missions";
        /// <summary>Mid-design autosave. Not a design slot: the key does not use <see cref="DesignPrefix"/>.</summary>
        public const string DraftKey = Prefix + "draft";
        /// <summary>Fictional credits wallet. Separate from the career Kc bank.</summary>
        public const string CreditsKey = Prefix + "credits";
        /// <summary>File copy of the PlayerPrefs toggles.</summary>
        public const string SettingsKey = Prefix + "settings";
        /// <summary>Raised after a record lands on disk. Listeners that throw are ignored.</summary>
        public static event Action Wrote;
        public const int MaxKeyLength = 200;
        public const int MaxValueBytes = 1_000_000;

        readonly string dir;
        readonly Func<long> now;
        readonly Func<double> random;
        public int? QuotaEntries;

        public NativeStore(string directory, Func<long> now, Func<double> random)
        {
            dir = directory;
            this.now = now;
            this.random = random;
            Directory.CreateDirectory(dir);
        }

        public static bool CheckKey(string key)
            => key != null && key.StartsWith("kh.save.", StringComparison.Ordinal) && key.Length <= MaxKeyLength && KeyChars(key);

        static bool KeyChars(string key)
        {
            foreach (char c in key)
                if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '.' || c == '_' || c == '-')) return false;
            return true;
        }

        public JsonObject GetDesign(string id)
        {
            var env = LoadDesignEnv(id);
            return env == null ? null : env.Obj("data");
        }

        public JsonObject GetMeta(string id)
        {
            var env = LoadDesignEnv(id);
            if (env == null) return null;
            var m = new JsonObject();
            m.Add("id", env["id"]); m.Add("name", env["name"]); m.Add("createdAt", env["createdAt"]); m.Add("updatedAt", env["updatedAt"]);
            return m;
        }

        public List<JsonObject> ListDesigns()
        {
            var rows = new List<JsonObject>();
            foreach (var key in Keys())
            {
                if (!key.StartsWith(DesignPrefix, StringComparison.Ordinal)) continue;
                var env = Read(key);
                if (env != null && env.Str("kind") == "design")
                {
                    var m = new JsonObject();
                    m.Add("id", env["id"]); m.Add("name", env["name"]); m.Add("createdAt", env["createdAt"]); m.Add("updatedAt", env["updatedAt"]);
                    rows.Add(m);
                }
            }
            rows.Sort((a, b) => b.Num("updatedAt").CompareTo(a.Num("updatedAt")));
            return rows;
        }

        public (string id, JsonObject envelope) PutDesign(JsonObject design, string id = null, string name = null)
        {
            var data = DesignMigration.Migrate(design);
            string sid = id;
            if (string.IsNullOrEmpty(sid)) sid = data.Str("id");
            if (string.IsNullOrEmpty(sid)) sid = JsRadix.NewId(now(), random());
            var prev = Read(DesignPrefix + sid);
            string nm = name;
            if (string.IsNullOrEmpty(nm)) nm = prev?.Str("name");
            if (string.IsNullOrEmpty(nm)) nm = data.Str("name");
            if (string.IsNullOrEmpty(nm)) nm = "Untitled";
            object created = prev != null && prev.ContainsKey("createdAt") ? prev["createdAt"] : (object)(double)now();
            var env = Envelope("design", sid, nm, data, created);
            Write(DesignPrefix + sid, env);
            return (sid, env);
        }

        public bool DeleteDesign(string id)
        {
            bool had = Read(DesignPrefix + id) != null;
            Remove(DesignPrefix + id);
            return had;
        }

        public (string id, JsonObject envelope) DuplicateDesign(string id, string name = null)
        {
            var e = LoadDesignEnv(id);
            if (e == null) throw new SaveException("missing", "no design " + id);
            string nid = JsRadix.NewId(now(), random());
            string nm = string.IsNullOrEmpty(name) ? e.Str("name") + " copy" : name;
            return PutDesign(e.Obj("data"), nid, nm);
        }

        public JsonObject RenameDesign(string id, string name)
        {
            var e = Read(DesignPrefix + id);
            if (e == null) throw new SaveException("missing", "no design " + id);
            var next = JsonMut.CloneObject(e);
            JsonMut.Set(next, "name", name ?? "");
            JsonMut.Set(next, "updatedAt", (double)now());
            Write(DesignPrefix + id, next);
            return next;
        }

        public JsonObject GetProgress()
        {
            var e = Read(ProgressKey);
            return e != null && e.Str("kind") == "progress" ? e.Obj("data") : null;
        }

        public JsonObject PutProgress(JsonObject data)
        {
            var prev = Read(ProgressKey);
            object created = prev != null && prev.ContainsKey("createdAt") ? prev["createdAt"] : (object)(double)now();
            var env = Envelope("progress", "progress", "progress", data, created);
            Write(ProgressKey, env);
            return env;
        }

        public void DeleteProgress() => Remove(ProgressKey);

        /// <summary>
        /// Write one non-design record with the same envelope as progress (schema 1).
        /// A previous envelope keeps its createdAt. The kind is also the id and the name.
        /// </summary>
        public JsonObject PutRecord(string key, string kind, JsonObject data)
        {
            if (string.IsNullOrEmpty(kind) || !KeyChars(kind)) throw new SaveException("invalid-key", "invalid kind");
            var prev = Read(key);
            object created = prev != null && prev.ContainsKey("createdAt") ? prev["createdAt"] : (object)(double)now();
            var env = Envelope(kind, kind, kind, data ?? new JsonObject(), created);
            Write(key, env);
            return env;
        }

        /// <summary>True when the main file or its interrupted temp file exists.</summary>
        public bool HasBytes(string key)
        {
            if (!CheckKey(key)) throw new SaveException("invalid-key", "invalid key");
            string main = PathFor(key);
            return File.Exists(main) || File.Exists(main + ".tmp");
        }

        /// <summary>
        /// Copy a record to <c>key.json.bak</c> once. A backup that already exists is left as it is.
        /// Returns true when the backup is on disk after the call.
        /// </summary>
        public bool KeepBackup(string key)
        {
            if (!CheckKey(key)) throw new SaveException("invalid-key", "invalid key");
            string main = PathFor(key);
            string source = File.Exists(main) ? main : main + ".tmp";
            if (!File.Exists(source)) return false;
            string bak = main + ".bak";
            if (File.Exists(bak)) return true;
            try { File.Copy(source, bak); return true; }
            catch (Exception e) { throw new SaveException("write", e.Message); }
        }

        /// <summary>
        /// If the main file exists and does not parse as a JSON object, copy it to
        /// <c>key.json.bak</c> before any temp-file recovery replaces it.
        /// A file larger than <see cref="MaxValueBytes"/> is not copied.
        /// </summary>
        public bool BackupCorruptMain(string key)
        {
            if (!CheckKey(key)) throw new SaveException("invalid-key", "invalid key");
            string main = PathFor(key);
            if (!File.Exists(main)) return false;
            string raw;
            try { raw = File.ReadAllText(main); }
            catch (Exception e) { throw new SaveException("write", e.Message); }
            if (TryParse(raw) != null) return false;
            if (new FileInfo(main).Length > MaxValueBytes) return false;
            return KeepBackup(key);
        }

        public Dictionary<string, JsonObject> Snapshot()
        {
            var o = new Dictionary<string, JsonObject>();
            foreach (var k in Keys()) o[k] = Read(k);
            return o;
        }

        JsonObject LoadDesignEnv(string id)
        {
            var env = Read(DesignPrefix + id);
            if (env == null || env.Str("kind") != "design") return null;
            try
            {
                var data = DesignMigration.Migrate(env.Obj("data"));
                var copy = JsonMut.CloneObject(env);
                JsonMut.Set(copy, "data", data);
                return copy;
            }
            catch (InvalidOperationException) { return null; }
        }

        JsonObject Envelope(string kind, string id, string name, object data, object createdAt)
        {
            var o = new JsonObject();
            o.Add("schema", (double)Schema);
            o.Add("kind", kind);
            o.Add("id", id);
            o.Add("name", name);
            o.Add("createdAt", createdAt);
            o.Add("updatedAt", (double)now());
            o.Add("data", data);
            return o;
        }

        void Write(string key, JsonObject env)
        {
            if (!CheckKey(key)) throw new SaveException("invalid-key", "invalid key");
            if (QuotaEntries.HasValue && !File.Exists(PathFor(key)) && Keys().Count >= QuotaEntries.Value)
                throw new SaveException("quota", "quota");
            string json = JsonWriter.Stringify(env);
            if (Encoding.UTF8.GetByteCount(json) > MaxValueBytes) throw new SaveException("quota", "value exceeds 1000000 bytes");
            string main = PathFor(key), tmp = main + ".tmp";
            try
            {
                File.WriteAllText(tmp, json);
                if (File.ReadAllText(tmp) != json) throw new SaveException("write", "temp verify failed");
                if (File.Exists(main)) File.Delete(main);
                File.Move(tmp, main);
            }
            catch (SaveException) { TryDelete(tmp); throw; }
            catch (Exception e) { TryDelete(tmp); throw new SaveException("write", e.Message); }
            try { Wrote?.Invoke(); }
            catch (Exception) { }
        }

        public JsonObject Read(string key)
        {
            if (!CheckKey(key)) throw new SaveException("invalid-key", "invalid key");
            string main = PathFor(key), tmp = main + ".tmp";
            var parsed = TryParse(File.Exists(main) ? File.ReadAllText(main) : null);
            if (parsed != null) return parsed;
            var recovered = TryParse(File.Exists(tmp) ? File.ReadAllText(tmp) : null);
            if (recovered != null)
            {
                try
                {
                    File.WriteAllText(main, JsonWriter.Stringify(recovered));
                    TryDelete(tmp);
                }
                catch (IOException) { /* keep tmp */ }
                return recovered;
            }
            return null;
        }

        void Remove(string key)
        {
            if (!CheckKey(key)) throw new SaveException("invalid-key", "invalid key");
            TryDelete(PathFor(key));
            TryDelete(PathFor(key) + ".tmp");
        }

        List<string> Keys()
        {
            var o = new List<string>();
            if (!Directory.Exists(dir)) return o;
            foreach (var f in Directory.GetFiles(dir))
            {
                string name = Path.GetFileName(f);
                if (!name.EndsWith(".json", StringComparison.Ordinal) || name.EndsWith(".json.tmp", StringComparison.Ordinal)) continue;
                o.Add(name.Substring(0, name.Length - ".json".Length));
            }
            o.Sort(StringComparer.Ordinal);
            return o;
        }

        string PathFor(string key) => Path.Combine(dir, key + ".json");

        static JsonObject TryParse(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            try { return MiniJson.Parse(raw) as JsonObject; }
            catch (FormatException) { return null; }
        }

        static void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch (IOException) { } }
    }
}
