using Syncfusion.Data;
using Syncfusion.WinForms.DataGrid;
using Syncfusion.WinForms.DataGrid.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.GridViews
{
    public class GenesysGridLayoutService
    {
        private readonly SfDataGrid grid;
        private readonly GenesysGridSummaryService summaryService;
        private readonly Func<bool> getIsApplyingLayout;
        private readonly Action<bool> setIsApplyingLayout;

        public GenesysGridLayoutService(
            SfDataGrid grid,
            GenesysGridSummaryService summaryService,
            Func<bool> getIsApplyingLayout,
            Action<bool> setIsApplyingLayout)
        {
            this.grid = grid;
            this.summaryService = summaryService;
            this.getIsApplyingLayout = getIsApplyingLayout;
            this.setIsApplyingLayout = setIsApplyingLayout;
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

            CaptureNativeGridLayout(layout);

            System.Diagnostics.Debug.WriteLine("===== GRID LAYOUT SERVICE: Capture =====");
            System.Diagnostics.Debug.WriteLine("ViewName: " + layout.ViewName);
            System.Diagnostics.Debug.WriteLine("Columns: " + (layout.Columns == null ? 0 : layout.Columns.Count));
            System.Diagnostics.Debug.WriteLine("Groups: " + (layout.Groups == null ? 0 : layout.Groups.Count));
            System.Diagnostics.Debug.WriteLine("Summaries: " + (layout.Summaries == null ? 0 : layout.Summaries.Count));
            System.Diagnostics.Debug.WriteLine("Sorts: " + (layout.Sorts == null ? 0 : layout.Sorts.Count));
            System.Diagnostics.Debug.WriteLine("FilterStateXml length: " + (layout.FilterStateXml == null ? 0 : layout.FilterStateXml.Length));
            System.Diagnostics.Debug.WriteLine("NativeGridLayoutXml length: " + (layout.NativeGridLayoutXml == null ? 0 : layout.NativeGridLayoutXml.Length));

            return layout;
        }

        public void Apply(GenesysGridViewLayout layout)
        {
            if (layout == null)
                return;

            System.Diagnostics.Debug.WriteLine("===== GRID LAYOUT SERVICE: Apply START =====");
            System.Diagnostics.Debug.WriteLine("ViewName: " + layout.ViewName);
            System.Diagnostics.Debug.WriteLine("Columns: " + (layout.Columns == null ? 0 : layout.Columns.Count));
            System.Diagnostics.Debug.WriteLine("Groups: " + (layout.Groups == null ? 0 : layout.Groups.Count));
            System.Diagnostics.Debug.WriteLine("Summaries: " + (layout.Summaries == null ? 0 : layout.Summaries.Count));
            System.Diagnostics.Debug.WriteLine("Sorts: " + (layout.Sorts == null ? 0 : layout.Sorts.Count));
            System.Diagnostics.Debug.WriteLine("FilterStateXml length: " + (layout.FilterStateXml == null ? 0 : layout.FilterStateXml.Length));
            System.Diagnostics.Debug.WriteLine("NativeGridLayoutXml length: " + (layout.NativeGridLayoutXml == null ? 0 : layout.NativeGridLayoutXml.Length));

            // FilterStateXml pertenece al panel superior de filtros. No se aplica desde la vista.
            // La vista aplica únicamente estado del grid: columnas, grupos, summaries, sort y filtros internos del SfDataGrid.

            ClearGroupDescriptions();
            ClearSortDescriptions();
            summaryService.ClearSummaryRows();

            if (layout.Columns != null)
            {
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
            }

            ReorderColumns(layout);
            ApplyGroups(layout);
            summaryService.Apply(layout);
            ApplySorts(layout);
            ApplyNativeGridLayoutDelayed(layout);

            System.Diagnostics.Debug.WriteLine("===== GRID LAYOUT SERVICE: Apply END =====");
        }

        public void ApplyDefaultLayout(string defaultNativeGridLayoutXml)
        {
            if (!string.IsNullOrWhiteSpace(defaultNativeGridLayoutXml))
                ApplyNativeGridLayoutNow(defaultNativeGridLayoutXml);
            else
                ClearNativeGridRuntimeState();

            ClearGroupDescriptions();
            ClearSortDescriptions();
            summaryService.ClearSummaryRows();

            foreach (GridColumn column in grid.Columns)
                column.Visible = true;

            grid.AutoSizeColumnsMode = AutoSizeColumnsMode.AllCells;
        }

        public string CaptureDefaultNativeGridLayoutIfNeeded(string defaultNativeGridLayoutXml)
        {
            if (!string.IsNullOrWhiteSpace(defaultNativeGridLayoutXml))
                return defaultNativeGridLayoutXml;

            string xml = CaptureCurrentNativeGridLayoutXml("DEFAULT");
            System.Diagnostics.Debug.WriteLine("Default native grid layout captured length: " +
                (xml == null ? 0 : xml.Length));

            return xml;
        }

        private void ReorderColumns(GenesysGridViewLayout layout)
        {
            if (layout == null || layout.Columns == null)
                return;

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

        private void CaptureNativeGridLayout(GenesysGridViewLayout layout)
        {
            if (layout == null)
                return;

            layout.NativeGridLayoutXml = null;

            if (grid == null)
            {
                System.Diagnostics.Debug.WriteLine("===== NATIVE GRID SERIALIZE =====");
                System.Diagnostics.Debug.WriteLine("grid null");
                System.Diagnostics.Debug.WriteLine("=================================");
                return;
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    bool serialized = TryInvokeNativeSerialize(stream);

                    if (!serialized)
                    {
                        System.Diagnostics.Debug.WriteLine("===== NATIVE GRID SERIALIZE =====");
                        System.Diagnostics.Debug.WriteLine("No se encontró un overload compatible de Serialize(Stream) o Serialize(Stream, options).");
                        System.Diagnostics.Debug.WriteLine("=================================");
                        return;
                    }

                    stream.Position = 0;

                    using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                    {
                        layout.NativeGridLayoutXml = reader.ReadToEnd();
                    }
                }

                System.Diagnostics.Debug.WriteLine("===== NATIVE GRID SERIALIZE =====");
                System.Diagnostics.Debug.WriteLine("NativeGridLayoutXml null: " + string.IsNullOrWhiteSpace(layout.NativeGridLayoutXml));
                System.Diagnostics.Debug.WriteLine("NativeGridLayoutXml length: " + (layout.NativeGridLayoutXml == null ? 0 : layout.NativeGridLayoutXml.Length));
                System.Diagnostics.Debug.WriteLine("NativeGridLayoutXml preview: " + SafePreview(layout.NativeGridLayoutXml, 400));
                System.Diagnostics.Debug.WriteLine("=================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("===== NATIVE GRID SERIALIZE ERROR =====");
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                System.Diagnostics.Debug.WriteLine("=======================================");
                layout.NativeGridLayoutXml = null;
            }
        }

        private string CaptureCurrentNativeGridLayoutXml(string reason)
        {
            if (grid == null)
                return null;

            try
            {
                using (var stream = new MemoryStream())
                {
                    bool serialized = TryInvokeNativeSerialize(stream);

                    if (!serialized)
                    {
                        System.Diagnostics.Debug.WriteLine("CaptureCurrentNativeGridLayoutXml(" + reason + "): serialize no compatible.");
                        return null;
                    }

                    stream.Position = 0;

                    using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                    {
                        string xml = reader.ReadToEnd();
                        System.Diagnostics.Debug.WriteLine("CaptureCurrentNativeGridLayoutXml(" + reason + ") length: " +
                            (xml == null ? 0 : xml.Length));
                        return xml;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("CaptureCurrentNativeGridLayoutXml ERROR " + reason + ":");
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return null;
            }
        }

        private void ApplyNativeGridLayoutDelayed(GenesysGridViewLayout layout)
        {
            string xml = layout == null ? null : layout.NativeGridLayoutXml;

            System.Diagnostics.Debug.WriteLine("===== NATIVE GRID DESERIALIZE QUEUED =====");
            System.Diagnostics.Debug.WriteLine("Layout null: " + (layout == null));
            System.Diagnostics.Debug.WriteLine("NativeGridLayoutXml length: " + (xml == null ? 0 : xml.Length));

            if (grid == null || string.IsNullOrWhiteSpace(xml))
            {
                System.Diagnostics.Debug.WriteLine("ApplyNativeGridLayoutDelayed omitido.");
                return;
            }

            grid.BeginInvoke(new Action(delegate
            {
                ApplyNativeGridLayoutNow(xml);
            }));
        }

        private void ApplyNativeGridLayoutNow(string xml)
        {
            System.Diagnostics.Debug.WriteLine("===== NATIVE GRID DESERIALIZE START =====");
            System.Diagnostics.Debug.WriteLine("NativeGridLayoutXml length: " + (xml == null ? 0 : xml.Length));
            System.Diagnostics.Debug.WriteLine("Rows BEFORE native deserialize: " + GetGridVisibleRecordCount());

            if (grid == null || string.IsNullOrWhiteSpace(xml))
            {
                System.Diagnostics.Debug.WriteLine("ApplyNativeGridLayoutNow omitido.");
                System.Diagnostics.Debug.WriteLine("===== NATIVE GRID DESERIALIZE END =====");
                return;
            }

            bool oldApplyingLayout = getIsApplyingLayout == null ? false : getIsApplyingLayout();

            if (setIsApplyingLayout != null)
                setIsApplyingLayout(true);

            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
                {
                    bool deserialized = TryInvokeNativeDeserialize(stream);
                    System.Diagnostics.Debug.WriteLine("Native Deserialize invoked: " + deserialized);
                }

                grid.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ApplyNativeGridLayoutNow ERROR:");
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            finally
            {
                if (setIsApplyingLayout != null)
                    setIsApplyingLayout(oldApplyingLayout);
            }

            System.Diagnostics.Debug.WriteLine("Rows AFTER native deserialize: " + GetGridVisibleRecordCount());
            System.Diagnostics.Debug.WriteLine("===== NATIVE GRID DESERIALIZE END =====");
        }

        private int GetGridVisibleRecordCount()
        {
            try
            {
                if (grid != null && grid.View != null && grid.View.Records != null)
                    return grid.View.Records.Count;
            }
            catch
            {
            }

            return -1;
        }

        private bool TryInvokeNativeSerialize(Stream stream)
        {
            MethodInfo serializeWithOptions = null;
            object options = CreateSerializationOptions(true);

            if (options != null)
            {
                serializeWithOptions = FindMethod(
                    grid.GetType(),
                    "Serialize",
                    typeof(Stream),
                    options.GetType());

                if (serializeWithOptions != null)
                {
                    serializeWithOptions.Invoke(grid, new object[] { stream, options });
                    return true;
                }
            }

            MethodInfo serializeStreamOnly = FindMethod(
                grid.GetType(),
                "Serialize",
                typeof(Stream));

            if (serializeStreamOnly != null)
            {
                serializeStreamOnly.Invoke(grid, new object[] { stream });
                return true;
            }

            return false;
        }

        private bool TryInvokeNativeDeserialize(Stream stream)
        {
            MethodInfo deserializeWithOptions = null;
            object options = CreateDeserializationOptions(true);

            if (options != null)
            {
                deserializeWithOptions = FindMethod(
                    grid.GetType(),
                    "Deserialize",
                    typeof(Stream),
                    options.GetType());

                if (deserializeWithOptions != null)
                {
                    deserializeWithOptions.Invoke(grid, new object[] { stream, options });
                    return true;
                }
            }

            MethodInfo deserializeStreamOnly = FindMethod(
                grid.GetType(),
                "Deserialize",
                typeof(Stream));

            if (deserializeStreamOnly != null)
            {
                deserializeStreamOnly.Invoke(grid, new object[] { stream });
                return true;
            }

            return false;
        }

        private MethodInfo FindMethod(Type type, string name, params Type[] parameterTypes)
        {
            if (type == null)
                return null;

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!string.Equals(method.Name, name, StringComparison.Ordinal))
                    continue;

                var parameters = method.GetParameters();

                if (parameters.Length != parameterTypes.Length)
                    continue;

                bool match = true;

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (!parameters[i].ParameterType.IsAssignableFrom(parameterTypes[i]))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                    return method;
            }

            return null;
        }

        private object CreateSerializationOptions(bool enabled)
        {
            return CreateNativeOptions("SerializationOptions", enabled);
        }

        private object CreateDeserializationOptions(bool enabled)
        {
            return CreateNativeOptions("DeserializationOptions", enabled);
        }

        private object CreateNativeOptions(string typeNameSuffix, bool enabled)
        {
            try
            {
                Assembly assembly = grid.GetType().Assembly;

                Type optionsType = assembly.GetTypes()
                    .FirstOrDefault(x => x.Name.Equals(typeNameSuffix, StringComparison.OrdinalIgnoreCase));

                if (optionsType == null)
                {
                    System.Diagnostics.Debug.WriteLine("Native options type not found: " + typeNameSuffix);
                    return null;
                }

                object options = Activator.CreateInstance(optionsType);

                SetBooleanOption(options, "SerializeFiltering", enabled);
                SetBooleanOption(options, "SerializeSorting", enabled);
                SetBooleanOption(options, "SerializeGrouping", enabled);
                SetBooleanOption(options, "SerializeColumns", enabled);
                SetBooleanOption(options, "SerializeTableSummaries", enabled);

                SetBooleanOption(options, "DeserializeFiltering", enabled);
                SetBooleanOption(options, "DeserializeSorting", enabled);
                SetBooleanOption(options, "DeserializeGrouping", enabled);
                SetBooleanOption(options, "DeserializeColumns", enabled);
                SetBooleanOption(options, "DeserializeTableSummaries", enabled);

                System.Diagnostics.Debug.WriteLine("Native options created: " + optionsType.FullName);

                return options;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("CreateNativeOptions ERROR: " + ex.Message);
                return null;
            }
        }

        private void SetBooleanOption(object options, string propertyName, bool value)
        {
            if (options == null)
                return;

            PropertyInfo property = options.GetType().GetProperty(propertyName);

            if (property == null || !property.CanWrite || property.PropertyType != typeof(bool))
                return;

            property.SetValue(options, value, null);
        }

        private void ClearNativeGridRuntimeState()
        {
            System.Diagnostics.Debug.WriteLine("===== GRID LAYOUT SERVICE: ClearNativeGridRuntimeState START =====");

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

            try
            {
                grid.Refresh();
            }
            catch
            {
            }

            System.Diagnostics.Debug.WriteLine("===== GRID LAYOUT SERVICE: ClearNativeGridRuntimeState END =====");
        }

        private void InvokeGridMethodIfExists(string methodName)
        {
            try
            {
                MethodInfo method = grid == null ? null : grid.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
                if (method == null)
                    return;

                method.Invoke(grid, null);
                System.Diagnostics.Debug.WriteLine("Invoked grid method: " + methodName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("InvokeGridMethodIfExists ERROR " + methodName + ": " + ex.Message);
            }
        }

        private void ClearCollectionProperty(object target, string propertyName)
        {
            try
            {
                if (target == null || string.IsNullOrWhiteSpace(propertyName))
                    return;

                PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                if (property == null)
                    return;

                object value = property.GetValue(target, null);
                if (value == null)
                    return;

                MethodInfo clear = value.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
                if (clear == null)
                    return;

                clear.Invoke(value, null);
                System.Diagnostics.Debug.WriteLine("Cleared collection property: " + target.GetType().Name + "." + propertyName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ClearCollectionProperty ERROR " + propertyName + ": " + ex.Message);
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

        private string SafePreview(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            string value = text.Replace("\r", " ").Replace("\n", " ");

            if (value.Length <= maxLength)
                return value;

            return value.Substring(0, maxLength) + "...";
        }
    }
}
