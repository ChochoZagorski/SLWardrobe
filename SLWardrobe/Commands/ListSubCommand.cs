using System;
using System.Text;
using CommandSystem;
#if EXILED
using Exiled.Permissions.Extensions;
#else
using LabApi.Features.Permissions;
#endif

namespace SLWardrobe.Commands
{
    public class ListSubCommand : ICommand
    {
        public string Command => "list";
        public string[] Aliases => new[] { "ls" };
        public string Description => "Lists loaded suits and/or weapons. Usage: slw list [suits|weapons]";

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

            string filter = arguments.Count > 0 ? arguments.At(0).ToLower() : "all";
            bool showSuits = filter == "all" || filter == "suits" || filter == "suit";
            bool showWeapons = filter == "all" || filter == "weapons" || filter == "weapon";

            if (!showSuits && !showWeapons)
            {
                response = "Usage: slw list [suits|weapons]\nOmit argument to show both.";
                return false;
            }

            var sb = new StringBuilder();

            if (showSuits)
            {
                sb.AppendLine($"=== Suits ({ConfigLoader.Suits.Count}) ===");
                if (ConfigLoader.Suits.Count == 0)
                {
                    sb.AppendLine("  (none)");
                }
                else
                {
                    foreach (var kvp in ConfigLoader.Suits)
                    {
                        var suit = kvp.Value;
                        sb.AppendLine($"  - {kvp.Key}");
                        sb.AppendLine($"      Type: {suit.WearerType} | Parts: {suit.Parts.Count} | Invisible: {suit.MakeWearerInvisible}");
                        if (!string.IsNullOrEmpty(suit.Description) && suit.Description != "Custom suit")
                            sb.AppendLine($"      {suit.Description}");
                    }
                }
            }

            if (showSuits && showWeapons)
                sb.AppendLine();

            if (showWeapons)
            {
                sb.AppendLine($"=== Weapons ({ConfigLoader.Weapons.Count}) ===");
                if (ConfigLoader.Weapons.Count == 0)
                {
                    sb.AppendLine("  (none)");
                }
                else
                {
                    foreach (var kvp in ConfigLoader.Weapons)
                    {
                        var weapon = kvp.Value;
                        sb.AppendLine($"  - {kvp.Key}");
                        sb.AppendLine($"      Detection: {weapon.Detection.Type} = {weapon.Detection.Identifier} | Parts: {weapon.Parts.Count}");
                    }
                }
            }

            response = sb.ToString();
            return true;
        }
    }
}