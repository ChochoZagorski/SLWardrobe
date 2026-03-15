using System.Collections.Generic;

namespace SLWardrobe.Models
{
    public class SuitDefinition
    {
        public string Name { get; set; } = "unnamed_suit";
        public string Description { get; set; } = "Custom suit";
        public bool MakeWearerInvisible { get; set; } = false;
        public string WearerType { get; set; } = "Human";
        public List<SuitPartDefinition> Parts { get; set; } = new List<SuitPartDefinition>();
    }

    public class SuitPartDefinition
    {
        public string SchematicName { get; set; } = "";
        public string BoneName { get; set; } = "body";
        public double PositionX { get; set; } = 0;
        public double PositionY { get; set; } = 0;
        public double PositionZ { get; set; } = 0;
        public double RotationX { get; set; } = 0;
        public double RotationY { get; set; } = 0;
        public double RotationZ { get; set; } = 0;
        public double ScaleX { get; set; } = 1;
        public double ScaleY { get; set; } = 1;
        public double ScaleZ { get; set; } = 1;
        public bool HideForWearer { get; set; } = false;
        public bool Static { get; set; } = false;
    }
}