using System;

namespace Genesys.UI.Components.Controls.GridViews
{
    [Serializable]
    public class GenesysGridViewState
    {
        public string GridKey { get; set; }
        public string CurrentViewName { get; set; }
        public DateTime ModifiedAt { get; set; }
    }
}
