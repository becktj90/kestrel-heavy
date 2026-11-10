// G-2: Game Center failures stay harmless offline (and with no plug-in / a broken Impl).
using System;
using System.Threading.Tasks;
using Kestrel.Slice;
using NUnit.Framework;

namespace Kestrel.Game.Tests
{
    public class GameCenterOfflineTests
    {
        IGameCenter previous;

        [SetUp]
        public void SaveImpl() => previous = GameCenterBridge.Impl;

        [TearDown]
        public void RestoreImpl() => GameCenterBridge.Impl = previous;

        [Test]
        public void DefaultImpl_AuthenticateAndSubmit_ReturnFalseWithoutThrowing()
        {
            GameCenterBridge.Impl = null;
            Assert.IsFalse(GameCenterBridge.TryAuthenticate().GetAwaiter().GetResult(), "auth");
            Assert.IsFalse(GameCenterBridge.TrySubmitScore(GameCenterBridge.MecoAltitudeBoard, 1200).GetAwaiter().GetResult(), "submit");
        }

        [Test]
        public void ThrowingImpl_AuthenticateAndSubmit_ReturnFalseWithoutThrowing()
        {
            GameCenterBridge.Impl = new ThrowingGameCenter();
            Assert.IsFalse(GameCenterBridge.TryAuthenticate().GetAwaiter().GetResult(), "auth throw");
            Assert.IsFalse(GameCenterBridge.TrySubmitScore("any.board", 1).GetAwaiter().GetResult(), "submit throw");
        }

        [Test]
        public void FaultedTaskImpl_AuthenticateAndSubmit_ReturnFalseWithoutThrowing()
        {
            GameCenterBridge.Impl = new FaultedGameCenter();
            Assert.IsFalse(GameCenterBridge.TryAuthenticate().GetAwaiter().GetResult(), "auth faulted");
            Assert.IsFalse(GameCenterBridge.TrySubmitScore("any.board", 1).GetAwaiter().GetResult(), "submit faulted");
        }

        sealed class ThrowingGameCenter : IGameCenter
        {
            public Task<bool> Authenticate() => throw new InvalidOperationException("offline");
            public Task<bool> SubmitScore(string leaderboardId, long score) => throw new InvalidOperationException("offline");
        }

        sealed class FaultedGameCenter : IGameCenter
        {
            public Task<bool> Authenticate() => Task.FromException<bool>(new InvalidOperationException("offline"));
            public Task<bool> SubmitScore(string leaderboardId, long score) => Task.FromException<bool>(new InvalidOperationException("offline"));
        }
    }
}
