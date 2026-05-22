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
