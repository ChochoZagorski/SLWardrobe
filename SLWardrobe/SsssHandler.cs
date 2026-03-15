using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mirror;
using UnityEngine;
using UserSettings.ServerSpecific;

#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
using Log = LabApi.Features.Console.Logger;
#endif

namespace SLWardrobe
{
    /// Integrates with SCP:SL's Server Specific Settings Sync (SSSS) system.
    /// Gives players in-game controls for suit visibility and cosmetic render distance.
    public class SsssHandler
    {
        private const int IdSuitToggle = 770;
        private const int IdLodSlider = 771;

        private static readonly MethodInfo HideForConnectionMethod = typeof(NetworkServer)
            .GetMethod("HideForConnection", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly MethodInfo ShowForConnectionMethod = typeof(NetworkServer)
            .GetMethod("ShowForConnection", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        private readonly Dictionary<Player, float> lodOverrides = new Dictionary<Player, float>();
        private readonly HashSet<Player> selfHiddenPlayers = new HashSet<Player>();

        public void Register()
        {
            var config = SLWardrobe.Instance.Config.Ssss;
            if (!config.Enabled) return;

            var settings = BuildSettingsList(config);
            if (settings.Count == 0) return;

            var existing = ServerSpecificSettingsSync.DefinedSettings?.ToList()
                           ?? new List<ServerSpecificSettingBase>();
            existing.AddRange(settings);
            ServerSpecificSettingsSync.DefinedSettings = existing.ToArray();

            ServerSpecificSettingsSync.ServerOnSettingValueReceived += OnSettingValueReceived;

#if EXILED
            Exiled.Events.Handlers.Player.Verified += OnPlayerVerified;
#else
            LabApi.Events.Handlers.PlayerEvents.Joined += OnPlayerJoined;
#endif

            Log.Info($"[SSSS] Registered {settings.Count} setting(s).");
        }

        public void Unregister()
        {
            ServerSpecificSettingsSync.ServerOnSettingValueReceived -= OnSettingValueReceived;

#if EXILED
            Exiled.Events.Handlers.Player.Verified -= OnPlayerVerified;
#else
            LabApi.Events.Handlers.PlayerEvents.Joined -= OnPlayerJoined;
#endif

            if (ServerSpecificSettingsSync.DefinedSettings != null)
            {
                ServerSpecificSettingsSync.DefinedSettings = ServerSpecificSettingsSync.DefinedSettings
                    .Where(s => s.SettingId < IdSuitToggle || s.SettingId > IdLodSlider)
                    .ToArray();
            }

            lodOverrides.Clear();
            selfHiddenPlayers.Clear();

            Log.Debug("[SSSS] Unregistered.");
        }

#if EXILED
        private void OnPlayerVerified(Exiled.Events.EventArgs.Player.VerifiedEventArgs ev)
        {
            SendSettingsToPlayer(ev.Player);
        }
#else
        private void OnPlayerJoined(LabApi.Events.Arguments.PlayerEvents.PlayerJoinedEventArgs ev)
        {
            var player = Player.Get(ev.Player.ReferenceHub);
            if (player != null)
                SendSettingsToPlayer(player);
        }
#endif

        private void SendSettingsToPlayer(Player player)
        {
            try
            {
                ServerSpecificSettingsSync.SendToPlayer(player.ReferenceHub);
            }
            catch (Exception ex)
            {
                Log.Debug($"[SSSS] Could not send settings to {player.Nickname}: {ex.Message}");
            }
        }

        private void OnSettingValueReceived(ReferenceHub hub, ServerSpecificSettingBase setting)
        {
            var player = Player.Get(hub);
            if (player == null) return;

            var config = SLWardrobe.Instance.Config.Ssss;

            switch (setting.SettingId)
            {
                case IdSuitToggle when config.AllowSuitToggle:
                    HandleSuitToggle(player, setting);
                    break;

                case IdLodSlider when config.AllowLodControl:
                    HandleLodChange(player, setting);
                    break;
            }
        }

        private void HandleSuitToggle(Player player, ServerSpecificSettingBase setting)
        {
            if (!(setting is SSTwoButtonsSetting toggle)) return;

            bool wantsHidden = toggle.SyncIsB;
            var suitData = SuitBinder.GetSuitData(player);

            if (suitData == null)
            {
                Log.Debug($"[SSSS] {player.Nickname} toggled suit visibility but has no suit.");
                return;
            }

            if (wantsHidden && !selfHiddenPlayers.Contains(player))
            {
                SetOwnSuitVisibility(player, suitData, false);
                selfHiddenPlayers.Add(player);
                Log.Debug($"[SSSS] {player.Nickname} hid their own suit.");
            }
            else if (!wantsHidden && selfHiddenPlayers.Contains(player))
            {
                SetOwnSuitVisibility(player, suitData, true);
                selfHiddenPlayers.Remove(player);
                Log.Debug($"[SSSS] {player.Nickname} showed their own suit.");
            }
        }

        private void HandleLodChange(Player player, ServerSpecificSettingBase setting)
        {
            if (!(setting is SSSliderSetting slider)) return;

            float value = slider.SyncFloatValue;
            var config = SLWardrobe.Instance.Config.Ssss;

            value = Mathf.Clamp(value, (float)config.MinLodDistance, (float)config.MaxLodDistance);

            lodOverrides[player] = value;
            Log.Debug($"[SSSS] {player.Nickname} set LOD distance to {value}m.");
        }
        
        private void SetOwnSuitVisibility(Player player, SuitData suitData, bool visible)
        {
            if (player.Connection == null) return;

            foreach (var part in suitData.Parts)
            {
                if (visible && part.Binding.HideForWearer)
                    continue;

                if (part.Schematic?.NetworkIdentities == null) continue;

                var method = visible ? ShowForConnectionMethod : HideForConnectionMethod;
                if (method == null) continue;

                foreach (var netId in part.Schematic.NetworkIdentities)
                {
                    try { method.Invoke(null, new object[] { netId, player.Connection }); }
                    catch { }
                }
            }
        }

        public float GetEffectiveLodDistance(Player viewer)
        {
            if (lodOverrides.TryGetValue(viewer, out var distance))
                return distance;

            return (float)SLWardrobe.Instance.Config.LodDistance;
        }

        public void CleanupPlayer(Player player)
        {
            lodOverrides.Remove(player);
            selfHiddenPlayers.Remove(player);
        }

        private List<ServerSpecificSettingBase> BuildSettingsList(SsssConfig config)
        {
            var settings = new List<ServerSpecificSettingBase>();

            settings.Add(new SSGroupHeader("SLWardrobe"));

            if (config.AllowSuitToggle)
            {
                settings.Add(new SSTwoButtonsSetting(
                    IdSuitToggle,
                    "Suit Visibility (Own View)",
                    "Show",
                    "Hide"
                ));
            }

            if (config.AllowLodControl && SLWardrobe.Instance.Config.LodDistance > 0)
            {
                settings.Add(new SSSliderSetting(
                    IdLodSlider,
                    "Cosmetic Render Distance",
                    (float)config.MinLodDistance,
                    (float)config.MaxLodDistance,
                    (float)SLWardrobe.Instance.Config.LodDistance,
                    true
                ));
            }

            return settings;
        }
    }
}