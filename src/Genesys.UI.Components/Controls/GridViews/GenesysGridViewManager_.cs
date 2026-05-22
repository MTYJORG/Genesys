using Syncfusion.WinForms.DataGrid;
using Syncfusion.WinForms.DataGrid.Events;
using System;
using System.Collections.Generic;
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
        private bool skipNextFilterApplication;
        private Func<string> captureFilterStateXml;
        private Action<string> applyFilterStateXml;
        private Action executeSearch;
        private readonly GenesysGridViewMenuService menuService;
        private readonly string ownerBaseText;
        private readonly List<Tuple<EventInfo, Delegate>> dynamicGridEventHandlers = new List<Tuple<EventInfo, Delegate>>();
        private string defaultNativeGridLayoutXml;

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
                delegate(bool value) { isApplyingLayout = value; });
            persistenceService = new GenesysGridViewPersistenceService(
                owner,
                gridKey,
                store,
                stateStore,
                DefaultViewName,
                delegate { return currentViewName; },
                delegate(string value) { currentViewName = value; },
                IsDefaultView,
                CaptureLayout,
                ApplyDefaultLayout,
                UpdateButtonState,
                delegate(bool value) { hasChanges = value; });
            menuService = new GenesysGridViewMenuService(
                owner,
                grid,
                button,
                numericFormats,
                DefaultViewName,
                persistenceService.LoadViews,
                delegate { return currentViewName; },
                IsDefaultView,
                ApplyDefaultLayout,
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
            ownerBaseText = owner == null ? string.Empty : owner.Text;
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

        public bool IsApplyingFilterSearch
        {
            get { return skipNextFilterApplication; }
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
            if (IsDefaultView(currentViewName))
            {
                ApplyDefaultLayout();
                return;
            }

            var views = persistenceService.LoadViews();
            var layout = views.FirstOrDefault(x =>
                string.Equals(x.ViewName, currentViewName, StringComparison.OrdinalIgnoreCase));

            if (layout != null)
            {
                CaptureDefaultNativeGridLayoutIfNeeded();
                ApplyLayout(layout);
            }
            else
                ApplyDefaultLayout();
        }

        public void ReapplyCurrentViewLayoutOnly()
        {
            System.Diagnostics.Debug.WriteLine("===== GRID VIEW MANAGER: ReapplyCurrentViewLayoutOnly START =====");
            System.Diagnostics.Debug.WriteLine("CurrentViewName: " + currentViewName);

            if (IsDefaultView(currentViewName))
            {
                System.Diagnostics.Debug.WriteLine("Vista predeterminada activa; no se reaplica layout guardado.");
                System.Diagnostics.Debug.WriteLine("===== GRID VIEW MANAGER: ReapplyCurrentViewLayoutOnly END =====");
                return;
            }

            var views = persistenceService.LoadViews();
            var layout = views.FirstOrDefault(x =>
                string.Equals(x.ViewName, currentViewName, StringComparison.OrdinalIgnoreCase));

            if (layout != null)
            {
                CaptureDefaultNativeGridLayoutIfNeeded();
                ApplyLayout(layout);
            }

            System.Diagnostics.Debug.WriteLine("===== GRID VIEW MANAGER: ReapplyCurrentViewLayoutOnly END =====");
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
            return persistenceService.SaveCurrentView(viewName);
        }

        private bool SaveAsNewView()
        {
            return persistenceService.SaveAsNewView();
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
            System.Diagnostics.Debug.WriteLine("===== GRID VIEW MANAGER: ApplyDefaultLayout START =====");

            isApplyingLayout = true;

            try
            {
                layoutService.ApplyDefaultLayout(defaultNativeGridLayoutXml);

                currentViewName = DefaultViewName;
                hasChanges = false;
                PersistCurrentViewName();

                System.Diagnostics.Debug.WriteLine("Vista predeterminada aplicada.");
            }
            finally
            {
                isApplyingLayout = false;
                skipNextFilterApplication = false;
                ApplyNumericFormats();
                UpdateButtonState();
            }

            System.Diagnostics.Debug.WriteLine("===== GRID VIEW MANAGER: ApplyDefaultLayout END =====");
        }

        private GenesysGridViewLayout CaptureLayout(string viewName)
        {
            return layoutService.Capture(viewName, gridKey, CaptureFilters);
        }

        private void ApplyLayout(GenesysGridViewLayout layout)
        {
            System.Diagnostics.Debug.WriteLine("===== GRID VIEW MANAGER: ApplyLayout START =====");

            if (layout == null)
            {
                System.Diagnostics.Debug.WriteLine("layout null");
                System.Diagnostics.Debug.WriteLine("===== GRID VIEW MANAGER: ApplyLayout END =====");
                return;
            }

            isApplyingLayout = true;

            try
            {
                layoutService.Apply(layout);

                currentViewName = layout.ViewName;
                hasChanges = false;
                PersistCurrentViewName();
            }
            finally
            {
                isApplyingLayout = false;
                skipNextFilterApplication = false;
                ApplyNumericFormats();
                UpdateButtonState();
            }

            System.Diagnostics.Debug.WriteLine("===== GRID VIEW MANAGER: ApplyLayout END =====");
        }

        


        private void CaptureDefaultNativeGridLayoutIfNeeded()
        {
            defaultNativeGridLayoutXml =
                layoutService.CaptureDefaultNativeGridLayoutIfNeeded(defaultNativeGridLayoutXml);
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


        public event EventHandler DesignerRequested;

        public void ToggleDesigner()
        {
            var handler = DesignerRequested;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        public bool SaveCurrentOrAsk()
        {
            return persistenceService.SaveCurrentOrAsk();
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

            HookDynamicGridEvent("FilterChanged");
            HookDynamicGridEvent("FilterChanging");
            HookDynamicGridEvent("SortColumnsChanged");
            HookDynamicGridEvent("SortColumnsChanging");
            HookDynamicGridEvent("SortChanged");
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

            UpdateOwnerTitle();
        }

        private void UpdateOwnerTitle()
        {
            if (owner == null)
                return;

            string suffix = " - Vista: " + currentViewName + (hasChanges ? " *" : string.Empty);
            owner.Text = (ownerBaseText ?? string.Empty) + suffix;
        }
    }
}
