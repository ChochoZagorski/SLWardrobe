using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using MEC;
using UnityEngine;
using PlayerRoles;
using SLWardrobe.Models;
using SLWardrobe.Weapons;

#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Loader.Features.Plugins;
using LabApi.Loader.Features.Plugins.Enums;
using LabApi.Loader;
using Log = LabApi.Features.Console.Logger;
#endif

namespace SLWardrobe
{
    public class SLWardrobe : Plugin<Config>
    {
        public override string Name => "SLWardrobe";
        public override string Author => "ChochoZagorski";
        public override Version Version => new Version(1, 8, 0);

#if EXILED
        public override string Prefix => "sl_wardrobe";
        public override Version RequiredExiledVersion => new Version(9, 13, 1);
        public override Exiled.API.Enums.PluginPriority Priority => Exiled.API.Enums.PluginPriority.Default;
#else
        public override string Description => "A plugin allowing players to \"wear\" schematics made with ProjectMER in SCP: Secret Laboratory";
        public override Version RequiredApiVersion => new Version(1, 1, 5);
        public override LoadPriority Priority => LoadPriority.Medium;
#endif

        public static SLWardrobe Instance { get; private set; }

        private readonly Dictionary<Player, string> playerSuitNames = new Dictionary<Player, string>();
        private static readonly HttpClient HttpClient = new HttpClient();

        private const string VERSION_URL = "https://raw.githubusercontent.com/ChochoZagorski/SLWardrobe/master/version.txt";
        private const string UPDATE_JSON_URL = "https://raw.githubusercontent.com/ChochoZagorski/SLWardrobe/master/update_info.json";

        public SsssHandler SsssHandler { get; private set; }

        public static string BuildFramework
        {
            get
            {
#if EXILED
                return "exiled";
#else
                return "labapi";
#endif
            }
        }

#if EXILED
        public override void OnEnabled()
        {
            PluginEnabled();
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            PluginDisabled();
            base.OnDisabled();
        }
#else
        public override void Enable()
        {
            PluginEnabled();
        }

        public override void Disable()
        {
            PluginDisabled();
        }
#endif

        private void PluginEnabled()
        {
            Instance = this;

#if EXILED
            ConfigLoader.SetPluginFolder(System.IO.Path.Combine(Paths.Configs, "SLWardrobe"));
#else
            ConfigLoader.SetPluginFolder(this.GetConfigDirectory().FullName);
#endif

            ConfigLoader.LoadAll();
            WeaponBinder.Initialize();

            SsssHandler = new SsssHandler();
            SsssHandler.Register();

#if EXILED
            Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
            Exiled.Events.Handlers.Player.Left += OnPlayerLeft;
            Exiled.Events.Handlers.Player.Died += OnPlayerDied;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;
#else
            PlayerEvents.ChangingRole += OnChangingRole;
            PlayerEvents.Left += OnPlayerLeft;
            PlayerEvents.Death += OnPlayerDied;
            ServerEvents.RoundStarted += OnRoundStarted;
            ServerEvents.RoundEnded += OnRoundEnded;
#endif

            if (Config.CheckForUpdates)
                Task.Run(async () => await CheckForUpdates());
        }

        private void PluginDisabled()
        {
            SsssHandler?.Unregister();
            SsssHandler = null;

            SuitBinder.StopGlobalUpdater();
            SuitBinder.StopLodUpdater();
            WeaponBinder.StopUpdater();
            WeaponBinder.RemoveAllWeapons();

#if EXILED
            Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
            Exiled.Events.Handlers.Player.Left -= OnPlayerLeft;
            Exiled.Events.Handlers.Player.Died -= OnPlayerDied;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;
#else
            PlayerEvents.ChangingRole -= OnChangingRole;
            PlayerEvents.Left -= OnPlayerLeft;
            PlayerEvents.Death -= OnPlayerDied;
            ServerEvents.RoundStarted -= OnRoundStarted;
            ServerEvents.RoundEnded -= OnRoundEnded;
#endif

            Instance = null;
        }

        #region Event Handlers

        private void OnRoundStarted()
        {
            Log.Debug("[SLWardrobe] Round started.");
            WeaponBinder.Initialize();

            if (ConfigLoader.Weapons.Count > 0)
            {
                WeaponBinder.StartUpdater((float)Config.UpdateInterval);
                Log.Debug($"[SLWardrobe] Weapon updater started ({ConfigLoader.Weapons.Count} weapons)");
            }
        }

#if EXILED
        private void OnChangingRole(Exiled.Events.EventArgs.Player.ChangingRoleEventArgs ev)
        {
            if (ev.NewRole == RoleTypeId.None || ev.NewRole == RoleTypeId.Spectator)
                CleanupPlayer(ev.Player);
        }

        private void OnPlayerDied(Exiled.Events.EventArgs.Player.DiedEventArgs ev)
        {
            CleanupPlayer(ev.Player);
        }

        private void OnPlayerLeft(Exiled.Events.EventArgs.Player.LeftEventArgs ev)
        {
            CleanupPlayer(ev.Player);
            SsssHandler?.CleanupPlayer(ev.Player);
            playerSuitNames.Remove(ev.Player);
        }

        private void OnRoundEnded(Exiled.Events.EventArgs.Server.RoundEndedEventArgs ev)
        {
            RoundCleanup();
        }
#else
        private void OnChangingRole(PlayerChangingRoleEventArgs ev)
        {
            if (ev.NewRole == RoleTypeId.None || ev.NewRole == RoleTypeId.Spectator)
            {
                var player = Player.Get(ev.Player.ReferenceHub);
                if (player != null) CleanupPlayer(player);
            }
        }

        private void OnPlayerDied(PlayerDeathEventArgs ev)
        {
            var player = Player.Get(ev.Player.ReferenceHub);
            if (player != null) CleanupPlayer(player);
        }

        private void OnPlayerLeft(PlayerLeftEventArgs ev)
        {
            var player = Player.Get(ev.Player.ReferenceHub);
            if (player == null) return;

            CleanupPlayer(player);
            SsssHandler?.CleanupPlayer(player);
            playerSuitNames.Remove(player);
        }

        private void OnRoundEnded(RoundEndedEventArgs ev)
        {
            RoundCleanup();
        }
#endif

        private void RoundCleanup()
        {
            foreach (var player in Player.List)
                SuitBinder.RemoveSuit(player);

            playerSuitNames.Clear();
            SuitBinder.StopGlobalUpdater();
            SuitBinder.StopLodUpdater();
            WeaponBinder.RemoveAllWeapons();
        }

        private void CleanupPlayer(Player player)
        {
            SuitBinder.RemoveSuit(player);
            SuitBinder.SetPlayerInvisibility(player, false);
            WeaponBinder.RemoveWeapon(player);
        }

        #endregion

        #region Suit Application

        public void ApplySuit(Player player, string suitName)
        {
            if (!Config.IsEnabled) return;
            if (player.Role == RoleTypeId.None || player.Role == RoleTypeId.Spectator) return;
            Timing.RunCoroutine(ApplySuitCoroutine(player, suitName));
        }

        private IEnumerator<float> ApplySuitCoroutine(Player player, string suitName)
        {
            yield return Timing.WaitForSeconds(0.5f);

            if (player == null || !player.IsAlive) yield break;

            var definition = ConfigLoader.GetSuit(suitName);
            if (definition == null)
            {
                Log.Warn($"[SLWardrobe] Unknown suit: {suitName}");
                yield break;
            }

            var bindings = ConvertDefinitionToBindings(definition);
            SuitBinder.ApplySuit(player, bindings);
            playerSuitNames[player] = suitName;

            if (definition.MakeWearerInvisible)
                SuitBinder.SetPlayerInvisibility(player, true);

            yield return Timing.WaitForSeconds(1f);

            var suitData = SuitBinder.GetSuitData(player);
            if (suitData != null)
            {
                int active = suitData.Parts.Count(p => p.GameObject != null);
                Log.Debug($"[SLWardrobe] Suit '{suitName}' applied to {player.Nickname} ({active} active parts)");
            }
        }

        private List<BoneBinding> ConvertDefinitionToBindings(SuitDefinition definition)
        {
            var bindings = new List<BoneBinding>();

            foreach (var part in definition.Parts)
            {
                var binding = new BoneBinding(
                    part.SchematicName,
                    part.BoneName,
                    definition.WearerType,
                    new Vector3((float)part.PositionX, (float)part.PositionY, (float)part.PositionZ),
                    new Vector3((float)part.RotationX, (float)part.RotationY, (float)part.RotationZ),
                    new Vector3((float)part.ScaleX,    (float)part.ScaleY,    (float)part.ScaleZ)
                );

                binding.HideForWearer = part.HideForWearer;
                binding.IsStaticPart = part.Static;
                bindings.Add(binding);
            }

            return bindings;
        }

        public string GetPlayerSuitName(Player player)
        {
            return playerSuitNames.TryGetValue(player, out var name) ? name : null;
        }

        #endregion

        #region Version Checking

        private async Task CheckForUpdates()
        {
            try
            {
                HttpClient.DefaultRequestHeaders.Clear();
                HttpClient.DefaultRequestHeaders.Add("User-Agent", $"SLWardrobe/{Version} ({BuildFramework})");

                if (await TryJsonVersionCheck())
                    return;

                await FallbackVersionCheck();
            }
            catch (Exception ex)
            {
                Log.Debug($"[SLWardrobe] Could not check for updates: {ex.Message}");
            }
        }

        private async Task<bool> TryJsonVersionCheck()
        {
            try
            {
                var json = await HttpClient.GetStringAsync(UPDATE_JSON_URL);
                var info = System.Text.Json.JsonSerializer.Deserialize<UpdateInfo>(json);

                if (info == null || string.IsNullOrEmpty(info.LatestVersion))
                    return false;

                if (!System.Version.TryParse(info.LatestVersion, out var latest))
                    return false;

                var current = Version;
                string framework = BuildFramework;

                string severity = GetFrameworkValue(info.Severity, info.ExiledSeverity, info.LabApiSeverity, framework) ?? "none";
                string alertMsg = GetFrameworkValue(info.AlertMessage, info.ExiledAlertMessage, info.LabApiAlertMessage, framework);
                string minSafeStr = GetFrameworkValue(info.MinimumSafeVersion, info.ExiledMinimumSafeVersion, info.LabApiMinimumSafeVersion, framework);

                if (latest > current)
                {
                    switch (severity.ToLower())
                    {
                        case "critical":
                        case "security":
                            Log.Error("=========================================");
                            Log.Error($"[SLWardrobe] CRITICAL UPDATE AVAILABLE: {current} -> {latest}");
                            if (!string.IsNullOrEmpty(alertMsg))
                                Log.Error($"[SLWardrobe] {alertMsg}");
                            Log.Error("[SLWardrobe] https://github.com/ChochoZagorski/SLWardrobe/releases/latest");
                            Log.Error("=========================================");
                            break;

                        case "important":
                            Log.Warn($"[SLWardrobe] Important update available: {current} -> {latest}");
                            if (!string.IsNullOrEmpty(info.Changelog))
                                Log.Warn($"[SLWardrobe] Changes: {info.Changelog}");
                            Log.Warn("[SLWardrobe] https://github.com/ChochoZagorski/SLWardrobe/releases/latest");
                            break;

                        case "none":
                        case "skip":
                            Log.Info($"[SLWardrobe] Update {latest} available but not relevant for your {framework} build.");
                            break;

                        default:
                            Log.Warn($"[SLWardrobe] New version available: {current} -> {latest}");
                            if (!string.IsNullOrEmpty(info.Changelog))
                                Log.Info($"[SLWardrobe] Changes: {info.Changelog}");
                            Log.Info("[SLWardrobe] https://github.com/ChochoZagorski/SLWardrobe/releases/latest");
                            break;
                    }

                    if (!string.IsNullOrEmpty(minSafeStr) &&
                        System.Version.TryParse(minSafeStr, out var minSafe) &&
                        current < minSafe)
                    {
                        Log.Error($"[SLWardrobe] Your version ({current}) is below minimum safe ({minSafe}) for {framework}. Update immediately.");
                    }
                }
                else if (latest < current)
                {
                    Log.Info($"[SLWardrobe] Running development version: {current} (latest stable: {latest})");
                }
                else
                {
                    Log.Info($"[SLWardrobe] Running latest version ({current})");
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string GetFrameworkValue(string global, string exiledValue, string labApiValue, string framework)
        {
            string specific = framework == "labapi" ? labApiValue : exiledValue;
            return !string.IsNullOrEmpty(specific) ? specific : global;
        }

        private async Task FallbackVersionCheck()
        {
            string latestVersion = (await HttpClient.GetStringAsync(VERSION_URL)).Trim();

            if (System.Version.TryParse(latestVersion, out var latest) && latest > Version)
            {
                Log.Warn($"[SLWardrobe] New version available! Current: {Version} | Latest: {latestVersion}");
                Log.Warn("[SLWardrobe] https://github.com/ChochoZagorski/SLWardrobe/releases/latest");
            }
            else if (latest != null && latest < Version)
            {
                Log.Info($"[SLWardrobe] Running development version: {Version} (latest stable: {latestVersion})");
            }
            else
            {
                Log.Info($"[SLWardrobe] Running latest version ({Version})");
            }
        }

        #endregion
    }

    public class UpdateInfo
    {
        public string LatestVersion { get; set; }
        public string Changelog { get; set; }

        public string Severity { get; set; }
        public string AlertMessage { get; set; }
        public string MinimumSafeVersion { get; set; }

        public string ExiledSeverity { get; set; }
        public string ExiledAlertMessage { get; set; }
        public string ExiledMinimumSafeVersion { get; set; }

        public string LabApiSeverity { get; set; }
        public string LabApiAlertMessage { get; set; }
        public string LabApiMinimumSafeVersion { get; set; }
    }
}