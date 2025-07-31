using System;
using System.Reflection;
using UnityEngine;
using Exiled.API.Features;
using System.Collections.Generic;
using Mirror;
using AdminToys;
using Exiled.API.Features.Toys;
using ProjectMER.Features;
using Exiled.API.Features.Roles;

namespace SLWardrobe
{
    public static class SuitBinder
    {
        private static Dictionary<Player, SuitData> activeSuits = new Dictionary<Player, SuitData>();
        private static readonly MethodInfo HideForConnectionMethod = typeof(NetworkServer)
            .GetMethod("HideForConnection", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        
        public static void ApplySuit(Player player, List<BoneBinding> bindings)
        {
            RemoveSuit(player);
            
            var suitData = new SuitData();

            var hitboxes = player.GameObject.GetComponentsInChildren<HitboxIdentity>();
            var boneMap = new Dictionary<string, Transform>();
            
            
            foreach (var hitbox in hitboxes)
            {
                boneMap[hitbox.name] = hitbox.transform;
            }

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

                    SetupSuitPart(suitPart);

                    var partData = new SuitPartData
                    {
                        GameObject = suitPart,
                        TargetBone = boneMap[binding.BoneName],
                        Binding = binding
                    };
                    
                    suitData.Parts.Add(partData);
                    
                    if (binding.HideForWearer && schematic != null)
                    {
                        try
                        {
                            foreach (var netId in schematic.NetworkIdentities)
                            {
                                if (HideForConnectionMethod != null)
                                {
                                    HideForConnectionMethod.Invoke(null, new object[] { netId, player.Connection });
                                    Log.Debug($"Hid {binding.SchematicName} from {player.Nickname}");
                                }
                                else
                                {
                                    Log.Error("HideForConnection method not found via reflection!");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Failed to hide {binding.SchematicName} from wearer: {ex.Message}");
                        }
                    }
                    
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

                var updater = player.GameObject.AddComponent<SuitUpdater>();
                updater.Initialize(player, SLWardrobe.Instance.Config.SuitUpdateInterval);
                
                Log.Debug($"Applied suit with {suitData.Parts.Count} parts to {player.Nickname}");
            }
        }
        
        private static void SetupSuitPart(GameObject obj)
        {
            var adminToyBase = obj.GetComponent<AdminToyBase>();
            if (adminToyBase != null)
            {
                adminToyBase.NetworkScale = adminToyBase.transform.localScale;
                
                try
                {
                    var movementSmoothing = adminToyBase.GetType().GetField("MovementSmoothing", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (movementSmoothing != null)
                    {
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

            foreach (var component in obj.GetComponents<Component>())
            {
                if (component.GetType().Name.Contains("Schematic"))
                {
                    var behaviour = component as MonoBehaviour;
                    if (behaviour != null)
                        behaviour.enabled = false;
                }
            }

            var netIdentity = obj.GetComponent<NetworkIdentity>();
            if (netIdentity == null)
            {
                netIdentity = obj.AddComponent<NetworkIdentity>();
            }
            
            if (!netIdentity.isServer && NetworkServer.active)
            {
                NetworkServer.Spawn(obj);
            }

            foreach (var rb in obj.GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = true;
            }

            foreach (var col in obj.GetComponentsInChildren<Collider>())
            {
                col.isTrigger = true;
            }

            obj.SetActive(true);
        }
        
        public static void RemoveSuit(Player player)
        {
            if (activeSuits.ContainsKey(player))
            {
                var suitData = activeSuits[player];

                foreach (var part in suitData.Parts)
                {
                    if (part.GameObject != null)
                    {
                        NetworkServer.Destroy(part.GameObject);
                    }
                }
                
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

        public static void SetPlayerInvisibility(Player player, bool invisible)
        {
            if (player.Role is FpcRole fpcRole)
            {
                fpcRole.IsInvisible = invisible;
                
                if (invisible)
                    Log.Debug($"Made {player.Nickname} invisible");
                else
                    Log.Debug($"Made {player.Nickname} visible");
            }
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
    public class SuitUpdater : MonoBehaviour
    {
        private Player owner;
        private float updateInterval;
        private float lastUpdate;
        
        public void Initialize(Player player, float interval)
        {
            owner = player;
            updateInterval = interval;
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

            foreach (var part in suitData.Parts)
            {
                if (part.GameObject != null && part.TargetBone != null)
                {
                    var worldPos = part.TargetBone.TransformPoint(part.Binding.LocalPosition);
                    var worldRot = part.TargetBone.rotation * Quaternion.Euler(part.Binding.LocalRotation);
                    
                    part.GameObject.transform.position = worldPos;
                    part.GameObject.transform.rotation = worldRot;

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
        public bool HideForWearer { get; set; }
    
        public BoneBinding(string schematicName, string boneName, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            SchematicName = schematicName;
            BoneName = boneName;
            LocalPosition = position;
            LocalRotation = rotation;
            Scale = scale;
            HideForWearer = false;
        }
    }
}