using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Mirror;
using AdminToys;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using MEC;
using SLWardrobe.Models;

#if EXILED
using Exiled.API.Features;
using Exiled.API.Features.Items;
#else
using LabApi.Features.Wrappers;
using Log = LabApi.Features.Console.Logger;
#endif

namespace SLWardrobe.Weapons
{
    public static class WeaponBinder
    {
        private static readonly Dictionary<Player, ActiveWeapon> ActiveWeaponsMap = new Dictionary<Player, ActiveWeapon>();
        private static readonly Dictionary<string, IItemMatcher> WeaponMatchers = new Dictionary<string, IItemMatcher>();

        private static readonly MethodInfo HideForConnectionMethod = typeof(NetworkServer)
            .GetMethod("HideForConnection", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly MethodInfo ShowForConnectionMethod = typeof(NetworkServer)
            .GetMethod("ShowForConnection", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        private static CoroutineHandle updateCoroutine;
        private static CoroutineHandle lodCoroutine;
        private static bool isUpdating;
        private static bool isLodRunning;

        public static IReadOnlyDictionary<Player, ActiveWeapon> ActiveWeapons => ActiveWeaponsMap;

        public static void Initialize()
        {
            WeaponMatchers.Clear();

            foreach (var kvp in ConfigLoader.Weapons)
            {
                var matcher = ItemMatcherFactory.Create(kvp.Value.Detection);
                WeaponMatchers[kvp.Key] = matcher;
                Log.Info($"[WeaponBinder] Registered weapon: {kvp.Key} ({matcher.Description})");
            }

            Log.Info($"[WeaponBinder] Initialized with {WeaponMatchers.Count} weapon(s)");

#if EXILED
            if (WeaponMatchers.Count > 0 && Round.IsStarted && !isUpdating)
#else
            if (WeaponMatchers.Count > 0 && Round.IsRoundStarted && !isUpdating)
#endif
                StartUpdater((float)SLWardrobe.Instance.Config.UpdateInterval);
        }

        #region Update Loop

        public static void StartUpdater(float interval)
        {
            if (!isUpdating && WeaponMatchers.Count > 0)
            {
                updateCoroutine = Timing.RunCoroutine(WeaponUpdateLoop(interval));
                isUpdating = true;
            }
        }

        public static void StopUpdater()
        {
            if (isUpdating)
            {
                Timing.KillCoroutines(updateCoroutine);
                isUpdating = false;
            }
        }

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

        private static IEnumerator<float> WeaponUpdateLoop(float interval)
        {
            var toRemove = new List<Player>();

            while (true)
            {
                toRemove.Clear();

                foreach (var player in Player.List)
                {
                    if (player == null || !player.IsAlive)
                    {
                        if (ActiveWeaponsMap.ContainsKey(player))
                            toRemove.Add(player);
                        continue;
                    }

                    var currentItem = player.CurrentItem;
                    var hasActive = ActiveWeaponsMap.TryGetValue(player, out var activeWeapon);

                    string matchedName = null;
                    foreach (var kvp in WeaponMatchers)
                    {
                        if (kvp.Value.Matches(currentItem, player))
                        {
                            matchedName = kvp.Key;
                            break;
                        }
                    }

                    if (matchedName != null)
                    {
                        if (!hasActive || activeWeapon.WeaponName != matchedName)
                        {
                            if (hasActive) RemoveWeaponInternal(player);
                            ApplyWeaponInternal(player, matchedName);
                        }
                    }
                    else if (hasActive)
                    {
                        toRemove.Add(player);
                    }
                }

                foreach (var player in toRemove)
                    RemoveWeaponInternal(player);

                yield return Timing.WaitForSeconds(interval);
            }
        }

        private static IEnumerator<float> LodUpdateLoop(float defaultLodDistance, float interval)
        {
            while (true)
            {
                foreach (var kvp in ActiveWeaponsMap)
                {
                    var wearer = kvp.Key;
                    var weapon = kvp.Value;

                    if (wearer == null || !wearer.IsAlive) continue;

                    var wearerPos = wearer.Position;

                    foreach (var viewer in Player.List)
                    {
                        if (viewer == wearer || viewer == null || !viewer.IsAlive) continue;

                        float lodDist = defaultLodDistance;
                        var ssss = SLWardrobe.Instance?.SsssHandler;
                        if (ssss != null)
                            lodDist = ssss.GetEffectiveLodDistance(viewer);

                        float sqrLod = lodDist * lodDist;
                        bool shouldHide = (viewer.Position - wearerPos).sqrMagnitude > sqrLod;
                        bool currentlyHidden = weapon.HiddenViewers.Contains(viewer);

                        if (shouldHide && !currentlyHidden)
                        {
                            SetWeaponVisibility(weapon, viewer, false);
                            weapon.HiddenViewers.Add(viewer);
                        }
                        else if (!shouldHide && currentlyHidden)
                        {
                            SetWeaponVisibility(weapon, viewer, true);
                            weapon.HiddenViewers.Remove(viewer);
                        }
                    }

#if EXILED
                    weapon.HiddenViewers.RemoveWhere(p => p == null || !p.IsConnected);
#else
                    weapon.HiddenViewers.RemoveWhere(p => p == null || !p.Connection.isReady);
#endif
                }

                yield return Timing.WaitForSeconds(interval);
            }
        }

        private static void SetWeaponVisibility(ActiveWeapon weapon, Player viewer, bool visible)
        {
            var method = visible ? ShowForConnectionMethod : HideForConnectionMethod;
            if (method == null || viewer.Connection == null) return;

            foreach (var part in weapon.Parts)
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

        #region Weapon Lifecycle

        private static void ApplyWeaponInternal(Player player, string weaponName)
        {
            var definition = ConfigLoader.GetWeapon(weaponName);
            if (definition == null)
            {
                Log.Warn($"[WeaponBinder] Definition not found: {weaponName}");
                return;
            }

            var boneTransform = BoneMappings.GetBoneTransform(player, definition.WearerType, definition.AttachBone);

            if (boneTransform == null)
            {
                Log.Warn($"[WeaponBinder] Bone '{definition.AttachBone}' not found on {player.Nickname}");
                if (SLWardrobe.Instance.Config.Debug)
                {
                    Log.Debug("[WeaponBinder] Available bones:");
                    foreach (var name in BoneMappings.GetAvailableBones(definition.WearerType))
                        Log.Debug($"  - \"{name}\"");
                }
                return;
            }

            var activeWeapon = new ActiveWeapon
            {
                WeaponName = weaponName
            };

            foreach (var partDef in definition.Parts)
            {
                try
                {
                    var partOffset = new Vector3((float)partDef.PositionX, (float)partDef.PositionY, (float)partDef.PositionZ);
                    var partRotation = Quaternion.Euler((float)partDef.RotationX, (float)partDef.RotationY, (float)partDef.RotationZ);

                    var worldPos = boneTransform.TransformPoint(partOffset);
                    var worldRot = boneTransform.rotation * partRotation;

                    var schematic = ObjectSpawner.SpawnSchematic(
                        partDef.SchematicName,
                        worldPos,
                        worldRot,
                        new Vector3((float)partDef.ScaleX, (float)partDef.ScaleY, (float)partDef.ScaleZ)
                    );

                    if (schematic?.gameObject == null)
                    {
                        Log.Error($"[WeaponBinder] Failed to spawn '{partDef.SchematicName}'");
                        continue;
                    }

                    var obj = schematic.gameObject;

                    SetupWeaponPart(obj);
                    obj.transform.SetParent(player.GameObject.transform, true);

                    if (partDef.Static)
                    {
                        LockPartStatic(obj);
                        Log.Debug($"[WeaponBinder] Static part '{partDef.SchematicName}'");
                    }
                    else
                    {
                        var tracker = obj.AddComponent<BoneTracker>();
                        tracker.Bone = boneTransform;
                        tracker.LocalOffset = partOffset;
                        tracker.RotationOffset = partRotation;
                        tracker.FixedScale = obj.transform.localScale;
                        tracker.LockScale = true;
                        Log.Debug($"[WeaponBinder] Tracked part '{partDef.SchematicName}'");
                    }

                    var part = new ActiveWeaponPart
                    {
                        GameObject = obj,
                        Schematic = schematic
                    };

                    activeWeapon.Parts.Add(part);

                    if (partDef.HideForWearer && HideForConnectionMethod != null && schematic.NetworkIdentities != null)
                    {
                        foreach (var netId in schematic.NetworkIdentities)
                        {
                            try { HideForConnectionMethod.Invoke(null, new object[] { netId, player.Connection }); }
                            catch (Exception ex) { Log.Debug($"[WeaponBinder] Could not hide from wearer: {ex.Message}"); }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[WeaponBinder] Error spawning '{partDef.SchematicName}': {ex}");
                }
            }

            if (activeWeapon.Parts.Count > 0)
            {
                ActiveWeaponsMap[player] = activeWeapon;

                var config = SLWardrobe.Instance.Config;
                if (config.LodDistance > 0 && !isLodRunning)
                    StartLodUpdater((float)config.LodDistance, (float)config.LodCheckInterval);

                Log.Debug($"[WeaponBinder] Applied '{weaponName}' to {player.Nickname} ({activeWeapon.Parts.Count} parts)");
            }
            else
            {
                Log.Warn($"[WeaponBinder] No parts spawned for '{weaponName}' on {player.Nickname}");
            }
        }

        private static void RemoveWeaponInternal(Player player)
        {
            if (!ActiveWeaponsMap.TryGetValue(player, out var weapon))
                return;

            foreach (var part in weapon.Parts)
            {
                if (part.GameObject != null)
                {
                    try { NetworkServer.Destroy(part.GameObject); }
                    catch (Exception ex) { Log.Debug($"[WeaponBinder] Error destroying part: {ex.Message}"); }
                }
            }

            ActiveWeaponsMap.Remove(player);
        }

        public static void RemoveWeapon(Player player)
        {
            RemoveWeaponInternal(player);
        }

        public static void RemoveAllWeapons()
        {
            foreach (var player in new List<Player>(ActiveWeaponsMap.Keys))
                RemoveWeaponInternal(player);

            ActiveWeaponsMap.Clear();
            StopUpdater();
            StopLodUpdater();
        }

        #endregion

        #region Part Setup

        private static void SetupWeaponPart(GameObject obj)
        {
            foreach (var atb in obj.GetComponentsInChildren<AdminToyBase>())
            {
                atb.NetworkMovementSmoothing = 0;
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

        public static string GetDebugStatus()
        {
            return $"Updater: {(isUpdating ? "Running" : "Stopped")} | " +
                   $"LOD: {(isLodRunning ? "Running" : "Off")} | " +
                   $"Matchers: {WeaponMatchers.Count} | " +
                   $"Active: {ActiveWeaponsMap.Count}";
        }

        #endregion
    }

    #region Data Classes

    public class ActiveWeapon
    {
        public string WeaponName { get; set; }
        public List<ActiveWeaponPart> Parts { get; set; } = new List<ActiveWeaponPart>();
        public HashSet<Player> HiddenViewers { get; set; } = new HashSet<Player>();
    }

    public class ActiveWeaponPart
    {
        public GameObject GameObject { get; set; }
        public SchematicObject Schematic { get; set; }
    }

    #endregion
}