using System;
using UnityEngine;
using Exiled.API.Features;
using System.Collections.Generic;
using Mirror;
using AdminToys;
using Exiled.API.Features.Toys;
using ProjectMER.Features;

namespace SLWardrobe
{
    public static class SuitBinder
    {
        // Store active suits with their bone mappings
        private static Dictionary<Player, SuitData> activeSuits = new Dictionary<Player, SuitData>();
        
        public static void ApplySuit(Player player, List<BoneBinding> bindings)
        {
            RemoveSuit(player);
            
            var suitData = new SuitData();
            
            // Find all bones first
            var hitboxes = player.GameObject.GetComponentsInChildren<HitboxIdentity>();
            var boneMap = new Dictionary<string, Transform>();
            
            foreach (var hitbox in hitboxes)
            {
                boneMap[hitbox.name] = hitbox.transform;
            }
            
            // Spawn schematics using ProjectMER
            foreach (var binding in bindings)
            {
                if (!boneMap.ContainsKey(binding.BoneName))
                {
                    Log.Warn($"Bone {binding.BoneName} not found on player");
                    continue;
                }
                
                GameObject suitPart = null;
                
                try
                {
                    // Spawn schematic using ProjectMER
                    var schematic = ObjectSpawner.SpawnSchematic(
                        binding.SchematicName,
                        Vector3.zero,
                        Quaternion.identity,
                        binding.Scale
                    );
                    
                    if (schematic != null)
                    {
                        suitPart = schematic.gameObject;
                    }
                    
                    if (suitPart == null)
                    {
                        Log.Error($"Failed to spawn suit part {binding.SchematicName}");
                        continue;
                    }
                    
                    // Setup the suit part for persistence
                    SetupSuitPart(suitPart);
                    
                    // Create the part data
                    var partData = new SuitPartData
                    {
                        GameObject = suitPart,
                        TargetBone = boneMap[binding.BoneName],
                        Binding = binding
                    };
                    
                    suitData.Parts.Add(partData);
                    
                    Log.Debug($"Created suit part {binding.SchematicName} for bone {binding.BoneName}");
                }
                catch (System.Exception ex)
                {
                    Log.Error($"Error spawning suit part {binding.SchematicName}: {ex.Message}");
                }
            }
            
            if (suitData.Parts.Count > 0)
            {
                suitData.Owner = player;
                activeSuits[player] = suitData;
                
                // Start the update coroutine
                var updater = player.GameObject.AddComponent<SuitUpdater>();
                updater.Initialize(player);
                
                Log.Info($"Applied suit with {suitData.Parts.Count} parts to {player.Nickname}");
            }
        }
        
        private static void SetupSuitPart(GameObject obj)
        {
            // Disable auto-destruction mechanisms
            var adminToyBase = obj.GetComponent<AdminToyBase>();
            if (adminToyBase != null)
            {
                // Set the toy to not auto-destroy
                adminToyBase.NetworkScale = adminToyBase.transform.localScale;
                
                // Try to access MovementSmoothing if it exists (like in SCP-999)
                try
                {
                    var movementSmoothing = adminToyBase.GetType().GetField("MovementSmoothing", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (movementSmoothing != null)
                    {
                        // Check the field type and convert accordingly
                        if (movementSmoothing.FieldType == typeof(byte))
                        {
                            movementSmoothing.SetValue(adminToyBase, (byte)60);
                        }
                        else if (movementSmoothing.FieldType == typeof(int))
                        {
                            movementSmoothing.SetValue(adminToyBase, 60);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug($"Could not set MovementSmoothing: {ex.Message}");
                }
            }
            
            // Try to find and disable any schematic management component
            foreach (var component in obj.GetComponents<Component>())
            {
                if (component.GetType().Name.Contains("Schematic"))
                {
                    var behaviour = component as MonoBehaviour;
                    if (behaviour != null)
                        behaviour.enabled = false;
                }
            }
            
            // Ensure NetworkIdentity is properly set up
            var netIdentity = obj.GetComponent<NetworkIdentity>();
            if (netIdentity == null)
            {
                netIdentity = obj.AddComponent<NetworkIdentity>();
            }
            
            // Make sure it's spawned on the network if not already
            if (!netIdentity.isServer && NetworkServer.active)
            {
                NetworkServer.Spawn(obj);
            }
            
            // Disable physics
            foreach (var rb in obj.GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = true;
            }
            
            // Make colliders trigger only
            foreach (var col in obj.GetComponentsInChildren<Collider>())
            {
                col.isTrigger = true;
            }
            
            // Keep it active
            obj.SetActive(true);
        }
        
        public static void RemoveSuit(Player player)
        {
            if (activeSuits.ContainsKey(player))
            {
                var suitData = activeSuits[player];
                
                // Destroy all parts
                foreach (var part in suitData.Parts)
                {
                    if (part.GameObject != null)
                    {
                        NetworkServer.Destroy(part.GameObject);
                    }
                }
                
                // Remove the updater
                var updater = player.GameObject.GetComponent<SuitUpdater>();
                if (updater != null)
                    UnityEngine.Object.Destroy(updater);
                
                activeSuits.Remove(player);
                
                Log.Debug($"Removed suit from {player.Nickname}");
            }
        }
        
        public static SuitData GetSuitData(Player player)
        {
            return activeSuits.ContainsKey(player) ? activeSuits[player] : null;
        }
    }
    
    public class SuitData
    {
        public Player Owner { get; set; }
        public List<SuitPartData> Parts { get; set; } = new List<SuitPartData>();
    }
    
    public class SuitPartData
    {
        public GameObject GameObject { get; set; }
        public Transform TargetBone { get; set; }
        public BoneBinding Binding { get; set; }
    }
    
    // MonoBehaviour to handle updates
    public class SuitUpdater : MonoBehaviour
    {
        private Player owner;
        private float updateInterval = 0.033f; // ~30 FPS for performance
        private float lastUpdate;
        
        public void Initialize(Player player)
        {
            owner = player;
        }
        
        void Update()
        {
            if (Time.time - lastUpdate < updateInterval)
                return;
                
            lastUpdate = Time.time;
            
            if (owner == null || !owner.IsAlive)
            {
                SuitBinder.RemoveSuit(owner);
                return;
            }
            
            var suitData = SuitBinder.GetSuitData(owner);
            if (suitData == null)
            {
                Destroy(this);
                return;
            }
            
            // Update positions for this player's suit
            foreach (var part in suitData.Parts)
            {
                if (part.GameObject != null && part.TargetBone != null)
                {
                    // Calculate world position based on bone position and binding offsets
                    var worldPos = part.TargetBone.TransformPoint(part.Binding.LocalPosition);
                    var worldRot = part.TargetBone.rotation * Quaternion.Euler(part.Binding.LocalRotation);
                    
                    part.GameObject.transform.position = worldPos;
                    part.GameObject.transform.rotation = worldRot;
                    
                    // Ensure it stays active
                    if (!part.GameObject.activeSelf)
                        part.GameObject.SetActive(true);
                }
            }
        }
        
        void OnDestroy()
        {
            if (owner != null)
                SuitBinder.RemoveSuit(owner);
        }
    }
    
    public class BoneBinding
    {
        public string SchematicName { get; set; }
        public string BoneName { get; set; }
        public Vector3 LocalPosition { get; set; }
        public Vector3 LocalRotation { get; set; }
        public Vector3 Scale { get; set; }
        
        public BoneBinding(string schematicName, string boneName, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            SchematicName = schematicName;
            BoneName = boneName;
            LocalPosition = position;
            LocalRotation = rotation;
            Scale = scale;
        }
    }
}