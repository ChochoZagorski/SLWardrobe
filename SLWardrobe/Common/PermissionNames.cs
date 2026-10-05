namespace SLWardrobe.Common
{
    // Named PermissionNames, not Permissions - Exiled.Permissions.Extensions already
    // declares a Permissions class, and the two collide under `using`.
    public static class PermissionNames
    {
        public const string Use = "slwardrobe.use";
        public const string Admin = "slwardrobe.admin";
    }
}
