using System;
using System.Collections.Generic;

namespace Genesys.UI.Components.Controls.GridViews
{
    [Serializable]
    public class GenesysGridViewLayout
    {
        public GenesysGridViewLayout()
        {
            IncludeFilters = true;

            Columns = new List<GenesysGridColumnLayout>();
            Groups = new List<GenesysGridGroupLayout>();
            Summaries = new List<GenesysGridSummaryLayout>();
            Sorts = new List<GenesysGridSortLayout>();

            // Filtros internos del SfDataGrid
            GridFilters = new List<GenesysGridColumnFilterLayout>();
        }

        public string GridKey { get; set; }
        public string ViewName { get; set; }
        public bool IsDefault { get; set; }
        public bool IncludeFilters { get; set; }
        public string FilterStateXml { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }

        public List<GenesysGridColumnLayout> Columns { get; set; }
        public List<GenesysGridGroupLayout> Groups { get; set; }
        public List<GenesysGridSummaryLayout> Summaries { get; set; }
        public List<GenesysGridSortLayout> Sorts { get; set; }

        // Persistencia filtros internos del grid
        public List<GenesysGridColumnFilterLayout> GridFilters { get; set; }
    }

    [Serializable]
    public class GenesysGridColumnLayout
    {
        public string MappingName { get; set; }
        public string HeaderText { get; set; }
        public int DisplayIndex { get; set; }
        public double Width { get; set; }
        public bool Visible { get; set; }
        public string Format { get; set; }
    }

    [Serializable]
    public class GenesysGridGroupLayout
    {
        public string ColumnName { get; set; }
    }

    [Serializable]
    public class GenesysGridSortLayout
    {
        public string ColumnName { get; set; }
        public string SortDirection { get; set; }
    }

    // ============================================================
    // Filtros internos del SfDataGrid
    // ============================================================

    [Serializable]
    public class GenesysGridColumnFilterLayout
    {
        public GenesysGridColumnFilterLayout()
        {
            Predicates = new List<GenesysGridFilterPredicateLayout>();
        }

        public string ColumnName { get; set; }

        public List<GenesysGridFilterPredicateLayout> Predicates { get; set; }
    }

    [Serializable]
    public class GenesysGridFilterPredicateLayout
    {
        public string FilterBehavior { get; set; }
        public string FilterMode { get; set; }
        public string FilterText { get; set; }
        public string FilterType { get; set; }
        public string FilterValue { get; set; }
        public string FilterValueType { get; set; }
        public bool IsCaseSensitive { get; set; }
        public string PredicateType { get; set; }
    }

    [Serializable]
    public class GenesysGridSummaryLayout
    {
        public string ColumnName { get; set; }
        public string SummaryType { get; set; }
        public string Format { get; set; }
    }

    [Serializable]
    public class GenesysGridViewState
    {
        public string GridKey { get; set; }
        public string CurrentViewName { get; set; }
        public DateTime ModifiedAt { get; set; }
    }

    public class GenesysGridColumnProfile
    {
        public string ColumnName { get; set; }
        public string HeaderText { get; set; }
        public bool Visible { get; set; }
        public double Width { get; set; }
        public string Format { get; set; }
        public int Decimals { get; set; }
        public string Alignment { get; set; }
        public string SummaryType { get; set; }
        public bool Grouped { get; set; }
        public bool Frozen { get; set; }
    }
}