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
    public class CreateSubCommand : ICommand
    {
        public string Command => "create";
        public string[] Aliases => new[] { "cr", "new" };
        public string Description => "Creates a suit or weapon template. Usage: slw create <suit|weapon> <name> [type]";

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

            if (arguments.Count < 2)
            {
                response = "Usage: slw create <suit|weapon> <name> [type]\n" +
                           "  Suit types: " + string.Join(", ", BoneMappings.GetWearerTypes()) + "\n" +
                           "  Weapon item types: Flashlight, GunCOM15, Medkit, KeycardO5, etc.";
                return false;
            }

            string kind = arguments.At(0).ToLower();
            string name = arguments.At(1);

            var player = Player.Get(sender);
            string creatorInfo = player != null
                ? $"{player.Nickname} ({player.UserId})"
                : "Server Console";

            switch (kind)
            {
                case "suit":
                {
                    string wearerType = arguments.Count > 2 ? arguments.At(2) : "Human";

                    if (ConfigLoader.CreateSuitTemplate(name, wearerType, creatorInfo))
                    {
                        response = $"Created suit template '{name}' ({wearerType}).\n" +
                                   $"File: {ConfigLoader.SuitsFolder}/{name}.yml\n" +
                                   "Edit the file then run 'slw reload'.";
                        return true;
                    }

                    response = $"Failed to create suit template. '{name}.yml' may already exist.";
                    return false;
                }

                case "weapon":
                {
                    string itemType = arguments.Count > 2 ? arguments.At(2) : "Flashlight";

                    if (ConfigLoader.CreateWeaponTemplate(name, itemType, creatorInfo))
                    {
                        response = $"Created weapon template '{name}' ({itemType}).\n" +
                                   $"File: {ConfigLoader.WeaponsFolder}/{name}.yml\n" +
                                   "Edit the file then run 'slw reload'.";
                        return true;
                    }

                    response = $"Failed to create weapon template. '{name}.yml' may already exist.";
                    return false;
                }

                default:
                    response = $"Unknown type '{kind}'. Use 'suit' or 'weapon'.";
                    return false;
            }
        }
    }
}