using System;
using System.Collections.Generic;

namespace Genesys.UI.Components.Controls.Filters
{
    public class GenesysGridFilterRequest
    {
        public string StoredProcedureName { get; set; }
        public string TipoDeAccion { get; set; }

        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFinal { get; set; }
        public string LookupParameterName { get; set; }
        public string ComboParameterName { get; set; }

        public string LookupValue { get; set; }
        
        public object ComboValue { get; set; }

        public Dictionary<string, object> ExtraParameters { get; private set; }

        public GenesysGridFilterRequest()
        {
            ExtraParameters = new Dictionary<string, object>();
        }
    }
}