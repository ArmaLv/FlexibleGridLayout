using UnityEngine;
using UnityEngine.UI;

namespace UnityUtilities.FlexibleGridLayout
{
    [AddComponentMenu("Layout/Flexible Grid Cell")]
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class FlexibleGridCell : MonoBehaviour
    {
        [Min(1)] public int columnSpan = 1;
        [Min(1)] public int rowSpan = 1;

        [Header("Fixed position (-1 = automatic)")]
        public int column = -1;
        public int row = -1;

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

        private void MarkParentDirty()
        {
            if (transform.parent != null)
            {
                LayoutRebuilder.MarkLayoutForRebuild(transform.parent as RectTransform);
            }
        }
    }
}
