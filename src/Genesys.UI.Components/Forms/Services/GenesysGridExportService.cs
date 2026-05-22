using Syncfusion.WinForms.DataGrid;
using Syncfusion.WinForms.DataGridConverter;
using Syncfusion.XlsIO;
using System.Windows.Forms;

namespace Genesys.UI.Components.Forms.Services
{
    internal sealed class GenesysGridExportService
    {
        private readonly Form owner;
        private readonly SfDataGrid grid;

        public GenesysGridExportService(Form owner, SfDataGrid grid)
        {
            this.owner = owner;
            this.grid = grid;
        }

        public void ExportarExcel(string defaultFileName)
        {
            if (grid == null || grid.View == null)
                return;

            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Exportar a Excel";
                dialog.Filter = "Archivo Excel (*.xlsx)|*.xlsx";
                dialog.FileName = defaultFileName;

                if (dialog.ShowDialog(owner) != DialogResult.OK)
                    return;

                var options = new ExcelExportingOptions
                {
                    ExportMode = ExportMode.Text,
                    ExcelVersion = ExcelVersion.Excel2013,
                    ExportStackedHeaders = true
                };

                var excelEngine = grid.ExportToExcel(grid.View, options);
                var workbook = excelEngine.Excel.Workbooks[0];

                workbook.SaveAs(dialog.FileName);
                excelEngine.Dispose();
            }
        }

        public void ExportarPdf(string defaultFileName)
        {
            if (grid == null || grid.View == null)
                return;

            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Exportar a PDF";
                dialog.Filter = "Archivo PDF (*.pdf)|*.pdf";
                dialog.FileName = defaultFileName;

                if (dialog.ShowDialog(owner) != DialogResult.OK)
                    return;

                var options = new PdfExportingOptions
                {
                    ExportStackedHeaders = true
                };

                var document = grid.ExportToPdf(options);

                document.Save(dialog.FileName);
                document.Close(true);
            }
        }
    }
}
