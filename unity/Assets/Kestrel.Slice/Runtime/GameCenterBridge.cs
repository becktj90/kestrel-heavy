// Engine-side Game Center seam. The real implementation lives in the optional
// Kestrel.Slice.GameCenter assembly (Apple's GameKit plug-in); without it this logs only.
using System.Threading.Tasks;
using UnityEngine;

namespace Kestrel.Slice
{
    public interface IGameCenter
    {
        Task<bool> Authenticate();
        Task<bool> SubmitScore(string leaderboardId, long score);
    }

    public static class GameCenterBridge
    {
        /// <summary>Sandbox test leaderboard created in App Store Connect for the dev bundle id only.</summary>
        public const string MecoAltitudeBoard = "kh.unityproto.meco_alt_m";
        public static IGameCenter Impl = new NullGameCenter();

        sealed class NullGameCenter : IGameCenter
        {
            public Task<bool> Authenticate() { Debug.Log("[GameCenter] plug-in not installed: auth skipped"); return Task.FromResult(false); }
            public Task<bool> SubmitScore(string id, long s) { Debug.Log($"[GameCenter] plug-in not installed: would submit {s} to {id}"); return Task.FromResult(false); }
        }
    }
}
