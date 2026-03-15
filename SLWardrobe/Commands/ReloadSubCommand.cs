using System;
using CommandSystem;
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
            if (!sender.CheckPermission("slwardrobe.admin"))
#else
            if (!sender.HasPermissions("slwardrobe.admin"))
#endif
            {
                response = "Missing permission: slwardrobe.admin";
                return false;
            }

            ConfigLoader.ReloadAll();
            WeaponBinder.Initialize();

            response = $"Reloaded configurations.\n" +
                       $"  Suits: {ConfigLoader.Suits.Count}\n" +
                       $"  Weapons: {ConfigLoader.Weapons.Count}";
            return true;
        }
    }
}