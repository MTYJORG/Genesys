using Syncfusion.WinForms.DataGrid;
using Syncfusion.WinForms.DataGrid.Enums;
using System.Collections.Generic;
using System.Data;
using System;
using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.GridViews
{
    public static class GenesysGridConfigurator
    {
        public static void Configure(SfDataGrid grid)
        {
            if (grid == null)
                return;

            grid.AllowFiltering = true;
            grid.AllowSorting = true;
            grid.AllowTriStateSorting = true;
            grid.AllowGrouping = true;
            grid.ShowGroupDropArea = true;
            grid.AllowDraggingColumns = true;
            grid.AllowResizingColumns = true;
            grid.AllowResizingHiddenColumns = true;
            grid.AllowEditing = false;
            grid.AllowDeleting = false;
            grid.SelectionMode = GridSelectionMode.Extended;
            grid.NavigationMode = NavigationMode.Row;
            grid.AutoSizeColumnsMode = AutoSizeColumnsMode.AllCells;

            // Importante: NO usar FooterRowCount para summaries.
            // FooterRowCount congela filas reales del DataSource, por eso puede verse
            // el último registro como si fuera una fila de totales. El summary tipo
            // Excel debe vivir en TableSummaryRows.
        }

        public static void BindDataTable(SfDataGrid grid, DataTable table, IDictionary<string, string> numericFormats)
        {
            if (grid == null)
                return;

            Configure(grid);
            grid.SuspendLayout();

            try
            {
                ResetBeforeDataSourceChange(grid);

                grid.AutoGenerateColumns = true;
                grid.DataSource = table;

                ApplyNumericFormats(grid, numericFormats);
            }
            finally
            {
                grid.ResumeLayout(true);
            }
        }

        private static void ResetBeforeDataSourceChange(SfDataGrid grid)
        {
            // Evita excepciones internas de Syncfusion cuando el nuevo DataTable
            // tiene menos columnas/filas y el grid conserva índices visuales antiguos.
            try
            {
                if (grid.TableSummaryRows != null)
                    grid.TableSummaryRows.Clear();
            }
            catch
            {
            }

            try
            {
                if (grid.GroupColumnDescriptions != null)
                    grid.GroupColumnDescriptions.Clear();
            }
            catch
            {
            }

            try
            {
                if (grid.SortColumnDescriptions != null)
                    grid.SortColumnDescriptions.Clear();
            }
            catch
            {
            }

            try
            {
                grid.DataSource = null;
            }
            catch
            {
            }

            try
            {
                grid.Columns.Clear();
            }
            catch
            {
            }
        }

        public static void ApplyNumericFormats(SfDataGrid grid, IDictionary<string, string> numericFormats)
        {
            if (grid == null || numericFormats == null)
                return;

            foreach (var item in numericFormats)
                ApplyNumericFormat(grid, item.Key, item.Value);
        }

        public static void ApplyNumericFormat(SfDataGrid grid, string mappingName, string format)
        {
            if (grid == null || string.IsNullOrWhiteSpace(mappingName))
                return;

            GridColumn column = null;

            foreach (GridColumn currentColumn in grid.Columns)
            {
                if (string.Equals(currentColumn.MappingName, mappingName, StringComparison.OrdinalIgnoreCase))
                {
                    column = currentColumn;
                    break;
                }
            }

            if (column == null)
                return;
            column.Format = format;
            column.CellStyle.HorizontalAlignment = HorizontalAlignment.Right;
            column.HeaderStyle.HorizontalAlignment = HorizontalAlignment.Right;
        }
    }
}
