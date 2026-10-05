using System.Collections.Generic;
using System.ComponentModel;
#if EXILED
using Exiled.API.Interfaces;
#endif

namespace SLWardrobe
{
#if EXILED
    public class Config : IConfig
#else
    public class Config
#endif
    {
#if EXILED
        [Description("Whether the plugin is enabled")]
        public bool IsEnabled { get; set; } = true;
#else
        public bool IsEnabled { get; set; } = true;
#endif

        [Description("Enable debug logging")]
        public bool Debug { get; set; } = false;

        [Description("Whether to check for plugin updates on startup")]
        public bool CheckForUpdates { get; set; } = true;

        [Description("How often (seconds) to poll held items for weapon detection. " +
                     "Lower = faster attach/detach response.")]
        public double UpdateInterval { get; set; } = 0.1;

        [Description("Maximum distance (meters) at which other players can see cosmetics. 0 = disabled (always visible)")]
        public double LodDistance { get; set; } = 0;

        [Description("How often (seconds) to recheck LOD visibility per viewer")]
        public double LodCheckInterval { get; set; } = 0.75;

        [Description("Network movement smoothing for core cosmetics (torso/head). 0-255; lower = smoother interpolation")]
        public byte CoreSmoothing { get; set; } = 60;

        [Description("Network movement smoothing for limb cosmetics (arms/legs). 255 = instant snap, best match for fast limb animation")]
        public byte LimbSmoothing { get; set; } = 255;

        [Description("Server Specific Settings System configuration")]
        public SsssConfig Ssss { get; set; } = new SsssConfig();

        [Description("Fade request system - allows players to request body-hide to improve suit appearance for observers")]
        public FadeRequestConfig FadeRequest { get; set; } = new FadeRequestConfig();
    }

    public class SsssConfig
    {
        [Description("Enable SSSS integration (shows settings in player's server settings menu)")]
        public bool Enabled { get; set; } = true;

        [Description("Allow players to toggle their own suit visibility")]
        public bool AllowSuitToggle { get; set; } = true;

        [Description("Allow players to control their personal cosmetic render distance")]
        public bool AllowLodControl { get; set; } = true;

        [Description("Minimum LOD distance players can set. Only matters if AllowLodControl is true")]
        public double MinLodDistance { get; set; } = 15;

        [Description("Maximum LOD distance players can set")]
        public double MaxLodDistance { get; set; } = 200;
    }

    public class FadeRequestConfig
    {
        [Description("Enable the fade request SSSS button and admin commands")]
        public bool Enabled { get; set; } = false;

        [Description("Permission required to approve/deny/revoke fade requests")]
        public string AdminPermission { get; set; } = "slwardrobe.admin";

        [Description("Seconds a player must wait between requests")]
        public int CooldownSeconds { get; set; } = 120;

        [Description("Maximum fade requests allowed per player per round. 0 = unlimited")]
        public int MaxRequestsPerRound { get; set; } = 2;

        [Description("Revoke fade if the player takes damage")]
        public bool RemoveOnTakeDamage { get; set; } = true;

        [Description("Revoke fade if the player deals damage")]
        public bool RemoveOnDealDamage { get; set; } = true;

        [Description("Item types that revoke fade when held. Empty = no item restriction")]
        public List<ItemType> HeldItemBlacklist { get; set; } = new List<ItemType>
        {
            ItemType.GunCOM15, ItemType.GunCOM18, ItemType.GunE11SR, ItemType.GunCrossvec,
            ItemType.GunFSP9, ItemType.GunLogicer, ItemType.GunRevolver, ItemType.GunShotgun,
            ItemType.GunAK, ItemType.GunCom45, ItemType.GunFRMG0, ItemType.GunA7,
            ItemType.MicroHID, ItemType.ParticleDisruptor, ItemType.Jailbird
        };

        [Description("Seconds between held-item checks for blacklist enforcement")]
        public float ItemCheckInterval { get; set; } = 0.5f;

        [Description("Broadcast admin notification when a request is made")]
        public bool NotifyAdminsOnRequest { get; set; } = true;

        [Description("Re-apply granted fade after the player respawns. False = grant is one-life only")]
        public bool PersistAcrossDeath { get; set; } = false;

        [Description("Seconds between admin reminders for unanswered fade requests. 0 = off")]
        public int AdminReminderIntervalSeconds { get; set; } = 0;
    }
}