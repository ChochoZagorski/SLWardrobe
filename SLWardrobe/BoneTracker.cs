using AdminToys;
using UnityEngine;

namespace SLWardrobe
{
    /// Drives a cosmetic part's transform from its assigned bone every frame.
    /// Writes localPosition/localRotation so AdminToyBase.LateUpdate reads correct SyncVar values.
    /// This is my attempt of having 1:1 sync, but again bugs exists. You have instant position tracking with animation fall backs... TODO: Fix me
    public class BoneTracker : MonoBehaviour
    {
        public Transform Bone;
        public Vector3 LocalOffset;
        public Quaternion RotationOffset;
        public Vector3 FixedScale;
        public bool LockScale = true;

        private void Update()
        {
            if (Bone == null || transform.parent == null)
                return;

            var worldPos = Bone.TransformPoint(LocalOffset);
            var worldRot = Bone.rotation * RotationOffset;

            transform.localPosition = transform.parent.InverseTransformPoint(worldPos);
            transform.localRotation = Quaternion.Inverse(transform.parent.rotation) * worldRot;
            
            if (LockScale)
            {
                var parentLossy = transform.parent.lossyScale;
                transform.localScale = new Vector3(
                    FixedScale.x / parentLossy.x,
                    FixedScale.y / parentLossy.y,
                    FixedScale.z / parentLossy.z
                );
            }
        }
    }
}