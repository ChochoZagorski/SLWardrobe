using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using MEC;
using UnityEngine;
using PlayerRoles;
using Exiled.API.Enums;
using Exiled.API.Features;

namespace SLWardrobe
{
    public class SLWardrobe : Plugin<Config>
    {
        public override string Name => "SLWardrobe";
        public override string Author => "ChochoZagorski";
        public override Version Version => new Version(1, 5, 0 );
        public override Version RequiredExiledVersion => new Version(9, 6, 1);
        
        public static SLWardrobe Instance { get; private set; }
        private Dictionary<Player, string> playerSuitNames = new Dictionary<Player, string>();
        private static readonly HttpClient HttpClient = new HttpClient();
        private const string VERSION_URL = "https://raw.githubusercontent.com/ChochoZagorski/SLWardrobe/master/version.txt";
        
        public override void OnEnabled()
        {
            Instance = this;
            Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
            Exiled.Events.Handlers.Player.Left += OnPlayerLeft;
            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;
            Exiled.Events.Handlers.Player.Died += OnPlayerDied;
            Task.Run(async () => await CheckForUpdates());
            base.OnEnabled();
            
        }
        
        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
            Exiled.Events.Handlers.Player.Left -= OnPlayerLeft;
            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;
            Exiled.Events.Handlers.Player.Died -= OnPlayerDied;
            Instance = null;
            base.OnDisabled();
        }
        
        private async Task CheckForUpdates()
        {
            try
            {
                HttpClient.DefaultRequestHeaders.Clear();
                HttpClient.DefaultRequestHeaders.Add("User-Agent", "SLWardrobe-VersionChecker");
        
                string latestVersion = await HttpClient.GetStringAsync(VERSION_URL);
                latestVersion = latestVersion.Trim();
        
                if (System.Version.TryParse(latestVersion, out var latest) && System.Version.TryParse(Version.ToString(), out var current))
                {
                    if (latest > current)
                    {
                        Log.Warn($"[SLWardrobe] A new version is available! Current: {Version} | Latest: {latestVersion}");
                        Log.Warn("[SLWardrobe] Download at: https://github.com/ChochoZagorski/SLWardrobe/releases/latest");
                    }
                    else if (latest < current)
                    {
                        Log.Info($"[SLWardrobe] There is a... Wait a minute, how do you have a future version? Anyways your version: {Version} | Latest: {latestVersion}");
                        Log.Info("[SLWardrobe] Seriously how?");
                    }
                    else
                    {
                        Log.Info($"[SLWardrobe] You are running the latest version ({Version})");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"[SLWardrobe] Could not check for updates: {ex.Message}");
            }
        }
        
        private void OnChangingRole(Exiled.Events.EventArgs.Player.ChangingRoleEventArgs ev)
        {
            if (ev.NewRole == RoleTypeId.None || ev.NewRole == RoleTypeId.Spectator)
            {
                SuitBinder.RemoveSuit(ev.Player);
            }
        }
        
        private void OnPlayerDied(Exiled.Events.EventArgs.Player.DiedEventArgs ev)
        {
            SuitBinder.RemoveSuit(ev.Player);
        }
        
        private void OnPlayerLeft(Exiled.Events.EventArgs.Player.LeftEventArgs ev)
        {
            SuitBinder.RemoveSuit(ev.Player);
            playerSuitNames.Remove(ev.Player);
        }
        
        private void OnRoundEnded(Exiled.Events.EventArgs.Server.RoundEndedEventArgs ev)
        {
            foreach (var player in Player.List)
            {
                SuitBinder.RemoveSuit(player);
            }
            playerSuitNames.Clear();
        }
        
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
            
            List<BoneBinding> bindings = null;

            if (Config.Suits.ContainsKey(suitName))
            {
                var suitConfig = Config.Suits[suitName];
                bindings = ConvertConfigToBindings(suitConfig);
            }
            else
            {
                Log.Warn($"Unknown suit: {suitName}. Please define it in the config.");
                yield break;
            }

            SuitBinder.ApplySuit(player, bindings);
            playerSuitNames[player] = suitName;

            yield return Timing.WaitForSeconds(1f);
            
            var suitData = SuitBinder.GetSuitData(player);
            if (suitData != null)
            {
                int activeCount = suitData.Parts.Count(p => p.GameObject != null);
            }
        }
        
        private List<BoneBinding> ConvertConfigToBindings(SuitConfig suitConfig)
        {
            var bindings = new List<BoneBinding>();
    
            foreach (var part in suitConfig.Parts)
            {
                var binding = new BoneBinding(
                    part.SchematicName,
                    part.BoneName,
                    new Vector3(part.PositionX, part.PositionY, part.PositionZ),
                    new Vector3(part.RotationX, part.RotationY, part.RotationZ),
                    new Vector3(part.ScaleX, part.ScaleY, part.ScaleZ)
                );
        
                binding.HideForWearer = part.HideForWearer;
        
                bindings.Add(binding);
            }
    
            return bindings;
        }
        public string GetPlayerSuitName(Player player)
        {
            return playerSuitNames.ContainsKey(player) ? playerSuitNames[player] : null;
        }
    }
}