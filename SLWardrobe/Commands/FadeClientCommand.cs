using System;
using CommandSystem;

#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
#endif

namespace SLWardrobe.Commands
{
    // Player-facing entry point - the SSSS button covers the same flow, but players
    // digging through server-settings menus is a discoverability dead end for something
    // this situational. `.fade` in the player console reaches the same handler.
    [CommandHandler(typeof(ClientCommandHandler))]
    public class FadeClientCommand : ICommand
    {
        public string Command => "fade";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Request that an admin hide your body to reduce suit misalignment for observers.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var handler = SLWardrobe.Instance?.FadeHandler;
            if (handler == null || !SLWardrobe.Instance.Config.FadeRequest.Enabled)
            {
                response = "Fade requests are not enabled on this server.";
                return false;
            }

            var player = Player.Get(sender);
            if (player == null) { response = "This command can only be used in-game."; return false; }

            handler.HandleRequest(player);
            response = "Request sent - check your hints for status.";
            return true;
        }
    }
}
