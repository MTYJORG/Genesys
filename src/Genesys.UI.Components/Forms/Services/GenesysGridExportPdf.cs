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
    /// Responsabilidad PDF: exportación PdfGrid, documento PDF, handlers Syncfusion, encabezados, pies y resumen.
    /// </summary>
    internal sealed partial class GenesysGridExportService
    {
        private GenesysGridExportSettings PreparePdfExportSettings(GenesysGridExportSettings settings)
        {
            if (settings == null)
                settings = new GenesysGridExportSettings();

            ApplyCentralizedBranding(settings);

            // PDF también necesita conocer la Vista activa para resolver
            // Alignment = Automatic correctamente.
            // No se aplica ApplyAutomaticPdfPaper aquí para conservar el comportamiento
            // estándar de Syncfusion y cualquier configuración directa del formulario hijo
            // en settings.PdfOptions.
            ResolveCurrentViewLayout(settings);
            AddDefaultExcludedColumns(settings);
            ApplyPdfExcludedColumns(settings);
            ApplyPdfNativeOptions(settings);

            return settings;
        }

        private void ApplyPdfExcludedColumns(GenesysGridExportSettings settings)
        {
            if (settings == null || settings.ExcludedColumns == null)
                return;

            foreach (string mappingName in settings.ExcludedColumns)
                AddPdfExcludedColumn(settings.PdfOptions, mappingName);
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

        private PdfExportingOptions CreatePdfExportingOptions(GenesysGridExportSettings settings)
        {
            if (settings == null || settings.PdfOptions == null)
                return new PdfExportingOptions();

            ApplyPdfExcludedColumns(settings);
            ApplyPdfNativeOptions(settings);
            return settings.PdfOptions;
        }

        private static GenesysPdfScalingMode GetPdfScalingModeFromFlags(bool fitColumns, bool fitRows)
        {
            if (fitColumns && fitRows)
                return GenesysPdfScalingMode.FitSheetOnOnePage;

            if (fitColumns)
                return GenesysPdfScalingMode.FitAllColumnsOnOnePage;

            if (fitRows)
                return GenesysPdfScalingMode.FitAllRowsOnOnePage;

            return GenesysPdfScalingMode.None;
        }

        private static void ApplyPdfScalingModeToSettings(GenesysGridExportSettings settings)
        {
            if (settings == null)
                return;

            switch (settings.PdfScalingMode)
            {
                case GenesysPdfScalingMode.FitSheetOnOnePage:
                    settings.PdfFitAllColumnsInOnePage = true;
                    settings.PdfFitAllRowsInOnePage = true;
                    break;

                case GenesysPdfScalingMode.FitAllColumnsOnOnePage:
                    settings.PdfFitAllColumnsInOnePage = true;
                    settings.PdfFitAllRowsInOnePage = false;
                    break;

                case GenesysPdfScalingMode.FitAllRowsOnOnePage:
                    settings.PdfFitAllColumnsInOnePage = false;
                    settings.PdfFitAllRowsInOnePage = true;
                    break;

                default:
                    settings.PdfFitAllColumnsInOnePage = false;
                    settings.PdfFitAllRowsInOnePage = false;
                    break;
            }
        }

        private void ApplyPdfNativeOptions(GenesysGridExportSettings settings)
        {
            if (settings == null || settings.PdfOptions == null)
                return;

            ApplyPdfScalingModeToSettings(settings);

            // Primero propiedades nativas de Syncfusion.
            settings.PdfOptions.FitAllColumnsInOnePage = settings.PdfFitAllColumnsInOnePage;
            settings.PdfOptions.AutoColumnWidth = settings.PdfOptions.AutoColumnWidth;
            settings.PdfOptions.AutoRowHeight = settings.PdfAutoRowHeight;
            settings.PdfOptions.RepeatHeaders = settings.PdfRepeatHeaders;
            settings.PdfOptions.ExportFormat = settings.PdfOptions.ExportFormat;
            settings.PdfOptions.ExportStackedHeaders = settings.PdfOptions.ExportStackedHeaders;
            settings.PdfOptions.ExportUnboundRows = settings.PdfOptions.ExportUnboundRows;

            TrySetProperty(settings.PdfOptions, "ExportGroups", TryGetBoolProperty(settings.PdfOptions, "ExportGroups", true));
            TrySetProperty(settings.PdfOptions, "ExportGroupSummary", TryGetBoolProperty(settings.PdfOptions, "ExportGroupSummary", true));
            TrySetProperty(settings.PdfOptions, "ExportTableSummary", TryGetBoolProperty(settings.PdfOptions, "ExportTableSummary", true));

            // Si la versión de Syncfusion expone un ajuste nativo para filas en una página,
            // úsalo. Si no existe, se respeta la propiedad Genesys y se aplica al layout.
            TrySetProperty(settings.PdfOptions, "FitAllRowsInOnePage", settings.PdfFitAllRowsInOnePage);
            TrySetProperty(settings.PdfOptions, "FitAllRowsOnOnePage", settings.PdfFitAllRowsInOnePage);
            TrySetProperty(settings.PdfOptions, "FitToPage", settings.PdfFitAllRowsInOnePage);
        }

        private void ApplyPdfDocumentOutputSettings(PdfDocument document, GenesysGridExportSettings settings)
        {
            if (document == null || settings == null)
                return;

            document.PageSettings.Size = GetPdfPageSize(settings);
            document.PageSettings.Orientation = settings.PdfOrientation;
            document.PageSettings.Margins.All = settings.PdfMargins;

            document.DocumentInformation.Title = settings.Title;
            document.DocumentInformation.Subject = settings.Title;
            document.DocumentInformation.Author = Environment.UserName;
            document.DocumentInformation.Creator = settings.CompanyName;

            AddPdfHeader(document, settings);
            AddPdfFooter(document, settings);
        }

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
                    // Encabezados centrados. No se modifica fuente ni layout aquí
                    // para conservar el estilo estándar de Syncfusion/PdfOptions.
                    e.PdfGridCell.StringFormat = CreatePdfStringFormat(PdfTextAlignment.Center, true, settings);
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

                // Esta es la corrección clave: Automatic NO cae a Left.
                // Si la vista no define alineación explícita, ResolveEffectiveAlignment
                // decide por tipo de dato/formato: Numeric y DateTime a la derecha.
                e.PdfGridCell.StringFormat = CreatePdfStringFormat(alignment, true, settings);
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

            document.DocumentInformation.Title = settings.Title;
            document.DocumentInformation.Subject = settings.Title;
            document.DocumentInformation.Author = Environment.UserName;
            document.DocumentInformation.Creator = settings.CompanyName;

            AddPdfHeader(document, settings);
            AddPdfFooter(document, settings);
        }

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
            if (settings == null)
                return new SizeF(792F, 612F);

            return GetPageSize(settings.PdfPaperMode, settings.PdfOrientation, settings.PdfCustomLandscapeSize);
        }

        private static SizeF GetPageSize(GenesysPdfPaperMode paperMode, PdfPageOrientation orientation, SizeF customLandscapeSize)
        {
            SizeF basePortraitSize;

            switch (paperMode)
            {
                case GenesysPdfPaperMode.LegalLandscape:
                    basePortraitSize = new SizeF(612F, 1008F);   // Legal 8.5 x 14
                    break;

                case GenesysPdfPaperMode.OficioLandscape:
                    basePortraitSize = new SizeF(612F, 936F);    // Oficio 8.5 x 13
                    break;

                case GenesysPdfPaperMode.DoubleLetterLandscape:
                    basePortraitSize = new SizeF(792F, 1224F);   // Doble carta / Tabloid 11 x 17
                    break;

                case GenesysPdfPaperMode.TripleLetterLandscape:
                    basePortraitSize = new SizeF(612F, 2376F);   // Triple carta 8.5 x 33
                    break;

                case GenesysPdfPaperMode.A3Landscape:
                    basePortraitSize = new SizeF(842F, 1190F);   // A3 aproximado
                    break;

                case GenesysPdfPaperMode.CustomLandscape:
                    return NormalizeCustomPdfSize(customLandscapeSize, orientation);

                case GenesysPdfPaperMode.AutomaticByColumns:
                case GenesysPdfPaperMode.LetterLandscape:
                default:
                    basePortraitSize = new SizeF(612F, 792F);    // Carta 8.5 x 11
                    break;
            }

            if (orientation == PdfPageOrientation.Portrait)
                return basePortraitSize;

            return new SizeF(basePortraitSize.Height, basePortraitSize.Width);
        }

        private static SizeF NormalizeCustomPdfSize(SizeF customSize, PdfPageOrientation orientation)
        {
            if (customSize.Width <= 0F || customSize.Height <= 0F)
                customSize = new SizeF(792F, 612F);

            if (orientation == PdfPageOrientation.Portrait)
            {
                if (customSize.Width <= customSize.Height)
                    return customSize;

                return new SizeF(customSize.Height, customSize.Width);
            }

            if (customSize.Width >= customSize.Height)
                return customSize;

            return new SizeF(customSize.Height, customSize.Width);
        }

        private static void AddPdfExcludedColumn(PdfExportingOptions options, string mappingName)
        {
            if (options == null || string.IsNullOrWhiteSpace(mappingName))
                return;

            if (!options.ExcludeColumns.Contains(mappingName))
                options.ExcludeColumns.Add(mappingName);
        }

        private void AddPdfHeader(PdfDocument document, GenesysGridExportSettings settings)
        {
            if (document == null || settings == null || !settings.IncludeCorporateHeader)
                return;

            string company = string.IsNullOrWhiteSpace(settings.CompanyName)
                ? ResolveCentralCompanyName()
                : settings.CompanyName.Trim();

            string application = string.IsNullOrWhiteSpace(GenesysExportBranding.ApplicationName)
                ? string.Empty
                : GenesysExportBranding.ApplicationName.Trim();

            string title = string.IsNullOrWhiteSpace(settings.Title)
                ? "Reporte"
                : settings.Title.Trim();

            bool showApplication =
                !string.IsNullOrWhiteSpace(application) &&
                !string.Equals(application, company, StringComparison.OrdinalIgnoreCase);

            PdfFont companyFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10F, PdfFontStyle.Bold);
            PdfFont applicationFont = new PdfStandardFont(PdfFontFamily.Helvetica, 8F, PdfFontStyle.Regular);
            PdfFont titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 8F, PdfFontStyle.Bold);
            PdfFont viewFont = new PdfStandardFont(PdfFontFamily.Helvetica, 7F, PdfFontStyle.Regular);

            float headerHeight = showApplication ? 56F : 44F;
            var header = new PdfPageTemplateElement(document.PageSettings.Width, headerHeight);
            PdfStringFormat centered = new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle);

            float y = 4F;

            header.Graphics.DrawString(
                company,
                companyFont,
                PdfBrushes.Black,
                new RectangleF(0, y, document.PageSettings.Width, 12F),
                centered);

            y += 13F;

            if (showApplication)
            {
                header.Graphics.DrawString(
                    application,
                    applicationFont,
                    PdfBrushes.Gray,
                    new RectangleF(0, y, document.PageSettings.Width, 10F),
                    centered);

                y += 11F;
            }

            header.Graphics.DrawString(
                title,
                titleFont,
                PdfBrushes.Black,
                new RectangleF(0, y, document.PageSettings.Width, 11F),
                centered);

            y += 12F;

            header.Graphics.DrawString(
                "Vista: " + GetCurrentViewNameForDisplay(settings),
                viewFont,
                PdfBrushes.Gray,
                new RectangleF(0, y, document.PageSettings.Width, 10F),
                centered);

            PdfPen linePen = new PdfPen(PdfBrushes.LightGray, 0.4F);
            header.Graphics.DrawLine(linePen, 0, headerHeight - 3F, document.PageSettings.Width, headerHeight - 3F);

            document.Template.Top = header;
        }

        private void AddPdfFooter(PdfDocument document, GenesysGridExportSettings settings)
        {
            if (document == null || settings == null)
                return;

            PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 8);
            float footerHeight = Math.Max(settings.PdfFooterHeight, 22F);
            var footer = new PdfPageTemplateElement(document.PageSettings.Width, footerHeight);

            PdfPageNumberField pageNumber = new PdfPageNumberField(font, PdfBrushes.Black);
            PdfPageCountField pageCount = new PdfPageCountField(font, PdfBrushes.Black);
            PdfCompositeField pageInfo = new PdfCompositeField(
                font,
                PdfBrushes.Black,
                "Página {0} de {1}",
                pageNumber,
                pageCount);

            pageInfo.Bounds = new RectangleF(
                0,
                2F,
                document.PageSettings.Width,
                18F);
            pageInfo.StringFormat = new PdfStringFormat(PdfTextAlignment.Right);
            pageInfo.Draw(footer.Graphics, new PointF(0, 2F));

            document.Template.Bottom = footer;
        }

        private void AppendPdfDocumentSummary(PdfDocument document, GenesysGridExportSettings settings)
        {
            if (document == null || settings == null)
                return;

            if (!settings.IncludeGenerationInfo && !settings.IncludeFilterInfo && !settings.IncludeExportSummary)
                return;

            PdfPage page = document.Pages.Add();
            PdfGraphics graphics = page.Graphics;

            PdfFont titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12F, PdfFontStyle.Bold);
            PdfFont sectionFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9F, PdfFontStyle.Bold);
            PdfFont normalFont = new PdfStandardFont(PdfFontFamily.Helvetica, 8F, PdfFontStyle.Regular);

            float pageWidth = page.GetClientSize().Width;
            float y = 12F;
            float lineHeight = 12F;

            graphics.DrawString(
                "Resumen de exportación",
                titleFont,
                PdfBrushes.Black,
                new RectangleF(0, y, pageWidth, 16F),
                new PdfStringFormat(PdfTextAlignment.Center));
            y += 26F;

            if (settings.IncludeGenerationInfo)
            {
                DrawPdfSummarySectionTitle(graphics, sectionFont, "Datos de generación", ref y);
                DrawPdfSummaryLine(graphics, normalFont, "Generado: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ref y, lineHeight);
                DrawPdfSummaryLine(graphics, normalFont, "Usuario: " + Environment.UserName, ref y, lineHeight);
                y += 6F;
            }

            if (settings.IncludeFilterInfo)
            {
                DrawPdfSummarySectionTitle(graphics, sectionFont, "Filtros aplicados", ref y);

                foreach (string line in GetFilterInfoLinesForDisplay(settings))
                    DrawPdfSummaryLine(graphics, normalFont, line, ref y, lineHeight);

                y += 6F;
            }

            if (settings.IncludeExportSummary)
            {
                DrawPdfSummarySectionTitle(graphics, sectionFont, "Resumen", ref y);
                DrawPdfSummaryLine(graphics, normalFont, "Registros exportados: " + GetApproximateExportedRecordCount().ToString(), ref y, lineHeight);
                DrawPdfSummaryLine(graphics, normalFont, "Papel: " + GetPdfPaperWithOrientationDisplayName(settings), ref y, lineHeight);
                DrawPdfSummaryLine(graphics, normalFont, "Escalado: " + GetPdfScalingDisplayNameForInfo(settings.PdfScalingMode), ref y, lineHeight);
            }
        }

        private static void DrawPdfSummarySectionTitle(PdfGraphics graphics, PdfFont font, string text, ref float y)
        {
            if (graphics == null || font == null)
                return;

            graphics.DrawString(text ?? string.Empty, font, PdfBrushes.Black, 0, y);
            y += 14F;
        }

        private static void DrawPdfSummaryLine(PdfGraphics graphics, PdfFont font, string text, ref float y, float lineHeight)
        {
            if (graphics == null || font == null)
                return;

            graphics.DrawString(text ?? string.Empty, font, PdfBrushes.Black, 12F, y);
            y += lineHeight;
        }

        private static string GetPdfScalingDisplayNameForInfo(GenesysPdfScalingMode scalingMode)
        {
            return GetScalingDisplayNameForInfo(scalingMode);
        }

        private static string GetPdfPaperDisplayName(GenesysGridExportSettings settings)
        {
            return GetPaperDisplayName(settings);
        }

        private static bool IsPdfExtension(string extension)
        {
            return string.Equals(extension, "pdf", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase);
        }

        private static bool CanApplyUserPdfOption(GenesysGridExportSettings settings, GenesysExportOptionLevel optionLevel)
        {
            if (settings == null)
                return true;

            return settings.AllowUserOverridePdfOptions && optionLevel == GenesysExportOptionLevel.UserSelectable;
        }

        private void ApplyPersistedPdfDialogStateToSettings(
            string extension,
            GenesysGridExportSettings settings,
            GenesysExportDialogState persisted)
        {
            if (!IsPdfExtension(extension) || settings == null || persisted == null)
                return;

            GenesysPdfPaperMode paperMode;
            if (CanApplyUserPdfOption(settings, settings.PdfPaperModeOptionLevel) && TryParsePdfPaperMode(persisted.PdfPaperMode, out paperMode))
                settings.PdfPaperMode = paperMode;

            PdfPageOrientation orientation;
            if (CanApplyUserPdfOption(settings, settings.PdfOrientationOptionLevel) && TryParsePdfOrientation(persisted.PdfOrientation, out orientation))
                settings.PdfOrientation = orientation;

            GenesysPdfScalingMode scalingMode = GenesysPdfScalingMode.None;
            bool persistedScalingModeWasLoaded =
                CanApplyUserPdfOption(settings, settings.PdfScalingModeOptionLevel) &&
                TryParsePdfScalingMode(persisted.PdfScalingMode, out scalingMode);

            if (persistedScalingModeWasLoaded)
                settings.PdfScalingMode = scalingMode;

            if (CanApplyUserPdfOption(settings, settings.PdfFitAllColumnsOptionLevel) && persisted.PdfFitAllColumnsInOnePage.HasValue)
                settings.PdfFitAllColumnsInOnePage = persisted.PdfFitAllColumnsInOnePage.Value;

            if (CanApplyUserPdfOption(settings, settings.PdfFitAllRowsOptionLevel) && persisted.PdfFitAllRowsInOnePage.HasValue)
                settings.PdfFitAllRowsInOnePage = persisted.PdfFitAllRowsInOnePage.Value;

            if (CanApplyUserPdfOption(settings, settings.PdfAutoColumnWidthOptionLevel) && persisted.PdfAutoColumnWidth.HasValue)
                settings.PdfOptions.AutoColumnWidth = persisted.PdfAutoColumnWidth.Value;
            if (CanApplyUserPdfOption(settings, settings.PdfAutoRowHeightOptionLevel) && persisted.PdfAutoRowHeight.HasValue)
                settings.PdfAutoRowHeight = persisted.PdfAutoRowHeight.Value;
            if (CanApplyUserPdfOption(settings, settings.PdfRepeatHeadersOptionLevel) && persisted.PdfRepeatHeaders.HasValue)
                settings.PdfRepeatHeaders = persisted.PdfRepeatHeaders.Value;
            if (CanApplyUserPdfOption(settings, settings.PdfExportFormatOptionLevel) && persisted.PdfExportFormat.HasValue)
                settings.PdfOptions.ExportFormat = persisted.PdfExportFormat.Value;
            if (CanApplyUserPdfOption(settings, settings.PdfExportStackedHeadersOptionLevel) && persisted.PdfExportStackedHeaders.HasValue)
                settings.PdfOptions.ExportStackedHeaders = persisted.PdfExportStackedHeaders.Value;
            if (CanApplyUserPdfOption(settings, settings.PdfExportUnboundRowsOptionLevel) && persisted.PdfExportUnboundRows.HasValue)
                settings.PdfOptions.ExportUnboundRows = persisted.PdfExportUnboundRows.Value;

            if (CanApplyUserPdfOption(settings, settings.IncludeGenerationInfoOptionLevel) && persisted.IncludeGenerationInfo.HasValue)
                settings.IncludeGenerationInfo = persisted.IncludeGenerationInfo.Value;

            if (CanApplyUserPdfOption(settings, settings.IncludeFilterInfoOptionLevel) && persisted.IncludeFilterInfo.HasValue)
                settings.IncludeFilterInfo = persisted.IncludeFilterInfo.Value;

            if (CanApplyUserPdfOption(settings, settings.IncludeExportSummaryOptionLevel) && persisted.IncludeExportSummary.HasValue)
                settings.IncludeExportSummary = persisted.IncludeExportSummary.Value;
            if (CanApplyUserPdfOption(settings, settings.PdfExportGroupsOptionLevel) && persisted.PdfExportGroups.HasValue)
                TrySetProperty(settings.PdfOptions, "ExportGroups", persisted.PdfExportGroups.Value);
            if (CanApplyUserPdfOption(settings, settings.PdfExportGroupSummaryOptionLevel) && persisted.PdfExportGroupSummary.HasValue)
                TrySetProperty(settings.PdfOptions, "ExportGroupSummary", persisted.PdfExportGroupSummary.Value);
            if (CanApplyUserPdfOption(settings, settings.PdfExportTableSummaryOptionLevel) && persisted.PdfExportTableSummary.HasValue)
                TrySetProperty(settings.PdfOptions, "ExportTableSummary", persisted.PdfExportTableSummary.Value);
            if (CanApplyUserPdfOption(settings, settings.PdfApplyViewColumnWidthsOptionLevel) && persisted.PdfApplyViewColumnWidths.HasValue)
                settings.PdfApplyViewColumnWidths = persisted.PdfApplyViewColumnWidths.Value;
            if (CanApplyUserPdfOption(settings, settings.PdfAllowTextWrapOptionLevel) && persisted.PdfAllowTextWrap.HasValue)
                settings.PdfAllowTextWrap = persisted.PdfAllowTextWrap.Value;

            if (!persistedScalingModeWasLoaded)
                settings.PdfScalingMode = GetPdfScalingModeFromFlags(settings.PdfFitAllColumnsInOnePage, settings.PdfFitAllRowsInOnePage);

            ApplyPdfNativeOptions(settings);
        }

        private void ApplyPdfDialogOptionsToSettings(
            string extension,
            GenesysGridExportSettings settings,
            GenesysExportFileDialog dialog)
        {
            if (!IsPdfExtension(extension) || settings == null || dialog == null)
                return;

            settings.PdfPaperMode = dialog.SelectedPdfPaperMode;
            settings.PdfOrientation = dialog.SelectedPdfOrientation;
            settings.PdfScalingMode = dialog.SelectedPdfScalingMode;
            settings.PdfFitAllColumnsInOnePage = dialog.SelectedPdfFitAllColumnsInOnePage;
            settings.PdfFitAllRowsInOnePage = dialog.SelectedPdfFitAllRowsInOnePage;
            settings.PdfOptions.AutoColumnWidth = dialog.SelectedPdfAutoColumnWidth;
            settings.PdfAutoRowHeight = dialog.SelectedPdfAutoRowHeight;
            settings.PdfRepeatHeaders = dialog.SelectedPdfRepeatHeaders;
            settings.PdfOptions.ExportFormat = dialog.SelectedPdfExportFormat;
            settings.PdfOptions.ExportStackedHeaders = dialog.SelectedPdfExportStackedHeaders;
            settings.PdfOptions.ExportUnboundRows = dialog.SelectedPdfExportUnboundRows;
            settings.IncludeGenerationInfo = dialog.SelectedIncludeGenerationInfo;
            settings.IncludeFilterInfo = dialog.SelectedIncludeFilterInfo;
            settings.IncludeExportSummary = dialog.SelectedIncludeExportSummary;
            TrySetProperty(settings.PdfOptions, "ExportGroups", dialog.SelectedPdfExportGroups);
            TrySetProperty(settings.PdfOptions, "ExportGroupSummary", dialog.SelectedPdfExportGroupSummary);
            TrySetProperty(settings.PdfOptions, "ExportTableSummary", dialog.SelectedPdfExportTableSummary);
            settings.PdfApplyViewColumnWidths = dialog.SelectedPdfApplyViewColumnWidths;
            settings.PdfAllowTextWrap = dialog.SelectedPdfAllowTextWrap;

            ApplyPdfNativeOptions(settings);
        }

        private void CapturePdfDialogState(
            string extension,
            GenesysGridExportSettings settings,
            GenesysExportDialogState state)
        {
            if (!IsPdfExtension(extension) || settings == null || state == null)
                return;

            state.PdfPaperMode = settings.PdfPaperMode.ToString();
            state.PdfOrientation = settings.PdfOrientation.ToString();
            state.PdfScalingMode = settings.PdfScalingMode.ToString();
            state.PdfFitAllColumnsInOnePage = settings.PdfFitAllColumnsInOnePage;
            state.PdfFitAllRowsInOnePage = settings.PdfFitAllRowsInOnePage;
            state.PdfAutoColumnWidth = settings.PdfOptions.AutoColumnWidth;
            state.PdfAutoRowHeight = settings.PdfAutoRowHeight;
            state.PdfRepeatHeaders = settings.PdfRepeatHeaders;
            state.PdfExportFormat = settings.PdfOptions.ExportFormat;
            state.PdfExportStackedHeaders = settings.PdfOptions.ExportStackedHeaders;
            state.PdfExportUnboundRows = settings.PdfOptions.ExportUnboundRows;
            state.IncludeGenerationInfo = settings.IncludeGenerationInfo;
            state.IncludeFilterInfo = settings.IncludeFilterInfo;
            state.IncludeExportSummary = settings.IncludeExportSummary;
            state.PdfExportGroups = TryGetBoolProperty(settings.PdfOptions, "ExportGroups", true);
            state.PdfExportGroupSummary = TryGetBoolProperty(settings.PdfOptions, "ExportGroupSummary", true);
            state.PdfExportTableSummary = TryGetBoolProperty(settings.PdfOptions, "ExportTableSummary", true);
            state.PdfApplyViewColumnWidths = settings.PdfApplyViewColumnWidths;
            state.PdfAllowTextWrap = settings.PdfAllowTextWrap;
        }

        private static bool TryParsePdfPaperMode(string value, out GenesysPdfPaperMode paperMode)
        {
            paperMode = GenesysPdfPaperMode.AutomaticByColumns;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            try
            {
                paperMode = (GenesysPdfPaperMode)Enum.Parse(typeof(GenesysPdfPaperMode), value, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryParsePdfScalingMode(string value, out GenesysPdfScalingMode scalingMode)
        {
            scalingMode = GenesysPdfScalingMode.None;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            try
            {
                scalingMode = (GenesysPdfScalingMode)Enum.Parse(typeof(GenesysPdfScalingMode), value, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryParsePdfOrientation(string value, out PdfPageOrientation orientation)
        {
            orientation = PdfPageOrientation.Landscape;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            try
            {
                orientation = (PdfPageOrientation)Enum.Parse(typeof(PdfPageOrientation), value, true);
                return true;
            }
            catch
            {
                return false;
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
}
