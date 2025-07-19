using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Interfaces;

namespace SLWardrobe
{
    public class Config : IConfig
    {
        [Description("Whether the plugin is enabled")]
        public bool IsEnabled { get; set; } = true;
        
        [Description("Enable debug logging")]
        public bool Debug { get; set; } = false;
        
        [Description("How often to update suit positions in seconds (default: 0.033)")]
        public float SuitUpdateInterval { get; set; } = 0.033f;
        
        [Description("Define custom suits. Each suit has a name and a list of parts.")]
        public Dictionary<string, SuitConfig> Suits { get; set; } = new Dictionary<string, SuitConfig>
        {
            ["example_suit"] = new SuitConfig
            {
                Description = "The example suit",
                Parts = new List<SuitPartConfig>
                {
                    new SuitPartConfig
                    {
                        SchematicName = "head",
                        BoneName = "Head",
                        PositionX = 0f,
                        PositionY = 0f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1f,
                        ScaleY = 1f,
                        ScaleZ = 1f
                    },
                    new SuitPartConfig
                    {
                        SchematicName = "body",
                        BoneName = "SpineMiddle",
                        PositionX = 0f,
                        PositionY = 0f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1f,
                        ScaleY = 1f,
                        ScaleZ = 1f
                    },
                    new SuitPartConfig
                    {
                        SchematicName = "leftarm",
                        BoneName = "Arm.L",
                        PositionX = 0f,
                        PositionY = 0f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1f,
                        ScaleY = 1f,
                        ScaleZ = 1f
                    },
                    new SuitPartConfig
                    {
                        SchematicName = "rightarm",
                        BoneName = "Arm.R",
                        PositionX = 0f,
                        PositionY = 0f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1f,
                        ScaleY = 1f,
                        ScaleZ = 1f
                    },
                    new SuitPartConfig
                    {
                        SchematicName = "leftforearm",
                        BoneName = "Forearm.L",
                        PositionX = 0f,
                        PositionY = 0f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1f,
                        ScaleY = 1f,
                        ScaleZ = 1f
                    },
                    new SuitPartConfig
                    {
                        SchematicName = "rightforearm",
                        BoneName = "Forearm.R",
                        PositionX = 0f,
                        PositionY = 0f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1f,
                        ScaleY = 1f,
                        ScaleZ = 1f
                    },
                    new SuitPartConfig
                    {
                        SchematicName = "leftthigh",
                        BoneName = "Thigh.L",
                        PositionX = 0f,
                        PositionY = 0f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1f,
                        ScaleY = 1f,
                        ScaleZ = 1f
                    },
                    new SuitPartConfig
                    {
                        SchematicName = "rightthigh",
                        BoneName = "Thigh.R",
                        PositionX = 0f,
                        PositionY = 0f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1f,
                        ScaleY = 1f,
                        ScaleZ = 1f
                    },
                    new SuitPartConfig
                    {
                        SchematicName = "leftleg",
                        BoneName = "leg.L",
                        PositionX = 0f,
                        PositionY = 0f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1f,
                        ScaleY = 1f,
                        ScaleZ = 1f
                    },
                    new SuitPartConfig
                    {
                        SchematicName = "rightleg",
                        BoneName = "leg.R",
                        PositionX = 0f,
                        PositionY = 0f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1f,
                        ScaleY = 1f,
                        ScaleZ = 1f
                    }
                }
            },
            ["armor_suit"] = new SuitConfig
            {
                Description = "Heavy armor suit",
                Parts = new List<SuitPartConfig>
                {
                    new SuitPartConfig
                    {
                        SchematicName = "helmet",
                        BoneName = "Head",
                        PositionX = 0f,
                        PositionY = 0.05f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1.1f,
                        ScaleY = 1.1f,
                        ScaleZ = 1.1f
                    },
                    new SuitPartConfig
                    {
                        SchematicName = "chestplate",
                        BoneName = "Chest",
                        PositionX = 0f,
                        PositionY = 0f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1.2f,
                        ScaleY = 1.2f,
                        ScaleZ = 1.2f
                    },
                    new SuitPartConfig
                    {
                        SchematicName = "shoulderpad_left",
                        BoneName = "Arm.L",
                        PositionX = 0f,
                        PositionY = 0.1f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1f,
                        ScaleY = 1f,
                        ScaleZ = 1f
                    },
                    new SuitPartConfig
                    {
                        SchematicName = "shoulderpad_right",
                        BoneName = "Arm.R",
                        PositionX = 0f,
                        PositionY = 0.1f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1f,
                        ScaleY = 1f,
                        ScaleZ = 1f
                    }
                }
            }
        };
    }
    
    public class SuitConfig
    {
        [Description("Description of the suit")]
        public string Description { get; set; } = "Custom suit";
        
        [Description("List of parts that make up the suit")]
        public List<SuitPartConfig> Parts { get; set; } = new List<SuitPartConfig>();
    }
    
    public class SuitPartConfig
    {
        [Description("Name of the schematic to use")]
        public string SchematicName { get; set; } = "";
        
        [Description("Name of the bone to attach to (Head, SpineMiddle, Arm.L, Arm.R, Forearm.L, Forearm.R, Thigh.L, Thigh.R, leg.L, leg.R)")]
        public string BoneName { get; set; } = "SpineMiddle";
        
        [Description("Local position offset X")]
        public float PositionX { get; set; } = 0f;
        
        [Description("Local position offset Y")]
        public float PositionY { get; set; } = 0f;
        
        [Description("Local position offset Z")]
        public float PositionZ { get; set; } = 0f;
        
        [Description("Local rotation X in degrees")]
        public float RotationX { get; set; } = 0f;
        
        [Description("Local rotation Y in degrees")]
        public float RotationY { get; set; } = 0f;
        
        [Description("Local rotation Z in degrees")]
        public float RotationZ { get; set; } = 0f;
        
        [Description("Scale X")]
        public float ScaleX { get; set; } = 1f;
        
        [Description("Scale Y")]
        public float ScaleY { get; set; } = 1f;
        
        [Description("Scale Z")]
        public float ScaleZ { get; set; } = 1f;
    }
}