using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityUtilities.FlexibleGridLayout
{
    // A smarter grid than the stock GridLayoutGroup.
    // Elements can span multiple columns/rows (span), and the rest flow around the
    // big ones on their own - just like CSS grid auto-placement.
    // The base cell is uniform (cellSize), but an element with FlexibleGridCell can be,
    // for example, 2x1 (400x200) or 1x2 (200x400).
    [AddComponentMenu("Layout/Flexible Grid Layout Group")]
    public class FlexibleGridLayoutGroup : LayoutGroup
    {
        // Which corner to start the layout from
        public enum Corner { UpperLeft = 0, UpperRight = 1, LowerLeft = 2, LowerRight = 3 }

        [SerializeField] protected Corner m_StartCorner = Corner.UpperLeft;
        public Corner startCorner { get { return m_StartCorner; } set { SetProperty(ref m_StartCorner, value); } }

        [SerializeField] protected Vector2 m_CellSize = new Vector2(100, 100);
        // Size of one (1x1) cell
        public Vector2 cellSize { get { return m_CellSize; } set { SetProperty(ref m_CellSize, value); } }

        [SerializeField] protected Vector2 m_Spacing = Vector2.zero;
        // Spacing between cells on both axes
        public Vector2 spacing { get { return m_Spacing; } set { SetProperty(ref m_Spacing, value); } }

        [SerializeField] protected int m_Columns = 4;
        // Number of columns (fixed). Rows grow automatically with the content.
        public int columns { get { return m_Columns; } set { SetProperty(ref m_Columns, Mathf.Max(1, value)); } }

        [SerializeField] protected bool m_FillGaps = true;
        // Whether to slot smaller elements into free gaps (dense packing).
        // If false - they flow sequentially and leave holes behind large elements.
        public bool fillGaps { get { return m_FillGaps; } set { SetProperty(ref m_FillGaps, value); } }

        // Where the whole block of cells sits inside the container (independent of the fill order).
        // Use these for centering or edge alignment, e.g. a centered row of pips.
        public enum HorizontalAlign { Left = 0, Center = 1, Right = 2 }
        public enum VerticalAlign { Top = 0, Middle = 1, Bottom = 2 }

        [SerializeField] protected HorizontalAlign m_HorizontalAlign = HorizontalAlign.Left;
        // Left / Center / Right placement of the content block
        public HorizontalAlign horizontalAlign { get { return m_HorizontalAlign; } set { SetProperty(ref m_HorizontalAlign, value); } }

        [SerializeField] protected VerticalAlign m_VerticalAlign = VerticalAlign.Top;
        // Top / Middle / Bottom placement of the content block
        public VerticalAlign verticalAlign { get { return m_VerticalAlign; } set { SetProperty(ref m_VerticalAlign, value); } }

        // Placement of a single element in the grid
        private struct Cell { public int col, row, wSpan, hSpan; }
        private readonly List<Cell> m_Cells = new List<Cell>();
        private int m_Rows = 1;

        protected FlexibleGridLayoutGroup() { }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            m_Columns = Mathf.Max(1, m_Columns);
        }
#endif

        public override void CalculateLayoutInputHorizontal()
        {
            // Refresh the rectChildren list (base method)
            base.CalculateLayoutInputHorizontal();

            // Compute the placement once - it does not depend on width
            ComputePlacement();

            float totalWidth = padding.horizontal + m_Columns * cellSize.x + (m_Columns - 1) * spacing.x;
            SetLayoutInputForAxis(totalWidth, totalWidth, -1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            float totalHeight = padding.vertical + m_Rows * cellSize.y + (m_Rows - 1) * spacing.y;
            SetLayoutInputForAxis(totalHeight, totalHeight, -1, 1);
        }

        public override void SetLayoutHorizontal()
        {
            SetCellsAlongAxis(0);
        }

        public override void SetLayoutVertical()
        {
            SetCellsAlongAxis(1);
        }

        // Lays the children out on the grid, honoring spans and any fixed positions
        private void ComputePlacement()
        {
            m_Cells.Clear();
            int cols = Mathf.Max(1, m_Columns);

            // Occupancy map: each row is a bool[cols]. Rows are added as needed.
            List<bool[]> grid = new List<bool[]>();
            int cursorCol = 0, cursorRow = 0;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                FlexibleGridCell meta = rectChildren[i].GetComponent<FlexibleGridCell>();
                int w = 1, h = 1, fixedCol = -1, fixedRow = -1;
                if (meta != null)
                {
                    w = Mathf.Clamp(meta.columnSpan, 1, cols);
                    h = Mathf.Max(1, meta.rowSpan);
                    fixedCol = meta.column;
                    fixedRow = meta.row;
                }

                int placeCol, placeRow;
                if (fixedCol >= 0 && fixedRow >= 0)
                {
                    // User-specified fixed position
                    placeCol = Mathf.Clamp(fixedCol, 0, cols - w);
                    placeRow = Mathf.Max(0, fixedRow);
                }
                else if (m_FillGaps)
                {
                    // Find the first free gap from the very start
                    FindSlot(grid, cols, w, h, 0, 0, out placeCol, out placeRow);
                }
                else
                {
                    // Flow from the current cursor
                    FindSlot(grid, cols, w, h, cursorRow, cursorCol, out placeCol, out placeRow);
                    cursorRow = placeRow;
                    cursorCol = placeCol + w;
                    if (cursorCol >= cols)
                    {
                        cursorCol = 0;
                        cursorRow = placeRow + 1;
                    }
                }

                MarkSlot(grid, cols, placeCol, placeRow, w, h);
                m_Cells.Add(new Cell { col = placeCol, row = placeRow, wSpan = w, hSpan = h });
            }

            m_Rows = Mathf.Max(1, grid.Count);
        }

        // Finds the first position where an element (w x h) fits in free space
        private void FindSlot(List<bool[]> grid, int cols, int w, int h, int startRow, int startCol, out int col, out int row)
        {
            int r = Mathf.Max(0, startRow);
            while (true)
            {
                int cStart = (r == startRow) ? startCol : 0;
                for (int c = cStart; c <= cols - w; c++)
                {
                    if (SlotFree(grid, c, r, w, h))
                    {
                        col = c;
                        row = r;
                        return;
                    }
                }
                r++;
            }
        }

        // Whether the area from (col,row) to (col+w, row+h) is free
        private bool SlotFree(List<bool[]> grid, int col, int row, int w, int h)
        {
            for (int r = row; r < row + h; r++)
            {
                if (r >= grid.Count)
                {
                    continue; // rows that don't exist yet count as free
                }
                for (int c = col; c < col + w; c++)
                {
                    if (grid[r][c])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // Marks an area as occupied, adding rows when necessary
        private void MarkSlot(List<bool[]> grid, int cols, int col, int row, int w, int h)
        {
            while (grid.Count < row + h)
            {
                grid.Add(new bool[cols]);
            }
            for (int r = row; r < row + h; r++)
            {
                for (int c = col; c < col + w; c++)
                {
                    grid[r][c] = true;
                }
            }
        }

        private void SetCellsAlongAxis(int axis)
        {
            int count = rectChildren.Count;

            // In the horizontal pass only set the anchors (same as GridLayoutGroup)
            if (axis == 0)
            {
                for (int i = 0; i < count; i++)
                {
                    RectTransform rect = rectChildren[i];
                    m_Tracker.Add(this, rect,
                        DrivenTransformProperties.Anchors |
                        DrivenTransformProperties.AnchoredPosition |
                        DrivenTransformProperties.SizeDelta);
                    rect.anchorMin = Vector2.up;
                    rect.anchorMax = Vector2.up;
                }
                return;
            }

            int cols = Mathf.Max(1, m_Columns);
            int rows = m_Rows;

            float requiredWidth = cols * cellSize.x + (cols - 1) * spacing.x;
            float requiredHeight = rows * cellSize.y + (rows - 1) * spacing.y;
            Vector2 startOffset = new Vector2(
                AlignedOffset(0, requiredWidth, AlignFactor(m_HorizontalAlign)),
                AlignedOffset(1, requiredHeight, AlignFactor(m_VerticalAlign))
            );

            int cornerX = (int)m_StartCorner % 2; // 1 = right side
            int cornerY = (int)m_StartCorner / 2; // 1 = bottom

            for (int i = 0; i < count && i < m_Cells.Count; i++)
            {
                Cell cell = m_Cells[i];

                int px = cell.col;
                int py = cell.row;
                if (cornerX == 1) px = cols - cell.wSpan - cell.col;
                if (cornerY == 1) py = rows - cell.hSpan - cell.row;

                // Element size based on span (including the spacing between occupied cells)
                float w = cell.wSpan * cellSize.x + (cell.wSpan - 1) * spacing.x;
                float h = cell.hSpan * cellSize.y + (cell.hSpan - 1) * spacing.y;

                float xpos = startOffset.x + (cellSize.x + spacing.x) * px;
                float ypos = startOffset.y + (cellSize.y + spacing.y) * py;

                SetChildAlongAxis(rectChildren[i], 0, xpos, w);
                SetChildAlongAxis(rectChildren[i], 1, ypos, h);
            }
        }

        // Aligns the whole content block inside the container.
        // factor: 0 = left/top, 0.5 = center/middle, 1 = right/bottom.
        private float AlignedOffset(int axis, float requiredSize, float factor)
        {
            if (axis == 0)
            {
                float available = rectTransform.rect.size.x - padding.horizontal;
                return padding.left + (available - requiredSize) * factor;
            }
            else
            {
                float available = rectTransform.rect.size.y - padding.vertical;
                return padding.top + (available - requiredSize) * factor;
            }
        }

        private static float AlignFactor(HorizontalAlign a)
        {
            return a == HorizontalAlign.Left ? 0f : (a == HorizontalAlign.Center ? 0.5f : 1f);
        }

        private static float AlignFactor(VerticalAlign a)
        {
            return a == VerticalAlign.Top ? 0f : (a == VerticalAlign.Middle ? 0.5f : 1f);
        }
    }
}
