using Syncfusion.WinForms.DataGrid;
using Syncfusion.WinForms.DataGrid.Renderers;
using Syncfusion.WinForms.DataGrid.Styles;
using Syncfusion.WinForms.GridCommon.ScrollAxis;
using System.Drawing;
using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.GridViews
{
    public class AlignedSummaryRenderer : GridTableSummaryCellRenderer
    {
        private const int RightPadding = 10;

        protected override void OnRender( Graphics paint, Rectangle cellRect, string cellValue, CellStyleInfo style, DataColumnBase column, RowColumnIndex rowColumnIndex)
        {
            if (string.IsNullOrEmpty(cellValue))
            {
                base.OnRender(paint, cellRect, cellValue, style, column, rowColumnIndex);
                return;
            }

            var oldHint = paint.TextRenderingHint;

            style.HorizontalAlignment = HorizontalAlignment.Right;

            // Para que pint el texto en Negritas y en el caso de las comas (,) salgan completas
            style.Font.Bold = true;
            paint.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

            Rectangle adjustedRect = new Rectangle( cellRect.X, cellRect.Y, cellRect.Width + RightPadding, cellRect.Height);

            base.OnRender(paint, adjustedRect, cellValue, style, column, rowColumnIndex);

            paint.TextRenderingHint = oldHint;
        }
    }
}
