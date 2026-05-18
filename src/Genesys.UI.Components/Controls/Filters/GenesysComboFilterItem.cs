namespace Genesys.UI.Components.Controls.Filters
{
    public class GenesysComboFilterItem
    {
        public string Text { get; set; }
        public object Value { get; set; }

        public GenesysComboFilterItem()
        {
        }

        public GenesysComboFilterItem(string text, object value)
        {
            Text = text;
            Value = value;
        }
    }
}