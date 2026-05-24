using Syncfusion.WinForms.DataGrid;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.GridViews
{
    public class GenesysGridViewMenuService : IDisposable
    {
        private readonly Form owner;
        private readonly SfDataGrid grid;
        private readonly ToolStripButton button;
        private readonly IDictionary<string, string> numericFormats;
        private readonly string defaultViewName;
        private readonly Func<IList<GenesysGridViewLayout>> loadViews;
        private readonly Func<string> getCurrentViewName;
        private readonly Func<string, bool> isDefaultView;
        private readonly Action applyDefaultLayout;
        private readonly Action<GenesysGridViewLayout> applyLayout;
        private readonly Func<bool> saveCurrentOrAsk;
        private readonly Func<bool> saveAsNewView;
        private readonly Action duplicateView;
        private readonly Action deleteViewFromMenu;
        private readonly Action toggleDesigner;
        private readonly Action<string, string, string> addOrReplaceSummary;
        private readonly Action<string> clearSummaryForColumn;
        private readonly Action clearSummaryRows;
        private readonly Action markChanged;
        private ContextMenuStrip currentMenu;

        public GenesysGridViewMenuService(
            Form owner,
            SfDataGrid grid,
            ToolStripButton button,
            IDictionary<string, string> numericFormats,
            string defaultViewName,
            Func<IList<GenesysGridViewLayout>> loadViews,
            Func<string> getCurrentViewName,
            Func<string, bool> isDefaultView,
            Action applyDefaultLayout,
            Action<GenesysGridViewLayout> applyLayout,
            Func<bool> saveCurrentOrAsk,
            Func<bool> saveAsNewView,
            Action duplicateView,
            Action deleteViewFromMenu,
            Action toggleDesigner,
            Action<string, string, string> addOrReplaceSummary,
            Action<string> clearSummaryForColumn,
            Action clearSummaryRows,
            Action markChanged)
        {
            this.owner = owner;
            this.grid = grid;
            this.button = button;
            this.numericFormats = numericFormats;
            this.defaultViewName = defaultViewName;
            this.loadViews = loadViews;
            this.getCurrentViewName = getCurrentViewName;
            this.isDefaultView = isDefaultView;
            this.applyDefaultLayout = applyDefaultLayout;
            this.applyLayout = applyLayout;
            this.saveCurrentOrAsk = saveCurrentOrAsk;
            this.saveAsNewView = saveAsNewView;
            this.duplicateView = duplicateView;
            this.deleteViewFromMenu = deleteViewFromMenu;
            this.toggleDesigner = toggleDesigner;
            this.addOrReplaceSummary = addOrReplaceSummary;
            this.clearSummaryForColumn = clearSummaryForColumn;
            this.clearSummaryRows = clearSummaryRows;
            this.markChanged = markChanged;
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

            IList<GenesysGridViewLayout> views = loadViews == null
                ? new List<GenesysGridViewLayout>()
                : loadViews();

            if (views == null)
                views = new List<GenesysGridViewLayout>();

            string currentViewName = getCurrentViewName == null
                ? defaultViewName
                : getCurrentViewName();

            ToolStripMenuItem title = new ToolStripMenuItem("Vistas");
            title.Enabled = false;
            menu.Items.Add(title);

            ToolStripMenuItem defaultItem = new ToolStripMenuItem(defaultViewName);
            defaultItem.Checked = IsDefault(currentViewName);
            defaultItem.Click += delegate { if (applyDefaultLayout != null) applyDefaultLayout(); };
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
                        var menuItem = s as ToolStripMenuItem;
                        var layout = menuItem == null ? null : menuItem.Tag as GenesysGridViewLayout;

                        if (layout != null)
                            if (applyLayout != null) applyLayout(layout);
                    };
                    menu.Items.Add(item);
                }
            }

            menu.Items.Add(new ToolStripSeparator());

            string saveText = IsDefault(currentViewName)
                ? "Guardar como nueva vista..."
                : "Guardar cambios en \"" + currentViewName + "\"";

            var save = new ToolStripMenuItem(saveText);
            save.Click += delegate { if (saveCurrentOrAsk != null) saveCurrentOrAsk(); };
            menu.Items.Add(save);

            var saveAs = new ToolStripMenuItem("Guardar como nueva vista...");
            saveAs.Click += delegate { if (saveAsNewView != null) saveAsNewView(); };
            menu.Items.Add(saveAs);

            var duplicate = new ToolStripMenuItem("Duplicar vista...");
            duplicate.Enabled = !IsDefault(currentViewName);
            duplicate.Click += delegate { if (duplicateView != null) duplicateView(); };
            menu.Items.Add(duplicate);

            var deleteView = new ToolStripMenuItem(IsDefault(currentViewName)
                ? "Eliminar vista..."
                : "Eliminar vista \"" + currentViewName + "\"...");
            deleteView.Enabled = views.Count > 0;
            deleteView.Click += delegate { if (deleteViewFromMenu != null) deleteViewFromMenu(); };
            menu.Items.Add(deleteView);

            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add(CreateSummariesMenu());

            var designer = new ToolStripMenuItem("Diseñar vista...");
            designer.Click += delegate { if (toggleDesigner != null) toggleDesigner(); };
            menu.Items.Add(designer);

            menu.Items.Add(new ToolStripSeparator());

            var restore = new ToolStripMenuItem("Restaurar vista predeterminada");
            restore.Click += delegate { if (applyDefaultLayout != null) applyDefaultLayout(); };
            menu.Items.Add(restore);

            ShowContextMenu(menu);
        }

        private ToolStripMenuItem CreateSummariesMenu()
        {
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
                    if (clearSummaryForColumn != null) clearSummaryForColumn(column.MappingName);
                    if (markChanged != null) markChanged();
                }
            };
            summaries.DropDownItems.Add(clearCurrentSummary);

            var clearSummaries = new ToolStripMenuItem("Quitar todos los summaries");
            clearSummaries.Click += delegate
            {
                if (clearSummaryRows != null) clearSummaryRows();
                if (markChanged != null) markChanged();
            };
            summaries.DropDownItems.Add(clearSummaries);

            return summaries;
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

                if (numericFormats != null)
                    numericFormats.TryGetValue(column.MappingName, out format);

                if (string.Equals(summaryType, "Count", StringComparison.OrdinalIgnoreCase))
                    format = "N0";

                if (addOrReplaceSummary != null) addOrReplaceSummary(column.MappingName, summaryType, string.IsNullOrWhiteSpace(format) ? "N2" : format);
                if (markChanged != null) markChanged();
            };

            return item;
        }

        private void ShowSummaryEditor()
        {
            using (var dialog = new GenesysGridSummaryPrompt(grid, numericFormats))
            {
                if (dialog.ShowDialog(owner) != DialogResult.OK)
                    return;

                if (addOrReplaceSummary != null) addOrReplaceSummary(dialog.ColumnName, dialog.SummaryTypeName, dialog.NumericFormat);
                if (markChanged != null) markChanged();
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

        private bool IsDefault(string viewName)
        {
            if (isDefaultView == null)
                return string.IsNullOrWhiteSpace(viewName) ||
                       string.Equals(viewName, defaultViewName, StringComparison.OrdinalIgnoreCase);

            return isDefaultView(viewName);
        }

        public void Dispose()
        {
            if (currentMenu != null)
            {
                currentMenu.Close();
                currentMenu.Dispose();
                currentMenu = null;
            }
        }
    }
}
