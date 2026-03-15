using System;
using CommandSystem;
#if EXILED
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
#else
using LabApi.Features.Wrappers;
using LabApi.Features.Permissions;
#endif

namespace SLWardrobe.Commands
{
    public class MergeSubCommand : ICommand
    {
        public string Command => "merge";
        public string[] Aliases => new[] { "combine" };
        public string Description => "Merges two suit or weapon configs into one. Usage: slw merge <source1> <source2> <outputName>";

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

            if (arguments.Count < 3)
            {
                response = "Usage: slw merge <source1> <source2> <outputName>\n" +
                           "Both sources must be the same type (both suits or both weapons).\n" +
                           "Names are file names without .yml extension.";
                return false;
            }

            string source1 = arguments.At(0);
            string source2 = arguments.At(1);
            string outputName = arguments.At(2);

            var player = Player.Get(sender);
            string mergerInfo = player != null
                ? $"{player.Nickname} ({player.UserId})"
                : "Server Console";

            var suit1 = ConfigLoader.GetSuit(source1);
            var suit2 = ConfigLoader.GetSuit(source2);
            var weapon1 = ConfigLoader.GetWeapon(source1);
            var weapon2 = ConfigLoader.GetWeapon(source2);

            bool isSuit1 = suit1 != null;
            bool isSuit2 = suit2 != null;
            bool isWeapon1 = weapon1 != null;
            bool isWeapon2 = weapon2 != null;

            if (!isSuit1 && !isWeapon1)
            {
                response = $"'{source1}' not found in loaded suits or weapons.\nMake sure configs are loaded (slw reload).";
                return false;
            }
            if (!isSuit2 && !isWeapon2)
            {
                response = $"'{source2}' not found in loaded suits or weapons.\nMake sure configs are loaded (slw reload).";
                return false;
            }

            if (isSuit1 && isWeapon2)
            {
                response = $"Type mismatch: '{source1}' is a suit but '{source2}' is a weapon.";
                return false;
            }
            if (isWeapon1 && isSuit2)
            {
                response = $"Type mismatch: '{source1}' is a weapon but '{source2}' is a suit.";
                return false;
            }

            if (isSuit1 && isSuit2)
            {
                if (suit1.WearerType != suit2.WearerType)
                {
                    response = $"Wearer type mismatch: '{source1}' is {suit1.WearerType} but '{source2}' is {suit2.WearerType}.";
                    return false;
                }

                if (ConfigLoader.MergeSuits(suit1, suit2, outputName, source1, source2, mergerInfo))
                {
                    int totalParts = suit1.Parts.Count + suit2.Parts.Count;
                    response = $"Merged suits '{source1}' + '{source2}' into '{outputName}'.\n" +
                               $"Combined {totalParts} parts ({suit1.Parts.Count} + {suit2.Parts.Count}).\n" +
                               $"File: {ConfigLoader.SuitsFolder}/{outputName}.yml\n" +
                               "Run 'slw reload' to load it.";
                    return true;
                }

                response = $"Failed to merge suits. '{outputName}.yml' may already exist.";
                return false;
            }

            if (isWeapon1 && isWeapon2)
            {
                if (ConfigLoader.MergeWeapons(weapon1, weapon2, outputName, source1, source2, mergerInfo))
                {
                    int totalParts = weapon1.Parts.Count + weapon2.Parts.Count;
                    response = $"Merged weapons '{source1}' + '{source2}' into '{outputName}'.\n" +
                               $"Combined {totalParts} parts ({weapon1.Parts.Count} + {weapon2.Parts.Count}).\n" +
                               $"File: {ConfigLoader.WeaponsFolder}/{outputName}.yml\n" +
                               "Run 'slw reload' to load it.";
                    return true;
                }

                response = $"Failed to merge weapons. '{outputName}.yml' may already exist.";
                return false;
            }

            response = "Could not determine file types. Make sure both sources are loaded.";
            return false;
        }
    }
}