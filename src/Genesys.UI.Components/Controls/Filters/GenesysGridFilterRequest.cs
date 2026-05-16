using System;
using System.Collections.Generic;

namespace Genesys.UI.Components.Controls.Filters
{
    public class GenesysGridFilterRequest
    {
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFinal { get; set; }

        public string LookupValue { get; set; }
        public string LookupDescripcion { get; set; }

        public object ComboValue { get; set; }

        public string StoredProcedureName { get; set; }

        public Dictionary<string, object> ExtraParameters { get; private set; }

        public GenesysGridFilterRequest()
        {
            ExtraParameters = new Dictionary<string, object>();
        }
    }
}