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
    public class RemoveSubCommand : ICommand
    {
        public string Command => "remove";
        public string[] Aliases => new[] { "rm" };
        public string Description => "Removes a suit from a player.";

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

            if (arguments.Count < 1)
            {
                response = "Usage: slw remove <player>";
                return false;
            }

#if EXILED
            var target = Player.Get(arguments.At(0));
            if (target == null)
            {
                response = $"Player '{arguments.At(0)}' not found.";
                return false;
            }

            string suitName = SLWardrobe.Instance.GetPlayerSuitName(target);
            SuitBinder.RemoveSuit(target);
            SuitBinder.SetPlayerInvisibility(target, false);

            response = string.IsNullOrEmpty(suitName)
                ? $"{target.Nickname} had no tracked suit, cleanup attempted."
                : $"Removed '{suitName}' from {target.Nickname}.";
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

            string suitName = SLWardrobe.Instance.GetPlayerSuitName(target);
            SuitBinder.RemoveSuit(target);
            SuitBinder.SetPlayerInvisibility(target, false);

            response = string.IsNullOrEmpty(suitName)
                ? $"{target.Nickname} had no tracked suit, cleanup attempted."
                : $"Removed '{suitName}' from {target.Nickname}.";
#endif
            return true;
        }
    }
}