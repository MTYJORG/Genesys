using System.Collections.Generic;
using System.Data;

namespace Genesys.UI.Controls
{
    public interface ILookupProvider
    {
        DataTable Search();                     // 🔍 Para el lookup (F3)
        LookupResult GetByValue(string value);  // ✔ Para validar 1 valor
    }
}