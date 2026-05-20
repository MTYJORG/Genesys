using Syncfusion.Data;
using Syncfusion.WinForms.DataGrid;
using Syncfusion.WinForms.DataGrid.Events;
using Syncfusion.WinForms.DataGrid.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.GridViews
{
    public class GenesysGridViewManager : IDisposable
    {
        private const string DefaultViewName = "Predeterminada";

        private readonly Form owner;
        private readonly SfDataGrid grid;
        private readonly ToolStripButton button;
        private readonly string gridKey;
        private readonly IGenesysGridViewStore store;
        private readonly IGenesysGridViewStateStore stateStore;
        private readonly Dictionary<string, string> numericFormats;
        private string currentViewName;
        private bool isApplyingLayout;
        private bool hasChanges;
        private ContextMenuStrip currentMenu;

        public GenesysGridViewManager(Form owner, SfDataGrid grid, ToolStripButton button, string gridKey)
            : this(owner, grid, button, gridKey, new GenesysGridViewFileStore())
        {
        }

        public GenesysGridViewManager(Form owner, SfDataGrid grid, ToolStripButton button, string gridKey, IGenesysGridViewStore store)
        {
            this.owner = owner;
            this.grid = grid;
            this.button = button;
            this.gridKey = gridKey;
            this.store = store;
            stateStore = store as IGenesysGridViewStateStore;
            numericFormats = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            currentViewName = DefaultViewName;
        }

        public bool HasChanges
        {
            get { return hasChanges; }
        }

        public string CurrentViewName
        {
            get { return currentViewName; }
        }

        public bool IsCurrentViewDefault
        {
            get { return IsDefaultView(currentViewName); }
        }

        public void Initialize()
        {
            GenesysGridConfigurator.Configure(grid);

            if (button != null)
                button.ToolTipText = "Vistas del grid";

            RestoreCurrentViewName();
            HookGridEvents();
            UpdateButtonState();
        }

        public void SetNumericFormat(string columnName, string format)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return;

            numericFormats[columnName] = format;
            GenesysGridConfigurator.ApplyNumericFormat(grid, columnName, format);
        }

        public void ApplyNumericFormats()
        {
            GenesysGridConfigurator.ApplyNumericFormats(grid, numericFormats);
        }

        private void RestoreCurrentViewName()
        {
            if (stateStore == null)
                return;

            string savedViewName = stateStore.LoadCurrentViewName(gridKey);
            if (!string.IsNullOrWhiteSpace(savedViewName))
                currentViewName = savedViewName;
        }

        public void PersistCurrentViewName()
        {
            if (stateStore != null)
                stateStore.SaveCurrentViewName(gridKey, currentViewName);
        }

        public void ReapplyCurrentView()
        {
            if (IsDefaultView(currentViewName))
            {
                ApplyDefaultLayout();
                return;
            }

            var views = store.Load(gridKey);
            var layout = views.FirstOrDefault(x =>
                string.Equals(x.ViewName, currentViewName, StringComparison.OrdinalIgnoreCase));

            if (layout != null)
                ApplyLayout(layout);
            else
                ApplyDefaultLayout();
        }

        public void MarkClean()
        {
            hasChanges = false;
            UpdateButtonState();
        }

        public void MarkChanged()
        {
            if (isApplyingLayout)
                return;

            hasChanges = true;
            UpdateButtonState();
        }

        public void ShowMenu()
        {
            if (currentMenu != null && !currentMenu.IsDisposed)
            {
                currentMenu.Close();
                currentMenu.Items.Clear();
            }
            else
            {
                currentMenu = new ContextMenuStrip();
            }

            ContextMenuStrip menu = currentMenu;
            menu.ShowImageMargin = false;

            var views = store.Load(gridKey);

            ToolStripMenuItem title = new ToolStripMenuItem("Vistas");
            title.Enabled = false;
            menu.Items.Add(title);

            ToolStripMenuItem defaultItem = new ToolStripMenuItem(DefaultViewName);
            defaultItem.Checked = IsDefaultView(currentViewName);
            defaultItem.Click += delegate { ApplyDefaultLayout(); };
            menu.Items.Add(defaultItem);

            if (views.Count > 0)
            {
                menu.Items.Add(new ToolStripSeparator());

                foreach (var view in views.OrderBy(x => x.ViewName))
                {
                    var item = new ToolStripMenuItem(view.ViewName);
                    item.Checked = string.Equals(view.ViewName, currentViewName, StringComparison.OrdinalIgnoreCase);
                    item.Tag = view;
                    item.Click += delegate(object s, EventArgs e)
                    {
                        ApplyLayout((GenesysGridViewLayout)((ToolStripMenuItem)s).Tag);
                    };
                    menu.Items.Add(item);
                }
            }

            menu.Items.Add(new ToolStripSeparator());

            string saveText = IsDefaultView(currentViewName)
                ? "Guardar como nueva vista..."
                : "Guardar cambios en \"" + currentViewName + "\"";

            var save = new ToolStripMenuItem(saveText);
            save.Click += delegate { SaveCurrentOrAsk(); };
            menu.Items.Add(save);

            var saveAs = new ToolStripMenuItem("Guardar como nueva vista...");
            saveAs.Click += delegate { SaveAsNewView(); };
            menu.Items.Add(saveAs);

            var duplicate = new ToolStripMenuItem("Duplicar vista...");
            duplicate.Enabled = !IsDefaultView(currentViewName);
            duplicate.Click += delegate { DuplicateView(); };
            menu.Items.Add(duplicate);

            var deleteView = new ToolStripMenuItem(IsDefaultView(currentViewName)
                ? "Eliminar vista..."
                : "Eliminar vista \"" + currentViewName + "\"...");
            deleteView.Enabled = views.Count > 0;
            deleteView.Click += delegate { DeleteViewFromMenu(); };
            menu.Items.Add(deleteView);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem summaries = new ToolStripMenuItem("Summary row");

            var addSummary = new ToolStripMenuItem("Agregar / editar summary...");
            addSummary.Click += delegate { ShowSummaryEditor(); };
            summaries.DropDownItems.Add(addSummary);

            GridColumn currentColumn = GetCurrentColumn();
            if (currentColumn != null && !string.IsNullOrWhiteSpace(currentColumn.MappingName))
            {
                summaries.DropDownItems.Add(new ToolStripSeparator());
                summaries.DropDownItems.Add(CreateSummaryMenuItem("Suma de " + currentColumn.HeaderText, "Sum"));
                summaries.DropDownItems.Add(CreateSummaryMenuItem("Promedio de " + currentColumn.HeaderText, "Average"));
                summaries.DropDownItems.Add(CreateSummaryMenuItem("Conteo de " + currentColumn.HeaderText, "Count"));
                summaries.DropDownItems.Add(CreateSummaryMenuItem("Mínimo de " + currentColumn.HeaderText, "Minimum"));
                summaries.DropDownItems.Add(CreateSummaryMenuItem("Máximo de " + currentColumn.HeaderText, "Maximum"));
            }

            summaries.DropDownItems.Add(new ToolStripSeparator());
            var clearCurrentSummary = new ToolStripMenuItem("Quitar summary de columna actual");
            clearCurrentSummary.Enabled = currentColumn != null && !string.IsNullOrWhiteSpace(currentColumn.MappingName);
            clearCurrentSummary.Click += delegate
            {
                GridColumn column = GetCurrentColumn();
                if (column != null && !string.IsNullOrWhiteSpace(column.MappingName))
                {
                    ClearSummaryForColumn(column.MappingName);
                    MarkChanged();
                }
            };
            summaries.DropDownItems.Add(clearCurrentSummary);

            var clearSummaries = new ToolStripMenuItem("Quitar todos los summaries");
            clearSummaries.Click += delegate { ClearSummaryRows(); MarkChanged(); };
            summaries.DropDownItems.Add(clearSummaries);
            menu.Items.Add(summaries);

            var designer = new ToolStripMenuItem("Diseñar vista...");
            designer.Click += delegate { ToggleDesigner(); };
            menu.Items.Add(designer);

            menu.Items.Add(new ToolStripSeparator());

            var restore = new ToolStripMenuItem("Restaurar vista predeterminada");
            restore.Click += delegate { ApplyDefaultLayout(); };
            menu.Items.Add(restore);

            ShowContextMenu(menu);
        }

        private ToolStripMenuItem CreateSummaryMenuItem(string text, string summaryType)
        {
            var item = new ToolStripMenuItem(text);
            item.Click += delegate
            {
                GridColumn column = GetCurrentColumn();

                if (column == null || string.IsNullOrWhiteSpace(column.MappingName))
                {
                    MessageBox.Show(owner, "Selecciona una celda de la columna que quieres resumir.", "Summary row", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string format = null;
                numericFormats.TryGetValue(column.MappingName, out format);

                if (string.Equals(summaryType, "Count", StringComparison.OrdinalIgnoreCase))
                    format = "N0";

                AddOrReplaceSummary(column.MappingName, summaryType, string.IsNullOrWhiteSpace(format) ? "N2" : format);
                MarkChanged();
            };

            return item;
        }

        private void ShowSummaryEditor()
        {
            using (var dialog = new GenesysGridSummaryPrompt(grid, numericFormats))
            {
                if (dialog.ShowDialog(owner) != DialogResult.OK)
                    return;

                AddOrReplaceSummary(dialog.ColumnName, dialog.SummaryTypeName, dialog.NumericFormat);
                MarkChanged();
            }
        }

        private void ShowContextMenu(ContextMenuStrip menu)
        {
            if (button != null)
            {
                ToolStrip ownerStrip = button.GetCurrentParent();

                if (ownerStrip != null)
                {
                    menu.Show(ownerStrip, new Point(button.Bounds.Left, button.Bounds.Bottom));
                    return;
                }
            }

            if (owner != null)
            {
                Point point = owner.PointToClient(Cursor.Position);
                menu.Show(owner, point);
            }
        }

        private bool SaveCurrentView(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName) || IsDefaultView(viewName))
                return SaveAsNewView();

            var layout = CaptureLayout(viewName);
            store.Save(layout);
            currentViewName = viewName;
            hasChanges = false;
            PersistCurrentViewName();
            UpdateButtonState();
            return true;
        }

        private bool SaveAsNewView()
        {
            string name = GenesysGridViewPrompt.Ask("Nueva vista", "Nombre de la vista:", IsDefaultView(currentViewName) ? string.Empty : currentViewName);

            if (string.IsNullOrWhiteSpace(name))
                return false;

            return SaveCurrentView(name);
        }

        private void DuplicateView()
        {
            string baseName = IsDefaultView(currentViewName) ? "Nueva vista" : currentViewName + " copia";
            string name = GenesysGridViewPrompt.Ask("Duplicar vista", "Nuevo nombre:", baseName);

            if (string.IsNullOrWhiteSpace(name))
                return;

            SaveCurrentView(name);
        }

        private void DeleteViewFromMenu()
        {
            IList<GenesysGridViewLayout> views = store.Load(gridKey);

            if (views == null || views.Count == 0)
            {
                MessageBox.Show(owner, "No hay vistas guardadas para eliminar.", "Eliminar vista", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string viewName = currentViewName;

            if (IsDefaultView(viewName))
                viewName = GenesysGridViewPrompt.Ask("Eliminar vista", "Nombre de la vista a eliminar:", string.Empty);

            if (string.IsNullOrWhiteSpace(viewName) || IsDefaultView(viewName))
                return;

            bool exists = views.Any(x => string.Equals(x.ViewName, viewName, StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                MessageBox.Show(owner, "No se encontró la vista '" + viewName + "'.", "Eliminar vista", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DeleteView(viewName);
        }

        private void DeleteView(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName) || IsDefaultView(viewName))
                return;

            DialogResult result = MessageBox.Show(
                owner,
                "¿Deseas eliminar la vista '" + viewName + "'? Esta acción no se puede deshacer.",
                "Eliminar vista",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            store.Delete(gridKey, viewName);

            if (string.Equals(currentViewName, viewName, StringComparison.OrdinalIgnoreCase))
            {
                ApplyDefaultLayout();
                return;
            }

            UpdateButtonState();
        }

        private void ApplyDefaultLayout()
        {
            isApplyingLayout = true;

            try
            {
                ClearGroupDescriptions();
                ClearSortDescriptions();
                ClearSummaryRows();

                foreach (GridColumn column in grid.Columns)
                {
                    column.Visible = true;
                }

                grid.AutoSizeColumnsMode = Syncfusion.WinForms.DataGrid.Enums.AutoSizeColumnsMode.AllCells;
                currentViewName = DefaultViewName;
                hasChanges = false;
                PersistCurrentViewName();
            }
            finally
            {
                isApplyingLayout = false;
                ApplyNumericFormats();
                UpdateButtonState();
            }
        }

        private GenesysGridViewLayout CaptureLayout(string viewName)
        {
            var layout = new GenesysGridViewLayout
            {
                GridKey = gridKey,
                ViewName = viewName,
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now
            };

            int index = 0;

            foreach (GridColumn column in grid.Columns)
            {
                layout.Columns.Add(new GenesysGridColumnLayout
                {
                    MappingName = column.MappingName,
                    HeaderText = column.HeaderText,
                    DisplayIndex = index,
                    Width = column.Width,
                    Visible = column.Visible,
                    Format = column.Format
                });

                index++;
            }

            CaptureGroups(layout);
            CaptureSummaries(layout);

            return layout;
        }

        private void ApplyLayout(GenesysGridViewLayout layout)
        {
            if (layout == null)
                return;

            isApplyingLayout = true;

            try
            {
                ClearGroupDescriptions();
                ClearSortDescriptions();
                ClearSummaryRows();

                foreach (var savedColumn in layout.Columns.OrderBy(x => x.DisplayIndex))
                {
                    if (string.IsNullOrWhiteSpace(savedColumn.MappingName))
                        continue;

                    GridColumn column = FindColumn(savedColumn.MappingName);
                    if (column == null)
                        continue;

                    column.Visible = savedColumn.Visible;

                    if (savedColumn.Width > 0)
                        column.Width = savedColumn.Width;

                    if (!string.IsNullOrWhiteSpace(savedColumn.Format))
                        column.Format = savedColumn.Format;
                }

                ReorderColumns(layout);
                ApplyGroups(layout);
                ApplySummaries(layout);

                currentViewName = layout.ViewName;
                hasChanges = false;
                PersistCurrentViewName();
            }
            finally
            {
                isApplyingLayout = false;
                ApplyNumericFormats();
                UpdateButtonState();
            }
        }

        private void ReorderColumns(GenesysGridViewLayout layout)
        {
            int targetIndex = 0;

            foreach (var savedColumn in layout.Columns.OrderBy(x => x.DisplayIndex))
            {
                if (string.IsNullOrWhiteSpace(savedColumn.MappingName))
                    continue;

                GridColumn column = FindColumn(savedColumn.MappingName);
                if (column == null)
                    continue;

                int currentIndex = grid.Columns.IndexOf(column);

                if (currentIndex >= 0 && currentIndex != targetIndex)
                    grid.Columns.Move(currentIndex, targetIndex);

                targetIndex++;
            }
        }

        private void CaptureGroups(GenesysGridViewLayout layout)
        {
            if (grid.GroupColumnDescriptions == null)
                return;

            foreach (var group in grid.GroupColumnDescriptions)
            {
                string columnName = GetPropertyAsString(group, "ColumnName");

                if (!string.IsNullOrWhiteSpace(columnName))
                    layout.Groups.Add(new GenesysGridGroupLayout { ColumnName = columnName });
            }
        }

        private void ApplyGroups(GenesysGridViewLayout layout)
        {
            Action apply = delegate
            {
                ClearGroupDescriptionsUnsafe();

                if (layout.Groups == null)
                    return;

                foreach (var group in layout.Groups)
                {
                    if (string.IsNullOrWhiteSpace(group.ColumnName) || FindColumn(group.ColumnName) == null)
                        continue;

                    grid.GroupColumnDescriptions.Add(new GroupColumnDescription
                    {
                        ColumnName = group.ColumnName
                    });
                }
            };

            SafeGridMutation(apply);
        }

        private void CaptureSummaries(GenesysGridViewLayout layout)
        {
            if (grid.TableSummaryRows == null)
                return;

            foreach (var row in grid.TableSummaryRows)
            {
                var summaryColumns = GetPropertyValue(row, "SummaryColumns") as System.Collections.IEnumerable;

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

        private void ApplySummaries(GenesysGridViewLayout layout)
        {
            ClearSummaryRows();

            if (layout.Summaries == null)
                return;

            foreach (var summary in layout.Summaries)
            {
                if (string.IsNullOrWhiteSpace(summary.ColumnName) || FindColumn(summary.ColumnName) == null)
                    continue;

                AddOrReplaceSummary(summary.ColumnName, summary.SummaryType, summary.Format);
            }
        }

        public void AddOrReplaceSummary(string columnName, string summaryType, string format)
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

        private GridTableSummaryRow GetOrCreateGenesysSummaryRow()
        {
            if (grid.TableSummaryRows != null)
            {
                foreach (GridTableSummaryRow existingRow in grid.TableSummaryRows)
                {
                    if (string.Equals(existingRow.Name, "GenesysSummaryRow", StringComparison.OrdinalIgnoreCase))
                        return existingRow;
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

        public void ClearSummaryForColumn(string columnName)
        {
            SafeGridMutation(delegate
            {
                ClearSummaryForColumnUnsafe(columnName);
                RefreshGridView();
            });
        }

        private void ClearSummaryForColumnUnsafe(string columnName)
        {
            if (grid.TableSummaryRows == null || grid.TableSummaryRows.Count == 0)
                return;

            for (int i = grid.TableSummaryRows.Count - 1; i >= 0; i--)
            {
                var row = grid.TableSummaryRows[i];
                var summaryColumns = GetPropertyValue(row, "SummaryColumns") as System.Collections.IList;

                if (summaryColumns == null)
                    continue;

                for (int j = summaryColumns.Count - 1; j >= 0; j--)
                {
                    string mappingName = GetPropertyAsString(summaryColumns[j], "MappingName");
                    string name = GetPropertyAsString(summaryColumns[j], "Name");

                    if (string.Equals(mappingName, columnName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrWhiteSpace(name) && name.StartsWith(columnName + "_", StringComparison.OrdinalIgnoreCase)))
                    {
                        summaryColumns.RemoveAt(j);
                    }
                }

                if (summaryColumns.Count == 0)
                    grid.TableSummaryRows.RemoveAt(i);
            }
        }

        private void ClearGroupDescriptions()
        {
            SafeGridMutation(ClearGroupDescriptionsUnsafe);
        }

        private void ClearGroupDescriptionsUnsafe()
        {
            if (grid.GroupColumnDescriptions == null)
                return;

            for (int i = grid.GroupColumnDescriptions.Count - 1; i >= 0; i--)
                grid.GroupColumnDescriptions.RemoveAt(i);
        }

        private void ClearSortDescriptions()
        {
            if (grid.SortColumnDescriptions != null)
                grid.SortColumnDescriptions.Clear();
        }

        public void ClearSummaryRows()
        {
            SafeGridMutation(delegate
            {
                if (grid.TableSummaryRows != null)
                    grid.TableSummaryRows.Clear();

                RefreshGridView();
            });
        }

        private GridColumn GetCurrentColumn()
        {
            try
            {
                var currentCell = grid.CurrentCell;
                if (currentCell == null)
                    return null;

                int columnIndex = currentCell.ColumnIndex;

                if (columnIndex < 0 || columnIndex >= grid.Columns.Count)
                    return null;

                return grid.Columns[columnIndex];
            }
            catch
            {
                return null;
            }
        }

        private GridColumn FindColumn(string mappingName)
        {
            if (string.IsNullOrWhiteSpace(mappingName))
                return null;

            foreach (GridColumn column in grid.Columns)
            {
                if (string.Equals(column.MappingName, mappingName, StringComparison.OrdinalIgnoreCase))
                    return column;
            }

            return null;
        }

        private bool IsDefaultView(string viewName)
        {
            return string.IsNullOrWhiteSpace(viewName) ||
                   string.Equals(viewName, DefaultViewName, StringComparison.OrdinalIgnoreCase);
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

        private string NormalizeDisplayFormat(string aggregateName, string numericFormat)
        {
            if (string.Equals(aggregateName, "Count", StringComparison.OrdinalIgnoreCase))
                return "{Count}";

            string format = string.IsNullOrWhiteSpace(numericFormat) ? "N2" : numericFormat;
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

        private SummaryType GetSummaryAggregateType(string columnName, string aggregateName)
        {
            if (string.Equals(aggregateName, "Count", StringComparison.OrdinalIgnoreCase))
                return SummaryType.CountAggregate;

            Type dataType = GetColumnDataType(columnName);

            if (dataType == typeof(byte) || dataType == typeof(short) || dataType == typeof(int) ||
                dataType == typeof(long) || dataType == typeof(byte?) || dataType == typeof(short?) ||
                dataType == typeof(int?) || dataType == typeof(long?))
                return SummaryType.Int32Aggregate;

            return SummaryType.DoubleAggregate;
        }

        private Type GetColumnDataType(string columnName)
        {
            try
            {
                var dataView = grid.DataSource as System.Data.DataView;
                if (dataView != null && dataView.Table != null && dataView.Table.Columns.Contains(columnName))
                    return dataView.Table.Columns[columnName].DataType;

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
                if (grid.View != null)
                    grid.View.Refresh();
            }
            catch
            {
            }

            try
            {
                grid.Refresh();
                grid.Invalidate();
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


        public event EventHandler DesignerRequested;

        public void ToggleDesigner()
        {
            var handler = DesignerRequested;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        public bool SaveCurrentOrAsk()
        {
            if (IsDefaultView(currentViewName))
                return SaveAsNewView();

            return SaveCurrentView(currentViewName);
        }

        public bool ConfirmPendingChangesBeforeClose(IWin32Window dialogOwner)
        {
            if (!hasChanges)
                return true;

            DialogResult result = MessageBox.Show(
                dialogOwner,
                "Hay cambios sin guardar en la vista actual.\n\n¿Deseas guardar los cambios antes de cerrar?",
                "Vistas del grid",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Cancel)
                return false;

            if (result == DialogResult.No)
            {
                hasChanges = false;
                UpdateButtonState();
                return true;
            }

            return SaveCurrentOrAsk();
        }

        public IList<GenesysGridColumnProfile> GetColumnProfiles()
        {
            var result = new List<GenesysGridColumnProfile>();

            foreach (GridColumn column in grid.Columns)
            {
                result.Add(GetColumnProfile(column.MappingName));
            }

            return result;
        }

        public GenesysGridColumnProfile GetColumnProfile(string columnName)
        {
            GridColumn column = FindColumn(columnName);
            if (column == null)
                return null;

            string format = column.Format;
            string summaryType = GetSummaryTypeForColumn(column.MappingName);

            return new GenesysGridColumnProfile
            {
                ColumnName = column.MappingName,
                HeaderText = column.HeaderText,
                Visible = column.Visible,
                Width = column.Width,
                Format = format,
                Decimals = ExtractDecimals(format),
                Alignment = DetectAlignment(column),
                SummaryType = string.IsNullOrWhiteSpace(summaryType) ? "None" : summaryType,
                Grouped = IsColumnGrouped(column.MappingName),
                Frozen = IsColumnFrozen(column.MappingName)
            };
        }

        public void ApplyColumnProfile(GenesysGridColumnProfile profile)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.ColumnName))
                return;

            GridColumn column = FindColumn(profile.ColumnName);
            if (column == null)
                return;

            column.Visible = profile.Visible;

            if (!string.IsNullOrWhiteSpace(profile.HeaderText))
                column.HeaderText = profile.HeaderText;

            if (profile.Width > 0)
                column.Width = profile.Width;

            column.Format = profile.Format;

            ApplyAlignment(column, profile.Alignment);

            if (string.IsNullOrWhiteSpace(profile.SummaryType) ||
                string.Equals(profile.SummaryType, "None", StringComparison.OrdinalIgnoreCase))
            {
                ClearSummaryForColumn(profile.ColumnName);
            }
            else
            {
                AddOrReplaceSummary(profile.ColumnName, profile.SummaryType, string.IsNullOrWhiteSpace(profile.Format) ? "N2" : profile.Format);
            }

            SetColumnGrouped(profile.ColumnName, profile.Grouped);
            SetFrozenThroughColumn(profile.ColumnName, profile.Frozen);

            if (!string.IsNullOrWhiteSpace(profile.Format))
                numericFormats[profile.ColumnName] = profile.Format;

            MarkChanged();
        }

        private void ApplyAlignment(GridColumn column, string alignment)
        {
            if (column == null)
                return;

            if (string.Equals(alignment, "Center", StringComparison.OrdinalIgnoreCase))
            {
                column.CellStyle.HorizontalAlignment = HorizontalAlignment.Center;
                column.HeaderStyle.HorizontalAlignment = HorizontalAlignment.Center;
                return;
            }

            if (string.Equals(alignment, "Right", StringComparison.OrdinalIgnoreCase))
            {
                column.CellStyle.HorizontalAlignment = HorizontalAlignment.Right;
                column.HeaderStyle.HorizontalAlignment = HorizontalAlignment.Right;
                return;
            }

            column.CellStyle.HorizontalAlignment = HorizontalAlignment.Left;
            column.HeaderStyle.HorizontalAlignment = HorizontalAlignment.Left;
        }

        private string DetectAlignment(GridColumn column)
        {
            try
            {
                if (column.CellStyle.HorizontalAlignment == HorizontalAlignment.Right)
                    return "Right";

                if (column.CellStyle.HorizontalAlignment == HorizontalAlignment.Center)
                    return "Center";
            }
            catch
            {
            }

            return "Left";
        }

        private int ExtractDecimals(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
                return 2;

            for (int i = format.Length - 1; i >= 0; i--)
            {
                if (char.IsDigit(format[i]))
                    return (int)char.GetNumericValue(format[i]);
            }

            return 2;
        }

        private string GetSummaryTypeForColumn(string columnName)
        {
            if (grid.TableSummaryRows == null)
                return "None";

            foreach (var row in grid.TableSummaryRows)
            {
                var summaryColumns = GetPropertyValue(row, "SummaryColumns") as System.Collections.IEnumerable;
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

        private bool IsColumnGrouped(string columnName)
        {
            if (grid.GroupColumnDescriptions == null)
                return false;

            foreach (var group in grid.GroupColumnDescriptions)
            {
                string current = GetPropertyAsString(group, "ColumnName");
                if (string.Equals(current, columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private void SetColumnGrouped(string columnName, bool grouped)
        {
            if (grid.GroupColumnDescriptions == null || string.IsNullOrWhiteSpace(columnName))
                return;

            for (int i = grid.GroupColumnDescriptions.Count - 1; i >= 0; i--)
            {
                string current = GetPropertyAsString(grid.GroupColumnDescriptions[i], "ColumnName");
                if (string.Equals(current, columnName, StringComparison.OrdinalIgnoreCase))
                    grid.GroupColumnDescriptions.RemoveAt(i);
            }

            if (grouped)
            {
                grid.GroupColumnDescriptions.Add(new GroupColumnDescription
                {
                    ColumnName = columnName
                });
            }
        }

        private bool IsColumnFrozen(string columnName)
        {
            object value = GetPropertyValue(grid, "FrozenColumnCount");
            if (value == null)
                return false;

            int frozenCount;
            if (!int.TryParse(Convert.ToString(value), out frozenCount) || frozenCount <= 0)
                return false;

            GridColumn column = FindColumn(columnName);
            if (column == null)
                return false;

            int index = grid.Columns.IndexOf(column);
            return index >= 0 && index < frozenCount;
        }

        private void SetFrozenThroughColumn(string columnName, bool frozen)
        {
            var property = grid.GetType().GetProperty("FrozenColumnCount");
            if (property == null || !property.CanWrite)
                return;

            GridColumn column = FindColumn(columnName);
            if (column == null)
                return;

            if (!frozen)
            {
                if (IsColumnFrozen(columnName))
                    property.SetValue(grid, 0, null);
                return;
            }

            int index = grid.Columns.IndexOf(column);
            if (index >= 0)
                property.SetValue(grid, index + 1, null);
        }

        private void HookGridEvents()
        {
            grid.ColumnDragging -= Grid_ColumnDragging;
            grid.ColumnDragging += Grid_ColumnDragging;

            grid.ColumnResizing -= Grid_ColumnResizing;
            grid.ColumnResizing += Grid_ColumnResizing;
        }

        private void Grid_ColumnDragging(object sender, ColumnDraggingEventArgs e)
        {
            MarkChanged();
        }

        private void Grid_ColumnResizing(object sender, ColumnResizingEventArgs e)
        {
            MarkChanged();
        }

        public void Dispose()
        {
            PersistCurrentViewName();

            if (currentMenu != null)
            {
                currentMenu.Close();
                currentMenu.Dispose();
                currentMenu = null;
            }
        }

        private void UpdateButtonState()
        {
            if (button == null)
                return;

            button.Text = hasChanges ? "⋮•" : "⋮";
            button.ForeColor = hasChanges ? Color.DarkOrange : Color.MidnightBlue;
            button.ToolTipText = hasChanges
                ? "Vistas del grid - cambios sin guardar"
                : "Vistas del grid";
        }
    }
}
