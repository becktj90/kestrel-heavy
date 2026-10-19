// One camera mode. The save record is the copy that survives a reinstall of prefs;
// PlayerPrefs is the cache the next launch can read before the store opens.
using System;
using System.IO;
using Kestrel.Core.Flight;
using Kestrel.Core.Save;
using UnityEngine;

namespace Kestrel.Slice
{
    public static class FlightCameraStore
    {
        public static FlightViewMode Load()
        {
            var saved = ReadRecord();
            if (saved.HasValue) return saved.Value;
            return ReadPrefs();
        }

        public static void Save(FlightViewMode mode)
        {
            WritePrefs(mode);
            WriteRecord(mode);
        }

        static FlightViewMode ReadPrefs()
        {
            try
            {
                if (!PlayerPrefs.HasKey(FlightView.PrefKey)) return FlightViewMode.Auto;
                return FlightView.Parse(PlayerPrefs.GetString(FlightView.PrefKey, "auto"));
            }
            catch (Exception) { return FlightViewMode.Auto; }
        }

        static void WritePrefs(FlightViewMode mode)
        {
            try
            {
                PlayerPrefs.SetString(FlightView.PrefKey, FlightView.Name(mode).ToLowerInvariant());
                PlayerPrefs.Save();
            }
            catch (Exception) { }
        }

        static FlightViewMode? ReadRecord()
        {
            try
            {
                string dir = SaveDir();
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return null;
                var store = Open(dir);
                var data = store.GetRecord(FlightView.RecordKey);
                if (data == null) return null;
                return FlightView.FromData(data);
            }
            catch (Exception) { return null; }
        }

        static void WriteRecord(FlightViewMode mode)
        {
            try
            {
                string dir = SaveDir();
                if (string.IsNullOrEmpty(dir)) return;
                Directory.CreateDirectory(dir);
                var store = Open(dir);
                store.PutRecord(FlightView.RecordKey, "settings", FlightView.Merge(store.GetRecord(FlightView.RecordKey), mode));
            }
            catch (Exception) { }
        }

        static string SaveDir()
        {
            return Path.Combine(Application.persistentDataPath, "saves");
        }

        static NativeStore Open(string dir)
        {
            return new NativeStore(dir, () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), () => 0.2);
        }
    }
}
