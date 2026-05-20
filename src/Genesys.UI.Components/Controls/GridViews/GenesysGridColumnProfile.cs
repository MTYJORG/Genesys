using System;

namespace Genesys.UI.Components.Controls.GridViews
{
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
