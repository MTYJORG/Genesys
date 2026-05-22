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
        public string NativeGridLayoutXml { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public List<GenesysGridColumnLayout> Columns { get; set; }
        public List<GenesysGridGroupLayout> Groups { get; set; }
        public List<GenesysGridSummaryLayout> Summaries { get; set; }
        public List<GenesysGridSortLayout> Sorts { get; set; }
    }
}
