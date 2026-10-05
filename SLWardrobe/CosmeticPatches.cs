using HarmonyLib;
using AdminToys;

namespace SLWardrobe
{
    [HarmonyPatch(typeof(AdminToyBase), nameof(AdminToyBase.LateUpdate))]
    public static class AdminToyLateUpdatePatch
    {
        /// <summary>
        /// Skips vanilla LateUpdate for AdminToyBases managed by CosmeticTracker.
        /// Without this, AdminToyBase.LateUpdate and our tracker race each other
        /// in the LateUpdate phase - sometimes vanilla reads stale positions and
        /// writes them to SyncVars before our tracker updates them, causing the
        /// client to oscillate between current and stale positions (crisscross).
        /// </summary>
        public static bool Prefix(AdminToyBase __instance)
        {
            if (CosmeticTracker.IsManagedToy(__instance))
            {
                CosmeticTracker.PrefixBlockCount++;
                return false;
            }
            return true;
        }
    }
}