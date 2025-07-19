using System;
using System.Linq;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;

namespace SLWardrobe
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class CheckSuitCommand : ICommand
    {
        public string Command => "checksuit";
        public string[] Aliases => new[] { "cs" };
        public string Description => "Check suit status for a player";
        
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission("slwardrobe.suits"))
            {
                response = "You can't check player for their attire, you don't have \"slwardrobe.suits\" permission.";
                return false;
            }
            
            if (arguments.Count < 1)
            {
                response = "Usage: checksuit <playerid>";
                return false;
            }
            
            if (!int.TryParse(arguments.At(0), out int playerId))
            {
                response = "Invalid player ID.";
                return false;
            }
            
            Player target = Player.Get(playerId);
            if (target == null)
            {
                response = $"Player {playerId} not found.";
                return false;
            }

            string suitName = SLWardrobe.Instance.GetPlayerSuitName(target);
            
            if (string.IsNullOrEmpty(suitName))
            {
                response = $"{target.Nickname} has no suit equipped.";
            }
            else
            {
                response = $"{target.Nickname} is wearing: {suitName}";
            }
            
            return true;
        }
    }
}