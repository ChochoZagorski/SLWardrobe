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
                     "Lower = faster attach/detach response. Does not affect suit smoothness.")]
        public double UpdateInterval { get; set; } = 0.1;

        [Description("Maximum distance (meters) at which other players can see cosmetics. 0 = disabled (always visible)")]
        public double LodDistance { get; set; } = 0;

        [Description("How often (seconds) to recheck LOD visibility per viewer")]
        public double LodCheckInterval { get; set; } = 0.75;

        [Description("Server Specific Settings System configuration")]
        public SsssConfig Ssss { get; set; } = new SsssConfig();
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
}