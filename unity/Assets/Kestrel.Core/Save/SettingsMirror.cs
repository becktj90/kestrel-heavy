// A file copy of the PlayerPrefs toggles. WebGL drops PlayerPrefs that were not Save()'d
// when the tab closes; this record is the copy that comes back with the other save files.
using System;
using System.Collections.Generic;
using Kestrel.Core.Designer;
using Kestrel.Core.Json;

namespace Kestrel.Core.Save
{
    public static class SettingsMirror
    {
        public static readonly string[] Keys =
        {
            "kh.audio.muted",
            "kh.audio.music.muted",
            "kh.audio.narration.muted",
            "kh.settings.subtitles",
            "kh.settings.sandbox",
            "kh.a11y.reduceMotion",
            "kh.a11y.reduceFlashing",
            "kh.hangar.haptics",
            "kh.hangar.engineer",
            "kh.designer.v3.engineer",
            "kh.designer.v3.tutorial",
            "kh.quality.chosen",
            "kh.quality.batterySaver",
        };

        public static JsonObject Capture(Func<string, bool> has, Func<string, int> read)
        {
            var data = new JsonObject();
            if (has == null || read == null) return data;
            for (int i = 0; i < Keys.Length; i++)
            {
                string key = Keys[i];
                if (!has(key)) continue;
                data.Add(key, read(key));
            }
            return data;
        }

        public static void Save(NativeStore store, Func<string, bool> has, Func<string, int> read)
        {
            if (store == null) return;
            var env = store.Read(NativeStore.SettingsKey);
            var prev = env != null && env.Str("kind") == "settings" ? env.Obj("data") : null;
            var data = prev != null ? JsonMut.CloneObject(prev) : new JsonObject();
            for (int i = 0; i < Keys.Length; i++)
            {
                string key = Keys[i];
                if (!data.ContainsKey(key)) continue;
                data.Remove(key);
                data.Order.Remove(key);
            }
            foreach (var kv in Capture(has, read)) JsonMut.Set(data, kv.Key, kv.Value);
            store.PutRecord(NativeStore.SettingsKey, "settings", data);
        }

        /// <summary>Fill keys the live prefs do not have yet. A key that already exists is left alone.</summary>
        public static int Restore(NativeStore store, Func<string, bool> has, Action<string, int> write)
        {
            if (store == null || has == null || write == null) return 0;
            var env = store.Read(NativeStore.SettingsKey);
            var data = env != null && env.Str("kind") == "settings" ? env.Obj("data") : null;
            if (data == null) return 0;
            int n = 0;
            for (int i = 0; i < Keys.Length; i++)
            {
                string key = Keys[i];
                if (!data.ContainsKey(key) || has(key)) continue;
                var v = data[key];
                int value = v is int iv ? iv : v is long lv ? (int)lv : v is double d ? (int)d : 0;
                write(key, value);
                n++;
            }
            return n;
        }
    }
}
