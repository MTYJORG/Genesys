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

        public GenesysGridLayoutService(
            SfDataGrid grid,
            GenesysGridSummaryService summaryService,
            Func<bool> getIsApplyingLayout,
            Action<bool> setIsApplyingLayout)
        {
            this.grid = grid;
            this.summaryService = summaryService;
        }

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
            ClearGroupDescriptions();
            ClearSortDescriptions();
            summaryService.ClearSummaryRows();

            ApplyColumns(layout);
            ReorderColumns(layout);
            ApplyGroups(layout);
            summaryService.Apply(layout);
            ApplySorts(layout);
        }

        public void ApplyDefaultLayout(string ignoredNativeLayoutXml)
        {
            ClearNativeGridRuntimeState();
            ClearGroupDescriptions();
            ClearSortDescriptions();
            summaryService.ClearSummaryRows();

            foreach (GridColumn column in grid.Columns)
                column.Visible = true;

            grid.AutoSizeColumnsMode = AutoSizeColumnsMode.AllCells;
        }


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

                if (!string.IsNullOrWhiteSpace(savedColumn.Format))
                    column.Format = savedColumn.Format;
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

                MethodInfo clear = value.GetType().GetMethod(
                    "Clear",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    Type.EmptyTypes,
                    null);

                if (clear == null)
                    return;

                clear.Invoke(value, null);
            }
            catch
            {
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
    }
}
