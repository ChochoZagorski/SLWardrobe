using System;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;

namespace SLWardrobe.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class SuitCommand : ICommand
    {
        public string Command => "suit";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Apply a suit to a player";
        
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission("slwardrobe.suits"))
            {
                response = "You can't wear suits, you don't have \"slwardrobe.suits\" permission.";
                return false;
            }
            
            if (arguments.Count < 2)
            {
                response = "Usage: suit <playerid> <suitname>\nExample: suit 2 example_suit";
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

            string suitName = arguments.At(1);

            SLWardrobe.Instance.ApplySuit(target, suitName);
            
            response = $"Applied suit '{suitName}' to {target.Nickname}";
            return true;
        }
    }
}