using System.Collections.Generic;
using UnityEngine;
using AdminToys;
using ProjectMER.Features.Objects;

#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
#endif

namespace SLWardrobe.Models
{
    public class PlayerCosmetics
    {
        public CosmeticSet Suit { get; set; }
        public CosmeticSet Weapon { get; set; }
    }

    public class CosmeticSet
    {
        public string Name { get; set; }
        public List<SpawnedPart> Parts { get; } = new List<SpawnedPart>();
        public HashSet<Player> HiddenViewers { get; } = new HashSet<Player>();
    }

    public class SpawnedPart
    {
        public GameObject SchematicRoot { get; set; }
        public SchematicObject Schematic { get; set; }
        public AdminToyBase RootToy { get; set; }
        public bool HideForWearer { get; set; }
        public bool IsStatic { get; set; }
    }

    public class PartBinding
    {
        public string SchematicName { get; set; }
        public string BoneName { get; set; }
        public string WearerType { get; set; }
        public Vector3 LocalPosition { get; set; }
        public Vector3 LocalRotation { get; set; }
        public Vector3 Scale { get; set; }
        public bool HideForWearer { get; set; }
        public bool IsStatic { get; set; }
    }
}