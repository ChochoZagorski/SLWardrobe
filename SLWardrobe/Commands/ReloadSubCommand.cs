using System;
using CommandSystem;
using SLWardrobe.Common;
#if EXILED
using Exiled.Permissions.Extensions;
#else
using LabApi.Features.Permissions;
#endif
using SLWardrobe.Weapons;

namespace SLWardrobe.Commands
{
    public class ReloadSubCommand : ICommand
    {
        public string Command => "reload";
        public string[] Aliases => new[] { "rl" };
        public string Description => "Reloads all suit and weapon configurations from disk.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
#if EXILED
            if (!sender.CheckPermission(PermissionNames.Admin))
#else
            if (!sender.HasPermissions(PermissionNames.Admin))
#endif
            { response = $"Missing permission: {PermissionNames.Admin}"; return false; }

            ConfigLoader.ReloadAll();
            WeaponDetector.Initialize();

            var config = SLWardrobe.Instance.Config;
            CosmeticTracker.SetSmoothing(config.CoreSmoothing, config.LimbSmoothing);

            response = $"Reloaded configurations.\n  Suits: {ConfigLoader.Suits.Count}\n  Weapons: {ConfigLoader.Weapons.Count}";
            return true;
        }
    }
}