using System;
using System.Collections.Generic;
using MEC;
using SLWardrobe.Common;
using SLWardrobe.Models;

#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
#endif

namespace SLWardrobe
{
    // Animator-phase drift between server and client is an unfixable ceiling (clients run faster than the server,
    // so animated bone phase diverges). Fading a wearer's real body removes the thing observers would see crisscross
    // against, leaving only the coherent suit. This class owns all request/grant state; SsssHandler
    // and FadeSubCommand only forward player/admin actions here.
    public class FadeRequestHandler
    {
        // A pending request older than this gets renagged by the reminder loop.
        private const int STALE_REQUEST_SECONDS = 60;

        private readonly HashSet<Player> _fadeGranted = new HashSet<Player>();
        private readonly Dictionary<Player, DateTime> _pendingRequests = new Dictionary<Player, DateTime>();
        private readonly Dictionary<Player, DateTime> _lastRequestTime = new Dictionary<Player, DateTime>();
        private readonly Dictionary<Player, int> _requestCounts = new Dictionary<Player, int>();
        private readonly List<Player> _scratchBuffer = new List<Player>();

        private HashSet<ItemType> _blacklist;
        private CoroutineHandle _itemCheckCoroutine;
        private CoroutineHandle _reminderCoroutine;
        private bool _itemCheckRunning;
        private bool _reminderRunning;

        public IReadOnlyDictionary<Player, DateTime> PendingRequests => _pendingRequests;

        public void Register()
        {
            var config = SLWardrobe.Instance.Config.FadeRequest;
            _blacklist = new HashSet<ItemType>(config.HeldItemBlacklist);

            if (config.RemoveOnTakeDamage || config.RemoveOnDealDamage)
            {
#if EXILED
                Exiled.Events.Handlers.Player.Hurting += OnHurting;
#else
                LabApi.Events.Handlers.PlayerEvents.Hurting += OnHurting;
#endif
            }
        }

        public void Unregister()
        {
#if EXILED
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;
#else
            LabApi.Events.Handlers.PlayerEvents.Hurting -= OnHurting;
#endif
            StopCoroutines();

            foreach (var player in _fadeGranted)
                CosmeticBinder.SetPlayerInvisibility(player, false);

            _fadeGranted.Clear();
            _pendingRequests.Clear();
            _lastRequestTime.Clear();
            _requestCounts.Clear();
        }

        #region Request flow

        public void HandleRequest(Player player)
        {
            var config = SLWardrobe.Instance.Config.FadeRequest;

            string suitName = SLWardrobe.Instance.GetPlayerSuitName(player);
            var suit = suitName != null ? ConfigLoader.GetSuit(suitName) : null;
            if (suit == null)
            {
                Messaging.Hint(player, "You are not wearing a suit.");
                return;
            }
            if (suit.MakeWearerInvisible || !suit.AllowFadeRequest)
            {
                Messaging.Hint(player, "This suit does not support fade requests.");
                return;
            }

            if (_fadeGranted.Contains(player))
            {
                Messaging.Hint(player, "Your body is already hidden.");
                return;
            }
            if (_pendingRequests.ContainsKey(player))
            {
                Messaging.Hint(player, "Your fade request is already pending.");
                return;
            }

            if (_lastRequestTime.TryGetValue(player, out var last))
            {
                double remaining = config.CooldownSeconds - (DateTime.UtcNow - last).TotalSeconds;
                if (remaining > 0)
                {
                    Messaging.Hint(player, $"Request cooldown: {(int)Math.Ceiling(remaining)}s remaining.");
                    return;
                }
            }

            if (config.MaxRequestsPerRound > 0
                && _requestCounts.TryGetValue(player, out var count)
                && count >= config.MaxRequestsPerRound)
            {
                Messaging.Hint(player, "Maximum requests for this round reached.");
                return;
            }

            _pendingRequests[player] = DateTime.UtcNow;
            _lastRequestTime[player] = DateTime.UtcNow;
            _requestCounts[player] = _requestCounts.TryGetValue(player, out var prev) ? prev + 1 : 1;

            Messaging.Hint(player, "Fade request sent. Awaiting admin approval.");

            if (config.NotifyAdminsOnRequest)
                NotifyAdmins($"{player.Nickname} wants their body faded - type: slw fade yes");

            EnsureReminderRunning();
        }

        private void NotifyAdmins(string message)
        {
            string permission = SLWardrobe.Instance.Config.FadeRequest.AdminPermission;
            foreach (var admin in Player.List)
            {
                if (admin == null) continue;
                if (Messaging.IsAdmin(admin, permission)) Messaging.Hint(admin, message);
            }
        }

        #endregion

        #region Grant / Deny / Revoke

        public bool Grant(Player player, bool requireRequest, out string error)
        {
            if (requireRequest && !_pendingRequests.ContainsKey(player))
            {
                error = "No pending fade request for this player.";
                return false;
            }
            if (!player.IsAlive || CosmeticBinder.GetSuitData(player) == null)
            {
                error = "Player is not currently wearing a suit.";
                return false;
            }

            _pendingRequests.Remove(player);
            _fadeGranted.Add(player);
            CosmeticBinder.SetPlayerInvisibility(player, true);
            Messaging.Hint(player, "Your body fade request was approved.");
            EnsureItemCheckRunning();

            error = null;
            return true;
        }

        public bool Deny(Player player, out string error)
        {
            if (!_pendingRequests.Remove(player))
            {
                error = "No pending fade request for this player.";
                return false;
            }
            Messaging.Hint(player, "Your body fade request was denied.");
            error = null;
            return true;
        }

        public bool Revoke(Player player, string reason)
        {
            if (!_fadeGranted.Remove(player)) return false;
            CosmeticBinder.SetPlayerInvisibility(player, false);
            Messaging.Hint(player, $"Body fade removed: {reason}");
            return true;
        }

        public int RevokeAll(string reason)
        {
            _scratchBuffer.Clear();
            _scratchBuffer.AddRange(_fadeGranted);
            foreach (var player in _scratchBuffer)
                Revoke(player, reason);
            return _scratchBuffer.Count;
        }

        public bool IsGranted(Player player) => _fadeGranted.Contains(player);

        // Backs the optional-target default for `slw fade yes/no` - the overwhelmingly common
        // case is exactly one pending request, so admins shouldn't have to type a name for it.
        public bool TryGetSolePending(out Player player, out string error)
        {
            if (_pendingRequests.Count == 0)
            {
                player = null;
                error = "No pending fade requests.";
                return false;
            }
            if (_pendingRequests.Count > 1)
            {
                player = null;
                var names = new List<string>(_pendingRequests.Count);
                foreach (var p in _pendingRequests.Keys) names.Add(p.Nickname);
                error = $"Multiple pending requests - specify a target: {string.Join(", ", names)}";
                return false;
            }

            foreach (var p in _pendingRequests.Keys) { player = p; error = null; return true; }
            player = null;
            error = "No pending fade requests.";
            return false;
        }

        #endregion

        #region Auto-revocation

#if EXILED
        private void OnHurting(Exiled.Events.EventArgs.Player.HurtingEventArgs ev)
        {
            var config = SLWardrobe.Instance.Config.FadeRequest;
            var victim = ev.Player;
            var attacker = ev.Attacker;

            if (config.RemoveOnTakeDamage && victim != null && _fadeGranted.Contains(victim))
                Revoke(victim, "You took damage.");
            if (config.RemoveOnDealDamage && attacker != null && attacker != victim && _fadeGranted.Contains(attacker))
                Revoke(attacker, "You dealt damage.");
        }
#else
        private void OnHurting(LabApi.Events.Arguments.PlayerEvents.PlayerHurtingEventArgs ev)
        {
            var config = SLWardrobe.Instance.Config.FadeRequest;
            var victim = ev.Player;
            var attacker = ev.Attacker;

            if (config.RemoveOnTakeDamage && victim != null && _fadeGranted.Contains(victim))
                Revoke(victim, "You took damage.");
            if (config.RemoveOnDealDamage && attacker != null && attacker != victim && _fadeGranted.Contains(attacker))
                Revoke(attacker, "You dealt damage.");
        }
#endif

        private void EnsureItemCheckRunning()
        {
            if (_itemCheckRunning || _blacklist.Count == 0) return;
            _itemCheckCoroutine = Timing.RunCoroutine(ItemCheckLoop());
            _itemCheckRunning = true;
        }

        private IEnumerator<float> ItemCheckLoop()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(SLWardrobe.Instance.Config.FadeRequest.ItemCheckInterval);

                if (_fadeGranted.Count == 0)
                {
                    _itemCheckRunning = false;
                    yield break;
                }

                _scratchBuffer.Clear();
                _scratchBuffer.AddRange(_fadeGranted);

                for (int i = 0; i < _scratchBuffer.Count; i++)
                {
                    var player = _scratchBuffer[i];
                    if (player == null || !player.IsAlive) continue;
                    var itemType = player.CurrentItem?.Type ?? ItemType.None;
                    if (_blacklist.Contains(itemType))
                        Revoke(player, "You equipped a restricted item.");
                }
            }
        }

        #endregion

        #region Reminder

        private void EnsureReminderRunning()
        {
            int interval = SLWardrobe.Instance.Config.FadeRequest.AdminReminderIntervalSeconds;
            if (_reminderRunning || interval <= 0) return;
            _reminderCoroutine = Timing.RunCoroutine(ReminderLoop(interval));
            _reminderRunning = true;
        }

        private IEnumerator<float> ReminderLoop(int interval)
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(interval);

                if (_pendingRequests.Count == 0)
                {
                    _reminderRunning = false;
                    yield break;
                }

                foreach (var kvp in _pendingRequests)
                {
                    double age = (DateTime.UtcNow - kvp.Value).TotalSeconds;
                    if (age < STALE_REQUEST_SECONDS) continue;
                    NotifyAdmins($"Reminder: {kvp.Key.Nickname} has an unanswered fade request ({(int)age}s ago). Type: slw fade yes");
                }
            }
        }

        #endregion

        #region Lifecycle

        public void ResetRoundState()
        {
            _lastRequestTime.Clear();
            _requestCounts.Clear();
            _pendingRequests.Clear();
        }

        public void CleanupPlayer(Player player)
        {
            _fadeGranted.Remove(player);
            _pendingRequests.Remove(player);
            _lastRequestTime.Remove(player);
            _requestCounts.Remove(player);
        }

        // Called on death/role change. The Fade effect is already stripped by the caller
        // (Main.CleanupPlayer -> CosmeticBinder.SetPlayerInvisibility) - this keeps bookkeeping
        // consistent so a stale entry doesn't block/duplicate future grants.
        public void OnPlayerReset(Player player)
        {
            _pendingRequests.Remove(player);
            if (!SLWardrobe.Instance.Config.FadeRequest.PersistAcrossDeath)
                _fadeGranted.Remove(player);
        }

        public void ReapplyIfGranted(Player player)
        {
            if (!SLWardrobe.Instance.Config.FadeRequest.PersistAcrossDeath) return;
            if (_fadeGranted.Contains(player))
                CosmeticBinder.SetPlayerInvisibility(player, true);
        }

        private void StopCoroutines()
        {
            if (_itemCheckRunning) { Timing.KillCoroutines(_itemCheckCoroutine); _itemCheckRunning = false; }
            if (_reminderRunning) { Timing.KillCoroutines(_reminderCoroutine); _reminderRunning = false; }
        }

        #endregion
    }
}
