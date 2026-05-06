using System.Data;

namespace Genesys.UI.Controls
{
    public class LookupResult
    {
        public string Value { get; set; }           // Llave
        public string Description { get; set; }     // Descripción
        public DataRow Data { get; set; }           // Todo el Renglon
    }
}

