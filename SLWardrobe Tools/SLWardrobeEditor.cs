using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SLWardrobe.EditorTools
{
    public class SLWardrobeEditor : MonoBehaviour
    {
        public const string Version = "1.0.0";

        [Header("SLWardrobe Previewer v" + Version)]
        public string suitName = "my_suit";
        public string wearerType = "Human";
        public bool makeWearerInvisible = false;

        [Header("Bone Assignments")]
        public List<BoneSlot> slots = new List<BoneSlot>();

        [Header("Export Output")]
        [TextArea(8, 30)]
        public string lastExport = "";

        [ContextMenu("1 - Attach Schematics to Bones")]
        public void AttachAll()
        {
            int count = 0;

            foreach (var slot in slots)
            {
                if (slot.bone == null || slot.schematic == null)
                    continue;

                if (slot.schematic.transform.parent == slot.bone)
                    continue;

                slot.schematic.transform.SetParent(slot.bone, true);
                count++;
            }

            Debug.Log("[SLWardrobe Previewer] Attached " + count + " schematic(s) to bones.");
        }

        [ContextMenu("2 - Detach Schematics from Bones")]
        public void DetachAll()
        {
            int count = 0;

            foreach (var slot in slots)
            {
                if (slot.schematic == null || slot.schematic.transform.parent == null)
                    continue;

                slot.schematic.transform.SetParent(null, true);
                count++;
            }

            Debug.Log("[SLWardrobe Previewer] Detached " + count + " schematic(s).");
        }

        [ContextMenu("3 - Export Offsets to YAML")]
        public void ExportOffsets()
        {
            var lines = new List<string>();

            lines.Add("name: " + suitName);
            lines.Add("description: \"Exported from SLWardrobe Previewer v" + Version + "\"");
            lines.Add("make_wearer_invisible: " + makeWearerInvisible.ToString().ToLower());
            lines.Add("wearer_type: " + wearerType);
            lines.Add("parts:");

            int exported = 0;

            foreach (var slot in slots)
            {
                if (slot.bone == null || slot.schematic == null)
                    continue;

                Vector3 localPos = slot.bone.InverseTransformPoint(slot.schematic.transform.position);
                Quaternion localRot = Quaternion.Inverse(slot.bone.rotation) * slot.schematic.transform.rotation;
                Vector3 euler = localRot.eulerAngles;
                Vector3 scale = slot.schematic.transform.lossyScale;

                string name = string.IsNullOrEmpty(slot.schematicName)
                    ? slot.schematic.name
                    : slot.schematicName;

                lines.Add("- schematic_name: " + name);
                lines.Add("  bone_name: " + slot.boneName);
                lines.Add("  position_x: " + F(localPos.x));
                lines.Add("  position_y: " + F(localPos.y));
                lines.Add("  position_z: " + F(localPos.z));
                lines.Add("  rotation_x: " + F(euler.x));
                lines.Add("  rotation_y: " + F(euler.y));
                lines.Add("  rotation_z: " + F(euler.z));
                lines.Add("  scale_x: " + F(scale.x));
                lines.Add("  scale_y: " + F(scale.y));
                lines.Add("  scale_z: " + F(scale.z));
                lines.Add("  hide_for_wearer: " + slot.hideForWearer.ToString().ToLower());
                lines.Add("  static: " + slot.isStatic.ToString().ToLower());

                exported++;
            }

            lastExport = string.Join("\n", lines);
            GUIUtility.systemCopyBuffer = lastExport;

            Debug.Log("[SLWardrobe Previewer] Exported " + exported + " part(s). YAML copied to clipboard.\n" + lastExport);
        }

        private static string F(float value)
        {
            if (Mathf.Abs(value) < 0.0001f)
                return "0";

            return Math.Round(value, 4).ToString(CultureInfo.InvariantCulture);
        }
    }

    [Serializable]
    public class BoneSlot
    {
        [Tooltip("Friendly bone name for the YAML config (e.g. head, chest, leftarm)")]
        public string boneName = "body";

        [Tooltip("Drag the bone/hitbox Transform from the model hierarchy")]
        public Transform bone;

        [Tooltip("Override schematic name for YAML. Leave empty to use the GameObject name.")]
        public string schematicName = "";

        [Tooltip("Drag your schematic GameObject here")]
        public GameObject schematic;

        [Tooltip("Hide this part from the wearer's own view")]
        public bool hideForWearer = false;

        [Tooltip("Disables this part Rotation update, it will only follow the player via Parenting")]
        public bool isStatic = false;
    }
}