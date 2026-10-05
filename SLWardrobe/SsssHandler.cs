using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UserSettings.ServerSpecific;
using SLWardrobe.Common;
using SLWardrobe.Models;

#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
using Log = LabApi.Features.Console.Logger;
#endif

namespace SLWardrobe
{
    public class SsssHandler
    {
        private const int IdSuitToggle = 770;
        private const int IdLodSlider = 771;
        private const int IdFadeRequest = 772;

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
                ServerSpecificSettingsSync.DefinedSettings = ServerSpecificSettingsSync.DefinedSettings
                    .Where(s => s.SettingId < IdSuitToggle || s.SettingId > IdFadeRequest).ToArray();

            lodOverrides.Clear();
            selfHiddenPlayers.Clear();
        }

#if EXILED
        private void OnPlayerVerified(Exiled.Events.EventArgs.Player.VerifiedEventArgs ev) => SendSettingsToPlayer(ev.Player);
#else
        private void OnPlayerJoined(LabApi.Events.Arguments.PlayerEvents.PlayerJoinedEventArgs ev)
        {
            var player = Player.Get(ev.Player.ReferenceHub);
            if (player != null) SendSettingsToPlayer(player);
        }
#endif

        private void SendSettingsToPlayer(Player player)
        {
            try { ServerSpecificSettingsSync.SendToPlayer(player.ReferenceHub); }
            catch (Exception ex) { GatedLogger.Debug($"[SSSS] Could not send settings to {player.Nickname}: {ex.Message}"); }
        }

        private void OnSettingValueReceived(ReferenceHub hub, ServerSpecificSettingBase setting)
        {
            var player = Player.Get(hub);
            if (player == null) return;
            var config = SLWardrobe.Instance.Config.Ssss;

            switch (setting.SettingId)
            {
                case IdSuitToggle when config.AllowSuitToggle: HandleSuitToggle(player, setting); break;
                case IdLodSlider when config.AllowLodControl: HandleLodChange(player, setting); break;
                case IdFadeRequest when SLWardrobe.Instance.Config.FadeRequest.Enabled:
                    if (setting is SSButton) SLWardrobe.Instance.FadeHandler?.HandleRequest(player);
                    break;
            }
        }

        private void HandleSuitToggle(Player player, ServerSpecificSettingBase setting)
        {
            if (!(setting is SSTwoButtonsSetting toggle)) return;
            bool wantsHidden = toggle.SyncIsB;
            var suitData = CosmeticBinder.GetSuitData(player);
            if (suitData == null) return;

            if (wantsHidden && !selfHiddenPlayers.Contains(player))
            {
                CosmeticBinder.SetVisibilityForViewer(suitData, player, false);
                selfHiddenPlayers.Add(player);
            }
            else if (!wantsHidden && selfHiddenPlayers.Contains(player))
            {
                CosmeticBinder.SetVisibilityForViewer(suitData, player, true, respectHideForWearer: true);
                selfHiddenPlayers.Remove(player);
            }
        }

        private void HandleLodChange(Player player, ServerSpecificSettingBase setting)
        {
            if (!(setting is SSSliderSetting slider)) return;
            var config = SLWardrobe.Instance.Config.Ssss;
            lodOverrides[player] = Mathf.Clamp(slider.SyncFloatValue, (float)config.MinLodDistance, (float)config.MaxLodDistance);
        }

        public float GetEffectiveLodDistance(Player viewer)
            => lodOverrides.TryGetValue(viewer, out var d) ? d : (float)SLWardrobe.Instance.Config.LodDistance;

        public void CleanupPlayer(Player player) { lodOverrides.Remove(player); selfHiddenPlayers.Remove(player); }

        /// <summary>
        /// Re-applies the player's persisted suit-visibility preference after a suit spawns.
        /// Called from ApplySuitCoroutine because selfHiddenPlayers is cleared on plugin reload -
        /// TryGetSettingOfUser reads the client's saved SSSS value as the durable source.
        /// </summary>
        public void ApplyPersistedSuitVisibility(Player player)
        {
            if (!SLWardrobe.Instance.Config.Ssss.AllowSuitToggle) return;
            var suit = CosmeticBinder.GetSuitData(player);
            if (suit == null) return;

            try
            {
                if (ServerSpecificSettingsSync.TryGetSettingOfUser<SSTwoButtonsSetting>(
                        player.ReferenceHub, IdSuitToggle, out var toggle)
                    && toggle != null && toggle.SyncIsB)
                {
                    CosmeticBinder.SetVisibilityForViewer(suit, player, false);
                    selfHiddenPlayers.Add(player);
                }
            }
            catch (Exception ex)
            {
                GatedLogger.Debug($"[SSSS] Could not read persisted suit visibility for {player.Nickname}: {ex.Message}");
            }
        }

        private List<ServerSpecificSettingBase> BuildSettingsList(SsssConfig config)
        {
            var settings = new List<ServerSpecificSettingBase> { new SSGroupHeader("SLWardrobe") };
            if (config.AllowSuitToggle)
                settings.Add(new SSTwoButtonsSetting(IdSuitToggle, "Suit Visibility (Own View)", "Show", "Hide"));
            if (config.AllowLodControl && SLWardrobe.Instance.Config.LodDistance > 0)
                settings.Add(new SSSliderSetting(IdLodSlider, "Cosmetic Render Distance",
                    (float)config.MinLodDistance, (float)config.MaxLodDistance,
                    (float)SLWardrobe.Instance.Config.LodDistance, true));
            if (SLWardrobe.Instance.Config.FadeRequest.Enabled)
                settings.Add(new SSButton(IdFadeRequest, "Request Body Fade", "Request", null,
                    "Ask an admin to hide your body model (reduces suit misalignment for observers)"));
            return settings;
        }
    }
}