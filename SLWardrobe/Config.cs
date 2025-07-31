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
				MakeWearerInvisible = false,
                WearerType = "Human",
                Parts = new List<SuitPartConfig>
                {
                    new SuitPartConfig
                    {
                        SchematicName = "head",
                        BoneName = "head",
                        PositionX = 0f,
                        PositionY = 0f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1f,
                        ScaleY = 1f,
                        ScaleZ = 1f,
                        HideForWearer = false
                    },
                    new SuitPartConfig
                    {
                        SchematicName = "body",
                        BoneName = "body",
                        PositionX = 0f,
                        PositionY = 0f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1f,
                        ScaleY = 1f,
                        ScaleZ = 1f,
                        HideForWearer = false
                    },
                    new SuitPartConfig
                    {
                        SchematicName = "leftarm",
                        BoneName = "leftarm",
                        PositionX = 0f,
                        PositionY = 0f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1f,
                        ScaleY = 1f,
                        ScaleZ = 1f,
                        HideForWearer = false
                    },
                    new SuitPartConfig
                    {
                        SchematicName = "rightarm",
                        BoneName = "rightarm",
                        PositionX = 0f,
                        PositionY = 0f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1f,
                        ScaleY = 1f,
                        ScaleZ = 1f,
                        HideForWearer = false
                    },
                    new SuitPartConfig
                    {
                        SchematicName = "leftforearm",
                        BoneName = "leftforearm",
                        PositionX = 0f,
                        PositionY = 0f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1f,
                        ScaleY = 1f,
                        ScaleZ = 1f,
                        HideForWearer = false
                    },
                    new SuitPartConfig
                    {
                        SchematicName = "rightforearm",
                        BoneName = "rightforearm",
                        PositionX = 0f,
                        PositionY = 0f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1f,
                        ScaleY = 1f,
                        ScaleZ = 1f,
                        HideForWearer = false
                    },
                    new SuitPartConfig
                    {
                        SchematicName = "leftthigh",
                        BoneName = "leftthigh",
                        PositionX = 0f,
                        PositionY = 0f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1f,
                        ScaleY = 1f,
                        ScaleZ = 1f,
                        HideForWearer = false
                    },
                    new SuitPartConfig
                    {
                        SchematicName = "rightthigh",
                        BoneName = "rightthigh",
                        PositionX = 0f,
                        PositionY = 0f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1f,
                        ScaleY = 1f,
                        ScaleZ = 1f,
                        HideForWearer = false
                    },
                    new SuitPartConfig
                    {
                        SchematicName = "leftleg",
                        BoneName = "leftleg",
                        PositionX = 0f,
                        PositionY = 0f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1f,
                        ScaleY = 1f,
                        ScaleZ = 1f,
                        HideForWearer = false
                    },
                    new SuitPartConfig
                    {
                        SchematicName = "rightleg",
                        BoneName = "rightleg",
                        PositionX = 0f,
                        PositionY = 0f,
                        PositionZ = 0f,
                        RotationX = 0f,
                        RotationY = 0f,
                        RotationZ = 0f,
                        ScaleX = 1f,
                        ScaleY = 1f,
                        ScaleZ = 1f,
                        HideForWearer = false
                    }
                }
            }
        };
    }
    
    public class SuitConfig
    {
        [Description("Description of the suit")]
        public string Description { get; set; } = "Custom suit";

        [Description("Makes the wearer completely invisible")]
        public bool MakeWearerInvisible { get; set; } = false;  
        
        [Description("Wearer type for this suit (Human, SCP-3114, SCP-049-2, SCP-173,SCP-939, SCP-096, SCP-049, SCP-106)")]
        public string WearerType { get; set; } = "Human";
        
        [Description("List of parts that make up the suit")]
        public List<SuitPartConfig> Parts { get; set; } = new List<SuitPartConfig>();
    }
    
    public class SuitPartConfig
    {
        [Description("Name of the schematic to use")]
        public string SchematicName { get; set; } = "";
        
        [Description("Name of the bone to attach to (consult with the README for more info)")]
        public string BoneName { get; set; } = "body";
        
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
        
        [Description("Hide this part from the player wearing the suit (other players will still see it)")]
        public bool HideForWearer { get; set; } = false;
    }
}