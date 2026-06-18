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
    /// Modo de selección de papel para PDF.
    /// AutomaticByColumns calcula el tamaño según el ancho visible del grid.
    /// </summary>
    public enum GenesysPdfPaperMode
    {
        AutomaticByColumns,
        LetterLandscape,
        LegalLandscape,
        OficioLandscape,
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
    /// Nivel de control de una opción de exportación.
    /// FrameworkDefault/FormDefault/Locked se muestran como no editables;
    /// UserSelectable permite que el usuario cambie el valor en el diálogo.
    /// </summary>
    public enum GenesysExportOptionLevel
    {
        FrameworkDefault,
        FormDefault,
        UserSelectable,
        Locked
    }

    /// <summary>
    /// Modo de escalado PDF. Centraliza opciones excluyentes de ajuste de página.
    /// </summary>
    public enum GenesysPdfScalingMode
    {
        None,
        FitSheetOnOnePage,
        FitAllColumnsOnOnePage,
        FitAllRowsOnOnePage
    }


    /// <summary>
    /// Modo de cálculo de ancho de columnas en PDF.
    /// Las opciones son excluyentes para evitar mezclar ancho de vista con auto ancho de Syncfusion.
    /// </summary>
    internal enum GenesysPdfColumnWidthMode
    {
        UseViewWidths,
        AutoColumnWidth,
        SyncfusionDefault
    }

    /// <summary>
    /// Branding centralizado para exportaciones Genesys.
    /// Puede configurarse una sola vez al iniciar la aplicación, por ejemplo en Program.cs.
    /// </summary>
    public static class GenesysExportBranding
    {
        static GenesysExportBranding()
        {
            CompanyName = "Genesys";
            ApplicationName = "Genesys";
        }

        public static string CompanyName { get; set; }
        public static string ApplicationName { get; set; }
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
            AdditionalFilterInfoLines = new List<string>();

            WorksheetName = "Reporte";
            Title = "Reporte";
            CompanyName = GenesysExportBranding.CompanyName;

            HeaderBackColor = Color.FromArgb(217, 225, 242);
            HeaderForeColor = Color.Black;

            ExcelHeaderRowIndex = 1;
            ExcelFreezeHeader = true;
            ExcelAutoFilter = true;
            ExcelAutoFitColumns = true;
            ExcelAppendFooter = true;
            ExcelPaperMode = GenesysPdfPaperMode.AutomaticByColumns;
            ExcelOrientation = PdfPageOrientation.Landscape;
            ExcelScalingMode = GenesysPdfScalingMode.None;
            ExcelRepeatHeaderOnEachPage = true;
            ExcelCenterHorizontally = true;
            ExcelCenterVertically = false;
            ExcelPrintGridLines = false;
            ExcelShowGridLines = true;
            ExcelShowHeadings = true;
            ExcelProtectSheet = false;
            ExcelExportMergedCells = false;

            PdfPaperMode = GenesysPdfPaperMode.AutomaticByColumns;
            PdfPrintTarget = GenesysPdfPrintTarget.OriginalSize;
            PdfOrientation = PdfPageOrientation.Landscape;
            PdfMargins = 25F;
            PdfFooterHeight = 30F;
            PdfMinimumFontSize = 7F;
            PdfMaximumFontSize = 10F;
            PdfScalingMode = GenesysPdfScalingMode.None;
            PdfFitAllColumnsInOnePage = false;
            PdfFitAllRowsInOnePage = false;
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

            AllowUserOverridePdfOptions = true;
            PdfPaperModeOptionLevel = GenesysExportOptionLevel.UserSelectable;
            PdfOrientationOptionLevel = GenesysExportOptionLevel.UserSelectable;
            PdfScalingModeOptionLevel = GenesysExportOptionLevel.UserSelectable;
            PdfFitAllColumnsOptionLevel = GenesysExportOptionLevel.UserSelectable;
            PdfFitAllRowsOptionLevel = GenesysExportOptionLevel.UserSelectable;
            PdfAutoColumnWidthOptionLevel = GenesysExportOptionLevel.UserSelectable;
            PdfAutoRowHeightOptionLevel = GenesysExportOptionLevel.UserSelectable;
            PdfRepeatHeadersOptionLevel = GenesysExportOptionLevel.UserSelectable;
            PdfExportFormatOptionLevel = GenesysExportOptionLevel.UserSelectable;
            PdfExportStackedHeadersOptionLevel = GenesysExportOptionLevel.UserSelectable;
            PdfExportUnboundRowsOptionLevel = GenesysExportOptionLevel.UserSelectable;
            PdfExportGroupsOptionLevel = GenesysExportOptionLevel.UserSelectable;
            PdfExportGroupSummaryOptionLevel = GenesysExportOptionLevel.UserSelectable;
            PdfExportTableSummaryOptionLevel = GenesysExportOptionLevel.UserSelectable;
            PdfApplyViewColumnWidthsOptionLevel = GenesysExportOptionLevel.UserSelectable;
            PdfAllowTextWrapOptionLevel = GenesysExportOptionLevel.UserSelectable;
            IncludeGenerationInfoOptionLevel = GenesysExportOptionLevel.UserSelectable;
            IncludeFilterInfoOptionLevel = GenesysExportOptionLevel.UserSelectable;
            IncludeExportSummaryOptionLevel = GenesysExportOptionLevel.UserSelectable;
            IncludeCorporateHeaderOptionLevel = GenesysExportOptionLevel.UserSelectable;

            IncludeGenerationInfo = true;
            IncludeFilterInfo = true;
            IncludeExportSummary = false;
            IncludeCorporateHeader = true;

            // Tamaño default en puntos PDF para CustomLandscape. 1 pulgada = 72 puntos.
            PdfCustomLandscapeSize = new SizeF(1224F, 792F); // Doble carta horizontal.

            ExcelOptions.ExportMode = ExportMode.Value;
            ExcelOptions.ExcelVersion = ExcelVersion.Excel2013;
            ExcelOptions.ExportStackedHeaders = true;
            ExcelOptions.ExportUnboundRows = true;
            ExcelOptions.AllowOutlining = true;
            ExcelOptions.StartRowIndex = 1;
            ExcelOptions.StartColumnIndex = 1;
            TrySetProperty(ExcelOptions, "ExportMergedCells", false);

            PdfOptions.ExportStackedHeaders = true;
            PdfOptions.ExportUnboundRows = false;
            PdfOptions.AutoColumnWidth = false;
            PdfOptions.AutoRowHeight = true;
            PdfOptions.RepeatHeaders = true;
            PdfOptions.FitAllColumnsInOnePage = false;
            TrySetProperty(PdfOptions, "FitAllRowsInOnePage", false);
            TrySetProperty(PdfOptions, "FitToPage", false);
            PdfOptions.ExportFormat = true;
            TrySetProperty(PdfOptions, "ExportGroups", true);
            TrySetProperty(PdfOptions, "ExportGroupSummary", true);
            TrySetProperty(PdfOptions, "ExportTableSummary", true);
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

        /// <summary>
        /// Líneas de filtros externos/panel de filtros que el formulario hijo puede agregar.
        /// Útil para filtros que no viven dentro del SfDataGrid.
        /// </summary>
        public IList<string> AdditionalFilterInfoLines { get; private set; }

        /// <summary>
        /// Proveedor opcional para que el formulario hijo entregue filtros legibles al exportar.
        /// </summary>
        public Func<IList<string>> FilterInfoProvider { get; set; }

        public Color HeaderBackColor { get; set; }
        public Color HeaderForeColor { get; set; }

        public int ExcelHeaderRowIndex { get; set; }
        public bool ExcelFreezeHeader { get; set; }
        public bool ExcelAutoFilter { get; set; }
        public bool ExcelAutoFitColumns { get; set; }
        public bool ExcelAppendFooter { get; set; }
        public GenesysPdfPaperMode ExcelPaperMode { get; set; }
        public PdfPageOrientation ExcelOrientation { get; set; }
        public GenesysPdfScalingMode ExcelScalingMode { get; set; }
        public bool ExcelRepeatHeaderOnEachPage { get; set; }
        public bool ExcelCenterHorizontally { get; set; }
        public bool ExcelCenterVertically { get; set; }
        public bool ExcelPrintGridLines { get; set; }
        public bool ExcelShowGridLines { get; set; }
        public bool ExcelShowHeadings { get; set; }
        public bool ExcelProtectSheet { get; set; }
        public bool ExcelExportMergedCells { get; set; }

        public GenesysPdfPaperMode PdfPaperMode { get; set; }
        public GenesysPdfPrintTarget PdfPrintTarget { get; set; }
        public PdfPageOrientation PdfOrientation { get; set; }
        public SizeF PdfCustomLandscapeSize { get; set; }
        public float PdfMargins { get; set; }
        public float PdfFooterHeight { get; set; }
        public float PdfMinimumFontSize { get; set; }
        public float PdfMaximumFontSize { get; set; }
        public GenesysPdfScalingMode PdfScalingMode { get; set; }
        public bool PdfFitAllColumnsInOnePage { get; set; }
        public bool PdfFitAllRowsInOnePage { get; set; }
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

        /// <summary>
        /// Permite que el usuario modifique opciones PDF en el diálogo.
        /// Si es false, las opciones se muestran solo como configuración fijada por el formulario/framework.
        /// </summary>
        public bool AllowUserOverridePdfOptions { get; set; }

        public GenesysExportOptionLevel PdfPaperModeOptionLevel { get; set; }
        public GenesysExportOptionLevel PdfOrientationOptionLevel { get; set; }
        public GenesysExportOptionLevel PdfScalingModeOptionLevel { get; set; }
        public GenesysExportOptionLevel PdfFitAllColumnsOptionLevel { get; set; }
        public GenesysExportOptionLevel PdfFitAllRowsOptionLevel { get; set; }
        public GenesysExportOptionLevel PdfAutoColumnWidthOptionLevel { get; set; }
        public GenesysExportOptionLevel PdfAutoRowHeightOptionLevel { get; set; }
        public GenesysExportOptionLevel PdfRepeatHeadersOptionLevel { get; set; }
        public GenesysExportOptionLevel PdfExportFormatOptionLevel { get; set; }
        public GenesysExportOptionLevel PdfExportStackedHeadersOptionLevel { get; set; }
        public GenesysExportOptionLevel PdfExportUnboundRowsOptionLevel { get; set; }
        public GenesysExportOptionLevel PdfExportGroupsOptionLevel { get; set; }
        public GenesysExportOptionLevel PdfExportGroupSummaryOptionLevel { get; set; }
        public GenesysExportOptionLevel PdfExportTableSummaryOptionLevel { get; set; }
        public GenesysExportOptionLevel PdfApplyViewColumnWidthsOptionLevel { get; set; }
        public GenesysExportOptionLevel PdfAllowTextWrapOptionLevel { get; set; }

        /// <summary>
        /// Incluye datos de generación en salidas/documentos exportados.
        /// </summary>
        public bool IncludeGenerationInfo { get; set; }

        /// <summary>
        /// Incluye una referencia a filtros/vista activa cuando el framework disponga de esa información.
        /// </summary>
        public bool IncludeFilterInfo { get; set; }

        /// <summary>
        /// Incluye resumen operativo al final, por ejemplo total aproximado de registros exportados.
        /// </summary>
        public bool IncludeExportSummary { get; set; }

        /// <summary>
        /// Incluye encabezado corporativo en PDF y metadatos equivalentes en Excel.
        /// En Excel no inserta filas antes del grid para conservar la primera línea como encabezado.
        /// </summary>
        public bool IncludeCorporateHeader { get; set; }

        public GenesysExportOptionLevel IncludeGenerationInfoOptionLevel { get; set; }
        public GenesysExportOptionLevel IncludeFilterInfoOptionLevel { get; set; }
        public GenesysExportOptionLevel IncludeExportSummaryOptionLevel { get; set; }
        public GenesysExportOptionLevel IncludeCorporateHeaderOptionLevel { get; set; }

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
                {
                    Type targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                    if (targetType.IsEnum)
                        convertedValue = Enum.Parse(targetType, Convert.ToString(value), true);
                    else
                        convertedValue = Convert.ChangeType(value, targetType);
                }

                property.SetValue(target, convertedValue, null);
            }
            catch
            {
            }
        }

        public void ExcludeColumn(string mappingName)
        {
            if (string.IsNullOrWhiteSpace(mappingName))
                return;

            if (!ExcludedColumns.Contains(mappingName))
                ExcludedColumns.Add(mappingName);
        }
    }
}
