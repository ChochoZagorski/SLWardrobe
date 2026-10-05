using System;
using System.Collections.Generic;
using System.Text;
using CommandSystem;
using UnityEngine;
using SLWardrobe.Common;

#if EXILED
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
#else
using LabApi.Features.Wrappers;
using LabApi.Features.Permissions;
using Log = LabApi.Features.Console.Logger;
#endif

namespace SLWardrobe.Commands
{
    public class BonesSubCommand : ICommand
    {
        public string Command => "bones";
        public string[] Aliases => new[] { "skeleton", "hitboxes" };
        public string Description =>
            "Lists all HitboxIdentity attach points on a player and their YAML key mappings. " +
            "Usage: slw bones <player> [wearerType]";

        // Maps RoleTypeId enum name -> WearerType key in BoneMappings.HitboxFallback
        private static readonly Dictionary<string, string> RoleToWearerType =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Scp173"]   = "SCP-173",
            ["Scp939"]   = "SCP-939",
            ["Scp106"]   = "SCP-106",
            ["Scp096"]   = "SCP-096",
            ["Scp049"]   = "SCP-049",
            ["Scp3114"]  = "SCP-3114",
            ["Scp0492"]  = "SCP-049-2",
            // All human/guard/CI/NTF roles fall back to "Human" below
        };

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
#if EXILED
            if (!sender.CheckPermission(PermissionNames.Admin))
#else
            if (!sender.HasPermissions(PermissionNames.Admin))
#endif
            {
                response = $"Missing permission: {PermissionNames.Admin}";
                return false;
            }

            if (arguments.Count < 1)
            {
                response = "Usage: slw bones <player> [wearerType]\n" +
                           "  Lists all HitboxIdentity attach points found on the player.\n" +
                           "  Use the output to fill in BoneName values in your suit YAML.\n" +
                           "  Optionally pass a WearerType (e.g. 'Human', 'SCP-939') to see YAML key mappings.";
                return false;
            }

            if (!Targets.TryResolveOne(arguments, 0, new List<Player>(), out var target, out string targetError))
            {
                response = targetError;
                return false;
            }

            // Determine WearerType - argument overrides auto-detect
            string wearerType = null;
            if (arguments.Count > 1)
            {
                wearerType = arguments.At(1);
            }
            else
            {
#if EXILED
                string roleName = target.Role.Type.ToString();
#else
                string roleName = target.Role.ToString();
#endif
                RoleToWearerType.TryGetValue(roleName, out wearerType);
                if (wearerType == null) wearerType = "Human";
            }

            var sb = new StringBuilder();
#if EXILED
            sb.AppendLine($"=== Hitboxes: {target.Nickname} ===");
            sb.AppendLine($"Role: {target.Role.Type}  |  WearerType: {wearerType}");
#else
            sb.AppendLine($"=== Hitboxes: {target.Nickname} ===");
            sb.AppendLine($"Role: {target.Role}  |  WearerType: {wearerType}");
#endif

            // Discover all HitboxIdentity on this player
            var hitboxes = target.GameObject.GetComponentsInChildren<HitboxIdentity>(true);

            // Build resolved names (handles duplicate names -> name_0, name_1, ...)
            var resolvedNames = new List<string>(hitboxes.Length);
            var nameCounts = new Dictionary<string, int>();
            foreach (var hb in hitboxes)
            {
                if (nameCounts.ContainsKey(hb.name)) nameCounts[hb.name]++;
                else nameCounts[hb.name] = 1;
            }
            var nameIndices = new Dictionary<string, int>();
            foreach (var hb in hitboxes)
            {
                if (nameCounts[hb.name] > 1)
                {
                    if (!nameIndices.ContainsKey(hb.name)) nameIndices[hb.name] = 0;
                    resolvedNames.Add($"{hb.name}_{nameIndices[hb.name]}");
                    nameIndices[hb.name]++;
                }
                else
                {
                    resolvedNames.Add(hb.name);
                }
            }

            sb.AppendLine();
            sb.AppendLine($"--- HitboxIdentity ({hitboxes.Length} found) ---");

            if (hitboxes.Length == 0)
            {
                sb.AppendLine("  (none - player may not be fully spawned yet)");
            }
            else
            {
                for (int i = 0; i < hitboxes.Length; i++)
                    sb.AppendLine($"  {resolvedNames[i]}");
            }

            // YAML key mapping for the detected WearerType
            sb.AppendLine();
            if (BoneMappings.HitboxFallback.TryGetValue(wearerType, out var boneMap) && boneMap.Count > 0)
            {
                sb.AppendLine($"--- YAML BoneName -> Hitbox mapping (WearerType: {wearerType}) ---");

                var resolvedSet = new HashSet<string>(resolvedNames);
                foreach (var kv in boneMap)
                {
                    bool found = resolvedSet.Contains(kv.Value);
                    string status = found ? "OK" : "NOT FOUND on this player";
                    sb.AppendLine($"  {kv.Key,-20} -> {kv.Value}  [{status}]");
                }

                // Unmapped hitboxes (on the player but not in the dictionary)
                var mappedValues = new HashSet<string>(boneMap.Values);
                var unmapped = new List<string>();
                foreach (var name in resolvedNames)
                    if (!mappedValues.Contains(name)) unmapped.Add(name);

                if (unmapped.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("--- Unmapped hitboxes (on player but no YAML key yet) ---");
                    foreach (var name in unmapped)
                        sb.AppendLine($"  {name}");
                }
            }
            else
            {
                sb.AppendLine($"--- YAML mapping (WearerType: {wearerType}) ---");
                sb.AppendLine($"  (empty - run this command and add the hitbox names above to");
                sb.AppendLine($"   BoneMappings.HitboxFallback[\"{wearerType}\"] in BoneMappings.cs)");
            }

            // Full transform hierarchy (secondary, useful for discovering new roles)
            sb.AppendLine();
            sb.AppendLine("--- Full transform hierarchy (depth 10) ---");
            var root = target.GameObject.transform;
            sb.AppendLine($"[ROOT] {root.name}  children={root.childCount}");
            WalkHierarchy(sb, root, root, "", 0, 10);

            response = sb.ToString();
            return true;
        }

        private static void WalkHierarchy(StringBuilder sb, Transform root, Transform current,
            string indent, int depth, int maxDepth)
        {
            if (depth >= maxDepth) return;
            for (int i = 0; i < current.childCount; i++)
            {
                var child = current.GetChild(i);
                bool isLast = i == current.childCount - 1;
                string connector   = isLast ? "└─ " : "├─ ";
                string childIndent = indent + (isLast ? "   " : "│  ");

                string extras = "";
                if (child.GetComponent<HitboxIdentity>() != null) extras += " [Hitbox]";
                if (child.childCount > 0) extras += $" ({child.childCount})";

                sb.AppendLine($"{indent}{connector}{child.name}{extras}");
                WalkHierarchy(sb, root, child, childIndent, depth + 1, maxDepth);
            }
        }
    }
}
