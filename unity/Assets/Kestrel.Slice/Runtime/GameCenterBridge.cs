// Engine-side Game Center seam. The real implementation lives in the optional
// Kestrel.Slice.GameCenter assembly (Apple's GameKit plug-in); without it this logs only.
// G-2: every public entry returns false on failure and never throws, including offline
// and when the plug-in is absent (1.0 ships without Game Center, DECISIONS #98).
using System;
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

        /// <summary>Authenticate without throwing. Offline, missing plug-in, or a broken Impl returns false.</summary>
        public static Task<bool> TryAuthenticate() => Safe("auth", impl => impl.Authenticate());

        /// <summary>Submit a score without throwing. Offline, missing plug-in, or a broken Impl returns false.</summary>
        public static Task<bool> TrySubmitScore(string leaderboardId, long score)
            => Safe("submit", impl => impl.SubmitScore(leaderboardId, score));

        static async Task<bool> Safe(string verb, Func<IGameCenter, Task<bool>> work)
        {
            try
            {
                var impl = Impl ?? new NullGameCenter();
                var task = work(impl);
                if (task == null) return false;
                return await task;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GameCenter] " + verb + " failed: " + e.Message);
                return false;
            }
        }

        sealed class NullGameCenter : IGameCenter
        {
            public Task<bool> Authenticate() { Debug.Log("[GameCenter] plug-in not installed: auth skipped"); return Task.FromResult(false); }
            public Task<bool> SubmitScore(string id, long s) { Debug.Log($"[GameCenter] plug-in not installed: would submit {s} to {id}"); return Task.FromResult(false); }
        }
    }
}
