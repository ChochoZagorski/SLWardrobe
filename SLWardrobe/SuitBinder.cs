using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Mirror;
using AdminToys;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using MEC;

#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
using CustomPlayerEffects;
using Log = LabApi.Features.Console.Logger;
#endif

namespace SLWardrobe
{
    public static class SuitBinder
    {
        private static readonly Dictionary<Player, SuitData> ActiveSuits = new Dictionary<Player, SuitData>();

        private static readonly MethodInfo HideForConnectionMethod = typeof(NetworkServer)
            .GetMethod("HideForConnection", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly MethodInfo ShowForConnectionMethod = typeof(NetworkServer)
            .GetMethod("ShowForConnection", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        private static CoroutineHandle globalUpdateCoroutine;
        private static CoroutineHandle lodCoroutine;
        private static bool isUpdating;
        private static bool isLodRunning;

        #region Cleanup Loop

        public static void StartGlobalUpdater()
        {
            if (!isUpdating)
            {
                globalUpdateCoroutine = Timing.RunCoroutine(GlobalSuitCleanup());
                isUpdating = true;
            }
        }

        public static void StopGlobalUpdater()
        {
            if (isUpdating)
            {
                Timing.KillCoroutines(globalUpdateCoroutine);
                isUpdating = false;
            }
        }

        private static IEnumerator<float> GlobalSuitCleanup()
        {
            var toRemove = new List<Player>();

            while (true)
            {
                toRemove.Clear();

                foreach (var kvp in ActiveSuits)
                {
                    if (kvp.Key == null || !kvp.Key.IsAlive)
                        toRemove.Add(kvp.Key);
                }

                foreach (var player in toRemove)
                    RemoveSuit(player);

                yield return Timing.WaitForSeconds(0.5f);
            }
        }

        #endregion

        #region LOD System

        public static void StartLodUpdater(float lodDistance, float checkInterval)
        {
            if (!isLodRunning && lodDistance > 0 && ShowForConnectionMethod != null && HideForConnectionMethod != null)
            {
                lodCoroutine = Timing.RunCoroutine(LodUpdateLoop(lodDistance, checkInterval));
                isLodRunning = true;
            }
        }

        public static void StopLodUpdater()
        {
            if (isLodRunning)
            {
                Timing.KillCoroutines(lodCoroutine);
                isLodRunning = false;
            }
        }

        private static IEnumerator<float> LodUpdateLoop(float defaultLodDistance, float interval)
        {
            while (true)
            {
                foreach (var kvp in ActiveSuits)
                {
                    var wearer = kvp.Key;
                    var suitData = kvp.Value;

                    if (wearer == null || !wearer.IsAlive)
                        continue;

                    var wearerPos = wearer.Position;

                    foreach (var viewer in Player.List)
                    {
                        if (viewer == wearer || viewer == null || !viewer.IsAlive)
                            continue;

                        float lodDist = defaultLodDistance;
                        var ssss = SLWardrobe.Instance?.SsssHandler;
                        if (ssss != null)
                            lodDist = ssss.GetEffectiveLodDistance(viewer);

                        float sqrLod = lodDist * lodDist;
                        bool shouldHide = (viewer.Position - wearerPos).sqrMagnitude > sqrLod;
                        bool currentlyHidden = suitData.HiddenViewers.Contains(viewer);

                        if (shouldHide && !currentlyHidden)
                        {
                            SetVisibilityForViewer(suitData, viewer, false);
                            suitData.HiddenViewers.Add(viewer);
                        }
                        else if (!shouldHide && currentlyHidden)
                        {
                            SetVisibilityForViewer(suitData, viewer, true);
                            suitData.HiddenViewers.Remove(viewer);
                        }
                    }

#if EXILED
                    suitData.HiddenViewers.RemoveWhere(p => p == null || !p.IsConnected);
#else
                    suitData.HiddenViewers.RemoveWhere(p => p == null || !p.Connection.isReady);
#endif
                }

                yield return Timing.WaitForSeconds(interval);
            }
        }

        private static void SetVisibilityForViewer(SuitData suitData, Player viewer, bool visible)
        {
            var method = visible ? ShowForConnectionMethod : HideForConnectionMethod;
            if (method == null || viewer.Connection == null) return;

            foreach (var part in suitData.Parts)
            {
                if (part.Schematic?.NetworkIdentities == null) continue;

                foreach (var netId in part.Schematic.NetworkIdentities)
                {
                    try { method.Invoke(null, new object[] { netId, viewer.Connection }); }
                    catch { }
                }
            }
        }

        #endregion

        #region Suit Lifecycle

        public static void ApplySuit(Player player, List<BoneBinding> bindings)
        {
            RemoveSuit(player);

            var suitData = new SuitData();

            foreach (var binding in bindings)
            {
                var boneTransform = BoneMappings.GetBoneTransform(player, binding.WearerType, binding.BoneName);
                if (boneTransform == null)
                {
                    Log.Warn($"[SuitBinder] Bone '{binding.BoneName}' not found on {player.Nickname} ({binding.WearerType})");
                    if (SLWardrobe.Instance.Config.Debug)
                    {
                        Log.Debug("[SuitBinder] Available bones:");
                        foreach (var name in BoneMappings.GetAvailableBones(binding.WearerType))
                            Log.Debug($"  - \"{name}\"");
                    }
                    continue;
                }

                try
                {
                    var worldPos = boneTransform.TransformPoint(binding.LocalPosition);
                    var worldRot = boneTransform.rotation * Quaternion.Euler(binding.LocalRotation);

                    var schematic = ObjectSpawner.SpawnSchematic(
                        binding.SchematicName,
                        worldPos,
                        worldRot,
                        binding.Scale
                    );

                    if (schematic?.gameObject == null)
                    {
                        Log.Error($"[SuitBinder] Failed to spawn schematic '{binding.SchematicName}'");
                        continue;
                    }

                    var obj = schematic.gameObject;

                    SetupSuitPart(obj);
                    
                    obj.transform.SetParent(player.GameObject.transform, true);

                    if (binding.IsStaticPart)
                    {
                        LockPartStatic(obj);
                        Log.Debug($"[SuitBinder] Static part '{binding.SchematicName}' on '{binding.BoneName}'");
                    }
                    else
                    {
                        var tracker = obj.AddComponent<BoneTracker>();
                        tracker.Init(boneTransform, binding.LocalPosition, Quaternion.Euler(binding.LocalRotation), obj.transform.localScale);
                        Log.Debug($"[SuitBinder] Tracked part '{binding.SchematicName}' on '{binding.BoneName}'");
                    }

                    var partData = new SuitPartData
                    {
                        GameObject = obj,
                        Schematic = schematic,
                        Binding = binding
                    };

                    suitData.Parts.Add(partData);

                    if (binding.HideForWearer && HideForConnectionMethod != null && schematic.NetworkIdentities != null)
                    {
                        foreach (var netId in schematic.NetworkIdentities)
                        {
                            try { HideForConnectionMethod.Invoke(null, new object[] { netId, player.Connection }); }
                            catch (Exception ex) { Log.Debug($"[SuitBinder] Could not hide from wearer: {ex.Message}"); }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[SuitBinder] Error spawning '{binding.SchematicName}': {ex.Message}");
                }
            }

            if (suitData.Parts.Count > 0)
            {
                ActiveSuits[player] = suitData;

                if (!isUpdating)
                    StartGlobalUpdater();

                var config = SLWardrobe.Instance.Config;
                if (config.LodDistance > 0 && !isLodRunning)
                    StartLodUpdater((float)config.LodDistance, (float)config.LodCheckInterval);

                Log.Debug($"[SuitBinder] Applied suit with {suitData.Parts.Count} parts to {player.Nickname}");
            }
        }

        public static void RemoveSuit(Player player)
        {
            if (!ActiveSuits.TryGetValue(player, out var suitData))
                return;

            foreach (var part in suitData.Parts)
            {
                if (part.GameObject != null)
                    NetworkServer.Destroy(part.GameObject);
            }

            ActiveSuits.Remove(player);

            if (ActiveSuits.Count == 0)
            {
                StopGlobalUpdater();
                StopLodUpdater();
            }

            Log.Debug($"[SuitBinder] Removed suit from {player.Nickname}");
        }

        public static SuitData GetSuitData(Player player)
        {
            return ActiveSuits.TryGetValue(player, out var data) ? data : null;
        }

        #endregion

        #region Player Invisibility

        public static void SetPlayerInvisibility(Player player, bool invisible)
        {
            if (invisible)
            {
#if EXILED
                player.EnableEffect(Exiled.API.Enums.EffectType.Fade, 255, 0, false);
#else
                player.EnableEffect<Fade>(255);
#endif
                Log.Debug($"[SuitBinder] Applied Fade effect to {player.Nickname}");
            }
            else
            {
#if EXILED
                player.DisableEffect(Exiled.API.Enums.EffectType.Fade);
#else
                player.DisableEffect<Fade>();
#endif
                Log.Debug($"[SuitBinder] Removed Fade effect from {player.Nickname}");
            }
        }

        #endregion

        #region Part Setup

        private static void SetupSuitPart(GameObject obj)
        {
            foreach (var atb in obj.GetComponentsInChildren<AdminToyBase>())
            {
                atb.NetworkMovementSmoothing = 40;
                atb.syncInterval = 0f;
            }

            var netIdentity = obj.GetComponent<NetworkIdentity>();
            if (netIdentity == null)
                netIdentity = obj.AddComponent<NetworkIdentity>();

            if (!netIdentity.isServer && NetworkServer.active)
                NetworkServer.Spawn(obj);

            foreach (var rb in obj.GetComponentsInChildren<Rigidbody>())
                rb.isKinematic = true;

            foreach (var col in obj.GetComponentsInChildren<Collider>())
                col.isTrigger = true;

            obj.SetActive(true);
        }

        /// Writes current local transforms to SyncVars, then sets IsStatic after a short delay.
        /// The delay ensures initial state (position, rotation, scale, parent) syncs to clients
        /// before IsStatic locks AdminToyBase.LateUpdate() on both server and client.
        private static void LockPartStatic(GameObject obj)
        {
            foreach (var atb in obj.GetComponentsInChildren<AdminToyBase>())
            {
                atb.NetworkPosition = atb.transform.localPosition;
                atb.NetworkRotation = atb.transform.localRotation;
                atb.NetworkScale = atb.transform.localScale;
                atb.NetworkMovementSmoothing = 0;
            }

            Timing.RunCoroutine(DelayedStaticLock(obj));
        }

        private static IEnumerator<float> DelayedStaticLock(GameObject obj)
        {
            yield return Timing.WaitForSeconds(0.1f);

            if (obj == null) yield break;

            foreach (var atb in obj.GetComponentsInChildren<AdminToyBase>())
            {
                if (atb == null) continue;
                atb.NetworkIsStatic = true;
            }
        }

        #endregion
    }

    #region Data Classes

    public class SuitData
    {
        public List<SuitPartData> Parts { get; set; } = new List<SuitPartData>();
        public HashSet<Player> HiddenViewers { get; set; } = new HashSet<Player>();
    }

    public class SuitPartData
    {
        public GameObject GameObject { get; set; }
        public SchematicObject Schematic { get; set; }
        public BoneBinding Binding { get; set; }
    }

    public class BoneBinding
    {
        public string SchematicName { get; set; }
        public string BoneName { get; set; }
        public string WearerType { get; set; }
        public Vector3 LocalPosition { get; set; }
        public Vector3 LocalRotation { get; set; }
        public Vector3 Scale { get; set; }
        public bool HideForWearer { get; set; }
        public bool IsStaticPart { get; set; }

        public BoneBinding(string schematicName, string boneName, string wearerType,
            Vector3 position, Vector3 rotation, Vector3 scale)
        {
            SchematicName = schematicName;
            BoneName = boneName;
            WearerType = wearerType;
            LocalPosition = position;
            LocalRotation = rotation;
            Scale = scale;
        }
    }

    #endregion
}