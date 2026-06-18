using Genesys.UI.Components.Controls.GridViews;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using Syncfusion.WinForms.DataGrid;
using Syncfusion.WinForms.DataGridConverter;
using Syncfusion.WinForms.DataGridConverter.Events;
using Syncfusion.XlsIO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Genesys.UI.Components.Forms.Services
{
    /// <summary>
    /// Fachada pública existente para exportación de grids. Mantiene el API original.
    /// </summary>
    internal sealed partial class GenesysGridExportService
    {

        private readonly Form owner;
        private readonly SfDataGrid grid;

        public GenesysGridExportService(Form owner, SfDataGrid grid)
        {
            this.owner = owner;
            this.grid = grid;
        }

        public void ExportarExcel(string defaultFileName, GenesysGridExportSettings settings)
        {
            if (grid == null || grid.View == null)
                return;

            settings = PrepareExportSettings(settings);
            ApplyExcelNativeOptions(settings);

            while (true)
            {
                GenesysExportFileDialogResult fileResult = ShowExportFileDialog(
                    "Exportar a Excel",
                    "xlsx",
                    defaultFileName,
                    settings);

                if (fileResult == null || !fileResult.Accepted)
                    return;

                if (!EnsureOutputFileAvailable(fileResult.FilePath, "Exportar a Excel"))
                    continue;

                var excelEngine = grid.ExportToExcel(grid.View, settings.ExcelOptions);

                try
                {
                    var workbook = excelEngine.Excel.Workbooks[0];
                    ConfigureExcelWorkbook(workbook, settings);
                    workbook.SaveAs(fileResult.FilePath);
                }
                finally
                {
                    excelEngine.Dispose();
                }

                HandleAfterExport(fileResult);
            }
        }

        public void ExportarPdf(string defaultFileName, GenesysGridExportSettings settings)
        {
            if (grid == null || grid.View == null)
                return;

            settings = PreparePdfExportSettings(settings);

            while (true)
            {
                GenesysExportFileDialogResult fileResult = ShowExportFileDialog(
                    "Exportar a PDF",
                    "pdf",
                    defaultFileName,
                    settings);

                if (fileResult == null || !fileResult.Accepted)
                    return;

                if (!EnsureOutputFileAvailable(fileResult.FilePath, "Exportar a PDF"))
                    continue;

                PdfDocument document = new PdfDocument();

                try
                {
                    // PDF estándar Syncfusion:
                    // - Se conserva el flujo SfDataGrid -> ExportToPdfGrid -> PdfGrid.Draw.
                    // - El formulario hijo sigue configurando directamente settings.PdfOptions.
                    // - Genesys conecta el CellExporting necesario para respetar
                    //   la alineación de la Vista activa sin cambiar el layout PDF de Syncfusion.
                    PdfExportingOptions pdfOptions = CreatePdfExportingOptions(settings);
                    AttachPdfExportingFontHandler(settings, pdfOptions);
                    AttachPdfCellExportingHandler(settings, pdfOptions);

                    ApplyPdfDocumentOutputSettings(document, settings);

                    PdfGrid pdfGrid = grid.ExportToPdfGrid(grid.View, pdfOptions);

                    PdfPage page = document.Pages.Add();

                    PdfGridLayoutFormat layout = new PdfGridLayoutFormat();
                    SetPdfGridLayoutValue(layout, "Layout", settings.PdfScalingMode == GenesysPdfScalingMode.FitSheetOnOnePage || settings.PdfFitAllRowsInOnePage ? "OnePage" : "Paginate");

                    pdfGrid.Draw(page, new PointF(0, 0), layout);
                    AppendPdfDocumentSummary(document, settings);
                    document.Save(fileResult.FilePath);
                }
                finally
                {
                    document.Close(true);
                }

                HandleAfterExport(fileResult);
            }
        }

    }
}
