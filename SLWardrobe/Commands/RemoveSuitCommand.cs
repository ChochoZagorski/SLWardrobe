using System;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;

namespace SLWardrobe.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class RemoveSuitCommand : ICommand
    {
        public string Command => "removesuit";
        public string[] Aliases => new[] { "rms" };
        public string Description => "Remove a suit from a player";
        
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission("slwardrobe.suits"))
            {
                response = "You can't remove suits, you don't have \"slwardrobe.suits\" permission.";
                return false;
            }
            
            if (arguments.Count < 1)
            {
                response = "Usage: removesuit <playerid> \nExample: removesuit 2";
                return false;
            }

            if (!int.TryParse(arguments.At(0), out int playerId))
            {
                response = "Invalid player ID. Use a number.";
                return false;
            }
            
            Player target = Player.Get(playerId);
            if (target == null)
            {
                response = $"Player with ID {playerId} not found.";
                return false;
            }
            string suitName = SLWardrobe.Instance.GetPlayerSuitName(target);
            
            if (string.IsNullOrEmpty(suitName))
            {
                response = $"{target.Nickname} is not wearing a suit. Still gonna try to remove it.";
            }
            else
            {
                response = $"Removed {suitName} from {target.Nickname}";
            }

            SuitBinder.RemoveSuit(target);
			SuitBinder.SetPlayerInvisibility(target, false);
            
            return true;
        }
    }
}