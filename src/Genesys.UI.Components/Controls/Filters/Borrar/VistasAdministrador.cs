using Syncfusion.WinForms.DataGrid;
using Syncfusion.WinForms.DataGrid.Events;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using System.Windows.Forms;
using Syncfusion.WinForms.DataGrid.Enums;
using Genesys.UI.Components.Controls.GridViews;

namespace Genesys.UI.Components.Controls.GridViews.Vistas
{
/// <summary>
    /// Administra el ciclo de vida de las vistas del grid: carga, aplica, captura cambios runtime,
    /// guarda, duplica, elimina y sincroniza filtros, ordenamiento, agrupamiento y resúmenes.
    /// </summary>
    public class VistasAdministrador : IDisposable
    {
        private const string DefaultViewName = "Predeterminada";

        private readonly Form owner;
        private readonly SfDataGrid grid;
        private readonly ToolStripButton button;
        private readonly string gridKey;
        private readonly IGridViewStore store;
        private readonly IGenesysGridViewStateStore stateStore;
        private readonly GridSummaryService summaryService;
        private readonly GridLayoutService layoutService;
        private readonly GridViewPersistenceService persistenceService;
        private string currentViewName;
        private bool isApplyingLayout;
        private bool hasChanges;
        private Func<string> captureFilterStateXml;
        private Action<string> applyFilterStateXml;
        private Action executeSearch;
        private readonly GridViewMenuService menuService;
        private readonly List<Tuple<EventInfo, Delegate>> dynamicGridEventHandlers = new List<Tuple<EventInfo, Delegate>>();
        private readonly Dictionary<string, GridViewLayout> runtimeLayoutsByViewName = new Dictionary<string, GridViewLayout>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> runtimeInternalFiltersByViewName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dictionary<string, string>> runtimeColumnFiltersByViewName = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        private INotifyCollectionChanged sortDescriptionsNotifier;
        private bool normalizeSelectionAfterSortPending;
        private bool suppressRuntimeCaptureOnNextBind;

        private const int WM_SETREDRAW = 0x000B;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public VistasAdministrador(Form owner, SfDataGrid grid, ToolStripButton button, string gridKey)
            : this(owner, grid, button, gridKey, new GridViewFileStore())
        {
        }

        public VistasAdministrador(Form owner, SfDataGrid grid, ToolStripButton button, string gridKey, IGridViewStore store)
        {
            this.owner = owner;
            this.grid = grid;
            this.button = button;
            this.gridKey = gridKey;
            this.store = store;
            stateStore = store as IGenesysGridViewStateStore;
            currentViewName = DefaultViewName;
            summaryService = new GridSummaryService(grid);
            layoutService = new GridLayoutService(
                grid,
                summaryService,
                delegate { return isApplyingLayout; },
                delegate (bool value) { isApplyingLayout = value; });
            persistenceService = new GridViewPersistenceService(
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
            menuService = new GridViewMenuService(
                owner,
                grid,
                button,
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
            GridConfigurator.Configure(grid);

            if (button != null)
                button.ToolTipText = "Vistas del grid";

            RestoreCurrentViewName();
            HookGridEvents();
            UpdateButtonState();
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

        public GridViewLayout GetCurrentViewLayout()
        {
            if (IsDefaultView(currentViewName))
                return null;

            var views = persistenceService.LoadViews();

            if (views == null)
                return null;

            return views.FirstOrDefault(x =>
                string.Equals(x.ViewName, currentViewName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Enlaza un DataTable aplicando la vista activa como una sola transacción visual.
        /// El bind de datos y la aplicación del estado de la vista se agrupan para evitar
        /// que el usuario vea primero el grid sin agrupamiento/sort/filtros y después
        /// un segundo repintado con la vista aplicada.
        /// </summary>
        public void BindDataTable(DataTable table)
        {
            if (table == null || grid == null)
                return;

            string viewName = IsDefaultView(currentViewName)
                ? DefaultViewName
                : currentViewName;

            if (suppressRuntimeCaptureOnNextBind)
                suppressRuntimeCaptureOnNextBind = false;
            else
                CaptureRuntimeLayoutForCurrentViewIfNeeded();

            GridViewLayout runtimeLayout;
            bool hasRuntimeLayout =
                runtimeLayoutsByViewName.TryGetValue(viewName, out runtimeLayout) &&
                runtimeLayout != null;

            GridViewLayout layout = hasRuntimeLayout
                ? runtimeLayout
                : GetCurrentViewLayout();

            using (BeginGridVisualTransaction())
            {
                isApplyingLayout = true;

                try
                {
                    GridConfigurator.BindDataTable(grid, table, layout);

                    if (layout != null)
                    {
                        ApplyLayoutStateToBoundGrid(layout);
                        currentViewName = string.IsNullOrWhiteSpace(layout.ViewName)
                            ? viewName
                            : layout.ViewName;

                        PersistCurrentViewName();
                    }

                    hasChanges = hasRuntimeLayout;
                }
                finally
                {
                    isApplyingLayout = false;
                    UpdateButtonState();
                }
            }

            if (layout != null)
                QueueNormalizeSelectionAfterSort();
        }
        public GridViewLayout CaptureCurrentRuntimeLayout()
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

            GridViewLayout layout = CaptureLayout(viewName);

            if (layout == null)
                return;

            runtimeLayoutsByViewName[viewName] = layout;
            runtimeInternalFiltersByViewName[viewName] = CaptureInternalGridFiltersXml();
            runtimeColumnFiltersByViewName[viewName] = CaptureColumnFilterValuesFromLayoutOrGrid(layout);
        }

        private void ApplyRuntimeLayout(GridViewLayout layout)
        {
            if (layout == null)
                return;

            using (BeginGridVisualTransaction())
            {
                isApplyingLayout = true;

                try
                {
                    layoutService.Apply(layout, false);

                    string runtimeViewName = string.IsNullOrWhiteSpace(layout.ViewName)
                        ? DefaultViewName
                        : layout.ViewName;

                    ApplyInternalGridFiltersFromLayout(layout);

                    currentViewName = runtimeViewName;

                    hasChanges = true;
                    PersistCurrentViewName();
                }
                finally
                {
                    isApplyingLayout = false;
                    UpdateButtonState();
                }
            }

            QueueNormalizeSelectionAfterSort();
        }
        private void ClearRuntimeLayoutForCurrentView()
        {
            string viewName = IsDefaultView(currentViewName)
                ? DefaultViewName
                : currentViewName;

            runtimeLayoutsByViewName.Remove(viewName);
            runtimeInternalFiltersByViewName.Remove(viewName);
            runtimeColumnFiltersByViewName.Remove(viewName);
        }

        public IList<string> GetAvailableViewNames()
        {
            var result = new List<string>();
            result.Add(DefaultViewName);

            IList<GridViewLayout> views = persistenceService.LoadViews();

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

            return SortViewNamesByUserOrder(result);
        }

        public void ApplyViewByName(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName))
                return;

            if (string.Equals(viewName, currentViewName, StringComparison.OrdinalIgnoreCase))
                return;

            CaptureRuntimeLayoutForCurrentViewIfNeeded();

            GridViewLayout runtimeLayout;
            if (runtimeLayoutsByViewName.TryGetValue(viewName, out runtimeLayout) && runtimeLayout != null)
            {
                if (ApplyViewFiltersAndSearchIfAvailable(runtimeLayout, true))
                    return;

                ApplyRuntimeLayout(runtimeLayout);
                return;
            }

            if (IsDefaultView(viewName))
            {
                ApplyDefaultViewAndRefresh();
                return;
            }

            IList<GridViewLayout> views = persistenceService.LoadViews();

            if (views == null)
                return;

            GridViewLayout layout = views.FirstOrDefault(x =>
                string.Equals(x.ViewName, viewName, StringComparison.OrdinalIgnoreCase));

            if (layout == null)
                return;

            if (ApplyViewFiltersAndSearchIfAvailable(layout, false))
                return;

            ApplyLayout(layout);
        }

        private bool ApplyViewFiltersAndSearchIfAvailable(GridViewLayout layout, bool keepRuntimeChanges)
        {
            if (layout == null || string.IsNullOrWhiteSpace(layout.FilterStateXml))
                return false;

            if (applyFilterStateXml == null || executeSearch == null)
                return false;

            string viewName = string.IsNullOrWhiteSpace(layout.ViewName)
                ? DefaultViewName
                : layout.ViewName;

            isApplyingLayout = true;

            try
            {
                applyFilterStateXml(layout.FilterStateXml);

                currentViewName = viewName;
                hasChanges = keepRuntimeChanges;
                PersistCurrentViewName();

                // El bind provocado por la búsqueda debe usar el layout runtime existente,
                // no recapturar accidentalmente el grid anterior como si fuera la vista nueva.
                suppressRuntimeCaptureOnNextBind = keepRuntimeChanges;
            }
            finally
            {
                isApplyingLayout = false;
                UpdateButtonState();
            }

            executeSearch();
            return true;
        }

        public void ApplyCurrentViewLayoutBeforePaint()
        {
            ApplyCurrentView();
        }

        public void ReapplyCurrentViewLayoutOnly()
        {
            if (IsCurrentViewDefault)
                return;

            GridViewLayout layout = GetCurrentViewLayout();

            if (layout == null)
                return;

            using (BeginGridVisualTransaction())
            {
                isApplyingLayout = true;

                try
                {
                    ApplyLayoutStateToBoundGrid(layout);

                    currentViewName = layout.ViewName;
                    hasChanges = false;
                    PersistCurrentViewName();
                }
                finally
                {
                    isApplyingLayout = false;
                    UpdateButtonState();
                }
            }

            QueueNormalizeSelectionAfterSort();
        }

        private void ApplyCurrentView()
        {
            if (IsDefaultView(currentViewName))
            {
                ApplyDefaultLayout();
                return;
            }

            GridViewLayout layout = GetCurrentViewLayout();

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

        /// <summary>
        /// Registra cambios runtime provenientes del panel superior de filtros.
        /// Estos cambios pertenecen a la vista activa, se conservan al cambiar de vista
        /// y se guardan únicamente cuando el usuario guarda la vista.
        /// </summary>
        public void MarkUpperFiltersChanged(string filterStateXml)
        {
            if (isApplyingLayout)
                return;

            string viewName = IsDefaultView(currentViewName)
                ? DefaultViewName
                : currentViewName;

            GridViewLayout layout = CaptureLayout(viewName);

            if (layout == null)
                layout = new GridViewLayout();

            layout.GridKey = string.IsNullOrWhiteSpace(layout.GridKey) ? gridKey : layout.GridKey;
            layout.ViewName = viewName;
            layout.IncludeFilters = true;
            layout.FilterStateXml = filterStateXml;

            runtimeLayoutsByViewName[viewName] = layout;
            runtimeInternalFiltersByViewName[viewName] = CaptureInternalGridFiltersXml();
            runtimeColumnFiltersByViewName[viewName] = CaptureColumnFilterValuesFromLayoutOrGrid(layout);

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
            {
                SaveInternalGridFiltersForView(viewName);
                ClearRuntimeLayoutForCurrentView();
            }

            return saved;
        }

        private bool SaveAsNewView()
        {
            bool saved = persistenceService.SaveAsNewView();

            if (saved)
            {
                SaveInternalGridFiltersForView(currentViewName);
                ClearRuntimeLayoutForCurrentView();
                RequestRefreshViews();
            }

            return saved;
        }

        public bool SaveAsNewViewFromDesigner()
        {
            return SaveAsNewView();
        }

        /// <summary>
        /// Guarda la vista activa desde el selector horizontal.
        /// Si la vista activa es Predeterminada, solicita crear una vista nueva.
        /// </summary>
        public bool SaveCurrentViewFromSelector()
        {
            return SaveCurrentOrAsk();
        }

        /// <summary>
        /// Guarda el estado actual del grid como una vista nueva desde el selector horizontal.
        /// </summary>
        public bool SaveAsNewViewFromSelector()
        {
            return SaveAsNewView();
        }

        /// <summary>
        /// Duplica la vista guardada indicada. Si no es la vista activa, primero se aplica
        /// para conservar el comportamiento existente del duplicado.
        /// </summary>
        public void DuplicateViewFromSelector(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName) || IsDefaultView(viewName))
                return;

            if (!string.Equals(viewName, currentViewName, StringComparison.OrdinalIgnoreCase))
                ApplyViewByName(viewName);

            DuplicateView();
        }

        /// <summary>
        /// Renombra una vista guardada conservando su configuración, filtros internos,
        /// orden personalizado y estado runtime si existía.
        /// </summary>
        public void RenameViewFromSelector(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName) || IsDefaultView(viewName))
                return;

            CaptureRuntimeLayoutForCurrentViewIfNeeded();

            string newName = GridViewPrompt.Ask(
                "Cambiar nombre de vista",
                "Nuevo nombre:",
                viewName);

            if (string.IsNullOrWhiteSpace(newName))
                return;

            newName = newName.Trim();

            if (IsDefaultView(newName))
            {
                MessageBox.Show(
                    owner,
                    "El nombre '" + DefaultViewName + "' está reservado.",
                    "Cambiar nombre de vista",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (string.Equals(viewName, newName, StringComparison.OrdinalIgnoreCase))
                return;

            IList<GridViewLayout> views = persistenceService.LoadViews();

            if (views != null && views.Any(x =>
                x != null &&
                string.Equals(x.ViewName, newName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(
                    owner,
                    "Ya existe una vista llamada '" + newName + "'.",
                    "Cambiar nombre de vista",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            GridViewLayout layout = views == null
                ? null
                : views.FirstOrDefault(x =>
                    x != null &&
                    string.Equals(x.ViewName, viewName, StringComparison.OrdinalIgnoreCase));

            if (layout == null)
            {
                runtimeLayoutsByViewName.TryGetValue(viewName, out layout);
            }

            if (layout == null)
                return;

            layout.ViewName = newName;
            layout.GridKey = string.IsNullOrWhiteSpace(layout.GridKey) ? gridKey : layout.GridKey;
            layout.ModifiedAt = DateTime.Now;

            store.Save(layout);
            store.Delete(gridKey, viewName);

            RenameInternalFilters(viewName, newName);
            RenameRuntimeState(viewName, newName);
            RenameViewInOrder(viewName, newName);

            if (string.Equals(currentViewName, viewName, StringComparison.OrdinalIgnoreCase))
            {
                currentViewName = newName;
                PersistCurrentViewName();
            }

            RequestRefreshViews();
            UpdateButtonState();
        }

        /// <summary>
        /// Elimina la vista indicada desde el selector horizontal.
        /// </summary>
        public void DeleteViewFromSelector(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName) || IsDefaultView(viewName))
                return;

            DeleteView(viewName);
        }

        /// <summary>
        /// Abre el diseñador lateral apuntando a la vista indicada.
        /// </summary>
        public void ShowDesignerFromSelector(string viewName)
        {
            if (!string.IsNullOrWhiteSpace(viewName) &&
                !string.Equals(viewName, currentViewName, StringComparison.OrdinalIgnoreCase))
            {
                ApplyViewByName(viewName);
            }

            ToggleDesigner();
        }

        public bool IsDefaultViewName(string viewName)
        {
            return IsDefaultView(viewName);
        }

        public bool CanMoveViewLeft(string viewName)
        {
            return GetMovableViewIndex(viewName) > 0;
        }

        public bool CanMoveViewRight(string viewName)
        {
            IList<string> customViews = GetCustomViewNamesInDisplayOrder();
            int index = IndexOfView(customViews, viewName);
            return index >= 0 && index < customViews.Count - 1;
        }

        public void MoveViewLeftFromSelector(string viewName)
        {
            MoveViewInOrder(viewName, -1);
        }

        public void MoveViewRightFromSelector(string viewName)
        {
            MoveViewInOrder(viewName, 1);
        }

        private void RenameInternalFilters(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
                return;

            string xml = LoadInternalGridFiltersXml(oldName);

            if (!string.IsNullOrWhiteSpace(xml))
                SaveInternalGridFiltersForView(newName, xml);

            DeleteInternalGridFiltersForView(oldName);
        }

        private void RenameRuntimeState(string oldName, string newName)
        {
            RenameDictionaryKey(runtimeLayoutsByViewName, oldName, newName);
            RenameDictionaryKey(runtimeInternalFiltersByViewName, oldName, newName);
            RenameDictionaryKey(runtimeColumnFiltersByViewName, oldName, newName);

            GridViewLayout runtimeLayout;
            if (runtimeLayoutsByViewName.TryGetValue(newName, out runtimeLayout) && runtimeLayout != null)
                runtimeLayout.ViewName = newName;
        }

        private void RenameDictionaryKey<T>(Dictionary<string, T> dictionary, string oldName, string newName)
        {
            if (dictionary == null || string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
                return;

            T value;
            if (!dictionary.TryGetValue(oldName, out value))
                return;

            dictionary.Remove(oldName);
            dictionary[newName] = value;
        }

        private void RenameViewInOrder(string oldName, string newName)
        {
            List<string> order = LoadViewOrder();
            int index = IndexOfView(order, oldName);

            if (index < 0)
                return;

            order[index] = newName;
            SaveViewOrder(order);
        }

        private void DuplicateView()
        {
            persistenceService.DuplicateView();
            RequestRefreshViews();
        }

        private void DeleteViewFromMenu()
        {
            persistenceService.DeleteViewFromMenu();
            RequestRefreshViews();
        }

        private void DeleteView(string viewName)
        {
            persistenceService.DeleteView(viewName);
            DeleteInternalGridFiltersForView(viewName);
            RemoveViewFromOrder(viewName);
            RequestRefreshViews();
        }

        private IList<string> SortViewNamesByUserOrder(IList<string> viewNames)
        {
            if (viewNames == null || viewNames.Count == 0)
                return new List<string>();

            var result = new List<string>();

            if (viewNames.Any(x => IsDefaultView(x)))
                result.Add(DefaultViewName);

            List<string> customViews = viewNames
                .Where(x => !string.IsNullOrWhiteSpace(x) && !IsDefaultView(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            List<string> savedOrder = LoadViewOrder();

            foreach (string savedViewName in savedOrder)
            {
                string match = customViews.FirstOrDefault(x =>
                    string.Equals(x, savedViewName, StringComparison.OrdinalIgnoreCase));

                if (match == null)
                    continue;

                result.Add(match);
                customViews.Remove(match);
            }

            foreach (string remaining in customViews.OrderBy(x => x))
                result.Add(remaining);

            SaveViewOrder(result.Where(x => !IsDefaultView(x)).ToList());

            return result;
        }

        private IList<string> GetCustomViewNamesInDisplayOrder()
        {
            return GetAvailableViewNames()
                .Where(x => !IsDefaultView(x))
                .ToList();
        }

        private int GetMovableViewIndex(string viewName)
        {
            IList<string> customViews = GetCustomViewNamesInDisplayOrder();
            return IndexOfView(customViews, viewName);
        }

        private int IndexOfView(IList<string> views, string viewName)
        {
            if (views == null || string.IsNullOrWhiteSpace(viewName))
                return -1;

            for (int i = 0; i < views.Count; i++)
            {
                if (string.Equals(views[i], viewName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private void MoveViewInOrder(string viewName, int direction)
        {
            if (string.IsNullOrWhiteSpace(viewName) || IsDefaultView(viewName))
                return;

            List<string> customViews = GetCustomViewNamesInDisplayOrder().ToList();
            int currentIndex = IndexOfView(customViews, viewName);

            if (currentIndex < 0)
                return;

            int targetIndex = currentIndex + direction;

            if (targetIndex < 0 || targetIndex >= customViews.Count)
                return;

            string temp = customViews[currentIndex];
            customViews[currentIndex] = customViews[targetIndex];
            customViews[targetIndex] = temp;

            SaveViewOrder(customViews);
            RequestRefreshViews();
        }

        private void RemoveViewFromOrder(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName))
                return;

            List<string> order = LoadViewOrder();
            int index = IndexOfView(order, viewName);

            if (index < 0)
                return;

            order.RemoveAt(index);
            SaveViewOrder(order);
        }

        private List<string> LoadViewOrder()
        {
            var result = new List<string>();

            try
            {
                string path = GetViewOrderFilePath();

                if (!File.Exists(path))
                    return result;

                foreach (string line in File.ReadAllLines(path))
                {
                    string value = (line ?? string.Empty).Trim();

                    if (string.IsNullOrWhiteSpace(value) || IsDefaultView(value))
                        continue;

                    if (result.Any(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    result.Add(value);
                }
            }
            catch
            {
            }

            return result;
        }

        private void SaveViewOrder(IList<string> orderedViewNames)
        {
            try
            {
                string path = GetViewOrderFilePath();
                string folder = Path.GetDirectoryName(path);

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                File.WriteAllLines(
                    path,
                    (orderedViewNames ?? new List<string>())
                        .Where(x => !string.IsNullOrWhiteSpace(x) && !IsDefaultView(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray());
            }
            catch
            {
            }
        }

        private string GetViewOrderFilePath()
        {
            string folder = Path.Combine(Application.UserAppDataPath, "GridViews");
            Directory.CreateDirectory(folder);

            string safeName = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(gridKey ?? "Grid"))
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');

            return Path.Combine(folder, safeName + ".view-order.txt");
        }

        private void ApplyDefaultLayout()
        {
            isApplyingLayout = true;

            try
            {
                ClearGridRuntimeFilters();

                DataTable table = GetCurrentDataTable();

                if (table != null)
                    GridConfigurator.BindDataTable(grid, table, null);
                else
                    layoutService.ApplyDefaultLayout(null);

                currentViewName = DefaultViewName;
                hasChanges = false;
                PersistCurrentViewName();
            }
            finally
            {
                isApplyingLayout = false;
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

                ClearGridRuntimeFilters();

                if (applyFilterStateXml != null)
                    applyFilterStateXml(null);

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

            // executeSearch puede provocar un bind asíncrono en el formulario.
            // La selección/carga de Predeterminada no debe dejar la vista marcada como modificada.
            hasChanges = false;
            UpdateButtonState();
        }

        private GridViewLayout CaptureLayout(string viewName)
        {
            GridViewLayout layout = layoutService.Capture(viewName, gridKey, CaptureFilters);

            if (layout != null)
                CaptureInternalGridFiltersIntoLayout(layout, viewName);

            return layout;
        }

        private void ApplyLayoutStateToBoundGrid(GridViewLayout layout)
        {
            if (layout == null)
                return;

            layoutService.Apply(layout, false);

            LoadRuntimeColumnFiltersFromLayout(layout);
            ApplyInternalGridFiltersFromLayout(layout);
        }

        private void ApplyLayout(GridViewLayout layout)
        {
            if (layout == null)
                return;

            using (BeginGridVisualTransaction())
            {
                isApplyingLayout = true;

                try
                {
                    ApplyLayoutStateToBoundGrid(layout);

                    currentViewName = layout.ViewName;
                    hasChanges = false;
                    PersistCurrentViewName();
                }
                finally
                {
                    isApplyingLayout = false;
                    UpdateButtonState();
                }
            }

            QueueNormalizeSelectionAfterSort();
        }

        /// <summary>
        /// Suspende temporalmente el redibujado del SfDataGrid mientras se aplica una
        /// operación compuesta de datos + vista. No oculta el control ni cambia layout;
        /// solamente evita que Windows pinte estados intermedios.
        /// </summary>
        private IDisposable BeginGridVisualTransaction()
        {
            return new GridVisualTransaction(grid);
        }

        private sealed class GridVisualTransaction : IDisposable
        {
            private readonly SfDataGrid grid;
            private readonly IntPtr handle;
            private readonly object initialView;
            private bool disposed;

            public GridVisualTransaction(SfDataGrid grid)
            {
                this.grid = grid;

                if (grid == null || grid.IsDisposed)
                    return;

                if (grid.IsHandleCreated)
                {
                    handle = grid.Handle;
                    SendMessage(handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
                }

                grid.SuspendLayout();
                initialView = grid.View;
                BeginViewInit(initialView);
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;

                if (grid == null || grid.IsDisposed)
                    return;

                object currentView = grid.View;

                if (!ReferenceEquals(currentView, initialView))
                {
                    BeginViewInit(currentView);
                    EndViewInit(currentView);
                }

                EndViewInit(initialView);

                RefreshView(currentView);

                grid.ResumeLayout(false);

                if (handle != IntPtr.Zero)
                {
                    SendMessage(handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);

                    grid.PerformLayout();
                    grid.Invalidate(true);

                }
                else
                {
                    grid.PerformLayout();
                    grid.Invalidate(true);
                }
            }
            private static void BeginViewInit(object view)
            {
                InvokeViewMethod(view, "BeginInit");
            }

            private static void EndViewInit(object view)
            {
                InvokeViewMethod(view, "EndInit");
            }

            private static void RefreshView(object view)
            {
                InvokeViewMethod(view, "Refresh");
            }

            private static void InvokeViewMethod(object view, string methodName)
            {
                if (view == null || string.IsNullOrWhiteSpace(methodName))
                    return;

                try
                {
                    MethodInfo method = view.GetType().GetMethod(
                        methodName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        Type.EmptyTypes,
                        null);

                    if (method != null)
                        method.Invoke(view, null);
                }
                catch
                {
                    // Algunos proveedores de vista de Syncfusion pueden no exponer
                    // BeginInit/EndInit/Refresh. En ese caso la transacción de redraw
                    // del control sigue protegiendo el pintado visual.
                }
            }
        }

        private DataTable GetCurrentDataTable()
        {
            if (grid == null)
                return null;

            object source = grid.DataSource;

            if (source is DataTable table)
                return table;

            if (source is DataView view)
                return view.Table;

            if (source is BindingSource bindingSource)
            {
                if (bindingSource.DataSource is DataTable bsTable)
                    return bsTable;

                if (bindingSource.DataSource is DataView bsView)
                    return bsView.Table;
            }

            return null;
        }

        private void CaptureFilters(GridViewLayout layout)
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

        public string GetColumnSortDirection(string columnName)
        {
            if (grid == null || grid.SortColumnDescriptions == null || string.IsNullOrWhiteSpace(columnName))
                return "None";

            foreach (var sort in grid.SortColumnDescriptions)
            {
                string current = GetPropertyAsString(sort, "ColumnName");

                if (!string.Equals(current, columnName, StringComparison.OrdinalIgnoreCase))
                    continue;

                string direction = GetPropertyAsString(sort, "SortDirection");

                if (string.Equals(direction, "Ascending", StringComparison.OrdinalIgnoreCase))
                    return "Ascending";

                if (string.Equals(direction, "Descending", StringComparison.OrdinalIgnoreCase))
                    return "Descending";
            }

            return "None";
        }

        public void SetColumnSortDirection(string columnName, string direction)
        {
            if (grid == null || grid.SortColumnDescriptions == null || string.IsNullOrWhiteSpace(columnName))
                return;

            for (int i = grid.SortColumnDescriptions.Count - 1; i >= 0; i--)
            {
                string current = GetPropertyAsString(grid.SortColumnDescriptions[i], "ColumnName");

                if (string.Equals(current, columnName, StringComparison.OrdinalIgnoreCase))
                    grid.SortColumnDescriptions.RemoveAt(i);
            }

            if (string.Equals(direction, "Ascending", StringComparison.OrdinalIgnoreCase))
            {
                grid.SortColumnDescriptions.Add(new SortColumnDescription
                {
                    ColumnName = columnName,
                    SortDirection = ListSortDirection.Ascending
                });
            }
            else if (string.Equals(direction, "Descending", StringComparison.OrdinalIgnoreCase))
            {
                grid.SortColumnDescriptions.Add(new SortColumnDescription
                {
                    ColumnName = columnName,
                    SortDirection = ListSortDirection.Descending
                });
            }

            MarkChanged();
        }

        public string GetColumnFilterValue(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return string.Empty;

            string viewName = IsDefaultView(currentViewName)
                ? DefaultViewName
                : currentViewName;

            Dictionary<string, string> values;
            if (runtimeColumnFiltersByViewName.TryGetValue(viewName, out values) && values != null)
            {
                string runtimeValue;
                if (values.TryGetValue(columnName, out runtimeValue))
                    return runtimeValue ?? string.Empty;
            }

            return GetColumnFilterValueFromGrid(columnName);
        }

        public void SetColumnFilterValue(string columnName, string filterValue)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return;

            string viewName = IsDefaultView(currentViewName)
                ? DefaultViewName
                : currentViewName;

            Dictionary<string, string> values;
            if (!runtimeColumnFiltersByViewName.TryGetValue(viewName, out values) || values == null)
            {
                values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                runtimeColumnFiltersByViewName[viewName] = values;
            }

            if (string.IsNullOrWhiteSpace(filterValue))
                values.Remove(columnName);
            else
                values[columnName] = filterValue.Trim();

            RemoveColumnInternalFilter(columnName);

            if (!string.IsNullOrWhiteSpace(filterValue))
                AddTextContainsInternalFilter(columnName, filterValue.Trim());

            RefreshGridFilterView();
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

        private void InvokeNoArgumentMethod(object instance, string methodName)
        {
            if (instance == null || string.IsNullOrWhiteSpace(methodName))
                return;

            try
            {
                MethodInfo method = instance.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    Type.EmptyTypes,
                    null);

                if (method != null)
                    method.Invoke(instance, null);
            }
            catch
            {
            }
        }

        private object InvokeMethod(object instance, string methodName, object[] args)
        {
            if (instance == null || string.IsNullOrWhiteSpace(methodName))
                return null;

            try
            {
                MethodInfo method = instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(x =>
                        string.Equals(x.Name, methodName, StringComparison.OrdinalIgnoreCase) &&
                        x.GetParameters().Length == (args == null ? 0 : args.Length));

                return method == null ? null : method.Invoke(instance, args);
            }
            catch
            {
                return null;
            }
        }

        public event EventHandler DesignerRequested;
        public event EventHandler ViewChanged;

        /// <summary>
        /// Callback opcional usado por el formulario host para refrescar la barra de vistas
        /// únicamente cuando cambia la colección de vistas guardadas.
        /// No se usa ViewChanged para evitar repintados durante bind/layout del grid.
        /// </summary>
        public Action RefreshViewsRequested { get; set; }

        private void RequestRefreshViews()
        {
            if (RefreshViewsRequested != null)
                RefreshViewsRequested();
        }

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
            {
                SaveInternalGridFiltersForView(currentViewName);
                ClearRuntimeLayoutForCurrentView();
            }

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
                foreach (KeyValuePair<string, GridViewLayout> item in runtimeLayoutsByViewName.ToArray())
                {
                    GridViewLayout layout = item.Value;

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

                    string internalFilterXml;
                    if (runtimeInternalFiltersByViewName.TryGetValue(layout.ViewName, out internalFilterXml))
                        SaveInternalGridFiltersForView(layout.ViewName, internalFilterXml);
                    else
                        SaveInternalGridFiltersForView(layout.ViewName);
                }

                runtimeLayoutsByViewName.Clear();
                runtimeInternalFiltersByViewName.Clear();
                runtimeColumnFiltersByViewName.Clear();
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



        private object GetColumnFilterPredicatesCollection(GridColumn column)
        {
            if (column == null)
                return null;

            return GetPropertyValue(column, "FilterPredicates");
        }

        private void ClearPredicatesCollection(object collection)
        {
            if (collection == null)
                return;

            try
            {
                MethodInfo clearMethod = collection.GetType().GetMethod(
                    "Clear",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    Type.EmptyTypes,
                    null);

                if (clearMethod != null)
                    clearMethod.Invoke(collection, null);
            }
            catch
            {
            }
        }

        private void ClearGridRuntimeFilters()
        {
            ClearGridRuntimeFilters(true);
        }

        private void ClearGridRuntimeFilters(bool refresh)
        {
            try
            {
                if (grid != null)
                    InvokeNoArgumentMethod(grid, "ClearFilters");
            }
            catch
            {
            }

            try
            {
                if (grid != null && grid.Columns != null)
                {
                    foreach (GridColumn column in grid.Columns)
                        ClearPredicatesCollection(GetColumnFilterPredicatesCollection(column));
                }
            }
            catch
            {
            }

            try
            {
                if (grid != null && grid.View != null)
                    SetPropertyValue(grid.View, "Filter", null);
            }
            catch
            {
            }

            if (refresh)
                RefreshGridFilterView();
        }

        private void RemoveColumnInternalFilter(string columnName)
        {
            GridColumn column = FindColumn(columnName);
            if (column == null)
                return;

            try
            {
                if (grid != null)
                    InvokeMethod(grid, "ClearFilter", new object[] { columnName });
            }
            catch
            {
            }

            ClearPredicatesCollection(GetColumnFilterPredicatesCollection(column));
        }

        private void AddTextContainsInternalFilter(string columnName, string filterValue)
        {
            if (string.IsNullOrWhiteSpace(columnName) || string.IsNullOrWhiteSpace(filterValue))
                return;

            GridColumn column = FindColumn(columnName);
            if (column == null)
                return;

            object predicate = CreateTextContainsPredicate(filterValue.Trim());
            if (predicate == null)
                return;

            AddPredicateToColumn(column, predicate);
        }

        private object CreateTextContainsPredicate(string filterValue)
        {
            Type predicateType = ResolveSyncfusionType("Syncfusion.WinForms.DataGrid.FilterPredicate");
            if (predicateType == null)
                predicateType = ResolveSyncfusionType("Syncfusion.Data.FilterPredicate");

            if (predicateType == null)
                return null;

            object predicate = Activator.CreateInstance(predicateType);

            SetPropertyValue(predicate, "FilterValue", filterValue);
            SetPropertyValue(predicate, "FilterText", filterValue);
            SetPropertyValue(predicate, "ActualFilterValue", filterValue);
            SetPropertyValue(predicate, "FilterType", ConvertValueForProperty(predicate, "FilterType", "Contains"));
            SetPropertyValue(predicate, "FilterBehavior", ConvertValueForProperty(predicate, "FilterBehavior", "StringTyped"));
            SetPropertyValue(predicate, "PredicateType", ConvertValueForProperty(predicate, "PredicateType", "And"));

            return predicate;
        }

        private void AddPredicateToColumn(GridColumn column, object predicate)
        {
            object collection = GetColumnFilterPredicatesCollection(column);
            if (collection == null || predicate == null)
                return;

            try
            {
                MethodInfo addMethod = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(x =>
                        string.Equals(x.Name, "Add", StringComparison.OrdinalIgnoreCase) &&
                        x.GetParameters().Length == 1 &&
                        x.GetParameters()[0].ParameterType.IsAssignableFrom(predicate.GetType()));

                if (addMethod != null)
                    addMethod.Invoke(collection, new object[] { predicate });
            }
            catch
            {
            }
        }

        private void ApplyColumnFilterValues(Dictionary<string, string> values)
        {
            ApplyColumnFilterValues(values, true, true);
        }

        private void ApplyColumnFilterValues(
            Dictionary<string, string> values,
            bool clearExistingFilters,
            bool refreshAfterApply)
        {
            if (clearExistingFilters)
                ClearGridRuntimeFilters(false);

            if (values != null && values.Count > 0)
            {
                foreach (KeyValuePair<string, string> item in values)
                {
                    if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value))
                        continue;

                    AddTextContainsInternalFilter(item.Key, item.Value);
                }
            }

            if (refreshAfterApply)
                RefreshGridFilterView();
        }

        private void ApplyInternalGridFiltersFromLayout(GridViewLayout layout)
        {
            string viewName = layout == null || string.IsNullOrWhiteSpace(layout.ViewName)
                ? DefaultViewName
                : layout.ViewName;

            Dictionary<string, string> values = GetColumnFilterValuesFromLayout(layout);
            runtimeColumnFiltersByViewName[viewName] =
                new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);

            ApplyColumnFilterLayoutsFromLayout(layout);
        }

        private void LoadRuntimeColumnFiltersFromLayout(GridViewLayout layout)
        {
            string viewName = layout == null || string.IsNullOrWhiteSpace(layout.ViewName)
                ? DefaultViewName
                : layout.ViewName;

            runtimeColumnFiltersByViewName[viewName] =
                new Dictionary<string, string>(GetColumnFilterValuesFromLayout(layout), StringComparer.OrdinalIgnoreCase);
        }

        private Dictionary<string, string> CaptureColumnFilterValuesFromLayoutOrGrid(GridViewLayout layout)
        {
            Dictionary<string, string> result = GetColumnFilterValuesFromLayout(layout);

            foreach (KeyValuePair<string, string> item in CaptureColumnFilterValuesFromGrid())
                result[item.Key] = item.Value;

            return result;
        }

        private Dictionary<string, string> CaptureColumnFilterValuesFromGrid()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (grid == null || grid.Columns == null)
                return result;

            foreach (GridColumn column in grid.Columns)
            {
                if (column == null || string.IsNullOrWhiteSpace(column.MappingName))
                    continue;

                string value = GetColumnFilterValueFromGrid(column.MappingName);
                if (!string.IsNullOrWhiteSpace(value))
                    result[column.MappingName] = value;
            }

            return result;
        }

        private string GetColumnFilterValueFromGrid(string columnName)
        {
            GridColumn column = FindColumn(columnName);
            if (column == null)
                return string.Empty;

            object collection = GetColumnFilterPredicatesCollection(column);
            if (collection == null)
                return string.Empty;

            try
            {
                System.Collections.IEnumerable enumerable = collection as System.Collections.IEnumerable;
                if (enumerable == null)
                    return string.Empty;

                foreach (object predicate in enumerable)
                {
                    string value = GetPredicateFilterValue(predicate);
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private string GetPredicateFilterValue(object predicate)
        {
            if (predicate == null)
                return string.Empty;

            string value = GetPropertyAsString(predicate, "FilterValue");
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            value = GetPropertyAsString(predicate, "Value");
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            value = GetPropertyAsString(predicate, "FilterText");
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            value = GetPropertyAsString(predicate, "ActualFilterValue");
            return value ?? string.Empty;
        }

        private void CaptureInternalGridFiltersIntoLayout(GridViewLayout layout, string viewName)
        {
            if (layout == null)
                return;

            string runtimeViewName = string.IsNullOrWhiteSpace(viewName)
                ? DefaultViewName
                : viewName;

            List<GenesysGridColumnFilterLayout> filters = CaptureColumnFilterLayoutsFromGrid();
            SetColumnFilterLayoutsToLayout(layout, filters);

            runtimeColumnFiltersByViewName[runtimeViewName] =
                new Dictionary<string, string>(GetColumnFilterValuesFromLayout(layout), StringComparer.OrdinalIgnoreCase);
        }

        private List<GenesysGridColumnFilterLayout> CaptureColumnFilterLayoutsFromGrid()
        {
            var result = new List<GenesysGridColumnFilterLayout>();

            if (grid == null || grid.Columns == null)
                return result;

            foreach (GridColumn column in grid.Columns)
            {
                if (column == null || string.IsNullOrWhiteSpace(column.MappingName))
                    continue;

                object collection = GetColumnFilterPredicatesCollection(column);
                if (collection == null)
                    continue;

                System.Collections.IEnumerable enumerable = collection as System.Collections.IEnumerable;
                if (enumerable == null)
                    continue;

                var filter = new GenesysGridColumnFilterLayout
                {
                    ColumnName = column.MappingName
                };

                foreach (object predicate in enumerable)
                {
                    GenesysGridFilterPredicateLayout predicateLayout = CapturePredicateLayout(predicate);
                    if (predicateLayout != null)
                        filter.Predicates.Add(predicateLayout);
                }

                if (filter.Predicates.Count > 0)
                    result.Add(filter);
            }

            return result;
        }

        private GenesysGridFilterPredicateLayout CapturePredicateLayout(object predicate)
        {
            if (predicate == null)
                return null;

            var result = new GenesysGridFilterPredicateLayout
            {
                TypeName = predicate.GetType().AssemblyQualifiedName,
                FilterBehavior = GetPropertyAsString(predicate, "FilterBehavior"),
                FilterMode = GetPropertyAsString(predicate, "FilterMode"),
                FilterText = GetPropertyAsString(predicate, "FilterText"),
                FilterType = GetPropertyAsString(predicate, "FilterType"),
                FilterValue = GetPropertyAsString(predicate, "FilterValue"),
                Value = GetPropertyAsString(predicate, "Value"),
                ActualFilterValue = GetPropertyAsString(predicate, "ActualFilterValue"),
                FilterValueType = GetPropertyAsString(predicate, "FilterValueType"),
                PredicateType = GetPropertyAsString(predicate, "PredicateType")
            };

            object isCaseSensitive = GetPropertyValue(predicate, "IsCaseSensitive");
            if (isCaseSensitive is bool)
                result.IsCaseSensitive = (bool)isCaseSensitive;

            foreach (PropertyInfo property in predicate.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!property.CanRead || !property.CanWrite)
                    continue;

                object value;
                try
                {
                    value = property.GetValue(predicate, null);
                }
                catch
                {
                    continue;
                }

                Type serializedType = value == null ? property.PropertyType : value.GetType();

                if (!IsSerializableSimpleType(serializedType) && !IsSerializableSimpleType(property.PropertyType))
                    continue;

                result.Properties.Add(new GenesysGridFilterPredicatePropertyLayout
                {
                    Name = property.Name,
                    Value = value == null ? null : Convert.ToString(value),
                    TypeName = serializedType.AssemblyQualifiedName
                });
            }

            return result;
        }

        private void SetColumnFilterLayoutsToLayout(GridViewLayout layout, IList<GenesysGridColumnFilterLayout> filters)
        {
            if (layout == null)
                return;

            if (layout.GridFilters == null)
                layout.GridFilters = new List<GenesysGridColumnFilterLayout>();

            layout.GridFilters.Clear();

            if (filters == null)
                return;

            foreach (GenesysGridColumnFilterLayout filter in filters)
            {
                if (filter == null || string.IsNullOrWhiteSpace(filter.ColumnName))
                    continue;

                if (filter.Predicates == null || filter.Predicates.Count == 0)
                    continue;

                var copy = new GenesysGridColumnFilterLayout
                {
                    ColumnName = filter.ColumnName
                };

                foreach (GenesysGridFilterPredicateLayout predicate in filter.Predicates)
                {
                    if (predicate != null)
                        copy.Predicates.Add(predicate);
                }

                if (copy.Predicates.Count > 0)
                    layout.GridFilters.Add(copy);
            }
        }

        private void ApplyColumnFilterLayoutsFromLayout(GridViewLayout layout)
        {
            ClearGridRuntimeFilters(false);

            if (layout == null || layout.GridFilters == null || layout.GridFilters.Count == 0)
            {
                RefreshGridFilterView();
                return;
            }

            foreach (GenesysGridColumnFilterLayout filter in layout.GridFilters)
            {
                if (filter == null || string.IsNullOrWhiteSpace(filter.ColumnName))
                    continue;

                GridColumn column = FindColumn(filter.ColumnName);
                if (column == null || filter.Predicates == null)
                    continue;

                foreach (GenesysGridFilterPredicateLayout predicateLayout in filter.Predicates)
                {
                    object predicate = CreatePredicateFromLayout(predicateLayout);
                    if (predicate != null)
                        AddPredicateToColumn(column, predicate);
                }
            }

            RefreshGridFilterView();
        }

        private object CreatePredicateFromLayout(GenesysGridFilterPredicateLayout predicateLayout)
        {
            if (predicateLayout == null)
                return null;

            Type predicateType = ResolveSyncfusionType(predicateLayout.TypeName);
            if (predicateType == null)
                predicateType = ResolveSyncfusionType("Syncfusion.WinForms.DataGrid.FilterPredicate");
            if (predicateType == null)
                predicateType = ResolveSyncfusionType("Syncfusion.Data.FilterPredicate");
            if (predicateType == null)
                return null;

            object predicate;
            try
            {
                predicate = Activator.CreateInstance(predicateType);
            }
            catch
            {
                return null;
            }

            if (predicateLayout.Properties != null)
            {
                foreach (GenesysGridFilterPredicatePropertyLayout property in predicateLayout.Properties)
                {
                    if (property == null || string.IsNullOrWhiteSpace(property.Name))
                        continue;

                    SetPropertyValue(predicate, property.Name, ConvertSerializedValueForProperty(predicate, property.Name, property.Value, property.TypeName));
                }
            }

            ApplyPredicateFallbackValue(predicate, "FilterBehavior", predicateLayout.FilterBehavior);
            ApplyPredicateFallbackValue(predicate, "FilterMode", predicateLayout.FilterMode);
            ApplyPredicateFallbackValue(predicate, "FilterText", predicateLayout.FilterText);
            ApplyPredicateFallbackValue(predicate, "FilterType", predicateLayout.FilterType);
            ApplyPredicateFallbackValue(predicate, "FilterValue", predicateLayout.FilterValue);
            ApplyPredicateFallbackValue(predicate, "Value", predicateLayout.Value);
            ApplyPredicateFallbackValue(predicate, "ActualFilterValue", predicateLayout.ActualFilterValue);
            ApplyPredicateFallbackValue(predicate, "FilterValueType", predicateLayout.FilterValueType);
            ApplyPredicateFallbackValue(predicate, "PredicateType", predicateLayout.PredicateType);
            ApplyPredicateFallbackValue(predicate, "IsCaseSensitive", predicateLayout.IsCaseSensitive ? "True" : null);

            return predicate;
        }

        private void ApplyPredicateFallbackValue(object predicate, string propertyName, string value)
        {
            if (predicate == null || string.IsNullOrWhiteSpace(propertyName) || string.IsNullOrWhiteSpace(value))
                return;

            PropertyInfo property = predicate.GetType().GetProperty(propertyName);
            if (property == null || !property.CanWrite)
                return;

            object currentValue = null;
            try
            {
                currentValue = property.CanRead ? property.GetValue(predicate, null) : null;
            }
            catch
            {
            }

            if (currentValue != null && !IsDefaultValue(currentValue))
                return;

            SetPropertyValue(predicate, propertyName, ConvertSerializedValueForProperty(predicate, propertyName, value));
        }

        private bool IsDefaultValue(object value)
        {
            if (value == null)
                return true;

            Type type = value.GetType();

            if (type == typeof(string))
                return string.IsNullOrWhiteSpace(Convert.ToString(value));

            if (!type.IsValueType)
                return false;

            object defaultValue = Activator.CreateInstance(type);
            return object.Equals(value, defaultValue);
        }

        private Dictionary<string, string> GetColumnFilterValuesFromLayout(GridViewLayout layout)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (layout == null)
                return result;

            object filters = GetPropertyValue(layout, "GridFilters");
            if (filters == null)
                return result;

            System.Collections.IEnumerable enumerable = filters as System.Collections.IEnumerable;
            if (enumerable == null)
                return result;

            foreach (object filter in enumerable)
            {
                if (filter == null)
                    continue;

                string columnName = GetPropertyAsString(filter, "ColumnName");
                if (string.IsNullOrWhiteSpace(columnName))
                    columnName = GetPropertyAsString(filter, "MappingName");

                if (string.IsNullOrWhiteSpace(columnName))
                    continue;

                string value = GetPropertyAsString(filter, "FilterValue");
                if (string.IsNullOrWhiteSpace(value))
                    value = GetPropertyAsString(filter, "Value");

                if (string.IsNullOrWhiteSpace(value))
                    value = GetPropertyAsString(filter, "FilterText");

                if (string.IsNullOrWhiteSpace(value))
                    value = GetFirstPredicateValueFromLayoutFilter(filter);

                if (!string.IsNullOrWhiteSpace(value))
                    result[columnName] = value;
            }

            return result;
        }

        private string GetFirstPredicateValueFromLayoutFilter(object filter)
        {
            if (filter == null)
                return string.Empty;

            object predicates = GetPropertyValue(filter, "Predicates");
            if (predicates == null)
                return string.Empty;

            System.Collections.IEnumerable enumerable = predicates as System.Collections.IEnumerable;
            if (enumerable == null)
                return string.Empty;

            foreach (object predicate in enumerable)
            {
                string value = GetPropertyAsString(predicate, "FilterValue");
                if (string.IsNullOrWhiteSpace(value))
                    value = GetPropertyAsString(predicate, "Value");

                if (string.IsNullOrWhiteSpace(value))
                    value = GetPropertyAsString(predicate, "Value");

                if (string.IsNullOrWhiteSpace(value))
                    value = GetPropertyAsString(predicate, "FilterText");

                if (string.IsNullOrWhiteSpace(value))
                    value = GetPropertyAsString(predicate, "ActualFilterValue");

                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return string.Empty;
        }

        private void SetColumnFilterValuesToLayout(GridViewLayout layout, Dictionary<string, string> values)
        {
            object filters = GetPropertyValue(layout, "GridFilters");
            if (filters == null)
                return;

            ClearPredicatesCollection(filters);

            if (values == null || values.Count == 0)
                return;

            Type itemType = GetCollectionItemType(filters.GetType());
            if (itemType == null)
                return;

            foreach (KeyValuePair<string, string> item in values.OrderBy(x => x.Key))
            {
                if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value))
                    continue;

                object filter = Activator.CreateInstance(itemType);

                SetPropertyValue(filter, "ColumnName", item.Key);
                SetPropertyValue(filter, "MappingName", item.Key);

                // Algunas versiones/modelos tienen el valor directamente en el filtro.
                SetPropertyValue(filter, "FilterValue", item.Value);
                SetPropertyValue(filter, "Value", item.Value);
                SetPropertyValue(filter, "FilterText", item.Value);
                SetPropertyValue(filter, "ActualFilterValue", item.Value);
                SetPropertyValue(filter, "FilterType", ConvertValueForProperty(filter, "FilterType", "Contains"));
                SetPropertyValue(filter, "FilterBehavior", ConvertValueForProperty(filter, "FilterBehavior", "StringTyped"));
                SetPropertyValue(filter, "PredicateType", ConvertValueForProperty(filter, "PredicateType", "And"));

                // En tu XML real el esquema es GridFilters -> GenesysGridColumnFilterLayout -> Predicates.
                // Por eso también debemos insertar un predicado dentro de la colección Predicates;
                // si no, se guarda <Predicates /> y al volver a cargar no hay valor que restaurar.
                AddFilterPredicateToLayoutFilter(filter, item.Value);

                AddItemToCollection(filters, filter);
            }
        }

        private void AddFilterPredicateToLayoutFilter(object filter, string filterValue)
        {
            if (filter == null || string.IsNullOrWhiteSpace(filterValue))
                return;

            object predicates = GetPropertyValue(filter, "Predicates");
            if (predicates == null)
                return;

            Type predicateType = GetCollectionItemType(predicates.GetType());
            if (predicateType == null)
                return;

            object predicate = Activator.CreateInstance(predicateType);

            SetPropertyValue(predicate, "FilterValue", filterValue);
            SetPropertyValue(predicate, "Value", filterValue);
            SetPropertyValue(predicate, "FilterText", filterValue);
            SetPropertyValue(predicate, "ActualFilterValue", filterValue);
            SetPropertyValue(predicate, "FilterType", ConvertValueForProperty(predicate, "FilterType", "Contains"));
            SetPropertyValue(predicate, "FilterBehavior", ConvertValueForProperty(predicate, "FilterBehavior", "StringTyped"));
            SetPropertyValue(predicate, "PredicateType", ConvertValueForProperty(predicate, "PredicateType", "And"));

            AddItemToCollection(predicates, predicate);
        }

        private Type GetCollectionItemType(Type collectionType)
        {
            if (collectionType == null)
                return null;

            if (collectionType.IsArray)
                return collectionType.GetElementType();

            if (collectionType.IsGenericType)
                return collectionType.GetGenericArguments().FirstOrDefault();

            Type enumerableType = collectionType.GetInterfaces()
                .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            return enumerableType == null
                ? null
                : enumerableType.GetGenericArguments().FirstOrDefault();
        }

        private void AddItemToCollection(object collection, object item)
        {
            if (collection == null || item == null)
                return;

            try
            {
                MethodInfo addMethod = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(x =>
                        string.Equals(x.Name, "Add", StringComparison.OrdinalIgnoreCase) &&
                        x.GetParameters().Length == 1 &&
                        x.GetParameters()[0].ParameterType.IsAssignableFrom(item.GetType()));

                if (addMethod != null)
                    addMethod.Invoke(collection, new object[] { item });
            }
            catch
            {
            }
        }

        private string CaptureInternalGridFiltersXml()
        {
            return SerializeInternalGridFilterState(CaptureInternalGridFiltersState());
        }

        private InternalGridFilterState CaptureInternalGridFiltersState()
        {
            var state = new InternalGridFilterState();

            if (grid == null || grid.Columns == null)
                return state;

            foreach (GridColumn column in grid.Columns)
            {
                if (column == null || string.IsNullOrWhiteSpace(column.MappingName))
                    continue;

                object collection = GetColumnFilterPredicatesCollection(column);
                if (collection == null)
                    continue;

                try
                {
                    System.Collections.IEnumerable enumerable = collection as System.Collections.IEnumerable;
                    if (enumerable == null)
                        continue;

                    foreach (object predicate in enumerable)
                        AddCapturedPredicate(state, column.MappingName, predicate);
                }
                catch
                {
                }
            }

            return state;
        }

        private void ApplyInternalGridFiltersXml(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                ClearGridRuntimeFilters();
                return;
            }

            InternalGridFilterState state = DeserializeInternalGridFilterState(xml);
            if (state == null || state.Columns == null)
            {
                ClearGridRuntimeFilters();
                return;
            }

            ClearGridRuntimeFilters(false);

            foreach (InternalGridColumnFilterState columnState in state.Columns)
            {
                if (columnState == null || string.IsNullOrWhiteSpace(columnState.ColumnName) || columnState.Predicates == null)
                    continue;

                GridColumn column = FindColumn(columnState.ColumnName);
                if (column == null)
                    continue;

                foreach (InternalGridFilterPredicateState predicateState in columnState.Predicates)
                {
                    object predicate = CreatePredicateFromInternalState(predicateState);
                    if (predicate != null)
                        AddPredicateToColumn(column, predicate);
                }
            }

            RefreshGridFilterView();
        }

        private object CreatePredicateFromInternalState(InternalGridFilterPredicateState predicateState)
        {
            if (predicateState == null)
                return null;

            Type predicateType = ResolveSyncfusionType(predicateState.TypeName);
            if (predicateType == null)
                predicateType = ResolveSyncfusionType("Syncfusion.WinForms.DataGrid.FilterPredicate");
            if (predicateType == null)
                predicateType = ResolveSyncfusionType("Syncfusion.Data.FilterPredicate");
            if (predicateType == null)
                return null;

            object predicate;
            try
            {
                predicate = Activator.CreateInstance(predicateType);
            }
            catch
            {
                return null;
            }

            if (predicateState.Properties != null)
            {
                foreach (InternalGridFilterPropertyState property in predicateState.Properties)
                {
                    if (property == null || string.IsNullOrWhiteSpace(property.Name))
                        continue;

                    SetPropertyValue(predicate, property.Name, ConvertSerializedValueForProperty(predicate, property.Name, property.Value, property.TypeName));
                }
            }

            return predicate;
        }

        private string GetPredicateStateValue(InternalGridFilterPredicateState predicateState)
        {
            if (predicateState == null || predicateState.Properties == null)
                return string.Empty;

            string[] names = new[] { "FilterValue", "Value", "FilterText", "ActualFilterValue" };

            foreach (string name in names)
            {
                InternalGridFilterPropertyState property = predicateState.Properties.FirstOrDefault(x =>
                    string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

                if (property != null && !string.IsNullOrWhiteSpace(property.Value))
                    return property.Value;
            }

            return string.Empty;
        }

        private void AddCapturedPredicate(InternalGridFilterState state, string columnName, object predicate)
        {
            if (state == null || string.IsNullOrWhiteSpace(columnName) || predicate == null)
                return;

            InternalGridColumnFilterState columnState = state.Columns.FirstOrDefault(x =>
                string.Equals(x.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));

            if (columnState == null)
            {
                columnState = new InternalGridColumnFilterState { ColumnName = columnName };
                state.Columns.Add(columnState);
            }

            var predicateState = new InternalGridFilterPredicateState
            {
                TypeName = predicate.GetType().AssemblyQualifiedName
            };

            foreach (PropertyInfo property in predicate.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!property.CanRead || !property.CanWrite)
                    continue;

                object value;
                try
                {
                    value = property.GetValue(predicate, null);
                }
                catch
                {
                    continue;
                }

                Type serializedType = value == null ? property.PropertyType : value.GetType();

                if (!IsSerializableSimpleType(serializedType) && !IsSerializableSimpleType(property.PropertyType))
                    continue;

                predicateState.Properties.Add(new InternalGridFilterPropertyState
                {
                    Name = property.Name,
                    Value = value == null ? null : Convert.ToString(value),
                    TypeName = serializedType.AssemblyQualifiedName
                });
            }

            columnState.Predicates.Add(predicateState);
        }

        private bool IsSerializableSimpleType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.IsEnum ||
                   type == typeof(string) ||
                   type == typeof(bool) ||
                   type == typeof(byte) ||
                   type == typeof(short) ||
                   type == typeof(int) ||
                   type == typeof(long) ||
                   type == typeof(float) ||
                   type == typeof(double) ||
                   type == typeof(decimal) ||
                   type == typeof(DateTime) ||
                   type == typeof(Type);
        }

        private object ConvertSerializedValueForProperty(object instance, string propertyName, string value)
        {
            return ConvertSerializedValueForProperty(instance, propertyName, value, null);
        }

        private object ConvertSerializedValueForProperty(object instance, string propertyName, string value, string serializedTypeName)
        {
            if (instance == null || string.IsNullOrWhiteSpace(propertyName))
                return value;

            PropertyInfo property = instance.GetType().GetProperty(propertyName);
            if (property == null || !property.CanWrite)
                return value;

            Type targetType = property.PropertyType;

            if (targetType == typeof(object) && !string.IsNullOrWhiteSpace(serializedTypeName))
            {
                Type serializedType = ResolveSyncfusionType(serializedTypeName);
                if (serializedType != null)
                    targetType = serializedType;
            }

            return ConvertTextToType(value, targetType);
        }

        private object ConvertValueForProperty(object instance, string propertyName, string value)
        {
            return ConvertSerializedValueForProperty(instance, propertyName, value);
        }

        private object ConvertTextToType(string value, Type targetType)
        {
            targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (targetType == typeof(string))
                return value;

            if (targetType == typeof(Type))
                return ResolveSyncfusionType(value);

            if (targetType.IsEnum)
            {
                try { return Enum.Parse(targetType, value, true); }
                catch { return Activator.CreateInstance(targetType); }
            }

            if (string.IsNullOrWhiteSpace(value))
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            try
            {
                if (targetType == typeof(bool)) return bool.Parse(value);
                if (targetType == typeof(byte)) return byte.Parse(value);
                if (targetType == typeof(short)) return short.Parse(value);
                if (targetType == typeof(int)) return int.Parse(value);
                if (targetType == typeof(long)) return long.Parse(value);
                if (targetType == typeof(float)) return float.Parse(value);
                if (targetType == typeof(double)) return double.Parse(value);
                if (targetType == typeof(decimal)) return decimal.Parse(value);
                if (targetType == typeof(DateTime)) return DateTime.Parse(value);
            }
            catch
            {
            }

            return value;
        }

        private Type ResolveSyncfusionType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return null;

            Type type = Type.GetType(typeName, false);
            if (type != null)
                return type;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = assembly.GetType(typeName, false);
                    if (type != null)
                        return type;
                }
                catch
                {
                }
            }

            return null;
        }

        private void RefreshGridFilterView()
        {
            try
            {
                if (grid != null && grid.View != null)
                    InvokeNoArgumentMethod(grid.View, "RefreshFilter");
            }
            catch
            {
            }

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
                    grid.Refresh();
            }
            catch
            {
            }
        }

        private string GetInternalFiltersFilePath()
        {
            string folder = Path.Combine(Application.UserAppDataPath, "GridViews");
            Directory.CreateDirectory(folder);

            string safeName = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(gridKey ?? "Grid"))
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');

            return Path.Combine(folder, safeName + ".syncfusion-filters.xml");
        }

        private Dictionary<string, string> LoadInternalFiltersStore()
        {
            try
            {
                string path = GetInternalFiltersFilePath();
                if (!File.Exists(path))
                    return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                var serializer = new XmlSerializer(typeof(InternalGridFilterStore));
                using (var reader = new StreamReader(path))
                {
                    InternalGridFilterStore store = serializer.Deserialize(reader) as InternalGridFilterStore;
                    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    if (store != null && store.Views != null)
                    {
                        foreach (InternalGridViewFilterState view in store.Views)
                        {
                            if (view != null && !string.IsNullOrWhiteSpace(view.ViewName))
                                result[view.ViewName] = view.FilterXml;
                        }
                    }

                    return result;
                }
            }
            catch
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private void SaveInternalFiltersStore(Dictionary<string, string> values)
        {
            try
            {
                var store = new InternalGridFilterStore();

                if (values != null)
                {
                    foreach (KeyValuePair<string, string> item in values.OrderBy(x => x.Key))
                    {
                        if (string.IsNullOrWhiteSpace(item.Key))
                            continue;

                        store.Views.Add(new InternalGridViewFilterState
                        {
                            ViewName = item.Key,
                            FilterXml = item.Value
                        });
                    }
                }

                string path = GetInternalFiltersFilePath();
                var serializer = new XmlSerializer(typeof(InternalGridFilterStore));
                using (var writer = new StreamWriter(path, false))
                    serializer.Serialize(writer, store);
            }
            catch
            {
            }
        }

        private string LoadInternalGridFiltersXml(string viewName)
        {
            if (IsDefaultView(viewName))
                return null;

            Dictionary<string, string> values = LoadInternalFiltersStore();
            string xml;
            return values.TryGetValue(viewName, out xml) ? xml : null;
        }

        private void SaveInternalGridFiltersForView(string viewName)
        {
            SaveInternalGridFiltersForView(viewName, CaptureInternalGridFiltersXml());
        }

        private void SaveInternalGridFiltersForView(string viewName, string filterXml)
        {
            if (IsDefaultView(viewName))
                return;

            Dictionary<string, string> values = LoadInternalFiltersStore();
            values[viewName] = filterXml;
            SaveInternalFiltersStore(values);
        }

        private void DeleteInternalGridFiltersForView(string viewName)
        {
            if (IsDefaultView(viewName))
                return;

            Dictionary<string, string> values = LoadInternalFiltersStore();
            if (values.Remove(viewName))
                SaveInternalFiltersStore(values);
        }

        private string SerializeInternalGridFilterState(InternalGridFilterState state)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(InternalGridFilterState));
                using (var writer = new StringWriter())
                {
                    serializer.Serialize(writer, state ?? new InternalGridFilterState());
                    return writer.ToString();
                }
            }
            catch
            {
                return null;
            }
        }

        private InternalGridFilterState DeserializeInternalGridFilterState(string xml)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(InternalGridFilterState));
                using (var reader = new StringReader(xml))
                    return serializer.Deserialize(reader) as InternalGridFilterState;
            }
            catch
            {
                return null;
            }
        }

        [Serializable]
        public class InternalGridFilterStore
        {
            public List<InternalGridViewFilterState> Views { get; set; } = new List<InternalGridViewFilterState>();
        }

        [Serializable]
        public class InternalGridViewFilterState
        {
            public string ViewName { get; set; }
            public string FilterXml { get; set; }
        }

        [Serializable]
        public class InternalGridFilterState
        {
            public List<InternalGridColumnFilterState> Columns { get; set; } = new List<InternalGridColumnFilterState>();
        }

        [Serializable]
        public class InternalGridColumnFilterState
        {
            public string ColumnName { get; set; }
            public List<InternalGridFilterPredicateState> Predicates { get; set; } = new List<InternalGridFilterPredicateState>();
        }

        [Serializable]
        public class InternalGridFilterPredicateState
        {
            public string TypeName { get; set; }
            public List<InternalGridFilterPropertyState> Properties { get; set; } = new List<InternalGridFilterPropertyState>();
        }

        [Serializable]
        public class InternalGridFilterPropertyState
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public string TypeName { get; set; }
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

            CaptureVisibleGridFiltersIntoRuntimeState();

            MarkChanged();
        }
        private void CaptureVisibleGridFiltersIntoRuntimeState()
        {
            string viewName = IsDefaultView(currentViewName)
                ? DefaultViewName
                : currentViewName;

            GridViewLayout runtimeLayout = CaptureLayout(viewName);

            if (runtimeLayout == null)
                return;

            SetColumnFilterLayoutsToLayout(runtimeLayout, CaptureColumnFilterLayoutsFromGrid());
            runtimeLayoutsByViewName[viewName] = runtimeLayout;
            runtimeColumnFiltersByViewName[viewName] =
                new Dictionary<string, string>(GetColumnFilterValuesFromLayout(runtimeLayout), StringComparer.OrdinalIgnoreCase);
        }
        private void QueueNormalizeSelectionAfterSort()
        {
            if (normalizeSelectionAfterSortPending)
                return;

            if (grid == null || grid.IsDisposed || !grid.IsHandleCreated)
                return;

            normalizeSelectionAfterSortPending = true;

            var timer = new Timer();
            timer.Interval = 50;
            timer.Tick += delegate
            {
                timer.Stop();
                timer.Dispose();

                normalizeSelectionAfterSortPending = false;
                NormalizeSelectionToFirstVisibleRecord();
            };

            timer.Start();
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
            runtimeInternalFiltersByViewName.Clear();
            runtimeColumnFiltersByViewName.Clear();

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


public class GridViewMenuService : IDisposable
    {
        private readonly Form owner;
        private readonly SfDataGrid grid;
        private readonly ToolStripButton button;
        private readonly string defaultViewName;
        private readonly Func<IList<GridViewLayout>> loadViews;
        private readonly Func<string> getCurrentViewName;
        private readonly Func<string, bool> isDefaultView;
        private readonly Action applyDefaultLayout;
        private readonly Action<GridViewLayout> applyLayout;
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

        public GridViewMenuService(
            Form owner,
            SfDataGrid grid,
            ToolStripButton button,
            string defaultViewName,
            Func<IList<GridViewLayout>> loadViews,
            Func<string> getCurrentViewName,
            Func<string, bool> isDefaultView,
            Action applyDefaultLayout,
            Action<GridViewLayout> applyLayout,
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

            IList<GridViewLayout> views = loadViews == null
                ? new List<GridViewLayout>()
                : loadViews();

            if (views == null)
                views = new List<GridViewLayout>();

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
                        var layout = menuItem == null ? null : menuItem.Tag as GridViewLayout;

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

                string format = GetSummaryFormatFromColumn(column, summaryType);

                if (addOrReplaceSummary != null) addOrReplaceSummary(column.MappingName, summaryType, format);
                if (markChanged != null) markChanged();
            };

            return item;
        }

        private string GetSummaryFormatFromColumn(GridColumn column, string summaryType)
        {
            if (string.Equals(summaryType, "Count", StringComparison.OrdinalIgnoreCase))
                return "N0";

            if (column != null && !string.IsNullOrWhiteSpace(column.Format))
                return column.Format;

            return "N2";
        }

        private void ShowSummaryEditor()
        {
            using (var dialog = new GridSummaryPrompt(grid))
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

public class GridLayoutService
    {
        private readonly SfDataGrid grid;
        private readonly GridSummaryService summaryService;
        private readonly Dictionary<GridViewLayout, List<RuntimeColumnFilterSnapshot>> runtimeFilterSnapshots;

        public GridLayoutService(
            SfDataGrid grid,
            GridSummaryService summaryService,
            Func<bool> getIsApplyingLayout,
            Action<bool> setIsApplyingLayout)
        {
            this.grid = grid;
            this.summaryService = summaryService;
            runtimeFilterSnapshots = new Dictionary<GridViewLayout, List<RuntimeColumnFilterSnapshot>>();
        }

        // ─────────────────────────────────────────────────────────────
        // API pública
        // ─────────────────────────────────────────────────────────────

        public GridViewLayout Capture(
            string viewName,
            string gridKey,
            Action<GridViewLayout> captureFilters)
        {
            var layout = new GridViewLayout
            {
                GridKey = gridKey,
                ViewName = viewName,
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now
            };

            int index = 0;

            foreach (GridColumn column in grid.Columns)
            {
                System.Diagnostics.Debug.WriteLine(       "CAPTURE VIEW [" + viewName + "] " +        column.MappingName +
                                                        " | Type=" + column.GetType().FullName +
                                                         " | Format=[" + column.Format + "]");

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
            summaryService.Capture(layout);
            CaptureSorts(layout);
            CaptureRuntimeFilters(layout);

            if (captureFilters != null)
                captureFilters(layout);

            return layout;
        }

        public void Apply(GridViewLayout layout)
        {
            Apply(layout, false);
        }

        public void Apply(GridViewLayout layout, bool applyNativeLayout)
        {
            if (layout == null)
                return;

            // Native layout de Syncfusion queda retirado. La vista se aplica manualmente.
            // Importante: al cambiar entre vistas NO se limpian filtros aquí.
            // Los filtros se reemplazan al final desde VistasAdministrador para evitar
            // un estado intermedio visible donde el grid aparece sin filtro.
            ResetLayoutState(false);

            ApplyColumns(layout);
            ReorderColumns(layout);
            ApplyGroups(layout);
            summaryService.Apply(layout);
            ApplySorts(layout);
        }

        public void ApplyDefaultLayout(string ignoredNativeLayoutXml)
        {
            ClearNativeGridRuntimeState();
            ResetLayoutState(true);

            foreach (GridColumn column in grid.Columns)
                column.Visible = true;

            grid.AutoSizeColumnsMode = AutoSizeColumnsMode.AllCells;
        }

        // ─────────────────────────────────────────────────────────────
        // Reset centralizado del estado visual/runtime del grid
        // Mantener juntos: groups, sorts, filtros internos y summaries.
        // ─────────────────────────────────────────────────────────────

        private void ResetLayoutState(bool clearFilters)
        {
            ClearGroupDescriptions();
            ClearSortDescriptions();

            if (clearFilters)
                ClearFilterDescriptions();

            summaryService.ClearSummaryRows();
        }

        // ─────────────────────────────────────────────────────────────
        // Columnas: ancho, visible, formato y orden visual
        // ─────────────────────────────────────────────────────────────

        private void ApplyColumns(GridViewLayout layout)
        {
            if (layout.Columns == null)
                return;

            foreach (var savedColumn in layout.Columns.OrderBy(x => x.DisplayIndex))
            {
                if (savedColumn == null || string.IsNullOrWhiteSpace(savedColumn.MappingName))
                    continue;

                GridColumn column = FindColumn(savedColumn.MappingName);
                if (column == null)
                    continue;

                column.Visible = savedColumn.Visible;

                if (savedColumn.Width > 0)
                    column.Width = savedColumn.Width;

                // Siempre asignar formato, incluso vacío.
                // Si no se limpia aquí, el formato runtime de una vista anterior
                // puede quedarse vivo al aplicar otra vista.
                column.Format = savedColumn.Format ?? string.Empty;
            }
        }

        private void ReorderColumns(GridViewLayout layout)
        {
            if (layout == null || layout.Columns == null)
                return;

            int targetIndex = 0;

            foreach (var savedColumn in layout.Columns.OrderBy(x => x.DisplayIndex))
            {
                if (savedColumn == null || string.IsNullOrWhiteSpace(savedColumn.MappingName))
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

        // ─────────────────────────────────────────────────────────────
        // Sort interno del SfDataGrid
        // ─────────────────────────────────────────────────────────────

        private void CaptureSorts(GridViewLayout layout)
        {
            if (grid.SortColumnDescriptions == null)
                return;

            foreach (var sort in grid.SortColumnDescriptions)
            {
                string columnName = GetPropertyAsString(sort, "ColumnName");
                string sortDirection = GetPropertyAsString(sort, "SortDirection");

                if (!string.IsNullOrWhiteSpace(columnName))
                {
                    layout.Sorts.Add(new GenesysGridSortLayout
                    {
                        ColumnName = columnName,
                        SortDirection = sortDirection
                    });
                }
            }
        }

        private void ApplySorts(GridViewLayout layout)
        {
            ClearSortDescriptions();

            if (layout.Sorts == null)
                return;

            foreach (var sort in layout.Sorts)
            {
                if (string.IsNullOrWhiteSpace(sort.ColumnName) || FindColumn(sort.ColumnName) == null)
                    continue;

                ListSortDirection direction = ListSortDirection.Ascending;

                if (string.Equals(sort.SortDirection, "Descending", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(sort.SortDirection, "Desc", StringComparison.OrdinalIgnoreCase))
                {
                    direction = ListSortDirection.Descending;
                }

                grid.SortColumnDescriptions.Add(new SortColumnDescription
                {
                    ColumnName = sort.ColumnName,
                    SortDirection = direction
                });
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Groups internos del SfDataGrid
        // ─────────────────────────────────────────────────────────────

        private void CaptureGroups(GridViewLayout layout)
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

        private void ApplyGroups(GridViewLayout layout)
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

        // ─────────────────────────────────────────────────────────────
        // Filtros internos del SfDataGrid
        // Se capturan/aplican como estado runtime temporal asociado al layout.
        // ─────────────────────────────────────────────────────────────

        private void CaptureRuntimeFilters(GridViewLayout layout)
        {
            if (layout == null || grid == null || grid.Columns == null)
                return;

            var snapshots = new List<RuntimeColumnFilterSnapshot>();

            foreach (GridColumn column in grid.Columns)
            {
                if (column == null || string.IsNullOrWhiteSpace(column.MappingName))
                    continue;

                object predicates = GetPropertyValue(column, "FilterPredicates");
                if (predicates == null)
                    continue;

                var values = new List<object>();

                foreach (object predicate in EnumerateCollection(predicates))
                {
                    if (predicate != null)
                        values.Add(ClonePredicate(predicate));
                }

                if (values.Count > 0)
                {
                    snapshots.Add(new RuntimeColumnFilterSnapshot
                    {
                        ColumnName = column.MappingName,
                        Predicates = values
                    });
                }
            }

            if (snapshots.Count > 0)
                runtimeFilterSnapshots[layout] = snapshots;
        }

        private void ClearFilterDescriptions()
        {
            try
            {
                if (grid == null)
                    return;

                MethodInfo clearFilters = grid.GetType().GetMethod(
                    "ClearFilters",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    Type.EmptyTypes,
                    null);

                if (clearFilters != null)
                {
                    clearFilters.Invoke(grid, null);
                    return;
                }

                MethodInfo clearFilter = grid.GetType().GetMethod(
                    "ClearFilter",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    Type.EmptyTypes,
                    null);

                if (clearFilter != null)
                    clearFilter.Invoke(grid, null);
            }
            catch
            {
            }

            try
            {
                if (grid == null || grid.Columns == null)
                    return;

                foreach (GridColumn column in grid.Columns)
                {
                    object predicates = GetPropertyValue(column, "FilterPredicates");
                    InvokeClear(predicates);
                }
            }
            catch
            {
            }

            try
            {
                if (grid != null && grid.View != null)
                    ClearCollectionProperty(grid.View, "FilterPredicates");
            }
            catch
            {
            }

            RefreshFilterView();
        }

        // ─────────────────────────────────────────────────────────────
        // Limpieza de estado visual/runtime del grid
        // ─────────────────────────────────────────────────────────────

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

        private void ClearNativeGridRuntimeState()
        {
            InvokeGridMethodIfExists("ClearFilters");
            InvokeGridMethodIfExists("ClearFilter");
            InvokeGridMethodIfExists("ClearSorting");
            InvokeGridMethodIfExists("ClearSort");

            ClearCollectionProperty(grid, "FilterSettings");
            ClearCollectionProperty(grid, "FilterPredicates");
            ClearCollectionProperty(grid, "SortColumnDescriptions");
            ClearCollectionProperty(grid, "GroupColumnDescriptions");

            try
            {
                if (grid != null && grid.View != null)
                {
                    ClearCollectionProperty(grid.View, "FilterPredicates");
                    ClearCollectionProperty(grid.View, "SortDescriptions");
                }
            }
            catch
            {
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Helpers reflection para APIs variables de Syncfusion
        // ─────────────────────────────────────────────────────────────

        private void InvokeGridMethodIfExists(string methodName)
        {
            try
            {
                MethodInfo method = grid == null
                    ? null
                    : grid.GetType().GetMethod(
                        methodName,
                        BindingFlags.Instance | BindingFlags.Public,
                        null,
                        Type.EmptyTypes,
                        null);

                if (method == null)
                    return;

                method.Invoke(grid, null);
            }
            catch
            {
            }
        }

        private void ClearCollectionProperty(object target, string propertyName)
        {
            try
            {
                if (target == null || string.IsNullOrWhiteSpace(propertyName))
                    return;

                PropertyInfo property = target.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Instance | BindingFlags.Public);

                if (property == null)
                    return;

                object value = property.GetValue(target, null);
                if (value == null)
                    return;

                InvokeClear(value);
            }
            catch
            {
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Helpers generales del grid
        // ─────────────────────────────────────────────────────────────

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

        private void RefreshFilterView()
        {
            try
            {
                if (grid == null || grid.View == null)
                    return;

                MethodInfo refreshFilter = grid.View.GetType().GetMethod(
                    "RefreshFilter",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    Type.EmptyTypes,
                    null);

                if (refreshFilter != null)
                {
                    refreshFilter.Invoke(grid.View, null);
                    return;
                }

                grid.View.Refresh();
            }
            catch
            {
            }
        }

        private object GetPropertyValue(object instance, string propertyName)
        {
            if (instance == null || string.IsNullOrWhiteSpace(propertyName))
                return null;

            PropertyInfo property = instance.GetType().GetProperty(propertyName);
            if (property == null)
                return null;

            return property.GetValue(instance, null);
        }

        private string GetPropertyAsString(object instance, string propertyName)
        {
            object value = GetPropertyValue(instance, propertyName);
            return value == null ? null : Convert.ToString(value);
        }

        private IEnumerable<object> EnumerateCollection(object collection)
        {
            if (collection == null)
                yield break;

            System.Collections.IEnumerable enumerable =
                collection as System.Collections.IEnumerable;

            if (enumerable == null)
                yield break;

            foreach (object item in enumerable)
                yield return item;
        }

        private void InvokeClear(object collection)
        {
            try
            {
                if (collection == null)
                    return;

                MethodInfo clear = collection.GetType().GetMethod(
                    "Clear",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    Type.EmptyTypes,
                    null);

                if (clear != null)
                    clear.Invoke(collection, null);
            }
            catch
            {
            }
        }

        private void InvokeAdd(object collection, object value)
        {
            try
            {
                if (collection == null || value == null)
                    return;

                MethodInfo add = null;

                foreach (MethodInfo method in collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (!string.Equals(method.Name, "Add", StringComparison.Ordinal))
                        continue;

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length != 1)
                        continue;

                    if (!parameters[0].ParameterType.IsAssignableFrom(value.GetType()))
                        continue;

                    add = method;
                    break;
                }

                if (add != null)
                    add.Invoke(collection, new object[] { value });
            }
            catch
            {
            }
        }

        private object ClonePredicate(object predicate)
        {
            if (predicate == null)
                return null;

            try
            {
                Type type = predicate.GetType();
                object clone = Activator.CreateInstance(type);

                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (!property.CanRead || !property.CanWrite)
                        continue;

                    object value = property.GetValue(predicate, null);
                    property.SetValue(clone, value, null);
                }

                return clone;
            }
            catch
            {
                return predicate;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // DTO interno para filtros runtime
        // ─────────────────────────────────────────────────────────────

        private sealed class RuntimeColumnFilterSnapshot
        {
            public string ColumnName { get; set; }
            public List<object> Predicates { get; set; }
        }
    }

}
