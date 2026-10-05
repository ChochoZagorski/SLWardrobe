using System;
using System.Collections.Generic;
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
    public class CheckSubCommand : ICommand
    {
        public string Command => "check";
        public string[] Aliases => new[] { "cs", "info" };
        public string Description => "Checks a player's suit status.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
#if EXILED
            if (!sender.CheckPermission(PermissionNames.Use))
#else
            if (!sender.HasPermissions(PermissionNames.Use))
#endif
            { response = $"Missing permission: {PermissionNames.Use}"; return false; }

            if (arguments.Count < 1) { response = "Usage: slw check <player>"; return false; }

            if (!Targets.TryResolveOne(arguments, 0, new List<Player>(), out var target, out string targetError))
            {
                response = targetError;
                return false;
            }

            string suitName = SLWardrobe.Instance.GetPlayerSuitName(target);
            var suitData = CosmeticBinder.GetSuitData(target);

            if (string.IsNullOrEmpty(suitName))
            {
                response = $"{target.Nickname} has no suit equipped.";
            }
            else
            {
                int activeParts = 0;
                if (suitData != null)
                    foreach (var part in suitData.Parts)
                        if (part.SchematicRoot != null) activeParts++;

                response = $"{target.Nickname} is wearing: {suitName} ({activeParts} active parts)";
            }
            return true;
        }
    }
}
