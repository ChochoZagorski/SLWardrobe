using System;
using System.Collections.Generic;
using System.Text;
using CommandSystem;
using SLWardrobe.Common;

#if EXILED
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
#else
using LabApi.Features.Wrappers;
using LabApi.Features.Permissions;
#endif

namespace SLWardrobe.Commands
{
    // Grammar favors the common case: one player requests, one admin approves.
    // `slw fade yes` with no target resolves to the sole pending request, so approval
    // costs two words instead of a numeric-ID lookup-and-retype round trip.
    public class FadeSubCommand : ICommand, IUsageProvider
    {
        public string Command => "fade";
        public string[] Aliases => new[] { "f" };
        public string Description => "Manages body-fade requests. Usage: slw fade [yes|no|off|on] [player]";
        public string[] Usage => new[] { "yes|no|off|on", "player" };

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            string adminPermission = SLWardrobe.Instance.Config.FadeRequest.AdminPermission;
#if EXILED
            if (!sender.CheckPermission(adminPermission))
#else
            if (!sender.HasPermissions(adminPermission))
#endif
            { response = $"Missing permission: {adminPermission}"; return false; }

            var handler = SLWardrobe.Instance.FadeHandler;
            if (handler == null) { response = "Fade request system is not active."; return false; }

            if (arguments.Count == 0)
                return StatusBoard(handler, out response);

            string action = arguments.At(0).ToLowerInvariant();
            switch (action)
            {
                case "yes": case "y": case "ok": case "grant": case "approve":
                    return Approve(arguments, handler, out response);
                case "no": case "n": case "deny": case "reject":
                    return DenyRequest(arguments, handler, out response);
                case "off": case "stop": case "revoke": case "unfade":
                    return RevokeAction(arguments, handler, out response);
                case "on": case "force":
                    return ForceGrant(arguments, handler, out response);
                default:
                    response = $"Unknown action '{action}'. Usage: slw fade [yes|no|off|on] [player]";
                    return false;
            }
        }

        private static bool StatusBoard(FadeRequestHandler handler, out string response)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Pending fade requests ({handler.PendingRequests.Count}):");
            if (handler.PendingRequests.Count == 0) sb.AppendLine("  (none)");
            foreach (var kvp in handler.PendingRequests)
            {
                int secondsAgo = (int)(DateTime.UtcNow - kvp.Value).TotalSeconds;
                sb.AppendLine($"  [{Messaging.PlayerId(kvp.Key)}] {kvp.Key.Nickname} - {secondsAgo}s ago");
            }
            response = sb.ToString();
            return true;
        }

        // yes/no default to the sole pending request when no target is given; with an explicit
        // target, any player can be named (not just those with a pending request).
        private static bool ResolveTarget(ArraySegment<string> arguments, FadeRequestHandler handler, out Player target, out string error)
        {
            if (arguments.Count > 1)
                return Targets.TryResolveOne(arguments, 1, new List<Player>(), out target, out error);
            return handler.TryGetSolePending(out target, out error);
        }

        private static bool Approve(ArraySegment<string> arguments, FadeRequestHandler handler, out string response)
        {
            if (!ResolveTarget(arguments, handler, out var target, out string targetError)) { response = targetError; return false; }
            if (!handler.Grant(target, requireRequest: true, out string error)) { response = error; return false; }
            response = $"Approved fade request for {target.Nickname}.";
            return true;
        }

        private static bool DenyRequest(ArraySegment<string> arguments, FadeRequestHandler handler, out string response)
        {
            if (!ResolveTarget(arguments, handler, out var target, out string targetError)) { response = targetError; return false; }
            if (!handler.Deny(target, out string error)) { response = error; return false; }
            response = $"Denied fade request for {target.Nickname}.";
            return true;
        }

        private static bool RevokeAction(ArraySegment<string> arguments, FadeRequestHandler handler, out string response)
        {
            if (arguments.Count > 1 && arguments.At(1).Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                int count = handler.RevokeAll("Revoked by an administrator.");
                response = $"Revoked fade for {count} player(s).";
                return true;
            }

            if (arguments.Count < 2) { response = "Usage: slw fade off <player|all>"; return false; }

            if (!Targets.TryResolveOne(arguments, 1, new List<Player>(), out var target, out string targetError))
            {
                response = targetError;
                return false;
            }
            if (!handler.Revoke(target, "Revoked by an administrator."))
            {
                response = $"{target.Nickname} does not have an active fade grant.";
                return false;
            }
            response = $"Revoked fade for {target.Nickname}.";
            return true;
        }

        private static bool ForceGrant(ArraySegment<string> arguments, FadeRequestHandler handler, out string response)
        {
            if (arguments.Count < 2) { response = "Usage: slw fade on <player>"; return false; }

            if (!Targets.TryResolveOne(arguments, 1, new List<Player>(), out var target, out string targetError))
            {
                response = targetError;
                return false;
            }
            if (!handler.Grant(target, requireRequest: false, out string error)) { response = error; return false; }
            response = $"Granted fade for {target.Nickname} (no prior request).";
            return true;
        }
    }
}
