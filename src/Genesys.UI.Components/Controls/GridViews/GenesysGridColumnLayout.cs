using System;

namespace Genesys.UI.Components.Controls.GridViews
{
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
}
