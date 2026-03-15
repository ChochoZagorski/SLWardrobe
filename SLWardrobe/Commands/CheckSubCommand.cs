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
    public class CheckSubCommand : ICommand
    {
        public string Command => "check";
        public string[] Aliases => new[] { "cs", "info" };
        public string Description => "Checks a player's suit status.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
#if EXILED
            if (!sender.CheckPermission("slwardrobe.use"))
#else
            if (!sender.HasPermissions("slwardrobe.use"))
#endif
            {
                response = "Missing permission: slwardrobe.use";
                return false;
            }

            if (arguments.Count < 1)
            {
                response = "Usage: slw check <player>";
                return false;
            }

#if EXILED
            var target = Player.Get(arguments.At(0));
            if (target == null)
            {
                response = $"Player '{arguments.At(0)}' not found.";
                return false;
            }

            string suitName = SLWardrobe.Instance.GetPlayerSuitName(target);
            var suitData = SuitBinder.GetSuitData(target);

            if (string.IsNullOrEmpty(suitName))
            {
                response = $"{target.Nickname} has no suit equipped.";
            }
            else
            {
                int activeParts = 0;
                if (suitData != null)
                {
                    foreach (var part in suitData.Parts)
                    {
                        if (part.GameObject != null) activeParts++;
                    }
                }

                response = $"{target.Nickname} is wearing: {suitName} ({activeParts} active parts)";
            }
#else
            if (!int.TryParse(arguments.At(0), out int playerId))
            {
                response = "Invalid player ID. ID must be an integer.";
                return false;
            }

            var target = Player.Get(playerId);
            if (target == null)
            {
                response = $"Player with ID '{playerId}' not found.";
                return false;
            }

            string suitName = SLWardrobe.Instance.GetPlayerSuitName(target);
            var suitData = SuitBinder.GetSuitData(target);

            if (string.IsNullOrEmpty(suitName))
            {
                response = $"{target.Nickname} has no suit equipped.";
            }
            else
            {
                int activeParts = 0;
                if (suitData != null)
                {
                    foreach (var part in suitData.Parts)
                    {
                        if (part.GameObject != null) activeParts++;
                    }
                }

                response = $"{target.Nickname} is wearing: {suitName} ({activeParts} active parts)";
            }
#endif
            return true;
        }
    }
}