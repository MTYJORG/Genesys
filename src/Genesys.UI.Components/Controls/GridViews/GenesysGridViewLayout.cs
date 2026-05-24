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

        // Filtros internos del SfDataGrid.
        // A diferencia de FilterStateXml, estos pertenecen al grid,
        // no al panel superior de filtros.
        public List<GenesysGridColumnFilterLayout> GridFilters { get; set; }
    }

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
        public string FilterType { get; set; }
        public string PredicateType { get; set; }
        public string FilterBehavior { get; set; }
        public string FilterMode { get; set; }
        public string FilterText { get; set; }
        public string IsCaseSensitive { get; set; }
        public string FilterValue { get; set; }
        public string FilterValueType { get; set; }
    }
}
