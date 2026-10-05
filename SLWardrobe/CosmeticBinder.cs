using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Mirror;
using AdminToys;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using MEC;
using SLWardrobe.Common;
using SLWardrobe.Models;

#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
using CustomPlayerEffects;
using Log = LabApi.Features.Console.Logger;
#endif

namespace SLWardrobe
{
    public static class CosmeticBinder
    {
        private static readonly Dictionary<Player, PlayerCosmetics> ActiveCosmetics = new Dictionary<Player, PlayerCosmetics>();

        private static readonly MethodInfo HideForConnectionMethod = typeof(NetworkServer)
            .GetMethod("HideForConnection", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly MethodInfo ShowForConnectionMethod = typeof(NetworkServer)
            .GetMethod("ShowForConnection", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        private static CoroutineHandle cleanupCoroutine;
        private static CoroutineHandle lodCoroutine;
        private static bool isCleanupRunning;
        private static bool isLodRunning;

        #region Public API

        public static void ApplySuit(Player player, List<PartBinding> bindings)
        {
            RemoveSuit(player);
            var set = SpawnCosmeticSet(player, bindings);
            if (set == null) return;
            GetOrCreateCosmetics(player).Suit = set;
            EnsureCoroutinesRunning();
        }

        public static void ApplyWeapon(Player player, string weaponName, List<PartBinding> bindings)
        {
            RemoveWeapon(player);
            var set = SpawnCosmeticSet(player, bindings);
            if (set == null) return;
            set.Name = weaponName;
            GetOrCreateCosmetics(player).Weapon = set;
            EnsureCoroutinesRunning();
        }

        public static void RemoveSuit(Player player)
        {
            if (!ActiveCosmetics.TryGetValue(player, out var cosmetics) || cosmetics.Suit == null) return;
            DestroyCosmeticSet(cosmetics.Suit);
            cosmetics.Suit = null;
            CleanupEmptyEntry(player);
        }

        public static void RemoveWeapon(Player player)
        {
            if (!ActiveCosmetics.TryGetValue(player, out var cosmetics) || cosmetics.Weapon == null) return;
            DestroyCosmeticSet(cosmetics.Weapon);
            cosmetics.Weapon = null;
            CleanupEmptyEntry(player);
        }

        public static void RemoveAll(Player player)
        {
            if (!ActiveCosmetics.TryGetValue(player, out var cosmetics)) return;
            if (cosmetics.Suit != null) DestroyCosmeticSet(cosmetics.Suit);
            if (cosmetics.Weapon != null) DestroyCosmeticSet(cosmetics.Weapon);
            ActiveCosmetics.Remove(player);
            StopCoroutinesIfEmpty();
        }

        public static void RemoveAllPlayers()
        {
            foreach (var kvp in new Dictionary<Player, PlayerCosmetics>(ActiveCosmetics))
            {
                if (kvp.Value.Suit != null) DestroyCosmeticSet(kvp.Value.Suit);
                if (kvp.Value.Weapon != null) DestroyCosmeticSet(kvp.Value.Weapon);
            }
            ActiveCosmetics.Clear();
            CosmeticTracker.UnregisterAll();
            StopAllCoroutines();
        }

        public static CosmeticSet GetSuitData(Player player)
            => ActiveCosmetics.TryGetValue(player, out var c) ? c.Suit : null;

        public static CosmeticSet GetWeaponData(Player player)
            => ActiveCosmetics.TryGetValue(player, out var c) ? c.Weapon : null;

        public static int ActivePlayerCount => ActiveCosmetics.Count;

        #endregion

        #region Player Invisibility

        public static void SetPlayerInvisibility(Player player, bool invisible)
        {
#if EXILED
            if (invisible) player.EnableEffect(Exiled.API.Enums.EffectType.Fade, 255, 0, false);
            else player.DisableEffect(Exiled.API.Enums.EffectType.Fade);
#else
            if (invisible) player.EnableEffect<Fade>(255);
            else player.DisableEffect<Fade>();
#endif
        }

        // Limb bones swing at high frequency with the walk/run cycle - they need snap smoothing
        // to keep observer-visible phase tight. Core bones move at body-translation frequency and
        // benefit from smoother interpolation. Matches the friendly bone names in BoneMappings.
        private static bool IsLimbBone(string boneName)
        {
            if (string.IsNullOrEmpty(boneName)) return false;
            string b = boneName.ToLowerInvariant();
            return b.Contains("arm") || b.Contains("forearm") || b.Contains("hand") || b.Contains("shoulder")
                || b.Contains("thigh") || b.Contains("leg") || b.Contains("toes");
        }

        #endregion

        #region Spawning

        private static CosmeticSet SpawnCosmeticSet(Player player, List<PartBinding> bindings)
        {
            var set = new CosmeticSet();

            foreach (var binding in bindings)
            {
                var boneTransform = BoneMappings.GetBoneTransform(player, binding.WearerType, binding.BoneName);
                if (boneTransform == null)
                {
                    Log.Warn($"[CosmeticBinder] Bone '{binding.BoneName}' not found on {player.Nickname} ({binding.WearerType})");
                    continue;
                }

                try
                {
                    var part = SpawnPart(binding, boneTransform);
                    if (part != null) set.Parts.Add(part);
                }
                catch (Exception ex)
                {
                    Log.Error($"[CosmeticBinder] Error spawning '{binding.SchematicName}': {ex.Message}");
                }
            }

            if (set.Parts.Count == 0) return null;

            if (HideForConnectionMethod != null && player.Connection != null)
            {
                foreach (var part in set.Parts)
                {
                    if (!part.HideForWearer) continue;
                    var identities = part.Schematic?.NetworkIdentities;
                    if (identities == null) continue;
                    foreach (var netId in identities)
                    {
                        try { HideForConnectionMethod.Invoke(null, new object[] { netId, player.Connection }); }
                        catch (Exception ex) { GatedLogger.Debug($"[CosmeticBinder] HideForConnection failed: {ex.Message}"); }
                    }
                }
            }

            return set;
        }

        private static SpawnedPart SpawnPart(PartBinding binding, Transform bone)
        {
            var worldPos = bone.TransformPoint(binding.LocalPosition);
            var worldRot = bone.rotation * Quaternion.Euler(binding.LocalRotation);

            var schematic = ObjectSpawner.SpawnSchematic(binding.SchematicName, worldPos, worldRot, binding.Scale);
            if (schematic?.gameObject == null)
            {
                Log.Error($"[CosmeticBinder] Failed to spawn schematic '{binding.SchematicName}'");
                return null;
            }
            var obj = schematic.gameObject;

            foreach (var rb in obj.GetComponentsInChildren<Rigidbody>())
                rb.isKinematic = true;
            foreach (var col in obj.GetComponentsInChildren<Collider>())
                col.isTrigger = true;

            var allToys = obj.GetComponentsInChildren<AdminToyBase>(true);
            if (allToys.Length == 0)
            {
                Log.Warn($"[CosmeticBinder] Schematic '{binding.SchematicName}' has no AdminToyBase children");
                NetworkServer.Destroy(obj);
                return null;
            }

            var rootAtb = obj.GetComponent<AdminToyBase>() ?? allToys[0];

            if (binding.IsStatic)
            {
                Timing.RunCoroutine(DelayedStaticLock(allToys));

                GatedLogger.Debug($"[CosmeticBinder] Static '{binding.SchematicName}' on '{binding.BoneName}'");
                return new SpawnedPart
                {
                    SchematicRoot = obj,
                    Schematic = schematic,
                    RootToy = null,
                    HideForWearer = binding.HideForWearer,
                    IsStatic = true
                };
            }
            else
            {
                bool isLimb = IsLimbBone(binding.BoneName);

                // syncInterval=0 flushes every server tick; limb bones snap, core bones interpolate -
                // splitting fixes the observer-visible leg crisscross without sacrificing translation
                rootAtb.syncInterval = 0f;
                rootAtb.NetworkMovementSmoothing = isLimb ? CosmeticTracker.LimbSmoothing : CosmeticTracker.CoreSmoothing;

                // Lock non-root toys in world-space so vanilla LateUpdate doesn't race our writes
                Timing.RunCoroutine(DelayedChildStaticLock(allToys, rootAtb));
                CosmeticTracker.Register(rootAtb, bone, binding.LocalPosition,
                                         Quaternion.Euler(binding.LocalRotation), isLimb);

                GatedLogger.Debug($"[CosmeticBinder] Tracked '{binding.SchematicName}' on '{binding.BoneName}' ({(isLimb ? "limb" : "core")})");
                return new SpawnedPart
                {
                    SchematicRoot = obj,
                    Schematic = schematic,
                    RootToy = rootAtb,
                    HideForWearer = binding.HideForWearer,
                    IsStatic = false
                };
            }
        }

        private static IEnumerator<float> DelayedStaticLock(AdminToyBase[] toys)
        {
            yield return Timing.WaitForSeconds(0.1f);
            foreach (var atb in toys)
                if (atb != null) atb.NetworkIsStatic = true;
        }

        // Locks all child toys except rootAtb so they stay frozen while rootAtb is tracked
        private static IEnumerator<float> DelayedChildStaticLock(AdminToyBase[] allToys, AdminToyBase rootAtb)
        {
            yield return Timing.WaitForSeconds(0.1f);
            foreach (var atb in allToys)
                if (atb != null && atb != rootAtb) atb.NetworkIsStatic = true;
        }

        #endregion

        #region Destruction

        private static void DestroyCosmeticSet(CosmeticSet set)
        {
            foreach (var part in set.Parts)
            {
                if (part.RootToy != null)
                    CosmeticTracker.Unregister(part.RootToy);

                if (part.SchematicRoot != null)
                {
                    try { NetworkServer.Destroy(part.SchematicRoot); }
                    catch (Exception ex) { GatedLogger.Debug($"[CosmeticBinder] Error destroying schematic: {ex.Message}"); }
                }
            }
        }

        private static void CleanupEmptyEntry(Player player)
        {
            if (!ActiveCosmetics.TryGetValue(player, out var c)) return;
            if (c.Suit == null && c.Weapon == null)
            {
                ActiveCosmetics.Remove(player);
                StopCoroutinesIfEmpty();
            }
        }

        #endregion

        #region Visibility

        public static void SetVisibilityForViewer(CosmeticSet set, Player viewer, bool visible, bool respectHideForWearer = false)
        {
            var method = visible ? ShowForConnectionMethod : HideForConnectionMethod;
            if (method == null || viewer.Connection == null) return;

            foreach (var part in set.Parts)
            {
                if (visible && respectHideForWearer && part.HideForWearer) continue;

                var identities = part.Schematic?.NetworkIdentities;
                if (identities == null) continue;
                foreach (var netId in identities)
                {
                    try { method.Invoke(null, new object[] { netId, viewer.Connection }); }
                    catch (Exception ex) { GatedLogger.Debug($"[CosmeticBinder] Visibility toggle failed: {ex.Message}"); }
                }
            }
        }

        #endregion

        #region Coroutines

        private static void EnsureCoroutinesRunning()
        {
            if (!isCleanupRunning)
            {
                cleanupCoroutine = Timing.RunCoroutine(CleanupLoop());
                isCleanupRunning = true;
            }

            var config = SLWardrobe.Instance.Config;
            if (config.LodDistance > 0 && !isLodRunning
                && ShowForConnectionMethod != null && HideForConnectionMethod != null)
            {
                lodCoroutine = Timing.RunCoroutine(LodLoop((float)config.LodDistance, (float)config.LodCheckInterval));
                isLodRunning = true;
            }
        }

        private static void StopCoroutinesIfEmpty()
        {
            if (ActiveCosmetics.Count > 0) return;
            StopAllCoroutines();
        }

        public static void StopAllCoroutines()
        {
            if (isCleanupRunning) { Timing.KillCoroutines(cleanupCoroutine); isCleanupRunning = false; }
            if (isLodRunning) { Timing.KillCoroutines(lodCoroutine); isLodRunning = false; }
        }

        private static IEnumerator<float> CleanupLoop()
        {
            var toRemove = new List<Player>();
            while (true)
            {
                toRemove.Clear();
                foreach (var kvp in ActiveCosmetics)
                    if (kvp.Key == null || !kvp.Key.IsAlive) toRemove.Add(kvp.Key);
                foreach (var p in toRemove) RemoveAll(p);
                yield return Timing.WaitForSeconds(0.5f);
            }
        }

        private static IEnumerator<float> LodLoop(float defaultLodDistance, float interval)
        {
            while (true)
            {
                foreach (var kvp in ActiveCosmetics)
                {
                    var wearer = kvp.Key;
                    if (wearer == null || !wearer.IsAlive) continue;

                    var wearerPos = wearer.Position;
                    var cosmetics = kvp.Value;

                    foreach (var viewer in Player.List)
                    {
                        if (viewer == wearer || viewer == null || !viewer.IsAlive) continue;

                        float lodDist = defaultLodDistance;
                        var ssss = SLWardrobe.Instance?.SsssHandler;
                        if (ssss != null) lodDist = ssss.GetEffectiveLodDistance(viewer);

                        bool shouldHide = (viewer.Position - wearerPos).sqrMagnitude > lodDist * lodDist;

                        if (cosmetics.Suit != null) UpdateLod(cosmetics.Suit, viewer, shouldHide);
                        if (cosmetics.Weapon != null) UpdateLod(cosmetics.Weapon, viewer, shouldHide);
                    }

                    if (cosmetics.Suit != null) PruneViewers(cosmetics.Suit);
                    if (cosmetics.Weapon != null) PruneViewers(cosmetics.Weapon);
                }
                yield return Timing.WaitForSeconds(interval);
            }
        }

        private static void UpdateLod(CosmeticSet set, Player viewer, bool shouldHide)
        {
            bool hidden = set.HiddenViewers.Contains(viewer);
            if (shouldHide && !hidden) { SetVisibilityForViewer(set, viewer, false); set.HiddenViewers.Add(viewer); }
            else if (!shouldHide && hidden) { SetVisibilityForViewer(set, viewer, true); set.HiddenViewers.Remove(viewer); }
        }

        private static void PruneViewers(CosmeticSet set)
        {
#if EXILED
            set.HiddenViewers.RemoveWhere(p => p == null || !p.IsConnected);
#else
            set.HiddenViewers.RemoveWhere(p => p == null || !p.Connection.isReady);
#endif
        }

        #endregion

        #region Helpers

        private static PlayerCosmetics GetOrCreateCosmetics(Player player)
        {
            if (!ActiveCosmetics.TryGetValue(player, out var c))
            {
                c = new PlayerCosmetics();
                ActiveCosmetics[player] = c;
            }
            return c;
        }

        public static string GetDebugStatus()
        {
            int suits = 0, weapons = 0;
            foreach (var c in ActiveCosmetics.Values)
            {
                if (c.Suit != null) suits++;
                if (c.Weapon != null) weapons++;
            }
            return $"Players: {ActivePlayerCount} | Suits: {suits} | Weapons: {weapons} | " +
                   $"Tracked: {CosmeticTracker.TrackedCount} | Harmony blocks: {CosmeticTracker.PrefixBlockCount} | " +
                   $"Cleanup: {(isCleanupRunning ? "Running" : "Stopped")} | LOD: {(isLodRunning ? "Running" : "Off")}";
        }

        #endregion
    }
}
