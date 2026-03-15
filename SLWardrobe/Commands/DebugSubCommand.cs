using System;
using System.Text;
using CommandSystem;
#if EXILED
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
#else
using LabApi.Features.Wrappers;
using LabApi.Features.Permissions;
#endif
using SLWardrobe.Weapons;

namespace SLWardrobe.Commands
{
    public class DebugSubCommand : ICommand
    {
        public string Command => "debug";
        public string[] Aliases => new[] { "dbg" };
        public string Description => "Shows debug info. Usage: slw debug [player]";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
#if EXILED
            if (!sender.CheckPermission("slwardrobe.admin"))
#else
            if (!sender.HasPermissions("slwardrobe.admin"))
#endif
            {
                response = "Missing permission: slwardrobe.admin";
                return false;
            }

            var config = SLWardrobe.Instance.Config;
            var sb = new StringBuilder();
            sb.AppendLine($"=== SLWardrobe v{SLWardrobe.Instance.Version} ({SLWardrobe.BuildFramework}) ===");

#if EXILED
            sb.AppendLine($"Round: {(Round.IsStarted ? "Active" : "Inactive")}");
#else
            sb.AppendLine($"Round: {(Round.IsRoundStarted ? "Active" : "Inactive")}");
#endif

            sb.AppendLine($"Weapon System: {WeaponBinder.GetDebugStatus()}");
            sb.AppendLine($"LOD: {(config.LodDistance > 0 ? $"{config.LodDistance}m" : "Disabled")}");
            sb.AppendLine($"SSSS: {(config.Ssss.Enabled ? "Enabled" : "Disabled")}");
            sb.AppendLine();

            sb.AppendLine($"Loaded Suits ({ConfigLoader.Suits.Count}):");
            foreach (var suit in ConfigLoader.Suits)
                sb.AppendLine($"  - {suit.Key}: {suit.Value.WearerType}, {suit.Value.Parts.Count} parts");

            sb.AppendLine();
            sb.AppendLine($"Loaded Weapons ({ConfigLoader.Weapons.Count}):");
            foreach (var weapon in ConfigLoader.Weapons)
                sb.AppendLine($"  - {weapon.Key}: {weapon.Value.Detection.Type} = {weapon.Value.Detection.Identifier}");

            sb.AppendLine();
            sb.AppendLine($"Active Weapons ({WeaponBinder.ActiveWeapons.Count}):");
            if (WeaponBinder.ActiveWeapons.Count == 0)
            {
                sb.AppendLine("  (none)");
            }
            else
            {
                foreach (var kvp in WeaponBinder.ActiveWeapons)
                    sb.AppendLine($"  - {kvp.Key.Nickname}: {kvp.Value.WeaponName} ({kvp.Value.Parts.Count} parts)");
            }

            if (arguments.Count > 0)
            {
                var target = Player.Get(arguments.At(0));
                if (target != null)
                {
                    sb.AppendLine();
                    sb.AppendLine($"=== Player: {target.Nickname} ===");
                    sb.AppendLine($"  Alive: {target.IsAlive}");

#if EXILED
                    sb.AppendLine($"  Role: {target.Role.Type}");
#else
                    sb.AppendLine($"  Role: {target.Role}");
#endif

                    sb.AppendLine($"  Current Item: {target.CurrentItem?.Type.ToString() ?? "None"}");

                    string suitName = SLWardrobe.Instance.GetPlayerSuitName(target);
                    sb.AppendLine($"  Suit: {suitName ?? "(none)"}");

                    var suitData = SuitBinder.GetSuitData(target);
                    if (suitData != null)
                    {
                        int active = 0;
                        foreach (var part in suitData.Parts)
                        {
                            if (part.GameObject != null) active++;
                        }
                        sb.AppendLine($"  Suit Parts: {active}/{suitData.Parts.Count} active");
                    }
                }
                else
                {
                    sb.AppendLine($"\nPlayer '{arguments.At(0)}' not found.");
                }
            }

            response = sb.ToString();
            return true;
        }
    }
}