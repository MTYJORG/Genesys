using System.Data;

namespace Genesys.UI.Components.Controls.Filters
{
    public class GenesysGridFilterResult
    {
        public DataTable Table { get; set; }
        public DataSet DataSet { get; set; }

        public bool HasTable
        {
            get { return Table != null; }
        }

        public bool HasDataSet
        {
            get { return DataSet != null; }
        }
    }
}