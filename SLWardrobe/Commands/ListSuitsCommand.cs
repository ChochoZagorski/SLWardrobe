using System;
using System.Linq;
using System.Text;
using CommandSystem;
using Exiled.Permissions.Extensions;

namespace SLWardrobe.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class ListSuitsCommand : ICommand
    {
        public string Command => "listsuits";
        public string[] Aliases => new[] { "ls", "suits" };
        public string Description => "List all available suits";
        
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission("slwardrobe.suits"))
            {
                response = "You can't list all available suits, you don't have \"slwardrobe.suits\" permission.";
                return false;
            }
            
            var suits = SLWardrobe.Instance.Config.Suits;
            
            if (suits == null || suits.Count == 0)
            {
                response = "No suits are configured.";
                return true;
            }
            
            var sb = new StringBuilder();
            sb.AppendLine($"Available Suits ({suits.Count} total):");
            sb.AppendLine("================================");
            
            foreach (var suit in suits.OrderBy(s => s.Key))
            {
                sb.AppendLine($"Name: {suit.Key}");
                sb.AppendLine($"Description: {suit.Value.Description}");
    
                if (arguments.Count > 0 && arguments.At(0).ToLower() == "detailed")
                {
                    sb.AppendLine($"Parts: {suit.Value.Parts.Count}");
                    foreach (var part in suit.Value.Parts)
                    {
                        string visibility = part.HideForWearer ? "Hidden from Wearer" : "Visible to Wearer";
                        sb.AppendLine($"  - {part.SchematicName} on {part.BoneName} ({visibility})");
                    }
                }
    
                sb.AppendLine("--------------------------------");
            }
            
            sb.AppendLine("\nUsage: suit <playerid> <suitname>");
            
            if (arguments.Count == 0)
            {
                sb.AppendLine("Tip: Use 'listsuits detailed' to see all parts in each suit.");
            }
            
            response = sb.ToString();
            return true;
        }
    }
}