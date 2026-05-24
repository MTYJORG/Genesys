using Syncfusion.Data;
using Syncfusion.WinForms.DataGrid;
using Syncfusion.WinForms.DataGrid.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.GridViews
{
    public class GenesysGridLayoutService
    {
        private readonly SfDataGrid grid;
        private readonly GenesysGridSummaryService summaryService;
        private readonly Dictionary<GenesysGridViewLayout, List<RuntimeColumnFilterSnapshot>> runtimeFilterSnapshots;

        public GenesysGridLayoutService(
            SfDataGrid grid,
            GenesysGridSummaryService summaryService,
            Func<bool> getIsApplyingLayout,
            Action<bool> setIsApplyingLayout)
        {
            this.grid = grid;
            this.summaryService = summaryService;
            runtimeFilterSnapshots = new Dictionary<GenesysGridViewLayout, List<RuntimeColumnFilterSnapshot>>();
        }

        // ─────────────────────────────────────────────────────────────
        // API pública
        // ─────────────────────────────────────────────────────────────

        public GenesysGridViewLayout Capture(
            string viewName,
            string gridKey,
            Action<GenesysGridViewLayout> captureFilters)
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

        public void Apply(GenesysGridViewLayout layout)
        {
            Apply(layout, false);
        }

        public void Apply(GenesysGridViewLayout layout, bool applyNativeLayout)
        {
            if (layout == null)
                return;

            // Native layout de Syncfusion queda retirado. La vista se aplica manualmente.
            ResetLayoutState();

            ApplyColumns(layout);
            ReorderColumns(layout);
            ApplyRuntimeFilters(layout);
            ApplyGroups(layout);
            summaryService.Apply(layout);
            ApplySorts(layout);
        }

        public void ApplyDefaultLayout(string ignoredNativeLayoutXml)
        {
            ClearNativeGridRuntimeState();
            ResetLayoutState();

            foreach (GridColumn column in grid.Columns)
                column.Visible = true;

            grid.AutoSizeColumnsMode = AutoSizeColumnsMode.AllCells;
        }

        // ─────────────────────────────────────────────────────────────
        // Reset centralizado del estado visual/runtime del grid
        // Mantener juntos: groups, sorts, filtros internos y summaries.
        // ─────────────────────────────────────────────────────────────

        private void ResetLayoutState()
        {
            ClearGroupDescriptions();
            ClearSortDescriptions();
            ClearFilterDescriptions();
            summaryService.ClearSummaryRows();
        }

        // ─────────────────────────────────────────────────────────────
        // Columnas: ancho, visible, formato y orden visual
        // ─────────────────────────────────────────────────────────────

        private void ApplyColumns(GenesysGridViewLayout layout)
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

        private void ReorderColumns(GenesysGridViewLayout layout)
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

        private void CaptureSorts(GenesysGridViewLayout layout)
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

        private void ApplySorts(GenesysGridViewLayout layout)
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

        // ─────────────────────────────────────────────────────────────
        // Filtros internos del SfDataGrid
        // Se capturan/aplican como estado runtime temporal asociado al layout.
        // ─────────────────────────────────────────────────────────────

        private void CaptureRuntimeFilters(GenesysGridViewLayout layout)
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

        private void ApplyRuntimeFilters(GenesysGridViewLayout layout)
        {
            if (layout == null)
                return;

            List<RuntimeColumnFilterSnapshot> snapshots;
            if (!runtimeFilterSnapshots.TryGetValue(layout, out snapshots))
                return;

            if (snapshots == null || snapshots.Count == 0)
                return;

            foreach (RuntimeColumnFilterSnapshot snapshot in snapshots)
            {
                if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.ColumnName))
                    continue;

                GridColumn column = FindColumn(snapshot.ColumnName);
                if (column == null)
                    continue;

                object predicates = GetPropertyValue(column, "FilterPredicates");
                if (predicates == null)
                    continue;

                InvokeClear(predicates);

                foreach (object predicate in snapshot.Predicates)
                {
                    if (predicate == null)
                        continue;

                    InvokeAdd(predicates, ClonePredicate(predicate));
                }
            }

            RefreshFilterView();
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
