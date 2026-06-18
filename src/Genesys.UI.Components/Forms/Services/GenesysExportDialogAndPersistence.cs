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
        private readonly bool isPdf;
        private TextBox pathTextBox;
        private Button browseButton;
        private CheckBox openFolderCheckBox;
        private CheckBox openFileCheckBox;
        private Label exportInfoLabel;
        private ComboBox pdfPaperComboBox;
        private ComboBox pdfOrientationComboBox;
        private CheckBox pdfAutoRowHeightCheckBox;
        private ComboBox pdfScalingModeComboBox;
        private ComboBox pdfColumnWidthModeComboBox;
        private CheckBox pdfRepeatHeadersCheckBox;
        private CheckBox pdfExportFormatCheckBox;
        private CheckBox pdfExportStackedHeadersCheckBox;
        private CheckBox pdfIncludeGenerationInfoCheckBox;
        private CheckBox pdfIncludeFilterInfoCheckBox;
        private CheckBox pdfIncludeExportSummaryCheckBox;
        private CheckBox pdfExportGroupsCheckBox;
        private CheckBox pdfExportGroupSummaryCheckBox;
        private CheckBox pdfExportTableSummaryCheckBox;
        private CheckBox pdfAllowTextWrapCheckBox;
        private ComboBox excelPaperComboBox;
        private ComboBox excelOrientationComboBox;
        private ComboBox excelScalingModeComboBox;
        private CheckBox excelExportTextCheckBox;
        private CheckBox excelExportStackedHeadersCheckBox;
        private CheckBox excelExportUnboundRowsCheckBox;
        private CheckBox excelAllowOutliningCheckBox;
        private CheckBox excelExportMergedCellsCheckBox;
        private CheckBox excelFreezeHeaderCheckBox;
        private CheckBox excelAutoFilterCheckBox;
        private CheckBox excelAutoFitColumnsCheckBox;
        private CheckBox excelAppendFooterCheckBox;
        private CheckBox excelRepeatHeaderCheckBox;
        private CheckBox excelCenterHorizontallyCheckBox;
        private CheckBox excelCenterVerticallyCheckBox;
        private CheckBox excelPrintGridLinesCheckBox;
        private CheckBox excelShowGridLinesCheckBox;
        private CheckBox excelShowHeadingsCheckBox;
        private CheckBox excelProtectSheetCheckBox;
        private Button acceptButton;
        private Button cancelButton;

        public GenesysExportFileDialog(
            string title,
            string extension,
            string defaultPath,
            bool openFileAfterExport,
            bool openFolderAfterExport,
            string exportInfo,
            GenesysGridExportSettings settings)
        {
            this.extension = NormalizeExtension(extension);
            isPdf = string.Equals(this.extension, "pdf", StringComparison.OrdinalIgnoreCase);

            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Width = 760;
            Height = isPdf ? 650 : 535;
            Font = new Font("Segoe UI", 9F);

            BuildControls(defaultPath, openFileAfterExport, openFolderAfterExport, exportInfo, settings);
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

        public GenesysPdfPaperMode SelectedPdfPaperMode
        {
            get { return GetSelectedPdfPaperMode(); }
        }

        public PdfPageOrientation SelectedPdfOrientation
        {
            get
            {
                string value = pdfOrientationComboBox == null ? null : Convert.ToString(pdfOrientationComboBox.SelectedItem);

                if (string.Equals(value, "Vertical", StringComparison.OrdinalIgnoreCase))
                    return PdfPageOrientation.Portrait;

                return PdfPageOrientation.Landscape;
            }
        }

        public GenesysPdfScalingMode SelectedPdfScalingMode
        {
            get { return GetSelectedPdfScalingMode(); }
        }

        public bool SelectedPdfFitAllColumnsInOnePage
        {
            get
            {
                GenesysPdfScalingMode mode = SelectedPdfScalingMode;
                return mode == GenesysPdfScalingMode.FitSheetOnOnePage ||
                       mode == GenesysPdfScalingMode.FitAllColumnsOnOnePage;
            }
        }

        public bool SelectedPdfFitAllRowsInOnePage
        {
            get
            {
                GenesysPdfScalingMode mode = SelectedPdfScalingMode;
                return mode == GenesysPdfScalingMode.FitSheetOnOnePage ||
                       mode == GenesysPdfScalingMode.FitAllRowsOnOnePage;
            }
        }

        public bool SelectedPdfAutoColumnWidth
        {
            get { return GetSelectedPdfColumnWidthMode() == GenesysPdfColumnWidthMode.AutoColumnWidth; }
        }

        public bool SelectedPdfAutoRowHeight
        {
            get { return pdfAutoRowHeightCheckBox != null && pdfAutoRowHeightCheckBox.Checked; }
        }

        public bool SelectedPdfRepeatHeaders
        {
            get { return pdfRepeatHeadersCheckBox != null && pdfRepeatHeadersCheckBox.Checked; }
        }

        public bool SelectedPdfExportFormat
        {
            get { return pdfExportFormatCheckBox != null && pdfExportFormatCheckBox.Checked; }
        }

        public bool SelectedPdfExportStackedHeaders
        {
            get { return pdfExportStackedHeadersCheckBox != null && pdfExportStackedHeadersCheckBox.Checked; }
        }

        public bool SelectedIncludeGenerationInfo
        {
            get { return pdfIncludeGenerationInfoCheckBox != null && pdfIncludeGenerationInfoCheckBox.Checked; }
        }

        public bool SelectedIncludeFilterInfo
        {
            get { return pdfIncludeFilterInfoCheckBox != null && pdfIncludeFilterInfoCheckBox.Checked; }
        }

        public bool SelectedIncludeExportSummary
        {
            get { return pdfIncludeExportSummaryCheckBox != null && pdfIncludeExportSummaryCheckBox.Checked; }
        }

        public bool SelectedPdfExportUnboundRows
        {
            get { return false; }
        }

        public bool SelectedPdfExportGroups
        {
            get { return pdfExportGroupsCheckBox != null && pdfExportGroupsCheckBox.Checked; }
        }

        public bool SelectedPdfExportGroupSummary
        {
            get { return pdfExportGroupSummaryCheckBox != null && pdfExportGroupSummaryCheckBox.Checked; }
        }

        public bool SelectedPdfExportTableSummary
        {
            get { return pdfExportTableSummaryCheckBox != null && pdfExportTableSummaryCheckBox.Checked; }
        }

        public bool SelectedPdfApplyViewColumnWidths
        {
            get { return GetSelectedPdfColumnWidthMode() == GenesysPdfColumnWidthMode.UseViewWidths; }
        }

        public bool SelectedPdfAllowTextWrap
        {
            get { return pdfAllowTextWrapCheckBox != null && pdfAllowTextWrapCheckBox.Checked; }
        }

        public GenesysPdfPaperMode SelectedExcelPaperMode { get { return GetSelectedExcelPaperMode(); } }

        public PdfPageOrientation SelectedExcelOrientation
        {
            get
            {
                string value = excelOrientationComboBox == null ? null : Convert.ToString(excelOrientationComboBox.SelectedItem);
                return string.Equals(value, "Vertical", StringComparison.OrdinalIgnoreCase) ? PdfPageOrientation.Portrait : PdfPageOrientation.Landscape;
            }
        }

        public GenesysPdfScalingMode SelectedExcelScalingMode { get { return GetSelectedExcelScalingMode(); } }
        public bool SelectedExcelExportText { get { return excelExportTextCheckBox != null && excelExportTextCheckBox.Checked; } }
        public bool SelectedExcelExportStackedHeaders { get { return excelExportStackedHeadersCheckBox != null && excelExportStackedHeadersCheckBox.Checked; } }
        public bool SelectedExcelExportUnboundRows { get { return excelExportUnboundRowsCheckBox != null && excelExportUnboundRowsCheckBox.Checked; } }
        public bool SelectedExcelAllowOutlining { get { return excelAllowOutliningCheckBox != null && excelAllowOutliningCheckBox.Checked; } }
        public bool SelectedExcelExportMergedCells { get { return excelExportMergedCellsCheckBox != null && excelExportMergedCellsCheckBox.Checked; } }
        public bool SelectedExcelFreezeHeader { get { return excelFreezeHeaderCheckBox != null && excelFreezeHeaderCheckBox.Checked; } }
        public bool SelectedExcelAutoFilter { get { return excelAutoFilterCheckBox != null && excelAutoFilterCheckBox.Checked; } }
        public bool SelectedExcelAutoFitColumns { get { return excelAutoFitColumnsCheckBox != null && excelAutoFitColumnsCheckBox.Checked; } }
        public bool SelectedExcelAppendFooter { get { return excelAppendFooterCheckBox != null && excelAppendFooterCheckBox.Checked; } }
        public bool SelectedExcelRepeatHeaderOnEachPage { get { return excelRepeatHeaderCheckBox != null && excelRepeatHeaderCheckBox.Checked; } }
        public bool SelectedExcelCenterHorizontally { get { return excelCenterHorizontallyCheckBox != null && excelCenterHorizontallyCheckBox.Checked; } }
        public bool SelectedExcelCenterVertically { get { return excelCenterVerticallyCheckBox != null && excelCenterVerticallyCheckBox.Checked; } }
        public bool SelectedExcelPrintGridLines { get { return excelPrintGridLinesCheckBox != null && excelPrintGridLinesCheckBox.Checked; } }
        public bool SelectedExcelShowGridLines { get { return excelShowGridLinesCheckBox != null && excelShowGridLinesCheckBox.Checked; } }
        public bool SelectedExcelShowHeadings { get { return excelShowHeadingsCheckBox != null && excelShowHeadingsCheckBox.Checked; } }
        public bool SelectedExcelProtectSheet { get { return excelProtectSheetCheckBox != null && excelProtectSheetCheckBox.Checked; } }

        private void BuildControls(
            string defaultPath,
            bool openFileAfterExport,
            bool openFolderAfterExport,
            string exportInfo,
            GenesysGridExportSettings settings)
        {
            Label pathLabel = new Label
            {
                Text = "Nombre de archivo:",
                Left = 16,
                Top = 18,
                Width = 120,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pathTextBox = new TextBox
            {
                Left = 140,
                Top = 17,
                Width = 545,
                Height = 24,
                Text = defaultPath
            };

            browseButton = new Button
            {
                Text = "...",
                Left = 692,
                Top = 15,
                Width = 42,
                Height = 27
            };
            browseButton.Click += BrowseButton_Click;

            int optionsTop = 58;

            if (isPdf)
            {
                GroupBox pdfGroupBox = new GroupBox
                {
                    Text = settings != null && !settings.AllowUserOverridePdfOptions
                        ? "Opciones de salida PDF (fijadas por el sistema)"
                        : "Opciones de salida PDF",
                    Left = 16,
                    Top = 52,
                    Width = 718,
                    Height = 420
                };

                GroupBox pageGroupBox = new GroupBox
                {
                    Text = "Configuración de página",
                    Left = 16,
                    Top = 26,
                    Width = 686,
                    Height = 78
                };

                Label paperLabel = new Label
                {
                    Text = "Papel:",
                    Left = 18,
                    Top = 33,
                    Width = 80,
                    Height = 22,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                pdfPaperComboBox = new ComboBox
                {
                    Left = 105,
                    Top = 30,
                    Width = 205,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                AddPaperOptions(pdfPaperComboBox);
                SelectPdfPaperOption(settings == null ? GenesysPdfPaperMode.AutomaticByColumns : settings.PdfPaperMode);
                pdfPaperComboBox.Enabled = IsPdfOptionEditable(settings, settings == null ? GenesysExportOptionLevel.UserSelectable : settings.PdfPaperModeOptionLevel);

                Label orientationLabel = new Label
                {
                    Text = "Orientación:",
                    Left = 355,
                    Top = 33,
                    Width = 90,
                    Height = 22,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                pdfOrientationComboBox = new ComboBox
                {
                    Left = 448,
                    Top = 30,
                    Width = 155,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                pdfOrientationComboBox.Items.Add("Horizontal");
                pdfOrientationComboBox.Items.Add("Vertical");
                pdfOrientationComboBox.SelectedItem = settings != null && settings.PdfOrientation == PdfPageOrientation.Portrait
                    ? "Vertical"
                    : "Horizontal";
                pdfOrientationComboBox.Enabled = IsPdfOptionEditable(settings, settings == null ? GenesysExportOptionLevel.UserSelectable : settings.PdfOrientationOptionLevel);

                pageGroupBox.Controls.Add(paperLabel);
                pageGroupBox.Controls.Add(pdfPaperComboBox);
                pageGroupBox.Controls.Add(orientationLabel);
                pageGroupBox.Controls.Add(pdfOrientationComboBox);

                GroupBox fitGroupBox = new GroupBox
                {
                    Text = "Ajuste del contenido",
                    Left = 16,
                    Top = 112,
                    Width = 686,
                    Height = 112
                };

                Label columnWidthModeLabel = new Label
                {
                    Text = "Ancho de columnas:",
                    Left = 18,
                    Top = 31,
                    Width = 125,
                    Height = 22,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                bool columnWidthModeEnabled = IsPdfColumnWidthModeEditable(settings);

                pdfColumnWidthModeComboBox = new ComboBox
                {
                    Left = 150,
                    Top = 28,
                    Width = 265,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Enabled = columnWidthModeEnabled
                };
                AddPdfColumnWidthModeOptions(pdfColumnWidthModeComboBox);
                SelectPdfColumnWidthMode(settings);

                Label scalingLabel = new Label
                {
                    Text = "Modo de ajuste:",
                    Left = 18,
                    Top = 65,
                    Width = 125,
                    Height = 22,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                pdfScalingModeComboBox = new ComboBox
                {
                    Left = 150,
                    Top = 62,
                    Width = 265,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                AddScalingOptions(pdfScalingModeComboBox);
                SelectPdfScalingOption(settings == null ? GenesysPdfScalingMode.None : settings.PdfScalingMode);
                pdfScalingModeComboBox.Enabled = IsPdfOptionEditable(settings, settings == null ? GenesysExportOptionLevel.UserSelectable : settings.PdfScalingModeOptionLevel);

                pdfAutoRowHeightCheckBox = CreatePdfCheckBox("Auto alto de filas", 455, 28, 190,
                    settings == null || settings.PdfAutoRowHeight,
                    IsPdfOptionEditable(settings, settings == null ? GenesysExportOptionLevel.UserSelectable : settings.PdfAutoRowHeightOptionLevel));

                pdfAllowTextWrapCheckBox = CreatePdfCheckBox("Texto en varias líneas", 455, 62, 205,
                    settings == null || settings.PdfAllowTextWrap,
                    IsPdfOptionEditable(settings, settings == null ? GenesysExportOptionLevel.UserSelectable : settings.PdfAllowTextWrapOptionLevel));

                fitGroupBox.Controls.Add(columnWidthModeLabel);
                fitGroupBox.Controls.Add(pdfColumnWidthModeComboBox);
                fitGroupBox.Controls.Add(scalingLabel);
                fitGroupBox.Controls.Add(pdfScalingModeComboBox);
                fitGroupBox.Controls.Add(pdfAutoRowHeightCheckBox);
                fitGroupBox.Controls.Add(pdfAllowTextWrapCheckBox);

                GroupBox contentGroupBox = new GroupBox
                {
                    Text = "Contenido a exportar",
                    Left = 16,
                    Top = 232,
                    Width = 336,
                    Height = 170
                };

                pdfRepeatHeadersCheckBox = CreatePdfCheckBox("Repetir encabezados", 18, 28, 260,
                    settings == null || settings.PdfRepeatHeaders,
                    IsPdfOptionEditable(settings, settings == null ? GenesysExportOptionLevel.UserSelectable : settings.PdfRepeatHeadersOptionLevel));

                pdfExportFormatCheckBox = CreatePdfCheckBox("Conservar formato visual", 18, 56, 260,
                    settings == null || settings.PdfOptions == null || settings.PdfOptions.ExportFormat,
                    IsPdfOptionEditable(settings, settings == null ? GenesysExportOptionLevel.UserSelectable : settings.PdfExportFormatOptionLevel));

                pdfExportStackedHeadersCheckBox = CreatePdfCheckBox("Encabezados agrupados", 18, 84, 260,
                    settings == null || settings.PdfOptions == null || settings.PdfOptions.ExportStackedHeaders,
                    IsPdfOptionEditable(settings, settings == null ? GenesysExportOptionLevel.UserSelectable : settings.PdfExportStackedHeadersOptionLevel));

                pdfExportGroupsCheckBox = CreatePdfCheckBox("Grupos", 18, 112, 130,
                    settings == null || settings.PdfOptions == null || TryGetBoolProperty(settings.PdfOptions, "ExportGroups", true),
                    IsPdfOptionEditable(settings, settings == null ? GenesysExportOptionLevel.UserSelectable : settings.PdfExportGroupsOptionLevel));

                pdfExportGroupSummaryCheckBox = CreatePdfCheckBox("Resúmenes de grupo", 150, 112, 170,
                    settings == null || settings.PdfOptions == null || TryGetBoolProperty(settings.PdfOptions, "ExportGroupSummary", true),
                    IsPdfOptionEditable(settings, settings == null ? GenesysExportOptionLevel.UserSelectable : settings.PdfExportGroupSummaryOptionLevel));

                pdfExportTableSummaryCheckBox = CreatePdfCheckBox("Resumen general", 18, 140, 260,
                    settings == null || settings.PdfOptions == null || TryGetBoolProperty(settings.PdfOptions, "ExportTableSummary", true),
                    IsPdfOptionEditable(settings, settings == null ? GenesysExportOptionLevel.UserSelectable : settings.PdfExportTableSummaryOptionLevel));

                contentGroupBox.Controls.Add(pdfRepeatHeadersCheckBox);
                contentGroupBox.Controls.Add(pdfExportFormatCheckBox);
                contentGroupBox.Controls.Add(pdfExportStackedHeadersCheckBox);
                contentGroupBox.Controls.Add(pdfExportGroupsCheckBox);
                contentGroupBox.Controls.Add(pdfExportGroupSummaryCheckBox);
                contentGroupBox.Controls.Add(pdfExportTableSummaryCheckBox);

                GroupBox infoGroupBox = new GroupBox
                {
                    Text = "Información adicional",
                    Left = 366,
                    Top = 232,
                    Width = 336,
                    Height = 170
                };

                pdfIncludeFilterInfoCheckBox = CreatePdfCheckBox("Filtros aplicados", 18, 28, 260,
                    settings == null || settings.IncludeFilterInfo,
                    IsPdfOptionEditable(settings, settings == null ? GenesysExportOptionLevel.UserSelectable : settings.IncludeFilterInfoOptionLevel));

                pdfIncludeGenerationInfoCheckBox = CreatePdfCheckBox("Datos de generación", 18, 56, 260,
                    settings == null || settings.IncludeGenerationInfo,
                    IsPdfOptionEditable(settings, settings == null ? GenesysExportOptionLevel.UserSelectable : settings.IncludeGenerationInfoOptionLevel));

                pdfIncludeExportSummaryCheckBox = CreatePdfCheckBox("Resumen final", 18, 84, 260,
                    settings != null && settings.IncludeExportSummary,
                    IsPdfOptionEditable(settings, settings == null ? GenesysExportOptionLevel.UserSelectable : settings.IncludeExportSummaryOptionLevel));

                infoGroupBox.Controls.Add(pdfIncludeFilterInfoCheckBox);
                infoGroupBox.Controls.Add(pdfIncludeGenerationInfoCheckBox);
                infoGroupBox.Controls.Add(pdfIncludeExportSummaryCheckBox);

                RegisterPdfOptionChangedHandlers();

                pdfGroupBox.Controls.Add(pageGroupBox);
                pdfGroupBox.Controls.Add(fitGroupBox);
                pdfGroupBox.Controls.Add(contentGroupBox);
                pdfGroupBox.Controls.Add(infoGroupBox);

                Controls.Add(pdfGroupBox);
                optionsTop = 486;
            }
            else
            {
                GroupBox excelGroupBox = new GroupBox { Text = "Opciones de salida Excel", Left = 16, Top = 52, Width = 718, Height = 300 };
                GroupBox pageGroupBox = new GroupBox { Text = "Configuración de página", Left = 16, Top = 26, Width = 686, Height = 78 };
                Label paperLabel = new Label { Text = "Papel:", Left = 18, Top = 33, Width = 80, Height = 22, TextAlign = ContentAlignment.MiddleLeft };
                excelPaperComboBox = new ComboBox { Left = 105, Top = 30, Width = 205, DropDownStyle = ComboBoxStyle.DropDownList };
                AddPaperOptions(excelPaperComboBox);
                SelectExcelPaperOption(settings == null ? GenesysPdfPaperMode.AutomaticByColumns : settings.ExcelPaperMode);
                Label orientationLabel = new Label { Text = "Orientación:", Left = 355, Top = 33, Width = 90, Height = 22, TextAlign = ContentAlignment.MiddleLeft };
                excelOrientationComboBox = new ComboBox { Left = 448, Top = 30, Width = 155, DropDownStyle = ComboBoxStyle.DropDownList };
                excelOrientationComboBox.Items.Add("Horizontal");
                excelOrientationComboBox.Items.Add("Vertical");
                excelOrientationComboBox.SelectedItem = settings != null && settings.ExcelOrientation == PdfPageOrientation.Portrait ? "Vertical" : "Horizontal";
                pageGroupBox.Controls.Add(paperLabel); pageGroupBox.Controls.Add(excelPaperComboBox); pageGroupBox.Controls.Add(orientationLabel); pageGroupBox.Controls.Add(excelOrientationComboBox);

                GroupBox printGroupBox = new GroupBox { Text = "Impresión y vista", Left = 16, Top = 112, Width = 330, Height = 168 };
                Label scalingLabel = new Label { Text = "Modo de ajuste:", Left = 18, Top = 29, Width = 105, Height = 22, TextAlign = ContentAlignment.MiddleLeft };
                excelScalingModeComboBox = new ComboBox { Left = 125, Top = 26, Width = 185, DropDownStyle = ComboBoxStyle.DropDownList };
                AddScalingOptions(excelScalingModeComboBox);
                SelectExcelScalingMode(settings == null ? GenesysPdfScalingMode.None : settings.ExcelScalingMode);
                excelFreezeHeaderCheckBox = CreatePdfCheckBox("Congelar encabezado", 18, 58, 145, settings == null || settings.ExcelFreezeHeader, true);
                excelAutoFilterCheckBox = CreatePdfCheckBox("Autofiltro", 178, 58, 130, settings == null || settings.ExcelAutoFilter, true);
                excelAutoFitColumnsCheckBox = CreatePdfCheckBox("Autoajustar columnas", 18, 86, 160, settings == null || settings.ExcelAutoFitColumns, true);
                excelRepeatHeaderCheckBox = CreatePdfCheckBox("Repetir encabezado al imprimir", 178, 86, 140, settings == null || settings.ExcelRepeatHeaderOnEachPage, true);
                excelCenterHorizontallyCheckBox = CreatePdfCheckBox("Centrar horizontal", 18, 114, 145, settings == null || settings.ExcelCenterHorizontally, true);
                excelCenterVerticallyCheckBox = CreatePdfCheckBox("Centrar vertical", 178, 114, 130, settings != null && settings.ExcelCenterVertically, true);
                excelPrintGridLinesCheckBox = CreatePdfCheckBox("Imprimir líneas", 18, 140, 145, settings != null && settings.ExcelPrintGridLines, true);
                excelShowGridLinesCheckBox = CreatePdfCheckBox("Ver líneas", 178, 140, 130, settings == null || settings.ExcelShowGridLines, true);
                printGroupBox.Controls.Add(scalingLabel); printGroupBox.Controls.Add(excelScalingModeComboBox); printGroupBox.Controls.Add(excelFreezeHeaderCheckBox); printGroupBox.Controls.Add(excelAutoFilterCheckBox); printGroupBox.Controls.Add(excelAutoFitColumnsCheckBox); printGroupBox.Controls.Add(excelRepeatHeaderCheckBox); printGroupBox.Controls.Add(excelCenterHorizontallyCheckBox); printGroupBox.Controls.Add(excelCenterVerticallyCheckBox); printGroupBox.Controls.Add(excelPrintGridLinesCheckBox); printGroupBox.Controls.Add(excelShowGridLinesCheckBox);

                GroupBox contentGroupBox = new GroupBox { Text = "Contenido", Left = 360, Top = 112, Width = 342, Height = 168 };
                excelExportTextCheckBox = CreatePdfCheckBox("Exportar texto visible", 18, 28, 155, settings != null && settings.ExcelOptions.ExportMode == ExportMode.Text, true);
                excelExportStackedHeadersCheckBox = CreatePdfCheckBox("Encabezados agrupados", 178, 28, 150, settings == null || settings.ExcelOptions.ExportStackedHeaders, true);
                excelExportUnboundRowsCheckBox = CreatePdfCheckBox("Filas no enlazadas", 18, 56, 155, settings == null || settings.ExcelOptions.ExportUnboundRows, true);
                excelAllowOutliningCheckBox = CreatePdfCheckBox("Agrupar con esquema", 178, 56, 150, settings == null || settings.ExcelOptions.AllowOutlining, true);
                excelExportMergedCellsCheckBox = CreatePdfCheckBox("Celdas combinadas", 18, 84, 155, settings != null && settings.ExcelExportMergedCells, true);
                excelAppendFooterCheckBox = CreatePdfCheckBox("Datos/filtros/resumen", 178, 84, 150, settings == null || settings.ExcelAppendFooter, true);
                excelShowHeadingsCheckBox = CreatePdfCheckBox("Ver encabezados fila/columna", 18, 112, 190, settings == null || settings.ExcelShowHeadings, true);
                excelProtectSheetCheckBox = CreatePdfCheckBox("Proteger hoja", 178, 112, 140, settings != null && settings.ExcelProtectSheet, true);
                contentGroupBox.Controls.Add(excelExportTextCheckBox); contentGroupBox.Controls.Add(excelExportStackedHeadersCheckBox); contentGroupBox.Controls.Add(excelExportUnboundRowsCheckBox); contentGroupBox.Controls.Add(excelAllowOutliningCheckBox); contentGroupBox.Controls.Add(excelExportMergedCellsCheckBox); contentGroupBox.Controls.Add(excelAppendFooterCheckBox); contentGroupBox.Controls.Add(excelShowHeadingsCheckBox); contentGroupBox.Controls.Add(excelProtectSheetCheckBox);
                excelGroupBox.Controls.Add(pageGroupBox); excelGroupBox.Controls.Add(printGroupBox); excelGroupBox.Controls.Add(contentGroupBox);
                Controls.Add(excelGroupBox);
                optionsTop = 366;
            }

            openFolderCheckBox = new CheckBox
            {
                Text = "Abrir/mostrar la carpeta al terminar",
                Left = 18,
                Top = optionsTop,
                Width = 300,
                Height = 24,
                Checked = openFolderAfterExport
            };

            openFileCheckBox = new CheckBox
            {
                Text = "Abrir el archivo generado al terminar",
                Left = 18,
                Top = optionsTop + 28,
                Width = 300,
                Height = 24,
                Checked = openFileAfterExport
            };

            acceptButton = new Button
            {
                Text = "Exportar",
                Left = 548,
                Top = optionsTop + 58,
                Width = 86,
                Height = 30,
                DialogResult = DialogResult.OK
            };
            acceptButton.Click += AcceptButton_Click;

            cancelButton = new Button
            {
                Text = "Cancelar",
                Left = 644,
                Top = optionsTop + 58,
                Width = 86,
                Height = 30,
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(pathLabel);
            Controls.Add(pathTextBox);
            Controls.Add(browseButton);
            if (exportInfoLabel != null && !isPdf)
                Controls.Add(exportInfoLabel);
            Controls.Add(openFolderCheckBox);
            Controls.Add(openFileCheckBox);
            Controls.Add(acceptButton);
            Controls.Add(cancelButton);

            AcceptButton = acceptButton;
            CancelButton = cancelButton;
        }

        private static bool TryGetBoolProperty(object target, string propertyName, bool defaultValue)
        {
            if (target == null || string.IsNullOrWhiteSpace(propertyName))
                return defaultValue;

            try
            {
                var property = target.GetType().GetProperty(propertyName);
                if (property == null || !property.CanRead)
                    return defaultValue;

                object value = property.GetValue(target, null);
                if (value == null)
                    return defaultValue;

                if (value is bool)
                    return (bool)value;

                bool parsed;
                return bool.TryParse(Convert.ToString(value), out parsed) ? parsed : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private static bool IsPdfOptionEditable(GenesysGridExportSettings settings, GenesysExportOptionLevel optionLevel)
        {
            if (settings == null)
                return true;

            return settings.AllowUserOverridePdfOptions && optionLevel == GenesysExportOptionLevel.UserSelectable;
        }

        private CheckBox CreatePdfCheckBox(string text, int left, int top, int width, bool isChecked, bool enabled)
        {
            return new CheckBox
            {
                Text = text,
                Left = left,
                Top = top,
                Width = width,
                Height = 24,
                Checked = isChecked,
                Enabled = enabled
            };
        }

        private RadioButton CreatePdfRadioButton(string text, int left, int top, int width, bool isChecked, bool enabled)
        {
            return new RadioButton
            {
                Text = text,
                Left = left,
                Top = top,
                Width = width,
                Height = 24,
                Checked = isChecked,
                Enabled = enabled
            };
        }

        private static bool IsPdfColumnWidthModeEditable(GenesysGridExportSettings settings)
        {
            if (settings == null)
                return true;

            if (!settings.AllowUserOverridePdfOptions)
                return false;

            return settings.PdfApplyViewColumnWidthsOptionLevel == GenesysExportOptionLevel.UserSelectable ||
                   settings.PdfAutoColumnWidthOptionLevel == GenesysExportOptionLevel.UserSelectable;
        }

        private void SelectPdfColumnWidthMode(GenesysGridExportSettings settings)
        {
            if (pdfColumnWidthModeComboBox == null)
                return;

            GenesysPdfColumnWidthMode mode = GenesysPdfColumnWidthMode.UseViewWidths;

            if (settings != null && settings.PdfOptions != null && settings.PdfOptions.AutoColumnWidth)
                mode = GenesysPdfColumnWidthMode.AutoColumnWidth;
            else if (settings != null && !settings.PdfApplyViewColumnWidths)
                mode = GenesysPdfColumnWidthMode.SyncfusionDefault;

            SelectPdfColumnWidthMode(mode);
        }

        private void SelectPdfColumnWidthMode(GenesysPdfColumnWidthMode mode)
        {
            if (pdfColumnWidthModeComboBox == null)
                return;

            string display = GetPdfColumnWidthModeDisplayName(mode);

            for (int i = 0; i < pdfColumnWidthModeComboBox.Items.Count; i++)
            {
                if (string.Equals(Convert.ToString(pdfColumnWidthModeComboBox.Items[i]), display, StringComparison.OrdinalIgnoreCase))
                {
                    pdfColumnWidthModeComboBox.SelectedIndex = i;
                    return;
                }
            }

            pdfColumnWidthModeComboBox.SelectedIndex = 0;
        }

        private GenesysPdfColumnWidthMode GetSelectedPdfColumnWidthMode()
        {
            string value = pdfColumnWidthModeComboBox == null ? null : Convert.ToString(pdfColumnWidthModeComboBox.SelectedItem);

            if (string.Equals(value, "Auto ancho de columnas", StringComparison.OrdinalIgnoreCase))
                return GenesysPdfColumnWidthMode.AutoColumnWidth;

            if (string.Equals(value, "Ancho estándar Syncfusion", StringComparison.OrdinalIgnoreCase))
                return GenesysPdfColumnWidthMode.SyncfusionDefault;

            return GenesysPdfColumnWidthMode.UseViewWidths;
        }

        private string GetSelectedPdfColumnWidthModeDisplay()
        {
            return GetPdfColumnWidthModeDisplayName(GetSelectedPdfColumnWidthMode());
        }

        private void AddPdfColumnWidthModeOptions(ComboBox comboBox)
        {
            if (comboBox == null)
                return;

            comboBox.Items.Add("Usar anchos de la vista");
            comboBox.Items.Add("Auto ancho de columnas");
            comboBox.Items.Add("Ancho estándar Syncfusion");
        }

        private static string GetPdfColumnWidthModeDisplayName(GenesysPdfColumnWidthMode mode)
        {
            switch (mode)
            {
                case GenesysPdfColumnWidthMode.AutoColumnWidth:
                    return "Auto ancho de columnas";

                case GenesysPdfColumnWidthMode.SyncfusionDefault:
                    return "Ancho estándar Syncfusion";

                default:
                    return "Usar anchos de la vista";
            }
        }

        private void RegisterPdfOptionChangedHandlers()
        {
            if (pdfPaperComboBox != null) pdfPaperComboBox.SelectedIndexChanged += PdfOutputOption_Changed;
            if (pdfOrientationComboBox != null) pdfOrientationComboBox.SelectedIndexChanged += PdfOutputOption_Changed;
            if (pdfColumnWidthModeComboBox != null) pdfColumnWidthModeComboBox.SelectedIndexChanged += PdfOutputOption_Changed;
            if (pdfScalingModeComboBox != null) pdfScalingModeComboBox.SelectedIndexChanged += PdfOutputOption_Changed;
            if (pdfAutoRowHeightCheckBox != null) pdfAutoRowHeightCheckBox.CheckedChanged += PdfOutputOption_Changed;
            if (pdfRepeatHeadersCheckBox != null) pdfRepeatHeadersCheckBox.CheckedChanged += PdfOutputOption_Changed;
            if (pdfExportFormatCheckBox != null) pdfExportFormatCheckBox.CheckedChanged += PdfOutputOption_Changed;
            if (pdfExportStackedHeadersCheckBox != null) pdfExportStackedHeadersCheckBox.CheckedChanged += PdfOutputOption_Changed;
            if (pdfIncludeGenerationInfoCheckBox != null) pdfIncludeGenerationInfoCheckBox.CheckedChanged += PdfOutputOption_Changed;
            if (pdfIncludeFilterInfoCheckBox != null) pdfIncludeFilterInfoCheckBox.CheckedChanged += PdfOutputOption_Changed;
            if (pdfIncludeExportSummaryCheckBox != null) pdfIncludeExportSummaryCheckBox.CheckedChanged += PdfOutputOption_Changed;
            if (pdfExportGroupsCheckBox != null) pdfExportGroupsCheckBox.CheckedChanged += PdfOutputOption_Changed;
            if (pdfExportGroupSummaryCheckBox != null) pdfExportGroupSummaryCheckBox.CheckedChanged += PdfOutputOption_Changed;
            if (pdfExportTableSummaryCheckBox != null) pdfExportTableSummaryCheckBox.CheckedChanged += PdfOutputOption_Changed;
            if (pdfAllowTextWrapCheckBox != null) pdfAllowTextWrapCheckBox.CheckedChanged += PdfOutputOption_Changed;
        }

        private void AddScalingOptions(ComboBox comboBox)
        {
            if (comboBox == null)
                return;

            comboBox.Items.Add("Sin escalado");
            comboBox.Items.Add("Ajustar hoja a una página");
            comboBox.Items.Add("Ajustar columnas a una página");
            comboBox.Items.Add("Ajustar filas a una página");
        }

        private void SelectPdfScalingOption(GenesysPdfScalingMode scalingMode)
        {
            if (pdfScalingModeComboBox == null)
                return;

            string display = GetScalingDisplayName(scalingMode);

            for (int i = 0; i < pdfScalingModeComboBox.Items.Count; i++)
            {
                if (string.Equals(Convert.ToString(pdfScalingModeComboBox.Items[i]), display, StringComparison.OrdinalIgnoreCase))
                {
                    pdfScalingModeComboBox.SelectedIndex = i;
                    return;
                }
            }

            pdfScalingModeComboBox.SelectedIndex = 0;
        }

        private GenesysPdfScalingMode GetSelectedPdfScalingMode()
        {
            return GetSelectedScalingMode(pdfScalingModeComboBox);
        }

        private static GenesysPdfScalingMode GetSelectedScalingMode(ComboBox comboBox)
        {
            string value = comboBox == null ? null : Convert.ToString(comboBox.SelectedItem);

            if (string.Equals(value, "Ajustar hoja a una página", StringComparison.OrdinalIgnoreCase))
                return GenesysPdfScalingMode.FitSheetOnOnePage;

            if (string.Equals(value, "Ajustar columnas a una página", StringComparison.OrdinalIgnoreCase))
                return GenesysPdfScalingMode.FitAllColumnsOnOnePage;

            if (string.Equals(value, "Ajustar filas a una página", StringComparison.OrdinalIgnoreCase))
                return GenesysPdfScalingMode.FitAllRowsOnOnePage;

            return GenesysPdfScalingMode.None;
        }

        private static string GetScalingDisplayName(GenesysPdfScalingMode scalingMode)
        {
            switch (scalingMode)
            {
                case GenesysPdfScalingMode.FitSheetOnOnePage:
                    return "Ajustar hoja a una página";

                case GenesysPdfScalingMode.FitAllColumnsOnOnePage:
                    return "Ajustar columnas a una página";

                case GenesysPdfScalingMode.FitAllRowsOnOnePage:
                    return "Ajustar filas a una página";

                default:
                    return "Sin escalado";
            }
        }

        // Compatibilidad con nombres históricos PDF dentro del diálogo.
        private static string GetPdfScalingDisplayName(GenesysPdfScalingMode scalingMode)
        {
            return GetScalingDisplayName(scalingMode);
        }

        private void SelectExcelPaperOption(GenesysPdfPaperMode paperMode)
        {
            if (excelPaperComboBox == null) return;
            string display = GetPaperDisplayName(paperMode);
            for (int i = 0; i < excelPaperComboBox.Items.Count; i++)
            {
                if (string.Equals(Convert.ToString(excelPaperComboBox.Items[i]), display, StringComparison.OrdinalIgnoreCase)) { excelPaperComboBox.SelectedIndex = i; return; }
            }
            excelPaperComboBox.SelectedIndex = 0;
        }

        private GenesysPdfPaperMode GetSelectedExcelPaperMode()
        {
            return GetSelectedPaperMode(excelPaperComboBox);
        }

        private void SelectExcelScalingMode(GenesysPdfScalingMode scalingMode)
        {
            if (excelScalingModeComboBox == null) return;
            string display = GetScalingDisplayName(scalingMode);
            for (int i = 0; i < excelScalingModeComboBox.Items.Count; i++)
            {
                if (string.Equals(Convert.ToString(excelScalingModeComboBox.Items[i]), display, StringComparison.OrdinalIgnoreCase)) { excelScalingModeComboBox.SelectedIndex = i; return; }
            }
            excelScalingModeComboBox.SelectedIndex = 0;
        }

        private GenesysPdfScalingMode GetSelectedExcelScalingMode()
        {
            return GetSelectedScalingMode(excelScalingModeComboBox);
        }

        private void AddPaperOptions(ComboBox comboBox)
        {
            if (comboBox == null)
                return;

            comboBox.Items.Add("Automático");
            comboBox.Items.Add("Carta");
            comboBox.Items.Add("Legal");
            comboBox.Items.Add("Oficio");
            comboBox.Items.Add("Doble carta");
            comboBox.Items.Add("Triple carta");
            comboBox.Items.Add("A3");
            comboBox.Items.Add("Personalizado");
        }

        private void SelectPdfPaperOption(GenesysPdfPaperMode paperMode)
        {
            if (pdfPaperComboBox == null)
                return;

            string display = GetPaperDisplayName(paperMode);

            for (int i = 0; i < pdfPaperComboBox.Items.Count; i++)
            {
                if (string.Equals(Convert.ToString(pdfPaperComboBox.Items[i]), display, StringComparison.OrdinalIgnoreCase))
                {
                    pdfPaperComboBox.SelectedIndex = i;
                    return;
                }
            }

            pdfPaperComboBox.SelectedIndex = 0;
        }

        private GenesysPdfPaperMode GetSelectedPdfPaperMode()
        {
            return GetSelectedPaperMode(pdfPaperComboBox);
        }

        private static GenesysPdfPaperMode GetSelectedPaperMode(ComboBox comboBox)
        {
            string value = comboBox == null ? null : Convert.ToString(comboBox.SelectedItem);

            if (string.Equals(value, "Carta", StringComparison.OrdinalIgnoreCase))
                return GenesysPdfPaperMode.LetterLandscape;

            if (string.Equals(value, "Legal", StringComparison.OrdinalIgnoreCase))
                return GenesysPdfPaperMode.LegalLandscape;

            if (string.Equals(value, "Oficio", StringComparison.OrdinalIgnoreCase))
                return GenesysPdfPaperMode.OficioLandscape;

            if (string.Equals(value, "Doble carta", StringComparison.OrdinalIgnoreCase))
                return GenesysPdfPaperMode.DoubleLetterLandscape;

            if (string.Equals(value, "Triple carta", StringComparison.OrdinalIgnoreCase))
                return GenesysPdfPaperMode.TripleLetterLandscape;

            if (string.Equals(value, "A3", StringComparison.OrdinalIgnoreCase))
                return GenesysPdfPaperMode.A3Landscape;

            if (string.Equals(value, "Personalizado", StringComparison.OrdinalIgnoreCase))
                return GenesysPdfPaperMode.CustomLandscape;

            return GenesysPdfPaperMode.AutomaticByColumns;
        }

        private static string GetPaperDisplayName(GenesysPdfPaperMode paperMode)
        {
            switch (paperMode)
            {
                case GenesysPdfPaperMode.LetterLandscape:
                    return "Carta";

                case GenesysPdfPaperMode.LegalLandscape:
                    return "Legal";

                case GenesysPdfPaperMode.OficioLandscape:
                    return "Oficio";

                case GenesysPdfPaperMode.DoubleLetterLandscape:
                    return "Doble carta";

                case GenesysPdfPaperMode.TripleLetterLandscape:
                    return "Triple carta";

                case GenesysPdfPaperMode.A3Landscape:
                    return "A3";

                case GenesysPdfPaperMode.CustomLandscape:
                    return "Personalizado";

                default:
                    return "Automático";
            }
        }

        private void PdfOutputOption_Changed(object sender, EventArgs e)
        {
            // Intencionalmente vacío. El diálogo ya muestra las opciones editables
            // y no necesita una etiqueta secundaria de resumen.
        }

        private string BuildCurrentPdfInfoText()
        {
            string paper = pdfPaperComboBox == null ? "Automático" : Convert.ToString(pdfPaperComboBox.SelectedItem);
            string orientation = pdfOrientationComboBox == null ? "Horizontal" : Convert.ToString(pdfOrientationComboBox.SelectedItem);

            return "Salida: " + paper + " " + orientation +
                   " | Columnas: " + GetSelectedPdfColumnWidthModeDisplay() +
                   " | Escalado: " + GetScalingDisplayName(SelectedPdfScalingMode) +
                   " | Encabezados: " + (SelectedPdfRepeatHeaders ? "repetir" : "no repetir") +
                   " | Formato: " + (SelectedPdfExportFormat ? "visual" : "valor real");
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
        public string PdfPaperMode { get; set; }
        public string PdfOrientation { get; set; }
        public string PdfScalingMode { get; set; }
        public bool? PdfFitAllColumnsInOnePage { get; set; }
        public bool? PdfFitAllRowsInOnePage { get; set; }
        public bool? PdfAutoColumnWidth { get; set; }
        public bool? PdfAutoRowHeight { get; set; }
        public bool? PdfRepeatHeaders { get; set; }
        public bool? PdfExportFormat { get; set; }
        public bool? PdfExportStackedHeaders { get; set; }
        public bool? PdfExportUnboundRows { get; set; }
        public bool? IncludeGenerationInfo { get; set; }
        public bool? IncludeFilterInfo { get; set; }
        public bool? IncludeExportSummary { get; set; }
        public bool? PdfExportGroups { get; set; }
        public bool? PdfExportGroupSummary { get; set; }
        public bool? PdfExportTableSummary { get; set; }
        public bool? PdfApplyViewColumnWidths { get; set; }
        public bool? PdfAllowTextWrap { get; set; }
        public string ExcelPaperMode { get; set; }
        public string ExcelOrientation { get; set; }
        public string ExcelScalingMode { get; set; }
        public bool? ExcelExportText { get; set; }
        public bool? ExcelExportStackedHeaders { get; set; }
        public bool? ExcelExportUnboundRows { get; set; }
        public bool? ExcelAllowOutlining { get; set; }
        public bool? ExcelExportMergedCells { get; set; }
        public bool? ExcelFreezeHeader { get; set; }
        public bool? ExcelAutoFilter { get; set; }
        public bool? ExcelAutoFitColumns { get; set; }
        public bool? ExcelAppendFooter { get; set; }
        public bool? ExcelRepeatHeaderOnEachPage { get; set; }
        public bool? ExcelCenterHorizontally { get; set; }
        public bool? ExcelCenterVertically { get; set; }
        public bool? ExcelPrintGridLines { get; set; }
        public bool? ExcelShowGridLines { get; set; }
        public bool? ExcelShowHeadings { get; set; }
        public bool? ExcelProtectSheet { get; set; }
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
                    else if (string.Equals(name, "PdfPaperMode", StringComparison.OrdinalIgnoreCase))
                        state.PdfPaperMode = value;
                    else if (string.Equals(name, "PdfOrientation", StringComparison.OrdinalIgnoreCase))
                        state.PdfOrientation = value;
                    else if (string.Equals(name, "PdfScalingMode", StringComparison.OrdinalIgnoreCase))
                        state.PdfScalingMode = value;
                    else if (string.Equals(name, "PdfFitAllColumnsInOnePage", StringComparison.OrdinalIgnoreCase))
                        state.PdfFitAllColumnsInOnePage = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "PdfFitAllRowsInOnePage", StringComparison.OrdinalIgnoreCase))
                        state.PdfFitAllRowsInOnePage = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "PdfAutoColumnWidth", StringComparison.OrdinalIgnoreCase))
                        state.PdfAutoColumnWidth = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "PdfAutoRowHeight", StringComparison.OrdinalIgnoreCase))
                        state.PdfAutoRowHeight = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "PdfRepeatHeaders", StringComparison.OrdinalIgnoreCase))
                        state.PdfRepeatHeaders = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "PdfExportFormat", StringComparison.OrdinalIgnoreCase))
                        state.PdfExportFormat = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "PdfExportStackedHeaders", StringComparison.OrdinalIgnoreCase))
                        state.PdfExportStackedHeaders = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "PdfExportUnboundRows", StringComparison.OrdinalIgnoreCase))
                        state.PdfExportUnboundRows = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "IncludeGenerationInfo", StringComparison.OrdinalIgnoreCase))
                        state.IncludeGenerationInfo = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "IncludeFilterInfo", StringComparison.OrdinalIgnoreCase))
                        state.IncludeFilterInfo = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "IncludeExportSummary", StringComparison.OrdinalIgnoreCase))
                        state.IncludeExportSummary = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "PdfExportGroups", StringComparison.OrdinalIgnoreCase))
                        state.PdfExportGroups = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "PdfExportGroupSummary", StringComparison.OrdinalIgnoreCase))
                        state.PdfExportGroupSummary = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "PdfExportTableSummary", StringComparison.OrdinalIgnoreCase))
                        state.PdfExportTableSummary = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "PdfApplyViewColumnWidths", StringComparison.OrdinalIgnoreCase))
                        state.PdfApplyViewColumnWidths = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "PdfAllowTextWrap", StringComparison.OrdinalIgnoreCase))
                        state.PdfAllowTextWrap = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "ExcelPaperMode", StringComparison.OrdinalIgnoreCase))
                        state.ExcelPaperMode = value;
                    else if (string.Equals(name, "ExcelOrientation", StringComparison.OrdinalIgnoreCase))
                        state.ExcelOrientation = value;
                    else if (string.Equals(name, "ExcelScalingMode", StringComparison.OrdinalIgnoreCase))
                        state.ExcelScalingMode = value;
                    else if (string.Equals(name, "ExcelExportText", StringComparison.OrdinalIgnoreCase))
                        state.ExcelExportText = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "ExcelExportStackedHeaders", StringComparison.OrdinalIgnoreCase))
                        state.ExcelExportStackedHeaders = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "ExcelExportUnboundRows", StringComparison.OrdinalIgnoreCase))
                        state.ExcelExportUnboundRows = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "ExcelAllowOutlining", StringComparison.OrdinalIgnoreCase))
                        state.ExcelAllowOutlining = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "ExcelExportMergedCells", StringComparison.OrdinalIgnoreCase))
                        state.ExcelExportMergedCells = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "ExcelFreezeHeader", StringComparison.OrdinalIgnoreCase))
                        state.ExcelFreezeHeader = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "ExcelAutoFilter", StringComparison.OrdinalIgnoreCase))
                        state.ExcelAutoFilter = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "ExcelAutoFitColumns", StringComparison.OrdinalIgnoreCase))
                        state.ExcelAutoFitColumns = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "ExcelAppendFooter", StringComparison.OrdinalIgnoreCase))
                        state.ExcelAppendFooter = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "ExcelRepeatHeaderOnEachPage", StringComparison.OrdinalIgnoreCase))
                        state.ExcelRepeatHeaderOnEachPage = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "ExcelCenterHorizontally", StringComparison.OrdinalIgnoreCase))
                        state.ExcelCenterHorizontally = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "ExcelCenterVertically", StringComparison.OrdinalIgnoreCase))
                        state.ExcelCenterVertically = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "ExcelPrintGridLines", StringComparison.OrdinalIgnoreCase))
                        state.ExcelPrintGridLines = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "ExcelShowGridLines", StringComparison.OrdinalIgnoreCase))
                        state.ExcelShowGridLines = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "ExcelShowHeadings", StringComparison.OrdinalIgnoreCase))
                        state.ExcelShowHeadings = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    else if (string.Equals(name, "ExcelProtectSheet", StringComparison.OrdinalIgnoreCase))
                        state.ExcelProtectSheet = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
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
                    if (!string.IsNullOrWhiteSpace(state.PdfPaperMode))
                        lines.Add("PdfPaperMode=" + state.PdfPaperMode);
                    if (!string.IsNullOrWhiteSpace(state.PdfOrientation))
                        lines.Add("PdfOrientation=" + state.PdfOrientation);
                    if (!string.IsNullOrWhiteSpace(state.PdfScalingMode))
                        lines.Add("PdfScalingMode=" + state.PdfScalingMode);
                    if (state.PdfFitAllColumnsInOnePage.HasValue)
                        lines.Add("PdfFitAllColumnsInOnePage=" + state.PdfFitAllColumnsInOnePage.Value.ToString().ToLowerInvariant());
                    if (state.PdfFitAllRowsInOnePage.HasValue)
                        lines.Add("PdfFitAllRowsInOnePage=" + state.PdfFitAllRowsInOnePage.Value.ToString().ToLowerInvariant());
                    if (state.PdfAutoColumnWidth.HasValue)
                        lines.Add("PdfAutoColumnWidth=" + state.PdfAutoColumnWidth.Value.ToString().ToLowerInvariant());
                    if (state.PdfAutoRowHeight.HasValue)
                        lines.Add("PdfAutoRowHeight=" + state.PdfAutoRowHeight.Value.ToString().ToLowerInvariant());
                    if (state.PdfRepeatHeaders.HasValue)
                        lines.Add("PdfRepeatHeaders=" + state.PdfRepeatHeaders.Value.ToString().ToLowerInvariant());
                    if (state.PdfExportFormat.HasValue)
                        lines.Add("PdfExportFormat=" + state.PdfExportFormat.Value.ToString().ToLowerInvariant());
                    if (state.PdfExportStackedHeaders.HasValue)
                        lines.Add("PdfExportStackedHeaders=" + state.PdfExportStackedHeaders.Value.ToString().ToLowerInvariant());
                    if (state.PdfExportUnboundRows.HasValue)
                        lines.Add("PdfExportUnboundRows=" + state.PdfExportUnboundRows.Value.ToString().ToLowerInvariant());
                    if (state.IncludeGenerationInfo.HasValue)
                        lines.Add("IncludeGenerationInfo=" + state.IncludeGenerationInfo.Value.ToString().ToLowerInvariant());
                    if (state.IncludeFilterInfo.HasValue)
                        lines.Add("IncludeFilterInfo=" + state.IncludeFilterInfo.Value.ToString().ToLowerInvariant());
                    if (state.IncludeExportSummary.HasValue)
                        lines.Add("IncludeExportSummary=" + state.IncludeExportSummary.Value.ToString().ToLowerInvariant());
                    if (state.PdfExportGroups.HasValue)
                        lines.Add("PdfExportGroups=" + state.PdfExportGroups.Value.ToString().ToLowerInvariant());
                    if (state.PdfExportGroupSummary.HasValue)
                        lines.Add("PdfExportGroupSummary=" + state.PdfExportGroupSummary.Value.ToString().ToLowerInvariant());
                    if (state.PdfExportTableSummary.HasValue)
                        lines.Add("PdfExportTableSummary=" + state.PdfExportTableSummary.Value.ToString().ToLowerInvariant());
                    if (state.PdfApplyViewColumnWidths.HasValue)
                        lines.Add("PdfApplyViewColumnWidths=" + state.PdfApplyViewColumnWidths.Value.ToString().ToLowerInvariant());
                    if (state.PdfAllowTextWrap.HasValue)
                        lines.Add("PdfAllowTextWrap=" + state.PdfAllowTextWrap.Value.ToString().ToLowerInvariant());

                    if (!string.IsNullOrWhiteSpace(state.ExcelPaperMode))
                        lines.Add("ExcelPaperMode=" + state.ExcelPaperMode);
                    if (!string.IsNullOrWhiteSpace(state.ExcelOrientation))
                        lines.Add("ExcelOrientation=" + state.ExcelOrientation);
                    if (!string.IsNullOrWhiteSpace(state.ExcelScalingMode))
                        lines.Add("ExcelScalingMode=" + state.ExcelScalingMode);
                    if (state.ExcelExportText.HasValue)
                        lines.Add("ExcelExportText=" + state.ExcelExportText.Value.ToString().ToLowerInvariant());
                    if (state.ExcelExportStackedHeaders.HasValue)
                        lines.Add("ExcelExportStackedHeaders=" + state.ExcelExportStackedHeaders.Value.ToString().ToLowerInvariant());
                    if (state.ExcelExportUnboundRows.HasValue)
                        lines.Add("ExcelExportUnboundRows=" + state.ExcelExportUnboundRows.Value.ToString().ToLowerInvariant());
                    if (state.ExcelAllowOutlining.HasValue)
                        lines.Add("ExcelAllowOutlining=" + state.ExcelAllowOutlining.Value.ToString().ToLowerInvariant());
                    if (state.ExcelExportMergedCells.HasValue)
                        lines.Add("ExcelExportMergedCells=" + state.ExcelExportMergedCells.Value.ToString().ToLowerInvariant());
                    if (state.ExcelFreezeHeader.HasValue)
                        lines.Add("ExcelFreezeHeader=" + state.ExcelFreezeHeader.Value.ToString().ToLowerInvariant());
                    if (state.ExcelAutoFilter.HasValue)
                        lines.Add("ExcelAutoFilter=" + state.ExcelAutoFilter.Value.ToString().ToLowerInvariant());
                    if (state.ExcelAutoFitColumns.HasValue)
                        lines.Add("ExcelAutoFitColumns=" + state.ExcelAutoFitColumns.Value.ToString().ToLowerInvariant());
                    if (state.ExcelAppendFooter.HasValue)
                        lines.Add("ExcelAppendFooter=" + state.ExcelAppendFooter.Value.ToString().ToLowerInvariant());
                    if (state.ExcelRepeatHeaderOnEachPage.HasValue)
                        lines.Add("ExcelRepeatHeaderOnEachPage=" + state.ExcelRepeatHeaderOnEachPage.Value.ToString().ToLowerInvariant());
                    if (state.ExcelCenterHorizontally.HasValue)
                        lines.Add("ExcelCenterHorizontally=" + state.ExcelCenterHorizontally.Value.ToString().ToLowerInvariant());
                    if (state.ExcelCenterVertically.HasValue)
                        lines.Add("ExcelCenterVertically=" + state.ExcelCenterVertically.Value.ToString().ToLowerInvariant());
                    if (state.ExcelPrintGridLines.HasValue)
                        lines.Add("ExcelPrintGridLines=" + state.ExcelPrintGridLines.Value.ToString().ToLowerInvariant());
                    if (state.ExcelShowGridLines.HasValue)
                        lines.Add("ExcelShowGridLines=" + state.ExcelShowGridLines.Value.ToString().ToLowerInvariant());
                    if (state.ExcelShowHeadings.HasValue)
                        lines.Add("ExcelShowHeadings=" + state.ExcelShowHeadings.Value.ToString().ToLowerInvariant());
                    if (state.ExcelProtectSheet.HasValue)
                        lines.Add("ExcelProtectSheet=" + state.ExcelProtectSheet.Value.ToString().ToLowerInvariant());

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
