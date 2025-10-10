using System.Collections.Generic;
using System.Drawing;

namespace MonLingo.Core.Model
{
    /// <summary>
    /// 版面分析欄位
    /// 代表版面分析階段二(智能分欄)識別出的單個欄位
    /// </summary>
    public class Column
    {
        /// <summary>
        /// 欄位ID (例如: column_1, column_2)
        /// </summary>
        public string ColumnId { get; set; }

        /// <summary>
        /// 欄位中的文字行列表
        /// </summary>
        public List<LayoutLine> Lines { get; set; } = new List<LayoutLine>();

        /// <summary>
        /// 欄位的整體邊界框
        /// </summary>
        public Rectangle BoundingBox { get; set; }

        /// <summary>
        /// 欄位索引(0-based)
        /// </summary>
        public int ColumnIndex { get; set; }

        /// <summary>
        /// 欄位顏色(用於調試視覺化)
        /// </summary>
        public Color DebugColor { get; set; }
    }
}
