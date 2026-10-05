using System.Collections.Generic;
using UnityEngine;
using MEC;
using SLWardrobe.Common;
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
    public static class WeaponDetector
    {
        private static readonly Dictionary<string, IItemMatcher> Matchers = new Dictionary<string, IItemMatcher>();
        private static CoroutineHandle updateCoroutine;
        private static bool isRunning;
        public static int MatcherCount => Matchers.Count;

        public static void Initialize()
        {
            Matchers.Clear();
            foreach (var kvp in ConfigLoader.Weapons)
            {
                var matcher = ItemMatcherFactory.Create(kvp.Value.Detection);
                Matchers[kvp.Key] = matcher;
                GatedLogger.Debug($"[WeaponDetector] Registered: {kvp.Key} ({matcher.Description})");
            }
            Log.Info($"[WeaponDetector] Initialized with {Matchers.Count} weapon(s)");

#if EXILED
            if (Matchers.Count > 0 && Round.IsStarted && !isRunning)
#else
            if (Matchers.Count > 0 && Round.IsRoundStarted && !isRunning)
#endif
                StartUpdater((float)SLWardrobe.Instance.Config.UpdateInterval);
        }

        public static void StartUpdater(float interval)
        {
            if (!isRunning && Matchers.Count > 0)
            {
                updateCoroutine = Timing.RunCoroutine(PollLoop(interval));
                isRunning = true;
            }
        }

        public static void StopUpdater()
        {
            if (isRunning) { Timing.KillCoroutines(updateCoroutine); isRunning = false; }
        }

        public static void RemoveAllWeapons()
        {
            foreach (var player in Player.List) CosmeticBinder.RemoveWeapon(player);
            StopUpdater();
        }

        private static IEnumerator<float> PollLoop(float interval)
        {
            var toDetach = new List<Player>();
            while (true)
            {
                toDetach.Clear();
                foreach (var player in Player.List)
                {
                    if (player == null || !player.IsAlive)
                    {
                        if (CosmeticBinder.GetWeaponData(player) != null) toDetach.Add(player);
                        continue;
                    }

                    var currentItem = player.CurrentItem;
                    var activeWeapon = CosmeticBinder.GetWeaponData(player);

                    string matchedName = null;
                    foreach (var kvp in Matchers)
                    {
                        if (kvp.Value.Matches(currentItem, player)) { matchedName = kvp.Key; break; }
                    }

                    if (matchedName != null)
                    {
                        if (activeWeapon == null || activeWeapon.Name != matchedName)
                        {
                            if (activeWeapon != null) CosmeticBinder.RemoveWeapon(player);
                            ApplyWeaponFromConfig(player, matchedName);
                        }
                    }
                    else if (activeWeapon != null) toDetach.Add(player);
                }
                foreach (var player in toDetach) CosmeticBinder.RemoveWeapon(player);
                yield return Timing.WaitForSeconds(interval);
            }
        }

        private static void ApplyWeaponFromConfig(Player player, string weaponName)
        {
            var definition = ConfigLoader.GetWeapon(weaponName);
            if (definition == null) { Log.Warn($"[WeaponDetector] Definition not found: {weaponName}"); return; }

            var bindings = new List<PartBinding>();
            foreach (var partDef in definition.Parts)
            {
                bindings.Add(new PartBinding
                {
                    SchematicName = partDef.SchematicName,
                    BoneName = definition.AttachBone,
                    WearerType = definition.WearerType,
                    LocalPosition = new Vector3((float)partDef.PositionX, (float)partDef.PositionY, (float)partDef.PositionZ),
                    LocalRotation = new Vector3((float)partDef.RotationX, (float)partDef.RotationY, (float)partDef.RotationZ),
                    Scale = new Vector3((float)partDef.ScaleX, (float)partDef.ScaleY, (float)partDef.ScaleZ),
                    HideForWearer = partDef.HideForWearer,
                    IsStatic = partDef.Static
                });
            }
            CosmeticBinder.ApplyWeapon(player, weaponName, bindings);
        }

        public static string GetDebugStatus() => $"Updater: {(isRunning ? "Running" : "Stopped")} | Matchers: {Matchers.Count}";
    }
}