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
    public class RemoveSubCommand : ICommand
    {
        public string Command => "remove";
        public string[] Aliases => new[] { "rm" };
        public string Description => "Removes a suit from a player.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
#if EXILED
            if (!sender.CheckPermission(PermissionNames.Use))
#else
            if (!sender.HasPermissions(PermissionNames.Use))
#endif
            { response = $"Missing permission: {PermissionNames.Use}"; return false; }

            if (arguments.Count < 1) { response = "Usage: slw remove <player>"; return false; }

            if (!Targets.TryResolveOne(arguments, 0, new List<Player>(), out var target, out string targetError))
            {
                response = targetError;
                return false;
            }

            string suitName = SLWardrobe.Instance.GetPlayerSuitName(target);
            CosmeticBinder.RemoveSuit(target);
            CosmeticBinder.SetPlayerInvisibility(target, false);

            response = string.IsNullOrEmpty(suitName)
                ? $"{target.Nickname} had no tracked suit, cleanup attempted."
                : $"Removed '{suitName}' from {target.Nickname}.";
            return true;
        }
    }
}
