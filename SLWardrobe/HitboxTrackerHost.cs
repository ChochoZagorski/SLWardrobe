using UnityEngine;

namespace SLWardrobe
{
    /// <summary>
    /// Persistent MonoBehaviour that drives CosmeticTracker.LateUpdateTick() every frame.
    /// Attached to a DontDestroyOnLoad GameObject created in Main.PluginEnabled.
    /// Using a real MonoBehaviour.LateUpdate guarantees execution AFTER the Animator
    /// finalizes bone transforms - the MEC Segment.Update approach read stale frame N-1 data.
    /// </summary>
    public class HitboxTrackerHost : MonoBehaviour
    {
        public static HitboxTrackerHost Instance { get; private set; }

        private void Awake() => Instance = this;
        private void OnDestroy() => Instance = null;
        private void LateUpdate() => CosmeticTracker.LateUpdateTick();
    }
}
