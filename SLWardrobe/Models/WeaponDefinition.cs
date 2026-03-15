using System.Collections.Generic;

namespace SLWardrobe.Models
{
    public class WeaponDefinition
    {
        public string Name { get; set; } = "unnamed_weapon";
        public string Description { get; set; } = "Custom weapon";
        public ItemDetection Detection { get; set; } = new ItemDetection();
        public string AttachBone { get; set; } = "rightforearm";
        public string WearerType { get; set; } = "Human";
        public List<WeaponPartDefinition> Parts { get; set; } = new List<WeaponPartDefinition>();
    }

    public class ItemDetection
    {
        public string Type { get; set; } = "VanillaItem";
        public string Identifier { get; set; } = "Flashlight";
        public string CustomItemSource { get; set; } = "";
    }

    public class WeaponPartDefinition
    {
        public string SchematicName { get; set; } = "";
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