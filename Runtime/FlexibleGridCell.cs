using UnityEngine;
using UnityEngine.UI;

namespace UnityUtilities.FlexibleGridLayout
{
    // Add this component to a FlexibleGridLayoutGroup child to specify how many
    // cells it occupies and (optionally) a fixed position in the grid.
    // E.g.: columnSpan=2 -> 400x200, rowSpan=2 -> 200x400 (when the cell is 200x200).
    [AddComponentMenu("Layout/Flexible Grid Cell")]
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class FlexibleGridCell : MonoBehaviour
    {
        [Min(1)] public int columnSpan = 1; // How many columns it spans
        [Min(1)] public int rowSpan = 1;    // How many rows it spans

        [Header("Fixed position (-1 = automatic)")]
        public int column = -1; // Column from the left (0-based), -1 = auto
        public int row = -1;    // Row from the top (0-based), -1 = auto

        private void OnEnable()
        {
            MarkParentDirty();
        }

        private void OnDisable()
        {
            MarkParentDirty();
        }

        private void OnTransformParentChanged()
        {
            MarkParentDirty();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            columnSpan = Mathf.Max(1, columnSpan);
            rowSpan = Mathf.Max(1, rowSpan);
            MarkParentDirty();
        }
#endif

        // Tells the parent layout to recalculate when these values change
        private void MarkParentDirty()
        {
            if (transform.parent != null)
            {
                LayoutRebuilder.MarkLayoutForRebuild(transform.parent as RectTransform);
            }
        }
    }
}
