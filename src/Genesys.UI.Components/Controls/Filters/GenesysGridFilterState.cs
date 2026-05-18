using System;

namespace Genesys.UI.Components.Controls.Filters
{
    public class GenesysGridFilterState
    {
        public string RangoFecha { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFinal { get; set; }

        public string LookupValue { get; set; }

        public object ComboValue { get; set; }
    }
}