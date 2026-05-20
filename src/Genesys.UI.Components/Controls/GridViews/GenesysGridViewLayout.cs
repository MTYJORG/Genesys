using System;
using System.Collections.Generic;

namespace Genesys.UI.Components.Controls.GridViews
{
    [Serializable]
    public class GenesysGridViewLayout
    {
        public GenesysGridViewLayout()
        {
            Columns = new List<GenesysGridColumnLayout>();
            Groups = new List<GenesysGridGroupLayout>();
            Summaries = new List<GenesysGridSummaryLayout>();
        }

        public string GridKey { get; set; }
        public string ViewName { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public List<GenesysGridColumnLayout> Columns { get; set; }
        public List<GenesysGridGroupLayout> Groups { get; set; }
        public List<GenesysGridSummaryLayout> Summaries { get; set; }
    }
}
