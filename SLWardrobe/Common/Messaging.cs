#if EXILED
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
#else
using LabApi.Features.Wrappers;
using LabApi.Features.Permissions;
#endif

namespace SLWardrobe.Common
{
    public static class Messaging
    {
        public const string Prefix = "[SLWardrobe] ";
        private const float HintSeconds = 5f;

        public static void Hint(Player player, string message)
        {
#if EXILED
            player.ShowHint(Prefix + message, HintSeconds);
#else
            player.SendHint(Prefix + message, HintSeconds);
#endif
        }

        public static int PlayerId(Player player)
        {
#if EXILED
            return player.Id;
#else
            return player.PlayerId;
#endif
        }

        public static bool IsAdmin(Player player, string permission)
        {
#if EXILED
            return player.CheckPermission(permission);
#else
            return PermissionsManager.HasPermissions(player, new[] { permission });
#endif
        }
    }
}
