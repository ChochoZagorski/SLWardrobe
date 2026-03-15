using System.Collections.Generic;
using UnityEngine;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;

#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
using Log = LabApi.Features.Console.Logger;
#endif

namespace SLWardrobe
{
    /// Resolves friendly bone names to transforms on the player's animated model or hitbox rig.
    /// Humanoid rigs use Animator.GetBoneTransform; non-humanoid SCPs fall back to HitboxIdentity colliders.
    public static class BoneMappings
    {
        private static readonly Dictionary<string, HumanBodyBones> HumanoidBones = new Dictionary<string, HumanBodyBones>
        {
            ["head"]          = HumanBodyBones.Head,
            ["neck"]          = HumanBodyBones.Neck,
            ["chest"]         = HumanBodyBones.Chest,
            ["upperchest"]    = HumanBodyBones.UpperChest,
            ["body"]          = HumanBodyBones.Spine,
            ["spine"]         = HumanBodyBones.Spine,
            ["hip"]           = HumanBodyBones.Hips,
            ["hips"]          = HumanBodyBones.Hips,
            ["leftshoulder"]  = HumanBodyBones.LeftShoulder,
            ["rightshoulder"] = HumanBodyBones.RightShoulder,
            ["leftarm"]       = HumanBodyBones.LeftUpperArm,
            ["rightarm"]      = HumanBodyBones.RightUpperArm,
            ["leftforearm"]   = HumanBodyBones.LeftLowerArm,
            ["rightforearm"]  = HumanBodyBones.RightLowerArm,
            ["lefthand"]      = HumanBodyBones.LeftHand,
            ["righthand"]     = HumanBodyBones.RightHand,
            ["leftthigh"]     = HumanBodyBones.LeftUpperLeg,
            ["rightthigh"]    = HumanBodyBones.RightUpperLeg,
            ["leftleg"]       = HumanBodyBones.LeftLowerLeg,
            ["rightleg"]      = HumanBodyBones.RightLowerLeg,
            ["leftfoot"]      = HumanBodyBones.LeftFoot,
            ["rightfoot"]     = HumanBodyBones.RightFoot,
            ["lefttoes"]      = HumanBodyBones.LeftToes,
            ["righttoes"]     = HumanBodyBones.RightToes,
        };

        private static readonly HashSet<string> HumanoidWearerTypes = new HashSet<string>
        {
            "Human", "SCP-3114", "SCP-049-2"
        };

        public static readonly Dictionary<string, Dictionary<string, string>> HitboxFallback = new Dictionary<string, Dictionary<string, string>>
        {
            ["SCP-173"] = new Dictionary<string, string>
            {
                ["body"] = "Center",
                ["head"] = "Heads",
                ["tophead"] = "Top head",
                ["backarm"] = "Little arm",
                ["leftarmup"] = "Arm pair 1",
                ["leftarmdown"] = "Arm pair 2",
                ["rightarm"] = "Big arm",
                ["backleg"] = "Big leg",
                ["rightleg"] = "Middle leg",
                ["leftleg"] = "Little leg",
                ["frontpelvis"] = "Front pelvis"
            },
            ["SCP-939"] = new Dictionary<string, string>
            {
                ["head"] = "Hitbox (10)_0",
                ["chest"] = "Hitbox (9)",
                ["stomach"] = "Hitbox (8)_1",
                ["leftshoulder"] = "Hitbox (10)_1",
                ["leftarm"] = "Hitbox (13)",
                ["leftforearm"] = "Hitbox (14)",
                ["lefthand"] = "Hitbox (17)",
                ["rightshoulder"] = "Hitbox (11)",
                ["rightarm"] = "Hitbox (12)",
                ["rightforearm"] = "Hitbox (15)",
                ["righthand"] = "Hitbox (16)",
                ["pelvis"] = "Hitbox (7)_0",
                ["leftthighupper"] = "Hitbox",
                ["leftthighlower"] = "Hitbox (1)",
                ["leftlegupper"] = "Hitbox (8)_0",
                ["leftleglower"] = "Hitbox (7)_1",
                ["leftlegfoot"] = "Hitbox (6)",
                ["rightthighupper"] = "Hitbox (2)",
                ["rightthighlower"] = "Hitbox (3)",
                ["rightlegupper"] = "Hitbox (4)",
                ["rightleglower"] = "Hitbox (5)",
                ["rightlegfoot"] = "Hitbox (7)_2",
            },
            ["SCP-106"] = new Dictionary<string, string>
            {
                ["head"] = "Capsule_4",
                ["chest"] = "Capsule_9",
                ["stomach"] = "Capsule_10",
                ["leftarm"] = "Capsule_6",
                ["leftforearm"] = "Capsule_5",
                ["rightarm"] = "Capsule_8",
                ["rightforearm"] = "Capsule_7",
                ["leftthigh"] = "Capsule_1",
                ["leftleg"] = "Capsule_0",
                ["rightthigh"] = "Capsule_3",
                ["rightleg"] = "Capsule_2"
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

        public static Transform GetBoneTransform(Player player, string wearerType, string boneName)
        {
            string key = boneName.ToLower();

            if (HumanoidWearerTypes.Contains(wearerType))
            {
                var animatorBone = TryGetAnimatorBone(player.ReferenceHub, key);
                if (animatorBone != null)
                    return animatorBone;
            }

            return TryGetHitboxBone(player.GameObject, wearerType, key);
        }

        // Uses GetComponentInChildren<Animator>(true) because the Animator property
        // is inaccessible at runtime despite DLL compilation — this is the reliable path.
        private static Transform TryGetAnimatorBone(ReferenceHub hub, string friendlyName)
        {
            if (!HumanoidBones.TryGetValue(friendlyName, out var boneEnum))
                return null;

            var role = hub.roleManager.CurrentRole;
            if (!(role is IFpcRole fpcRole))
                return null;

            var fpcModule = fpcRole.FpcModule;
            if (fpcModule == null)
                return null;

            var charModel = fpcModule.CharacterModelInstance as AnimatedCharacterModel;
            if (charModel == null)
                return null;

            var animator = hub.gameObject.GetComponentInChildren<Animator>(true);
            if (animator == null)
                return null;

            Log.Debug($"[BoneMappings] Animator found on: {animator.gameObject.name}");
            Log.Debug($"[BoneMappings] isHuman: {animator.isHuman}, avatar: {(animator.avatar != null ? animator.avatar.name : "null")}");
            Log.Debug($"[BoneMappings] Requesting bone: {boneEnum}");

            var bone = animator.GetBoneTransform(boneEnum);
            Log.Debug($"[BoneMappings] Result: {(bone != null ? bone.name : "null")}");

            return bone;
        }

        /// Resolves bones via HitboxIdentity colliders. Handles duplicate names by appending
        /// "_0", "_1", etc. in traversal order, matching the naming in HitboxFallback dictionaries.
        /// This is the reason 939 and 106 now have full hitbox rig.
        private static Transform TryGetHitboxBone(GameObject playerObject, string wearerType, string friendlyName)
        {
            if (!HitboxFallback.TryGetValue(wearerType, out var boneMap))
                return null;

            if (!boneMap.TryGetValue(friendlyName, out var hitboxName))
                return null;

            var hitboxes = playerObject.GetComponentsInChildren<HitboxIdentity>(true);
            var nameCounts = new Dictionary<string, int>();
            foreach (var hb in hitboxes)
            {
                if (nameCounts.ContainsKey(hb.name))
                    nameCounts[hb.name]++;
                else
                    nameCounts[hb.name] = 1;
            }

            var currentIndex = new Dictionary<string, int>();
            foreach (var hb in hitboxes)
            {
                string resolvedName;
                if (nameCounts[hb.name] > 1)
                {
                    if (!currentIndex.ContainsKey(hb.name))
                        currentIndex[hb.name] = 0;
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

            return null;
        }

        public static List<string> GetAvailableBones(string wearerType)
        {
            if (HumanoidWearerTypes.Contains(wearerType))
                return new List<string>(HumanoidBones.Keys);

            if (HitboxFallback.TryGetValue(wearerType, out var bones))
                return new List<string>(bones.Keys);

            return new List<string>();
        }

        public static List<string> GetWearerTypes()
        {
            var types = new List<string>(HumanoidWearerTypes);
            foreach (var key in HitboxFallback.Keys)
            {
                if (!types.Contains(key))
                    types.Add(key);
            }
            return types;
        }
    }
}