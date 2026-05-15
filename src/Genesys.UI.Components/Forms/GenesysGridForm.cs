using Genesys.UI.Components.Controls.Containers;
using System.Drawing;
using System.Windows.Forms;

namespace Genesys.UI.Components.Forms
{
    public class GenesysGridForm : GenesysBaseForm
    {
        protected FiltersContainer FiltersPanel { get; private set; }

        protected Panel GridContentPanel { get; private set; }

        protected InformativePanel InformativePanel { get; private set; }

        public GenesysGridForm()
        {
            InitializeGridLayout();
        }

        private void InitializeGridLayout()
        {
            FiltersPanel = new FiltersContainer
            {
                Dock = DockStyle.Top
            };

            InformativePanel = new InformativePanel
            {
                Dock = DockStyle.Bottom
            };

            GridContentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            MainContentPanel.Controls.Add(GridContentPanel);

            MainContentPanel.Controls.Add(InformativePanel);

            MainContentPanel.Controls.Add(FiltersPanel);
        }
    }
}