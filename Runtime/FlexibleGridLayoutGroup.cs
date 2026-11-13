using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityUtilities.FlexibleGridLayout
{
    [AddComponentMenu("Layout/Flexible Grid Layout Group")]
    public class FlexibleGridLayoutGroup : LayoutGroup
    {
        public enum Corner { UpperLeft = 0, UpperRight = 1, LowerLeft = 2, LowerRight = 3 }

        [SerializeField] protected Corner m_StartCorner = Corner.UpperLeft;
        public Corner startCorner { get { return m_StartCorner; } set { SetProperty(ref m_StartCorner, value); } }

        [SerializeField] protected Vector2 m_CellSize = new Vector2(100, 100);

        public Vector2 cellSize { get { return m_CellSize; } set { SetProperty(ref m_CellSize, value); } }

        //   Fixed  - always use Cell Size on this axis.
        //   Expand - stretch to fill the container, divided evenly across columns/rows.
        //   Aspect - derive from the OTHER axis, keeping the Cell Size aspect ratio.
        public enum CellSizeMode { Fixed = 0, Expand = 1, Aspect = 2 }

        [SerializeField] protected CellSizeMode m_WidthMode = CellSizeMode.Fixed;
        // How the cell width is sized (see CellSizeMode).
        public CellSizeMode widthMode { get { return m_WidthMode; } set { SetProperty(ref m_WidthMode, value); } }

        [SerializeField] protected CellSizeMode m_HeightMode = CellSizeMode.Fixed;
        // How the cell height is sized (see CellSizeMode).
        public CellSizeMode heightMode { get { return m_HeightMode; } set { SetProperty(ref m_HeightMode, value); } }

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
            base.CalculateLayoutInputHorizontal();

            ComputePlacement();

            float totalWidth = padding.horizontal + m_Columns * cellSize.x + (m_Columns - 1) * spacing.x;
            float flexibleWidth = m_WidthMode == CellSizeMode.Expand ? 1 : -1;
            SetLayoutInputForAxis(totalWidth, totalWidth, flexibleWidth, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            float totalHeight = padding.vertical + m_Rows * cellSize.y + (m_Rows - 1) * spacing.y;
            // In Expand mode the grid has no intrinsic height - let a parent layout stretch it.
            float flexibleHeight = m_HeightMode == CellSizeMode.Expand ? 1 : -1;
            SetLayoutInputForAxis(totalHeight, totalHeight, flexibleHeight, 1);
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
                    continue;
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

            // Cell size can scale with the container depending on the per-axis modes.
            Vector2 effectiveCell = GetEffectiveCellSize(cols, rows);

            float requiredWidth = cols * effectiveCell.x + (cols - 1) * spacing.x;
            float requiredHeight = rows * effectiveCell.y + (rows - 1) * spacing.y;
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
                float w = cell.wSpan * effectiveCell.x + (cell.wSpan - 1) * spacing.x;
                float h = cell.hSpan * effectiveCell.y + (cell.hSpan - 1) * spacing.y;

                float xpos = startOffset.x + (effectiveCell.x + spacing.x) * px;
                float ypos = startOffset.y + (effectiveCell.y + spacing.y) * py;

                SetChildAlongAxis(rectChildren[i], 0, xpos, w);
                SetChildAlongAxis(rectChildren[i], 1, ypos, h);
            }
        }

        // Calculates the effective cell size based on the current mode (Fixed, Expand, Aspect) and the container size.
        private Vector2 GetEffectiveCellSize(int cols, int rows)
        {
            float cw = m_CellSize.x;
            float ch = m_CellSize.y;

            CellSizeMode wMode = m_WidthMode;
            CellSizeMode hMode = m_HeightMode;

            // Both axes can't derive from each other - keep width fixed to break the loop.
            if (wMode == CellSizeMode.Aspect && hMode == CellSizeMode.Aspect)
            {
                wMode = CellSizeMode.Fixed;
            }

            Vector2 size = rectTransform.rect.size;

            // Expand first, so an Aspect axis can read the resolved value of the other.
            if (wMode == CellSizeMode.Expand)
            {
                float avail = size.x - padding.horizontal - (cols - 1) * spacing.x;
                cw = cols > 0 ? Mathf.Max(0f, avail) / cols : 0f;
            }
            if (hMode == CellSizeMode.Expand)
            {
                float avail = size.y - padding.vertical - (rows - 1) * spacing.y;
                ch = rows > 0 ? Mathf.Max(0f, avail) / rows : 0f;
            }

            if (wMode == CellSizeMode.Aspect && m_CellSize.y != 0f)
            {
                cw = ch * (m_CellSize.x / m_CellSize.y);
            }
            if (hMode == CellSizeMode.Aspect && m_CellSize.x != 0f)
            {
                ch = cw * (m_CellSize.y / m_CellSize.x);
            }

            return new Vector2(cw, ch);
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
