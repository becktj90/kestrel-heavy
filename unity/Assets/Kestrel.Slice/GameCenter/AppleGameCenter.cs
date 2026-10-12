// Game Center via Apple's Unity plug-ins (Apple.Core + Apple.GameKit, built from
// https://github.com/apple/unityplugins and added as local tarballs; see unity/README.md).
// This assembly compiles ONLY when the com.apple.unityplugin.gamekit package is installed
// (defineConstraints KH_GAMEKIT), so the project opens and builds without it.
// NOTE: written against the plug-in's documented API (GKLocalPlayer.Authenticate,
// GKLeaderboard.LoadLeaderboards, GKLeaderboard.SubmitScore); not compiled here. Verify on first open.
using System;
using System.Threading.Tasks;
using Apple.GameKit;
using Apple.GameKit.Leaderboards;
using UnityEngine;

namespace Kestrel.Slice
{
    public sealed class AppleGameCenter : IGameCenter
    {
        GKLocalPlayer player;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameCenterBridge.Impl = new AppleGameCenter();

        public async Task<bool> Authenticate()
        {
            try { player = await GKLocalPlayer.Authenticate(); return player != null && player.IsAuthenticated; }
            catch (Exception e) { Debug.LogWarning("[GameCenter] auth failed: " + e.Message); return false; }
        }

        public async Task<bool> SubmitScore(string leaderboardId, long score)
        {
            try
            {
                if (player == null && !await Authenticate()) return false;
                var boards = await GKLeaderboard.LoadLeaderboards(leaderboardId);
                if (boards == null || boards.Count == 0) { Debug.LogWarning("[GameCenter] leaderboard not found: " + leaderboardId); return false; }
                await boards[0].SubmitScore(score, 0, GKLocalPlayer.Local);
                return true;
            }
            catch (Exception e) { Debug.LogWarning("[GameCenter] submit failed: " + e.Message); return false; }
        }
    }
}
