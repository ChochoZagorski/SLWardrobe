using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.API.Enums;
using MEC;
using UnityEngine;
using PlayerRoles;

namespace SLWardrobe
{
    public class SLWardrobe : Plugin<Config>
    {
        public override string Name => "SLWardrobe";
        public override string Author => "ChochoZagorski";
        public override Version Version => new Version(1, 1, 0);
        public override Version RequiredExiledVersion => new Version(9, 6, 1);
        
        public static SLWardrobe Instance { get; private set; }
        private Dictionary<Player, string> playerSuitNames = new Dictionary<Player, string>();
        
        public override void OnEnabled()
        {
            Instance = this;
            Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
            Exiled.Events.Handlers.Player.Left += OnPlayerLeft;
            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;
            Exiled.Events.Handlers.Player.Died += OnPlayerDied;
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
        
        private void OnChangingRole(Exiled.Events.EventArgs.Player.ChangingRoleEventArgs ev)
        {
            if (ev.NewRole == RoleTypeId.None || ev.NewRole == RoleTypeId.Spectator)
            {
                SuitBinder.RemoveSuit(ev.Player);
            }
            else if (playerSuitNames.ContainsKey(ev.Player))
            {
                // Reapply suit after role change
                string suitName = playerSuitNames[ev.Player];
                Timing.CallDelayed(1f, () => ApplySuit(ev.Player, suitName));
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
            
            // Apply suit with a small delay to ensure player is fully spawned
            Timing.RunCoroutine(ApplySuitCoroutine(player, suitName));
        }
        
        private IEnumerator<float> ApplySuitCoroutine(Player player, string suitName)
        {
            // Wait a moment for the player to be fully initialized
            yield return Timing.WaitForSeconds(0.5f);
            
            if (player == null || !player.IsAlive) yield break;
            
            List<BoneBinding> bindings = null;
            
            // Check config suits
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
            
            // Apply the suit using the new manual tracking system
            SuitBinder.ApplySuit(player, bindings);
            playerSuitNames[player] = suitName;
            
            // Verify after a delay
            yield return Timing.WaitForSeconds(1f);
            
            var suitData = SuitBinder.GetSuitData(player);
            if (suitData != null)
            {
                int activeCount = suitData.Parts.Count(p => p.GameObject != null);
                Log.Info($"Suit verification for {player.Nickname}: {activeCount}/{suitData.Parts.Count} parts active");
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
                
                bindings.Add(binding);
            }
            
            return bindings;
        }
    }
}