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
    public class SuitSubCommand : ICommand
    {
        public string Command => "suit";
        public string[] Aliases => new[] { "s", "wear", "apply" };
        public string Description => "Applies a suit to a player.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
#if EXILED
            if (!sender.CheckPermission(PermissionNames.Use))
#else
            if (!sender.HasPermissions(PermissionNames.Use))
#endif
            {
                response = $"Missing permission: {PermissionNames.Use}";
                return false;
            }

            if (arguments.Count < 2)
            {
                response = "Usage: slw suit <player> <suitName>\nUse 'slw list suits' to see available suits.";
                return false;
            }

            if (!Targets.TryResolveOne(arguments, 0, new List<Player>(), out var target, out string targetError))
            {
                response = targetError;
                return false;
            }

            string suitName = arguments.At(1);
            if (ConfigLoader.GetSuit(suitName) == null)
            {
                response = $"Suit '{suitName}' not found.\nUse 'slw list suits' to see available suits.";
                return false;
            }

            SLWardrobe.Instance.ApplySuit(target, suitName);
            response = $"Applying suit '{suitName}' to {target.Nickname}.";
            return true;
        }
    }
}
