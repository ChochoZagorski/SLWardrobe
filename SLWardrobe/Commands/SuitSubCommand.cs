using System;
using CommandSystem;
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
            if (!sender.CheckPermission("slwardrobe.use"))
#else
            if (!sender.HasPermissions("slwardrobe.use"))
#endif
            {
                response = "Missing permission: slwardrobe.use";
                return false;
            }

            if (arguments.Count < 2)
            {
                response = "Usage: slw suit <player> <suitName>\nUse 'slw list suits' to see available suits.";
                return false;
            }

#if EXILED
            var target = Player.Get(arguments.At(0));

            if (target == null)
            {
                response = $"Player '{arguments.At(0)}' not found.";
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
#else
            if (!int.TryParse(arguments.At(0), out int playerId))
            {
                response = "Invalid player ID. ID must be an integer.";
                return false;
            }

            var target = Player.Get(playerId);
            if (target == null)
            {
                response = $"Player with ID '{playerId}' not found.";
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
#endif
            return true;
        }
    }
}