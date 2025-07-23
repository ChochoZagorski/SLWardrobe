using System.Collections.Generic;

namespace SLWardrobe
{
    public static class BoneMappings
    {
        public static Dictionary<string, Dictionary<string, string>> WearerTypes = new Dictionary<string, Dictionary<string, string>>
        {
            // Rating Stability: It means how much of the rig is actually usable and has not duplicated hitbox names. It also defines if the rig is actually usable.
            
            
            // The stable rigs.
            ["Human"] = new Dictionary<string, string>
            {
                ["head"] = "Head",
                ["neck"] = "Neck",
                ["chest"] = "Chest",
                ["body"] = "SpineMiddle",
                ["hip"] = "Hip",
                ["leftarm"] = "Arm.L",
                ["rightarm"] = "Arm.R",
                ["leftforearm"] = "Forearm.L",
                ["rightforearm"] = "Forearm.R",
                ["leftthigh"] = "Thigh.L",
                ["rightthigh"] = "Thigh.R",
                ["leftleg"] = "leg.L",
                ["rightleg"] = "leg.R",
                ["lefttoes"] = "Toes.L",
                ["righttoes"] = "Toes.R"
            },
            ["SCP-3114"] = new Dictionary<string, string>
            {
                ["head"] = "mixamorig:Head",
                ["chest"] = "mixamorig:Spine",
                ["leftarm"] = "mixamorig:LeftArm",
                ["rightarm"] = "mixamorig:RightArm",
                ["leftforearm"] = "mixamorig:LeftForeArm",
                ["rightforearm"] = "mixamorig:RightForeArm",
                ["leftthigh"] = "mixamorig:LeftUpLeg",
                ["rightthigh"] = "mixamorig:RightUpLeg",
                ["leftleg"] = "mixamorig:LeftLeg",
                ["rightleg"] = "mixamorig:RightLeg"
            },
            ["SCP-049-2"] = new Dictionary<string, string>
            {
                ["head"] = "mixamorig:HeadTop_End",
                ["chest"] = "mixamorig:Spine2",
                ["leftarm"] = "mixamorig:LeftArm",
                ["rightarm"] = "mixamorig:RightArm",
                ["leftforearm"] = "mixamorig:LeftForeArm",  
                ["rightforearm"] = "mixamorig:RightForeArm",
                ["leftthigh"] = "mixamorig:LeftUpLeg",
                ["rightthigh"] = "mixamorig:RightUpLeg",
                ["leftleg"] = "mixamorig:LeftLeg",
                ["rightleg"] = "mixamorig:RightLeg"
            },
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
            // The not so stable rigs.
            ["SCP-939"] = new Dictionary<string, string>
            {
                // Notice how Head is missing? It's not that 939 doesn't have a defined head hitbox, it's because "Hitbox (10)" name has the same name as THE LEFT SHOULDER
                ["chest"] = "Hitbox (9)",
                ["stomach"] = "Hitbox (8)", // Hitbox is duped 2 times, because why would someone check the names? [ *cough, cough* body_NTF (1) ]
                ["leftshoulder"] = "Hitbox (10)", // Guess which important hitbox cannot be bound because of this bugger. Hint: look up
                ["leftarm"] = "Hitbox (13)",
                ["leftforearm"] = "Hitbox (14)",
                ["lefthand"] = "Hitbox (17)",
                ["rightshoulder"] = "Hitbox (11)",
                ["rightarm"] = "Hitbox (12)",
                ["rightforearm"] = "Hitbox (15)",
                ["righthand"] = "Hitbox (16)",
                ["leftthighupper"] = "Hitbox",
                ["leftthighlower"] = "Hitbox (1)",
                ["rightthighupper"] = "Hitbox (2)",
                ["rightthighlower"] = "Hitbox (3)",
                // Notice how LeftLegUpper is missing? because if you check the stomach hitbox it has the same NAME, like why?
                // Notice how LeftLegLower is missing? because it shares the same name with: pelvis, left upper leg and right leg foot, like why?
                ["leftlegfoot"] = "Hitbox (6)",
                ["rightlegupper"] = "Hitbox (4)",
                ["rightleglower"] = "Hitbox (5)",
                ["rightlegfoot"] = "Hitbox (7)" // Hitbox is also duped. It has the same name as the pelvis one. Like it takes 3 seconds in Unity to just go over each hitbox im pretty sure the game is made in unity.
            },
            // The no rigs, rigs.
            ["SCP-096"] = new Dictionary<string, string>
            {
                ["root"] = "Hitbox" // quite literally 096's only hitbox.
            },
            ["SCP-049"] = new Dictionary<string, string>
            {
                ["root"] = "Hitbox" // exactly the same as 096, this is 049's only hitbox.
            },
            ["SCP-106"] = new Dictionary<string, string>
            {
                ["chest"] = "Capsule" // I love northwood with 106's every hitbox being named CAPSULE....
            }
        };
        
        public static string GetBoneName(string wearerType, string simpleName)
        {
            if (WearerTypes.ContainsKey(wearerType) && WearerTypes[wearerType].ContainsKey(simpleName.ToLower()))
            {
                return WearerTypes[wearerType][simpleName.ToLower()];
            }

            return simpleName;
        }
        
        public static List<string> GetAvailableBones(string wearerType)
        {
            if (WearerTypes.ContainsKey(wearerType))
            {
                return new List<string>(WearerTypes[wearerType].Keys);
            }
            
            return new List<string>();
        }
        
        public static List<string> GetWearerTypes()
        {
            return new List<string>(WearerTypes.Keys);
        }
    }
}