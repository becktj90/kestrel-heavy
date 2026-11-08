// Fun review: in the sandbox designer the side-booster chips "did nothing". The editor works; the wallet gate
// (HangarSession.Afford) turned the edit down and nothing said so. Refused now fires so the panel can say why.
using Kestrel.Core.Hangar;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class HangarRefusedTests
    {
        static HangarSession Hopper() =>
            new HangarSession(Golden.Catalog, HangarFixtures.Data, HangarFixtures.Template("hopper"));

        [Test]
        public void BoostersChangeTheDesignWhenTheWalletAllowsIt()
        {
            var session = Hopper();
            Assert.AreEqual(0, HangarParts.BoosterCount(session.Design));
            int fired = 0;
            session.Refused += () => fired++;
            Assert.IsTrue(session.Apply(d => HangarEditor.SetBoosterCount(Golden.Catalog, d, 2)).changed);
            Assert.AreEqual(2, HangarParts.BoosterCount(session.Design));
            Assert.AreEqual(0, fired);
        }

        [Test]
        public void ARefusedEditSaysSoInsteadOfLookingDead()
        {
            var session = Hopper();
            string before = HangarSession.Snapshot(session.Design);
            int fired = 0;
            session.Refused += () => fired++;
            session.Afford = (_, __) => false;
            Assert.IsFalse(session.Apply(d => HangarEditor.SetBoosterCount(Golden.Catalog, d, 2)).changed);
            Assert.AreEqual(before, HangarSession.Snapshot(session.Design));
            Assert.AreEqual(1, fired);
            Assert.AreEqual(1, session.RefusedCount);
        }

        [Test]
        public void ANoOpEditIsNotARefusal()
        {
            var session = Hopper();
            int fired = 0;
            session.Refused += () => fired++;
            session.Afford = (_, __) => false;
            Assert.IsFalse(session.Apply(d => HangarEditor.SetBoosterCount(Golden.Catalog, d, 0)).changed);
            Assert.AreEqual(0, fired, "same count: nothing to refuse");
        }

        [Test]
        public void RefusedUndoAndRedoAlsoSaySo()
        {
            var session = Hopper();
            Assert.IsTrue(session.Apply(d => HangarEditor.SetBoosterCount(Golden.Catalog, d, 2)).changed);
            int fired = 0;
            session.Refused += () => fired++;
            session.Afford = (_, __) => false;
            Assert.IsNull(session.Undo());
            Assert.AreEqual(1, fired);
            session.Afford = null;
            Assert.IsNotNull(session.Undo());
            session.Afford = (_, __) => false;
            Assert.IsNull(session.Redo());
            Assert.AreEqual(2, fired);
        }

        [Test]
        public void RefusedTextIsKidReadable()
        {
            StringAssert.Contains("Kc", HangarSession.RefusedText);
            Assert.That(HangarSession.RefusedText.Length, Is.LessThanOrEqualTo(60));
            StringAssert.DoesNotContain("$", HangarSession.RefusedText);
        }
    }
}
