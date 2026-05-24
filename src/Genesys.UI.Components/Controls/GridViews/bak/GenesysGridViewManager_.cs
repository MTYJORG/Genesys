using Syncfusion.WinForms.DataGrid;
using Syncfusion.WinForms.DataGrid.Events;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
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
        private readonly GenesysGridSummaryService summaryService;
        private readonly GenesysGridLayoutService layoutService;
        private readonly GenesysGridViewPersistenceService persistenceService;
        private string currentViewName;
        private bool isApplyingLayout;
        private bool hasChanges;
        private Func<string> captureFilterStateXml;
        private Action<string> applyFilterStateXml;
        private Action executeSearch;
        private readonly GenesysGridViewMenuService menuService;
        private readonly List<Tuple<EventInfo, Delegate>> dynamicGridEventHandlers = new List<Tuple<EventInfo, Delegate>>();
        private readonly Dictionary<string, GenesysGridViewLayout> runtimeLayoutsByViewName = new Dictionary<string, GenesysGridViewLayout>(StringComparer.OrdinalIgnoreCase);
        private INotifyCollectionChanged sortDescriptionsNotifier;
        private bool normalizeSelectionAfterSortPending;

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
            summaryService = new GenesysGridSummaryService(grid, numericFormats);
            layoutService = new GenesysGridLayoutService(
                grid,
                summaryService,
                delegate { return isApplyingLayout; },
                delegate (bool value) { isApplyingLayout = value; });
            persistenceService = new GenesysGridViewPersistenceService(
                owner,
                gridKey,
                store,
                stateStore,
                DefaultViewName,
                delegate { return currentViewName; },
                delegate (string value) { currentViewName = value; },
                IsDefaultView,
                CaptureLayout,
                ApplyDefaultLayout,
                UpdateButtonState,
                delegate (bool value) { hasChanges = value; });
            menuService = new GenesysGridViewMenuService(
                owner,
                grid,
                button,
                numericFormats,
                DefaultViewName,
                persistenceService.LoadViews,
                delegate { return currentViewName; },
                IsDefaultView,
                ApplyDefaultViewAndRefresh,
                ApplyLayout,
                SaveCurrentOrAsk,
                SaveAsNewView,
                DuplicateView,
                DeleteViewFromMenu,
                ToggleDesigner,
                AddOrReplaceSummary,
                ClearSummaryForColumn,
                ClearSummaryRows,
                MarkChanged);
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

            /*
             * numericFormats representa formatos base/globales del formulario.
             * No debe convertirse en formato de la vista activa.
             *
             * Por eso solo se aplica inmediatamente cuando estamos en la vista
             * predeterminada. En vistas guardadas, el formato de la vista manda y
             * se aplicará desde GenesysGridColumnLayout.Format.
             */
            numericFormats[columnName] = format;

            if (IsDefaultView(currentViewName))
                GenesysGridConfigurator.ApplyNumericFormat(grid, columnName, format);
        }

        public void ApplyNumericFormats()
        {
            GenesysGridConfigurator.ApplyNumericFormats(grid, numericFormats);
        }

        public void AttachFilters(
            Func<string> captureFilterStateXml,
            Action<string> applyFilterStateXml,
            Action executeSearch)
        {
            this.captureFilterStateXml = captureFilterStateXml;
            this.applyFilterStateXml = applyFilterStateXml;
            this.executeSearch = executeSearch;
        }

        private void RestoreCurrentViewName()
        {
            currentViewName = persistenceService.RestoreCurrentViewName(currentViewName);
        }

        public void PersistCurrentViewName()
        {
            persistenceService.PersistCurrentViewName(currentViewName);
        }

        public void ReapplyCurrentView()
        {
            ApplyCurrentView();
        }

        public GenesysGridViewLayout GetCurrentViewLayout()
        {
            if (IsDefaultView(currentViewName))
                return null;

            var views = persistenceService.LoadViews();

            if (views == null)
                return null;

            return views.FirstOrDefault(x =>
                string.Equals(x.ViewName, currentViewName, StringComparison.OrdinalIgnoreCase));
        }

        public GenesysGridViewLayout CaptureCurrentRuntimeLayout()
        {
            string viewName = IsDefaultView(currentViewName)
                ? DefaultViewName
                : currentViewName;

            return CaptureLayout(viewName);
        }

        private void CaptureRuntimeLayoutForCurrentViewIfNeeded()
        {
            if (!hasChanges)
                return;

            string viewName = IsDefaultView(currentViewName)
                ? DefaultViewName
                : currentViewName;

            GenesysGridViewLayout layout = CaptureLayout(viewName);

            if (layout == null)
                return;

            runtimeLayoutsByViewName[viewName] = layout;
        }

        private void ApplyRuntimeLayout(GenesysGridViewLayout layout)
        {
            if (layout == null)
                return;

            isApplyingLayout = true;

            try
            {
                layoutService.Apply(layout, false);

                currentViewName = string.IsNullOrWhiteSpace(layout.ViewName)
                    ? DefaultViewName
                    : layout.ViewName;

                hasChanges = true;
                PersistCurrentViewName();
            }
            finally
            {
                isApplyingLayout = false;

                /*
                 * No aplicar formatos globales aquí.
                 * ApplyRuntimeLayout representa una vista/session layout específico;
                 * reaplicar numericFormats en este punto pisa column.Format de esa vista.
                 */
                UpdateButtonState();
            }

            QueueNormalizeSelectionAfterSort();
        }

        private void ClearRuntimeLayoutForCurrentView()
        {
            string viewName = IsDefaultView(currentViewName)
                ? DefaultViewName
                : currentViewName;

            runtimeLayoutsByViewName.Remove(viewName);
        }

        public IList<string> GetAvailableViewNames()
        {
            var result = new List<string>();
            result.Add(DefaultViewName);

            IList<GenesysGridViewLayout> views = persistenceService.LoadViews();

            if (views == null)
                return result;

            foreach (var view in views.OrderBy(x => x.ViewName))
            {
                if (view == null || string.IsNullOrWhiteSpace(view.ViewName))
                    continue;

                if (IsDefaultView(view.ViewName))
                    continue;

                if (result.Any(x => string.Equals(x, view.ViewName, StringComparison.OrdinalIgnoreCase)))
                    continue;

                result.Add(view.ViewName);
            }

            return result;
        }

        public void ApplyViewByName(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName))
                return;

            if (string.Equals(viewName, currentViewName, StringComparison.OrdinalIgnoreCase))
                return;

            CaptureRuntimeLayoutForCurrentViewIfNeeded();

            GenesysGridViewLayout runtimeLayout;
            if (runtimeLayoutsByViewName.TryGetValue(viewName, out runtimeLayout) && runtimeLayout != null)
            {
                ApplyRuntimeLayout(runtimeLayout);
                return;
            }

            if (IsDefaultView(viewName))
            {
                ApplyDefaultViewAndRefresh();
                return;
            }

            IList<GenesysGridViewLayout> views = persistenceService.LoadViews();

            if (views == null)
                return;

            GenesysGridViewLayout layout = views.FirstOrDefault(x =>
                string.Equals(x.ViewName, viewName, StringComparison.OrdinalIgnoreCase));

            if (layout == null)
                return;

            ApplyLayout(layout);
        }

        public void ApplyCurrentViewLayoutBeforePaint()
        {
            ApplyCurrentView();
        }

        public void ReapplyCurrentViewLayoutOnly()
        {
            ApplyCurrentView();
        }

        private void ApplyCurrentView()
        {
            if (IsDefaultView(currentViewName))
            {
                ApplyDefaultLayout();
                return;
            }

            GenesysGridViewLayout layout = GetCurrentViewLayout();

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
            menuService.ShowMenu();
        }

        private bool SaveCurrentView(string viewName)
        {
            bool saved = persistenceService.SaveCurrentView(viewName);

            if (saved)
                ClearRuntimeLayoutForCurrentView();

            return saved;
        }

        private bool SaveAsNewView()
        {
            bool saved = persistenceService.SaveAsNewView();

            if (saved)
                ClearRuntimeLayoutForCurrentView();

            return saved;
        }

        public bool SaveAsNewViewFromDesigner()
        {
            return SaveAsNewView();
        }

        private void DuplicateView()
        {
            persistenceService.DuplicateView();
        }

        private void DeleteViewFromMenu()
        {
            persistenceService.DeleteViewFromMenu();
        }

        private void DeleteView(string viewName)
        {
            persistenceService.DeleteView(viewName);
        }

        private void ApplyDefaultLayout()
        {
            isApplyingLayout = true;

            try
            {
                layoutService.ApplyDefaultLayout(null);

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

        public void ApplyDefaultViewAndRefresh()
        {
            isApplyingLayout = true;

            try
            {
                currentViewName = DefaultViewName;
                hasChanges = false;

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
                    if (grid.TableSummaryRows != null)
                        grid.TableSummaryRows.Clear();
                }
                catch
                {
                }

                try
                {
                    if (grid.Columns != null)
                        grid.Columns.Clear();
                }
                catch
                {
                }

                grid.AutoGenerateColumns = true;

                PersistCurrentViewName();
                UpdateButtonState();
            }
            finally
            {
                isApplyingLayout = false;
            }

            if (executeSearch != null)
                executeSearch();
        }

        private GenesysGridViewLayout CaptureLayout(string viewName)
        {
            return layoutService.Capture(viewName, gridKey, CaptureFilters);
        }

        private void ApplyLayout(GenesysGridViewLayout layout)
        {
            if (layout == null)
                return;

            isApplyingLayout = true;

            try
            {
                layoutService.Apply(layout, false);

                currentViewName = layout.ViewName;
                hasChanges = false;
                PersistCurrentViewName();
            }
            finally
            {
                isApplyingLayout = false;
                UpdateButtonState();
            }

            QueueNormalizeSelectionAfterSort();
        }













        private void CaptureFilters(GenesysGridViewLayout layout)
        {
            if (layout == null)
                return;

            // Las vistas de Genesys SIEMPRE guardan el estado actual de filtros.
            // La propiedad IncludeFilters se conserva solo por compatibilidad con XML antiguos.
            layout.IncludeFilters = true;

            if (captureFilterStateXml == null)
                return;

            layout.FilterStateXml = captureFilterStateXml();
        }





        public void AddOrReplaceSummary(string columnName, string summaryType, string format)
        {
            summaryService.AddOrReplaceSummary(columnName, summaryType, format);
        }

        public void ClearSummaryForColumn(string columnName)
        {
            summaryService.ClearSummaryForColumn(columnName);
        }

        public void ClearSummaryRows()
        {
            summaryService.ClearSummaryRows();
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

        public void ReorderColumnsByMappingNames(IList<string> columnNames)
        {
            if (grid == null || grid.Columns == null || columnNames == null)
                return;

            int targetIndex = 0;

            foreach (string columnName in columnNames)
            {
                if (string.IsNullOrWhiteSpace(columnName))
                    continue;

                GridColumn column = FindColumn(columnName);

                if (column == null)
                    continue;

                int currentIndex = grid.Columns.IndexOf(column);

                if (currentIndex >= 0 && currentIndex != targetIndex)
                    grid.Columns.Move(currentIndex, targetIndex);

                targetIndex++;
            }

            MarkChanged();
        }

        private bool IsDefaultView(string viewName)
        {
            return string.IsNullOrWhiteSpace(viewName) ||
                   string.Equals(viewName, DefaultViewName, StringComparison.OrdinalIgnoreCase);
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

        private void SetPropertyValue(object instance, string propertyName, object value)
        {
            if (instance == null || string.IsNullOrWhiteSpace(propertyName))
                return;

            try
            {
                var property = instance.GetType().GetProperty(propertyName);

                if (property == null || !property.CanWrite)
                    return;

                property.SetValue(instance, value, null);
            }
            catch
            {
            }
        }


        public event EventHandler DesignerRequested;
        public event EventHandler ViewChanged;

        public void ToggleDesigner()
        {
            var handler = DesignerRequested;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        public bool SaveCurrentOrAsk()
        {
            bool saved = persistenceService.SaveCurrentOrAsk();

            if (saved)
                ClearRuntimeLayoutForCurrentView();

            return saved;
        }

        public bool ConfirmPendingChangesBeforeClose(IWin32Window dialogOwner)
        {
            CaptureRuntimeLayoutForCurrentViewIfNeeded();

            if (runtimeLayoutsByViewName.Count == 0)
                return true;

            string changedViews = BuildChangedViewsText();

            string message =
                "Hay cambios temporales sin guardar en las siguientes vistas:" +
                "\n\n" +
                changedViews +
                "\n\n¿Deseas guardar todos los cambios antes de cerrar?";

            DialogResult result = MessageBox.Show(
                dialogOwner,
                message,
                "Vistas del grid",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Cancel)
                return false;

            if (result == DialogResult.No)
            {
                hasChanges = false;
                runtimeLayoutsByViewName.Clear();
                UpdateButtonState();
                return true;
            }

            return SaveAllRuntimeLayouts(dialogOwner);
        }

        private string BuildChangedViewsText()
        {
            if (runtimeLayoutsByViewName.Count == 0)
                return "- " + currentViewName;

            return string.Join(
                Environment.NewLine,
                runtimeLayoutsByViewName.Keys
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .OrderBy(x => x)
                    .Select(x => "- " + x)
                    .ToArray());
        }

        private bool SaveAllRuntimeLayouts(IWin32Window dialogOwner)
        {
            CaptureRuntimeLayoutForCurrentViewIfNeeded();

            if (runtimeLayoutsByViewName.Count == 0)
                return true;

            try
            {
                foreach (KeyValuePair<string, GenesysGridViewLayout> item in runtimeLayoutsByViewName.ToArray())
                {
                    GenesysGridViewLayout layout = item.Value;

                    if (layout == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(layout.GridKey))
                        layout.GridKey = gridKey;

                    if (string.IsNullOrWhiteSpace(layout.ViewName))
                        layout.ViewName = item.Key;

                    if (IsDefaultView(layout.ViewName))
                        continue;

                    layout.ModifiedAt = DateTime.Now;

                    if (layout.CreatedAt == DateTime.MinValue)
                        layout.CreatedAt = DateTime.Now;

                    store.Save(layout);
                }

                runtimeLayoutsByViewName.Clear();
                hasChanges = false;
                PersistCurrentViewName();
                UpdateButtonState();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    dialogOwner,
                    "No se pudieron guardar todos los cambios de vistas.\n\n" + ex.Message,
                    "Vistas del grid",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }
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
            string summaryType = summaryService.GetSummaryTypeForColumn(column.MappingName);

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

            // No actualizar numericFormats aquí.
            // numericFormats representa formatos base/globales configurados por el formulario,
            // no formatos particulares de una vista. Si se actualiza desde el diseñador,
            // el formato de una vista contamina a las demás.

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



        private void HookDynamicGridEvent(string eventName)
        {
            try
            {
                if (grid == null || string.IsNullOrWhiteSpace(eventName))
                    return;

                EventInfo eventInfo = grid.GetType().GetEvent(eventName, BindingFlags.Instance | BindingFlags.Public);
                if (eventInfo == null)
                {
                    System.Diagnostics.Debug.WriteLine("Grid event not found: " + eventName);
                    return;
                }

                Delegate handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, "Grid_GenericChanged", false);
                if (handler == null)
                {
                    System.Diagnostics.Debug.WriteLine("No se pudo crear handler para evento: " + eventName);
                    return;
                }

                eventInfo.AddEventHandler(grid, handler);
                dynamicGridEventHandlers.Add(new Tuple<EventInfo, Delegate>(eventInfo, handler));
                System.Diagnostics.Debug.WriteLine("Grid event hooked: " + eventName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("HookDynamicGridEvent ERROR " + eventName + ": " + ex.Message);
            }
        }

        private void Grid_GenericChanged(object sender, EventArgs e)
        {
            MarkChanged();
        }

        private void HookGridEvents()
        {
            grid.ColumnDragging -= Grid_ColumnDragging;
            grid.ColumnDragging += Grid_ColumnDragging;

            grid.ColumnResizing -= Grid_ColumnResizing;
            grid.ColumnResizing += Grid_ColumnResizing;

            HookGridRuntimeStateChanged();
        }

        private void HookGridRuntimeStateChanged()
        {
            HookSortDescriptionsChanged();
            HookFilterChanged();
        }


        private void HookSortDescriptionsChanged()
        {
            if (sortDescriptionsNotifier != null)
                sortDescriptionsNotifier.CollectionChanged -= SortDescriptions_CollectionChanged;

            sortDescriptionsNotifier = grid == null
                ? null
                : grid.SortColumnDescriptions as INotifyCollectionChanged;

            if (sortDescriptionsNotifier != null)
                sortDescriptionsNotifier.CollectionChanged += SortDescriptions_CollectionChanged;
        }

        private void SortDescriptions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (isApplyingLayout)
                return;

            QueueNormalizeSelectionAfterSort();
            MarkChanged();
        }

        private void HookFilterChanged()
        {
            if (grid == null)
                return;

            grid.FilterChanged -= Grid_FilterChanged;
            grid.FilterChanged += Grid_FilterChanged;
        }

        private void Grid_FilterChanged(object sender, FilterChangedEventArgs e)
        {
            if (isApplyingLayout)
                return;

            MarkChanged();
        }

        private void QueueNormalizeSelectionAfterSort()
        {
            if (normalizeSelectionAfterSortPending)
                return;

            if (grid == null || grid.IsDisposed || !grid.IsHandleCreated)
                return;

            normalizeSelectionAfterSortPending = true;

            grid.BeginInvoke(new MethodInvoker(delegate
            {
                normalizeSelectionAfterSortPending = false;
                NormalizeSelectionToFirstVisibleRecord();
            }));
        }

        private void NormalizeSelectionToFirstVisibleRecord()
        {
            try
            {
                if (grid == null || grid.View == null || grid.View.Records == null)
                    return;

                if (grid.View.Records.Count == 0)
                    return;

                object record = grid.View.Records[0];
                object data = GetPropertyValue(record, "Data");

                if (data == null)
                    data = GetPropertyValue(record, "Record");

                if (data == null)
                    data = record;

                grid.SelectedItem = data;
                SetPropertyValue(grid, "CurrentItem", data);
            }
            catch
            {
            }
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

            if (sortDescriptionsNotifier != null)
            {
                sortDescriptionsNotifier.CollectionChanged -= SortDescriptions_CollectionChanged;
                sortDescriptionsNotifier = null;
            }

            if (grid != null)
                grid.FilterChanged -= Grid_FilterChanged;

            foreach (var item in dynamicGridEventHandlers.ToArray())
            {
                try
                {
                    item.Item1.RemoveEventHandler(grid, item.Item2);
                }
                catch
                {
                }
            }

            dynamicGridEventHandlers.Clear();
            runtimeLayoutsByViewName.Clear();

            if (menuService != null)
                menuService.Dispose();
        }

        private void UpdateButtonState()
        {
            if (button != null)
            {
                button.Text = hasChanges ? "⋮•" : "⋮";
                button.ForeColor = hasChanges ? Color.DarkOrange : Color.MidnightBlue;
                button.ToolTipText = hasChanges
                    ? "Vista activa: " + currentViewName + " - cambios sin guardar"
                    : "Vista activa: " + currentViewName;
            }

            if (ViewChanged != null)
                ViewChanged(this, EventArgs.Empty);
        }
        public Type GetColumnDataType(string columnName)
        {
            if (grid == null || string.IsNullOrWhiteSpace(columnName))
                return null;

            Type type = GetColumnTypeFromDataSource(columnName);

            if (type != null)
                return type;

            GridColumn column = FindColumn(columnName);

            if (column != null)
            {
                object valueType = GetPropertyValue(column, "ValueType");

                if (valueType is Type)
                    return Nullable.GetUnderlyingType((Type)valueType) ?? (Type)valueType;
            }

            return null;
        }

        private Type GetColumnTypeFromDataSource(string columnName)
        {
            object source = grid.DataSource;

            if (source is DataTable table)
                return GetDataTableColumnType(table, columnName);

            if (source is DataView view)
                return GetDataTableColumnType(view.Table, columnName);

            if (source is BindingSource bindingSource)
            {
                if (bindingSource.DataSource is DataTable bsTable)
                    return GetDataTableColumnType(bsTable, columnName);

                if (bindingSource.DataSource is DataView bsView)
                    return GetDataTableColumnType(bsView.Table, columnName);
            }

            return null;
        }

        private Type GetDataTableColumnType(DataTable table, string columnName)
        {
            if (table == null || !table.Columns.Contains(columnName))
                return null;

            Type type = table.Columns[columnName].DataType;

            return Nullable.GetUnderlyingType(type) ?? type;
        }

        public bool IsNumericColumn(string columnName)
        {
            Type type = GetColumnDataType(columnName);

            if (type == null)
                return false;

            type = Nullable.GetUnderlyingType(type) ?? type;

            return type == typeof(byte) ||
                   type == typeof(short) ||
                   type == typeof(int) ||
                   type == typeof(long) ||
                   type == typeof(float) ||
                   type == typeof(double) ||
                   type == typeof(decimal);
        }

        public bool IsDateColumn(string columnName)
        {
            Type type = GetColumnDataType(columnName);
            return type == typeof(DateTime);
        }

        public bool IsBooleanColumn(string columnName)
        {
            Type type = GetColumnDataType(columnName);
            return type == typeof(bool);
        }
    }
}
