using System.Collections.Generic;
using UnityEngine;
using SLWardrobe.Common;

#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
#endif

namespace SLWardrobe
{
    /// <summary>
    /// Resolves YAML bone names to HitboxIdentity transforms on the player's rig.
    /// All roles - including humanoids - use HitboxIdentity colliders as attach points.
    /// Humanoid tables were verified via /slw bones on all roles on 2026-06-19 (game 14.2.7).
    /// </summary>
    public static class BoneMappings
    {
        /// <summary>
        /// Maps WearerType -> (YAML bone name -> HitboxIdentity.name on the rig).
        /// Duplicate HitboxIdentity names on the same rig are disambiguated by appending
        /// "_0", "_1", ... in child traversal order - exactly as done in TryGetHitboxBone.
        /// </summary>
        public static readonly Dictionary<string, Dictionary<string, string>> HitboxFallback =
            new Dictionary<string, Dictionary<string, string>>
        {
            // Humanoids
            // All human roles (ClassD/Scientist/NTF x4/Chaos x4/FacilityGuard/Tutorial) share
            // the identical 15-bone rig - verified across all roles on 2026-06-19.
            ["Human"] = new Dictionary<string, string>
            {
                ["hip"]          = "Hip",
                ["spine"]        = "SpineMiddle",
                ["chest"]        = "Chest",
                ["neck"]         = "Neck",
                ["head"]         = "Head",
                ["leftarm"]      = "Arm.L",
                ["leftforearm"]  = "Forearm.L",
                ["rightarm"]     = "Arm.R",
                ["rightforearm"] = "Forearm.R",
                ["leftthigh"]    = "Thigh.L",
                ["leftleg"]      = "leg.L",
                ["lefttoes"]     = "Toes.L",
                ["rightthigh"]   = "Thigh.R",
                ["rightleg"]     = "leg.R",
                ["righttoes"]    = "Toes.R",
            },
            // SCP-3114 uses the mixamorig: skeleton (10 hitboxes). Spine and Head differ from
            // SCP-049-2 despite similar rigs - separate table required.
            ["SCP-3114"] = new Dictionary<string, string>
            {
                ["spine"]        = "mixamorig:Spine",
                ["head"]         = "mixamorig:Head",
                ["leftarm"]      = "mixamorig:LeftArm",
                ["leftforearm"]  = "mixamorig:LeftForeArm",
                ["rightarm"]     = "mixamorig:RightArm",
                ["rightforearm"] = "mixamorig:RightForeArm",
                ["leftthigh"]    = "mixamorig:LeftUpLeg",
                ["leftleg"]      = "mixamorig:LeftLeg",
                ["rightthigh"]   = "mixamorig:RightUpLeg",
                ["rightleg"]     = "mixamorig:RightLeg",
            },
            // SCP-049-2 shares the mixamorig: skeleton but uses Spine2 and HeadTop_End.
            ["SCP-049-2"] = new Dictionary<string, string>
            {
                ["spine"]        = "mixamorig:Spine2",
                ["head"]         = "mixamorig:HeadTop_End",
                ["leftarm"]      = "mixamorig:LeftArm",
                ["leftforearm"]  = "mixamorig:LeftForeArm",
                ["rightarm"]     = "mixamorig:RightArm",
                ["rightforearm"] = "mixamorig:RightForeArm",
                ["leftthigh"]    = "mixamorig:LeftUpLeg",
                ["leftleg"]      = "mixamorig:LeftLeg",
                ["rightthigh"]   = "mixamorig:RightUpLeg",
                ["rightleg"]     = "mixamorig:RightLeg",
            },

            // Non-humanoid SCPs
            ["SCP-173"] = new Dictionary<string, string>
            {
                ["body"]       = "Center",
                ["head"]       = "Heads",
                ["tophead"]    = "Top head",
                ["backarm"]    = "Little arm",
                ["leftarmup"]  = "Arm pair 1",
                ["leftarmdown"]= "Arm pair 2",
                ["rightarm"]   = "Big arm",
                ["backleg"]    = "Big leg",
                ["rightleg"]   = "Middle leg",
                ["leftleg"]    = "Little leg",
                ["frontpelvis"]= "Front pelvis"
            },
            ["SCP-939"] = new Dictionary<string, string>
            {
                ["head"]            = "Hitbox (10)_0",
                ["chest"]           = "Hitbox (9)",
                ["stomach"]         = "Hitbox (8)_1",
                ["leftshoulder"]    = "Hitbox (10)_1",
                ["leftarm"]         = "Hitbox (13)",
                ["leftforearm"]     = "Hitbox (14)",
                ["lefthand"]        = "Hitbox (17)",
                ["rightshoulder"]   = "Hitbox (11)",
                ["rightarm"]        = "Hitbox (12)",
                ["rightforearm"]    = "Hitbox (15)",
                ["righthand"]       = "Hitbox (16)",
                ["pelvis"]          = "Hitbox (7)_0",
                ["leftthighupper"]  = "Hitbox",
                ["leftthighlower"]  = "Hitbox (1)",
                ["leftlegupper"]    = "Hitbox (8)_0",
                ["leftleglower"]    = "Hitbox (7)_1",
                ["leftlegfoot"]     = "Hitbox (6)",
                ["rightthighupper"] = "Hitbox (2)",
                ["rightthighlower"] = "Hitbox (3)",
                ["rightlegupper"]   = "Hitbox (4)",
                ["rightleglower"]   = "Hitbox (5)",
                ["rightlegfoot"]    = "Hitbox (7)_2",
            },
            ["SCP-106"] = new Dictionary<string, string>
            {
                ["head"]      = "Capsule_4",
                ["chest"]     = "Capsule_9",
                ["stomach"]   = "Capsule_10",
                ["leftarm"]   = "Capsule_6",
                ["leftforearm"]= "Capsule_5",
                ["rightarm"]  = "Capsule_8",
                ["rightforearm"]= "Capsule_7",
                ["leftthigh"] = "Capsule_1",
                ["leftleg"]   = "Capsule_0",
                ["rightthigh"]= "Capsule_3",
                ["rightleg"]  = "Capsule_2"
            },
            ["SCP-096"] = new Dictionary<string, string>
            {
                ["root"] = "Hitbox"
            },
            ["SCP-049"] = new Dictionary<string, string>
            {
                ["root"] = "Hitbox"
            }
        };

        /// <summary>
        /// Returns the HitboxIdentity Transform for the given (wearerType, boneName) pair,
        /// or null if the mapping is unknown or the hitbox is not found on this player.
        /// </summary>
        public static Transform GetBoneTransform(Player player, string wearerType, string boneName)
        {
            return TryGetHitboxBone(player.GameObject, wearerType, boneName.ToLower());
        }

        /// <summary>
        /// Resolves bones via HitboxIdentity colliders. Handles duplicate names by appending
        /// "_0", "_1", etc. in child traversal order, matching the naming in HitboxFallback.
        /// </summary>
        private static Transform TryGetHitboxBone(GameObject playerObject, string wearerType, string friendlyName)
        {
            if (!HitboxFallback.TryGetValue(wearerType, out var boneMap))
            {
                GatedLogger.Debug($"[BoneMappings] Unknown wearerType '{wearerType}'");
                return null;
            }

            if (!boneMap.TryGetValue(friendlyName, out var hitboxName))
            {
                GatedLogger.Debug($"[BoneMappings] No mapping for bone '{friendlyName}' in wearerType '{wearerType}'");
                return null;
            }

            var hitboxes = playerObject.GetComponentsInChildren<HitboxIdentity>(true);
            var nameCounts = new Dictionary<string, int>();
            foreach (var hb in hitboxes)
            {
                if (nameCounts.ContainsKey(hb.name)) nameCounts[hb.name]++;
                else nameCounts[hb.name] = 1;
            }

            var currentIndex = new Dictionary<string, int>();
            foreach (var hb in hitboxes)
            {
                string resolvedName;
                if (nameCounts[hb.name] > 1)
                {
                    if (!currentIndex.ContainsKey(hb.name)) currentIndex[hb.name] = 0;
                    resolvedName = $"{hb.name}_{currentIndex[hb.name]}";
                    currentIndex[hb.name]++;
                }
                else
                {
                    resolvedName = hb.name;
                }

                if (resolvedName == hitboxName)
                    return hb.transform;
            }

            GatedLogger.Debug($"[BoneMappings] HitboxIdentity '{hitboxName}' not found on player for wearerType '{wearerType}'");
            return null;
        }

        public static List<string> GetAvailableBones(string wearerType)
        {
            if (HitboxFallback.TryGetValue(wearerType, out var bones))
                return new List<string>(bones.Keys);
            return new List<string>();
        }

        public static List<string> GetWearerTypes()
            => new List<string>(HitboxFallback.Keys);
    }
}
