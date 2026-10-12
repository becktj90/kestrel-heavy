using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Designer;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class SaveGoldenTests
    {
        [Test]
        public void Migrate_MatchesJs()
        {
            foreach (JsonObject c in Golden.Cases("vehicle-migrate"))
            {
                var input = c.Obj("input") ?? new JsonObject();
                if (c.ContainsKey("error"))
                {
                    var ex = Assert.Throws<InvalidOperationException>(() => DesignMigration.Migrate(input));
                    Assert.AreEqual(c.Str("error"), ex.Message, c.Str("name"));
                }
                else JsonSame.Equal(DesignMigration.Migrate(input), c["out"], c.Str("name"));
            }
        }

        [Test]
        public void Ids_Keys_AndSession_MatchJs()
        {
            JsonObject ids = null, keys = null, session = null;
            foreach (JsonObject c in Golden.Cases("save-store"))
            {
                if (c.Str("kind") == "ids") ids = c;
                if (c.Str("kind") == "keys") keys = c;
                if (c.Str("kind") == "session") session = c;
            }
            foreach (JsonObject row in ids.Arr("cases"))
                Assert.AreEqual(row.Str("id"), JsRadix.NewId((long)row.Num("now"), row.Num("random")));
            foreach (JsonObject row in keys.Arr("cases"))
                Assert.AreEqual(row.Bool("ok"), NativeStore.CheckKey(row.Str("key")), row.Str("key"));
            Assert.AreEqual(200, (int)keys.Num("maxKeyLength"));
            Assert.AreEqual(1000000, (int)keys.Num("maxValueBytes"));

            long clock = (long)session.Num("clockStart");
            long step = (long)session.Num("clockStep");
            var randoms = session.Arr("randoms");
            int ri = 0;
            string dir = Path.Combine(Path.GetTempPath(), "kh-save-" + Guid.NewGuid().ToString("N"));
            var store = new NativeStore(dir, () => { long t = clock; clock += step; return t; }, () => (double)randoms[ri++]);
            try
            {
                JsonObject scout = null;
                foreach (JsonObject p in MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "presets.json"))).Arr("presets"))
                    if (p.Str("id") == "scout") scout = JsonMut.CloneObject(p);
                scout.Remove("id"); scout.Order.Remove("id");
                var put = store.PutDesign(scout, null, "Mine");
                var dup = store.DuplicateDesign(put.id);
                store.RenameDesign(put.id, "Renamed");
                var progress = new JsonObject();
                progress.Add("orbitsCompleted", 1d);
                progress.Add("engineerEnabled", false);
                store.PutProgress(progress);
                Assert.IsTrue(store.DeleteDesign(dup.id));
                store.PutDesign(Obj(("name", "Old")), "legacy", null);
                var snap = store.Snapshot();
                var want = session.Obj("snapshot");
                Assert.AreEqual(want.Count, snap.Count, "record count");
                foreach (var k in want.Order) JsonSame.Equal(snap[k], want[k], k);
                var list = store.ListDesigns();
                Assert.AreEqual(session.Arr("list").Count, list.Count);
                for (int i = 0; i < list.Count; i++) JsonSame.Equal(list[i], session.Arr("list")[i], "list" + i);
                JsonSame.Equal(store.GetProgress(), session["progress"], "progress");
                JsonSame.Equal(store.GetDesign("legacy"), session["legacy"], "legacy");
            }
            finally { Directory.Delete(dir, true); }
        }

        [Test]
        public void InterruptedWrite_PromotesTemp()
        {
            string dir = Path.Combine(Path.GetTempPath(), "kh-save-" + Guid.NewGuid().ToString("N"));
            long t = 10;
            var store = new NativeStore(dir, () => t++, () => 0.123456789);
            try
            {
                var env = store.PutDesign(Obj(("name", "Keep")), "one", "Keep").envelope;
                string main = Path.Combine(dir, NativeStore.DesignPrefix + "one.json");
                string tmp = main + ".tmp";
                File.WriteAllText(tmp, File.ReadAllText(main));
                File.WriteAllText(main, "{not json");
                var got = store.Read(NativeStore.DesignPrefix + "one");
                JsonSame.Equal(got, env, "recovered");
                Assert.IsFalse(File.Exists(tmp));
                Assert.Throws<SaveException>(() => store.PutDesign(Obj(("name", "X")), "bad key", null));
            }
            finally { Directory.Delete(dir, true); }
        }

        static JsonObject Obj(params (string k, object v)[] pairs)
        {
            var o = new JsonObject();
            foreach (var (k, v) in pairs) o.Add(k, v);
            return o;
        }
    }
}
