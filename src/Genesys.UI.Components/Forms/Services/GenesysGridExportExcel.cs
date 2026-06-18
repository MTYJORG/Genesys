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
    /// Responsabilidad Excel: exportación XlsIO, formato, impresión, autofiltro y opciones del diálogo Excel.
    /// </summary>
    internal sealed partial class GenesysGridExportService
    {
        private GenesysGridExportSettings PrepareExportSettings(GenesysGridExportSettings settings)
        {
            if (settings == null)
                settings = new GenesysGridExportSettings();

            ApplyCentralizedBranding(settings);
            ResolveCurrentViewLayout(settings);
            AddDefaultExcludedColumns(settings);
            ApplyExcludedColumns(settings);
            ApplyAutomaticPdfPaper(settings);

            return settings;
        }

        private void ApplyExcelNativeOptions(GenesysGridExportSettings settings)
        {
            if (settings == null || settings.ExcelOptions == null)
                return;

            TrySetProperty(settings.ExcelOptions, "ExportMergedCells", settings.ExcelExportMergedCells);
        }

        private void ConfigureExcelWorkbook(IWorkbook workbook, GenesysGridExportSettings settings)
        {
            if (workbook == null || workbook.Worksheets.Count == 0 || settings == null)
                return;

            ApplyExcelWorkbookDefaults(workbook, settings);
            RenameExcelWorksheetsInSpanish(workbook, settings);

            IWorksheet sheet = workbook.Worksheets[0];

            ApplyExcelActiveViewFormatting(sheet, settings);
            ApplyExcelPageSetup(sheet, settings);
            ApplyExcelWorksheetViewOptions(sheet, settings);

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

        private void ApplyExcelWorksheetViewOptions(IWorksheet sheet, GenesysGridExportSettings settings)
        {
            if (sheet == null || settings == null)
                return;

            TrySetProperty(sheet, "IsGridLinesVisible", settings.ExcelShowGridLines);
            TrySetProperty(sheet, "DisplayGridlines", settings.ExcelShowGridLines);
            TrySetProperty(sheet, "ShowGridlines", settings.ExcelShowGridLines);
            TrySetProperty(sheet, "IsRowColumnHeadersVisible", settings.ExcelShowHeadings);
            TrySetProperty(sheet, "DisplayHeadings", settings.ExcelShowHeadings);
            TrySetProperty(sheet, "ShowHeadings", settings.ExcelShowHeadings);

            if (settings.ExcelProtectSheet)
                TryInvokeNoArgumentMethod(sheet, "Protect");
        }

        private void ApplyExcelPageSetup(IWorksheet sheet, GenesysGridExportSettings settings)
        {
            if (sheet == null || settings == null)
                return;

            object pageSetup = GetObjectPropertyValue(sheet, "PageSetup");
            if (pageSetup == null)
                return;

            TrySetEnumProperty(pageSetup, "Orientation", settings.ExcelOrientation == PdfPageOrientation.Portrait ? "Portrait" : "Landscape");
            TrySetEnumProperty(pageSetup, "PaperSize", GetExcelPaperSizeName(settings.ExcelPaperMode));

            if (settings.ExcelRepeatHeaderOnEachPage)
                TrySetProperty(pageSetup, "PrintTitleRows", "$" + settings.ExcelHeaderRowIndex.ToString() + ":$" + settings.ExcelHeaderRowIndex.ToString());

            TrySetProperty(pageSetup, "CenterHorizontally", settings.ExcelCenterHorizontally);
            TrySetProperty(pageSetup, "CenterVertically", settings.ExcelCenterVertically);
            TrySetProperty(pageSetup, "PrintGridlines", settings.ExcelPrintGridLines);
            TrySetProperty(pageSetup, "PrintGridLines", settings.ExcelPrintGridLines);

            // Márgenes básicos en pulgadas. Si la versión de XlsIO usa otros nombres, se ignoran.
            TrySetProperty(pageSetup, "LeftMargin", 0.25D);
            TrySetProperty(pageSetup, "RightMargin", 0.25D);
            TrySetProperty(pageSetup, "TopMargin", 0.50D);
            TrySetProperty(pageSetup, "BottomMargin", 0.50D);

            if (settings.ExcelScalingMode == GenesysPdfScalingMode.FitSheetOnOnePage)
            {
                TrySetProperty(pageSetup, "IsFitToPage", true);
                TrySetProperty(pageSetup, "FitToPagesWide", 1);
                TrySetProperty(pageSetup, "FitToPagesTall", 1);
                return;
            }

            if (settings.ExcelScalingMode == GenesysPdfScalingMode.FitAllColumnsOnOnePage)
            {
                TrySetProperty(pageSetup, "IsFitToPage", true);
                TrySetProperty(pageSetup, "FitToPagesWide", 1);
                TrySetProperty(pageSetup, "FitToPagesTall", 0);
                return;
            }

            if (settings.ExcelScalingMode == GenesysPdfScalingMode.FitAllRowsOnOnePage)
            {
                TrySetProperty(pageSetup, "IsFitToPage", true);
                TrySetProperty(pageSetup, "FitToPagesWide", 0);
                TrySetProperty(pageSetup, "FitToPagesTall", 1);
                return;
            }

            TrySetProperty(pageSetup, "IsFitToPage", false);
        }

        private static string GetExcelPaperSizeName(GenesysPdfPaperMode paperMode)
        {
            switch (paperMode)
            {
                case GenesysPdfPaperMode.LegalLandscape:
                    return "PaperLegal";

                case GenesysPdfPaperMode.OficioLandscape:
                    return "PaperLegal";

                case GenesysPdfPaperMode.DoubleLetterLandscape:
                    return "Paper11x17";

                case GenesysPdfPaperMode.TripleLetterLandscape:
                    return "Paper11x17";

                case GenesysPdfPaperMode.A3Landscape:
                    return "PaperA3";

                case GenesysPdfPaperMode.LetterLandscape:
                case GenesysPdfPaperMode.AutomaticByColumns:
                default:
                    return "PaperLetter";
            }
        }

        private static void TryInvokeNoArgumentMethod(object target, string methodName)
        {
            if (target == null || string.IsNullOrWhiteSpace(methodName))
                return;

            try
            {
                var method = target.GetType().GetMethod(methodName, Type.EmptyTypes);
                if (method != null)
                    method.Invoke(target, null);
            }
            catch
            {
            }
        }

        private static void TrySetEnumProperty(object target, string propertyName, string enumName)
        {
            if (target == null || string.IsNullOrWhiteSpace(propertyName) || string.IsNullOrWhiteSpace(enumName))
                return;

            try
            {
                System.Reflection.PropertyInfo property = target.GetType().GetProperty(propertyName);
                if (property == null || !property.CanWrite)
                    return;

                Type targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                if (!targetType.IsEnum)
                    return;

                object value = Enum.Parse(targetType, enumName, true);
                property.SetValue(target, value, null);
            }
            catch
            {
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

            if (settings.IncludeGenerationInfo)
            {
                AppendExcelFooterLine(sheet, ref row, firstColumn, "Generado:", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                AppendExcelFooterLine(sheet, ref row, firstColumn, "Usuario:", Environment.UserName);
            }

            if (settings.IncludeFilterInfo)
            {
                List<string> filterLines = GetFilterInfoLinesForDisplay(settings);

                if (filterLines.Count == 0)
                {
                    AppendExcelFooterLine(sheet, ref row, firstColumn, "Filtros:", "Sin filtros registrados");
                }
                else
                {
                    AppendExcelFooterLine(sheet, ref row, firstColumn, "Filtros:", filterLines[0]);

                    for (int index = 1; index < filterLines.Count; index++)
                        AppendExcelFooterLine(sheet, ref row, firstColumn, string.Empty, filterLines[index]);
                }
            }

            if (settings.IncludeExportSummary)
            {
                AppendExcelFooterLine(sheet, ref row, firstColumn, "Registros exportados:", GetApproximateExportedRecordCount().ToString());
                AppendExcelFooterLine(sheet, ref row, firstColumn, "Papel:", GetExcelPaperWithOrientationDisplayName(settings));
                AppendExcelFooterLine(sheet, ref row, firstColumn, "Escalado:", GetScalingDisplayNameForInfo(settings.ExcelScalingMode));
            }

            AppendExcelCustomFooter(sheet, settings, ref row);
        }

        private static void AppendExcelFooterLine(IWorksheet sheet, ref int row, int firstColumn, string label, string value)
        {
            if (sheet == null)
                return;

            sheet.Range[row, firstColumn].Text = label ?? string.Empty;
            sheet.Range[row, firstColumn + 1].Text = value ?? string.Empty;
            row++;
        }

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

        private string GetExcelPaperWithOrientationDisplayName(GenesysGridExportSettings settings)
        {
            if (settings == null)
                return "Carta";

            string paper = GetPaperDisplayName(settings.ExcelPaperMode);

            // La orientación mostrada debe reflejar la salida efectiva de Excel,
            // no la configuración PDF. Excel ya tiene papel/orientación propios.
            SizeF pageSize = GetPageSize(settings.ExcelPaperMode, settings.ExcelOrientation, settings.PdfCustomLandscapeSize);
            string orientation = pageSize.Width >= pageSize.Height
                ? "horizontal"
                : "vertical";

            if (string.IsNullOrWhiteSpace(paper))
                paper = "Carta";

            return paper + " " + orientation;
        }

        private static bool IsExcelExtension(string extension)
        {
            return string.Equals(extension, "xlsx", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyPersistedExcelDialogStateToSettings(string extension, GenesysGridExportSettings settings, GenesysExportDialogState persisted)
        {
            if (!IsExcelExtension(extension) || settings == null || persisted == null) return;
            GenesysPdfPaperMode paperMode; if (TryParsePdfPaperMode(persisted.ExcelPaperMode, out paperMode)) settings.ExcelPaperMode = paperMode;
            PdfPageOrientation orientation; if (TryParsePdfOrientation(persisted.ExcelOrientation, out orientation)) settings.ExcelOrientation = orientation;
            GenesysPdfScalingMode scalingMode; if (TryParsePdfScalingMode(persisted.ExcelScalingMode, out scalingMode)) settings.ExcelScalingMode = scalingMode;
            if (persisted.ExcelExportText.HasValue) settings.ExcelOptions.ExportMode = persisted.ExcelExportText.Value ? ExportMode.Text : ExportMode.Value;
            if (persisted.ExcelExportStackedHeaders.HasValue) settings.ExcelOptions.ExportStackedHeaders = persisted.ExcelExportStackedHeaders.Value;
            if (persisted.ExcelExportUnboundRows.HasValue) settings.ExcelOptions.ExportUnboundRows = persisted.ExcelExportUnboundRows.Value;
            if (persisted.ExcelAllowOutlining.HasValue) settings.ExcelOptions.AllowOutlining = persisted.ExcelAllowOutlining.Value;
            if (persisted.ExcelExportMergedCells.HasValue) settings.ExcelExportMergedCells = persisted.ExcelExportMergedCells.Value;
            if (persisted.ExcelFreezeHeader.HasValue) settings.ExcelFreezeHeader = persisted.ExcelFreezeHeader.Value;
            if (persisted.ExcelAutoFilter.HasValue) settings.ExcelAutoFilter = persisted.ExcelAutoFilter.Value;
            if (persisted.ExcelAutoFitColumns.HasValue) settings.ExcelAutoFitColumns = persisted.ExcelAutoFitColumns.Value;
            if (persisted.ExcelAppendFooter.HasValue) settings.ExcelAppendFooter = persisted.ExcelAppendFooter.Value;
            if (persisted.ExcelRepeatHeaderOnEachPage.HasValue) settings.ExcelRepeatHeaderOnEachPage = persisted.ExcelRepeatHeaderOnEachPage.Value;
            if (persisted.ExcelCenterHorizontally.HasValue) settings.ExcelCenterHorizontally = persisted.ExcelCenterHorizontally.Value;
            if (persisted.ExcelCenterVertically.HasValue) settings.ExcelCenterVertically = persisted.ExcelCenterVertically.Value;
            if (persisted.ExcelPrintGridLines.HasValue) settings.ExcelPrintGridLines = persisted.ExcelPrintGridLines.Value;
            if (persisted.ExcelShowGridLines.HasValue) settings.ExcelShowGridLines = persisted.ExcelShowGridLines.Value;
            if (persisted.ExcelShowHeadings.HasValue) settings.ExcelShowHeadings = persisted.ExcelShowHeadings.Value;
            if (persisted.ExcelProtectSheet.HasValue) settings.ExcelProtectSheet = persisted.ExcelProtectSheet.Value;
            TrySetProperty(settings.ExcelOptions, "ExportMergedCells", settings.ExcelExportMergedCells);
        }

        private void ApplyExcelDialogOptionsToSettings(string extension, GenesysGridExportSettings settings, GenesysExportFileDialog dialog)
        {
            if (!IsExcelExtension(extension) || settings == null || dialog == null) return;
            settings.ExcelPaperMode = dialog.SelectedExcelPaperMode; settings.ExcelOrientation = dialog.SelectedExcelOrientation; settings.ExcelScalingMode = dialog.SelectedExcelScalingMode;
            settings.ExcelOptions.ExportMode = dialog.SelectedExcelExportText ? ExportMode.Text : ExportMode.Value;
            settings.ExcelOptions.ExportStackedHeaders = dialog.SelectedExcelExportStackedHeaders; settings.ExcelOptions.ExportUnboundRows = dialog.SelectedExcelExportUnboundRows; settings.ExcelOptions.AllowOutlining = dialog.SelectedExcelAllowOutlining;
            settings.ExcelExportMergedCells = dialog.SelectedExcelExportMergedCells; settings.ExcelFreezeHeader = dialog.SelectedExcelFreezeHeader; settings.ExcelAutoFilter = dialog.SelectedExcelAutoFilter; settings.ExcelAutoFitColumns = dialog.SelectedExcelAutoFitColumns; settings.ExcelAppendFooter = dialog.SelectedExcelAppendFooter;
            settings.ExcelRepeatHeaderOnEachPage = dialog.SelectedExcelRepeatHeaderOnEachPage; settings.ExcelCenterHorizontally = dialog.SelectedExcelCenterHorizontally; settings.ExcelCenterVertically = dialog.SelectedExcelCenterVertically; settings.ExcelPrintGridLines = dialog.SelectedExcelPrintGridLines; settings.ExcelShowGridLines = dialog.SelectedExcelShowGridLines; settings.ExcelShowHeadings = dialog.SelectedExcelShowHeadings; settings.ExcelProtectSheet = dialog.SelectedExcelProtectSheet;
            TrySetProperty(settings.ExcelOptions, "ExportMergedCells", settings.ExcelExportMergedCells);
        }

        private void CaptureExcelDialogState(string extension, GenesysGridExportSettings settings, GenesysExportDialogState state)
        {
            if (!IsExcelExtension(extension) || settings == null || state == null) return;
            state.ExcelPaperMode = settings.ExcelPaperMode.ToString(); state.ExcelOrientation = settings.ExcelOrientation.ToString(); state.ExcelScalingMode = settings.ExcelScalingMode.ToString(); state.ExcelExportText = settings.ExcelOptions.ExportMode == ExportMode.Text;
            state.ExcelExportStackedHeaders = settings.ExcelOptions.ExportStackedHeaders; state.ExcelExportUnboundRows = settings.ExcelOptions.ExportUnboundRows; state.ExcelAllowOutlining = settings.ExcelOptions.AllowOutlining; state.ExcelExportMergedCells = settings.ExcelExportMergedCells;
            state.ExcelFreezeHeader = settings.ExcelFreezeHeader; state.ExcelAutoFilter = settings.ExcelAutoFilter; state.ExcelAutoFitColumns = settings.ExcelAutoFitColumns; state.ExcelAppendFooter = settings.ExcelAppendFooter; state.ExcelRepeatHeaderOnEachPage = settings.ExcelRepeatHeaderOnEachPage; state.ExcelCenterHorizontally = settings.ExcelCenterHorizontally; state.ExcelCenterVertically = settings.ExcelCenterVertically; state.ExcelPrintGridLines = settings.ExcelPrintGridLines; state.ExcelShowGridLines = settings.ExcelShowGridLines; state.ExcelShowHeadings = settings.ExcelShowHeadings; state.ExcelProtectSheet = settings.ExcelProtectSheet;
        }

    }
}
