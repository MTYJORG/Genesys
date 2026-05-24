using Syncfusion.Data;
using Syncfusion.WinForms.DataGrid;
using Syncfusion.WinForms.DataGrid.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.GridViews
{
    public class GenesysGridSummaryService
    {
        private readonly SfDataGrid grid;

        public GenesysGridSummaryService(SfDataGrid grid)
        {
            this.grid = grid;
        }

        public void Capture(GenesysGridViewLayout layout)
        {
            if (layout == null || grid == null || grid.TableSummaryRows == null)
                return;

            foreach (var row in grid.TableSummaryRows)
            {
                var summaryColumns =
                    GetPropertyValue(row, "SummaryColumns") as System.Collections.IEnumerable;

                if (summaryColumns == null)
                    continue;

                foreach (var summaryColumn in summaryColumns)
                {
                    string mappingName = GetPropertyAsString(summaryColumn, "MappingName");
                    string name = GetPropertyAsString(summaryColumn, "Name");
                    string format = GetPropertyAsString(summaryColumn, "Format");

                    if (string.IsNullOrWhiteSpace(mappingName))
                        mappingName = name;

                    if (string.IsNullOrWhiteSpace(mappingName))
                        continue;

                    layout.Summaries.Add(new GenesysGridSummaryLayout
                    {
                        ColumnName = mappingName,
                        SummaryType = DetectSummaryType(format),
                        Format = ExtractFormatFromSummaryFormat(format)
                    });
                }
            }
        }

        public void Apply(GenesysGridViewLayout layout)
        {
            ClearSummaryRows();

            if (layout == null || layout.Summaries == null)
                return;

            foreach (var summary in layout.Summaries)
            {
                if (string.IsNullOrWhiteSpace(summary.ColumnName) ||
                    FindColumn(summary.ColumnName) == null)
                {
                    continue;
                }

                AddOrReplaceSummary(
                    summary.ColumnName,
                    summary.SummaryType,
                    summary.Format);
            }
        }

        public void AddOrReplaceSummary(
            string columnName,
            string summaryType,
            string format)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return;

            GridColumn gridColumn = FindColumn(columnName);
            if (gridColumn == null)
                return;

            string aggregateName = NormalizeSummaryType(summaryType);
            string displayFormat = NormalizeDisplayFormat(aggregateName, format);

            Action apply = delegate
            {
                ClearSummaryForColumnUnsafe(columnName);

                GridTableSummaryRow row = GetOrCreateGenesysSummaryRow();

                row.SummaryColumns.Add(new GridSummaryColumn
                {
                    Name = columnName + "_" + aggregateName,
                    MappingName = columnName,
                    SummaryType = GetSummaryAggregateType(columnName, aggregateName),
                    Format = displayFormat
                });

                RefreshGridView();
            };

            SafeGridMutation(apply);
        }

        public void ClearSummaryForColumn(string columnName)
        {
            SafeGridMutation(delegate
            {
                ClearSummaryForColumnUnsafe(columnName);
                RefreshGridView();
            });
        }

        public void ClearSummaryRows()
        {
            SafeGridMutation(delegate
            {
                if (grid != null && grid.TableSummaryRows != null)
                    grid.TableSummaryRows.Clear();

                RefreshGridView();
            });
        }

        public string GetSummaryTypeForColumn(string columnName)
        {
            if (grid == null || grid.TableSummaryRows == null)
                return "None";

            foreach (var row in grid.TableSummaryRows)
            {
                var summaryColumns =
                    GetPropertyValue(row, "SummaryColumns") as System.Collections.IEnumerable;

                if (summaryColumns == null)
                    continue;

                foreach (var summaryColumn in summaryColumns)
                {
                    string mappingName = GetPropertyAsString(summaryColumn, "MappingName");
                    string format = GetPropertyAsString(summaryColumn, "Format");

                    if (string.Equals(mappingName, columnName, StringComparison.OrdinalIgnoreCase))
                        return DetectSummaryType(format);
                }
            }

            return "None";
        }

        private GridTableSummaryRow GetOrCreateGenesysSummaryRow()
        {
            if (grid.TableSummaryRows != null)
            {
                foreach (GridTableSummaryRow existingRow in grid.TableSummaryRows)
                {
                    if (string.Equals(
                        existingRow.Name,
                        "GenesysSummaryRow",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return existingRow;
                    }
                }
            }

            var row = new GridTableSummaryRow
            {
                Name = "GenesysSummaryRow",
                ShowSummaryInRow = false,
                Position = VerticalPosition.Bottom
            };

            grid.TableSummaryRows.Add(row);
            return row;
        }

        private void ClearSummaryForColumnUnsafe(string columnName)
        {
            if (grid == null ||
                grid.TableSummaryRows == null ||
                grid.TableSummaryRows.Count == 0)
            {
                return;
            }

            for (int i = grid.TableSummaryRows.Count - 1; i >= 0; i--)
            {
                var row = grid.TableSummaryRows[i];

                var summaryColumns =
                    GetPropertyValue(row, "SummaryColumns") as System.Collections.IList;

                if (summaryColumns == null)
                    continue;

                for (int j = summaryColumns.Count - 1; j >= 0; j--)
                {
                    string mappingName =
                        GetPropertyAsString(summaryColumns[j], "MappingName");

                    string name =
                        GetPropertyAsString(summaryColumns[j], "Name");

                    if (string.Equals(
                            mappingName,
                            columnName,
                            StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(
                            name,
                            columnName,
                            StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrWhiteSpace(name) &&
                         name.StartsWith(
                             columnName + "_",
                             StringComparison.OrdinalIgnoreCase)))
                    {
                        summaryColumns.RemoveAt(j);
                    }
                }

                if (summaryColumns.Count == 0)
                    grid.TableSummaryRows.RemoveAt(i);
            }
        }

        private GridColumn FindColumn(string mappingName)
        {
            if (grid == null || string.IsNullOrWhiteSpace(mappingName))
                return null;

            foreach (GridColumn column in grid.Columns)
            {
                if (string.Equals(
                    column.MappingName,
                    mappingName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return column;
                }
            }

            return null;
        }

        private string NormalizeSummaryType(string summaryType)
        {
            if (string.IsNullOrWhiteSpace(summaryType))
                return "Sum";

            if (string.Equals(summaryType, "Average", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(summaryType, "Avg", StringComparison.OrdinalIgnoreCase))
                return "Average";

            if (string.Equals(summaryType, "Minimum", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(summaryType, "Min", StringComparison.OrdinalIgnoreCase))
                return "Min";

            if (string.Equals(summaryType, "Maximum", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(summaryType, "Max", StringComparison.OrdinalIgnoreCase))
                return "Max";

            if (string.Equals(summaryType, "Count", StringComparison.OrdinalIgnoreCase))
                return "Count";

            return "Sum";
        }

        private string NormalizeDisplayFormat(
            string aggregateName,
            string numericFormat)
        {
            if (string.Equals(aggregateName, "Count", StringComparison.OrdinalIgnoreCase))
                return "{Count}";

            string format = string.IsNullOrWhiteSpace(numericFormat)
                ? "N2"
                : numericFormat;

            return "{" + aggregateName + ":" + format + "}";
        }

        private string DetectSummaryType(string summaryFormat)
        {
            if (string.IsNullOrWhiteSpace(summaryFormat))
                return "Sum";

            if (summaryFormat.IndexOf("Average", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Average";

            if (summaryFormat.IndexOf("Minimum", StringComparison.OrdinalIgnoreCase) >= 0 ||
                summaryFormat.IndexOf("Min", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Min";

            if (summaryFormat.IndexOf("Maximum", StringComparison.OrdinalIgnoreCase) >= 0 ||
                summaryFormat.IndexOf("Max", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Max";

            if (summaryFormat.IndexOf("Count", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Count";

            return "Sum";
        }

        private string ExtractFormatFromSummaryFormat(string summaryFormat)
        {
            if (string.IsNullOrWhiteSpace(summaryFormat))
                return "N2";

            int colon = summaryFormat.IndexOf(':');
            int close = summaryFormat.IndexOf('}', colon + 1);

            if (colon >= 0 && close > colon)
                return summaryFormat.Substring(colon + 1, close - colon - 1);

            return "N2";
        }

        private SummaryType GetSummaryAggregateType(
            string columnName,
            string aggregateName)
        {
            if (string.Equals(aggregateName, "Count", StringComparison.OrdinalIgnoreCase))
                return SummaryType.CountAggregate;

            Type dataType = GetColumnDataType(columnName);

            if (dataType == typeof(byte) ||
                dataType == typeof(short) ||
                dataType == typeof(int) ||
                dataType == typeof(long) ||
                dataType == typeof(byte?) ||
                dataType == typeof(short?) ||
                dataType == typeof(int?) ||
                dataType == typeof(long?))
            {
                return SummaryType.Int32Aggregate;
            }

            return SummaryType.DoubleAggregate;
        }

        private Type GetColumnDataType(string columnName)
        {
            try
            {
                var dataView = grid.DataSource as System.Data.DataView;

                if (dataView != null &&
                    dataView.Table != null &&
                    dataView.Table.Columns.Contains(columnName))
                {
                    return dataView.Table.Columns[columnName].DataType;
                }

                var dataTable = grid.DataSource as System.Data.DataTable;

                if (dataTable != null && dataTable.Columns.Contains(columnName))
                    return dataTable.Columns[columnName].DataType;
            }
            catch
            {
            }

            return typeof(double);
        }

        private void SafeGridMutation(Action action)
        {
            if (action == null)
                return;

            try
            {
                action();
            }
            catch (ArgumentOutOfRangeException)
            {
                DeferGridMutation(action);
            }
            catch (InvalidOperationException)
            {
                DeferGridMutation(action);
            }
        }

        private void DeferGridMutation(Action action)
        {
            if (grid == null || grid.IsDisposed || !grid.IsHandleCreated)
                return;

            grid.BeginInvoke(new MethodInvoker(delegate
            {
                try
                {
                    action();
                    RefreshGridView();
                }
                catch
                {
                    // Se evita romper la pantalla por estados transitorios internos del grid.
                    // La siguiente carga/aplicación de vista volverá a sincronizar el estado.
                }
            }));
        }

        private void RefreshGridView()
        {
            try
            {
                if (grid != null && grid.View != null)
                    grid.View.Refresh();
            }
            catch
            {
            }

            try
            {
                if (grid != null)
                {
                    grid.Refresh();
                    grid.Invalidate();
                }
            }
            catch
            {
            }
        }

        private object GetPropertyValue(object instance, string propertyName)
        {
            if (instance == null || string.IsNullOrWhiteSpace(propertyName))
                return null;

            var property = instance.GetType().GetProperty(propertyName);
            if (property == null)
                return null;

            return property.GetValue(instance, null);
        }

        private string GetPropertyAsString(object instance, string propertyName)
        {
            object value = GetPropertyValue(instance, propertyName);
            return value == null ? null : Convert.ToString(value);
        }
    }
}
