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
    /// Responsabilidad compartida: branding, vista activa, columnas, filtros, diálogo, persistencia y utilidades comunes.
    /// </summary>
    internal sealed partial class GenesysGridExportService
    {
        private void ApplyCentralizedBranding(GenesysGridExportSettings settings)
        {
            if (settings == null)
                return;

            if (string.IsNullOrWhiteSpace(settings.CompanyName) ||
                string.Equals(settings.CompanyName, "Genesys", StringComparison.OrdinalIgnoreCase))
            {
                string company = ResolveCentralCompanyName();
                if (!string.IsNullOrWhiteSpace(company))
                    settings.CompanyName = company;
            }
        }

        private static string ResolveCentralCompanyName()
        {
            if (!string.IsNullOrWhiteSpace(GenesysExportBranding.CompanyName))
                return GenesysExportBranding.CompanyName;

            string fromGenesysUi = TryResolveStaticStringFromType(
                "Genesys.UI.Components.GenesysUI",
                "CompanyName",
                "Empresa",
                "ApplicationName",
                "ProductName");

            if (!string.IsNullOrWhiteSpace(fromGenesysUi))
                return fromGenesysUi;

            return "Genesys";
        }

        private static string TryResolveStaticStringFromType(string typeName, params string[] propertyNames)
        {
            if (string.IsNullOrWhiteSpace(typeName) || propertyNames == null)
                return null;

            try
            {
                Type type = Type.GetType(typeName);

                if (type == null)
                {
                    foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            type = assembly.GetType(typeName, false, true);
                            if (type != null)
                                break;
                        }
                        catch
                        {
                        }
                    }
                }

                if (type == null)
                    return null;

                foreach (string propertyName in propertyNames)
                {
                    System.Reflection.PropertyInfo property = type.GetProperty(
                        propertyName,
                        System.Reflection.BindingFlags.Static |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic);

                    if (property == null || !property.CanRead)
                        continue;

                    object value = property.GetValue(null, null);
                    string text = value == null ? null : Convert.ToString(value);

                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }
            }
            catch
            {
            }

            return null;
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

        private string GetCurrentViewNameForDisplay(GenesysGridExportSettings settings)
        {
            if (settings != null && settings.CurrentViewLayout != null && !string.IsNullOrWhiteSpace(settings.CurrentViewLayout.ViewName))
                return settings.CurrentViewLayout.ViewName;

            return "Vista activa del grid";
        }

        private string GetFilterInfoForDisplay(GenesysGridExportSettings settings)
        {
            List<string> lines = GetFilterInfoLinesForDisplay(settings);

            if (lines.Count == 0)
                return "Sin filtros registrados";

            return string.Join("; ", lines.ToArray());
        }

        private List<string> GetFilterInfoLinesForDisplay(GenesysGridExportSettings settings)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Fuente oficial de filtros externos/panel: settings.AdditionalFilterInfoLines
            // y settings.FilterInfoProvider, cargados desde GenesysGridForm/GenesysGridFiltersPanel.
            // No invocar métodos del owner por reflexión aquí, porque duplicaba filtros
            // ya reportados por el panel y podía volver a mezclar XML persistido.
            AppendCustomFilterInfoLines(result, seen, settings);
            // No leer CurrentViewLayout.FilterStateXml aquí.
            // La fuente oficial de filtros del panel es GenesysGridFiltersPanel.GetExportFilterDescriptions(),
            // que GenesysGridForm coloca en settings.AdditionalFilterInfoLines.
            // Leer el XML aquí duplicaba Fecha/Rango/Fecha inicio/Fecha final.
            AppendSfDataGridColumnFilterLines(result, seen);
            AppendSfDataGridViewFilterLines(result, seen);

            if (result.Count == 0)
                result.Add("Sin filtros registrados");

            return result;
        }

        private void AppendCustomFilterInfoLines(List<string> result, HashSet<string> seen, GenesysGridExportSettings settings)
        {
            if (result == null || seen == null || settings == null)
                return;

            if (settings.AdditionalFilterInfoLines != null)
            {
                foreach (string line in settings.AdditionalFilterInfoLines)
                    AddFilterInfoLine(result, seen, line);
            }

            if (settings.FilterInfoProvider == null)
                return;

            try
            {
                IList<string> lines = settings.FilterInfoProvider();

                if (lines == null)
                    return;

                foreach (string line in lines)
                    AddFilterInfoLine(result, seen, line);
            }
            catch
            {
            }
        }

        private void AppendOwnerProvidedFilterInfoLines(List<string> result, HashSet<string> seen)
        {
            if (result == null || seen == null || owner == null)
                return;

            foreach (string methodName in new[]
            {
                "GetExportFilterInfoLines",
                "GetExportFilterInfo",
                "GetPanelFilterInfoLines",
                "GetPanelFilterInfo",
                "CaptureExportFilterInfo",
                "CaptureFilterDisplayText"
            })
            {
                object value = TryInvokeOwnerNoArgumentMethod(methodName);
                AppendFilterInfoValue(result, seen, value);
            }
        }

        private static bool IsTrivialFilterValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;

            value = value.Trim();

            return
                string.Equals(value, "Todos", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Todas", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Todo", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "All", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Seleccione", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Seleccionar", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "N/A", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "-", StringComparison.OrdinalIgnoreCase);
        }

        private object TryInvokeOwnerNoArgumentMethod(string methodName)
        {
            if (owner == null || string.IsNullOrWhiteSpace(methodName))
                return null;

            try
            {
                System.Reflection.MethodInfo method = owner.GetType().GetMethod(
                    methodName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic,
                    null,
                    Type.EmptyTypes,
                    null);

                if (method == null)
                    return null;

                return method.Invoke(owner, null);
            }
            catch
            {
                return null;
            }
        }

        private static void AppendFilterInfoValue(List<string> result, HashSet<string> seen, object value)
        {
            if (result == null || seen == null || value == null)
                return;

            string text = value as string;
            if (text != null)
            {
                foreach (string line in text.Split(new[] { "\r\n", "\n", ";" }, StringSplitOptions.RemoveEmptyEntries))
                    AddFilterInfoLine(result, seen, line);

                return;
            }

            System.Collections.IDictionary dictionary = value as System.Collections.IDictionary;
            if (dictionary != null)
            {
                foreach (System.Collections.DictionaryEntry entry in dictionary)
                    AddFilterInfoLine(result, seen, Convert.ToString(entry.Key) + ": " + Convert.ToString(entry.Value));

                return;
            }

            System.Collections.IEnumerable enumerable = value as System.Collections.IEnumerable;
            if (enumerable == null)
                return;

            foreach (object item in enumerable)
                AddFilterInfoLine(result, seen, item == null ? null : Convert.ToString(item));
        }

        private void AppendFilterStateXmlLines(
            List<string> result,
            HashSet<string> seen,
            GenesysGridExportSettings settings)
        {
            if (result == null || seen == null || settings == null || settings.CurrentViewLayout == null)
                return;

            string xml = settings.CurrentViewLayout.FilterStateXml;

            if (string.IsNullOrWhiteSpace(xml))
                return;

            try
            {
                xml = System.Net.WebUtility.HtmlDecode(xml);

                MatchCollection matches = Regex.Matches(
                    xml,
                    @"<(?<name>[A-Za-z_][A-Za-z0-9_\.\-]*)>(?<value>.*?)</\k<name>>",
                    RegexOptions.Singleline);

                foreach (Match match in matches)
                {
                    if (!match.Success)
                        continue;

                    string name = match.Groups["name"].Value;
                    string value = match.Groups["value"].Value;

                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
                        continue;

                    if (string.Equals(name, "GenesysGridFilterState", StringComparison.OrdinalIgnoreCase))
                        continue;

                    value = NormalizeFilterValue(value);

                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    AddFilterInfoLine(result, seen, NormalizeFilterName(name) + ": " + value);
                }
            }
            catch
            {
            }
        }

        private void AppendSfDataGridColumnFilterLines(List<string> result, HashSet<string> seen)
        {
            if (result == null || seen == null || grid == null || grid.Columns == null)
                return;

            foreach (var column in grid.Columns)
            {
                if (column == null)
                    continue;

                string columnName = GetColumnHeaderText(column);
                if (string.IsNullOrWhiteSpace(columnName))
                    columnName = GetColumnMappingName(column);

                object predicates = GetObjectPropertyValue(column, "FilterPredicates");
                System.Collections.IEnumerable enumerable = predicates as System.Collections.IEnumerable;

                if (enumerable == null)
                    continue;

                foreach (object predicate in enumerable)
                {
                    string value = GetFirstStringPropertyValue(
                        predicate,
                        "FilterValue",
                        "ActualFilterValue",
                        "Value",
                        "FilterText");

                    value = NormalizeFilterValue(value);

                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    string filterType = GetFirstStringPropertyValue(predicate, "FilterType", "PredicateType", "FilterBehavior");
                    filterType = NormalizeFilterValue(filterType);

                    string line = string.IsNullOrWhiteSpace(filterType)
                        ? columnName + ": " + value
                        : columnName + ": " + value + " (" + filterType + ")";

                    AddFilterInfoLine(result, seen, line);
                }
            }
        }

        private void AppendSfDataGridViewFilterLines(List<string> result, HashSet<string> seen)
        {
            if (result == null || seen == null || grid == null || grid.View == null)
                return;

            string filterText = GetFirstStringPropertyValue(
                grid.View,
                "FilterText",
                "FilterString",
                "FilterExpression");

            filterText = NormalizeFilterValue(filterText);

            if (!string.IsNullOrWhiteSpace(filterText))
                AddFilterInfoLine(result, seen, "Filtro de vista: " + filterText);

            object filter = GetObjectPropertyValue(grid.View, "Filter");

            if (filter == null)
                return;

            string raw = Convert.ToString(filter);
            raw = NormalizeFilterValue(raw);

            if (string.IsNullOrWhiteSpace(raw))
                return;

            // Evita mostrar nombres técnicos de delegates/clases cuando no aportan al usuario.
            if (raw.IndexOf("System.", StringComparison.OrdinalIgnoreCase) >= 0 ||
                raw.IndexOf("Func`", StringComparison.OrdinalIgnoreCase) >= 0 ||
                raw.IndexOf("Predicate", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return;
            }

            AddFilterInfoLine(result, seen, "Filtro interno del grid: " + raw);
        }

        private static string GetFirstStringPropertyValue(object target, params string[] propertyNames)
        {
            if (target == null || propertyNames == null)
                return null;

            foreach (string propertyName in propertyNames)
            {
                string value = GetStringPropertyValue(target, propertyName);

                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return null;
        }

        private static string NormalizeFilterName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            string result = Regex.Replace(name.Trim(), "([a-z])([A-Z])", "$1 $2");
            result = result.Replace("_", " ");
            return result;
        }

        private static string NormalizeFilterValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            value = System.Net.WebUtility.HtmlDecode(value);
            value = Regex.Replace(value, @"\s+", " ").Trim();

            if (string.Equals(value, "{}", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "[]", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            return value;
        }

        private static void AddFilterInfoLine(List<string> result, HashSet<string> seen, string line)
        {
            if (result == null || seen == null || string.IsNullOrWhiteSpace(line))
                return;

            line = NormalizeFilterValue(line);
            line = Regex.Replace(line, @"\s*:+\s*", ": ").Trim();

            if (string.IsNullOrWhiteSpace(line))
                return;

            if (seen.Contains(line))
                return;

            seen.Add(line);
            result.Add(line);
        }

        private int GetApproximateExportedRecordCount()
        {
            try
            {
                if (grid == null || grid.View == null)
                    return 0;

                object records = GetObjectPropertyValue(grid.View, "Records");
                if (records != null)
                {
                    object countValue = GetObjectPropertyValue(records, "Count");
                    if (countValue != null)
                        return Convert.ToInt32(countValue);
                }

                System.Collections.ICollection collection = grid.View as System.Collections.ICollection;
                if (collection != null)
                    return collection.Count;
            }
            catch
            {
            }

            return 0;
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

        private static string GetScalingDisplayNameForInfo(GenesysPdfScalingMode scalingMode)
        {
            switch (scalingMode)
            {
                case GenesysPdfScalingMode.FitSheetOnOnePage:
                    return "ajustar hoja a una página";

                case GenesysPdfScalingMode.FitAllColumnsOnOnePage:
                    return "ajustar columnas a una página";

                case GenesysPdfScalingMode.FitAllRowsOnOnePage:
                    return "ajustar filas a una página";

                default:
                    return "sin escalado";
            }
        }

        private string BuildExportDialogInfo(string extension, GenesysGridExportSettings settings)
        {
            if (!string.Equals(extension, "pdf", StringComparison.OrdinalIgnoreCase) || settings == null)
                return string.Empty;

            return "Salida PDF: " + GetPdfPaperWithOrientationDisplayName(settings) +
                   " | Escalado: " + GetPdfScalingDisplayNameForInfo(settings.PdfScalingMode) +
                   " | Columnas: " + (settings.PdfOptions != null && settings.PdfOptions.AutoColumnWidth ? "auto ancho" : "anchos de la vista");
        }

        private static string GetPaperDisplayName(GenesysGridExportSettings settings)
        {
            if (settings == null)
                return "Carta";

            return GetPaperDisplayName(settings.PdfPaperMode);
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

                case GenesysPdfPaperMode.AutomaticByColumns:
                    return "Automático";

                default:
                    return "Carta";
            }
        }

        private string GetPdfPaperWithOrientationDisplayName(GenesysGridExportSettings settings)
        {
            if (settings == null)
                return "Carta";

            string paper = GetPaperDisplayName(settings.PdfPaperMode);

            // La orientación mostrada debe reflejar la salida efectiva.
            // En algunos modos de papel el enum conserva sufijo Landscape y el tamaño final
            // puede quedar horizontal aunque settings.PdfOrientation venga Portrait por persistencia.
            SizeF pageSize = GetPdfPageSize(settings);
            string orientation = pageSize.Width >= pageSize.Height
                ? "horizontal"
                : "vertical";

            if (string.IsNullOrWhiteSpace(paper))
                paper = "Carta";

            return paper + " " + orientation;
        }

        private static string NormalizeExtensionForFileName(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return string.Empty;

            extension = extension.Trim();
            if (extension.StartsWith("."))
                extension = extension.Substring(1);

            return extension.ToLowerInvariant();
        }

        private static string RemoveDateTimeFromDefaultFileName(string fileName, string extension)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return fileName;

            string normalizedExtension = NormalizeExtensionForFileName(extension);
            string ext = Path.GetExtension(fileName);
            string name = Path.GetFileNameWithoutExtension(fileName);

            if (string.IsNullOrWhiteSpace(name))
                return fileName;

            // Patrones comunes: Reporte_20260616_153012, Reporte-2026-06-16-15-30-12,
            // Reporte 2026-06-16 15-30, etc.
            string cleaned = System.Text.RegularExpressions.Regex.Replace(
                name,
                @"([ _\-\.]+)?(20\d{2}[\-\.]?\d{2}[\-\.]?\d{2})([ _\-\.]?\d{2}[\-\.]?\d{2}([\-\.]?\d{2})?)?$",
                string.Empty);

            cleaned = cleaned.Trim(' ', '_', '-', '.');

            if (string.IsNullOrWhiteSpace(cleaned))
                cleaned = name;

            if (string.IsNullOrWhiteSpace(ext))
                ext = string.IsNullOrWhiteSpace(normalizedExtension) ? string.Empty : "." + normalizedExtension;

            return cleaned + ext;
        }

        private static string AppendViewNameToDefaultFileName(string fileName, string extension, GenesysGridExportSettings settings)
        {
            if (string.IsNullOrWhiteSpace(fileName) || settings == null || settings.CurrentViewLayout == null)
                return fileName;

            string viewName = settings.CurrentViewLayout.ViewName;
            if (string.IsNullOrWhiteSpace(viewName))
                return fileName;

            string ext = Path.GetExtension(fileName);
            string name = Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrWhiteSpace(name))
                return fileName;

            string safeViewName = GetSafeFileNamePart(viewName);
            if (string.IsNullOrWhiteSpace(safeViewName))
                return fileName;

            if (name.IndexOf(safeViewName, StringComparison.OrdinalIgnoreCase) >= 0)
                return fileName;

            if (string.IsNullOrWhiteSpace(ext))
            {
                string normalizedExtension = NormalizeExtensionForFileName(extension);
                ext = string.IsNullOrWhiteSpace(normalizedExtension) ? string.Empty : "." + normalizedExtension;
            }

            return name + " - " + safeViewName + ext;
        }

        private static string GetSafeFileNamePart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string result = value.Trim();

            foreach (char invalid in Path.GetInvalidFileNameChars())
                result = result.Replace(invalid, '_');

            while (result.Contains("  "))
                result = result.Replace("  ", " ");

            return result.Trim();
        }

        private GenesysExportFileDialogResult ShowExportFileDialog(
            string title,
            string extension,
            string defaultFileName,
            GenesysGridExportSettings settings)
        {
            string persistenceKey = BuildPersistenceKey(settings, extension);
            GenesysExportDialogState persisted = GenesysExportDialogStateStore.Load(persistenceKey);

            ApplyPersistedPdfDialogStateToSettings(extension, settings, persisted);
            ApplyPersistedExcelDialogStateToSettings(extension, settings, persisted);

            string fileName = Path.GetFileName(defaultFileName);
            fileName = RemoveDateTimeFromDefaultFileName(fileName, extension);
            fileName = AppendViewNameToDefaultFileName(fileName, extension, settings);
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
                openFolder,
                BuildExportDialogInfo(extension, settings),
                settings))
            {
                if (dialog.ShowDialog(owner) != DialogResult.OK)
                    return null;

                ApplyPdfDialogOptionsToSettings(extension, settings, dialog);
                ApplyExcelDialogOptionsToSettings(extension, settings, dialog);

                GenesysExportFileDialogResult result = new GenesysExportFileDialogResult();
                result.Accepted = true;
                result.FilePath = dialog.FilePath;
                result.OpenFileAfterExport = dialog.OpenFileAfterExport;
                result.OpenFolderAfterExport = dialog.OpenFolderAfterExport;

                GenesysExportDialogState state = new GenesysExportDialogState();
                state.LastFolder = Path.GetDirectoryName(result.FilePath);
                state.OpenFileAfterExport = result.OpenFileAfterExport;
                state.OpenFolderAfterExport = result.OpenFolderAfterExport;
                CapturePdfDialogState(extension, settings, state);
                CaptureExcelDialogState(extension, settings, state);
                GenesysExportDialogStateStore.Save(persistenceKey, state);

                return result;
            }
        }

        private static string RemoveGeneratedTimestampFromFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return fileName;

            try
            {
                string name = Path.GetFileNameWithoutExtension(fileName);
                string extension = Path.GetExtension(fileName);

                // El formulario puede mandar nombres como Reporte_20260616_153012 o Reporte-2026-06-16-15-30-12.
                name = Regex.Replace(name, @"([_\-\s])\d{8}[_\-]\d{6}$", string.Empty);
                name = Regex.Replace(name, @"([_\-\s])\d{4}[_\-]\d{2}[_\-]\d{2}[_\-\s]\d{2}[_\-]\d{2}[_\-]\d{2}$", string.Empty);

                return string.IsNullOrWhiteSpace(name) ? fileName : name + extension;
            }
            catch
            {
                return fileName;
            }
        }

        private string BuildPersistenceKey(GenesysGridExportSettings settings, string extension)
        {
            string baseKey = settings == null ? null : settings.ExportDialogPersistenceKey;

            if (string.IsNullOrWhiteSpace(baseKey))
                baseKey = owner == null ? "Genesys.Export" : owner.GetType().FullName;

            string normalizedExtension = NormalizeExportPersistenceExtension(extension);
            string viewKey = GetExportPersistenceViewKey(settings);

            // Nueva granularidad de persistencia:
            // Formulario + Vista + TipoExportacion.
            // Si el formulario ya incluyó el nombre de la vista en ExportDialogPersistenceKey,
            // no se duplica para conservar compatibilidad con formularios existentes.
            if (!string.IsNullOrWhiteSpace(viewKey) && !PersistenceKeyAlreadyContainsView(baseKey, viewKey))
                return baseKey + "." + viewKey + "." + normalizedExtension;

            return baseKey + "." + normalizedExtension;
        }

        private static string NormalizeExportPersistenceExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return "export";

            extension = extension.Trim().TrimStart('.');

            return string.IsNullOrWhiteSpace(extension)
                ? "export"
                : GetSafePersistenceKeyPart(extension.ToLowerInvariant());
        }

        private static string GetExportPersistenceViewKey(GenesysGridExportSettings settings)
        {
            if (settings == null || settings.CurrentViewLayout == null)
                return null;

            string viewName = settings.CurrentViewLayout.ViewName;

            if (string.IsNullOrWhiteSpace(viewName))
                return null;

            viewName = viewName.Trim();

            if (string.Equals(viewName, "Vista activa del grid", StringComparison.OrdinalIgnoreCase))
                return null;

            return GetSafePersistenceKeyPart(viewName);
        }

        private static bool PersistenceKeyAlreadyContainsView(string baseKey, string viewKey)
        {
            if (string.IsNullOrWhiteSpace(baseKey) || string.IsNullOrWhiteSpace(viewKey))
                return false;

            string normalizedBase = NormalizePersistenceKeyForComparison(baseKey);
            string normalizedView = NormalizePersistenceKeyForComparison(viewKey);

            if (string.IsNullOrWhiteSpace(normalizedBase) || string.IsNullOrWhiteSpace(normalizedView))
                return false;

            return normalizedBase.EndsWith("." + normalizedView, StringComparison.OrdinalIgnoreCase) ||
                   normalizedBase.EndsWith("|" + normalizedView, StringComparison.OrdinalIgnoreCase) ||
                   normalizedBase.EndsWith("_" + normalizedView, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(normalizedBase, normalizedView, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePersistenceKeyForComparison(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return GetSafePersistenceKeyPart(value).Trim().ToLowerInvariant();
        }

        private static string GetSafePersistenceKeyPart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string result = value.Trim();

            foreach (char invalid in Path.GetInvalidFileNameChars())
                result = result.Replace(invalid, '_');

            result = result.Replace('\\', '_');
            result = result.Replace('/', '_');
            result = result.Replace(':', '_');
            result = result.Replace('*', '_');
            result = result.Replace('?', '_');
            result = result.Replace('"', '_');
            result = result.Replace('<', '_');
            result = result.Replace('>', '_');

            while (result.Contains("  "))
                result = result.Replace("  ", " ");

            while (result.Contains("__"))
                result = result.Replace("__", "_");

            return result.Trim(' ', '_');
        }

        private bool EnsureOutputFileAvailable(string filePath, string title)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            try
            {
                string folder = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrWhiteSpace(folder) && !Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                if (!File.Exists(filePath))
                    return true;

                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    owner,
                    "No se puede generar el archivo porque está abierto o bloqueado por otra aplicación.\r\n\r\n" +
                    "Cierra el archivo y vuelve a intentar.\r\n\r\n" +
                    filePath + "\r\n\r\n" +
                    ex.Message,
                    string.IsNullOrWhiteSpace(title) ? "Exportar" : title,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return false;
            }
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

    }
}
