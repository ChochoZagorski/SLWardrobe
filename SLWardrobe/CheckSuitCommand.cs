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
            
            // Check for suit data in the new system
            var suitData = SuitBinder.GetSuitData(target);
            
            if (suitData == null)
            {
                response = $"{target.Nickname} has no suit equipped.";
                return true;
            }
            
            response = $"{target.Nickname} has a suit with {suitData.Parts.Count} parts:\n";
            
            int activeCount = 0;
            int destroyedCount = 0;
            
            foreach (var part in suitData.Parts)
            {
                if (part != null && part.GameObject != null)
                {
                    activeCount++;
                    var pos = part.GameObject.transform.position;
                    var boneName = part.TargetBone != null ? part.TargetBone.name : "NONE";
                    var active = part.GameObject.activeSelf ? "Active" : "Inactive";
                    
                    response += $"- {part.Binding.SchematicName} tracking {boneName} - {active} at ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})\n";
                    
                    // Check for AdminToyBase component
                    var adminToy = part.GameObject.GetComponent<AdminToys.AdminToyBase>();
                    if (adminToy != null)
                    {
                        response += $"  AdminToy: Present, Scale: {adminToy.Scale}\n";
                    }
                }
                else
                {
                    destroyedCount++;
                    response += $"- {part.Binding?.SchematicName ?? "Unknown"} - DESTROYED\n";
                }
            }
            
            response += $"\nSummary: {activeCount} active, {destroyedCount} destroyed";
            
            // Check for the updater component
            var updater = target.GameObject.GetComponent<SuitUpdater>();
            response += $"\nSuitUpdater component: {(updater != null ? "Present" : "Missing")}";
            
            return true;
        }
    }
}