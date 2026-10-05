using System;
using System.Collections.Generic;
using Utils;

#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
#endif

namespace SLWardrobe.Common
{
    // Wraps the game's own RA target parser (the same one ban/kick use) so every command
    // accepts player names and dot-separated multi-target IDs identically on both builds.
    // LabApi's Player.Get(string) is userId-only, so hand-rolled parsing used to degrade
    // to numeric-ID-only on that build; RAUtils lives in Assembly-CSharp and works on both.
    public static class Targets
    {
        public static bool TryResolve(ArraySegment<string> args, int startIndex, List<Player> results, out string error)
        {
            results.Clear();

            if (startIndex >= args.Count)
            {
                error = "No target specified.";
                return false;
            }

            string query = args.At(startIndex);
            var hubs = RAUtils.ProcessPlayerIdOrNamesList(args, startIndex, out _);
            foreach (var hub in hubs)
            {
                var player = Player.Get(hub);
                if (player != null && !results.Contains(player)) results.Add(player);
            }

            if (results.Count == 0)
            {
                error = $"No players matched '{query}'.";
                return false;
            }

            error = null;
            return true;
        }

        public static bool TryResolveOne(ArraySegment<string> args, int startIndex, List<Player> buffer, out Player player, out string error)
        {
            if (!TryResolve(args, startIndex, buffer, out error)) { player = null; return false; }
            player = buffer[0];
            return true;
        }
    }
}
