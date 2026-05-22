using System;

namespace Genesys.UI.Components.Controls.GridViews
{
    [Serializable]
    public class GenesysGridSortLayout
    {
        public string ColumnName { get; set; }
        public string SortDirection { get; set; }

        public GenesysGridSortLayout()
        {
        }

        public GenesysGridSortLayout(string columnName, string sortDirection)
        {
            ColumnName = columnName;
            SortDirection = sortDirection;
        }
    }
}
