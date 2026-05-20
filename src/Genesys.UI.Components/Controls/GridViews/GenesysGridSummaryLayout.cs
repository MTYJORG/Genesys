using System;

namespace Genesys.UI.Components.Controls.GridViews
{
    [Serializable]
    public class GenesysGridSummaryLayout
    {
        public string ColumnName { get; set; }
        public string SummaryType { get; set; }
        public string Format { get; set; }
    }
}
