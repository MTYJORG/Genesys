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
using System.Windows.Forms;

namespace Genesys.UI.Components.Forms.Services
{
    /// <summary>
    /// Modo de selección de papel para PDF.
    /// AutomaticByColumns calcula el tamaño según el ancho visible del grid.
    /// </summary>
    public enum GenesysPdfPaperMode
    {
        AutomaticByColumns,
        LetterLandscape,
        LegalLandscape,
        DoubleLetterLandscape,
        TripleLetterLandscape,
        A3Landscape,
        CustomLandscape
    }

    /// <summary>
    /// Objetivo de impresión del PDF.
    /// Sirve como documentación/configuración para saber si el PDF se genera para verse completo
    /// o para imprimirse reducido en carta horizontal.
    /// </summary>
    public enum GenesysPdfPrintTarget
    {
        OriginalSize,
        LetterLandscapeScaled
    }

    /// <summary>
    /// Configuración única de exportación Excel/PDF.
    /// El framework asigna valores por default y cada formulario hijo puede sobrescribirlos
    /// desde un solo método: ConfigureExport(settings).
    /// </summary>
    public sealed class GenesysGridExportSettings
    {
        public GenesysGridExportSettings()
        {
            ExcelOptions = new ExcelExportingOptions();
            PdfOptions = new PdfExportingOptions();
            ExcludedColumns = new List<string>();

            WorksheetName = "Reporte";
            Title = "Reporte";
            CompanyName = "Genesys";

            HeaderBackColor = Color.FromArgb(217, 225, 242);
            HeaderForeColor = Color.Black;

            ExcelHeaderRowIndex = 1;
            ExcelFreezeHeader = true;
            ExcelAutoFilter = true;
            ExcelAutoFitColumns = true;
            ExcelAppendFooter = true;

            PdfPaperMode = GenesysPdfPaperMode.AutomaticByColumns;
            PdfPrintTarget = GenesysPdfPrintTarget.OriginalSize;
            PdfMargins = 25F;
            PdfFooterHeight = 30F;
            PdfMinimumFontSize = 7F;
            PdfMaximumFontSize = 10F;
            PdfFitAllColumnsInOnePage = true;
            PdfAutoRowHeight = true;
            PdfRepeatHeaders = true;
            PdfApplyViewColumnWidths = true;
            PdfMaximumColumnWidth = 110F;
            PdfMinimumColumnWidth = 34F;
            PdfAllowTextWrap = true;
            PdfMaximumTextLines = 2;

            ExportDialogPersistenceKey = string.Empty;
            ExportDialogDefaultFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            OpenFileAfterExportDefault = false;
            OpenFolderAfterExportDefault = false;

            // Tamaño default en puntos PDF para CustomLandscape. 1 pulgada = 72 puntos.
            PdfCustomLandscapeSize = new SizeF(1224F, 792F); // Doble carta horizontal.

            ExcelOptions.ExportMode = ExportMode.Value;
            ExcelOptions.ExcelVersion = ExcelVersion.Excel2013;
            ExcelOptions.ExportStackedHeaders = true;
            ExcelOptions.ExportUnboundRows = true;
            ExcelOptions.AllowOutlining = true;
            ExcelOptions.StartRowIndex = 1;
            ExcelOptions.StartColumnIndex = 1;

            PdfOptions.ExportStackedHeaders = true;
            PdfOptions.ExportUnboundRows = true;
            PdfOptions.AutoColumnWidth = false;
            PdfOptions.AutoRowHeight = true;
            PdfOptions.RepeatHeaders = true;
            PdfOptions.FitAllColumnsInOnePage = true;
            PdfOptions.ExportFormat = true;
        }

        public ExcelExportingOptions ExcelOptions { get; private set; }
        public PdfExportingOptions PdfOptions { get; private set; }
        public IList<string> ExcludedColumns { get; private set; }

        /// <summary>
        /// Layout activo capturado desde el módulo de Vistas.
        /// La exportación debe usar primero Columns[].Alignment, Width, Format y Visible.
        /// </summary>
        public GridViewLayout CurrentViewLayout { get; set; }

        public string WorksheetName { get; set; }
        public string Title { get; set; }
        public string CompanyName { get; set; }

        public Color HeaderBackColor { get; set; }
        public Color HeaderForeColor { get; set; }

        public int ExcelHeaderRowIndex { get; set; }
        public bool ExcelFreezeHeader { get; set; }
        public bool ExcelAutoFilter { get; set; }
        public bool ExcelAutoFitColumns { get; set; }
        public bool ExcelAppendFooter { get; set; }

        public GenesysPdfPaperMode PdfPaperMode { get; set; }
        public GenesysPdfPrintTarget PdfPrintTarget { get; set; }
        public SizeF PdfCustomLandscapeSize { get; set; }
        public float PdfMargins { get; set; }
        public float PdfFooterHeight { get; set; }
        public float PdfMinimumFontSize { get; set; }
        public float PdfMaximumFontSize { get; set; }
        public bool PdfFitAllColumnsInOnePage { get; set; }
        public bool PdfAutoRowHeight { get; set; }
        public bool PdfRepeatHeaders { get; set; }

        /// <summary>
        /// Cuando está activo, el PDF intenta respetar el ancho de columnas de la vista/grid.
        /// </summary>
        public bool PdfApplyViewColumnWidths { get; set; }

        /// <summary>
        /// Ancho máximo por columna PDF en puntos. Evita que una columna muy grande se coma la página.
        /// </summary>
        public float PdfMaximumColumnWidth { get; set; }

        /// <summary>
        /// Ancho mínimo por columna PDF en puntos. Evita columnas ilegibles.
        /// </summary>
        public float PdfMinimumColumnWidth { get; set; }

        /// <summary>
        /// Permite partir el texto de celdas PDF en varios renglones.
        /// </summary>
        public bool PdfAllowTextWrap { get; set; }

        /// <summary>
        /// Máximo sugerido de renglones para valores PDF. La implementación limita altura de fila de forma prudente.
        /// </summary>
        public int PdfMaximumTextLines { get; set; }

        /// <summary>
        /// Llave base para persistir opciones del diálogo de exportación.
        /// El servicio agrega el formato: .Excel o .Pdf.
        /// </summary>
        public string ExportDialogPersistenceKey { get; set; }

        /// <summary>
        /// Carpeta sugerida cuando no existe persistencia previa.
        /// </summary>
        public string ExportDialogDefaultFolder { get; set; }

        public bool OpenFileAfterExportDefault { get; set; }
        public bool OpenFolderAfterExportDefault { get; set; }

        public void ExcludeColumn(string mappingName)
        {
            if (string.IsNullOrWhiteSpace(mappingName))
                return;

            if (!ExcludedColumns.Contains(mappingName))
                ExcludedColumns.Add(mappingName);
        }
    }

    internal sealed class GenesysGridExportService
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

            GenesysExportFileDialogResult fileResult = ShowExportFileDialog(
                "Exportar a Excel",
                "xlsx",
                defaultFileName,
                settings);

            if (fileResult == null || !fileResult.Accepted)
                return;

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

        public void ExportarPdf(string defaultFileName, GenesysGridExportSettings settings)
        {
            if (grid == null || grid.View == null)
                return;

            settings = PrepareExportSettings(settings);

            GenesysExportFileDialogResult fileResult = ShowExportFileDialog(
                "Exportar a PDF",
                "pdf",
                defaultFileName,
                settings);

            if (fileResult == null || !fileResult.Accepted)
                return;

            PdfDocument document = new PdfDocument();

            try
            {
                ConfigurePdfDocumentBeforeDraw(document, settings);

                // Se crea una instancia nueva de PdfExportingOptions en cada exportación.
                // Evita acumular handlers CellExporting cuando el usuario exporta varias veces.
                PdfExportingOptions pdfOptions = CreatePdfExportingOptions(settings);
                AttachPdfExportingFontHandler(settings, pdfOptions);
                AttachPdfCellExportingHandler(settings, pdfOptions);

                PdfGrid pdfGrid = grid.ExportToPdfGrid(grid.View, pdfOptions);
                ConfigurePdfGridLayoutOnly(pdfGrid, settings);

                PdfPage page = document.Pages.Add();

                PdfGridLayoutFormat layout = new PdfGridLayoutFormat();
                SetPdfGridLayoutValue(layout, "Layout", "Paginate");
                SetPdfGridLayoutValue(layout, "Break", "FitPage");

                pdfGrid.Draw(page, new PointF(0, 0), layout);
                document.Save(fileResult.FilePath);
            }
            finally
            {
                document.Close(true);
            }

            HandleAfterExport(fileResult);
        }

        private GenesysGridExportSettings PrepareExportSettings(GenesysGridExportSettings settings)
        {
            if (settings == null)
                settings = new GenesysGridExportSettings();

            ResolveCurrentViewLayout(settings);
            AddDefaultExcludedColumns(settings);
            ApplyExcludedColumns(settings);
            ApplyAutomaticPdfPaper(settings);

            return settings;
        }

        private void ResolveCurrentViewLayout(GenesysGridExportSettings settings)
        {
            if (settings == null || settings.CurrentViewLayout != null || owner == null)
                return;

            try
            {
                System.Reflection.PropertyInfo property = null;
                Type ownerType = owner.GetType();

                while (ownerType != null && property == null)
                {
                    property = ownerType.GetProperty(
                        "VistasAdministrador",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.DeclaredOnly);

                    ownerType = ownerType.BaseType;
                }

                object manager = property == null ? null : property.GetValue(owner, null);
                if (manager == null)
                    return;

                // Captura runtime: incluye cambios no guardados de ancho, visible, formato, alineación y orden.
                var method = manager.GetType().GetMethod(
                    "CaptureCurrentRuntimeLayout",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic,
                    null,
                    Type.EmptyTypes,
                    null);

                GridViewLayout layout = method == null ? null : method.Invoke(manager, null) as GridViewLayout;
                if (layout != null)
                    settings.CurrentViewLayout = layout;
            }
            catch
            {
            }
        }

        private void AddDefaultExcludedColumns(GenesysGridExportSettings settings)
        {
            if (settings == null || grid == null || grid.Columns == null)
                return;

            foreach (var column in grid.Columns)
            {
                if (column == null || column.Visible)
                    continue;

                settings.ExcludeColumn(column.MappingName);
            }
        }

        private void ApplyExcludedColumns(GenesysGridExportSettings settings)
        {
            if (settings == null || settings.ExcludedColumns == null)
                return;

            foreach (string mappingName in settings.ExcludedColumns)
            {
                AddExcelExcludedColumn(settings.ExcelOptions, mappingName);
                AddPdfExcludedColumn(settings.PdfOptions, mappingName);
            }
        }

        private void ApplyAutomaticPdfPaper(GenesysGridExportSettings settings)
        {
            if (settings == null)
                return;

            if (settings.PdfPaperMode != GenesysPdfPaperMode.AutomaticByColumns)
                return;

            float columnsWidth = GetFinalPdfColumnsWidth(settings);
            float pageWidth = columnsWidth + (settings.PdfMargins * 2F) + 20F;

            // La página se calcula sobre el ancho real final de columnas PDF.
            // Así se evita comprimir el grid y que los encabezados/valores queden letra por letra.
            if (pageWidth <= 792F)
            {
                settings.PdfPaperMode = GenesysPdfPaperMode.LetterLandscape;
                return;
            }

            if (pageWidth <= 1008F)
            {
                settings.PdfPaperMode = GenesysPdfPaperMode.LegalLandscape;
                return;
            }

            if (pageWidth <= 1224F)
            {
                settings.PdfPaperMode = GenesysPdfPaperMode.DoubleLetterLandscape;
                return;
            }

            if (pageWidth <= 2376F)
            {
                settings.PdfPaperMode = GenesysPdfPaperMode.TripleLetterLandscape;
                return;
            }

            // Para grids más anchos, el framework genera una hoja horizontal personalizada
            // con el ancho exacto requerido por las columnas visibles.
            settings.PdfPaperMode = GenesysPdfPaperMode.CustomLandscape;
            settings.PdfCustomLandscapeSize = new SizeF(pageWidth, 792F);
        }

        private float GetFinalPdfColumnsWidth(GenesysGridExportSettings settings)
        {
            if (grid == null || grid.Columns == null)
                return 0F;

            float total = 0F;

            foreach (var column in grid.Columns)
            {
                if (column == null || !column.Visible)
                    continue;

                if (settings != null && settings.ExcludedColumns != null && settings.ExcludedColumns.Contains(column.MappingName))
                    continue;

                float width = GetExportColumnWidth(column, settings);

                if (settings != null && settings.PdfMaximumColumnWidth > 0F && width > settings.PdfMaximumColumnWidth)
                    width = settings.PdfMaximumColumnWidth;

                if (settings != null && settings.PdfMinimumColumnWidth > 0F && width < settings.PdfMinimumColumnWidth)
                    width = settings.PdfMinimumColumnWidth;

                total += width;
            }

            return total;
        }

        private float GetVisibleGridColumnsWidth(GenesysGridExportSettings settings)
        {
            if (grid == null || grid.Columns == null)
                return 0F;

            float total = 0F;

            foreach (var column in grid.Columns)
            {
                if (column == null || !column.Visible)
                    continue;

                if (settings != null && settings.ExcludedColumns != null && settings.ExcludedColumns.Contains(column.MappingName))
                    continue;

                float width = 80F;

                try
                {
                    if (column.Width > 0)
                        width = (float)column.Width;
                }
                catch
                {
                }

                total += width;
            }

            return total;
        }

        private void ConfigureExcelWorkbook(IWorkbook workbook, GenesysGridExportSettings settings)
        {
            if (workbook == null || workbook.Worksheets.Count == 0 || settings == null)
                return;

            ApplyExcelWorkbookDefaults(workbook, settings);
            RenameExcelWorksheetsInSpanish(workbook, settings);

            IWorksheet sheet = workbook.Worksheets[0];

            ApplyExcelActiveViewFormatting(sheet, settings);

            if (settings.ExcelAppendFooter)
                AppendExcelExportFooter(sheet, settings);

            if (sheet.UsedRange != null)
            {
                if (settings.ExcelAutoFilter)
                {
                    try
                    {
                        IRange exportRange = GetExcelExportedGridRange(sheet, settings);
                        if (exportRange != null)
                            sheet.AutoFilters.FilterRange = exportRange;
                    }
                    catch
                    {
                    }
                }

                if (settings.ExcelAutoFitColumns)
                    sheet.UsedRange.AutofitColumns();
            }

            workbook.BuiltInDocumentProperties.Author = Environment.UserName;
            workbook.BuiltInDocumentProperties.Title = settings.Title;
            workbook.BuiltInDocumentProperties.Subject = settings.Title;
            workbook.BuiltInDocumentProperties.Company = settings.CompanyName;
        }


        private void ApplyExcelWorkbookDefaults(IWorkbook workbook, GenesysGridExportSettings settings)
        {
            if (workbook == null)
                return;

            TrySetProperty(workbook, "StandardFont", "Calibri");
            TrySetProperty(workbook, "StandardFontSize", 11D);

            try
            {
                foreach (IWorksheet worksheet in workbook.Worksheets)
                {
                    if (worksheet == null || worksheet.UsedRange == null)
                        continue;

                    worksheet.UsedRange.CellStyle.Font.FontName = "Calibri";
                    worksheet.UsedRange.CellStyle.Font.Size = 11;
                }
            }
            catch
            {
            }
        }

        private void RenameExcelWorksheetsInSpanish(IWorkbook workbook, GenesysGridExportSettings settings)
        {
            if (workbook == null || workbook.Worksheets == null)
                return;

            for (int index = 0; index < workbook.Worksheets.Count; index++)
            {
                IWorksheet worksheet = workbook.Worksheets[index];

                if (worksheet == null)
                    continue;

                string name = index == 0
                    ? (settings == null ? "Reporte" : settings.WorksheetName)
                    : "Hoja" + (index + 1).ToString();

                try
                {
                    worksheet.Name = GetSafeWorksheetName(name);
                }
                catch
                {
                }
            }
        }

        private static void TrySetProperty(object target, string propertyName, object value)
        {
            if (target == null || string.IsNullOrWhiteSpace(propertyName))
                return;

            try
            {
                var property = target.GetType().GetProperty(propertyName);

                if (property == null || !property.CanWrite)
                    return;

                object convertedValue = value;

                if (value != null && property.PropertyType != value.GetType())
                    convertedValue = Convert.ChangeType(value, property.PropertyType);

                property.SetValue(target, convertedValue, null);
            }
            catch
            {
            }
        }


        private PdfExportingOptions CreatePdfExportingOptions(GenesysGridExportSettings settings)
        {
            PdfExportingOptions options = new PdfExportingOptions();

            if (settings == null)
                return options;

            options.ExportStackedHeaders = settings.PdfOptions.ExportStackedHeaders;
            options.ExportUnboundRows = settings.PdfOptions.ExportUnboundRows;
            options.AutoColumnWidth = settings.PdfOptions.AutoColumnWidth;
            options.AutoRowHeight = settings.PdfOptions.AutoRowHeight;
            options.RepeatHeaders = settings.PdfOptions.RepeatHeaders;
            options.FitAllColumnsInOnePage = settings.PdfOptions.FitAllColumnsInOnePage;
            options.ExportFormat = settings.PdfOptions.ExportFormat;

            if (settings.ExcludedColumns != null)
            {
                foreach (string mappingName in settings.ExcludedColumns)
                    AddPdfExcludedColumn(options, mappingName);
            }

            return options;
        }

        /// <summary>
        /// Aplica la fuente durante la exportación oficial de Syncfusion.
        /// Syncfusion documenta que el cambio de fuente debe hacerse en
        /// PdfExportingOptions.Exporting usando e.CellStyle.Font, antes de crear el PdfGrid.
        /// No se aplica en ConfigurePdfGridLayoutOnly porque ahí el PdfGrid ya fue generado.
        /// </summary>
        private void AttachPdfExportingFontHandler(GenesysGridExportSettings settings, PdfExportingOptions options)
        {
            if (settings == null || options == null)
                return;

            PdfFont headerFont = new PdfStandardFont(
                PdfFontFamily.Helvetica,
                settings.PdfMaximumFontSize,
                PdfFontStyle.Bold);

            PdfFont cellFont = new PdfStandardFont(
                PdfFontFamily.Helvetica,
                settings.PdfMinimumFontSize,
                PdfFontStyle.Regular);

            options.Exporting += delegate (object sender, DataGridPdfExportingEventArgs e)
            {
                if (e == null || e.CellStyle == null)
                    return;

                if (e.CellType == ExportCellType.HeaderCell)
                {
                    e.CellStyle.Font = headerFont;
                    return;
                }

                if (e.CellType == ExportCellType.RecordCell)
                    e.CellStyle.Font = cellFont;
            };
        }

        /// <summary>
        /// Aplica formato PDF durante la conversión oficial de Syncfusion.
        /// La documentación de Syncfusion indica que la alineación/formato de celdas PDF
        /// debe aplicarse en PdfExportingOptions.CellExporting usando e.PdfGridCell.StringFormat.
        /// Esto es más confiable que modificar únicamente PdfGridCell.Style después de exportar.
        /// </summary>
        private void AttachPdfCellExportingHandler(GenesysGridExportSettings settings, PdfExportingOptions options)
        {
            if (settings == null || options == null)
                return;

            options.CellExporting += delegate (object sender, DataGridCellPdfExportingEventArgs e)
            {
                if (e == null || e.PdfGridCell == null)
                    return;

                if (e.CellType == ExportCellType.HeaderCell)
                {
                    e.PdfGridCell.StringFormat = CreatePdfStringFormat(PdfTextAlignment.Center, true, settings);

                    if (e.PdfGridCell.Style != null)
                    {
                        e.PdfGridCell.Style.BackgroundBrush = new PdfSolidBrush(
                            new PdfColor(settings.HeaderBackColor.R, settings.HeaderBackColor.G, settings.HeaderBackColor.B));
                        e.PdfGridCell.Style.TextBrush = new PdfSolidBrush(
                            new PdfColor(settings.HeaderForeColor.R, settings.HeaderForeColor.G, settings.HeaderForeColor.B));
                        e.PdfGridCell.Style.Borders.All = new PdfPen(PdfBrushes.Gray, 0.5F);
                    }

                    return;
                }

                if (e.CellType != ExportCellType.RecordCell)
                    return;

                string columnName = e.ColumnName;
                object gridColumn = FindGridColumnByMappingName(columnName);
                string mappingName = GetColumnMappingName(gridColumn);

                // En Syncfusion el ColumnName del evento puede llegar como HeaderText.
                // Siempre que sea posible se resuelve primero al MappingName real.
                if (string.IsNullOrWhiteSpace(mappingName))
                    mappingName = columnName;

                Type dataType = GetColumnDataType(mappingName);

                bool alignmentCameFromView;
                PdfTextAlignment alignment = GetPdfAlignmentForColumn(gridColumn, columnName, dataType, settings, out alignmentCameFromView);

                // Sólo aplicar fallback por valor numérico cuando la Vista NO definió alineación.
                // Si la Vista dice izquierda/centro/derecha, se respeta aunque el dato sea numérico.
                if (!alignmentCameFromView && alignment == PdfTextAlignment.Left && IsObjectNumericValue(e.CellValue))
                    alignment = PdfTextAlignment.Right;

                e.PdfGridCell.StringFormat = CreatePdfStringFormat(alignment, true, settings);

                if (e.PdfGridCell.Style != null)
                    e.PdfGridCell.Style.Borders.All = new PdfPen(PdfBrushes.LightGray, 0.3F);
            };
        }

        private object FindGridColumnByMappingName(string columnNameOrHeader)
        {
            if (string.IsNullOrWhiteSpace(columnNameOrHeader) || grid == null || grid.Columns == null)
                return null;

            foreach (var column in grid.Columns)
            {
                if (column == null)
                    continue;

                string mappingName = GetColumnMappingName(column);
                string headerText = GetColumnHeaderText(column);

                if (string.Equals(mappingName, columnNameOrHeader, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(headerText, columnNameOrHeader, StringComparison.OrdinalIgnoreCase))
                    return column;
            }

            return null;
        }

        private static bool IsObjectNumericValue(object value)
        {
            if (value == null)
                return false;

            if (value is byte || value is short || value is int || value is long ||
                value is decimal || value is double || value is float)
                return true;

            string text = Convert.ToString(value);
            if (string.IsNullOrWhiteSpace(text))
                return false;

            decimal numeric;
            return decimal.TryParse(text, System.Globalization.NumberStyles.Number | System.Globalization.NumberStyles.Currency,
                System.Globalization.CultureInfo.CurrentCulture, out numeric) ||
                   decimal.TryParse(text, System.Globalization.NumberStyles.Number | System.Globalization.NumberStyles.Currency,
                System.Globalization.CultureInfo.InvariantCulture, out numeric);
        }

        private void ConfigurePdfDocumentBeforeDraw(PdfDocument document, GenesysGridExportSettings settings)
        {
            if (document == null || settings == null)
                return;

            document.PageSettings.Size = GetPdfPageSize(settings);
            document.PageSettings.Orientation = PdfPageOrientation.Landscape;
            document.PageSettings.Margins.All = settings.PdfMargins;

            AddPdfFooter(document, settings);

            document.DocumentInformation.Title = settings.Title;
            document.DocumentInformation.Subject = settings.Title;
            document.DocumentInformation.Author = Environment.UserName;
            document.DocumentInformation.Creator = settings.CompanyName;
        }

        /// <summary>
        /// Ajustes posteriores al ExportToPdfGrid que NO tocan alineación.
        /// La alineación debe quedarse únicamente en PdfExportingOptions.CellExporting,
        /// tal como lo documenta Syncfusion para PdfGridCell.StringFormat.
        /// </summary>
        private void ConfigurePdfGridLayoutOnly(PdfGrid pdfGrid, GenesysGridExportSettings settings)
        {
            if (pdfGrid == null || settings == null)
                return;

            List<object> exportColumns = GetExportableGridColumns(settings);

            // Solo anchos y bordes. No aplicar fuente ni StringFormat aquí:
            // - La fuente se aplica en PdfExportingOptions.Exporting.
            // - La alineación/StringFormat se aplica en PdfExportingOptions.CellExporting.
            ApplyPdfViewColumnWidths(pdfGrid, settings, exportColumns);

            foreach (PdfGridRow row in pdfGrid.Headers)
            {
                row.Style.BackgroundBrush = new PdfSolidBrush(new PdfColor(settings.HeaderBackColor.R, settings.HeaderBackColor.G, settings.HeaderBackColor.B));
                row.Style.TextBrush = new PdfSolidBrush(new PdfColor(settings.HeaderForeColor.R, settings.HeaderForeColor.G, settings.HeaderForeColor.B));

                for (int cellIndex = 0; cellIndex < row.Cells.Count; cellIndex++)
                {
                    PdfGridCell cell = row.Cells[cellIndex];
                    cell.Style.Borders.All = new PdfPen(PdfBrushes.Gray, 0.5F);
                }
            }

            foreach (PdfGridRow row in pdfGrid.Rows)
            {
                for (int cellIndex = 0; cellIndex < row.Cells.Count; cellIndex++)
                {
                    PdfGridCell cell = row.Cells[cellIndex];
                    cell.Style.Borders.All = new PdfPen(PdfBrushes.LightGray, 0.3F);
                }
            }
        }

        private List<object> GetExportableGridColumns(GenesysGridExportSettings settings)
        {
            List<object> columns = new List<object>();

            if (grid == null || grid.Columns == null)
                return columns;

            foreach (var column in grid.Columns)
            {
                if (column == null || !column.Visible)
                    continue;

                string mappingName = column.MappingName;

                if (settings != null && settings.ExcludedColumns != null && settings.ExcludedColumns.Contains(mappingName))
                    continue;

                columns.Add(column);
            }

            return columns;
        }


        private void ApplyPdfColumnStringFormats(PdfGrid pdfGrid, GenesysGridExportSettings settings, List<object> exportColumns)
        {
            // Intencionalmente no se aplica StringFormat después de CellExporting.
            // En Syncfusion 33.x hacerlo aquí sobreescribe la alineación correcta.
        }

        private void ApplyPdfViewColumnWidths(PdfGrid pdfGrid, GenesysGridExportSettings settings, List<object> exportColumns)
        {
            if (pdfGrid == null || settings == null || !settings.PdfApplyViewColumnWidths || exportColumns == null)
                return;

            int count = Math.Min(pdfGrid.Columns.Count, exportColumns.Count);

            for (int index = 0; index < count; index++)
            {
                float width = GetExportColumnWidth(exportColumns[index], settings);

                if (settings.PdfMaximumColumnWidth > 0F && width > settings.PdfMaximumColumnWidth)
                    width = settings.PdfMaximumColumnWidth;

                if (settings.PdfMinimumColumnWidth > 0F && width < settings.PdfMinimumColumnWidth)
                    width = settings.PdfMinimumColumnWidth;

                try
                {
                    pdfGrid.Columns[index].Width = width;
                }
                catch
                {
                }
            }
        }

        private float GetGridColumnWidth(object gridColumn)
        {
            if (gridColumn == null)
                return 70F;

            foreach (string propertyName in new[] { "ActualWidth", "Width" })
            {
                try
                {
                    var property = gridColumn.GetType().GetProperty(propertyName);
                    if (property == null)
                        continue;

                    object value = property.GetValue(gridColumn, null);
                    if (value == null)
                        continue;

                    float width = Convert.ToSingle(value);
                    if (width > 0F)
                        return width;
                }
                catch
                {
                }
            }

            return 70F;
        }

        private float GetExportColumnWidth(object gridColumn, GenesysGridExportSettings settings)
        {
            string mappingName = GetColumnMappingName(gridColumn);
            string headerText = GetColumnHeaderText(gridColumn);
            GenesysGridColumnLayout layoutColumn = GetLayoutColumn(settings, mappingName, headerText);

            if (layoutColumn != null && layoutColumn.Width > 0D)
                return (float)layoutColumn.Width;

            return GetGridColumnWidth(gridColumn);
        }

        private string GetExportColumnFormat(object gridColumn, GenesysGridExportSettings settings)
        {
            string mappingName = GetColumnMappingName(gridColumn);
            string headerText = GetColumnHeaderText(gridColumn);
            GenesysGridColumnLayout layoutColumn = GetLayoutColumn(settings, mappingName, headerText);

            if (layoutColumn != null && !string.IsNullOrWhiteSpace(layoutColumn.Format))
                return layoutColumn.Format;

            return GetColumnFormatFromGrid(gridColumn);
        }

        private GenesysGridColumnLayout GetLayoutColumn(GenesysGridExportSettings settings, string mappingName, string headerText)
        {
            if (settings == null || settings.CurrentViewLayout == null || settings.CurrentViewLayout.Columns == null)
                return null;

            foreach (GenesysGridColumnLayout column in settings.CurrentViewLayout.Columns)
            {
                if (column == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(mappingName) &&
                    string.Equals(column.MappingName, mappingName, StringComparison.OrdinalIgnoreCase))
                    return column;

                if (!string.IsNullOrWhiteSpace(headerText) &&
                    string.Equals(column.HeaderText, headerText, StringComparison.OrdinalIgnoreCase))
                    return column;
            }

            return null;
        }

        private static string GetColumnMappingName(object gridColumn)
        {
            if (gridColumn == null)
                return null;

            try
            {
                var property = gridColumn.GetType().GetProperty("MappingName");
                if (property == null)
                    return null;

                object value = property.GetValue(gridColumn, null);
                return value == null ? null : Convert.ToString(value);
            }
            catch
            {
                return null;
            }
        }

        private static string GetColumnHeaderText(object gridColumn)
        {
            if (gridColumn == null)
                return null;

            try
            {
                var property = gridColumn.GetType().GetProperty("HeaderText");
                if (property == null)
                    return null;

                object value = property.GetValue(gridColumn, null);
                return value == null ? null : Convert.ToString(value);
            }
            catch
            {
                return null;
            }
        }
        private PdfTextAlignment GetPdfAlignmentForColumn(object gridColumn, Type dataType)
        {
            bool ignored;
            return GetPdfAlignmentForColumn(gridColumn, null, dataType, null, out ignored);
        }

        private PdfTextAlignment GetPdfAlignmentForColumn(
            object gridColumn,
            string columnNameOrHeader,
            Type dataType,
            GenesysGridExportSettings settings,
            out bool alignmentCameFromView)
        {
            alignmentCameFromView = false;

            string mappingName = GetColumnMappingName(gridColumn);
            string headerText = GetColumnHeaderText(gridColumn);

            GenesysGridColumnLayout layoutColumn = GetLayoutColumn(settings, mappingName, headerText);
            if (layoutColumn == null && !string.IsNullOrWhiteSpace(columnNameOrHeader))
                layoutColumn = GetLayoutColumn(settings, columnNameOrHeader, columnNameOrHeader);

            string viewAlignment = layoutColumn == null ? null : layoutColumn.Alignment;
            alignmentCameFromView = GenesysGridColumnVisualHelper.IsExplicitAlignment(viewAlignment);

            string effectiveAlignment = GenesysGridColumnVisualHelper.ResolveEffectiveAlignment(
                viewAlignment,
                gridColumn,
                dataType,
                GetExportColumnFormat(gridColumn, settings));

            return ToPdfTextAlignment(effectiveAlignment);
        }

        private static PdfTextAlignment ToPdfTextAlignment(string alignment)
        {
            string normalized = GenesysGridColumnVisualHelper.NormalizeAlignment(alignment);

            if (normalized == GenesysGridColumnVisualHelper.AlignmentRight)
                return PdfTextAlignment.Right;

            if (normalized == GenesysGridColumnVisualHelper.AlignmentCenter)
                return PdfTextAlignment.Center;

            return PdfTextAlignment.Left;
        }

        private static string GetStringPropertyValue(object target, params string[] propertyNames)
        {
            if (target == null || propertyNames == null)
                return null;

            foreach (string propertyName in propertyNames)
            {
                try
                {
                    var property = target.GetType().GetProperty(propertyName);
                    if (property == null)
                        continue;

                    object value = property.GetValue(target, null);
                    if (value != null && !string.IsNullOrWhiteSpace(Convert.ToString(value)))
                        return Convert.ToString(value);
                }
                catch
                {
                }
            }

            return null;
        }

        private static object GetObjectPropertyValue(object target, string propertyName)
        {
            if (target == null || string.IsNullOrWhiteSpace(propertyName))
                return null;

            try
            {
                var property = target.GetType().GetProperty(propertyName);
                return property == null ? null : property.GetValue(target, null);
            }
            catch
            {
                return null;
            }
        }


        private static bool IsPdfCellNumericValue(PdfGridCell cell)
        {
            object value = GetObjectPropertyValue(cell, "Value");
            if (value == null)
                return false;

            if (value is byte || value is short || value is int || value is long ||
                value is decimal || value is double || value is float)
                return true;

            string text = Convert.ToString(value);
            if (string.IsNullOrWhiteSpace(text))
                return false;

            decimal numeric;
            return decimal.TryParse(text, System.Globalization.NumberStyles.Number | System.Globalization.NumberStyles.Currency,
                System.Globalization.CultureInfo.CurrentCulture, out numeric) ||
                   decimal.TryParse(text, System.Globalization.NumberStyles.Number | System.Globalization.NumberStyles.Currency,
                System.Globalization.CultureInfo.InvariantCulture, out numeric);
        }

        private static void ApplyPdfCellStringFormat(PdfGridCell cell, PdfStringFormat format)
        {
            if (cell == null || format == null)
                return;

            // En Syncfusion.Pdf.Grid algunas versiones usan PdfGridCell.StringFormat
            // y otras usan PdfGridCell.Style.StringFormat. Se aplican ambas rutas.
            TrySetProperty(cell, "StringFormat", format);

            try
            {
                PdfGridCellStyle style = cell.Style;

                if (style == null)
                    style = new PdfGridCellStyle();

                style.StringFormat = format;
                cell.Style = style;
            }
            catch
            {
                try
                {
                    cell.Style.StringFormat = format;
                }
                catch
                {
                }
            }
        }

        private PdfStringFormat CreatePdfStringFormat(PdfTextAlignment alignment, bool allowWrap, GenesysGridExportSettings settings)
        {
            PdfStringFormat format = new PdfStringFormat(alignment, PdfVerticalAlignment.Middle);

            if (settings != null && settings.PdfAllowTextWrap && allowWrap)
                TryEnablePdfWordWrap(format);

            return format;
        }

        private static void TryEnablePdfWordWrap(PdfStringFormat format)
        {
            if (format == null)
                return;

            try
            {
                var property = format.GetType().GetProperty("WordWrap");
                if (property == null || !property.CanWrite)
                    return;

                object value = Enum.Parse(property.PropertyType, "Word");
                property.SetValue(format, value, null);
            }
            catch
            {
            }
        }

        private SizeF GetPdfPageSize(GenesysGridExportSettings settings)
        {
            switch (settings.PdfPaperMode)
            {
                case GenesysPdfPaperMode.LetterLandscape:
                    return new SizeF(792F, 612F);          // Carta horizontal 11 x 8.5

                case GenesysPdfPaperMode.LegalLandscape:
                    return new SizeF(1008F, 612F);         // Legal horizontal 14 x 8.5

                case GenesysPdfPaperMode.DoubleLetterLandscape:
                    return new SizeF(1224F, 792F);         // Doble carta / Tabloid horizontal 17 x 11

                case GenesysPdfPaperMode.TripleLetterLandscape:
                    return new SizeF(2376F, 612F);         // Triple carta horizontal 33 x 8.5

                case GenesysPdfPaperMode.A3Landscape:
                    return new SizeF(1190F, 842F);         // A3 horizontal aproximado

                case GenesysPdfPaperMode.CustomLandscape:
                    return settings.PdfCustomLandscapeSize;

                default:
                    return new SizeF(792F, 612F);
            }
        }

        private void ApplyExcelActiveViewFormatting(IWorksheet sheet, GenesysGridExportSettings settings)
        {
            if (sheet == null || sheet.UsedRange == null || settings == null)
                return;

            int headerRow = settings.ExcelHeaderRowIndex;
            int firstColumn = sheet.UsedRange.Column;
            int lastColumn = sheet.UsedRange.LastColumn;
            int lastRow = sheet.UsedRange.LastRow;

            if (lastColumn < firstColumn || lastRow < headerRow)
                return;

            IRange headerRange = sheet.Range[headerRow, firstColumn, headerRow, lastColumn];
            ApplyExcelHeaderStyle(headerRange, settings);

            if (settings.ExcelFreezeHeader)
            {
                try
                {
                    sheet.Range[headerRow + 1, 1].FreezePanes();
                }
                catch
                {
                }
            }

            ApplyExcelColumnFormatsFromGrid(sheet, settings, headerRow, firstColumn, lastColumn, lastRow);
        }

        private void ApplyExcelHeaderStyle(IRange headerRange, GenesysGridExportSettings settings)
        {
            if (headerRange == null || settings == null)
                return;

            headerRange.CellStyle.Font.Bold = true;
            headerRange.CellStyle.Font.Color = ExcelKnownColors.Black;
            headerRange.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
            headerRange.CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;
            headerRange.CellStyle.Color = settings.HeaderBackColor;
            headerRange.CellStyle.WrapText = true;

            ApplyExcelBorders(headerRange);
        }

        private void ApplyExcelBorders(IRange range)
        {
            if (range == null)
                return;

            range.CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
            range.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
            range.CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
            range.CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
            range.CellStyle.Borders[ExcelBordersIndex.InsideHorizontal].LineStyle = ExcelLineStyle.Thin;
            range.CellStyle.Borders[ExcelBordersIndex.InsideVertical].LineStyle = ExcelLineStyle.Thin;
        }

        private void ApplyExcelColumnFormatsFromGrid(
            IWorksheet sheet,
            GenesysGridExportSettings settings,
            int headerRow,
            int firstColumn,
            int lastColumn,
            int lastRow)
        {
            if (grid == null || grid.Columns == null || lastRow <= headerRow)
                return;

            int exportColumn = firstColumn;

            foreach (var gridColumn in grid.Columns)
            {
                if (gridColumn == null || !gridColumn.Visible)
                    continue;

                if (settings != null && settings.ExcludedColumns != null && settings.ExcludedColumns.Contains(gridColumn.MappingName))
                    continue;

                if (exportColumn > lastColumn)
                    break;

                IRange dataRange = sheet.Range[headerRow + 1, exportColumn, lastRow, exportColumn];
                Type dataType = GetColumnDataType(gridColumn.MappingName);
                string numberFormat = GetExcelNumberFormatForColumn(gridColumn, dataType, settings);

                if (!string.IsNullOrWhiteSpace(numberFormat))
                    dataRange.NumberFormat = numberFormat;

                ApplyExcelHorizontalAlignment(dataRange, gridColumn, dataType, settings);

                exportColumn++;
            }
        }

        private string GetExcelNumberFormatForColumn(object gridColumn, Type dataType, GenesysGridExportSettings settings)
        {
            string formatFromGrid = GetExportColumnFormat(gridColumn, settings);
            string excelFormat = ConvertDotNetFormatToExcelFormat(formatFromGrid);

            if (!string.IsNullOrWhiteSpace(excelFormat))
                return excelFormat;

            dataType = Nullable.GetUnderlyingType(dataType) ?? dataType;

            if (dataType == typeof(decimal) || dataType == typeof(double) || dataType == typeof(float))
                return "#,##0.00";

            if (dataType == typeof(byte) || dataType == typeof(short) || dataType == typeof(int) || dataType == typeof(long))
                return "#,##0";

            if (dataType == typeof(DateTime))
                return "dd/mm/yyyy";

            return null;
        }

        private static string ConvertDotNetFormatToExcelFormat(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
                return null;

            string normalized = format.Trim();

            if (normalized.StartsWith("{0:") && normalized.EndsWith("}"))
                normalized = normalized.Substring(3, normalized.Length - 4);

            string upper = normalized.ToUpperInvariant();

            if (upper.StartsWith("C"))
                return "$#,##0" + GetDecimalZeros(upper, 2);

            if (upper.StartsWith("N"))
                return "#,##0" + GetDecimalZeros(upper, 2);

            if (upper.StartsWith("P"))
                return "0" + GetDecimalZeros(upper, 2) + "%";

            if (upper.StartsWith("D"))
                return "dd/mm/yyyy";

            if (upper.StartsWith("F"))
                return "0" + GetDecimalZeros(upper, 2);

            if (normalized.Contains("#") || normalized.Contains("0") || normalized.Contains("/") || normalized.Contains("%"))
                return normalized;

            return null;
        }

        private static string GetDecimalZeros(string format, int defaultDecimals)
        {
            int decimals = defaultDecimals;

            if (format.Length > 1)
            {
                int parsed;
                if (int.TryParse(format.Substring(1), out parsed))
                    decimals = parsed;
            }

            if (decimals <= 0)
                return string.Empty;

            return "." + new string('0', decimals);
        }
        private void ApplyExcelHorizontalAlignment(IRange dataRange, object gridColumn, Type dataType, GenesysGridExportSettings settings)
        {
            if (dataRange == null)
                return;

            string mappingName = GetColumnMappingName(gridColumn);
            string headerText = GetColumnHeaderText(gridColumn);
            GenesysGridColumnLayout layoutColumn = GetLayoutColumn(settings, mappingName, headerText);
            string viewAlignment = layoutColumn == null ? null : layoutColumn.Alignment;

            string effectiveAlignment = GenesysGridColumnVisualHelper.ResolveEffectiveAlignment(
                viewAlignment,
                gridColumn,
                dataType,
                GetExportColumnFormat(gridColumn, settings));

            dataRange.CellStyle.HorizontalAlignment = ToExcelHorizontalAlignment(effectiveAlignment);
        }

        private static ExcelHAlign ToExcelHorizontalAlignment(string alignment)
        {
            string normalized = GenesysGridColumnVisualHelper.NormalizeAlignment(alignment);

            if (normalized == GenesysGridColumnVisualHelper.AlignmentRight)
                return ExcelHAlign.HAlignRight;

            if (normalized == GenesysGridColumnVisualHelper.AlignmentCenter)
                return ExcelHAlign.HAlignCenter;

            return ExcelHAlign.HAlignLeft;
        }

        private Type GetColumnDataType(string mappingName)
        {
            if (string.IsNullOrWhiteSpace(mappingName) || grid == null || grid.View == null)
                return typeof(string);

            DataView dataView = grid.DataSource as DataView;
            if (dataView != null && dataView.Table != null && dataView.Table.Columns.Contains(mappingName))
                return dataView.Table.Columns[mappingName].DataType;

            DataTable dataTable = grid.DataSource as DataTable;
            if (dataTable != null && dataTable.Columns.Contains(mappingName))
                return dataTable.Columns[mappingName].DataType;

            return typeof(string);
        }

        private static string GetColumnFormatFromGrid(object gridColumn)
        {
            if (gridColumn == null)
                return null;

            foreach (string propertyName in new[] { "Format", "FormatString", "NumberFormat" })
            {
                var property = gridColumn.GetType().GetProperty(propertyName);

                if (property == null)
                    continue;

                object value = property.GetValue(gridColumn, null);
                if (value != null && !string.IsNullOrWhiteSpace(Convert.ToString(value)))
                    return Convert.ToString(value);
            }

            return null;
        }

        private IRange GetExcelExportedGridRange(IWorksheet sheet, GenesysGridExportSettings settings)
        {
            if (sheet == null || sheet.UsedRange == null || settings == null)
                return null;

            int headerRow = settings.ExcelHeaderRowIndex;
            int firstColumn = sheet.UsedRange.Column;
            int lastColumn = sheet.UsedRange.LastColumn;
            int lastGridRow = GetExcelLastGridRow(sheet);

            if (lastGridRow < headerRow)
                return null;

            return sheet.Range[headerRow, firstColumn, lastGridRow, lastColumn];
        }

        private int GetExcelLastGridRow(IWorksheet sheet)
        {
            if (sheet == null || sheet.UsedRange == null)
                return 0;

            return sheet.UsedRange.LastRow;
        }

        private void AppendExcelExportFooter(IWorksheet sheet, GenesysGridExportSettings settings)
        {
            if (sheet == null || sheet.UsedRange == null || settings == null)
                return;

            int row = sheet.UsedRange.LastRow + 2;
            int firstColumn = sheet.UsedRange.Column;

            sheet.Range[row, firstColumn].Text = "Reporte:";
            sheet.Range[row, firstColumn + 1].Text = settings.Title;

            row++;
            sheet.Range[row, firstColumn].Text = "Generado:";
            sheet.Range[row, firstColumn + 1].Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            row++;
            sheet.Range[row, firstColumn].Text = "Usuario:";
            sheet.Range[row, firstColumn + 1].Text = Environment.UserName;

            row++;
            sheet.Range[row, firstColumn].Text = "Vista exportada:";
            sheet.Range[row, firstColumn + 1].Text = "Vista activa del grid";

            row++;
            sheet.Range[row, firstColumn].Text = "Papel PDF sugerido:";
            sheet.Range[row, firstColumn + 1].Text = settings.PdfPaperMode.ToString();

            AppendExcelCustomFooter(sheet, settings, ref row);
        }

        /// <summary>
        /// Permite que formularios hijos agreguen información propia al final del Excel.
        /// No se escribe nada arriba del grid: el encabezado siempre permanece en la primera línea.
        /// </summary>
        private void AppendExcelCustomFooter(IWorksheet sheet, GenesysGridExportSettings settings, ref int row)
        {
        }

        private static void AddExcelExcludedColumn(ExcelExportingOptions options, string mappingName)
        {
            if (options == null || string.IsNullOrWhiteSpace(mappingName))
                return;

            if (!options.ExcludeColumns.Contains(mappingName))
                options.ExcludeColumns.Add(mappingName);
        }

        private static void AddPdfExcludedColumn(PdfExportingOptions options, string mappingName)
        {
            if (options == null || string.IsNullOrWhiteSpace(mappingName))
                return;

            if (!options.ExcludeColumns.Contains(mappingName))
                options.ExcludeColumns.Add(mappingName);
        }

        private void AddPdfFooter(PdfDocument document, GenesysGridExportSettings settings)
        {
            PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 8);
            var footer = new PdfPageTemplateElement(document.PageSettings.Width, settings.PdfFooterHeight);

            PdfPageNumberField pageNumber = new PdfPageNumberField(font, PdfBrushes.Black);
            PdfPageCountField pageCount = new PdfPageCountField(font, PdfBrushes.Black);
            PdfCompositeField pageInfo = new PdfCompositeField(
                font,
                PdfBrushes.Black,
                "Página {0} de {1}",
                pageNumber,
                pageCount);

            footer.Graphics.DrawString(
                settings.Title + " | Generado: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | Usuario: " + Environment.UserName,
                font,
                PdfBrushes.Black,
                0,
                6);

            pageInfo.Bounds = new RectangleF(
                0,
                6,
                document.PageSettings.Width,
                18);
            pageInfo.StringFormat = new PdfStringFormat(PdfTextAlignment.Right);
            pageInfo.Draw(footer.Graphics, new PointF(0, 6));

            document.Template.Bottom = footer;
        }

        private static string GetSafeWorksheetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = "Reporte";

            foreach (char invalidChar in new[] { ':', '\\', '/', '?', '*', '[', ']' })
                name = name.Replace(invalidChar, '_');

            if (name.Length > 31)
                name = name.Substring(0, 31);

            return name;
        }

        private GenesysExportFileDialogResult ShowExportFileDialog(
            string title,
            string extension,
            string defaultFileName,
            GenesysGridExportSettings settings)
        {
            string persistenceKey = BuildPersistenceKey(settings, extension);
            GenesysExportDialogState persisted = GenesysExportDialogStateStore.Load(persistenceKey);

            string fileName = Path.GetFileName(defaultFileName);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "Reporte." + extension;

            string folder = null;

            if (persisted != null && !string.IsNullOrWhiteSpace(persisted.LastFolder) && Directory.Exists(persisted.LastFolder))
                folder = persisted.LastFolder;

            if (string.IsNullOrWhiteSpace(folder))
                folder = settings.ExportDialogDefaultFolder;

            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                folder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            string defaultPath = Path.Combine(folder, fileName);

            bool openFile = persisted == null ? settings.OpenFileAfterExportDefault : persisted.OpenFileAfterExport;
            bool openFolder = persisted == null ? settings.OpenFolderAfterExportDefault : persisted.OpenFolderAfterExport;

            using (GenesysExportFileDialog dialog = new GenesysExportFileDialog(
                title,
                extension,
                defaultPath,
                openFile,
                openFolder))
            {
                if (dialog.ShowDialog(owner) != DialogResult.OK)
                    return null;

                GenesysExportFileDialogResult result = new GenesysExportFileDialogResult();
                result.Accepted = true;
                result.FilePath = dialog.FilePath;
                result.OpenFileAfterExport = dialog.OpenFileAfterExport;
                result.OpenFolderAfterExport = dialog.OpenFolderAfterExport;

                GenesysExportDialogState state = new GenesysExportDialogState();
                state.LastFolder = Path.GetDirectoryName(result.FilePath);
                state.OpenFileAfterExport = result.OpenFileAfterExport;
                state.OpenFolderAfterExport = result.OpenFolderAfterExport;
                GenesysExportDialogStateStore.Save(persistenceKey, state);

                return result;
            }
        }

        private string BuildPersistenceKey(GenesysGridExportSettings settings, string extension)
        {
            string baseKey = settings == null ? null : settings.ExportDialogPersistenceKey;

            if (string.IsNullOrWhiteSpace(baseKey))
                baseKey = owner == null ? "Genesys.Export" : owner.GetType().FullName;

            return baseKey + "." + extension.ToLowerInvariant();
        }

        private static void HandleAfterExport(GenesysExportFileDialogResult result)
        {
            if (result == null || string.IsNullOrWhiteSpace(result.FilePath))
                return;

            string folder = Path.GetDirectoryName(result.FilePath);

            if (result.OpenFolderAfterExport && Directory.Exists(folder))
            {
                try
                {
                    Process.Start("explorer.exe", "/select,\"" + result.FilePath + "\"");
                }
                catch
                {
                    try { Process.Start(folder); }
                    catch { }
                }
            }

            if (result.OpenFileAfterExport && File.Exists(result.FilePath))
            {
                try
                {
                    Process.Start(result.FilePath);
                }
                catch
                {
                }
            }
        }

        private static void SetPdfGridLayoutValue(PdfGridLayoutFormat layout, string propertyName, string enumName)
        {
            if (layout == null)
                return;

            var property = layout.GetType().GetProperty(propertyName);
            if (property == null || !property.CanWrite)
                return;

            try
            {
                object value = Enum.Parse(property.PropertyType, enumName);
                property.SetValue(layout, value, null);
            }
            catch
            {
            }
        }
    }

    internal sealed class GenesysExportFileDialogResult
    {
        public bool Accepted { get; set; }
        public string FilePath { get; set; }
        public bool OpenFolderAfterExport { get; set; }
        public bool OpenFileAfterExport { get; set; }
    }

    internal sealed class GenesysExportFileDialog : Form
    {
        private readonly string extension;
        private TextBox pathTextBox;
        private Button browseButton;
        private CheckBox openFolderCheckBox;
        private CheckBox openFileCheckBox;
        private Button acceptButton;
        private Button cancelButton;

        public GenesysExportFileDialog(
            string title,
            string extension,
            string defaultPath,
            bool openFileAfterExport,
            bool openFolderAfterExport)
        {
            this.extension = NormalizeExtension(extension);

            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Width = 680;
            Height = 205;
            Font = new Font("Segoe UI", 9F);

            BuildControls(defaultPath, openFileAfterExport, openFolderAfterExport);
        }

        public string FilePath
        {
            get { return pathTextBox == null ? string.Empty : pathTextBox.Text.Trim(); }
        }

        public bool OpenFolderAfterExport
        {
            get { return openFolderCheckBox != null && openFolderCheckBox.Checked; }
        }

        public bool OpenFileAfterExport
        {
            get { return openFileCheckBox != null && openFileCheckBox.Checked; }
        }

        private void BuildControls(string defaultPath, bool openFileAfterExport, bool openFolderAfterExport)
        {
            Label pathLabel = new Label
            {
                Text = "Path:",
                Left = 12,
                Top = 18,
                Width = 45,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pathTextBox = new TextBox
            {
                Left = 62,
                Top = 17,
                Width = 535,
                Height = 24,
                Text = defaultPath
            };

            browseButton = new Button
            {
                Text = "...",
                Left = 604,
                Top = 15,
                Width = 42,
                Height = 27
            };
            browseButton.Click += BrowseButton_Click;

            openFolderCheckBox = new CheckBox
            {
                Text = "Abrir/mostrar la carpeta al terminar",
                Left = 62,
                Top = 58,
                Width = 300,
                Height = 24,
                Checked = openFolderAfterExport
            };

            openFileCheckBox = new CheckBox
            {
                Text = "Abrir el archivo generado al terminar",
                Left = 62,
                Top = 86,
                Width = 300,
                Height = 24,
                Checked = openFileAfterExport
            };

            acceptButton = new Button
            {
                Text = "Exportar",
                Left = 464,
                Top = 124,
                Width = 86,
                Height = 30,
                DialogResult = DialogResult.OK
            };
            acceptButton.Click += AcceptButton_Click;

            cancelButton = new Button
            {
                Text = "Cancelar",
                Left = 560,
                Top = 124,
                Width = 86,
                Height = 30,
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(pathLabel);
            Controls.Add(pathTextBox);
            Controls.Add(browseButton);
            Controls.Add(openFolderCheckBox);
            Controls.Add(openFileCheckBox);
            Controls.Add(acceptButton);
            Controls.Add(cancelButton);

            AcceptButton = acceptButton;
            CancelButton = cancelButton;
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = Text;
                dialog.Filter = GetFilter(extension);
                dialog.DefaultExt = extension;
                dialog.AddExtension = true;
                dialog.OverwritePrompt = true;

                string currentPath = FilePath;
                if (!string.IsNullOrWhiteSpace(currentPath))
                {
                    string folder = Path.GetDirectoryName(currentPath);
                    if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
                        dialog.InitialDirectory = folder;

                    string fileName = Path.GetFileName(currentPath);
                    if (!string.IsNullOrWhiteSpace(fileName))
                        dialog.FileName = fileName;
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                    pathTextBox.Text = dialog.FileName;
            }
        }

        private void AcceptButton_Click(object sender, EventArgs e)
        {
            string path = FilePath;

            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show(this, "Capture el path del archivo a generar.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.None;
                return;
            }

            path = EnsureExtension(path, extension);
            pathTextBox.Text = path;

            string folder = Path.GetDirectoryName(path);

            if (string.IsNullOrWhiteSpace(folder))
            {
                MessageBox.Show(this, "El path no es válido.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.None;
                return;
            }

            if (!Directory.Exists(folder))
            {
                DialogResult create = MessageBox.Show(
                    this,
                    "La carpeta no existe. ¿Desea crearla?",
                    Text,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (create != DialogResult.Yes)
                {
                    DialogResult = DialogResult.None;
                    return;
                }

                try
                {
                    Directory.CreateDirectory(folder);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "No se pudo crear la carpeta.\r\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    DialogResult = DialogResult.None;
                    return;
                }
            }

            if (File.Exists(path))
            {
                DialogResult overwrite = MessageBox.Show(
                    this,
                    "El archivo ya existe. ¿Desea reemplazarlo?",
                    Text,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (overwrite != DialogResult.Yes)
                {
                    DialogResult = DialogResult.None;
                    return;
                }
            }
        }

        private static string NormalizeExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return string.Empty;

            extension = extension.Trim();
            if (extension.StartsWith("."))
                extension = extension.Substring(1);

            return extension.ToLowerInvariant();
        }

        private static string EnsureExtension(string path, string extension)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(extension))
                return path;

            string current = Path.GetExtension(path);
            if (string.Equals(current, "." + extension, StringComparison.OrdinalIgnoreCase))
                return path;

            if (string.IsNullOrWhiteSpace(current))
                return path + "." + extension;

            return Path.ChangeExtension(path, extension);
        }

        private static string GetFilter(string extension)
        {
            if (string.Equals(extension, "xlsx", StringComparison.OrdinalIgnoreCase))
                return "Archivo Excel (*.xlsx)|*.xlsx";

            if (string.Equals(extension, "pdf", StringComparison.OrdinalIgnoreCase))
                return "Archivo PDF (*.pdf)|*.pdf";

            return "Archivo (*." + extension + ")|*." + extension;
        }
    }

    internal sealed class GenesysExportDialogState
    {
        public string LastFolder { get; set; }
        public bool OpenFolderAfterExport { get; set; }
        public bool OpenFileAfterExport { get; set; }
    }

    internal static class GenesysExportDialogStateStore
    {
        private static readonly object syncRoot = new object();

        public static GenesysExportDialogState Load(string key)
        {
            try
            {
                string file = GetFileName(key);
                if (!File.Exists(file))
                    return null;

                string[] lines = File.ReadAllLines(file, Encoding.UTF8);
                GenesysExportDialogState state = new GenesysExportDialogState();

                foreach (string line in lines)
                {
                    if (line == null)
                        continue;

                    int index = line.IndexOf('=');
                    if (index <= 0)
                        continue;

                    string name = line.Substring(0, index).Trim();
                    string value = line.Substring(index + 1);

                    if (string.Equals(name, "LastFolder", StringComparison.OrdinalIgnoreCase))
                        state.LastFolder = value;
                    else if (string.Equals(name, "OpenFolderAfterExport", StringComparison.OrdinalIgnoreCase))
                        state.OpenFolderAfterExport = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "OpenFileAfterExport", StringComparison.OrdinalIgnoreCase))
                        state.OpenFileAfterExport = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                }

                return state;
            }
            catch
            {
                return null;
            }
        }

        public static void Save(string key, GenesysExportDialogState state)
        {
            if (state == null)
                return;

            try
            {
                lock (syncRoot)
                {
                    string file = GetFileName(key);
                    string folder = Path.GetDirectoryName(file);

                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    List<string> lines = new List<string>();
                    lines.Add("LastFolder=" + (state.LastFolder ?? string.Empty));
                    lines.Add("OpenFolderAfterExport=" + state.OpenFolderAfterExport.ToString().ToLowerInvariant());
                    lines.Add("OpenFileAfterExport=" + state.OpenFileAfterExport.ToString().ToLowerInvariant());

                    File.WriteAllLines(file, lines.ToArray(), Encoding.UTF8);
                }
            }
            catch
            {
            }
        }

        private static string GetFileName(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                key = "Genesys.Export";

            string safe = key;
            foreach (char invalid in Path.GetInvalidFileNameChars())
                safe = safe.Replace(invalid, '_');

            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Genesys",
                "Exportacion");

            return Path.Combine(folder, safe + ".cfg");
        }
    }
}
