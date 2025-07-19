using System;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;

namespace SLWardrobe
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class SuitCommand : ICommand
    {
        public string Command => "suit";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Apply a suit to a player";
        
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 2)
            {
                response = "Usage: suit <playerid> <suitname>\nExample: suit 2 test";
                return false;
            }
            
            // Get player
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
            
            // Get suit name
            string suitName = arguments.At(1);
            
            // Apply suit
            SLWardrobe.Instance.ApplySuit(target, suitName);
            
            response = $"Applied suit '{suitName}' to {target.Nickname}";
            return true;
        }
    }
}