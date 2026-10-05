using System;
using System.Collections.Generic;
using UnityEngine;
using AdminToys;

namespace SLWardrobe
{
    public static class CosmeticTracker
    {
        public struct TrackedRoot
        {
            public AdminToyBase RootToy;
            public Transform Bone;
            public Vector3 PositionOffset;
            public Quaternion RotationOffset;
            public bool IsLimb;                 // limb bones get snap smoothing; core bones interpolate
            public Vector3 LastPos;             // world-space, for idle-skip
            public Quaternion LastRot;          // world-space, for idle-skip
        }

        // 1 mm² / ~0.001° dot threshold - skip SyncVar writes when toy hasn't meaningfully moved
        private const float IDLE_POS_SQR_THRESHOLD = 0.000001f;
        private const float IDLE_ROT_DOT_THRESHOLD = 0.9999999f;

        private static readonly List<TrackedRoot> Entries = new List<TrackedRoot>();
        private static readonly HashSet<AdminToyBase> ManagedToys = new HashSet<AdminToyBase>();

        public static int TrackedCount => Entries.Count;
        public static int PrefixBlockCount;

        // Split smoothing: limbs snap (minimal interpolation lag -> tightest phase match for observers
        // watching the wearer's legs/arms swing); core interpolates smoothly (clean whole-body
        // translation). One global value can't serve both - high-frequency limb swing wants snap,
        // low-frequency translation wants smoothing. Configurable via Config.CoreSmoothing/LimbSmoothing.
        private static byte _coreSmoothing = 60;
        private static byte _limbSmoothing = 255;
        public static byte CoreSmoothing => _coreSmoothing;
        public static byte LimbSmoothing => _limbSmoothing;

        public static bool IsManagedToy(AdminToyBase atb) => ManagedToys.Contains(atb);

        public static void Register(AdminToyBase rootToy, Transform bone, Vector3 posOffset,
                                    Quaternion rotOffset, bool isLimb)
        {
            Entries.Add(new TrackedRoot
            {
                RootToy = rootToy,
                Bone = bone,
                PositionOffset = posOffset,
                RotationOffset = rotOffset,
                IsLimb = isLimb
            });
            ManagedToys.Add(rootToy);
        }

        public static void Unregister(AdminToyBase rootToy)
        {
            ManagedToys.Remove(rootToy);
            Entries.RemoveAll(e => e.RootToy == rootToy);
        }

        public static void UnregisterAll()
        {
            ManagedToys.Clear();
            Entries.Clear();
        }

        // Live-applies to all tracked toys immediately and seeds new spawns
        public static void SetSmoothing(byte core, byte limb)
        {
            _coreSmoothing = core;
            _limbSmoothing = limb;
            foreach (var e in Entries)
                if (e.RootToy != null) e.RootToy.NetworkMovementSmoothing = e.IsLimb ? limb : core;
        }

        /// <summary>
        /// Called from HitboxTrackerHost.LateUpdate() - after Animator finalizes bone transforms.
        ///
        /// Position/rotation always come fresh from the bone - never extrapolated from the bone
        /// itself, because bone-to-bone deltas are animation noise (idle sway, walk cycle), not
        /// body movement; extrapolating that noise caused a visible seizure. The controllable
        /// lever for observer-visible phase match is per-region smoothing (see SetSmoothing).
        /// </summary>
        public static void LateUpdateTick()
        {
            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                var e = Entries[i];

                if (e.RootToy == null || e.Bone == null)
                {
                    if (e.RootToy != null) ManagedToys.Remove(e.RootToy);
                    Entries.RemoveAt(i);
                    continue;
                }

                var pos = e.Bone.TransformPoint(e.PositionOffset);
                var rot = e.Bone.rotation * e.RotationOffset;

                bool posChanged = (pos - e.LastPos).sqrMagnitude > IDLE_POS_SQR_THRESHOLD;
                bool rotChanged = Mathf.Abs(Quaternion.Dot(rot, e.LastRot)) < IDLE_ROT_DOT_THRESHOLD;

                if (posChanged || rotChanged)
                {
                    e.RootToy.NetworkPosition = pos;
                    e.RootToy.NetworkRotation = rot;
                    e.LastPos = pos;
                    e.LastRot = rot;
                }

                Entries[i] = e;
            }
        }
    }
}
