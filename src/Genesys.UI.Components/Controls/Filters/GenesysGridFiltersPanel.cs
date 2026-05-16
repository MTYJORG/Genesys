using Genesys.UI.Components.Controls.Filters;
using Genesys.UI.Controls;
using System;
using System.Data;
using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.Filters
{
    public class GenesysGridFiltersPanel : UserControl
    {
        private readonly Label lblFecha;
        private readonly DateTimePicker dtpInicio;
        private readonly DateTimePicker dtpFinal;

        private readonly Label lblLookup;
        private readonly TextBox txtLookupDescripcion;

        private readonly Label lblCombo;
        private readonly ComboBox cboFiltro;

        private readonly GenesysGridDataProvider dataProvider;

        public aTextBox LookupTextBox { get; }

        public string StoredProcedureName { get; set; }

        public event EventHandler<GenesysGridFilterResult> SearchCompleted;

        public GenesysGridFiltersPanel()
        {
            dataProvider = new GenesysGridDataProvider();

            Dock = DockStyle.Top;
            Height = 80;

            lblFecha = new Label();
            dtpInicio = new DateTimePicker();
            dtpFinal = new DateTimePicker();

            lblLookup = new Label();
            LookupTextBox = new aTextBox();
            txtLookupDescripcion = new TextBox();

            lblCombo = new Label();
            cboFiltro = new ComboBox();

            BuildLayout();
            WireEvents();
        }

        private void BuildLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 8,
                RowCount = 2
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));

            lblFecha.Text = "Fechas";
            lblLookup.Text = "Lookup";
            lblCombo.Text = "Estatus";

            txtLookupDescripcion.ReadOnly = true;

            layout.Controls.Add(lblFecha, 0, 0);
            layout.Controls.Add(dtpInicio, 1, 0);
            layout.Controls.Add(dtpFinal, 2, 0);

            layout.Controls.Add(lblLookup, 3, 0);
            layout.Controls.Add(LookupTextBox, 4, 0);
            layout.Controls.Add(txtLookupDescripcion, 5, 0);

            layout.Controls.Add(lblCombo, 6, 0);
            layout.Controls.Add(cboFiltro, 7, 0);

            Controls.Add(layout);
        }

        private void WireEvents()
        {
            dtpInicio.Leave += delegate { ExecuteSearch(); };
            dtpFinal.Leave += delegate { ExecuteSearch(); };
            LookupTextBox.Leave += delegate { ExecuteSearch(); };
            cboFiltro.Leave += delegate { ExecuteSearch(); };
        }

        public void SetFechaTitle(string title)
        {
            lblFecha.Text = title;
        }

        public void SetLookupTitle(string title)
        {
            lblLookup.Text = title;
        }

        public void SetLookupDescriptionControl(Control control)
        {
            LookupTextBox.LookupControl = control;
        }

        public void SetComboTitle(string title)
        {
            lblCombo.Text = title;
        }

        public void SetComboDataSource(object dataSource, string displayMember, string valueMember)
        {
            cboFiltro.DataSource = dataSource;
            cboFiltro.DisplayMember = displayMember;
            cboFiltro.ValueMember = valueMember;
        }

        public GenesysGridFilterRequest BuildRequest()
        {
            return new GenesysGridFilterRequest
            {
                FechaInicio = dtpInicio.Value.Date,
                FechaFinal = dtpFinal.Value.Date,
                LookupValue = LookupTextBox.Text,
                LookupDescripcion = txtLookupDescripcion.Text,
                ComboValue = cboFiltro.SelectedValue,
                StoredProcedureName = StoredProcedureName
            };
        }

        public void ExecuteSearch()
        {
            if (string.IsNullOrWhiteSpace(StoredProcedureName))
                return;

            var request = BuildRequest();
            var result = dataProvider.Execute(request);

            SearchCompleted?.Invoke(this, result);
        }
    }
}